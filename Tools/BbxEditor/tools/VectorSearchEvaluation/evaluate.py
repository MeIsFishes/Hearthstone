from __future__ import annotations

import json
import re
import time
from dataclasses import dataclass
from pathlib import Path

import numpy as np
import onnxruntime as ort
from tokenizers import Tokenizer


ROOT = Path(__file__).resolve().parents[2]
SETTINGS = json.loads((ROOT / "settings.json").read_text(encoding="utf-8"))
PROJECT_ROOT = (ROOT / SETTINGS["gameProjectPath"]).resolve()
METADATA_ROOT = (ROOT / SETTINGS["metadataPath"]).resolve()
MODEL_ROOT = ROOT / "Models" / "Embedding" / "multilingual-e5-small-qint8"
MODEL_PATH = MODEL_ROOT / "model_opt2_QInt8.onnx"
TOKENIZER_PATH = MODEL_ROOT / "tokenizer.json"
CASES_PATH = Path(__file__).with_name("cases.json")
REPORT_ROOT = ROOT / "AutoDoc" / "Temp"


@dataclass(frozen=True)
class Document:
    name: str
    kind: str
    relative_path: str
    identity: str
    summary: str


def load_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def readable_identifier(value: object) -> str:
    text = str(value).replace("\\", " ").replace("/", " ").replace("_", " ").replace(".", " ")
    text = re.sub(r"(?<=[a-z0-9])(?=[A-Z])", " ", text)
    text = re.sub(r"(?<=[A-Z])(?=[A-Z][a-z])", " ", text)
    return " ".join(text.split())


def semantic_document_name(path: Path, kind: str) -> str:
    name = path.name
    if kind == "Task" and name.casefold().endswith(".editor.json"):
        return name[: -len(".editor.json")]

    stem = path.stem
    if kind == "CSV":
        stem = re.sub(r"CsvData$", "", stem, flags=re.IGNORECASE)
    return stem


def strip_csv_data_suffix(value: object) -> str:
    return re.sub(r"CsvData$", "", str(value), flags=re.IGNORECASE)


def collect_values(value, keys: set[str], result: set[str]) -> None:
    if isinstance(value, dict):
        for key, child in value.items():
            if key in keys and isinstance(child, (str, int, float, bool)):
                text = str(child).strip()
                if text and text.lower() != "null":
                    result.add(text)
            collect_values(child, keys, result)
    elif isinstance(value, list):
        for child in value:
            collect_values(child, keys, result)


def build_documents() -> list[Document]:
    allowed_roots = []
    for configured in SETTINGS.get("explorerDirectories", ["Assets/Resources", "Mods"]):
        path = (PROJECT_ROOT / configured).resolve()
        if path.is_dir():
            allowed_roots.append(path)

    csv_metadata = {}
    for path in (METADATA_ROOT / "Csv").glob("*.json"):
        item = load_json(path)
        for table_name in item.get("TableNames", []):
            csv_metadata[table_name.casefold()] = item
        csv_metadata.setdefault(item.get("TypeName", "").casefold(), item)

    scriptable_metadata = {}
    for path in (METADATA_ROOT / "ScriptableObject").glob("*.json"):
        item = load_json(path)
        scriptable_metadata[item.get("FullTypeName", "")] = item

    asset_index = load_json(METADATA_ROOT / "Assets" / "asset-index.json")
    asset_items = asset_index.get("Assets", asset_index.get("assets", []))
    editable_assets = {item["AssetPath"].replace("\\", "/"): item for item in asset_items}

    documents: list[Document] = []
    seen: set[Path] = set()
    for allowed_root in allowed_roots:
        for path in allowed_root.rglob("*"):
            if not path.is_file() or path in seen:
                continue
            seen.add(path)
            relative = path.relative_to(PROJECT_ROOT).as_posix()
            lower_name = path.name.casefold()
            if lower_name.endswith(".editor.json"):
                semantic_name = semantic_document_name(path, "Task")
                values: set[str] = set()
                collect_values(load_json(path), {"TaskType", "FromTask", "ToTask", "FieldName"}, values)
                readable_values = " ".join(readable_identifier(value) for value in sorted(values))
                summary = (
                    f"passage: file type Task behavior graph. file name {readable_identifier(semantic_name)}. "
                    f"graph name {readable_identifier(semantic_name)}. "
                    f"nodes and fields {readable_values}."
                )
                identity = (
                    f"passage: Task behavior graph named "
                    f"{readable_identifier(semantic_name)}."
                )
                documents.append(Document(path.name, "Task", relative, identity, summary))
            elif path.suffix.casefold() == ".csv":
                semantic_name = semantic_document_name(path, "CSV")
                item = csv_metadata.get(path.stem.casefold(), {})
                columns = [readable_identifier(column.get("Name", "")) for column in item.get("Columns", [])]
                summary = (
                    f"passage: file type CSV configuration table. file name {readable_identifier(semantic_name)}. "
                    f"binding type "
                    f"{readable_identifier(strip_csv_data_suffix(item.get('FullTypeName', semantic_name)))}. "
                    f"data group {readable_identifier(item.get('DataGroup', ''))}. "
                    f"columns {' '.join(columns)}."
                )
                identity = f"passage: CSV configuration table named {readable_identifier(semantic_name)}."
                documents.append(Document(path.name, "CSV", relative, identity, summary))
            elif path.suffix.casefold() == ".asset" and relative in editable_assets:
                semantic_name = semantic_document_name(path, "ScriptableObject")
                asset = editable_assets[relative]
                item = scriptable_metadata.get(asset.get("TypeName", ""), {})
                fields = [readable_identifier(field.get("Name", "")) for field in item.get("Fields", [])]
                summary = (
                    f"passage: file type Bbx Scriptable Object asset settings. "
                    f"file name {readable_identifier(semantic_name)}. "
                    f"asset type {readable_identifier(asset.get('TypeName', ''))}. fields {' '.join(fields)}."
                )
                identity = (
                    f"passage: Bbx Scriptable Object settings asset named {readable_identifier(semantic_name)}."
                )
                documents.append(Document(path.name, "ScriptableObject", relative, identity, summary))

    return sorted(documents, key=lambda item: (item.kind, item.name.casefold()))


