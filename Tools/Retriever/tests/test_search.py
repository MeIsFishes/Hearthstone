from __future__ import annotations

from pathlib import Path
import sqlite3

from retriever.config import SearchConfig
from retriever.models import IndexedChunk
from retriever.search import BM25Search, KeywordSearch
from retriever.search.bm25 import prepare_fts_text, tokenize_for_fts


def test_chinese_fts_preparation_uses_jieba_search_tokens() -> None:
    tokens = tokenize_for_fts("如何配置角色技能")
    prepared = prepare_fts_text("如何配置角色技能")

    assert "配置" in tokens
    assert "角色" in tokens
    assert "技能" in tokens
    assert prepared.split() == tokens


def test_keyword_search_is_high_score_first_and_persistent(tmp_path: Path) -> None:
    chunks = [
        IndexedChunk(
            "doc.md - skill",
            "r1",
            "skill configuration",
            "character skill configuration",
            0,
        ),
        IndexedChunk("doc.md - log", "r2", "logging", "log file path", 1),
    ]
    active = {
        (chunk.chunk_key, chunk.content_revision): {
            "relative_path": "doc.md",
            "title": chunk.title,
            "body": chunk.body,
        }
        for chunk in chunks
    }

    bm25_path = tmp_path / "bm25.db"
    first = KeywordSearch(
        BM25Search(bm25_path),
        SearchConfig(),
    )
    first.index_chunks(1, chunks)
    results = first.search("skill configuration", active, k=2, directory_ids=[1])
    first.close()

    assert results[0].chunk_key == "doc.md - skill"
    assert results[0].scores["bm25_body_relevance"] >= 0
    assert results[0].scores["final"] == results[0].scores["bm25"]
    assert "vector" not in results[0].scores

    reopened = KeywordSearch(
        BM25Search(bm25_path),
        SearchConfig(),
    )
    persisted = reopened.search(
        "skill configuration", active, k=1, directory_ids=[1]
    )
    reopened.close()
    assert persisted[0].chunk_key == "doc.md - skill"


def test_legacy_pair_status_migrates_to_per_term_lists(tmp_path: Path) -> None:
    database_path = tmp_path / "bm25.db"
    connection = sqlite3.connect(database_path)
    connection.execute(
        """
        CREATE TABLE synonym_relations (
            directory_id INTEGER NOT NULL,
            term_a TEXT NOT NULL,
            term_b TEXT NOT NULL,
            status TEXT NOT NULL,
            negative_count INTEGER NOT NULL DEFAULT 0,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL,
            PRIMARY KEY(directory_id, term_a, term_b)
        )
        """
    )
    connection.execute(
        """
        INSERT INTO synonym_relations(
            directory_id, term_a, term_b, status, negative_count,
            created_at, updated_at
        ) VALUES(1, '大模型', '语言模型', 'accepted', 0, 'now', 'now')
        """
    )
    connection.commit()
    connection.close()

    bm25 = BM25Search(database_path)
    try:
        records = bm25.list_relations([1])
    finally:
        bm25.close()

    large_model = next(record for record in records if record["term"] == "大模型")
    language_model = next(
        record for record in records if record["term"] == "语言模型"
    )
    assert large_model["equivalent_terms"] == ["语言模型"]
    assert language_model["equivalent_terms"] == ["大模型"]
