from __future__ import annotations

import re
import sqlite3
import threading
import uuid
from collections import Counter
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Any, Iterable, Mapping, Sequence

import jieba

from retriever.models import IndexedChunk


ChunkRef = tuple[str, str]
_TOKEN_RE = re.compile(r"[A-Za-z0-9_]+|[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]+")
_SAFE_TOKENIZER_RE = re.compile(r"^[A-Za-z0-9_ ]+$")
_HAN_RE = re.compile(r"^[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]+$")


def tokenize_for_fts(text: str) -> list[str]:
    tokens: list[str] = []
    for segment in jieba.cut_for_search(text.casefold(), HMM=True):
        tokens.extend(match.group(0) for match in _TOKEN_RE.finditer(segment))
    return tokens


def prepare_fts_text(text: str) -> str:
    return " ".join(tokenize_for_fts(text))


def prepare_match_query(text: str) -> str:
    tokens = list(dict.fromkeys(tokenize_for_fts(text)))
    return " OR ".join(f'"{token.replace(chr(34), chr(34) * 2)}"' for token in tokens)


class BM25Search:
    def __init__(self, database_path: str | Path, tokenizer: str = "unicode61") -> None:
        if not _SAFE_TOKENIZER_RE.fullmatch(tokenizer):
            raise ValueError(f"Unsafe FTS5 tokenizer expression: {tokenizer}")
        self.database_path = Path(database_path)
        self.database_path.parent.mkdir(parents=True, exist_ok=True)
        self.connection = sqlite3.connect(
            self.database_path,
            check_same_thread=False,
            isolation_level=None,
        )
        self.connection.row_factory = sqlite3.Row
        self.connection.execute("PRAGMA journal_mode = WAL")
        self.connection.execute("PRAGMA synchronous = FULL")
        self._lock = threading.RLock()
        self._create_schema(tokenizer)

    def _create_schema(self, tokenizer: str) -> None:
        with self._lock:
            self.connection.executescript(
                f"""
                CREATE VIRTUAL TABLE IF NOT EXISTS chunk_title_fts USING fts5(
                    directory_id UNINDEXED,
                    chunk_key UNINDEXED,
                    content_revision UNINDEXED,
                    search_text,
                    tokenize = '{tokenizer}'
                );
                CREATE VIRTUAL TABLE IF NOT EXISTS chunk_body_fts USING fts5(
                    directory_id UNINDEXED,
                    chunk_key UNINDEXED,
                    content_revision UNINDEXED,
                    search_text,
                    tokenize = '{tokenizer}'
                );
                CREATE TEMP TABLE IF NOT EXISTS allowed_chunks (
                    chunk_key TEXT NOT NULL,
                    content_revision TEXT NOT NULL,
                    PRIMARY KEY(chunk_key, content_revision)
                ) WITHOUT ROWID;
                CREATE TABLE IF NOT EXISTS document_terms (
                    directory_id INTEGER NOT NULL,
                    chunk_key TEXT NOT NULL,
                    content_revision TEXT NOT NULL,
                    term TEXT NOT NULL,
                    term_count INTEGER NOT NULL,
                    PRIMARY KEY(
                        directory_id, chunk_key, content_revision, term
                    )
                ) WITHOUT ROWID;
                CREATE INDEX IF NOT EXISTS idx_document_terms_reference
                    ON document_terms(chunk_key, content_revision);
                CREATE TABLE IF NOT EXISTS lexicon_terms (
                    directory_id INTEGER NOT NULL,
                    term TEXT NOT NULL,
                    document_frequency INTEGER NOT NULL,
                    total_frequency INTEGER NOT NULL,
                    vector_eligible INTEGER NOT NULL,
                    updated_at TEXT NOT NULL,
                    PRIMARY KEY(directory_id, term)
                ) WITHOUT ROWID;
                CREATE TABLE IF NOT EXISTS synonym_relations (
                    directory_id INTEGER NOT NULL,
                    term_a TEXT NOT NULL,
                    term_b TEXT NOT NULL,
                    status TEXT NOT NULL CHECK(
                        status IN ('pending', 'accepted', 'rejected')
                    ),
                    negative_count INTEGER NOT NULL DEFAULT 0,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    PRIMARY KEY(directory_id, term_a, term_b),
                    CHECK(term_a < term_b)
                ) WITHOUT ROWID;
                CREATE TABLE IF NOT EXISTS synonym_review_requests (
                    request_id TEXT PRIMARY KEY,
                    search_id TEXT NOT NULL,
                    directory_id INTEGER NOT NULL,
                    query_term TEXT NOT NULL,
                    candidate_term TEXT NOT NULL,
                    term_a TEXT NOT NULL,
                    term_b TEXT NOT NULL,
                    similarity REAL NOT NULL,
                    created_at TEXT NOT NULL,
                    UNIQUE(search_id, directory_id, term_a, term_b)
                );
                CREATE INDEX IF NOT EXISTS idx_synonym_reviews_pair
                    ON synonym_review_requests(
                        directory_id, term_a, term_b, created_at
                    );
                CREATE TABLE IF NOT EXISTS synonym_feedback_events (
                    event_id TEXT PRIMARY KEY,
                    search_id TEXT NOT NULL,
                    directory_id INTEGER NOT NULL,
                    term_a TEXT NOT NULL,
                    term_b TEXT NOT NULL,
                    verdict TEXT NOT NULL CHECK(
                        verdict IN ('equivalent', 'not_equivalent', 'unsure')
                    ),
                    created_at TEXT NOT NULL,
                    UNIQUE(search_id, directory_id, term_a, term_b)
                );
                CREATE TABLE IF NOT EXISTS equivalence_terms (
                    directory_id INTEGER NOT NULL,
                    term TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    PRIMARY KEY(directory_id, term)
                ) WITHOUT ROWID;
                CREATE TABLE IF NOT EXISTS equivalence_entries (
                    directory_id INTEGER NOT NULL,
                    term TEXT NOT NULL,
                    related_term TEXT NOT NULL,
                    category TEXT NOT NULL CHECK(
                        category IN (
                            'equivalent', 'candidate', 'non_equivalent'
                        )
                    ),
                    similarity REAL,
                    negative_count INTEGER NOT NULL DEFAULT 0,
                    origin TEXT NOT NULL DEFAULT 'index',
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    PRIMARY KEY(directory_id, term, related_term),
                    CHECK(term <> related_term)
                ) WITHOUT ROWID;
                CREATE INDEX IF NOT EXISTS idx_equivalence_entries_category
                    ON equivalence_entries(directory_id, category, term);
                CREATE TABLE IF NOT EXISTS bm25_metadata (
                    key TEXT PRIMARY KEY,
                    value TEXT NOT NULL
                ) WITHOUT ROWID;
                """
            )
            self.connection.commit()
            self._migrate_legacy_relations()

    def _migrate_legacy_relations(self) -> None:
        migrated = self.connection.execute(
            "SELECT value FROM bm25_metadata WHERE key = 'equivalence_lists_v1'"
        ).fetchone()
        if migrated is not None:
            return
        now = _utc_now()
        rows = self.connection.execute(
            """
            SELECT directory_id, term_a, term_b, status, negative_count,
                   created_at, updated_at
            FROM synonym_relations
            """
        ).fetchall()
        try:
            self.connection.execute("BEGIN IMMEDIATE")
            for row in rows:
                category = {
                    "accepted": "equivalent",
                    "rejected": "non_equivalent",
                    "pending": "candidate",
                }[str(row["status"])]
                self._write_pair_entries(
                    directory_id=int(row["directory_id"]),
                    first=str(row["term_a"]),
                    second=str(row["term_b"]),
                    category=category,
                    similarity=None,
                    negative_count=int(row["negative_count"]),
                    origin="legacy",
                    created_at=str(row["created_at"]),
                    updated_at=str(row["updated_at"]),
                    overwrite=False,
                )
            self.connection.execute(
                """
                INSERT INTO bm25_metadata(key, value)
                VALUES('equivalence_lists_v1', ?)
                """,
                (now,),
            )
            self.connection.commit()
        except Exception:
            self.connection.rollback()
            raise

    def _write_pair_entries(
        self,
        *,
        directory_id: int,
        first: str,
        second: str,
        category: str,
        similarity: float | None,
        negative_count: int,
        origin: str,
        created_at: str,
        updated_at: str,
        overwrite: bool,
    ) -> None:
        for term in (first, second):
            self.connection.execute(
                """
                INSERT INTO equivalence_terms(
                    directory_id, term, created_at, updated_at
                ) VALUES(?, ?, ?, ?)
                ON CONFLICT(directory_id, term) DO UPDATE SET
                    updated_at = excluded.updated_at
                """,
                (directory_id, term, created_at, updated_at),
            )
        conflict = (
            "DO UPDATE SET category = excluded.category, "
            "similarity = excluded.similarity, "
            "negative_count = excluded.negative_count, "
            "origin = excluded.origin, updated_at = excluded.updated_at"
            if overwrite
            else "DO NOTHING"
        )
        for term, related_term in ((first, second), (second, first)):
            self.connection.execute(
                f"""
                INSERT INTO equivalence_entries(
                    directory_id, term, related_term, category, similarity,
                    negative_count, origin, created_at, updated_at
                ) VALUES(?, ?, ?, ?, ?, ?, ?, ?, ?)
                ON CONFLICT(directory_id, term, related_term) {conflict}
                """,
                (
                    directory_id,
                    term,
                    related_term,
                    category,
                    similarity,
                    negative_count,
                    origin,
                    created_at,
                    updated_at,
                ),
            )

    def close(self) -> None:
        with self._lock:
            self.connection.close()

    def upsert(self, directory_id: int, chunks: Sequence[IndexedChunk]) -> None:
        if not chunks:
            return
        with self._lock:
            try:
                self.connection.execute("BEGIN IMMEDIATE")
                for chunk in chunks:
                    parameters = (chunk.chunk_key, chunk.content_revision)
                    self.connection.execute(
                        """
                        DELETE FROM chunk_title_fts
                        WHERE chunk_key = ? AND content_revision = ?
                        """,
                        parameters,
                    )
                    self.connection.execute(
                        """
                        DELETE FROM chunk_body_fts
                        WHERE chunk_key = ? AND content_revision = ?
                        """,
                        parameters,
                    )
                    self.connection.execute(
                        """
                        INSERT INTO chunk_title_fts(
                            directory_id, chunk_key, content_revision, search_text
                        ) VALUES(?, ?, ?, ?)
                        """,
                        (
                            directory_id,
                            chunk.chunk_key,
                            chunk.content_revision,
                            prepare_fts_text(chunk.title),
                        ),
                    )
                    self.connection.execute(
                        """
                        DELETE FROM document_terms
                        WHERE chunk_key = ? AND content_revision = ?
                        """,
                        parameters,
                    )
                    terms = Counter(tokenize_for_fts(f"{chunk.title}\n{chunk.body}"))
                    self.connection.executemany(
                        """
                        INSERT INTO document_terms(
                            directory_id, chunk_key, content_revision,
                            term, term_count
                        ) VALUES(?, ?, ?, ?, ?)
                        """,
                        [
                            (
                                directory_id,
                                chunk.chunk_key,
                                chunk.content_revision,
                                term,
                                count,
                            )
                            for term, count in terms.items()
                        ],
                    )
                    self.connection.execute(
                        """
                        INSERT INTO chunk_body_fts(
                            directory_id, chunk_key, content_revision, search_text
                        ) VALUES(?, ?, ?, ?)
                        """,
                        (
                            directory_id,
                            chunk.chunk_key,
                            chunk.content_revision,
                            prepare_fts_text(chunk.body),
                        ),
                    )
                self.connection.commit()
            except Exception:
                self.connection.rollback()
                raise

    def delete_refs(self, references: Iterable[ChunkRef]) -> None:
        refs = list(references)
        if not refs:
            return
        with self._lock:
            try:
                self.connection.execute("BEGIN IMMEDIATE")
                self.connection.executemany(
                    """
                    DELETE FROM chunk_title_fts
                    WHERE chunk_key = ? AND content_revision = ?
                    """,
                    refs,
                )
                self.connection.executemany(
                    """
                    DELETE FROM document_terms
                    WHERE chunk_key = ? AND content_revision = ?
                    """,
                    refs,
                )
                self.connection.executemany(
                    """
                    DELETE FROM chunk_body_fts
                    WHERE chunk_key = ? AND content_revision = ?
                    """,
                    refs,
                )
                self.connection.commit()
            except Exception:
                self.connection.rollback()
                raise

    def clear(self, directory_ids: Sequence[int] | None = None) -> None:
        with self._lock:
            try:
                self.connection.execute("BEGIN IMMEDIATE")
                if directory_ids:
                    placeholders = ",".join("?" for _ in directory_ids)
                    for table in ("chunk_title_fts", "chunk_body_fts"):
                        self.connection.execute(
                            f"DELETE FROM {table} WHERE directory_id IN ({placeholders})",
                            list(directory_ids),
                        )
                    for table in (
                        "document_terms",
                        "lexicon_terms",
                        "equivalence_terms",
                        "equivalence_entries",
                        "synonym_relations",
                        "synonym_review_requests",
                        "synonym_feedback_events",
                    ):
                        self.connection.execute(
                            f"DELETE FROM {table} WHERE directory_id IN ({placeholders})",
                            list(directory_ids),
                        )
                else:
                    self.connection.execute("DELETE FROM chunk_title_fts")
                    self.connection.execute("DELETE FROM chunk_body_fts")
                    self.connection.execute("DELETE FROM document_terms")
                    self.connection.execute("DELETE FROM lexicon_terms")
                    self.connection.execute("DELETE FROM equivalence_terms")
                    self.connection.execute("DELETE FROM equivalence_entries")
                    self.connection.execute("DELETE FROM synonym_relations")
                    self.connection.execute("DELETE FROM synonym_review_requests")
                    self.connection.execute("DELETE FROM synonym_feedback_events")
                self.connection.commit()
            except Exception:
                self.connection.rollback()
                raise

    def refresh_lexicon(
        self,
        directory_id: int,
        active_references: Iterable[ChunkRef],
    ) -> None:
        refs = list(active_references)
        now = _utc_now()
        with self._lock:
            try:
                self.connection.execute("BEGIN IMMEDIATE")
                self.connection.execute("DELETE FROM allowed_chunks")
                self.connection.executemany(
                    "INSERT INTO allowed_chunks(chunk_key, content_revision) VALUES(?, ?)",
                    refs,
                )
                self.connection.execute(
                    "DELETE FROM lexicon_terms WHERE directory_id = ?",
                    (directory_id,),
                )
                rows = self.connection.execute(
                    """
                    SELECT d.term,
                           COUNT(DISTINCT d.chunk_key || char(0) || d.content_revision)
                               AS document_frequency,
                           SUM(d.term_count) AS total_frequency
                    FROM document_terms d
                    JOIN allowed_chunks a
                      ON a.chunk_key = d.chunk_key
                     AND a.content_revision = d.content_revision
                    WHERE d.directory_id = ?
                    GROUP BY d.term
                    """,
                    (directory_id,),
                ).fetchall()
                document_count = len(refs)
                self.connection.executemany(
                    """
                    INSERT INTO lexicon_terms(
                        directory_id, term, document_frequency,
                        total_frequency, vector_eligible, updated_at
                    ) VALUES(?, ?, ?, ?, ?, ?)
                    """,
                    [
                        (
                            directory_id,
                            str(row["term"]),
                            int(row["document_frequency"]),
                            int(row["total_frequency"]),
                            int(
                                _is_vector_eligible(
                                    str(row["term"]),
                                    int(row["document_frequency"]),
                                    document_count,
                                )
                            ),
                            now,
                        )
                        for row in rows
                    ],
                )
                self.connection.executemany(
                    """
                    INSERT INTO equivalence_terms(
                        directory_id, term, created_at, updated_at
                    ) VALUES(?, ?, ?, ?)
                    ON CONFLICT(directory_id, term) DO UPDATE SET
                        updated_at = excluded.updated_at
                    """,
                    [
                        (directory_id, str(row["term"]), now, now)
                        for row in rows
                    ],
                )
                self.connection.commit()
            except Exception:
                self.connection.rollback()
                raise

    def lexicon_terms(
        self,
        directory_ids: Sequence[int] | None = None,
        *,
        vector_eligible_only: bool = False,
    ) -> list[dict[str, Any]]:
        clauses: list[str] = []
        parameters: list[object] = []
        if directory_ids:
            placeholders = ",".join("?" for _ in directory_ids)
            clauses.append(f"directory_id IN ({placeholders})")
            parameters.extend(directory_ids)
        if vector_eligible_only:
            clauses.append("vector_eligible = 1")
        where = f"WHERE {' AND '.join(clauses)}" if clauses else ""
        with self._lock:
            rows = self.connection.execute(
                f"""
                SELECT directory_id, term, document_frequency,
                       total_frequency, vector_eligible
                FROM lexicon_terms
                {where}
                ORDER BY directory_id, term
                """,
                parameters,
            ).fetchall()
        return [dict(row) for row in rows]

    def known_terms_by_directory(
        self,
        terms: Iterable[str],
        directory_ids: Sequence[int],
    ) -> dict[int, set[str]]:
        values = sorted(set(terms))
        result = {directory_id: set() for directory_id in directory_ids}
        if not values or not directory_ids:
            return result
        directory_placeholders = ",".join("?" for _ in directory_ids)
        term_placeholders = ",".join("?" for _ in values)
        with self._lock:
            rows = self.connection.execute(
                f"""
                SELECT directory_id, term
                FROM lexicon_terms
                WHERE directory_id IN ({directory_placeholders})
                  AND term IN ({term_placeholders})
                """,
                [*directory_ids, *values],
            ).fetchall()
        for row in rows:
            result[int(row["directory_id"])].add(str(row["term"]))
        return result

    def add_candidate_pair(
        self,
        directory_id: int,
        first: str,
        second: str,
        similarity: float,
        *,
        origin: str = "index",
    ) -> bool:
        term_a, term_b = _ordered_pair(first, second)
        now = _utc_now()
        with self._lock:
            current = self.connection.execute(
                """
                SELECT category, similarity, negative_count, origin, created_at
                FROM equivalence_entries
                WHERE directory_id = ? AND term = ? AND related_term = ?
                """,
                (directory_id, term_a, term_b),
            ).fetchone()
            if current is not None and str(current["category"]) in {
                "equivalent",
                "non_equivalent",
            }:
                return False
            previous_similarity = (
                float(current["similarity"])
                if current is not None and current["similarity"] is not None
                else -1.0
            )
            negative_count = int(current["negative_count"] if current else 0)
            created_at = str(current["created_at"] if current else now)
            stored_origin = str(current["origin"] if current else origin)
            try:
                self.connection.execute("BEGIN IMMEDIATE")
                self._write_pair_entries(
                    directory_id=directory_id,
                    first=term_a,
                    second=term_b,
                    category="candidate",
                    similarity=max(similarity, previous_similarity),
                    negative_count=negative_count,
                    origin=stored_origin,
                    created_at=created_at,
                    updated_at=now,
                    overwrite=True,
                )
                self.connection.commit()
            except Exception:
                self.connection.rollback()
                raise
        return current is None

    def accepted_expansions(
        self, term: str, directory_ids: Sequence[int]
    ) -> list[dict[str, Any]]:
        if not directory_ids:
            return []
        placeholders = ",".join("?" for _ in directory_ids)
        with self._lock:
            rows = self.connection.execute(
                f"""
                SELECT directory_id, related_term, category, negative_count
                FROM equivalence_entries
                WHERE directory_id IN ({placeholders})
                  AND category = 'equivalent'
                  AND term = ?
                ORDER BY directory_id, related_term
                """,
                [*directory_ids, term],
            ).fetchall()
        return [
            {
                "directory_id": int(row["directory_id"]),
                "term": str(row["related_term"]),
                "status": "accepted",
                "category": str(row["category"]),
                "negative_count": int(row["negative_count"]),
            }
            for row in rows
        ]

    def relation_state(
        self, directory_id: int, first: str, second: str
    ) -> dict[str, Any] | None:
        term_a, term_b = _ordered_pair(first, second)
        with self._lock:
            row = self.connection.execute(
                """
                SELECT directory_id, term, related_term, category,
                       similarity, negative_count, origin,
                       created_at, updated_at
                FROM equivalence_entries
                WHERE directory_id = ? AND term = ? AND related_term = ?
                """,
                (directory_id, term_a, term_b),
            ).fetchone()
        if row is None:
            return None
        value = dict(row)
        value.update(
            {
                "term_a": term_a,
                "term_b": term_b,
                "status": {
                    "equivalent": "accepted",
                    "candidate": "pending",
                    "non_equivalent": "rejected",
                }[str(row["category"])],
            }
        )
        return value

    def create_review_request(
        self,
        *,
        search_id: str,
        directory_id: int,
        query_term: str,
        candidate_term: str,
        similarity: float,
        cooldown_hours: int,
    ) -> dict[str, Any] | None:
        term_a, term_b = _ordered_pair(query_term, candidate_term)
        now = datetime.now(timezone.utc)
        cutoff = (now - timedelta(hours=cooldown_hours)).isoformat()
        with self._lock:
            state = self.connection.execute(
                """
                SELECT category FROM equivalence_entries
                WHERE directory_id = ? AND term = ? AND related_term = ?
                """,
                (directory_id, term_a, term_b),
            ).fetchone()
            if state is None or str(state["category"]) != "candidate":
                return None
            recent = self.connection.execute(
                """
                SELECT 1 FROM synonym_review_requests
                WHERE directory_id = ? AND term_a = ? AND term_b = ?
                  AND created_at >= ?
                LIMIT 1
                """,
                (directory_id, term_a, term_b, cutoff),
            ).fetchone()
            if recent is not None:
                return None
            request_id = str(uuid.uuid4())
            created_at = now.isoformat()
            try:
                self.connection.execute("BEGIN IMMEDIATE")
                self.connection.execute(
                    """
                    INSERT INTO synonym_review_requests(
                        request_id, search_id, directory_id, query_term,
                        candidate_term, term_a, term_b, similarity, created_at
                    ) VALUES(?, ?, ?, ?, ?, ?, ?, ?, ?)
                    """,
                    (
                        request_id,
                        search_id,
                        directory_id,
                        query_term,
                        candidate_term,
                        term_a,
                        term_b,
                        similarity,
                        created_at,
                    ),
                )
                self.connection.commit()
            except Exception:
                self.connection.rollback()
                raise
        return {
            "request_id": request_id,
            "search_id": search_id,
            "directory_id": directory_id,
            "query_term": query_term,
            "candidate_term": candidate_term,
            "similarity": similarity,
            "question": (
                f"在当前文档目录的检索语境中，‘{query_term}’与"
                f"‘{candidate_term}’是否可以作为检索等价词？"
            ),
        }

    def submit_feedback(
        self,
        *,
        search_id: str,
        directory_id: int,
        query_term: str,
        candidate_term: str,
        verdict: str,
        rejection_threshold: int,
    ) -> dict[str, Any]:
        if verdict not in {"equivalent", "not_equivalent", "unsure"}:
            raise ValueError(f"Unsupported feedback verdict: {verdict}")
        term_a, term_b = _ordered_pair(query_term, candidate_term)
        now = _utc_now()
        with self._lock:
            request = self.connection.execute(
                """
                SELECT 1 FROM synonym_review_requests
                WHERE search_id = ? AND directory_id = ?
                  AND term_a = ? AND term_b = ?
                """,
                (search_id, directory_id, term_a, term_b),
            ).fetchone()
            if request is None:
                raise ValueError("Feedback does not match a recorded review request")
            existing_event = self.connection.execute(
                """
                SELECT verdict FROM synonym_feedback_events
                WHERE search_id = ? AND directory_id = ?
                  AND term_a = ? AND term_b = ?
                """,
                (search_id, directory_id, term_a, term_b),
            ).fetchone()
            if existing_event is not None:
                if str(existing_event["verdict"]) != verdict:
                    raise ValueError("Feedback for this search is already recorded")
                state = self.relation_state(directory_id, term_a, term_b)
                assert state is not None
                return {**state, "idempotent": True}

            current = self.relation_state(directory_id, term_a, term_b)
            if current is None:
                raise ValueError("The reviewed pair is not in a candidate list")
            if current["category"] in {"equivalent", "non_equivalent"}:
                return {**current, "idempotent": True}
            try:
                self.connection.execute("BEGIN IMMEDIATE")
                self.connection.execute(
                    """
                    INSERT INTO synonym_feedback_events(
                        event_id, search_id, directory_id, term_a, term_b,
                        verdict, created_at
                    ) VALUES(?, ?, ?, ?, ?, ?, ?)
                    """,
                    (
                        str(uuid.uuid4()),
                        search_id,
                        directory_id,
                        term_a,
                        term_b,
                        verdict,
                        now,
                    ),
                )
                if verdict == "equivalent":
                    category = "equivalent"
                    negative_count = int(current["negative_count"])
                else:
                    negative_count = int(
                        self.connection.execute(
                            """
                            SELECT COUNT(DISTINCT search_id)
                            FROM synonym_feedback_events
                            WHERE directory_id = ? AND term_a = ? AND term_b = ?
                              AND verdict = 'not_equivalent'
                            """,
                            (directory_id, term_a, term_b),
                        ).fetchone()[0]
                    )
                    category = (
                        "non_equivalent"
                        if negative_count >= rejection_threshold
                        else "candidate"
                    )
                self._write_pair_entries(
                    directory_id=directory_id,
                    first=term_a,
                    second=term_b,
                    category=category,
                    similarity=(
                        float(current["similarity"])
                        if current["similarity"] is not None
                        else None
                    ),
                    negative_count=negative_count,
                    origin=str(current["origin"]),
                    created_at=str(current["created_at"]),
                    updated_at=now,
                    overwrite=True,
                )
                self.connection.commit()
            except Exception:
                self.connection.rollback()
                raise
        state = self.relation_state(directory_id, term_a, term_b)
        assert state is not None
        return {**state, "idempotent": False}

    def list_relations(
        self,
        directory_ids: Sequence[int] | None = None,
        status: str | None = None,
    ) -> list[dict[str, Any]]:
        clauses: list[str] = []
        parameters: list[object] = []
        if directory_ids:
            placeholders = ",".join("?" for _ in directory_ids)
            clauses.append(f"directory_id IN ({placeholders})")
            parameters.extend(directory_ids)
        if status is not None:
            category = {
                "pending": "candidate",
                "accepted": "equivalent",
                "rejected": "non_equivalent",
                "candidate": "candidate",
                "equivalent": "equivalent",
                "non_equivalent": "non_equivalent",
            }.get(status)
            if category is None:
                raise ValueError(f"Unsupported relation status: {status}")
            clauses.append("category = ?")
            parameters.append(category)
        where = f"WHERE {' AND '.join(clauses)}" if clauses else ""
        with self._lock:
            rows = self.connection.execute(
                f"""
                SELECT directory_id, term, related_term, category,
                       similarity, negative_count, origin,
                       created_at, updated_at
                FROM equivalence_entries
                {where}
                ORDER BY directory_id, term, category, related_term
                """,
                parameters,
            ).fetchall()
        records: dict[tuple[int, str], dict[str, Any]] = {}
        for row in rows:
            key = (int(row["directory_id"]), str(row["term"]))
            record = records.setdefault(
                key,
                {
                    "directory_id": key[0],
                    "term": key[1],
                    "equivalent_terms": [],
                    "candidate_equivalent_terms": [],
                    "non_equivalent_terms": [],
                },
            )
            related_term = str(row["related_term"])
            category = str(row["category"])
            if category == "equivalent":
                record["equivalent_terms"].append(related_term)
            elif category == "non_equivalent":
                record["non_equivalent_terms"].append(related_term)
            else:
                record["candidate_equivalent_terms"].append(
                    {
                        "term": related_term,
                        "similarity": (
                            float(row["similarity"])
                            if row["similarity"] is not None
                            else None
                        ),
                        "negative_count": int(row["negative_count"]),
                        "origin": str(row["origin"]),
                    }
                )
        return list(records.values())

    def review_candidates_for_results(
        self,
        *,
        search_id: str,
        references: Sequence[ChunkRef],
        limit: int,
        cooldown_hours: int,
    ) -> list[dict[str, Any]]:
        if not references or limit <= 0:
            return []
        with self._lock:
            self.connection.execute("DELETE FROM allowed_chunks")
            self.connection.executemany(
                "INSERT INTO allowed_chunks(chunk_key, content_revision) VALUES(?, ?)",
                references,
            )
            rows = self.connection.execute(
                """
                SELECT d.directory_id, d.chunk_key, e.term, e.related_term,
                       e.similarity, e.negative_count
                FROM document_terms d
                JOIN allowed_chunks a
                  ON a.chunk_key = d.chunk_key
                 AND a.content_revision = d.content_revision
                JOIN equivalence_entries e
                  ON e.directory_id = d.directory_id
                 AND e.term = d.term
                WHERE e.category = 'candidate'
                ORDER BY e.negative_count DESC,
                         e.similarity DESC,
                         e.term ASC,
                         e.related_term ASC,
                         d.chunk_key ASC
                """
            ).fetchall()
        pairs: dict[tuple[int, str, str], dict[str, Any]] = {}
        for row in rows:
            term_a, term_b = _ordered_pair(
                str(row["term"]), str(row["related_term"])
            )
            key = (int(row["directory_id"]), term_a, term_b)
            item = pairs.setdefault(
                key,
                {
                    "query_term": str(row["term"]),
                    "candidate_term": str(row["related_term"]),
                    "similarity": float(row["similarity"] or 0.0),
                    "document_chunk_keys": [],
                },
            )
            chunk_key = str(row["chunk_key"])
            if chunk_key not in item["document_chunk_keys"]:
                item["document_chunk_keys"].append(chunk_key)

        requests: list[dict[str, Any]] = []
        for (directory_id, _, _), item in pairs.items():
            request = self.create_review_request(
                search_id=search_id,
                directory_id=directory_id,
                query_term=str(item["query_term"]),
                candidate_term=str(item["candidate_term"]),
                similarity=float(item["similarity"]),
                cooldown_hours=cooldown_hours,
            )
            if request is None:
                continue
            request["document_chunk_keys"] = list(item["document_chunk_keys"])
            requests.append(request)
            if len(requests) >= limit:
                break
        return requests

    def reset_relation(self, directory_id: int, first: str, second: str) -> bool:
        term_a, term_b = _ordered_pair(first, second)
        with self._lock:
            try:
                self.connection.execute("BEGIN IMMEDIATE")
                changed = False
                cursor = self.connection.execute(
                    """
                    DELETE FROM equivalence_entries
                    WHERE directory_id = ?
                      AND ((term = ? AND related_term = ?)
                        OR (term = ? AND related_term = ?))
                    """,
                    (directory_id, term_a, term_b, term_b, term_a),
                )
                changed = cursor.rowcount > 0
                for table in (
                    "synonym_feedback_events",
                    "synonym_review_requests",
                ):
                    self.connection.execute(
                        f"""
                        DELETE FROM {table}
                        WHERE directory_id = ? AND term_a = ? AND term_b = ?
                        """,
                        (directory_id, term_a, term_b),
                    )
                self.connection.commit()
            except Exception:
                self.connection.rollback()
                raise
        return changed

    def feedback_stats(self) -> dict[str, int]:
        with self._lock:
            values = {
                str(row["category"]): int(row["count"])
                for row in self.connection.execute(
                    """
                    SELECT category, COUNT(*) AS count
                    FROM equivalence_entries
                    WHERE term < related_term
                    GROUP BY category
                    """
                ).fetchall()
            }
        return {
            "candidate": values.get("candidate", 0),
            "equivalent": values.get("equivalent", 0),
            "non_equivalent": values.get("non_equivalent", 0),
        }

    def contains_refs(self, references: Iterable[ChunkRef]) -> bool:
        refs = list(references)
        if not refs:
            return True
        with self._lock:
            self.connection.execute("DELETE FROM allowed_chunks")
            self.connection.executemany(
                "INSERT INTO allowed_chunks(chunk_key, content_revision) VALUES(?, ?)",
                refs,
            )
            for table in ("chunk_title_fts", "chunk_body_fts"):
                count = int(
                    self.connection.execute(
                        f"""
                        SELECT COUNT(*) FROM {table} f
                        JOIN allowed_chunks a
                          ON a.chunk_key = f.chunk_key
                         AND a.content_revision = f.content_revision
                        """
                    ).fetchone()[0]
                )
                if count != len(refs):
                    return False
        return True

    def recall(
        self,
        query: str,
        allowed: Mapping[ChunkRef, object] | Iterable[ChunkRef],
        limit: int,
        directory_ids: Sequence[int] | None = None,
    ) -> tuple[dict[ChunkRef, float], dict[ChunkRef, float]]:
        refs = list(allowed.keys() if isinstance(allowed, Mapping) else allowed)
        return (
            self._search_table(
                "chunk_title_fts", query, refs, limit, directory_ids=directory_ids
            ),
            self._search_table(
                "chunk_body_fts", query, refs, limit, directory_ids=directory_ids
            ),
        )

    def score_refs(
        self,
        query: str,
        references: Iterable[ChunkRef],
        directory_ids: Sequence[int] | None = None,
    ) -> tuple[dict[ChunkRef, float], dict[ChunkRef, float]]:
        refs = list(references)
        limit = max(len(refs), 1)
        return (
            self._search_table(
                "chunk_title_fts", query, refs, limit, directory_ids=directory_ids
            ),
            self._search_table(
                "chunk_body_fts", query, refs, limit, directory_ids=directory_ids
            ),
        )

    def _search_table(
        self,
        table: str,
        query: str,
        allowed: list[ChunkRef],
        limit: int,
        *,
        directory_ids: Sequence[int] | None,
    ) -> dict[ChunkRef, float]:
        match_query = prepare_match_query(query)
        if not match_query or not allowed:
            return {}
        with self._lock:
            self.connection.execute("DELETE FROM allowed_chunks")
            self.connection.executemany(
                "INSERT INTO allowed_chunks(chunk_key, content_revision) VALUES(?, ?)",
                allowed,
            )
            parameters: list[object] = [match_query]
            directory_clause = ""
            if directory_ids:
                placeholders = ",".join("?" for _ in directory_ids)
                directory_clause = f" AND f.directory_id IN ({placeholders})"
                parameters.extend(directory_ids)
            parameters.append(limit)
            rows = self.connection.execute(
                f"""
                SELECT f.chunk_key, f.content_revision, bm25({table}) AS raw_score
                FROM {table} f
                JOIN allowed_chunks a
                  ON a.chunk_key = f.chunk_key
                 AND a.content_revision = f.content_revision
                WHERE {table} MATCH ? {directory_clause}
                ORDER BY raw_score ASC
                LIMIT ?
                """,
                parameters,
            ).fetchall()
        return {
            (str(row["chunk_key"]), str(row["content_revision"])): float(
                row["raw_score"]
            )
            for row in rows
        }


def _ordered_pair(first: str, second: str) -> tuple[str, str]:
    left = first.strip().casefold()
    right = second.strip().casefold()
    if not left or not right:
        raise ValueError("Synonym terms must not be empty")
    if left == right:
        raise ValueError("A term cannot be paired with itself")
    return (left, right) if left < right else (right, left)


def _is_vector_eligible(
    term: str, document_frequency: int, document_count: int
) -> bool:
    if term.isdecimal():
        return False
    if len(term) == 1 and (_HAN_RE.fullmatch(term) or term.isascii()):
        return False
    if document_count >= 20 and document_frequency / document_count > 0.8:
        return False
    return True


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()
