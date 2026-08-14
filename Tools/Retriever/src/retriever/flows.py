from __future__ import annotations

import shutil
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Sequence

from retriever.config import AppConfig
from retriever.manager import IndexDirectoryResult, IndexFileResult, RetrieverManager
from retriever.models import SearchResult
from retriever.process_lock import ServiceLock


@dataclass(frozen=True, slots=True)
class DatabaseDeleteResult:
    storage_dir: str
    deleted_paths: tuple[str, ...]

    def as_dict(self) -> dict[str, object]:
        return {
            "storage_dir": self.storage_dir,
            "deleted_paths": list(self.deleted_paths),
        }


def batch_write_documents(
    manager: RetrieverManager,
    directory: str | Path | None = None,
    *,
    force: bool = False,
    continue_on_error: bool = False,
) -> list[IndexDirectoryResult]:
    """Scan and write one directory or every enabled registered directory."""
    if directory is not None:
        return [
            manager.index_directory(
                directory,
                force=force,
                continue_on_error=continue_on_error,
            )
        ]
    return manager.index_all(
        force=force,
        continue_on_error=continue_on_error,
    )


def change_document(
    manager: RetrieverManager, source_path: str | Path
) -> IndexFileResult | None:
    """Apply one source-file creation, modification, move, or deletion."""
    return manager.handle_path_change(source_path)


def delete_database(
    config: AppConfig,
    *,
    process_cwd: str | Path | None = None,
) -> DatabaseDeleteResult:
    """Physically delete Catalog and search databases while they are offline."""
    cwd = Path(process_cwd or Path.cwd()).resolve()
    storage_dir = config.resolve_storage_dir(cwd)
    _validate_delete_target(storage_dir, cwd)
    runtime_dir = storage_dir / "runtime"
    lock_path = runtime_dir / "database.lock"
    deleted: list[str] = []
    with ServiceLock(lock_path):
        for target in (storage_dir / "manager", storage_dir / "search"):
            if not target.exists():
                continue
            shutil.rmtree(target)
            deleted.append(str(target))
    lock_path.unlink(missing_ok=True)
    return DatabaseDeleteResult(str(storage_dir), tuple(deleted))


def search_documents(
    manager: RetrieverManager,
    query: str | None = None,
    *,
    keywords: list[str] | tuple[str, ...] | None = None,
    k: int | None = None,
    directory: str | Path | None = None,
) -> list[SearchResult]:
    """Run one hybrid document search."""
    return manager.search(query, keywords=keywords, k=k, directory=directory)


def search_documents_detailed(
    manager: RetrieverManager,
    query: str | None = None,
    *,
    keywords: Sequence[str] | None = None,
    k: int | None = None,
    directory: str | Path | None = None,
) -> dict[str, Any]:
    """Run keyword BM25 search with OOV expansion and review metadata."""
    return manager.search_detailed(
        query,
        keywords=keywords,
        k=k,
        directory=directory,
    )


def submit_synonym_feedback(
    manager: RetrieverManager,
    *,
    search_id: str,
    directory_id: int,
    query_term: str,
    candidate_term: str,
    verdict: str,
) -> dict[str, Any]:
    """Record one idempotent Agent decision for a review request."""
    return manager.submit_synonym_feedback(
        search_id=search_id,
        directory_id=directory_id,
        query_term=query_term,
        candidate_term=candidate_term,
        verdict=verdict,
    )


def rebuild_index(
    manager: RetrieverManager,
    directory: str | Path | None = None,
) -> list[IndexDirectoryResult]:
    """Build, validate, and atomically activate a new directory generation."""
    return manager.rebuild(directory)


def _validate_delete_target(storage_dir: Path, process_cwd: Path) -> None:
    forbidden = {
        process_cwd,
        Path(storage_dir.anchor).resolve(),
        Path.home().resolve(),
    }
    if storage_dir in forbidden:
        raise ValueError(
            f"Refusing to delete databases from unsafe storage_dir: {storage_dir}"
        )


__all__ = [
    "DatabaseDeleteResult",
    "batch_write_documents",
    "change_document",
    "delete_database",
    "rebuild_index",
    "search_documents",
    "search_documents_detailed",
    "submit_synonym_feedback",
]
