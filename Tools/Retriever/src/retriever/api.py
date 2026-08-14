from __future__ import annotations

import asyncio
from contextlib import asynccontextmanager
from pathlib import Path
from typing import AsyncIterator

from fastapi import FastAPI, HTTPException, Request
from pydantic import BaseModel, Field, model_validator

from retriever.config import AppConfig
from retriever.flows import (
    batch_write_documents,
    rebuild_index,
    search_documents_detailed,
    submit_synonym_feedback,
)
from retriever.manager import IndexingError, RetrieverManager
from retriever.watcher import DirectoryWatcher, IndexTaskQueue


class SearchRequest(BaseModel):
    query: str | None = None
    keywords: list[str] = Field(default_factory=list)
    k: int | None = Field(default=None, ge=1)
    directory: str | None = None

    @model_validator(mode="after")
    def validate_search_input(self) -> "SearchRequest":
        if not (self.query or "").strip() and not any(
            value.strip() for value in self.keywords
        ):
            raise ValueError("Provide query or at least one keyword")
        return self


class SynonymFeedbackRequest(BaseModel):
    search_id: str = Field(min_length=1)
    directory_id: int = Field(ge=1)
    query_term: str = Field(min_length=1)
    candidate_term: str = Field(min_length=1)
    verdict: str


class SynonymResetRequest(BaseModel):
    directory_id: int = Field(ge=1)
    first_term: str = Field(min_length=1)
    second_term: str = Field(min_length=1)


class DirectoryRequest(BaseModel):
    directory: str


class IndexRequest(BaseModel):
    directory: str | None = None
    force: bool = False
    continue_on_error: bool = False


class RebuildRequest(BaseModel):
    directory: str | None = None


