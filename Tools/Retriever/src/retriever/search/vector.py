from __future__ import annotations

import hashlib
import math
import os
import sys
import threading
from pathlib import Path
from typing import Protocol, Sequence


class Embedder(Protocol):
    @property
    def dimension(self) -> int: ...

    @property
    def version(self) -> str: ...

    def encode(self, texts: Sequence[str]) -> list[list[float]]: ...


class TermVectorIndex(Protocol):
    @property
    def embedding_fingerprint(self) -> str: ...

    def replace_directory(self, directory_id: int, terms: Sequence[str]) -> None: ...

    def nearest(
        self,
        text: str,
        directory_ids: Sequence[int],
        *,
        limit: int,
        similarity_threshold: float,
    ) -> list[dict[str, object]]: ...

    def candidate_pairs(
        self,
        directory_id: int,
        *,
        limit_per_term: int,
        similarity_threshold: float,
    ) -> list[dict[str, object]]: ...

    def clear(self, directory_ids: Sequence[int] | None = None) -> None: ...

    def count(self) -> int: ...


class SentenceTransformerEmbedder:
    def __init__(self, model_name: str, device: str = "cpu") -> None:
        self.model_name = model_name
        self.device = device
        self._model = None
        self._lock = threading.Lock()

    def _load(self):
        if self._model is None:
            with self._lock:
                if self._model is None:
                    from sentence_transformers import SentenceTransformer

                    self._model = SentenceTransformer(
                        self._model_source(), device=self.device
                    )
        return self._model

    def _model_source(self) -> str:
        override = os.environ.get("RETRIEVER_MODEL_DIR")
        if override:
            return override
        if getattr(sys, "frozen", False):
            bundle_root = Path(getattr(sys, "_MEIPASS", Path(sys.executable).parent))
            bundled = bundle_root / "models" / self.model_name.rsplit("/", 1)[-1]
            if bundled.is_dir():
                return str(bundled)
        return self.model_name

    @property
    def dimension(self) -> int:
        model = self._load()
        getter = getattr(model, "get_embedding_dimension", None)
        if getter is None:
            getter = model.get_sentence_embedding_dimension
        return int(getter())

    @property
    def version(self) -> str:
        return self.model_name

    def encode(self, texts: Sequence[str]) -> list[list[float]]:
        if not texts:
            return []
        values = self._load().encode(
            list(texts),
            normalize_embeddings=True,
            show_progress_bar=False,
        )
        return [[float(item) for item in vector] for vector in values]


class HashingEmbedder:
    """Deterministic embedder for tests; it is not a semantic production model."""

    def __init__(self, dimension: int = 64) -> None:
        self._dimension = dimension

    @property
    def dimension(self) -> int:
        return self._dimension

    @property
    def version(self) -> str:
        return f"hashing-{self.dimension}"

    def encode(self, texts: Sequence[str]) -> list[list[float]]:
        vectors: list[list[float]] = []
        for text in texts:
            vector = [0.0] * self.dimension
            for token in text.casefold().split():
                digest = hashlib.sha256(token.encode("utf-8")).digest()
                index = int.from_bytes(digest[:4], "little") % self.dimension
                vector[index] += -1.0 if digest[4] & 1 else 1.0
            if not any(vector):
                vector[0] = 1.0
            norm = math.sqrt(sum(value * value for value in vector))
            vectors.append([value / norm for value in vector])
        return vectors


