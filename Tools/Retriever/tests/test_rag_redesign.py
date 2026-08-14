from __future__ import annotations

from pathlib import Path
from typing import Sequence

from fastapi.testclient import TestClient

from retriever.api import create_app
from retriever.config import AppConfig
from retriever.manager import RetrieverManager


class FakeTermVectorIndex:
    embedding_fingerprint = "fake-semantic-v1"

    def __init__(self) -> None:
        self.directory_terms: dict[int, list[str]] = {}
        self.nearest_calls: list[tuple[str, tuple[int, ...]]] = []
        self.candidates: dict[str, list[dict[str, object]]] = {}
        self.index_candidates: list[dict[str, object]] = []
        self.fail_queries = False

    def replace_directory(self, directory_id: int, terms: Sequence[str]) -> None:
        self.directory_terms[directory_id] = list(terms)

    def nearest(
        self,
        text: str,
        directory_ids: Sequence[int],
        *,
        limit: int,
        similarity_threshold: float,
    ) -> list[dict[str, object]]:
        self.nearest_calls.append((text, tuple(directory_ids)))
        if self.fail_queries:
            raise RuntimeError("injected vector failure")
        return [
            candidate
            for candidate in self.candidates.get(text, [])
            if int(candidate["directory_id"]) in directory_ids
            and float(candidate["similarity"]) >= similarity_threshold
        ][:limit]

    def clear(self, directory_ids: Sequence[int] | None = None) -> None:
        if directory_ids is None:
            self.directory_terms.clear()
            return
        for directory_id in directory_ids:
            self.directory_terms.pop(directory_id, None)

    def count(self) -> int:
        return sum(len(terms) for terms in self.directory_terms.values())

    def candidate_pairs(
        self,
        directory_id: int,
        *,
        limit_per_term: int,
        similarity_threshold: float,
    ) -> list[dict[str, object]]:
        return [
            item
            for item in self.index_candidates
            if int(item["directory_id"]) == directory_id
            and float(item["similarity"]) >= similarity_threshold
        ]


def _config(tmp_path: Path) -> AppConfig:
    config = AppConfig.model_validate(
        {
            "index": {
                "directories": [],
                "storage_dir": str(tmp_path / "index"),
                "write_vmeta_next_to_source": True,
            },
            "search": {
                "default_k": 3,
                "equivalence_review_limit_per_search": 1,
                "synonym_review_cooldown_hours": 0,
            },
            "logging": {"log_dir": str(tmp_path / "logs"), "level": "INFO"},
        }
    )
    return config


def _manager_with_document(
    tmp_path: Path,
) -> tuple[RetrieverManager, FakeTermVectorIndex, Path, int]:
    docs = tmp_path / "docs"
    docs.mkdir()
    (docs / "doc.md").write_text(
        "# 战斗配置\n角色技能和冷却时间说明\n",
        encoding="utf-8",
    )
    vectors = FakeTermVectorIndex()
    manager = RetrieverManager(
        _config(tmp_path),
        process_cwd=tmp_path,
        term_vector_index=vectors,
    )
    directory = manager.add_directory(docs)
    vectors.index_candidates = [
        {
            "directory_id": directory.directory_id,
            "term": "角色",
            "candidate_term": "技能",
            "similarity": 0.88,
        }
    ]
    manager.index_directory(docs)
    vectors.candidates["招式系统"] = [
        {
            "directory_id": directory.directory_id,
            "term": "技能",
            "similarity": 0.93,
        }
    ]
    return manager, vectors, docs, directory.directory_id


def test_known_keyword_uses_bm25_without_query_embedding(tmp_path: Path) -> None:
    manager, vectors, docs, _ = _manager_with_document(tmp_path)
    try:
        execution = manager.search_detailed(
            keywords=["角色技能"], directory=docs
        )

        assert execution["keyword_statuses"][0]["status"] == "known"
        assert execution["review_requests"][0]["query_term"] in {"角色", "技能"}
        assert set(
            (
                execution["review_requests"][0]["query_term"],
                execution["review_requests"][0]["candidate_term"],
            )
        ) == {"角色", "技能"}
        assert vectors.nearest_calls == []
        assert execution["results"][0].chunk_key == "docs/doc.md"
        assert execution["results"][0].scores["final"] == execution["results"][0].scores["bm25"]
        assert all(
            match["source"] == "exact"
            for match in execution["results"][0].matches
        )
    finally:
        manager.close()


