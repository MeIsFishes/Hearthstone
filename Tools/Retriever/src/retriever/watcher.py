from __future__ import annotations

import logging
import queue
import threading
from pathlib import Path

from watchdog.events import FileSystemEvent, FileSystemEventHandler
from watchdog.observers import Observer

from retriever.flows import batch_write_documents, change_document
from retriever.manager import RetrieverManager


LOGGER = logging.getLogger(__name__)


class IndexTaskQueue:
    def __init__(self, manager: RetrieverManager) -> None:
        self.manager = manager
        self._queue: queue.Queue[tuple[str, str] | None] = queue.Queue()
        self._pending: set[tuple[str, str]] = set()
        self._pending_lock = threading.Lock()
        self._worker = threading.Thread(
            target=self._run, name="retriever-index-worker", daemon=True
        )
        self._started = False

    def start(self) -> None:
        if not self._started:
            self._started = True
            self._worker.start()

    def stop(self) -> None:
        if self._started:
            self._queue.put(None)
            self._worker.join(timeout=30)
            self._started = False

    def submit_path(self, path: str | Path) -> bool:
        return self._submit(("path", str(Path(path).resolve())))

    def submit_scan(self, directory: str | Path) -> bool:
        return self._submit(("scan", str(Path(directory).resolve())))

    def _submit(self, task: tuple[str, str]) -> bool:
        with self._pending_lock:
            if task in self._pending:
                return False
            self._pending.add(task)
        self._queue.put(task)
        return True

    def _run(self) -> None:
        while True:
            task = self._queue.get()
            if task is None:
                self._queue.task_done()
                break
            with self._pending_lock:
                self._pending.discard(task)
            kind, target = task
            try:
                if kind == "scan":
                    batch_write_documents(
                        self.manager, target, continue_on_error=True
                    )
                else:
                    change_document(self.manager, target)
            except Exception:
                LOGGER.exception("Background index task failed: %s %s", kind, target)
            finally:
                self._queue.task_done()


class _MarkdownEventHandler(FileSystemEventHandler):
    def __init__(self, task_queue: IndexTaskQueue) -> None:
        self.task_queue = task_queue

    def on_any_event(self, event: FileSystemEvent) -> None:
        if event.is_directory or event.event_type not in {
            "created",
            "modified",
            "deleted",
            "moved",
        }:
            return
        source = Path(event.src_path)
        if source.suffix.casefold() == ".md":
            self.task_queue.submit_path(source)
        destination = getattr(event, "dest_path", None)
        if destination:
            target = Path(destination)
            if target.suffix.casefold() == ".md":
                self.task_queue.submit_path(target)


class DirectoryWatcher:
    def __init__(self, manager: RetrieverManager, task_queue: IndexTaskQueue) -> None:
        self.manager = manager
        self.task_queue = task_queue
        self._observer: Observer | None = None

    def start(self) -> None:
        self.stop()
        observer = Observer()
        handler = _MarkdownEventHandler(self.task_queue)
        for directory in self.manager.list_directories():
            observer.schedule(handler, directory.root_path, recursive=True)
            self.task_queue.submit_scan(directory.root_path)
        observer.start()
        self._observer = observer

    def refresh(self) -> None:
        self.start()

    def stop(self) -> None:
        if self._observer is not None:
            self._observer.stop()
            self._observer.join(timeout=10)
            self._observer = None
