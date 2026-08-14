from __future__ import annotations

import hashlib
import json
from pathlib import Path
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator


class ServiceConfig(BaseModel):
    host: str = "127.0.0.1"
    port: int = Field(default=8765, ge=1, le=65535)
    auto_start_for_mcp: bool = True


class IndexConfig(BaseModel):
    directories: list[str] = Field(default_factory=list)
    include_extensions: list[str] = Field(default_factory=lambda: [".md"])
    exclude_globs: list[str] = Field(
        default_factory=lambda: ["**/.git/**", "**/node_modules/**", "**/.venv/**"]
    )
    storage_dir: str = "./retriever-index"
    write_vmeta_next_to_source: bool = True


class ChunkConfig(BaseModel):
    # Legacy settings are still accepted when loading old configuration files.
    # They are excluded on save because documents are no longer split.
    max_chars: int = Field(default=500, ge=2, exclude=True)
    overlap_chars: int = Field(default=100, ge=0, exclude=True)
    preserve_english_words: bool = Field(default=True, exclude=True)
    split_algorithm_version: int = Field(default=3, ge=1)

    @model_validator(mode="after")
    def validate_overlap(self) -> "ChunkConfig":
        if self.overlap_chars >= self.max_chars:
            raise ValueError("overlap_chars must be smaller than max_chars")
        return self


class EmbeddingConfig(BaseModel):
    provider: Literal["sentence-transformers"] = "sentence-transformers"
    model: str = "BAAI/bge-small-zh-v1.5"
    device: str = "cpu"
    batch_size: int = Field(default=64, ge=1)


class BM25Config(BaseModel):
    engine: Literal["sqlite-fts5"] = "sqlite-fts5"
    tokenizer: str = "unicode61"
    segmenter: Literal["jieba"] = "jieba"
    segmenter_version: int = Field(default=1, ge=1)


class SearchConfig(BaseModel):
    default_k: int = Field(default=3, ge=1)
    candidates_per_channel: int = Field(default=20, ge=1)
    bm25_title_weight: float = 1.0
    bm25_body_weight: float = 1.0
    bm25_weight: float = 1.0
    exact_keyword_weight: float = Field(default=1.0, ge=0)
    accepted_synonym_weight: float = Field(default=0.65, ge=0)
    provisional_synonym_weight: float = Field(default=0.30, ge=0)
    oov_candidate_limit: int = Field(default=3, ge=1)
    oov_similarity_threshold: float = Field(default=0.72, ge=-1, le=1)
    equivalence_candidate_limit_per_term: int = Field(default=3, ge=1)
    equivalence_review_limit_per_search: int = Field(default=5, ge=1)
    synonym_rejection_threshold: int = Field(default=3, ge=1)
    synonym_review_cooldown_hours: int = Field(default=24, ge=0)
    vector_title_weight: float = Field(default=1.0, exclude=True)
    vector_body_weight: float = Field(default=1.0, exclude=True)
    vector_weight: float = Field(default=1.0, exclude=True)


class MaintenanceConfig(BaseModel):
    optimize_lancedb_when_idle: bool = True
    idle_seconds_before_maintenance: int = Field(default=300, ge=1)


class LoggingConfig(BaseModel):
    level: str = "INFO"
    log_dir: str = "./retriever-index/logs"


class AppConfig(BaseModel):
    model_config = ConfigDict(extra="forbid")

    service: ServiceConfig = Field(default_factory=ServiceConfig)
    index: IndexConfig = Field(default_factory=IndexConfig)
    chunk: ChunkConfig = Field(default_factory=ChunkConfig)
    embedding: EmbeddingConfig | None = None
    bm25: BM25Config = Field(default_factory=BM25Config)
    search: SearchConfig = Field(default_factory=SearchConfig)
    maintenance: MaintenanceConfig | None = Field(default=None, exclude=True)
    logging: LoggingConfig = Field(default_factory=LoggingConfig)

    @classmethod
    def load(cls, path: str | Path) -> "AppConfig":
        config_path = Path(path)
        with config_path.open("r", encoding="utf-8") as handle:
            return cls.model_validate(json.load(handle))

    def save(self, path: str | Path) -> None:
        config_path = Path(path)
        config_path.parent.mkdir(parents=True, exist_ok=True)
        temp_path = config_path.with_suffix(config_path.suffix + ".tmp")
        with temp_path.open("w", encoding="utf-8", newline="\n") as handle:
            json.dump(self.model_dump(mode="json"), handle, ensure_ascii=False, indent=2)
            handle.write("\n")
            handle.flush()
        temp_path.replace(config_path)

    def resolve_storage_dir(self, cwd: Path | None = None) -> Path:
        base = (cwd or Path.cwd()).resolve()
        storage = Path(self.index.storage_dir)
        return (base / storage).resolve() if not storage.is_absolute() else storage.resolve()

    def resolve_log_dir(self, cwd: Path | None = None) -> Path:
        base = (cwd or Path.cwd()).resolve()
        log_dir = Path(self.logging.log_dir)
        return (base / log_dir).resolve() if not log_dir.is_absolute() else log_dir.resolve()

    def index_format_fingerprint(self) -> str:
        value = {
            "chunk": self.chunk.model_dump(mode="json"),
            "bm25": self.bm25.model_dump(mode="json"),
        }
        encoded = json.dumps(
            value,
            ensure_ascii=False,
            sort_keys=True,
            separators=(",", ":"),
        ).encode("utf-8")
        return hashlib.sha256(encoded).hexdigest()