class TermVectorSearch:
    """LanceDB-backed vectors for lexicon terms only, never documents."""

    TABLE_NAME = "term_vectors"

    def __init__(
        self,
        database_dir: str | Path,
        embedder: Embedder,
        *,
        batch_size: int = 64,
    ) -> None:
        import lancedb
        import pyarrow as pa

        self.database_dir = Path(database_dir)
        self.database_dir.mkdir(parents=True, exist_ok=True)
        self.embedder = embedder
        self.batch_size = batch_size
        self._lock = threading.RLock()
        self._database = lancedb.connect(str(self.database_dir))
        fingerprint_value = f"{embedder.version}:{embedder.dimension}:normalized"
        self._embedding_fingerprint = hashlib.sha256(
            fingerprint_value.encode("utf-8")
        ).hexdigest()
        schema = pa.schema(
            [
                pa.field("directory_id", pa.int64()),
                pa.field("term", pa.string()),
                pa.field("embedding_fingerprint", pa.string()),
                pa.field("vector", pa.list_(pa.float32(), embedder.dimension)),
            ]
        )
        existing = set(self._database.list_tables().tables)
        if self.TABLE_NAME not in existing:
            self._database.create_table(self.TABLE_NAME, schema=schema)

    @property
    def embedding_fingerprint(self) -> str:
        return self._embedding_fingerprint

    def replace_directory(self, directory_id: int, terms: Sequence[str]) -> None:
        unique_terms = list(dict.fromkeys(term.casefold() for term in terms if term))
        desired = set(unique_terms)
        with self._lock:
            table = self._database.open_table(self.TABLE_NAME)
            existing_rows = [
                row
                for row in table.to_arrow().to_pylist()
                if int(row["directory_id"]) == directory_id
                and str(row["embedding_fingerprint"])
                == self.embedding_fingerprint
            ]
        existing = {str(row["term"]) for row in existing_rows}
        missing_terms = [term for term in unique_terms if term not in existing]
        rows: list[dict[str, object]] = []
        for start in range(0, len(missing_terms), self.batch_size):
            batch = missing_terms[start : start + self.batch_size]
            vectors = self.embedder.encode(batch)
            rows.extend(
                {
                    "directory_id": directory_id,
                    "term": term,
                    "embedding_fingerprint": self.embedding_fingerprint,
                    "vector": vector,
                }
                for term, vector in zip(batch, vectors, strict=True)
            )
        with self._lock:
            table = self._database.open_table(self.TABLE_NAME)
            for removed in existing - desired:
                escaped = removed.replace("'", "''")
                table.delete(
                    f"directory_id = {int(directory_id)} AND term = '{escaped}'"
                )
            if rows:
                table.add(rows)

    def nearest(
        self,
        text: str,
        directory_ids: Sequence[int],
        *,
        limit: int,
        similarity_threshold: float,
    ) -> list[dict[str, object]]:
        if not text.strip() or not directory_ids:
            return []
        query_vector = self.embedder.encode([text])[0]
        values = ",".join(str(int(value)) for value in directory_ids)
        fingerprint = self.embedding_fingerprint.replace("'", "''")
        where = (
            f"directory_id IN ({values}) AND "
            f"embedding_fingerprint = '{fingerprint}'"
        )
        with self._lock:
            rows = (
                self._database.open_table(self.TABLE_NAME)
                .search(query_vector, vector_column_name="vector")
                .distance_type("cosine")
                .where(where, prefilter=True)
                .limit(max(limit, 1))
                .to_list()
            )
        result: list[dict[str, object]] = []
        for row in rows:
            similarity = 1.0 - float(row["_distance"])
            if similarity < similarity_threshold:
                continue
            result.append(
                {
                    "directory_id": int(row["directory_id"]),
                    "term": str(row["term"]),
                    "similarity": similarity,
                }
            )
        return result

    def candidate_pairs(
        self,
        directory_id: int,
        *,
        limit_per_term: int,
        similarity_threshold: float,
    ) -> list[dict[str, object]]:
        fingerprint = self.embedding_fingerprint.replace("'", "''")
        where = (
            f"directory_id = {int(directory_id)} AND "
            f"embedding_fingerprint = '{fingerprint}'"
        )
        with self._lock:
            table = self._database.open_table(self.TABLE_NAME)
            source_rows = [
                row
                for row in table.to_arrow().to_pylist()
                if int(row["directory_id"]) == directory_id
                and str(row["embedding_fingerprint"])
                == self.embedding_fingerprint
            ]
            pairs: dict[tuple[str, str], dict[str, object]] = {}
            for source in source_rows:
                rows = (
                    table.search(
                        list(source["vector"]),
                        vector_column_name="vector",
                    )
                    .distance_type("cosine")
                    .where(where, prefilter=True)
                    .limit(max(limit_per_term + 1, 2))
                    .to_list()
                )
                source_term = str(source["term"])
                accepted_for_source = 0
                for row in rows:
                    candidate_term = str(row["term"])
                    if candidate_term == source_term:
                        continue
                    similarity = 1.0 - float(row["_distance"])
                    if similarity < similarity_threshold:
                        continue
                    term_a, term_b = sorted((source_term, candidate_term))
                    key = (term_a, term_b)
                    previous = pairs.get(key)
                    if previous is None or similarity > float(previous["similarity"]):
                        pairs[key] = {
                            "directory_id": directory_id,
                            "term": term_a,
                            "candidate_term": term_b,
                            "similarity": similarity,
                        }
                    accepted_for_source += 1
                    if accepted_for_source >= limit_per_term:
                        break
        return list(pairs.values())

    def clear(self, directory_ids: Sequence[int] | None = None) -> None:
        with self._lock:
            table = self._database.open_table(self.TABLE_NAME)
            if directory_ids:
                values = ",".join(str(int(value)) for value in directory_ids)
                table.delete(f"directory_id IN ({values})")
            else:
                table.delete("true")

    def count(self) -> int:
        with self._lock:
            return int(self._database.open_table(self.TABLE_NAME).count_rows())


# Temporary import compatibility for callers of the old experimental class.
VectorSearch = TermVectorSearch
