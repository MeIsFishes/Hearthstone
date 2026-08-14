from __future__ import annotations

from pathlib import Path

import portalocker


class ServiceLock:
    def __init__(self, lock_path: str | Path) -> None:
        self.lock_path = Path(lock_path)
        self.lock_path.parent.mkdir(parents=True, exist_ok=True)
        self._lock = portalocker.Lock(
            str(self.lock_path),
            mode="a+",
            timeout=0,
            flags=portalocker.LOCK_EX | portalocker.LOCK_NB,
        )
        self._handle = None

    def acquire(self) -> None:
        try:
            self._handle = self._lock.acquire()
        except portalocker.exceptions.LockException as error:
            raise RuntimeError(
                f"Another Retriever service already owns {self.lock_path}"
            ) from error

    def release(self) -> None:
        if self._handle is not None:
            self._lock.release()
            self._handle = None

    def __enter__(self) -> "ServiceLock":
        self.acquire()
        return self

    def __exit__(self, *_: object) -> None:
        self.release()

