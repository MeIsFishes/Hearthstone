from __future__ import annotations

import asyncio
import json
from pathlib import Path
from typing import Any

import typer

from retriever.config import AppConfig
from retriever.mcp_server import run_mcp
from retriever.service_control import (
    ensure_config,
    is_running,
    request_json,
    start_service,
    stop_service,
)
from retriever.service_runner import run_service


app = typer.Typer(no_args_is_help=True, help="Local Markdown keyword retriever")
service_app = typer.Typer(no_args_is_help=True, help="Manage the background service")
index_app = typer.Typer(no_args_is_help=True, help="Build and maintain indexes")
config_app = typer.Typer(no_args_is_help=True, help="Manage configuration")
synonym_app = typer.Typer(no_args_is_help=True, help="Manage retrieval synonyms")
app.add_typer(service_app, name="service")
app.add_typer(index_app, name="index")
app.add_typer(config_app, name="config")
app.add_typer(synonym_app, name="synonym")


def _emit(value: Any) -> None:
    typer.echo(json.dumps(value, ensure_ascii=False, indent=2))


def _load_config(path: Path) -> AppConfig:
    _, config = ensure_config(path)
    return config


def _fail(error: Exception) -> None:
    typer.echo(f"error: {error}", err=True)
    raise typer.Exit(code=1)


@config_app.command("init")
def config_init(
    config: Path = typer.Option(Path("retriever.json"), "--config"),
    force: bool = typer.Option(False, "--force"),
) -> None:
    """Create a default JSON configuration."""
    try:
        path = config.resolve()
        if path.exists() and not force:
            raise RuntimeError(f"Configuration already exists: {path}")
        AppConfig().save(path)
        _emit({"status": "created", "config": str(path)})
    except Exception as error:
        _fail(error)


@service_app.command("start")
def service_start(
    config: Path = typer.Option(Path("retriever.json"), "--config"),
) -> None:
    """Start the hidden local HTTP service."""
    try:
        _emit(start_service(config))
    except Exception as error:
        _fail(error)


@service_app.command("stop")
def service_stop(
    config: Path = typer.Option(Path("retriever.json"), "--config"),
) -> None:
    """Stop the local HTTP service."""
    try:
        _emit(stop_service(config))
    except Exception as error:
        _fail(error)


@service_app.command("status")
def service_status(
    config: Path = typer.Option(Path("retriever.json"), "--config"),
) -> None:
    """Show service and index status."""
    try:
        loaded = _load_config(config)
        if not is_running(loaded):
            _emit({"status": "not-running"})
            raise typer.Exit(code=1)
        value = request_json(loaded, "GET", "/v1/status")
        value["status"] = "running"
        _emit(value)
    except typer.Exit:
        raise
    except Exception as error:
        _fail(error)


@index_app.command("build")
def index_build(
    directory: Path | None = typer.Option(None, "--directory", "-d"),
    force: bool = typer.Option(False, "--force"),
    continue_on_error: bool = typer.Option(False, "--continue-on-error"),
    config: Path = typer.Option(Path("retriever.json"), "--config"),
) -> None:
    """Build or incrementally update one directory or all registered directories."""
    try:
        loaded = _load_config(config)
        _emit(
            request_json(
                loaded,
                "POST",
                "/v1/index/build",
                json={
                    "directory": str(directory.resolve()) if directory else None,
                    "force": force,
                    "continue_on_error": continue_on_error,
                },
                timeout=3600,
            )
        )
    except Exception as error:
        _fail(error)


@index_app.command("rebuild")
def index_rebuild(
    directory: Path | None = typer.Option(None, "--directory", "-d"),
    all_indexes: bool = typer.Option(False, "--all"),
    config: Path = typer.Option(Path("retriever.json"), "--config"),
) -> None:
    """Clear and rebuild one directory or all registered directories."""
    try:
        if directory is None and not all_indexes:
            raise RuntimeError("Specify --directory or --all")
        loaded = _load_config(config)
        _emit(
            request_json(
                loaded,
                "POST",
                "/v1/index/rebuild",
                json={"directory": str(directory.resolve()) if directory else None},
                timeout=3600,
            )
        )
    except Exception as error:
        _fail(error)