class Embedder:
    def __init__(self) -> None:
        self.tokenizer = Tokenizer.from_file(str(TOKENIZER_PATH))
        self.tokenizer.enable_truncation(max_length=256)
        self.session = ort.InferenceSession(str(MODEL_PATH), providers=["CPUExecutionProvider"])

    def encode(self, texts: list[str], batch_size: int = 16) -> np.ndarray:
        batches = []
        pad_id = self.tokenizer.token_to_id("<pad>")
        if pad_id is None:
            raise RuntimeError("Tokenizer does not define a <pad> token")
        for start in range(0, len(texts), batch_size):
            encoded = self.tokenizer.encode_batch(texts[start : start + batch_size])
            max_length = max(len(item.ids) for item in encoded)
            input_ids = np.full((len(encoded), max_length), pad_id, dtype=np.int64)
            attention_mask = np.zeros_like(input_ids)
            token_type_ids = np.zeros_like(input_ids)
            for row, item in enumerate(encoded):
                length = len(item.ids)
                input_ids[row, :length] = item.ids
                attention_mask[row, :length] = item.attention_mask
                if item.type_ids:
                    token_type_ids[row, :length] = item.type_ids
            hidden = self.session.run(
                ["last_hidden_state"],
                {
                    "input_ids": input_ids,
                    "attention_mask": attention_mask,
                    "token_type_ids": token_type_ids,
                },
            )[0]
            mask = attention_mask[..., None].astype(np.float32)
            pooled = (hidden * mask).sum(axis=1) / np.maximum(mask.sum(axis=1), 1.0)
            pooled /= np.maximum(np.linalg.norm(pooled, axis=1, keepdims=True), 1e-12)
            batches.append(pooled)
        return np.concatenate(batches, axis=0)


