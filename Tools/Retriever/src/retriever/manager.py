from __future__ import annotations

import logging
import os
import threading
import uuid
import hashlib
from dataclasses import dataclass
from functools import wraps
from pathlib import Path
from typing import Any, Callable, Sequence, TypeVar

from retriever.catalog import (
    Catalog,
    CatalogError,
    DirectoryRecord,
    PreparedGeneration,
    PreparedRevision,
)
from retriever.chunking import parse_markdown
from retriever.config import AppConfig
from retriever.logging_utils import configure_logging
from retriever.models import IndexedChunk, SearchChannel, SearchResult
from retriever.paths import (
    canonical_path,
    discover_files,
    matches_any_glob,
    relative_to_process,
)
from retriever.process_lock import ServiceLock
from retriever.search import BM25Search, KeywordSearch
from retriever.search.bm25 import tokenize_for_fts
from retriever.search.vector import (
    SentenceTransformerEmbedder,
    TermVectorIndex,
    TermVectorSearch,
)
from retriever.vmeta import VMeta, delete_vmeta, read_vmeta, write_vmeta_atomic


LOGGER = logging.getLogger(__name__)
_T = TypeVar("_T")


def synchronized(method: Callable[..., _T]) -> Callable[..., _T]:
    @wraps(method)
    def wrapper(self: "RetrieverManager", *args: Any, **kwargs: Any) -> _T:
        with self._operation_lock:
            return method(self, *args, **kwargs)

    return wrapper


class IndexingError(RuntimeError):
    pass


@dataclass(slots=True)
class IndexFileResult:
    source_path: str
    revision: int
    total_chunks: int
    indexed_chunks: int
    reused_chunks: int
    changed: bool

    def as_dict(self) -> dict[str, object]:
        return {
            "source_path": self.source_path,
            "revision": self.revision,
            "total_chunks": self.total_chunks,
            "indexed_chunks": self.indexed_chunks,
            "reused_chunks": self.reused_chunks,
            "changed": self.changed,
        }


@dataclass(slots=True)
class IndexDirectoryResult:
    directory: str
    indexed_files: int = 0
    skipped_files: int = 0
    deleted_files: int = 0
    failed_files: int = 0
    indexed_chunks: int = 0
    reused_chunks: int = 0

    def as_dict(self) -> dict[str, object]:
        return {
            "directory": self.directory,
            "indexed_files": self.indexed_files,
            "skipped_files": self.skipped_files,
            "deleted_files": self.deleted_files,
            "failed_files": self.failed_files,
            "indexed_chunks": self.indexed_chunks,
            "reused_chunks": self.reused_chunks,
        }


