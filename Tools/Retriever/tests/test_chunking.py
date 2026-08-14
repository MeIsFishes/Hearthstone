from __future__ import annotations

from retriever.chunking import parse_markdown, split_body
from retriever.config import ChunkConfig


def test_entire_markdown_is_kept_as_one_input_without_heading_parsing() -> None:
    markdown = (
        "preface\n"
        "# Parent\n"
        "parent body\n"
        "```md\n"
        "# not a heading\n"
        "```\n"
        "## Child\n"
        "child body\n"
        "## Child\n"
        "second child\n"
    )

    chunks = parse_markdown(markdown, relative_path="docs/example.md")

    assert len(chunks) == 1
    assert chunks[0].chunk_key == "docs/example.md"
    assert chunks[0].title == "example.md"
    assert chunks[0].body == markdown
    assert chunks[0].heading_path == ()


def test_chunk_settings_do_not_split_the_document() -> None:
    markdown = "# First\n12345\n## Second\n67890\n"
    config = ChunkConfig(
        max_chars=12,
        overlap_chars=0,
        preserve_english_words=False,
    )
    chunks = parse_markdown(markdown, relative_path="doc.md", config=config)

    assert "".join(chunk.body for chunk in chunks) == markdown
    assert [chunk.chunk_key for chunk in chunks] == ["doc.md"]
    assert all(chunk.title == "doc.md" for chunk in chunks)
    assert all(chunk.heading_path == () for chunk in chunks)
    assert chunks[0].split_number is None


def test_split_uses_overlap_without_cutting_english_words() -> None:
    config = ChunkConfig(max_chars=20, overlap_chars=5)
    body = "alpha bravo charlie delta echo foxtrot"

    pieces = split_body(body, config)

    assert len(pieces) >= 2
    assert all(len(piece) <= 20 for piece in pieces)
    for piece in pieces:
        assert not piece.startswith(("lpha", "ravo", "harlie", "elta", "cho", "oxtrot"))
        assert not piece.endswith(("alph", "brav", "charli", "delt", "ech", "foxtro"))
    for left, right in zip(pieces, pieces[1:], strict=False):
        assert any(right.startswith(left[-size:]) for size in range(5, min(len(left), 12) + 1))


def test_split_allows_single_extreme_word_to_exceed_limit() -> None:
    word = "a" * 30
    pieces = split_body(word + " tail", ChunkConfig(max_chars=20, overlap_chars=5))

    assert pieces[0] == word
    assert pieces[1].endswith(" tail")


def test_long_documents_keep_one_stable_document_key() -> None:
    config = ChunkConfig(max_chars=12, overlap_chars=3)
    markdown = "# Heading\none two three four five"

    first = parse_markdown(markdown, relative_path="doc.md", config=config)
    second = parse_markdown(markdown, relative_path="doc.md", config=config)

    assert [chunk.chunk_key for chunk in first] == [chunk.chunk_key for chunk in second]
    assert len(first) == 1
    assert first[0].chunk_key == "doc.md"
    assert first[0].body == markdown