def main() -> None:
    documents = build_documents()
    cases = load_json(CASES_PATH)
    expected_names = {case["expected"] for case in cases}
    available_names = {document.name for document in documents}
    missing = expected_names - available_names
    if missing:
        raise RuntimeError(f"Evaluation targets are absent from the current index: {sorted(missing)}")

    load_start = time.perf_counter()
    embedder = Embedder()
    model_load_ms = (time.perf_counter() - load_start) * 1000

    index_start = time.perf_counter()
    identity_vectors = embedder.encode([document.identity for document in documents])
    document_vectors = embedder.encode([document.summary for document in documents])
    index_ms = (time.perf_counter() - index_start) * 1000

    query_start = time.perf_counter()
    query_vectors = embedder.encode(["query: " + case["query"] for case in cases])
    query_ms = (time.perf_counter() - query_start) * 1000
    identity_similarities = query_vectors @ identity_vectors.T
    metadata_similarities = query_vectors @ document_vectors.T
    similarities = 0.7 * identity_similarities + 0.3 * metadata_similarities

    results = []
    for case_index, case in enumerate(cases):
        order = np.argsort(-similarities[case_index])
        target_index = next(index for index, document in enumerate(documents) if document.name == case["expected"])
        rank = int(np.where(order == target_index)[0][0]) + 1
        target_score = float(similarities[case_index, target_index])
        best_other_score = max(
            float(similarities[case_index, index]) for index in range(len(documents)) if index != target_index
        )
        top = [
            {
                "rank": position + 1,
                "name": documents[index].name,
                "kind": documents[index].kind,
                "score": round(float(similarities[case_index, index]), 6),
            }
            for position, index in enumerate(order[:5])
        ]
        results.append(
            {
                **case,
                "rank": rank,
                "targetScore": round(target_score, 6),
                "targetVsBestOtherMargin": round(target_score - best_other_score, 6),
                "top5": top,
            }
        )

    top1 = sum(result["rank"] == 1 for result in results)
    top3 = sum(result["rank"] <= 3 for result in results)
    mean_reciprocal_rank = sum(1.0 / result["rank"] for result in results) / len(results)
    target_scores = [result["targetScore"] for result in results]
    margins = [result["targetVsBestOtherMargin"] for result in results]
    report = {
        "model": "intfloat/multilingual-e5-small, QInt8 ONNX conversion",
        "scoring": "0.7 * identity cosine similarity + 0.3 * metadata cosine similarity",
        "modelBytes": MODEL_PATH.stat().st_size,
        "tokenizerBytes": TOKENIZER_PATH.stat().st_size,
        "documentCount": len(documents),
        "caseCount": len(cases),
        "top1Accuracy": top1 / len(results),
        "top3Accuracy": top3 / len(results),
        "meanReciprocalRank": mean_reciprocal_rank,
        "meanTargetSimilarity": sum(target_scores) / len(target_scores),
        "meanTargetMargin": sum(margins) / len(margins),
        "modelLoadMs": model_load_ms,
        "indexEmbeddingMs": index_ms,
        "queryBatchMs": query_ms,
        "results": results,
        "documents": [document.__dict__ for document in documents],
    }

    REPORT_ROOT.mkdir(parents=True, exist_ok=True)
    json_path = REPORT_ROOT / "VectorSearchEvaluation-Report.json"
    markdown_path = REPORT_ROOT / "VectorSearchEvaluation-Report.md"
    json_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    lines = [
        "# BbxEditor 向量搜索评测",
        "",
        f"- 模型：`{report['model']}`",
        f"- 评分：`{report['scoring']}`",
        f"- 模型与 tokenizer：{(report['modelBytes'] + report['tokenizerBytes']) / 1024 / 1024:.2f} MB",
        f"- 真实文档：{len(documents)}",
        f"- 用例：{len(cases)}",
        f"- Top-1：{top1}/{len(results)} ({report['top1Accuracy']:.0%})",
        f"- Top-3：{top3}/{len(results)} ({report['top3Accuracy']:.0%})",
        f"- MRR：{mean_reciprocal_rank:.3f}",
        f"- 平均目标余弦相似度：{report['meanTargetSimilarity']:.4f}",
        f"- 平均目标相对最佳干扰项间隔：{report['meanTargetMargin']:+.4f}",
        f"- 模型加载：{model_load_ms:.1f} ms；{len(documents)} 份文档建向量：{index_ms:.1f} ms；"
        f"{len(cases)} 条查询批量推理：{query_ms:.1f} ms",
        "",
        "| # | 查询 | 目标 | 排名 | 目标相似度 | 间隔 | Top-1 |",
        "|---:|---|---|---:|---:|---:|---|",
    ]
    for index, result in enumerate(results, 1):
        lines.append(
            f"| {index} | {result['query']} | `{result['expected']}` | {result['rank']} | "
            f"{result['targetScore']:.4f} | {result['targetVsBestOtherMargin']:+.4f} | `{result['top5'][0]['name']}` |"
        )
    lines.extend(["", "## 每条查询 Top-5", ""])
    for index, result in enumerate(results, 1):
        lines.append(f"### {index}. {result['query']}")
        lines.append("")
        for item in result["top5"]:
            marker = " ← 目标" if item["name"] == result["expected"] else ""
            lines.append(f"{item['rank']}. `{item['name']}` — {item['score']:.4f}{marker}")
        lines.append("")
    markdown_path.write_text("\n".join(lines), encoding="utf-8")

    print(json.dumps({key: value for key, value in report.items() if key not in {"results", "documents"}}, ensure_ascii=False, indent=2))
    for result in results:
        print(
            f"rank={result['rank']:>2} score={result['targetScore']:.4f} margin={result['targetVsBestOtherMargin']:+.4f} "
            f"query={result['query']} target={result['expected']} top1={result['top5'][0]['name']}"
        )
    print(f"report={markdown_path}")


if __name__ == "__main__":
    main()