class RetrieverManager:
    def __init__(
        self,
        config: AppConfig,
        *,
        process_cwd: str | Path | None = None,
        term_vector_index: TermVectorIndex | None = None,
    ) -> None:
        self.config = config
        self.index_format_fingerprint = config.index_format_fingerprint()
        self._operation_lock = threading.RLock()
        self.process_cwd = canonical_path(process_cwd or Path.cwd())
        self.storage_dir = config.resolve_storage_dir(self.process_cwd)
        self.storage_dir.mkdir(parents=True, exist_ok=True)
        configure_logging(config, self.process_cwd)
        self._database_lock = ServiceLock(
            self.storage_dir / "runtime" / "database.lock"
        )
        self._closed = False
        self._database_lock.acquire()
        try:
            manager_dir = self.storage_dir / "manager"
            search_dir = self.storage_dir / "search"
            manager_dir.mkdir(parents=True, exist_ok=True)
            search_dir.mkdir(parents=True, exist_ok=True)
            self.catalog = Catalog(manager_dir / "catalog.db", self.process_cwd)
            self.bm25 = BM25Search(search_dir / "bm25.db", config.bm25.tokenizer)
            self.search_engine = KeywordSearch(self.bm25, config.search)
            self.term_vectors: TermVectorIndex | None = term_vector_index
            self.term_vector_error: str | None = None
            if self.term_vectors is None and config.embedding is not None:
                try:
                    embedder = SentenceTransformerEmbedder(
                        config.embedding.model,
                        config.embedding.device,
                    )
                    model_key = hashlib.sha256(
                        config.embedding.model.encode("utf-8")
                    ).hexdigest()[:16]
                    self.term_vectors = TermVectorSearch(
                        search_dir / "term-vectors" / model_key,
                        embedder,
                        batch_size=config.embedding.batch_size,
                    )
                except Exception as error:
                    self.term_vector_error = str(error)
                    LOGGER.warning(
                        "Term vectors are unavailable; BM25 remains active: %s",
                        error,
                    )
            for directory in config.index.directories:
                self.catalog.add_directory(self._resolve_input_path(directory))
        except Exception:
            self._database_lock.release()
            raise

    def close(self) -> None:
        if self._closed:
            return
        try:
            self.search_engine.close()
            self.catalog.close()
        finally:
            self._closed = True
            self._database_lock.release()

    def __enter__(self) -> "RetrieverManager":
        return self

    def __exit__(self, *_: object) -> None:
        self.close()

    def _resolve_input_path(self, path: str | Path) -> Path:
        candidate = Path(path).expanduser()
        if not candidate.is_absolute():
            candidate = self.process_cwd / candidate
        return candidate.resolve()

    @synchronized
    def add_directory(self, directory: str | Path) -> DirectoryRecord:
        return self.catalog.add_directory(self._resolve_input_path(directory))

    @synchronized
    def list_directories(self) -> list[DirectoryRecord]:
        return self.catalog.list_directories()

    @synchronized
    def needs_index(self, source_path: str | Path) -> bool:
        source = canonical_path(source_path)
        relative = relative_to_process(source, self.process_cwd)
        metadata = read_vmeta(source)
        document = self.catalog.document_status(relative)
        if metadata is None or document is None:
            return True
        if metadata.source_path != relative:
            return True
        if metadata.index_format_fingerprint != self.index_format_fingerprint:
            return True
        if (
            metadata.split_algorithm_version
            != self.config.chunk.split_algorithm_version
        ):
            return True
        if metadata.modified_at_ns != source.stat().st_mtime_ns:
            return True
        if document["active_revision"] is None or str(document["status"]) != "ready":
            return True
        return int(document["active_revision"]) != metadata.index_revision

    @synchronized
    def index_file(
        self,
        source_path: str | Path,
        directory: DirectoryRecord,
        *,
        refresh_vectors: bool = True,
    ) -> IndexFileResult:
        source = canonical_path(source_path)
        relative = relative_to_process(source, self.process_cwd)
        task_id = self.catalog.create_task("index-file", relative)
        stage = "read"
        prepared: PreparedRevision | None = None
        activated = False
        try:
            initial_stat = source.stat()
            with source.open("r", encoding="utf-8", newline="") as handle:
                markdown = handle.read()
            final_stat = source.stat()
            if (
                initial_stat.st_mtime_ns != final_stat.st_mtime_ns
                or initial_stat.st_size != final_stat.st_size
            ):
                raise IndexingError(f"File changed while being read: {source}")

            stage = "chunk"
            chunks = parse_markdown(
                markdown,
                relative_path=relative,
                document_name=source.name,
                config=self.config.chunk,
            )
            stage = "catalog-prepare"
            prepared = self.catalog.prepare_revision(
                directory_id=directory.directory_id,
                relative_path=relative,
                absolute_path=source,
                modified_at_ns=final_stat.st_mtime_ns,
                chunks=chunks,
                revision_salt=self.index_format_fingerprint,
            )
            if prepared.changed and prepared.chunks_to_index:
                stage = "search-index"
                self.search_engine.index_chunks(
                    directory.directory_id, prepared.chunks_to_index
                )
            stage = "catalog-activate"
            self.catalog.activate_revision(prepared)
            activated = True
            stage = "lexicon-refresh"
            self._refresh_directory_lexicon(
                directory.directory_id,
                refresh_vectors=refresh_vectors,
            )
            stage = "vmeta"
            if self.config.index.write_vmeta_next_to_source:
                write_vmeta_atomic(
                    source,
                    VMeta(
                        source_path=relative,
                        modified_at_ns=final_stat.st_mtime_ns,
                        index_revision=prepared.index_revision,
                        split_algorithm_version=(
                            self.config.chunk.split_algorithm_version
                        ),
                        index_format_fingerprint=self.index_format_fingerprint,
                    ),
                )
            self.catalog.finish_task(task_id)
            return IndexFileResult(
                source_path=relative,
                revision=prepared.index_revision,
                total_chunks=len(prepared.chunks),
                indexed_chunks=len(prepared.chunks_to_index),
                reused_chunks=prepared.unchanged_count,
                changed=prepared.changed,
            )
        except Exception as error:
            if prepared is not None and not activated:
                self.catalog.fail_revision(prepared, str(error))
            self.catalog.fail_task(task_id, stage, str(error))
            LOGGER.exception("Indexing failed at %s for %s", stage, source)
            raise IndexingError(f"Indexing failed at {stage} for {relative}: {error}") from error

    @synchronized
    def index_directory(
        self,
        directory: str | Path,
        *,
        force: bool = False,
        continue_on_error: bool = False,
    ) -> IndexDirectoryResult:
        root = self._resolve_input_path(directory)
        record = self.catalog.add_directory(root)
        result = IndexDirectoryResult(directory=str(root))
        discovered = discover_files(
            root,
            include_extensions=self.config.index.include_extensions,
            exclude_globs=self.config.index.exclude_globs,
        )
        discovered_relative = {
            relative_to_process(path, self.process_cwd): path for path in discovered
        }
        for relative, path in discovered_relative.items():
            if not force and not self.needs_index(path):
                result.skipped_files += 1
                continue
            try:
                file_result = self.index_file(
                    path,
                    record,
                    refresh_vectors=False,
                )
            except IndexingError:
                result.failed_files += 1
                if not continue_on_error:
                    raise
                continue
            result.indexed_files += 1
            result.indexed_chunks += file_result.indexed_chunks
            result.reused_chunks += file_result.reused_chunks

        known_documents = self.catalog.list_documents(record.directory_id)
        for document in known_documents:
            relative = str(document["relative_path"])
            if relative in discovered_relative:
                continue
            self._delete_document(
                relative,
                Path(str(document["absolute_path"])),
                refresh_lexicon=False,
            )
            result.deleted_files += 1
        self._refresh_directory_lexicon(record.directory_id)
        return result

    @synchronized
    def index_all(
        self, *, force: bool = False, continue_on_error: bool = False
    ) -> list[IndexDirectoryResult]:
        return [
            self.index_directory(
                record.root_path,
                force=force,
                continue_on_error=continue_on_error,
            )
            for record in self.catalog.list_directories(enabled_only=True)
        ]

    def _delete_document(
        self,
        relative_path: str,
        absolute_path: Path,
        *,
        refresh_lexicon: bool = True,
    ) -> None:
        document = self.catalog.document_status(relative_path)
        directory_id = int(document["directory_id"]) if document is not None else None
        references = self.catalog.document_references(relative_path)
        self.search_engine.delete_refs(references)
        self.catalog.delete_document(relative_path)
        if refresh_lexicon and directory_id is not None:
            self._refresh_directory_lexicon(directory_id)
        if self.config.index.write_vmeta_next_to_source:
            delete_vmeta(absolute_path)

    @synchronized
    def search(
        self,
        query: str | None = None,
        *,
        keywords: Sequence[str] | None = None,
        k: int | None = None,
        directory: str | Path | None = None,
    ) -> list[SearchResult]:
        execution = self.search_detailed(
            query,
            keywords=keywords,
            k=k,
            directory=directory,
        )
        return execution["results"]

    @synchronized
    def search_detailed(
        self,
        query: str | None = None,
        *,
        keywords: Sequence[str] | None = None,
        k: int | None = None,
        directory: str | Path | None = None,
    ) -> dict[str, Any]:
        directory_ids: list[int] | None = None
        if directory is not None:
            record = self.catalog.get_directory(self._resolve_input_path(directory))
            if record is None:
                raise CatalogError(f"Directory is not registered: {directory}")
            directory_ids = [record.directory_id]
        else:
            directory_ids = [
                record.directory_id for record in self.catalog.list_directories()
            ]
        normalized_keywords = _normalize_keywords(query, keywords)
        if not normalized_keywords:
            raise ValueError("Provide at least one non-empty keyword or query")
        active = self.catalog.active_chunk_map(directory_ids)
        search_id = str(uuid.uuid4())
        channels: list[SearchChannel] = []
        expansions: list[dict[str, Any]] = []
        keyword_statuses: list[dict[str, Any]] = []
        review_requests: list[dict[str, Any]] = []
        warnings: list[str] = []

        for keyword in normalized_keywords:
            channels.append(
                SearchChannel(
                    original_keyword=keyword,
                    term=keyword,
                    source="exact",
                    weight=self.config.search.exact_keyword_weight,
                    directory_ids=tuple(directory_ids),
                )
            )
            lexical_terms = list(dict.fromkeys(tokenize_for_fts(keyword)))
            known = self.bm25.known_terms_by_directory(
                lexical_terms,
                directory_ids,
            )
            known_directory_ids = [
                directory_id
                for directory_id in directory_ids
                if known.get(directory_id)
            ]
            oov_directory_ids = [
                directory_id
                for directory_id in directory_ids
                if not known.get(directory_id)
            ]

            accepted_directory_ids: set[int] = set()
            for accepted in self.bm25.accepted_expansions(keyword, directory_ids):
                accepted_directory_ids.add(int(accepted["directory_id"]))
                expansion = {
                    "original_keyword": keyword,
                    "term": accepted["term"],
                    "source": "accepted",
                    "weight": self.config.search.accepted_synonym_weight,
                    "similarity": None,
                    "directory_id": accepted["directory_id"],
                }
                expansions.append(expansion)
                channels.append(
                    SearchChannel(
                        original_keyword=keyword,
                        term=str(accepted["term"]),
                        source="accepted",
                        weight=self.config.search.accepted_synonym_weight,
                        directory_ids=(int(accepted["directory_id"]),),
                    )
                )

            unresolved_oov_directory_ids = [
                directory_id
                for directory_id in oov_directory_ids
                if directory_id not in accepted_directory_ids
            ]
            if unresolved_oov_directory_ids and self.term_vectors is None:
                warning = "term_vectors_unavailable"
                if warning not in warnings:
                    warnings.append(warning)
            for directory_id in oov_directory_ids:
                if self.term_vectors is None or directory_id in accepted_directory_ids:
                    continue
                try:
                    candidates = self.term_vectors.nearest(
                        keyword,
                        [directory_id],
                        limit=self.config.search.oov_candidate_limit,
                        similarity_threshold=(
                            self.config.search.oov_similarity_threshold
                        ),
                    )
                except Exception as error:
                    self.term_vector_error = str(error)
                    if "term_vector_query_failed" not in warnings:
                        warnings.append("term_vector_query_failed")
                    LOGGER.warning("OOV term vector query failed: %s", error)
                    continue
                for candidate in candidates:
                    candidate_term = str(candidate["term"])
                    if candidate_term == keyword:
                        continue
                    state = self.bm25.relation_state(
                        directory_id, keyword, candidate_term
                    )
                    if state is not None and state["status"] == "rejected":
                        continue
                    if state is not None and state["status"] == "accepted":
                        continue
                    similarity = float(candidate["similarity"])
                    self.bm25.add_candidate_pair(
                        directory_id,
                        keyword,
                        candidate_term,
                        similarity,
                        origin="query",
                    )
                    weight = (
                        self.config.search.provisional_synonym_weight * similarity
                    )
                    expansion = {
                        "original_keyword": keyword,
                        "term": candidate_term,
                        "source": "provisional",
                        "weight": weight,
                        "similarity": similarity,
                        "directory_id": directory_id,
                    }
                    expansions.append(expansion)
                    channels.append(
                        SearchChannel(
                            original_keyword=keyword,
                            term=candidate_term,
                            source="provisional",
                            weight=weight,
                            directory_ids=(directory_id,),
                            similarity=similarity,
                        )
                    )
            keyword_statuses.append(
                {
                    "keyword": keyword,
                    "status": "known" if not oov_directory_ids else (
                        "oov" if not known_directory_ids else "partial_oov"
                    ),
                    "lexical_terms": lexical_terms,
                    "known_directory_ids": known_directory_ids,
                    "oov_directory_ids": oov_directory_ids,
                }
            )

        results = self.search_engine.search_channels(
            channels,
            active,
            k=k,
        )
        result_chunk_keys = {result.chunk_key for result in results}
        result_references = [
            reference
            for reference in active
            if reference[0] in result_chunk_keys
        ]
        review_requests = self.bm25.review_candidates_for_results(
            search_id=search_id,
            references=result_references,
            limit=self.config.search.equivalence_review_limit_per_search,
            cooldown_hours=self.config.search.synonym_review_cooldown_hours,
        )
        return {
            "search_id": search_id,
            "query": query,
            "keywords": normalized_keywords,
            "keyword_statuses": keyword_statuses,
            "expansions": expansions,
            "review_requests": review_requests,
            "warnings": warnings,
            "results": results,
        }

    @synchronized
    def clear(self, directory: str | Path | None = None) -> dict[str, int]:
        records: Sequence[DirectoryRecord]
        if directory is None:
            records = self.catalog.list_directories()
        else:
            record = self.catalog.get_directory(self._resolve_input_path(directory))
            if record is None:
                return {"directories": 0, "documents": 0}
            records = [record]

        document_count = 0
        for record in records:
            documents = self.catalog.list_documents(record.directory_id)
            self.search_engine.clear([record.directory_id])
            if self.term_vectors is not None:
                self.term_vectors.clear([record.directory_id])
            source_paths = self.catalog.remove_directory(record.directory_id)
            document_count += len(documents)
            if self.config.index.write_vmeta_next_to_source:
                for source_path in source_paths:
                    delete_vmeta(source_path)
        return {"directories": len(records), "documents": document_count}

    @synchronized
    def rebuild(
        self, directory: str | Path | None = None
    ) -> list[IndexDirectoryResult]:
        targets = (
            [self._resolve_input_path(directory)]
            if directory is not None
            else [Path(item.root_path) for item in self.catalog.list_directories()]
        )
        return [self._rebuild_directory(target) for target in targets]

    def _rebuild_directory(self, directory: str | Path) -> IndexDirectoryResult:
        root = self._resolve_input_path(directory)
        record = self.catalog.add_directory(root)
        result = IndexDirectoryResult(directory=str(root))
        task_id = self.catalog.create_task("rebuild-directory", str(root))
        generation: PreparedGeneration | None = None
        activated = False
        stage = "generation-prepare"
        prepared_files: list[tuple[Path, PreparedRevision]] = []
        try:
            preexisting_references = set(
                self.catalog.directory_references(record.directory_id)
            )
            old_active_paths = {
                str(row["relative_path"])
                for row in self.catalog.list_documents(record.directory_id)
                if row["active_revision"] is not None
            }
            generation = self.catalog.begin_rebuild(record.directory_id)
            revision_salt = (
                f"{self.index_format_fingerprint}:directory:{record.directory_id}:"
                f"generation:{generation.generation}"
            )

            stage = "discover"
            discovered = discover_files(
                root,
                include_extensions=self.config.index.include_extensions,
                exclude_globs=self.config.index.exclude_globs,
            )
            discovered_relative = {
                relative_to_process(path, self.process_cwd) for path in discovered
            }
            result.deleted_files = len(old_active_paths - discovered_relative)

            for source in discovered:
                relative = relative_to_process(source, self.process_cwd)
                stage = f"read:{relative}"
                initial_stat = source.stat()
                with source.open("r", encoding="utf-8", newline="") as handle:
                    markdown = handle.read()
                final_stat = source.stat()
                if (
                    initial_stat.st_mtime_ns != final_stat.st_mtime_ns
                    or initial_stat.st_size != final_stat.st_size
                ):
                    raise IndexingError(f"File changed while being read: {source}")

                stage = f"chunk:{relative}"
                chunks = parse_markdown(
                    markdown,
                    relative_path=relative,
                    document_name=source.name,
                    config=self.config.chunk,
                )
                stage = f"catalog-stage:{relative}"
                prepared = self.catalog.prepare_revision(
                    directory_id=record.directory_id,
                    relative_path=relative,
                    absolute_path=source,
                    modified_at_ns=final_stat.st_mtime_ns,
                    chunks=chunks,
                    revision_salt=revision_salt,
                    staged=True,
                )
                self.catalog.stage_rebuild_document(generation, prepared)
                prepared_files.append((source, prepared))

                stage = f"search-stage:{relative}"
                self.search_engine.index_chunks(record.directory_id, prepared.chunks)
                result.indexed_files += 1
                result.indexed_chunks += len(prepared.chunks)

            stage = "generation-validate"
            new_references = self.catalog.generation_references(generation)
            expected_count = sum(
                len(prepared.chunks) for _, prepared in prepared_files
            )
            if len(new_references) != expected_count:
                raise IndexingError(
                    "Catalog generation is incomplete: "
                    f"expected {expected_count} chunks, found {len(new_references)}"
                )
            self.search_engine.validate_refs(new_references)

            stage = "generation-activate"
            deleted_paths = self.catalog.activate_rebuild(generation)
            activated = True

            stage = "lexicon-refresh"
            self._refresh_directory_lexicon(record.directory_id)

            stage = "vmeta"
            if self.config.index.write_vmeta_next_to_source:
                for source, prepared in prepared_files:
                    write_vmeta_atomic(
                        source,
                        VMeta(
                            source_path=prepared.relative_path,
                            modified_at_ns=prepared.modified_at_ns,
                            index_revision=prepared.index_revision,
                            split_algorithm_version=(
                                self.config.chunk.split_algorithm_version
                            ),
                            index_format_fingerprint=self.index_format_fingerprint,
                        ),
                    )
                for source_path in deleted_paths:
                    delete_vmeta(source_path)

            stage = "generation-cleanup"
            obsolete_references = list(
                preexisting_references - set(new_references)
            )
            self.search_engine.delete_refs(obsolete_references)
            self.catalog.prune_inactive_revisions(record.directory_id)
            self.catalog.finish_task(task_id)
            return result
        except Exception as error:
            if generation is not None and not activated:
                self.catalog.fail_rebuild(generation, str(error))
            self.catalog.fail_task(task_id, stage, str(error))
            LOGGER.exception("Rebuild failed at %s for %s", stage, root)
            raise IndexingError(
                f"Rebuild failed at {stage} for {root}: {error}"
            ) from error

    @synchronized
    def status(self) -> dict[str, object]:
        term_vector_status = self._term_vector_status()
        equivalence_stats = self.bm25.feedback_stats()
        return {
            "storage_dir": str(self.storage_dir),
            "process_cwd": str(self.process_cwd),
            "search_engine": "sqlite-fts5-bm25",
            "term_vectors": term_vector_status,
            "equivalence_table": equivalence_stats,
            "synonyms": equivalence_stats,
            "directories": [
                {
                    "directory_id": item.directory_id,
                    "root_path": item.root_path,
                    "enabled": item.enabled,
                    "active_generation": item.active_generation,
                }
                for item in self.catalog.list_directories()
            ],
            "documents": len(self.catalog.list_documents()),
            "chunks": len(self.catalog.active_chunks()),
            "recent_tasks": self.catalog.recent_tasks(),
        }

    @synchronized
    def handle_path_change(self, source_path: str | Path) -> IndexFileResult | None:
        source = canonical_path(source_path)
        owner: DirectoryRecord | None = None
        source_identity = os.path.normcase(str(source))
        for record in self.catalog.list_directories(enabled_only=True):
            root_identity = os.path.normcase(str(canonical_path(record.root_path)))
            try:
                if os.path.commonpath([source_identity, root_identity]) == root_identity:
                    owner = record
                    break
            except ValueError:
                continue
        if owner is None:
            return None

        relative_to_root = source.relative_to(canonical_path(owner.root_path)).as_posix()
        if (
            source.exists()
            and source.suffix.casefold()
            in {item.casefold() for item in self.config.index.include_extensions}
            and not matches_any_glob(relative_to_root, self.config.index.exclude_globs)
        ):
            return self.index_file(source, owner)

        relative = relative_to_process(source, self.process_cwd)
        document = self.catalog.document_status(relative)
        if document is not None:
            self._delete_document(relative, source)
        return None

    @synchronized
    def submit_synonym_feedback(
        self,
        *,
        search_id: str,
        directory_id: int,
        query_term: str,
        candidate_term: str,
        verdict: str,
    ) -> dict[str, Any]:
        if directory_id not in {
            record.directory_id for record in self.catalog.list_directories()
        }:
            raise ValueError(f"Unknown directory_id: {directory_id}")
        return self.bm25.submit_feedback(
            search_id=search_id,
            directory_id=directory_id,
            query_term=query_term.strip().casefold(),
            candidate_term=candidate_term.strip().casefold(),
            verdict=verdict,
            rejection_threshold=self.config.search.synonym_rejection_threshold,
        )

    @synchronized
    def list_synonyms(
        self,
        *,
        directory: str | Path | None = None,
        status: str | None = None,
    ) -> list[dict[str, Any]]:
        directory_ids: list[int] | None = None
        if directory is not None:
            record = self.catalog.get_directory(self._resolve_input_path(directory))
            if record is None:
                raise CatalogError(f"Directory is not registered: {directory}")
            directory_ids = [record.directory_id]
        return self.bm25.list_relations(directory_ids, status)

    def list_equivalence_terms(
        self,
        *,
        directory: str | Path | None = None,
        category: str | None = None,
    ) -> list[dict[str, Any]]:
        return self.list_synonyms(directory=directory, status=category)

    @synchronized
    def reset_synonym(
        self,
        *,
        directory_id: int,
        first_term: str,
        second_term: str,
    ) -> bool:
        return self.bm25.reset_relation(
            directory_id,
            first_term.strip().casefold(),
            second_term.strip().casefold(),
        )

    def _refresh_directory_lexicon(
        self,
        directory_id: int,
        *,
        refresh_vectors: bool = True,
    ) -> None:
        references = self.catalog.active_directory_references(directory_id)
        self.bm25.refresh_lexicon(directory_id, references)
        if self.term_vectors is None or not refresh_vectors:
            return
        try:
            terms = [
                str(row["term"])
                for row in self.bm25.lexicon_terms(
                    [directory_id], vector_eligible_only=True
                )
            ]
            self.term_vectors.replace_directory(directory_id, terms)
            pairs = self.term_vectors.candidate_pairs(
                directory_id,
                limit_per_term=(
                    self.config.search.equivalence_candidate_limit_per_term
                ),
                similarity_threshold=self.config.search.oov_similarity_threshold,
            )
            for pair in pairs:
                self.bm25.add_candidate_pair(
                    directory_id,
                    str(pair["term"]),
                    str(pair["candidate_term"]),
                    float(pair["similarity"]),
                    origin="index",
                )
            self.term_vector_error = None
        except Exception as error:
            self.term_vector_error = str(error)
            LOGGER.warning(
                "Term vector refresh failed; BM25 remains active: %s", error
            )

    def _term_vector_status(self) -> dict[str, object]:
        if self.term_vectors is None:
            return {
                "configured": self.config.embedding is not None,
                "enabled": False,
                "count": 0,
                "embedding_fingerprint": None,
                "last_error": self.term_vector_error,
            }
        try:
            count = self.term_vectors.count()
        except Exception as error:
            self.term_vector_error = str(error)
            count = 0
        return {
            "configured": True,
            "enabled": True,
            "count": count,
            "embedding_fingerprint": self.term_vectors.embedding_fingerprint,
            "last_error": self.term_vector_error,
        }


def _normalize_keywords(
    query: str | None,
    keywords: Sequence[str] | None,
) -> list[str]:
    source = list(keywords or [])
    if not source and query is not None:
        source = [query]
    return list(
        dict.fromkeys(value.strip().casefold() for value in source if value.strip())
    )
