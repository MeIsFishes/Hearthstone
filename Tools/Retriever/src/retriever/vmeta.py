from __future__ import annotations

import json
import os
import tempfile
from dataclasses import dataclass
from pathlib import Path
from typing import Any


@dataclass(frozen=True, slots=True)
class VMeta:
    source_path: str
    modified_at_ns: int
    index_revision: int
    split_algorithm_version: int = 1
    index_format_fingerprint: str | None = None

    @classmethod
    def from_dict(cls, value: dict[str, Any]) -> "VMeta":
        return cls(
            source_path=str(value["source_path"]),
            modified_at_ns=int(value["modified_at_ns"]),
            index_revision=int(value["index_revision"]),
            split_algorithm_version=int(value.get("split_algorithm_version", 1)),
            index_format_fingerprint=(
                str(value["index_format_fingerprint"])
                if value.get("index_format_fingerprint") is not None
                else None
            ),
        )

    def as_dict(self) -> dict[str, Any]:
        return {
            "source_path": self.source_path,
            "modified_at_ns": self.modified_at_ns,
            "index_revision": self.index_revision,
            "split_algorithm_version": self.split_algorithm_version,
            "index_format_fingerprint": self.index_format_fingerprint,
        }


def vmeta_path(source_path: str | Path) -> Path:
    source = Path(source_path)
    return source.with_name(f"{source.name}.vmeta")


def read_vmeta(source_path: str | Path) -> VMeta | None:
    path = vmeta_path(source_path)
    try:
        with path.open("r", encoding="utf-8") as handle:
            return VMeta.from_dict(json.load(handle))
    except (FileNotFoundError, OSError, ValueError, KeyError, TypeError, json.JSONDecodeError):
        return None


def write_vmeta_atomic(source_path: str | Path, metadata: VMeta) -> Path:
    destination = vmeta_path(source_path)
    destination.parent.mkdir(parents=True, exist_ok=True)
    temporary_name: str | None = None
    try:
        with tempfile.NamedTemporaryFile(
            mode="w",
            encoding="utf-8",
            newline="\n",
            prefix=f".{destination.name}.",
            suffix=".tmp",
            dir=destination.parent,
            delete=False,
        ) as handle:
            temporary_name = handle.name
            json.dump(metadata.as_dict(), handle, ensure_ascii=False, indent=2)
            handle.write("\n")
            handle.flush()
            os.fsync(handle.fileno())
        os.replace(temporary_name, destination)
    finally:
        if temporary_name is not None:
            try:
                Path(temporary_name).unlink(missing_ok=True)
            except OSError:
                pass
    return destination


def delete_vmeta(source_path: str | Path) -> None:
    vmeta_path(source_path).unlink(missing_ok=True)
