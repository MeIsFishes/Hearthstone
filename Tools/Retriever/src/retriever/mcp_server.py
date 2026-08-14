from __future__ import annotations

from pathlib import Path
from typing import Any

from mcp.server.fastmcp import FastMCP

from retriever.service_control import (
    ensure_config,
    is_running,
    request_json,
    start_service,
)


def run_mcp(config_path: str | Path) -> None:
    resolved_config, config = ensure_config(config_path)
    if not is_running(config):
        if not config.service.auto_start_for_mcp:
            raise RuntimeError("Retriever service is not running")
        start_service(resolved_config)

    mcp = FastMCP(
        "Retriever",
        instructions=(
            "Search indexed local Markdown documents with keywords. "
            "After search_documents returns review_requests, judge retrieval "
            "equivalence and call submit_synonym_feedback for each decision."
        ),
        log_level=config.logging.level.upper(),
    )

    @mcp.tool()
    def search_documents(
        query: str | None = None,
        keywords: list[str] | None = None,
        k: int = 3,
        directory: str | None = None,
    ) -> dict[str, Any]:
        """BM25-search complete documents; prefer explicit keyword phrases."""
        return request_json(
            config,
            "POST",
            "/v1/search",
            json={
                "query": query,
                "keywords": keywords or [],
                "k": k,
                "directory": directory,
            },
        )

    @mcp.tool()
    def submit_synonym_feedback(
        search_id: str,
        directory_id: int,
        query_term: str,
        candidate_term: str,
        verdict: str,
    ) -> dict[str, Any]:
        """Submit equivalent, not_equivalent, or unsure for a review request."""
        return request_json(
            config,
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

    mcp.run(transport="stdio")
