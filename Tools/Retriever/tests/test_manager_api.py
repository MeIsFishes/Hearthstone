from __future__ import annotations

import os
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from retriever.api import create_app
from retriever.config import AppConfig
from retriever.manager import IndexingError, RetrieverManager
from retriever.vmeta import read_vmeta


def _config(tmp_path: Path) -> AppConfig:
    return AppConfig.model_validate(
        {
            "index": {
                "directories": [],
                "storage_dir": str(tmp_path / "index"),
                "write_vmeta_next_to_source": True,
            },
            "logging": {"log_dir": str(tmp_path / "logs"), "level": "INFO"},
        }
    )


def test_manager_incremental_update_replaces_the_full_document_chunk(
    tmp_path: Path,
) -> None:
    docs = tmp_path / "docs"
    docs.mkdir()
    source = docs / "doc.md"
    source.write_text("abcdefghijold-body", encoding="utf-8")
    config = _config(tmp_path)
    config.chunk.max_chars = 10
    config.chunk.overlap_chars = 0
    config.chunk.preserve_english_words = False
    manager = RetrieverManager(config, process_cwd=tmp_path)
    try:
        first = manager.index_directory(docs)
        assert first.indexed_chunks == 1
        metadata = read_vmeta(source)
        assert metadata is not None
        assert metadata.split_algorithm_version == 3

        source.write_text("abcdefghijnew-body", encoding="utf-8")
        os.utime(source, None)
        second = manager.index_directory(docs, force=True)
        assert second.indexed_chunks == 1
        assert second.reused_chunks == 0

        results = manager.search("new-body", directory=docs, k=1)
        assert results[0].chunk_key == "docs/doc.md"
        assert results[0].body == "abcdefghijnew-body"
    finally:
        manager.close()


def test_failed_search_write_does_not_activate_new_revision(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch
) -> None:
    docs = tmp_path / "docs"
    docs.mkdir()
    source = docs / "doc.md"
    source.write_text("# A\nbody\n", encoding="utf-8")
    manager = RetrieverManager(_config(tmp_path), process_cwd=tmp_path)
    directory = manager.add_directory(docs)

    def fail(*_: object, **__: object) -> None:
        raise RuntimeError("injected write failure")

    monkeypatch.setattr(manager.search_engine, "index_chunks", fail)
    try:
        with pytest.raises(IndexingError):
            manager.index_file(source, directory)
        document = manager.catalog.document_status("docs/doc.md")
        assert document is not None
        assert document["active_revision"] is None
        assert manager.catalog.recent_tasks(1)[0]["status"] == "failed"
    finally:
        manager.close()


def test_bm25_segmenter_change_reindexes_unchanged_document(
    tmp_path: Path,
) -> None:
    docs = tmp_path / "docs"
    docs.mkdir()
    source = docs / "doc.md"
    source.write_text("# 配置\n角色技能配置说明\n", encoding="utf-8")
    first_config = _config(tmp_path)
    first_manager = RetrieverManager(first_config, process_cwd=tmp_path)
    try:
        first_manager.index_directory(docs)
        directory = first_manager.catalog.get_directory(docs)
        assert directory is not None
        first_refs = set(
            first_manager.catalog.active_directory_references(directory.directory_id)
        )
    finally:
        first_manager.close()

    second_config = _config(tmp_path)
    second_config.bm25.segmenter_version = 2
    second_manager = RetrieverManager(second_config, process_cwd=tmp_path)
    try:
        assert second_manager.needs_index(source)
        result = second_manager.index_directory(docs)
        assert result.indexed_chunks == 1
        directory = second_manager.catalog.get_directory(docs)
        assert directory is not None
        second_refs = set(
            second_manager.catalog.active_directory_references(directory.directory_id)
        )
        assert second_refs.isdisjoint(first_refs)
        assert second_manager.bm25.contains_refs(second_refs)
    finally:
        second_manager.close()