def test_index_build_populates_per_term_candidate_lists(tmp_path: Path) -> None:
    manager, _, docs, _ = _manager_with_document(tmp_path)
    try:
        records = manager.list_synonyms(directory=docs)
        role = next(record for record in records if record["term"] == "角色")
        skill = next(record for record in records if record["term"] == "技能")

        assert role["equivalent_terms"] == []
        assert role["non_equivalent_terms"] == []
        assert role["candidate_equivalent_terms"][0]["term"] == "技能"
        assert skill["candidate_equivalent_terms"][0]["term"] == "角色"
    finally:
        manager.close()


def test_oov_candidate_can_be_accepted_and_then_skips_vector_query(
    tmp_path: Path,
) -> None:
    manager, vectors, docs, directory_id = _manager_with_document(tmp_path)
    try:
        first = manager.search_detailed(keywords=["招式系统"], directory=docs)

        assert first["keyword_statuses"][0]["status"] == "oov"
        assert first["results"][0].matches[0]["source"] == "provisional"
        review = first["review_requests"][0]
        assert review["document_chunk_keys"] == ["docs/doc.md"]
        accepted = manager.submit_synonym_feedback(
            search_id=first["search_id"],
            directory_id=directory_id,
            query_term="招式系统",
            candidate_term="技能",
            verdict="equivalent",
        )
        assert accepted["status"] == "accepted"

        vectors.nearest_calls.clear()
        second = manager.search_detailed(keywords=["招式系统"], directory=docs)

        assert vectors.nearest_calls == []
        assert all(
            {item["query_term"], item["candidate_term"]}
            != {"招式系统", "技能"}
            for item in second["review_requests"]
        )
        assert second["expansions"][0]["source"] == "accepted"
        assert second["results"][0].matches[0]["source"] == "accepted"
        assert set((review["query_term"], review["candidate_term"])) == {
            "招式系统",
            "技能",
        }
        records = manager.list_synonyms(directory=docs)
        query_record = next(
            record for record in records if record["term"] == "招式系统"
        )
        assert query_record["equivalent_terms"] == ["技能"]
        assert query_record["candidate_equivalent_terms"] == []
    finally:
        manager.close()


def test_three_distinct_negative_searches_permanently_reject_pair(
    tmp_path: Path,
) -> None:
    manager, _, docs, directory_id = _manager_with_document(tmp_path)
    try:
        search_ids: set[str] = set()
        for expected_count in (1, 2, 3):
            execution = manager.search_detailed(
                keywords=["招式系统"], directory=docs
            )
            assert len(execution["review_requests"]) == 1
            search_ids.add(execution["search_id"])
            feedback = manager.submit_synonym_feedback(
                search_id=execution["search_id"],
                directory_id=directory_id,
                query_term="招式系统",
                candidate_term="技能",
                verdict="not_equivalent",
            )
            duplicate = manager.submit_synonym_feedback(
                search_id=execution["search_id"],
                directory_id=directory_id,
                query_term="招式系统",
                candidate_term="技能",
                verdict="not_equivalent",
            )
            assert feedback["negative_count"] == expected_count
            assert duplicate["negative_count"] == expected_count
            assert duplicate["idempotent"] is True

        assert len(search_ids) == 3
        assert feedback["status"] == "rejected"
        records = manager.list_synonyms(directory=docs)
        query_record = next(
            record for record in records if record["term"] == "招式系统"
        )
        assert query_record["non_equivalent_terms"] == ["技能"]
        assert query_record["candidate_equivalent_terms"] == []
        later = manager.search_detailed(keywords=["招式系统"], directory=docs)
        assert later["review_requests"] == []
        assert later["expansions"] == []
        assert later["results"] == []
    finally:
        manager.close()


