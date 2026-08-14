from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass(frozen=True, slots=True)
class Chunk:
    chunk_key: str
    title: str
    body: str
    heading_path: tuple[str, ...]
    split_number: int | None
    ordinal: int


@dataclass(frozen=True, slots=True)
class IndexedChunk:
    chunk_key: str
    content_revision: str
    title: str
    body: str
    ordinal: int


@dataclass(frozen=True, slots=True)
class SearchChannel:
    original_keyword: str
    term: str
    source: str
    weight: float
    directory_ids: tuple[int, ...]
    similarity: float | None = None


@dataclass(slots=True)
class SearchResult:
    chunk_key: str
    source_path: str
    title: str
    body: str
    scores: dict[str, float] = field(default_factory=dict)
    matches: list[dict[str, Any]] = field(default_factory=list)

    def as_dict(self) -> dict[str, Any]:
        return {
            "chunk_key": self.chunk_key,
            "source_path": self.source_path,
            "title": self.title,
            "body": self.body,
            "scores": self.scores,
            "matches": self.matches,
        }