def test_rebuild_atomically_switches_generation_and_cleans_old_index(
    tmp_path: Path,
) -> None:
    docs = tmp_path / "docs"
    docs.mkdir()
    source = docs / "doc.md"
    removed = docs / "removed.md"
    source.write_text("# A\nold body\n", encoding="utf-8")
    removed.write_text("# Removed\nobsolete\n", encoding="utf-8")
    config = _config(tmp_path)
    manager = RetrieverManager(config, process_cwd=tmp_path)
    try:
        manager.index_directory(docs)
        directory = manager.catalog.get_directory(docs)
        assert directory is not None
        old_refs = set(
            manager.catalog.active_directory_references(directory.directory_id)
        )
        old_vmeta = read_vmeta(source)
        assert old_vmeta is not None

        source.write_text("# A\nnew body\n", encoding="utf-8")
        removed.unlink()
        result = manager.rebuild(docs)[0]

        assert result.indexed_files == 1
        assert result.deleted_files == 1
        active_directory = manager.catalog.get_directory(docs)
        assert active_directory is not None
        assert active_directory.active_generation == 1
        active_rows = manager.catalog.active_chunks([directory.directory_id])
        assert any(
            str(row["body"]).strip().endswith("new body") for row in active_rows
        )
        new_refs = set(
            manager.catalog.active_directory_references(directory.directory_id)
        )
        assert old_refs.isdisjoint(new_refs)
        assert manager.bm25.contains_refs(new_refs)
        assert not manager.bm25.contains_refs(old_refs)
        assert manager.search("new body", directory=docs, k=1)[
            0
        ].body.strip().endswith("new body")
        assert read_vmeta(source).index_revision > old_vmeta.index_revision
        assert read_vmeta(removed) is None
    finally:
        manager.close()

    reopened = RetrieverManager(config, process_cwd=tmp_path)
    try:
        directory = reopened.catalog.get_directory(docs)
        assert directory is not None
        assert directory.active_generation == 1
        assert any(
            str(row["body"]).strip().endswith("new body")
            for row in reopened.catalog.active_chunks([directory.directory_id])
        )
    finally:
        reopened.close()


def test_failed_rebuild_keeps_previous_generation_active(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch
) -> None:
    docs = tmp_path / "docs"
    docs.mkdir()
    source = docs / "doc.md"
    source.write_text("# A\nold body\n", encoding="utf-8")
    manager = RetrieverManager(_config(tmp_path), process_cwd=tmp_path)
    try:
        manager.index_directory(docs)
        directory = manager.catalog.get_directory(docs)
        assert directory is not None
        old_refs = set(
            manager.catalog.active_directory_references(directory.directory_id)
        )
        old_vmeta = read_vmeta(source)

        source.write_text("# A\nnew body\n", encoding="utf-8")

        def fail_validation(*_: object, **__: object) -> None:
            raise RuntimeError("injected generation validation failure")

        monkeypatch.setattr(manager.search_engine, "validate_refs", fail_validation)
        with pytest.raises(IndexingError):
            manager.rebuild(docs)

        active_directory = manager.catalog.get_directory(docs)
        assert active_directory is not None
        assert active_directory.active_generation == 0
        assert set(
            manager.catalog.active_directory_references(directory.directory_id)
        ) == old_refs
        assert any(
            str(row["body"]).strip().endswith("old body")
            for row in manager.catalog.active_chunks([directory.directory_id])
        )
        assert manager.search("old body", directory=docs, k=1)[
            0
        ].body.strip().endswith("old body")
        assert read_vmeta(source) == old_vmeta
        task = manager.catalog.recent_tasks(1)[0]
        assert task["status"] == "failed"
        assert task["stage"] == "generation-validate"
    finally:
        manager.close()


def test_http_api_build_search_status_and_clear(tmp_path: Path) -> None:
    docs = tmp_path / "docs"
    docs.mkdir()
    (docs / "doc.md").write_text(
        "# Configuration\nskill configuration guide\n", encoding="utf-8"
    )
    manager = RetrieverManager(_config(tmp_path), process_cwd=tmp_path)
    app = create_app(
        manager.config,
        process_cwd=tmp_path,
        manager=manager,
        enable_watcher=False,
    )
    try:
        with TestClient(app) as client:
            build = client.post(
                "/v1/index/build",
                json={"directory": str(docs), "force": False},
            )
            assert build.status_code == 200
            search = client.post(
                "/v1/search",
                json={"query": "skill configuration", "k": 1},
            )
            assert search.status_code == 200
            assert search.json()["results"][0]["title"] == "doc.md"
            assert search.json()["results"][0]["body"].lstrip().startswith(
                "# Configuration"
            )
            assert not (tmp_path / "index" / "search" / "vector").exists()
            status = client.get("/v1/status")
            assert status.json()["documents"] == 1
            clear = client.delete(
                "/v1/index", params={"directory": str(docs), "all": "false"}
            )
            assert clear.status_code == 200
            assert clear.json()["documents"] == 1
    finally:
        manager.close()
