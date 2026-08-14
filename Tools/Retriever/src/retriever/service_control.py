from __future__ import annotations

import json
import os
import subprocess
import sys
import time
from pathlib import Path
from typing import Any

import httpx

from retriever.config import AppConfig


def ensure_config(config_path: str | Path) -> tuple[Path, AppConfig]:
    path = Path(config_path).resolve()
    if path.exists():
        return path, AppConfig.load(path)
    config = AppConfig()
    config.save(path)
    return path, config


def base_url(config: AppConfig) -> str:
    return f"http://{config.service.host}:{config.service.port}"


def request_json(
    config: AppConfig,
    method: str,
    path: str,
    **kwargs: Any,
) -> dict[str, Any]:
    try:
        response = httpx.request(
            method,
            f"{base_url(config)}{path}",
            timeout=kwargs.pop("timeout", 120.0),
            **kwargs,
        )
        response.raise_for_status()
    except httpx.HTTPStatusError as error:
        try:
            detail = error.response.json().get("detail", error.response.text)
        except ValueError:
            detail = error.response.text
        raise RuntimeError(f"Retriever API error: {detail}") from error
    except httpx.HTTPError as error:
        raise RuntimeError(
            f"Retriever service is unavailable at {base_url(config)}"
        ) from error
    value = response.json()
    if not isinstance(value, dict):
        raise RuntimeError("Retriever API returned a non-object response")
    return value


def is_running(config: AppConfig) -> bool:
    try:
        response = httpx.get(f"{base_url(config)}/health", timeout=1.0)
        return response.status_code == 200
    except httpx.HTTPError:
        return False


def start_service(
    config_path: str | Path,
    *,
    wait_seconds: float = 120.0,
) -> dict[str, object]:
    resolved_config, config = ensure_config(config_path)
    if is_running(config):
        return {"status": "already-running", "url": base_url(config)}

    log_dir = config.resolve_log_dir(Path.cwd())
    log_dir.mkdir(parents=True, exist_ok=True)
    stdout_path = log_dir / "service-stdout.log"
    stderr_path = log_dir / "service-stderr.log"
    if getattr(sys, "frozen", False):
        command = [
            sys.executable,
            "_service-runner",
            "--config",
            str(resolved_config),
        ]
    else:
        command = [
            sys.executable,
            "-m",
            "retriever.service_runner",
            "--config",
            str(resolved_config),
        ]
    creation_flags = 0
    if os.name == "nt":
        creation_flags = (
            subprocess.CREATE_NEW_PROCESS_GROUP
            | subprocess.DETACHED_PROCESS
            | subprocess.CREATE_NO_WINDOW
        )
    with stdout_path.open("ab") as stdout, stderr_path.open("ab") as stderr:
        process = subprocess.Popen(
            command,
            cwd=Path.cwd(),
            stdin=subprocess.DEVNULL,
            stdout=stdout,
            stderr=stderr,
            creationflags=creation_flags,
            close_fds=True,
        )

    deadline = time.monotonic() + wait_seconds
    while time.monotonic() < deadline:
        if is_running(config):
            return {
                "status": "started",
                "pid": process.pid,
                "url": base_url(config),
            }
        if process.poll() is not None:
            raise RuntimeError(
                f"Retriever service exited with code {process.returncode}; "
                f"see {stderr_path}"
            )
        time.sleep(0.25)
    raise RuntimeError(
        f"Retriever service did not become ready in {wait_seconds:.0f}s; "
        f"see {stderr_path}"
    )


def stop_service(config_path: str | Path) -> dict[str, object]:
    _, config = ensure_config(config_path)
    if not is_running(config):
        return {"status": "not-running"}
    response = request_json(config, "POST", "/v1/service/shutdown", timeout=10.0)
    deadline = time.monotonic() + 30
    while time.monotonic() < deadline:
        if not is_running(config):
            return {"status": "stopped"}
        time.sleep(0.25)
    raise RuntimeError(f"Retriever service did not stop: {json.dumps(response)}")
