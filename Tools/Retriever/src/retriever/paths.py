from __future__ import annotations

import fnmatch
import os
from pathlib import Path


class PathPolicyError(ValueError):
    """Raised when a path cannot be represented by the configured policy."""


def canonical_path(path: str | Path) -> Path:
    return Path(path).expanduser().resolve()


def path_identity(path: str | Path) -> str:
    return os.path.normcase(str(canonical_path(path)))


def relative_to_process(path: str | Path, process_cwd: str | Path) -> str:
    absolute = canonical_path(path)
    base = canonical_path(process_cwd)
    if absolute.drive.casefold() != base.drive.casefold():
        raise PathPolicyError(
            f"Document and process working directory must be on the same volume: "
            f"{absolute} vs {base}"
        )
    return os.path.relpath(absolute, base).replace("\\", "/")


def roots_overlap(left: str | Path, right: str | Path) -> bool:
    left_id = path_identity(left)
    right_id = path_identity(right)
    try:
        common = os.path.commonpath([left_id, right_id])
    except ValueError:
        return False
    return common == left_id or common == right_id


def matches_any_glob(relative_path: str, patterns: list[str]) -> bool:
    normalized = relative_path.replace("\\", "/")
    for pattern in patterns:
        normalized_pattern = pattern.replace("\\", "/")
        if fnmatch.fnmatch(normalized, normalized_pattern):
            return True
        if normalized_pattern.startswith("**/") and fnmatch.fnmatch(
            normalized, normalized_pattern[3:]
        ):
            return True
    return False


def discover_files(
    root: str | Path,
    *,
    include_extensions: list[str],
    exclude_globs: list[str],
) -> list[Path]:
    root_path = canonical_path(root)
    extensions = {extension.casefold() for extension in include_extensions}
    discovered: list[Path] = []
    for current_root, directory_names, file_names in os.walk(root_path):
        current = Path(current_root)
        relative_current = current.relative_to(root_path).as_posix()
        kept_directories: list[str] = []
        for directory_name in directory_names:
            relative = (
                f"{relative_current}/{directory_name}"
                if relative_current != "."
                else directory_name
            )
            if not matches_any_glob(f"{relative}/", exclude_globs):
                kept_directories.append(directory_name)
        directory_names[:] = kept_directories

        for file_name in file_names:
            candidate = current / file_name
            relative = candidate.relative_to(root_path).as_posix()
            if candidate.suffix.casefold() not in extensions:
                continue
            if matches_any_glob(relative, exclude_globs):
                continue
            discovered.append(candidate.resolve())
    return sorted(discovered, key=lambda item: os.path.normcase(str(item)))

