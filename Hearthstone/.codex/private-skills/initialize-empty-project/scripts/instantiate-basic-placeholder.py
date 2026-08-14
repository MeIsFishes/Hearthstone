#!/usr/bin/env python3
"""Instantiate the reusable BbxCommon placeholder scripts into a Unity project."""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path


SKILL_ROOT = Path(__file__).resolve().parent.parent
TEMPLATE_ROOT = SKILL_ROOT / "assets" / "basic-placeholder"
IDENTIFIER = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")
NAMESPACE = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*$")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description=(
            "Copy the BbxCommon GameEngine, Stage, ECS, configuration and UI placeholder "
            "files into Assets/Scripts/<project-folder>. Existing differing files stop the operation."
        )
    )
    parser.add_argument("--project-root", default=".", help="Unity project root")
    parser.add_argument("--project-name", required=True, help="C# identifier used in type names")
    parser.add_argument("--namespace", required=True, help="C# root namespace")
    parser.add_argument(
        "--project-folder",
        help="Folder below Assets/Scripts; defaults to project-name",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Report target files without writing them",
    )
    return parser


def replace_tokens(value: str, project_name: str, namespace: str) -> str:
    return value.replace("__PROJECT_NAME__", project_name).replace(
        "__PROJECT_NAMESPACE__", namespace
    )


def main() -> int:
    args = build_parser().parse_args()
    project_name = args.project_name
    namespace = args.namespace
    project_folder = args.project_folder or project_name

    errors = []
    if not IDENTIFIER.fullmatch(project_name):
        errors.append("project-name must be a valid C# identifier")
    if not NAMESPACE.fullmatch(namespace):
        errors.append("namespace must contain valid dot-separated C# identifiers")
    if not IDENTIFIER.fullmatch(project_folder):
        errors.append("project-folder must be one folder name and a valid C# identifier")

    project_root = Path(args.project_root).resolve()
    if not (project_root / "Assets").is_dir():
        errors.append(f"Unity Assets directory not found: {project_root / 'Assets'}")
    if not (project_root / "Packages" / "manifest.json").is_file():
        errors.append(f"Unity manifest not found: {project_root / 'Packages' / 'manifest.json'}")
    if not TEMPLATE_ROOT.is_dir():
        errors.append(f"Template directory not found: {TEMPLATE_ROOT}")

    if errors:
        print(json.dumps({"status": "error", "errors": errors}, ensure_ascii=False, indent=2))
        return 1

    destination_root = project_root / "Assets" / "Scripts" / project_folder
    planned_files = []
    conflicts = []
    for source_path in sorted(path for path in TEMPLATE_ROOT.rglob("*") if path.is_file()):
        relative_text = replace_tokens(
            source_path.relative_to(TEMPLATE_ROOT).as_posix(), project_name, namespace
        )
        destination_path = destination_root / Path(relative_text)
        rendered_text = replace_tokens(
            source_path.read_text(encoding="utf-8"), project_name, namespace
        )

        file_status = "create"
        if destination_path.exists():
            existing_text = destination_path.read_text(encoding="utf-8")
            if existing_text == rendered_text:
                file_status = "unchanged"
            else:
                file_status = "conflict"
                conflicts.append(str(destination_path))

        planned_files.append(
            {
                "source": str(source_path),
                "destination": str(destination_path),
                "content": rendered_text,
                "status": file_status,
            }
        )

    if conflicts:
        print(
            json.dumps(
                {
                    "status": "conflict",
                    "conflicts": conflicts,
                    "message": "No files were written. Preserve existing files and resolve the conflicts first.",
                },
                ensure_ascii=False,
                indent=2,
            )
        )
        return 2

    created = []
    unchanged = []
    for planned_file in planned_files:
        destination_path = Path(planned_file["destination"])
        if planned_file["status"] == "unchanged":
            unchanged.append(str(destination_path))
            continue
        created.append(str(destination_path))
        if not args.dry_run:
            destination_path.parent.mkdir(parents=True, exist_ok=True)
            destination_path.write_text(planned_file["content"], encoding="utf-8")

    print(
        json.dumps(
            {
                "status": "dry-run" if args.dry_run else "ok",
                "destinationRoot": str(destination_root),
                "created": created,
                "unchanged": unchanged,
                "unityGeneratedFilesCreated": False,
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())

