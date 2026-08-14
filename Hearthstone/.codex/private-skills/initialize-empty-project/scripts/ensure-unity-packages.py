#!/usr/bin/env python3
"""Add the BbxCommon baseline Unity packages to Packages/manifest.json."""

from __future__ import annotations

import argparse
import json
import os
import sys
from pathlib import Path


SKILL_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_REQUIREMENTS = SKILL_ROOT / "assets" / "required-unity-packages.json"


def load_json(path: Path) -> dict:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as error:
        raise ValueError(f"File not found: {path}") from error
    except json.JSONDecodeError as error:
        raise ValueError(f"Invalid JSON in {path}: {error}") from error

    if not isinstance(value, dict):
        raise ValueError(f"JSON root must be an object: {path}")
    return value


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description=(
            "Add missing BbxCommon baseline packages without overwriting existing "
            "version choices. Unity Package Manager will update packages-lock.json."
        )
    )
    parser.add_argument("--project-root", default=".", help="Unity project root")
    parser.add_argument(
        "--requirements",
        default=str(DEFAULT_REQUIREMENTS),
        help="JSON file containing a dependencies object",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Report changes without writing manifest.json",
    )
    return parser


def main() -> int:
    args = build_parser().parse_args()
    project_root = Path(args.project_root).resolve()
    manifest_path = project_root / "Packages" / "manifest.json"
    requirements_path = Path(args.requirements).resolve()

    try:
        manifest = load_json(manifest_path)
        requirements = load_json(requirements_path)
    except ValueError as error:
        print(json.dumps({"status": "error", "message": str(error)}, ensure_ascii=False))
        return 1

    dependencies = manifest.get("dependencies")
    required_dependencies = requirements.get("dependencies")
    if not isinstance(dependencies, dict):
        print(
            json.dumps(
                {
                    "status": "error",
                    "message": f"dependencies must be an object: {manifest_path}",
                },
                ensure_ascii=False,
            )
        )
        return 1
    if not isinstance(required_dependencies, dict) or not required_dependencies:
        print(
            json.dumps(
                {
                    "status": "error",
                    "message": f"requirements dependencies must be a non-empty object: {requirements_path}",
                },
                ensure_ascii=False,
            )
        )
        return 1

    conflicts = []
    missing = []
    present = []
    for package_name, required_version in required_dependencies.items():
        current_version = dependencies.get(package_name)
        if current_version is None:
            missing.append({"name": package_name, "version": required_version})
        elif current_version == required_version:
            present.append({"name": package_name, "version": current_version})
        else:
            conflicts.append(
                {
                    "name": package_name,
                    "required": required_version,
                    "existing": current_version,
                }
            )

    if conflicts:
        print(
            json.dumps(
                {
                    "status": "version-conflict",
                    "manifest": str(manifest_path),
                    "conflicts": conflicts,
                    "message": "Resolve package versions before initialization; existing versions were not overwritten.",
                },
                ensure_ascii=False,
                indent=2,
            )
        )
        return 2

    for package in missing:
        dependencies[package["name"]] = package["version"]

    if missing and not args.dry_run:
        temporary_path = manifest_path.with_name(manifest_path.name + ".codex.tmp")
        temporary_path.write_text(
            json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )
        os.replace(temporary_path, manifest_path)

    print(
        json.dumps(
            {
                "status": "dry-run" if args.dry_run else "ok",
                "manifest": str(manifest_path),
                "added": missing,
                "alreadyPresent": present,
                "packagesLockUpdatedByUnity": True,
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())

