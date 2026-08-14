from __future__ import annotations

from collections.abc import Mapping, Sequence

from retriever.config import SearchConfig
from retriever.models import IndexedChunk, SearchChannel, SearchResult
from retriever.search.bm25 import BM25Search


ChunkRef = tuple[str, str]


class KeywordSearch:
    def __init__(
        self,
        bm25: BM25Search,
        config: SearchConfig,
    ) -> None:
        self.bm25 = bm25
        self.config = config

    def close(self) -> None:
        self.bm25.close()

    def index_chunks(self, directory_id: int, chunks: Sequence[IndexedChunk]) -> None:
        if not chunks:
            return
        self.bm25.upsert(directory_id, chunks)

    def delete_refs(self, references: Sequence[ChunkRef]) -> None:
        self.bm25.delete_refs(references)

    def clear(self, directory_ids: Sequence[int] | None = None) -> None:
        self.bm25.clear(directory_ids)

    def validate_refs(self, references: Sequence[ChunkRef]) -> None:
        if not self.bm25.contains_refs(references):
            raise RuntimeError("BM25 generation is incomplete")

    def search(
        self,
        query: str | None,
        active_chunks: Mapping[ChunkRef, object],
        *,
        keywords: Sequence[str] | None = None,
        k: int | None = None,
        directory_ids: Sequence[int] | None = None,
    ) -> list[SearchResult]:
        normalized_keywords = [
            value.strip() for value in (keywords or []) if value.strip()
        ]
        search_text = " ".join(dict.fromkeys(normalized_keywords))
        if not search_text:
            search_text = (query or "").strip()
        if not search_text or not active_chunks:
            return []
        scope = tuple(directory_ids or ())
        channels = [
            SearchChannel(
                original_keyword=keyword,
                term=keyword,
                source="exact",
                weight=1.0,
                directory_ids=scope,
            )
            for keyword in (normalized_keywords or [search_text])
        ]
        return self.search_channels(channels, active_chunks, k=k)

    def search_channels(
        self,
        channels: Sequence[SearchChannel],
        active_chunks: Mapping[ChunkRef, object],
        *,
        k: int | None = None,
    ) -> list[SearchResult]:
        if not channels or not active_chunks:
            return []
        result_count = k or self.config.default_k
        candidate_limit = max(self.config.candidates_per_channel, result_count)
        by_reference: dict[ChunkRef, SearchResult] = {}
        deduplicated: dict[tuple[str, str, tuple[int, ...]], SearchChannel] = {}
        for channel in channels:
            key = (channel.term, channel.source, channel.directory_ids)
            previous = deduplicated.get(key)
            if previous is None or channel.weight > previous.weight:
                deduplicated[key] = channel

        for channel in deduplicated.values():
            scope = list(channel.directory_ids) or None
            title_recall, body_recall = self.bm25.recall(
                channel.term,
                active_chunks,
                candidate_limit,
                scope,
            )
            candidate_refs = set(title_recall) | set(body_recall)
            if not candidate_refs:
                continue
            title_scores, body_scores = self.bm25.score_refs(
                channel.term,
                candidate_refs,
                scope,
            )
            for reference in candidate_refs:
                row = active_chunks.get(reference)
                if row is None:
                    continue
                title_raw = title_scores.get(reference, 0.0)
                body_raw = body_scores.get(reference, 0.0)
                title_relevance = -title_raw
                body_relevance = -body_raw
                unweighted = (
                    self.config.bm25_title_weight * title_relevance
                    + self.config.bm25_body_weight * body_relevance
                )
                contribution = self.config.bm25_weight * channel.weight * unweighted
                result = by_reference.setdefault(
                    reference,
                    SearchResult(
                        chunk_key=reference[0],
                        source_path=str(row["relative_path"]),
                        title=str(row["title"]),
                        body=str(row["body"]),
                        scores={
                            "bm25_title_raw": 0.0,
                            "bm25_body_raw": 0.0,
                            "bm25_title_relevance": 0.0,
                            "bm25_body_relevance": 0.0,
                            "bm25_exact": 0.0,
                            "bm25_accepted": 0.0,
                            "bm25_provisional": 0.0,
                            "bm25": 0.0,
                            "final": 0.0,
                        },
                    ),
                )
                result.scores["bm25_title_raw"] += title_raw
                result.scores["bm25_body_raw"] += body_raw
                result.scores["bm25_title_relevance"] += title_relevance
                result.scores["bm25_body_relevance"] += body_relevance
                bucket = f"bm25_{channel.source}"
                result.scores[bucket] = result.scores.get(bucket, 0.0) + contribution
                result.scores["bm25"] += contribution
                result.scores["final"] += contribution
                result.matches.append(
                    {
                        "original_keyword": channel.original_keyword,
                        "term": channel.term,
                        "source": channel.source,
                        "weight": channel.weight,
                        "similarity": channel.similarity,
                        "bm25": unweighted,
                        "contribution": contribution,
                    }
                )
        results = list(by_reference.values())
        results.sort(key=lambda item: (-item.scores["final"], item.chunk_key))
        return results[:result_count]


# Backward-compatible import name while vector retrieval is disabled.
HybridSearch = KeywordSearch
