from __future__ import annotations

import logging
from pathlib import Path

from retriever.config import AppConfig


def configure_logging(config: AppConfig, process_cwd: Path | None = None) -> Path:
    log_dir = config.resolve_log_dir(process_cwd)
    log_dir.mkdir(parents=True, exist_ok=True)
    log_path = log_dir / "retriever.log"
    level = getattr(logging, config.logging.level.upper(), logging.INFO)
    root = logging.getLogger()
    root.setLevel(level)
    formatter = logging.Formatter(
        "%(asctime)s %(levelname)s %(name)s %(message)s",
        datefmt="%Y-%m-%dT%H:%M:%S",
    )

    if not any(
        isinstance(handler, logging.FileHandler)
        and Path(handler.baseFilename).resolve() == log_path.resolve()
        for handler in root.handlers
    ):
        file_handler = logging.FileHandler(log_path, encoding="utf-8")
        file_handler.setFormatter(formatter)
        root.addHandler(file_handler)
    return log_path

