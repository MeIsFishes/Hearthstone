from __future__ import annotations

from pathlib import Path

import pytest

from retriever.config import AppConfig
from retriever.flows import (
    batch_write_documents,
    change_document,
    delete_database,
    rebuild_index,
    search_documents,
)
from retriever.manager import RetrieverManager


def _config(tmp_path: Path) -> AppConfig:
    return AppConfig.model_validate(
        {
            "index": {
                "directories": [],
                "storage_dir": str(tmp_path / "index"),
                "write_vmeta_next_to_source": True,
            },
            "logging": {"log_dir": str(tmp_path / "index" / "logs")},
        }
    )


def test_online_flow_functions_are_independently_callable(tmp_path: Path) -> None:
    docs = tmp_path / "docs"
    docs.mkdir()
    source = docs / "doc.md"
    source.write_text("# A\nold body\n", encoding="utf-8")
    manager = RetrieverManager(_config(tmp_path), process_cwd=tmp_path)
    try:
        batch = batch_write_documents(manager, docs)
        assert batch[0].indexed_files == 1
        assert search_documents(manager, "old body", directory=docs, k=1)[
            0
        ].body.strip().endswith("old body")

        source.write_text("# A\nchanged body\n", encoding="utf-8")
        changed = change_document(manager, source)
        assert changed is not None
        assert changed.changed
        assert search_documents(manager, "changed body", directory=docs, k=1)[
            0
        ].body.strip().endswith("changed body")

        rebuilt = rebuild_index(manager, docs)
        assert rebuilt[0].indexed_files == 1
        directory = manager.catalog.get_directory(docs)
        assert directory is not None
        assert directory.active_generation == 1

    finally:
        manager.close()


def test_delete_database_physically_removes_persistent_stores(tmp_path: Path) -> None:
    docs = tmp_path / "docs"
    docs.mkdir()
    (docs / "doc.md").write_text("# A\nbody\n", encoding="utf-8")
    config = _config(tmp_path)
    manager = RetrieverManager(config, process_cwd=tmp_path)
    manager.index_directory(docs)
    manager.close()

    storage_dir = config.resolve_storage_dir(tmp_path)
    manager_dir = storage_dir / "manager"
    search_dir = storage_dir / "search"
    assert manager_dir.is_dir()
    assert search_dir.is_dir()

    result = delete_database(config, process_cwd=tmp_path)

    assert set(result.deleted_paths) == {str(manager_dir), str(search_dir)}
    assert not manager_dir.exists()
    assert not search_dir.exists()
    assert (storage_dir / "logs").is_dir()
    assert (docs / "doc.md").is_file()
    assert (docs / "doc.md.vmeta").is_file()

    reopened = RetrieverManager(config, process_cwd=tmp_path)
    try:
        assert reopened.catalog.list_documents() == []
    finally:
        reopened.close()


def test_delete_database_rejects_active_manager(tmp_path: Path) -> None:
    config = _config(tmp_path)
    manager = RetrieverManager(config, process_cwd=tmp_path)
    try:
        with pytest.raises(RuntimeError, match="Another Retriever service"):
            delete_database(config, process_cwd=tmp_path)
        assert (config.resolve_storage_dir(tmp_path) / "manager" / "catalog.db").is_file()
    finally:
        manager.close()


def test_delete_database_rejects_unsafe_storage_root(tmp_path: Path) -> None:
    config = AppConfig.model_validate(
        {
            "index": {"storage_dir": str(tmp_path)},
            "logging": {"log_dir": str(tmp_path / "logs")},
        }
    )
    with pytest.raises(ValueError, match="unsafe storage_dir"):
        delete_database(config, process_cwd=tmp_path)
