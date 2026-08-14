from __future__ import annotations

from retriever.config import AppConfig


def test_embedding_is_emitted_but_legacy_document_vector_weights_are_not() -> None:
    config = AppConfig.model_validate(
        {
            "embedding": {
                "provider": "sentence-transformers",
                "model": "legacy-model",
                "device": "cpu",
            },
            "search": {
                "vector_title_weight": 2.0,
                "vector_body_weight": 3.0,
                "vector_weight": 4.0,
            },
            "maintenance": {
                "optimize_lancedb_when_idle": True,
                "idle_seconds_before_maintenance": 60,
            },
        }
    )

    dumped = config.model_dump(mode="json")

    assert dumped["embedding"]["model"] == "legacy-model"
    assert "maintenance" not in dumped
    assert "vector_title_weight" not in dumped["search"]
    assert "vector_body_weight" not in dumped["search"]
    assert "vector_weight" not in dumped["search"]


def test_bm25_segmenter_version_changes_index_fingerprint() -> None:
    first = AppConfig()
    second = AppConfig()
    second.bm25.segmenter_version = 2

    assert first.index_format_fingerprint() != second.index_format_fingerprint()
