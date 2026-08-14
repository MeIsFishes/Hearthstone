from __future__ import annotations

import hashlib
import json
import sqlite3
from contextlib import contextmanager
from dataclasses import dataclass
from datetime import UTC, datetime
from pathlib import Path
from typing import Iterator, Sequence

from retriever.models import Chunk, IndexedChunk
from retriever.paths import canonical_path, path_identity, roots_overlap


SCHEMA_VERSION = 2


@dataclass(frozen=True, slots=True)
class DirectoryRecord:
    directory_id: int
    root_path: str
    enabled: bool
    active_generation: int


@dataclass(frozen=True, slots=True)
class PreparedGeneration:
    generation_id: int
    directory_id: int
    generation: int


@dataclass(frozen=True, slots=True)
class PreparedRevision:
    document_id: int
    directory_id: int
    relative_path: str
    index_revision: int
    modified_at_ns: int
    chunks: tuple[IndexedChunk, ...]
    chunks_to_index: tuple[IndexedChunk, ...]
    unchanged_count: int
    changed: bool


class CatalogError(RuntimeError):
    pass


class Catalog:
    def __init__(self, database_path: str | Path, process_cwd: str | Path) -> None:
        self.database_path = Path(database_path)
        self.database_path.parent.mkdir(parents=True, exist_ok=True)
        self.process_cwd = canonical_path(process_cwd)
        self.connection = sqlite3.connect(self.database_path, check_same_thread=False)
        self.connection.row_factory = sqlite3.Row
        self.connection.execute("PRAGMA foreign_keys = ON")
        self.connection.execute("PRAGMA journal_mode = WAL")
        self.connection.execute("PRAGMA synchronous = FULL")
        self._create_schema()
        self._migrate_schema()
        self._validate_metadata()

    def close(self) -> None:
        self.connection.close()

    def __enter__(self) -> "Catalog":
        return self

    def __exit__(self, *_: object) -> None:
        self.close()

    @contextmanager
    def transaction(self) -> Iterator[sqlite3.Connection]:
        try:
            self.connection.execute("BEGIN IMMEDIATE")
            yield self.connection
            self.connection.commit()
        except Exception:
            self.connection.rollback()
            raise

    def _create_schema(self) -> None:
        self.connection.executescript(
            """
            CREATE TABLE IF NOT EXISTS catalog_metadata (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS directories (
                directory_id INTEGER PRIMARY KEY AUTOINCREMENT,
                root_path TEXT NOT NULL UNIQUE,
                enabled INTEGER NOT NULL DEFAULT 1,
                active_generation INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS documents (
                document_id INTEGER PRIMARY KEY AUTOINCREMENT,
                directory_id INTEGER NOT NULL REFERENCES directories(directory_id),
                relative_path TEXT NOT NULL UNIQUE,
                absolute_path TEXT NOT NULL UNIQUE,
                modified_at_ns INTEGER NOT NULL DEFAULT 0,
                active_revision INTEGER,
                status TEXT NOT NULL DEFAULT 'new'
            );

            CREATE TABLE IF NOT EXISTS document_revisions (
                document_id INTEGER NOT NULL REFERENCES documents(document_id),
                index_revision INTEGER NOT NULL,
                status TEXT NOT NULL,
                created_at TEXT NOT NULL,
                error TEXT,
                PRIMARY KEY (document_id, index_revision)
            );

            CREATE TABLE IF NOT EXISTS chunks (
                document_id INTEGER NOT NULL REFERENCES documents(document_id),
                chunk_key TEXT NOT NULL,
                content_revision TEXT NOT NULL,
                title TEXT NOT NULL,
                body TEXT NOT NULL,
                heading_path TEXT NOT NULL,
                split_number INTEGER,
                PRIMARY KEY (chunk_key, content_revision)
            );

            CREATE TABLE IF NOT EXISTS document_revision_chunks (
                document_id INTEGER NOT NULL,
                index_revision INTEGER NOT NULL,
                chunk_key TEXT NOT NULL,
                content_revision TEXT NOT NULL,
                ordinal INTEGER NOT NULL,
                PRIMARY KEY (document_id, index_revision, chunk_key),
                FOREIGN KEY (document_id, index_revision)
                    REFERENCES document_revisions(document_id, index_revision)
            );

            CREATE TABLE IF NOT EXISTS index_tasks (
                task_id INTEGER PRIMARY KEY AUTOINCREMENT,
                task_type TEXT NOT NULL,
                target TEXT NOT NULL,
                status TEXT NOT NULL,
                stage TEXT,
                error TEXT,
                created_at TEXT NOT NULL,
                finished_at TEXT
            );

            CREATE TABLE IF NOT EXISTS rebuild_generations (
                generation_id INTEGER PRIMARY KEY AUTOINCREMENT,
                directory_id INTEGER NOT NULL
                    REFERENCES directories(directory_id) ON DELETE CASCADE,
                generation INTEGER NOT NULL,
                status TEXT NOT NULL,
                created_at TEXT NOT NULL,
                activated_at TEXT,
                error TEXT,
                UNIQUE(directory_id, generation)
            );

            CREATE TABLE IF NOT EXISTS rebuild_generation_documents (
                generation_id INTEGER NOT NULL
                    REFERENCES rebuild_generations(generation_id) ON DELETE CASCADE,
                document_id INTEGER NOT NULL,
                index_revision INTEGER NOT NULL,
                modified_at_ns INTEGER NOT NULL,
                PRIMARY KEY (generation_id, document_id),
                FOREIGN KEY (document_id, index_revision)
                    REFERENCES document_revisions(document_id, index_revision)
                    ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_documents_directory
                ON documents(directory_id);
            CREATE INDEX IF NOT EXISTS idx_revision_chunks_content
                ON document_revision_chunks(chunk_key, content_revision);
            """
        )
        self.connection.commit()

    def _migrate_schema(self) -> None:
        columns = {
            str(row["name"])
            for row in self.connection.execute("PRAGMA table_info(directories)")
        }
        if "active_generation" not in columns:
            with self.transaction():
                self.connection.execute(
                    "ALTER TABLE directories "
                    "ADD COLUMN active_generation INTEGER NOT NULL DEFAULT 0"
                )

        row = self.connection.execute(
            "SELECT value FROM catalog_metadata WHERE key = 'schema_version'"
        ).fetchone()
        if row is not None and int(row["value"]) == 1:
            with self.transaction():
                self.connection.execute(
                    "UPDATE catalog_metadata SET value = ? WHERE key = 'schema_version'",
                    (str(SCHEMA_VERSION),),
                )

    def _validate_metadata(self) -> None:
        expected_base = path_identity(self.process_cwd)
        row = self.connection.execute(
            "SELECT value FROM catalog_metadata WHERE key = 'index_base_path'"
        ).fetchone()
        if row is None:
            with self.transaction():
                self.connection.executemany(
                    "INSERT INTO catalog_metadata(key, value) VALUES(?, ?)",
                    [
                        ("index_base_path", expected_base),
                        ("schema_version", str(SCHEMA_VERSION)),
                    ],
                )
            return
        if row["value"] != expected_base:
            raise CatalogError(
                "Process working directory does not match catalog index_base_path: "
                f"{self.process_cwd} != {row['value']}"
            )
        schema_row = self.connection.execute(
            "SELECT value FROM catalog_metadata WHERE key = 'schema_version'"
        ).fetchone()
        if schema_row is None or int(schema_row["value"]) != SCHEMA_VERSION:
            raise CatalogError("Unsupported catalog schema version")

    def add_directory(self, root_path: str | Path) -> DirectoryRecord:
        canonical = canonical_path(root_path)
        if not canonical.is_dir():
            raise CatalogError(f"Directory does not exist: {canonical}")
        existing = self.list_directories()
        for item in existing:
            if roots_overlap(item.root_path, canonical):
                if path_identity(item.root_path) == path_identity(canonical):
                    return item
                raise CatalogError(
                    f"Nested index roots are not allowed: {item.root_path} and {canonical}"
                )
        with self.transaction():
            cursor = self.connection.execute(
                """
                INSERT INTO directories(root_path, enabled, created_at)
                VALUES(?, 1, ?)
                """,
                (str(canonical), _utc_now()),
            )
        return DirectoryRecord(int(cursor.lastrowid), str(canonical), True, 0)

    def list_directories(self, enabled_only: bool = False) -> list[DirectoryRecord]:
        sql = (
            "SELECT directory_id, root_path, enabled, active_generation "
            "FROM directories"
        )
        if enabled_only:
            sql += " WHERE enabled = 1"
        sql += " ORDER BY root_path"
        return [
            DirectoryRecord(
                directory_id=int(row["directory_id"]),
                root_path=str(row["root_path"]),
                enabled=bool(row["enabled"]),
                active_generation=int(row["active_generation"]),
            )
            for row in self.connection.execute(sql)
        ]

    def get_directory(self, root_path: str | Path) -> DirectoryRecord | None:
        identity = path_identity(root_path)
        for item in self.list_directories():
            if path_identity(item.root_path) == identity:
                return item
        return None

    def list_documents(self, directory_id: int | None = None) -> list[sqlite3.Row]:
        if directory_id is None:
            return self.connection.execute(
                "SELECT * FROM documents ORDER BY relative_path"
            ).fetchall()
        return self.connection.execute(
            """
            SELECT * FROM documents
            WHERE directory_id = ?
            ORDER BY relative_path
            """,
            (directory_id,),
        ).fetchall()

    def directory_references(self, directory_id: int) -> list[tuple[str, str]]:
        return [
            (str(row["chunk_key"]), str(row["content_revision"]))
            for row in self.connection.execute(
                """
                SELECT DISTINCT drc.chunk_key, drc.content_revision
                FROM document_revision_chunks drc
                JOIN documents d ON d.document_id = drc.document_id
                WHERE d.directory_id = ?
                """,
                (directory_id,),
            )
        ]

    def remove_directory(self, directory_id: int) -> list[str]:
        paths = [
            str(row["absolute_path"])
            for row in self.connection.execute(
                "SELECT absolute_path FROM documents WHERE directory_id = ?",
                (directory_id,),
            )
        ]
        with self.transaction():
            document_rows = self.connection.execute(
                "SELECT document_id FROM documents WHERE directory_id = ?",
                (directory_id,),
            ).fetchall()
            for row in document_rows:
                self._delete_document_rows(int(row["document_id"]))
            self.connection.execute(
                "DELETE FROM directories WHERE directory_id = ?", (directory_id,)
            )
        return paths

    def _delete_document_rows(self, document_id: int) -> None:
        self.connection.execute(
            "DELETE FROM document_revision_chunks WHERE document_id = ?", (document_id,)
        )
        self.connection.execute(
            "DELETE FROM document_revisions WHERE document_id = ?", (document_id,)
        )
        self.connection.execute("DELETE FROM chunks WHERE document_id = ?", (document_id,))
        self.connection.execute(
            "DELETE FROM documents WHERE document_id = ?", (document_id,)
        )

    def create_task(self, task_type: str, target: str) -> int:
        with self.transaction():
            cursor = self.connection.execute(
                """
                INSERT INTO index_tasks(task_type, target, status, created_at)
                VALUES(?, ?, 'running', ?)
                """,
                (task_type, target, _utc_now()),
            )
        return int(cursor.lastrowid)

    def finish_task(self, task_id: int) -> None:
        with self.transaction():
            self.connection.execute(
                """
                UPDATE index_tasks
                SET status = 'completed', finished_at = ?
                WHERE task_id = ?
                """,
                (_utc_now(), task_id),
            )

    def fail_task(self, task_id: int, stage: str, error: str) -> None:
        with self.transaction():
            self.connection.execute(
                """
                UPDATE index_tasks
                SET status = 'failed', stage = ?, error = ?, finished_at = ?
                WHERE task_id = ?
                """,
                (stage, error, _utc_now(), task_id),
            )

    def begin_rebuild(self, directory_id: int) -> PreparedGeneration:
        with self.transaction():
            directory = self.connection.execute(
                "SELECT active_generation FROM directories WHERE directory_id = ?",
                (directory_id,),
            ).fetchone()
            if directory is None:
                raise CatalogError(f"Unknown directory id: {directory_id}")
            generation = int(
                self.connection.execute(
                    """
                    SELECT MAX(value) FROM (
                        SELECT active_generation AS value
                        FROM directories WHERE directory_id = ?
                        UNION ALL
                        SELECT generation AS value
                        FROM rebuild_generations WHERE directory_id = ?
                    )
                    """,
                    (directory_id, directory_id),
                ).fetchone()[0]
                or 0
            ) + 1
            cursor = self.connection.execute(
                """
                INSERT INTO rebuild_generations(
                    directory_id, generation, status, created_at
                ) VALUES(?, ?, 'preparing', ?)
                """,
                (directory_id, generation, _utc_now()),
            )
        return PreparedGeneration(
            generation_id=int(cursor.lastrowid),
            directory_id=directory_id,
            generation=generation,
        )

    def stage_rebuild_document(
        self, generation: PreparedGeneration, prepared: PreparedRevision
    ) -> None:
        if prepared.directory_id != generation.directory_id:
            raise CatalogError("Prepared document belongs to another directory")
        with self.transaction():
            row = self.connection.execute(
                "SELECT status FROM rebuild_generations WHERE generation_id = ?",
                (generation.generation_id,),
            ).fetchone()
            if row is None or str(row["status"]) != "preparing":
                raise CatalogError("Rebuild generation is not preparing")
            self.connection.execute(
                """
                INSERT INTO rebuild_generation_documents(
                    generation_id, document_id, index_revision, modified_at_ns
                ) VALUES(?, ?, ?, ?)
                """,
                (
                    generation.generation_id,
                    prepared.document_id,
                    prepared.index_revision,
                    prepared.modified_at_ns,
                ),
            )

    def generation_references(
        self, generation: PreparedGeneration
    ) -> list[tuple[str, str]]:
        return [
            (str(row["chunk_key"]), str(row["content_revision"]))
            for row in self.connection.execute(
                """
                SELECT drc.chunk_key, drc.content_revision
                FROM rebuild_generation_documents rgd
                JOIN document_revision_chunks drc
                  ON drc.document_id = rgd.document_id
                 AND drc.index_revision = rgd.index_revision
                WHERE rgd.generation_id = ?
                ORDER BY rgd.document_id, drc.ordinal
                """,
                (generation.generation_id,),
            )
        ]

    def activate_rebuild(self, generation: PreparedGeneration) -> list[str]:
        deleted_paths: list[str] = []
        with self.transaction():
            row = self.connection.execute(
                """
                SELECT directory_id, status FROM rebuild_generations
                WHERE generation_id = ? AND generation = ?
                """,
                (generation.generation_id, generation.generation),
            ).fetchone()
            if (
                row is None
                or int(row["directory_id"]) != generation.directory_id
                or str(row["status"]) != "preparing"
            ):
                raise CatalogError("Rebuild generation is not ready to activate")

            staged_rows = self.connection.execute(
                """
                SELECT document_id, index_revision, modified_at_ns
                FROM rebuild_generation_documents
                WHERE generation_id = ?
                """,
                (generation.generation_id,),
            ).fetchall()
            staged_ids = {int(item["document_id"]) for item in staged_rows}
            existing_rows = self.connection.execute(
                """
                SELECT document_id, absolute_path FROM documents
                WHERE directory_id = ?
                """,
                (generation.directory_id,),
            ).fetchall()

            for item in staged_rows:
                document_id = int(item["document_id"])
                index_revision = int(item["index_revision"])
                self.connection.execute(
                    """
                    UPDATE document_revisions SET status = 'ready', error = NULL
                    WHERE document_id = ? AND index_revision = ?
                    """,
                    (document_id, index_revision),
                )
                self.connection.execute(
                    """
                    UPDATE documents
                    SET active_revision = ?, modified_at_ns = ?, status = 'ready'
                    WHERE document_id = ?
                    """,
                    (index_revision, int(item["modified_at_ns"]), document_id),
                )

            for item in existing_rows:
                document_id = int(item["document_id"])
                if document_id in staged_ids:
                    continue
                deleted_paths.append(str(item["absolute_path"]))
                self._delete_document_rows(document_id)

            self.connection.execute(
                """
                UPDATE rebuild_generations SET status = 'superseded'
                WHERE directory_id = ? AND status = 'active'
                """,
                (generation.directory_id,),
            )
            self.connection.execute(
                """
                UPDATE rebuild_generations
                SET status = 'active', activated_at = ?, error = NULL
                WHERE generation_id = ?
                """,
                (_utc_now(), generation.generation_id),
            )
            self.connection.execute(
                """
                UPDATE directories SET active_generation = ?
                WHERE directory_id = ?
                """,
                (generation.generation, generation.directory_id),
            )
        return deleted_paths

    def fail_rebuild(self, generation: PreparedGeneration, error: str) -> None:
        with self.transaction():
            rows = self.connection.execute(
                """
                SELECT document_id, index_revision
                FROM rebuild_generation_documents WHERE generation_id = ?
                """,
                (generation.generation_id,),
            ).fetchall()
            for row in rows:
                document_id = int(row["document_id"])
                index_revision = int(row["index_revision"])
                self.connection.execute(
                    """
                    UPDATE document_revisions SET status = 'failed', error = ?
                    WHERE document_id = ? AND index_revision = ?
                      AND status = 'preparing'
                    """,
                    (error, document_id, index_revision),
                )
                self.connection.execute(
                    """
                    UPDATE documents
                    SET status = CASE
                        WHEN active_revision IS NULL THEN 'failed' ELSE 'ready' END
                    WHERE document_id = ?
                    """,
                    (document_id,),
                )
            self.connection.execute(
                """
                UPDATE rebuild_generations SET status = 'failed', error = ?
                WHERE generation_id = ? AND status = 'preparing'
                """,
                (error, generation.generation_id),
            )

    def active_directory_references(
        self, directory_id: int
    ) -> list[tuple[str, str]]:
        return [
            (str(row["chunk_key"]), str(row["content_revision"]))
            for row in self.active_chunks([directory_id])
        ]

    def prune_inactive_revisions(self, directory_id: int) -> None:
        with self.transaction():
            document_ids = [
                int(row["document_id"])
                for row in self.connection.execute(
                    "SELECT document_id FROM documents WHERE directory_id = ?",
                    (directory_id,),
                )
            ]
            for document_id in document_ids:
                active_row = self.connection.execute(
                    "SELECT active_revision FROM documents WHERE document_id = ?",
                    (document_id,),
                ).fetchone()
                active_revision = int(active_row["active_revision"])
                self.connection.execute(
                    """
                    DELETE FROM document_revision_chunks
                    WHERE document_id = ? AND index_revision != ?
                    """,
                    (document_id, active_revision),
                )
                self.connection.execute(
                    """
                    DELETE FROM document_revisions
                    WHERE document_id = ? AND index_revision != ?
                    """,
                    (document_id, active_revision),
                )
                self.connection.execute(
                    """
                    DELETE FROM chunks
                    WHERE document_id = ?
                      AND NOT EXISTS (
                          SELECT 1 FROM document_revision_chunks drc
                          WHERE drc.document_id = chunks.document_id
                            AND drc.chunk_key = chunks.chunk_key
                            AND drc.content_revision = chunks.content_revision
                      )
                    """,
                    (document_id,),
                )

    def prepare_revision(
        self,
        *,
        directory_id: int,
        relative_path: str,
        absolute_path: str | Path,
        modified_at_ns: int,
        chunks: Sequence[Chunk],
        revision_salt: str | None = None,
        staged: bool = False,
    ) -> PreparedRevision:
        absolute = str(canonical_path(absolute_path))
        with self.transaction():
            document_row = self.connection.execute(
                "SELECT * FROM documents WHERE relative_path = ?", (relative_path,)
            ).fetchone()
            if document_row is None:
                cursor = self.connection.execute(
                    """
                    INSERT INTO documents(
                        directory_id, relative_path, absolute_path, modified_at_ns, status
                    ) VALUES(?, ?, ?, 0, 'new')
                    """,
                    (directory_id, relative_path, absolute),
                )
                document_id = int(cursor.lastrowid)
                active_revision = None
            else:
                document_id = int(document_row["document_id"])
                active_revision = document_row["active_revision"]
                if int(document_row["directory_id"]) != directory_id:
                    raise CatalogError(
                        f"Document already belongs to another directory: {relative_path}"
                    )

            old_chunks = {
                str(row["chunk_key"]): row
                for row in self._revision_rows(document_id, active_revision)
            }
            next_revision = int(
                self.connection.execute(
                    """
                    SELECT COALESCE(MAX(index_revision), 0) + 1
                    FROM document_revisions WHERE document_id = ?
                    """,
                    (document_id,),
                ).fetchone()[0]
            )

            indexed_chunks: list[IndexedChunk] = []
            chunks_to_index: list[IndexedChunk] = []
            unchanged_count = 0
            for chunk in chunks:
                old = old_chunks.get(chunk.chunk_key)
                content_revision = _content_revision(chunk, revision_salt)
                if (
                    old is not None
                    and str(old["title"]) == chunk.title
                    and str(old["body"]) == chunk.body
                    and str(old["content_revision"]) == content_revision
                ):
                    content_revision = str(old["content_revision"])
                    unchanged_count += 1
                else:
                    chunks_to_index.append(
                        IndexedChunk(
                            chunk_key=chunk.chunk_key,
                            content_revision=content_revision,
                            title=chunk.title,
                            body=chunk.body,
                            ordinal=chunk.ordinal,
                        )
                    )
                indexed = IndexedChunk(
                    chunk_key=chunk.chunk_key,
                    content_revision=content_revision,
                    title=chunk.title,
                    body=chunk.body,
                    ordinal=chunk.ordinal,
                )
                indexed_chunks.append(indexed)
                self.connection.execute(
                    """
                    INSERT OR IGNORE INTO chunks(
                        document_id, chunk_key, content_revision, title, body,
                        heading_path, split_number
                    ) VALUES(?, ?, ?, ?, ?, ?, ?)
                    """,
                    (
                        document_id,
                        chunk.chunk_key,
                        content_revision,
                        chunk.title,
                        chunk.body,
                        json.dumps(chunk.heading_path, ensure_ascii=False),
                        chunk.split_number,
                    ),
                )

            old_signature = [
                (str(row["chunk_key"]), str(row["content_revision"]), int(row["ordinal"]))
                for row in self._revision_rows(document_id, active_revision)
            ]
            new_signature = [
                (chunk.chunk_key, chunk.content_revision, chunk.ordinal)
                for chunk in indexed_chunks
            ]
            changed = old_signature != new_signature
            if not changed:
                if not staged:
                    self.connection.execute(
                        """
                        UPDATE documents
                        SET modified_at_ns = ?, status = 'ready'
                        WHERE document_id = ?
                        """,
                        (modified_at_ns, document_id),
                    )
                return PreparedRevision(
                    document_id=document_id,
                    directory_id=directory_id,
                    relative_path=relative_path,
                    index_revision=int(active_revision),
                    modified_at_ns=modified_at_ns,
                    chunks=tuple(indexed_chunks),
                    chunks_to_index=(),
                    unchanged_count=unchanged_count,
                    changed=False,
                )

            self.connection.execute(
                """
                INSERT INTO document_revisions(
                    document_id, index_revision, status, created_at
                ) VALUES(?, ?, 'preparing', ?)
                """,
                (document_id, next_revision, _utc_now()),
            )
            self.connection.executemany(
                """
                INSERT INTO document_revision_chunks(
                    document_id, index_revision, chunk_key, content_revision, ordinal
                ) VALUES(?, ?, ?, ?, ?)
                """,
                [
                    (
                        document_id,
                        next_revision,
                        chunk.chunk_key,
                        chunk.content_revision,
                        chunk.ordinal,
                    )
                    for chunk in indexed_chunks
                ],
            )
            if not staged:
                self.connection.execute(
                    "UPDATE documents SET status = 'preparing' WHERE document_id = ?",
                    (document_id,),
                )

        return PreparedRevision(
            document_id=document_id,
            directory_id=directory_id,
            relative_path=relative_path,
            index_revision=next_revision,
            modified_at_ns=modified_at_ns,
            chunks=tuple(indexed_chunks),
            chunks_to_index=tuple(chunks_to_index),
            unchanged_count=unchanged_count,
            changed=True,
        )

    def activate_revision(self, prepared: PreparedRevision) -> None:
        if not prepared.changed:
            return
        with self.transaction():
            self.connection.execute(
                """
                UPDATE document_revisions
                SET status = 'ready', error = NULL
                WHERE document_id = ? AND index_revision = ?
                """,
                (prepared.document_id, prepared.index_revision),
            )
            self.connection.execute(
                """
                UPDATE documents
                SET active_revision = ?, modified_at_ns = ?, status = 'ready'
                WHERE document_id = ?
                """,
                (
                    prepared.index_revision,
                    prepared.modified_at_ns,
                    prepared.document_id,
                ),
            )

    def fail_revision(self, prepared: PreparedRevision, error: str) -> None:
        if not prepared.changed:
            return
        with self.transaction():
            self.connection.execute(
                """
                UPDATE document_revisions
                SET status = 'failed', error = ?
                WHERE document_id = ? AND index_revision = ?
                """,
                (error, prepared.document_id, prepared.index_revision),
            )
            self.connection.execute(
                """
                UPDATE documents
                SET status = CASE WHEN active_revision IS NULL THEN 'failed' ELSE 'ready' END
                WHERE document_id = ?
                """,
                (prepared.document_id,),
            )

    def active_chunks(
        self, directory_ids: Sequence[int] | None = None
    ) -> list[sqlite3.Row]:
        params: list[object] = []
        directory_clause = ""
        if directory_ids:
            placeholders = ",".join("?" for _ in directory_ids)
            directory_clause = f" AND d.directory_id IN ({placeholders})"
            params.extend(directory_ids)
        return self.connection.execute(
            f"""
            SELECT d.document_id, d.directory_id, d.relative_path,
                   drc.chunk_key, drc.content_revision, drc.ordinal,
                   c.title, c.body
            FROM documents d
            JOIN document_revision_chunks drc
              ON drc.document_id = d.document_id
             AND drc.index_revision = d.active_revision
            JOIN chunks c
              ON c.chunk_key = drc.chunk_key
             AND c.content_revision = drc.content_revision
            WHERE d.status = 'ready' {directory_clause}
            ORDER BY d.relative_path, drc.ordinal
            """,
            params,
        ).fetchall()

    def active_chunk_map(
        self, directory_ids: Sequence[int] | None = None
    ) -> dict[tuple[str, str], sqlite3.Row]:
        return {
            (str(row["chunk_key"]), str(row["content_revision"])): row
            for row in self.active_chunks(directory_ids)
        }

    def document_status(self, relative_path: str) -> sqlite3.Row | None:
        return self.connection.execute(
            "SELECT * FROM documents WHERE relative_path = ?", (relative_path,)
        ).fetchone()

    def _revision_rows(
        self, document_id: int, revision: int | None
    ) -> list[sqlite3.Row]:
        if revision is None:
            return []
        return self.connection.execute(
            """
            SELECT drc.chunk_key, drc.content_revision, drc.ordinal, c.title, c.body
            FROM document_revision_chunks drc
            JOIN chunks c
              ON c.chunk_key = drc.chunk_key
             AND c.content_revision = drc.content_revision
            WHERE drc.document_id = ? AND drc.index_revision = ?
            ORDER BY drc.ordinal
            """,
            (document_id, revision),
        ).fetchall()

    def delete_document(self, relative_path: str) -> list[tuple[str, str]]:
        row = self.connection.execute(
            "SELECT document_id FROM documents WHERE relative_path = ?", (relative_path,)
        ).fetchone()
        if row is None:
            return []
        document_id = int(row["document_id"])
        references = [
            (str(item["chunk_key"]), str(item["content_revision"]))
            for item in self.connection.execute(
                """
                SELECT DISTINCT chunk_key, content_revision
                FROM document_revision_chunks WHERE document_id = ?
                """,
                (document_id,),
            )
        ]
        with self.transaction():
            self._delete_document_rows(document_id)
        return references

    def document_references(self, relative_path: str) -> list[tuple[str, str]]:
        row = self.connection.execute(
            "SELECT document_id FROM documents WHERE relative_path = ?", (relative_path,)
        ).fetchone()
        if row is None:
            return []
        return [
            (str(item["chunk_key"]), str(item["content_revision"]))
            for item in self.connection.execute(
                """
                SELECT DISTINCT chunk_key, content_revision
                FROM document_revision_chunks WHERE document_id = ?
                """,
                (int(row["document_id"]),),
            )
        ]

    def recent_tasks(self, limit: int = 20) -> list[dict[str, object]]:
        return [
            dict(row)
            for row in self.connection.execute(
                "SELECT * FROM index_tasks ORDER BY task_id DESC LIMIT ?", (limit,)
            )
        ]


def _content_revision(chunk: Chunk, revision_salt: str | None = None) -> str:
    digest = hashlib.sha256()
    for value in (chunk.chunk_key, chunk.title, chunk.body):
        digest.update(value.encode("utf-8"))
        digest.update(b"\0")
    if revision_salt is not None:
        digest.update(revision_salt.encode("utf-8"))
        digest.update(b"\0")
    return digest.hexdigest()


def _utc_now() -> str:
    return datetime.now(UTC).isoformat()