def create_app(
    config: AppConfig,
    *,
    process_cwd: str | Path | None = None,
    manager: RetrieverManager | None = None,
    enable_watcher: bool = True,
) -> FastAPI:
    owns_manager = manager is None

    @asynccontextmanager
    async def lifespan(app: FastAPI) -> AsyncIterator[None]:
        current_manager = manager or RetrieverManager(
            config, process_cwd=process_cwd
        )
        app.state.manager = current_manager
        task_queue: IndexTaskQueue | None = None
        watcher: DirectoryWatcher | None = None
        if enable_watcher:
            task_queue = IndexTaskQueue(current_manager)
            task_queue.start()
            watcher = DirectoryWatcher(current_manager, task_queue)
            watcher.start()
        app.state.task_queue = task_queue
        app.state.watcher = watcher
        try:
            yield
        finally:
            if watcher is not None:
                watcher.stop()
            if task_queue is not None:
                task_queue.stop()
            if owns_manager:
                current_manager.close()

    app = FastAPI(title="Retriever", version="0.2.0", lifespan=lifespan)

    @app.get("/health")
    async def health() -> dict[str, str]:
        return {"status": "ok"}

    @app.post("/v1/search")
    async def search(payload: SearchRequest, request: Request) -> dict[str, object]:
        try:
            execution = await asyncio.to_thread(
                search_documents_detailed,
                request.app.state.manager,
                payload.query,
                keywords=payload.keywords,
                k=payload.k,
                directory=payload.directory,
            )
        except (ValueError, RuntimeError) as error:
            raise HTTPException(status_code=400, detail=str(error)) from error
        return {
            **execution,
            "k": payload.k or config.search.default_k,
            "results": [item.as_dict() for item in execution["results"]],
        }

    @app.post("/v1/synonym-feedback")
    async def synonym_feedback(
        payload: SynonymFeedbackRequest, request: Request
    ) -> dict[str, object]:
        try:
            return await asyncio.to_thread(
                submit_synonym_feedback,
                request.app.state.manager,
                search_id=payload.search_id,
                directory_id=payload.directory_id,
                query_term=payload.query_term,
                candidate_term=payload.candidate_term,
                verdict=payload.verdict,
            )
        except (ValueError, RuntimeError) as error:
            raise HTTPException(status_code=400, detail=str(error)) from error

    @app.get("/v1/synonyms")
    async def list_synonyms(
        request: Request,
        directory: str | None = None,
        status: str | None = None,
    ) -> dict[str, object]:
        try:
            relations = await asyncio.to_thread(
                request.app.state.manager.list_synonyms,
                directory=directory,
                status=status,
            )
        except (ValueError, RuntimeError) as error:
            raise HTTPException(status_code=400, detail=str(error)) from error
        return {"terms": relations, "relations": relations}

    @app.get("/v1/equivalence-terms")
    async def list_equivalence_terms(
        request: Request,
        directory: str | None = None,
        category: str | None = None,
    ) -> dict[str, object]:
        try:
            terms = await asyncio.to_thread(
                request.app.state.manager.list_equivalence_terms,
                directory=directory,
                category=category,
            )
        except (ValueError, RuntimeError) as error:
            raise HTTPException(status_code=400, detail=str(error)) from error
        return {"terms": terms}

    @app.delete("/v1/synonyms")
    async def reset_synonym(
        payload: SynonymResetRequest, request: Request
    ) -> dict[str, object]:
        try:
            removed = await asyncio.to_thread(
                request.app.state.manager.reset_synonym,
                directory_id=payload.directory_id,
                first_term=payload.first_term,
                second_term=payload.second_term,
            )
        except (ValueError, RuntimeError) as error:
            raise HTTPException(status_code=400, detail=str(error)) from error
        return {"removed": removed}

    @app.post("/v1/directories")
    async def add_directory(
        payload: DirectoryRequest, request: Request
    ) -> dict[str, object]:
        try:
            record = await asyncio.to_thread(
                request.app.state.manager.add_directory, payload.directory
            )
            watcher: DirectoryWatcher | None = request.app.state.watcher
            if watcher is not None:
                await asyncio.to_thread(watcher.refresh)
        except (ValueError, RuntimeError) as error:
            raise HTTPException(status_code=400, detail=str(error)) from error
        return {
            "directory_id": record.directory_id,
            "root_path": record.root_path,
            "enabled": record.enabled,
        }

    @app.post("/v1/index/build")
    async def build_index(
        payload: IndexRequest, request: Request
    ) -> dict[str, object]:
        try:
            built = await asyncio.to_thread(
                batch_write_documents,
                request.app.state.manager,
                payload.directory,
                force=payload.force,
                continue_on_error=payload.continue_on_error,
            )
            results = [item.as_dict() for item in built]
        except IndexingError as error:
            raise HTTPException(status_code=500, detail=str(error)) from error
        except (ValueError, RuntimeError) as error:
            raise HTTPException(status_code=400, detail=str(error)) from error
        watcher: DirectoryWatcher | None = request.app.state.watcher
        if watcher is not None:
            await asyncio.to_thread(watcher.refresh)
        return {"results": results}

    @app.post("/v1/index/rebuild")
    async def rebuild_index(
        payload: RebuildRequest, request: Request
    ) -> dict[str, object]:
        try:
            results = await asyncio.to_thread(
                rebuild_index, request.app.state.manager, payload.directory
            )
        except IndexingError as error:
            raise HTTPException(status_code=500, detail=str(error)) from error
        except (ValueError, RuntimeError) as error:
            raise HTTPException(status_code=400, detail=str(error)) from error
        return {"results": [item.as_dict() for item in results]}

    @app.delete("/v1/index")
    async def clear_index(
        request: Request, directory: str | None = None, all: bool = False
    ) -> dict[str, int]:
        if directory is None and not all:
            raise HTTPException(
                status_code=400, detail="Specify directory or set all=true"
            )
        try:
            result = await asyncio.to_thread(
                request.app.state.manager.clear, None if all else directory
            )
            watcher: DirectoryWatcher | None = request.app.state.watcher
            if watcher is not None:
                await asyncio.to_thread(watcher.refresh)
            return result
        except (ValueError, RuntimeError) as error:
            raise HTTPException(status_code=400, detail=str(error)) from error

    @app.get("/v1/status")
    async def status(request: Request) -> dict[str, object]:
        return await asyncio.to_thread(request.app.state.manager.status)

    @app.post("/v1/service/shutdown")
    async def shutdown(request: Request) -> dict[str, str]:
        server = getattr(request.app.state, "server", None)
        if server is None:
            raise HTTPException(status_code=409, detail="Shutdown is not available")
        server.should_exit = True
        return {"status": "stopping"}

    return app