def test_non_equivalent_pair_is_not_readded_by_later_index_build(
    tmp_path: Path,
) -> None:
    manager, _, docs, directory_id = _manager_with_document(tmp_path)
    try:
        for _ in range(3):
            execution = manager.search_detailed(
                keywords=["角色技能"], directory=docs
            )
            review = execution["review_requests"][0]
            assert {review["query_term"], review["candidate_term"]} == {
                "角色",
                "技能",
            }
            manager.submit_synonym_feedback(
                search_id=execution["search_id"],
                directory_id=directory_id,
                query_term=review["query_term"],
                candidate_term=review["candidate_term"],
                verdict="not_equivalent",
            )

        manager.index_directory(docs, force=True)
        role = next(
            record
            for record in manager.list_synonyms(directory=docs)
            if record["term"] == "角色"
        )
        assert role["candidate_equivalent_terms"] == []
        assert role["non_equivalent_terms"] == ["技能"]
        later = manager.search_detailed(keywords=["角色技能"], directory=docs)
        assert all(
            {item["query_term"], item["candidate_term"]} != {"角色", "技能"}
            for item in later["review_requests"]
        )
    finally:
        manager.close()


def test_unsure_does_not_increment_negative_count(tmp_path: Path) -> None:
    manager, _, docs, directory_id = _manager_with_document(tmp_path)
    try:
        execution = manager.search_detailed(keywords=["招式系统"], directory=docs)
        feedback = manager.submit_synonym_feedback(
            search_id=execution["search_id"],
            directory_id=directory_id,
            query_term="招式系统",
            candidate_term="技能",
            verdict="unsure",
        )

        assert feedback["status"] == "pending"
        assert feedback["negative_count"] == 0
    finally:
        manager.close()


def test_vector_query_failure_degrades_to_exact_bm25(tmp_path: Path) -> None:
    manager, vectors, docs, _ = _manager_with_document(tmp_path)
    try:
        vectors.fail_queries = True
        execution = manager.search_detailed(
            keywords=["招式系统", "角色技能"], directory=docs
        )

        assert "term_vector_query_failed" in execution["warnings"]
        assert execution["results"][0].chunk_key == "docs/doc.md"
        assert all(
            match["source"] == "exact"
            for match in execution["results"][0].matches
        )
    finally:
        manager.close()


def test_http_keyword_search_and_feedback_contract(tmp_path: Path) -> None:
    manager, _, docs, directory_id = _manager_with_document(tmp_path)
    app = create_app(
        manager.config,
        process_cwd=tmp_path,
        manager=manager,
        enable_watcher=False,
    )
    try:
        with TestClient(app) as client:
            response = client.post(
                "/v1/search",
                json={"keywords": ["招式系统"], "directory": str(docs)},
            )
            assert response.status_code == 200
            payload = response.json()
            assert payload["keywords"] == ["招式系统"]
            assert payload["results"][0]["body"].startswith("# 战斗配置")
            review = payload["review_requests"][0]

            feedback = client.post(
                "/v1/synonym-feedback",
                json={
                    "search_id": payload["search_id"],
                    "directory_id": directory_id,
                    "query_term": review["query_term"],
                    "candidate_term": review["candidate_term"],
                    "verdict": "equivalent",
                },
            )
            assert feedback.status_code == 200
            assert feedback.json()["status"] == "accepted"
            relations = client.get(
                "/v1/synonyms", params={"directory": str(docs)}
            )
            records = relations.json()["terms"]
            reviewed = next(
                record
                for record in records
                if record["term"] == review["query_term"]
            )
            assert review["candidate_term"] in reviewed["equivalent_terms"]
            term_table = client.get(
                "/v1/equivalence-terms", params={"directory": str(docs)}
            )
            assert term_table.status_code == 200
            assert term_table.json()["terms"] == records
    finally:
        manager.close()
