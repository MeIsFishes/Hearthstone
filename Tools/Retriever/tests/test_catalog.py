from __future__ import annotations

import sqlite3
from pathlib import Path

import pytest

from retriever.catalog import Catalog, CatalogError
from retriever.chunking import parse_markdown
from retriever.config import ChunkConfig


def test_catalog_replaces_the_single_document_chunk_across_snapshots(
    tmp_path: Path,
) -> None:
    root = tmp_path / "docs"
    root.mkdir()
    database = tmp_path / "storage" / "catalog.db"
    source = root / "doc.md"
    source.write_text("abcdefghijold-body", encoding="utf-8")
    chunk_config = ChunkConfig(
        max_chars=10,
        overlap_chars=0,
        preserve_english_words=False,
    )

    with Catalog(database, tmp_path) as catalog:
        directory = catalog.add_directory(root)
        first_chunks = parse_markdown(
            source.read_text(encoding="utf-8"),
            relative_path="docs/doc.md",
            config=chunk_config,
        )
        first = catalog.prepare_revision(
            directory_id=directory.directory_id,
            relative_path="docs/doc.md",
            absolute_path=source,
            modified_at_ns=1,
            chunks=first_chunks,
        )
        catalog.activate_revision(first)

        second_chunks = parse_markdown(
            "abcdefghijnew-body",
            relative_path="docs/doc.md",
            config=chunk_config,
        )
        second = catalog.prepare_revision(
            directory_id=directory.directory_id,
            relative_path="docs/doc.md",
            absolute_path=source,
            modified_at_ns=2,
            chunks=second_chunks,
        )

        assert second.changed
        assert second.unchanged_count == 0
        assert len(second.chunks_to_index) == 1
        catalog.activate_revision(second)
        active = catalog.active_chunks()
        assert len(active) == 1
        assert str(active[0]["body"]) == "abcdefghijnew-body"


def test_catalog_rejects_changed_process_working_directory(tmp_path: Path) -> None:
    database = tmp_path / "catalog.db"
    with Catalog(database, tmp_path):
        pass
    different = tmp_path / "different"
    different.mkdir()

    with pytest.raises(CatalogError):
        Catalog(database, different)


def test_catalog_migrates_schema_v1_directory_generation(tmp_path: Path) -> None:
    database = tmp_path / "catalog.db"
    connection = sqlite3.connect(database)
    connection.executescript(
        """
        CREATE TABLE catalog_metadata (
            key TEXT PRIMARY KEY,
            value TEXT NOT NULL
        );
        CREATE TABLE directories (
            directory_id INTEGER PRIMARY KEY AUTOINCREMENT,
            root_path TEXT NOT NULL UNIQUE,
            enabled INTEGER NOT NULL DEFAULT 1,
            created_at TEXT NOT NULL
        );
        """
    )
    connection.executemany(
        "INSERT INTO catalog_metadata(key, value) VALUES(?, ?)",
        [("index_base_path", str(tmp_path).casefold()), ("schema_version", "1")],
    )
    connection.commit()
    connection.close()

    with Catalog(database, tmp_path) as catalog:
        columns = {
            str(row["name"])
            for row in catalog.connection.execute("PRAGMA table_info(directories)")
        }
        version = catalog.connection.execute(
            "SELECT value FROM catalog_metadata WHERE key = 'schema_version'"
        ).fetchone()[0]
        assert "active_generation" in columns
        assert version == "2"
