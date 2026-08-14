from __future__ import annotations

import os
from pathlib import Path

import pytest

from retriever.paths import PathPolicyError, relative_to_process, roots_overlap
from retriever.vmeta import VMeta, delete_vmeta, read_vmeta, vmeta_path, write_vmeta_atomic


def test_relative_path_uses_process_working_directory(tmp_path: Path) -> None:
    document = tmp_path / "docs" / "test.md"
    document.parent.mkdir()
    document.write_text("text", encoding="utf-8")

    assert relative_to_process(document, tmp_path) == "docs/test.md"


def test_cross_volume_is_rejected_on_windows(tmp_path: Path) -> None:
    if os.name != "nt":
        pytest.skip("Windows volume policy")
    other_drive = "C:" if tmp_path.drive.casefold() != "c:" else "D:"
    with pytest.raises(PathPolicyError):
        relative_to_process(Path(other_drive + "/doc.md"), tmp_path)


def test_root_overlap_detection(tmp_path: Path) -> None:
    parent = tmp_path / "docs"
    child = parent / "nested"
    child.mkdir(parents=True)

    assert roots_overlap(parent, child)


def test_vmeta_uses_document_md_vmeta_and_round_trips(tmp_path: Path) -> None:
    source = tmp_path / "document.md"
    source.write_text("body", encoding="utf-8")
    metadata = VMeta("document.md", source.stat().st_mtime_ns, 7)

    destination = write_vmeta_atomic(source, metadata)

    assert destination == tmp_path / "document.md.vmeta"
    assert vmeta_path(source).name == "document.md.vmeta"
    assert read_vmeta(source) == metadata
    delete_vmeta(source)
    assert not destination.exists()


def test_legacy_vmeta_defaults_to_previous_split_algorithm() -> None:
    metadata = VMeta.from_dict(
        {
            "source_path": "document.md",
            "modified_at_ns": 1,
            "index_revision": 2,
        }
    )

    assert metadata.split_algorithm_version == 1
