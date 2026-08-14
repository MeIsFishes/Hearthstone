from __future__ import annotations

import json
import os
import time
from pathlib import Path

import numpy as np
import onnxruntime as ort
from tokenizers import Tokenizer

import evaluate as base


CASES_PATH = Path(__file__).with_name("centered-search-cases.json")
COMMON_SETTINGS_PATH = Path(os.environ.get("LOCALAPPDATA", "")) / "BbxCommon" / "settings.json"
COMMON_SETTINGS = json.loads(COMMON_SETTINGS_PATH.read_text(encoding="utf-8"))
MODEL_BASE = Path(COMMON_SETTINGS["modelDirectory"])
MODEL_ROOT = (
    MODEL_BASE
    if (MODEL_BASE.name == "paraphrase-multilingual-mpnet-base-v2-quint8-avx2"
    else MODEL_BASE / "paraphrase-multilingual-mpnet-base-v2-quint8-avx2"
)
MODEL_PATH = MODEL_ROOT / "model_quint8_avx2.onnx"
TOKENIZER_PATH = MODEL_ROOT / "tokenizer.json"
JSON_REPORT_PATH = base.REPORT_ROOT / "VectorSearchMpnetSymmetric50-Report.json"
MARKDOWN_REPORT_PATH = base.REPORT_ROOT / "VectorSearchMpnetSymmetric50-Report.md"
E5_REPORT_PATH = base.REPORT_ROOT / "VectorSearchSymmetric50-Report.json"


class MpnetEmbedder:
    def __init__(self) -> None:
        self.tokenizer = Tokenizer.from_file(str(TOKENIZER_PATH))
        self.tokenizer.enable_truncation(max_length=128)
        self.pad_id = self.tokenizer.token_to_id("<pad>")
        if self.pad_id is None:
            raise RuntimeError("Tokenizer does not define a <pad> token")
        self.session = ort.InferenceSession(str(MODEL_PATH), providers=["CPUExecutionProvider"])

    def encode(self, texts: list[str], batch_size: int = 16) -> np.ndarray:
        batches = []
        for start in range(0, len(texts), batch_size):
            encoded = self.tokenizer.encode_batch(texts[start : start + batch_size])
            max_length = max(len(item.ids) for item in encoded)
            input_ids = np.full((len(encoded), max_length), self.pad_id, dtype=np.int64)
            attention_mask = np.zeros_like(input_ids)
            for row, item in enumerate(encoded):
                length = len(item.ids)
                input_ids[row, :length] = item.ids
                attention_mask[row, :length] = item.attention_mask

            hidden = self.session.run(
                ["last_hidden_state"],
                {"input_ids": input_ids, "attention_mask": attention_mask},
            )[0]
            mask = attention_mask[..., None].astype(np.float32)
            pooled = (hidden * mask).sum(axis=1) / np.maximum(mask.sum(axis=1), 1.0)
            pooled /= np.maximum(np.linalg.norm(pooled, axis=1, keepdims=True), 1e-12)
            batches.append(pooled)
        return np.concatenate(batches, axis=0)


def clean_name(document: base.Document) -> str:
    semantic_name = base.semantic_document_name(Path(document.name), document.kind)
    return base.readable_identifier(semantic_name)


def normalize(vectors: np.ndarray) -> np.ndarray:
    return vectors / np.maximum(np.linalg.norm(vectors, axis=1, keepdims=True), 1e-12)


def evaluate_scores(
    cases: list[dict], documents: list[base.Document], scores: np.ndarray
) -> list[dict]:
    document_index = {document.name: index for index, document in enumerate(documents)}
    results = []
    for case_index, case in enumerate(cases):
        row = scores[case_index]
        order = np.argsort(-row)
        target_index = document_index[case["expected"]]
        rank = int(np.where(order == target_index)[0][0]) + 1
        target_score = float(row[target_index])
        best_other_score = max(float(row[index]) for index in range(len(documents)) if index != target_index)
        results.append(
            {
                **case,
                "rank": rank,
                "targetScore": round(target_score, 6),
                "targetVsBestOtherMargin": round(target_score - best_other_score, 6),
                "top5": [
                    {
                        "rank": position + 1,
                        "name": documents[index].name,
                        "score": round(float(row[index]), 6),
                    }
                    for position, index in enumerate(order[:5])
                ],
            }
        )
    return results


def calculate_metrics(results: list[dict]) -> dict:
    return {
        "top1Accuracy": sum(result["rank"] == 1 for result in results) / len(results),
        "top3Accuracy": sum(result["rank"] <= 3 for result in results) / len(results),
        "top5Accuracy": sum(result["rank"] <= 5 for result in results) / len(results),
        "meanReciprocalRank": sum(1 / result["rank"] for result in results) / len(results),
        "meanTargetSimilarity": sum(result["targetScore"] for result in results) / len(results),
        "meanTargetMargin": sum(result["targetVsBestOtherMargin"] for result in results) / len(results),
    }


def off_diagonal_mean_cosine(vectors: np.ndarray) -> float:
    similarities = vectors @ vectors.T
    count = similarities.shape[0]
    return float((similarities.sum() - np.trace(similarities)) / (count * (count - 1)))


def main() -> None:
    cases = base.load_json(CASES_PATH)
    if len(cases) != 50:
        raise RuntimeError(f"Expected exactly 50 frozen cases, got {len(cases)}")

    documents = base.build_documents()
    available = {document.name for document in documents}
    missing = {case["expected"] for case in cases} - available
    if missing:
        raise RuntimeError(f"Evaluation targets are absent from the current index: {sorted(missing)}")

    load_started = time.perf_counter()
    embedder = MpnetEmbedder()
    model_load_ms = (time.perf_counter() - load_started) * 1000

    index_started = time.perf_counter()
    document_vectors = embedder.encode([clean_name(document) for document in documents])
    index_ms = (time.perf_counter() - index_started) * 1000

    query_started = time.perf_counter()
    query_vectors = embedder.encode([case["query"] for case in cases])
    query_ms = (time.perf_counter() - query_started) * 1000
    raw_scores = query_vectors @ document_vectors.T
    results = evaluate_scores(cases, documents, raw_scores)
    metrics = calculate_metrics(results)

    document_center = document_vectors.mean(axis=0)
    centering_ablation = []
    centered_results_by_fraction: dict[str, list[dict]] = {}
    for fraction in (0.0, 0.25, 0.5, 0.75, 1.0):
        centered_documents = normalize(document_vectors - fraction * document_center)
        centered_queries = normalize(query_vectors - fraction * document_center)
        fraction_results = evaluate_scores(cases, documents, centered_queries @ centered_documents.T)
        centered_results_by_fraction[str(fraction)] = fraction_results
        centering_ablation.append(
            {"centerFraction": fraction, **calculate_metrics(fraction_results)}
        )

    fully_centered_documents = normalize(document_vectors - document_center)
    fully_centered_results = centered_results_by_fraction["1.0"]
    fully_centered_by_id = {result["id"]: result for result in fully_centered_results}
    centering_comparisons = [
        {
            "id": result["id"],
            "query": result["query"],
            "expected": result["expected"],
            "rawRank": result["rank"],
            "centeredRank": fully_centered_by_id[result["id"]]["rank"],
            "rankChange": result["rank"] - fully_centered_by_id[result["id"]]["rank"],
            "rawTop1": result["top5"][0]["name"],
            "centeredTop1": fully_centered_by_id[result["id"]]["top5"][0]["name"],
        }
        for result in results
    ]

    e5_metrics = None
    if E5_REPORT_PATH.is_file():
        e5_report = base.load_json(E5_REPORT_PATH)
        e5_metrics = e5_report.get("metrics", {}).get("symmetricNameOnly")

    report = {
        "model": "sentence-transformers/paraphrase-multilingual-mpnet-base-v2, QUInt8 AVX2 ONNX",
        "modelBytes": MODEL_PATH.stat().st_size,
        "tokenizerBytes": TOKENIZER_PATH.stat().st_size,
        "embeddingDimension": int(document_vectors.shape[1]),
        "documentCount": len(documents),
        "queryCount": len(cases),
        "matching": "symmetric clean-name cosine similarity; no prefixes; no metadata; no type weighting",
        "centering": (
            "Corpus mean is calculated from the 24 clean file-name vectors only. The same stored mean is "
            "subtracted from document and query vectors, followed by L2 normalization. Queries do not "
            "participate in calculating the center."
        ),
        "documentCenterNorm": float(np.linalg.norm(document_center)),
        "meanDocumentCosineBeforeCentering": off_diagonal_mean_cosine(document_vectors),
        "meanDocumentCosineAfterFullCentering": off_diagonal_mean_cosine(fully_centered_documents),
        "metrics": metrics,
        "centeringAblation": centering_ablation,
        "fullCenteringRankChanges": {
            "improved": sum(item["rankChange"] > 0 for item in centering_comparisons),
            "unchanged": sum(item["rankChange"] == 0 for item in centering_comparisons),
            "worsened": sum(item["rankChange"] < 0 for item in centering_comparisons),
        },
        "e5SymmetricNameOnlyMetrics": e5_metrics,
        "modelLoadMs": model_load_ms,
        "indexEmbeddingMs": index_ms,
        "queryBatchMs": query_ms,
        "results": results,
        "fullCenteringComparisons": centering_comparisons,
        "documents": [
            {"name": document.name, "cleanName": clean_name(document)} for document in documents
        ],
    }

    base.REPORT_ROOT.mkdir(parents=True, exist_ok=True)
    JSON_REPORT_PATH.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    lines = [
        "# BbxEditor multilingual MPNet 对称文件名匹配评测",
        "",
        f"- 模型：`{report['model']}`",
        f"- 模型与 tokenizer：{(report['modelBytes'] + report['tokenizerBytes']) / 1024 / 1024:.2f} MiB",
        f"- 文档：{len(documents)}；冻结关键词：{len(cases)}",
        "- 输入：清洗后的文件名与搜索关键词；不加前缀",
        "- 不使用元数据或文件类型权重；对照测试 0%–100% 重中心化",
        "",
        "| 模型 | Top-1 | Top-3 | Top-5 | MRR | 目标间隔 |",
        "|---|---:|---:|---:|---:|---:|",
        f"| multilingual MPNet | {metrics['top1Accuracy']:.0%} | {metrics['top3Accuracy']:.0%} | "
        f"{metrics['top5Accuracy']:.0%} | {metrics['meanReciprocalRank']:.3f} | "
        f"{metrics['meanTargetMargin']:+.4f} |",
    ]
    if e5_metrics:
        lines.append(
            f"| multilingual E5-small | {e5_metrics['top1Accuracy']:.0%} | "
            f"{e5_metrics['top3Accuracy']:.0%} | {e5_metrics['top5Accuracy']:.0%} | "
            f"{e5_metrics['meanReciprocalRank']:.3f} | {e5_metrics['meanTargetMargin']:+.4f} |"
        )

    lines.extend(
        [
            "",
            "## 重中心化对照",
            "",
            f"- 文档间平均余弦：{report['meanDocumentCosineBeforeCentering']:.4f} → "
            f"{report['meanDocumentCosineAfterFullCentering']:.4f}",
            "- 中心仅由 24 个文件名计算，50 条查询不参与中心计算",
            "",
            "| 中心移除比例 | Top-1 | Top-3 | Top-5 | MRR | 目标间隔 |",
            "|---:|---:|---:|---:|---:|---:|",
        ]
    )
    for item in centering_ablation:
        lines.append(
            f"| {item['centerFraction']:.2f} | {item['top1Accuracy']:.0%} | "
            f"{item['top3Accuracy']:.0%} | {item['top5Accuracy']:.0%} | "
            f"{item['meanReciprocalRank']:.3f} | {item['meanTargetMargin']:+.4f} |"
        )
    lines.extend(
        [
            "",
            f"完整中心化后：排名改善 {report['fullCenteringRankChanges']['improved']} 条，"
            f"不变 {report['fullCenteringRankChanges']['unchanged']} 条，"
            f"变差 {report['fullCenteringRankChanges']['worsened']} 条。",
        ]
    )

    lines.extend(
        [
            "",
            f"模型加载 {model_load_ms:.1f} ms；{len(documents)} 个文件名建向量 {index_ms:.1f} ms；"
            f"{len(cases)} 条查询批量推理 {query_ms:.1f} ms。",
            "",
            "## 逐条结果",
            "",
            "| # | 关键词 | 目标 | 排名 | 相似度 | 间隔 | Top-1 |",
            "|---:|---|---|---:|---:|---:|---|",
        ]
    )
    for result in results:
        lines.append(
            f"| {result['id']} | {result['query']} | `{result['expected']}` | {result['rank']} | "
            f"{result['targetScore']:.4f} | {result['targetVsBestOtherMargin']:+.4f} | "
            f"`{result['top5'][0]['name']}` |"
        )
    MARKDOWN_REPORT_PATH.write_text("\n".join(lines) + "\n", encoding="utf-8")

    print(
        json.dumps(
            {
                key: value
                for key, value in report.items()
                if key not in {"results", "documents", "fullCenteringComparisons"}
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    print(f"report={MARKDOWN_REPORT_PATH}")


if __name__ == "__main__":
    main()
