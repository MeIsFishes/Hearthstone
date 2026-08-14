from __future__ import annotations

from pathlib import Path

import pytest


pytest.importorskip("lancedb")
pytest.importorskip("pyarrow")

from retriever.search.vector import HashingEmbedder, TermVectorSearch


def test_term_vector_database_syncs_terms_and_finds_nearest(tmp_path: Path) -> None:
    vectors = TermVectorSearch(
        tmp_path / "term-vectors",
        HashingEmbedder(dimension=32),
        batch_size=2,
    )

    vectors.replace_directory(1, ["alpha", "beta", "alpha"])
    assert vectors.count() == 2
    nearest = vectors.nearest(
        "alpha",
        [1],
        limit=1,
        similarity_threshold=0.99,
    )
    assert nearest[0]["term"] == "alpha"
    assert nearest[0]["similarity"] == pytest.approx(1.0)
    pairs = vectors.candidate_pairs(
        1,
        limit_per_term=1,
        similarity_threshold=-1.0,
    )
    assert {(item["term"], item["candidate_term"]) for item in pairs} == {
        ("alpha", "beta")
    }

    vectors.replace_directory(1, ["beta", "gamma"])
    assert vectors.count() == 2
    vectors.clear([1])
    assert vectors.count() == 0
