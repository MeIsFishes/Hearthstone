from __future__ import annotations

import argparse
import asyncio
import json
import os
from pathlib import Path

import uvicorn

from retriever.api import create_app
from retriever.config import AppConfig
from retriever.process_lock import ServiceLock


def _write_runtime_file(path: Path, value: dict[str, object]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".tmp")
    with temporary.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(value, handle, ensure_ascii=False, indent=2)
        handle.write("\n")
        handle.flush()
        os.fsync(handle.fileno())
    os.replace(temporary, path)


async def run_service(config_path: Path) -> None:
    config = AppConfig.load(config_path)
    process_cwd = Path.cwd().resolve()
    storage_dir = config.resolve_storage_dir(process_cwd)
    runtime_dir = storage_dir / "runtime"
    runtime_dir.mkdir(parents=True, exist_ok=True)
    pid_path = runtime_dir / "service.json"
    with ServiceLock(runtime_dir / "service.lock"):
        _write_runtime_file(
            pid_path,
            {
                "pid": os.getpid(),
                "config": str(config_path),
                "process_cwd": str(process_cwd),
                "host": config.service.host,
                "port": config.service.port,
            },
        )
        app = create_app(config, process_cwd=process_cwd)
        server = uvicorn.Server(
            uvicorn.Config(
                app,
                host=config.service.host,
                port=config.service.port,
                log_level=config.logging.level.casefold(),
            )
        )
        app.state.server = server
        try:
            await server.serve()
        finally:
            pid_path.unlink(missing_ok=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", required=True)
    arguments = parser.parse_args()
    asyncio.run(run_service(Path(arguments.config).resolve()))


if __name__ == "__main__":
    main()

