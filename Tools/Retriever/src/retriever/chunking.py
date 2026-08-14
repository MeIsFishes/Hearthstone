from __future__ import annotations

import re
from pathlib import Path

from retriever.config import ChunkConfig
from retriever.models import Chunk


_ENGLISH_WORD_CHAR_RE = re.compile(r"[A-Za-z0-9_'-]")


def _is_word_char(character: str) -> bool:
    return bool(character and _ENGLISH_WORD_CHAR_RE.fullmatch(character))


def _adjust_end_for_word(text: str, start: int, end: int) -> int:
    if end >= len(text) or end <= start:
        return end
    if not (_is_word_char(text[end - 1]) and _is_word_char(text[end])):
        return end
    adjusted = end
    while adjusted > start and _is_word_char(text[adjusted - 1]):
        adjusted -= 1
    if adjusted > start:
        return adjusted
    adjusted = end
    while adjusted < len(text) and _is_word_char(text[adjusted]):
        adjusted += 1
    return adjusted


def _adjust_overlap_start(text: str, start: int, desired: int) -> int:
    if desired <= start or desired >= len(text):
        return desired
    if not (_is_word_char(text[desired - 1]) and _is_word_char(text[desired])):
        return desired
    adjusted = desired
    while adjusted > start and _is_word_char(text[adjusted - 1]):
        adjusted -= 1
    return adjusted


def split_body(body: str, config: ChunkConfig) -> list[str]:
    if len(body) <= config.max_chars:
        return [body]

    pieces: list[str] = []
    start = 0
    while start < len(body):
        target_end = min(start + config.max_chars, len(body))
        end = (
            _adjust_end_for_word(body, start, target_end)
            if config.preserve_english_words
            else target_end
        )
        if end <= start:
            end = min(start + config.max_chars, len(body))
        pieces.append(body[start:end])
        if end >= len(body):
            break

        desired_start = max(start + 1, end - config.overlap_chars)
        next_start = (
            _adjust_overlap_start(body, start, desired_start)
            if config.preserve_english_words
            else desired_start
        )
        if next_start <= start:
            next_start = end
        start = next_start
    return pieces


def parse_markdown(
    markdown: str,
    *,
    relative_path: str,
    document_name: str | None = None,
    config: ChunkConfig | None = None,
) -> list[Chunk]:
    # ``config`` remains accepted so existing callers and configuration files do
    # not break, but the index unit is deliberately the complete document.
    _ = config
    name = document_name or Path(relative_path).name
    return [
        Chunk(
            chunk_key=relative_path,
            title=name,
            body=markdown,
            heading_path=(),
            split_number=None,
            ordinal=0,
        )
    ]