@index_app.command("clear")
def index_clear(
    directory: Path | None = typer.Option(None, "--directory", "-d"),
    all_indexes: bool = typer.Option(False, "--all"),
    config: Path = typer.Option(Path("retriever.json"), "--config"),
) -> None:
    """Clear one directory or all indexes."""
    try:
        if directory is None and not all_indexes:
            raise RuntimeError("Specify --directory or --all")
        loaded = _load_config(config)
        _emit(
            request_json(
                loaded,
                "DELETE",
                "/v1/index",
                params={
                    "directory": str(directory.resolve()) if directory else None,
                    "all": str(all_indexes).lower(),
                },
                timeout=3600,
            )
        )
    except Exception as error:
        _fail(error)


@app.command("search")
def search(
    query: str | None = typer.Argument(None),
    keyword: list[str] | None = typer.Option(None, "--keyword", "-w"),
    k: int = typer.Option(3, "--k", min=1),
    directory: Path | None = typer.Option(None, "--directory", "-d"),
    config: Path = typer.Option(Path("retriever.json"), "--config"),
) -> None:
    """Search complete documents; repeat --keyword for structured input."""
    try:
        if not (query or "").strip() and not keyword:
            raise RuntimeError("Provide QUERY or at least one --keyword")
        loaded = _load_config(config)
        _emit(
            request_json(
                loaded,
                "POST",
                "/v1/search",
                json={
                    "query": query,
                    "keywords": keyword or [],
                    "k": k,
                    "directory": str(directory.resolve()) if directory else None,
                },
            )
        )
    except Exception as error:
        _fail(error)


@synonym_app.command("list")
def synonym_list(
    directory: Path | None = typer.Option(None, "--directory", "-d"),
    status: str | None = typer.Option(None, "--status"),
    config: Path = typer.Option(Path("retriever.json"), "--config"),
) -> None:
    """List each term's equivalent, candidate, and non-equivalent lists."""
    try:
        loaded = _load_config(config)
        _emit(
            request_json(
                loaded,
                "GET",
                "/v1/synonyms",
                params={
                    "directory": str(directory.resolve()) if directory else None,
                    "status": status,
                },
            )
        )
    except Exception as error:
        _fail(error)


@synonym_app.command("feedback")
def synonym_feedback(
    search_id: str = typer.Option(..., "--search-id"),
    directory_id: int = typer.Option(..., "--directory-id", min=1),
    query_term: str = typer.Option(..., "--query-term"),
    candidate_term: str = typer.Option(..., "--candidate-term"),
    verdict: str = typer.Option(..., "--verdict"),
    config: Path = typer.Option(Path("retriever.json"), "--config"),
) -> None:
    """Submit one Agent review decision."""
    try:
        loaded = _load_config(config)
        _emit(
            request_json(
                loaded,
                "POST",
                "/v1/synonym-feedback",
                json={
                    "search_id": search_id,
                    "directory_id": directory_id,
                    "query_term": query_term,
                    "candidate_term": candidate_term,
                    "verdict": verdict,
                },
            )
        )
    except Exception as error:
        _fail(error)


@synonym_app.command("reset")
def synonym_reset(
    directory_id: int = typer.Option(..., "--directory-id", min=1),
    first_term: str = typer.Option(..., "--first-term"),
    second_term: str = typer.Option(..., "--second-term"),
    config: Path = typer.Option(Path("retriever.json"), "--config"),
) -> None:
    """Remove one pair from all three lists and clear its feedback history."""
    try:
        loaded = _load_config(config)
        _emit(
            request_json(
                loaded,
                "DELETE",
                "/v1/synonyms",
                json={
                    "directory_id": directory_id,
                    "first_term": first_term,
                    "second_term": second_term,
                },
            )
        )
    except Exception as error:
        _fail(error)


@app.command("mcp")
def mcp(
    config: Path = typer.Option(Path("retriever.json"), "--config"),
) -> None:
    """Run the stdio MCP server exposing search and synonym feedback."""
    try:
        run_mcp(config)
    except Exception as error:
        _fail(error)


@app.command("_service-runner", hidden=True)
def service_runner_command(
    config: Path = typer.Option(..., "--config"),
) -> None:
    """Internal frozen-process service entrypoint."""
    asyncio.run(run_service(config.resolve()))


def main() -> None:
    app()
