from __future__ import annotations

import json
import time
from collections import defaultdict
from pathlib import Path

import numpy as np

import evaluate as base


CASES_PATH = Path(__file__).with_name("centered-search-cases.json")
JSON_REPORT_PATH = base.REPORT_ROOT / "VectorSearchCentered50-Report.json"
MARKDOWN_REPORT_PATH = base.REPORT_ROOT / "VectorSearchCentered50-Report.md"


def normalize(vectors: np.ndarray) -> np.ndarray:
    return vectors / np.maximum(np.linalg.norm(vectors, axis=1, keepdims=True), 1e-12)


def off_diagonal_mean_cosine(vectors: np.ndarray) -> float:
    similarities = vectors @ vectors.T
    count = similarities.shape[0]
    return float((similarities.sum() - np.trace(similarities)) / (count * (count - 1)))


def metrics(results: list[dict]) -> dict:
    return {
        "top1Accuracy": sum(result["rank"] == 1 for result in results) / len(results),
        "top3Accuracy": sum(result["rank"] <= 3 for result in results) / len(results),
        "top5Accuracy": sum(result["rank"] <= 5 for result in results) / len(results),
        "meanReciprocalRank": sum(1 / result["rank"] for result in results) / len(results),
        "meanTargetScore": sum(result["targetScore"] for result in results) / len(results),
        "meanTargetMargin": sum(result["targetVsBestOtherMargin"] for result in results) / len(results),
    }


def evaluate_scores(
    cases: list[dict],
    documents: list[base.Document],
    scores: np.ndarray,
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
                        "kind": documents[index].kind,
                        "score": round(float(row[index]), 6),
                    }
                    for position, index in enumerate(order[:5])
                ],
            }
        )
    return results


def kind_metrics(results: list[dict], documents: list[base.Document]) -> dict:
    kinds = {document.name: document.kind for document in documents}
    grouped: dict[str, list[dict]] = defaultdict(list)
    for result in results:
        grouped[kinds[result["expected"]]].append(result)
    return {
        kind: {
            "count": len(items),
            "top1Accuracy": sum(item["rank"] == 1 for item in items) / len(items),
            "top3Accuracy": sum(item["rank"] <= 3 for item in items) / len(items),
            "meanReciprocalRank": sum(1 / item["rank"] for item in items) / len(items),
        }
        for kind, items in sorted(grouped.items())
    }


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
    embedder = base.Embedder()
    model_load_ms = (time.perf_counter() - load_started) * 1000

    embedding_started = time.perf_counter()
    identity_vectors = embedder.encode([document.identity for document in documents])
    metadata_vectors = embedder.encode([document.summary for document in documents])
    query_vectors = embedder.encode(["query: " + case["query"] for case in cases])
    embedding_ms = (time.perf_counter() - embedding_started) * 1000

    raw_scores = 0.7 * (query_vectors @ identity_vectors.T) + 0.3 * (query_vectors @ metadata_vectors.T)

    identity_center = identity_vectors.mean(axis=0)
    metadata_center = metadata_vectors.mean(axis=0)

    def scores_at_center_fraction(fraction: float) -> np.ndarray:
        centered_identity_vectors = normalize(identity_vectors - fraction * identity_center)
        centered_metadata_vectors = normalize(metadata_vectors - fraction * metadata_center)
        identity_centered_queries = normalize(query_vectors - fraction * identity_center)
        metadata_centered_queries = normalize(query_vectors - fraction * metadata_center)
        return 0.7 * (identity_centered_queries @ centered_identity_vectors.T) + 0.3 * (
            metadata_centered_queries @ centered_metadata_vectors.T
        )

    centered_identity_vectors = normalize(identity_vectors - identity_center)
    centered_metadata_vectors = normalize(metadata_vectors - metadata_center)
    centered_scores = scores_at_center_fraction(1.0)

    raw_results = evaluate_scores(cases, documents, raw_scores)
    centered_results = evaluate_scores(cases, documents, centered_scores)
    raw_by_id = {result["id"]: result for result in raw_results}

    comparisons = []
    for centered in centered_results:
        raw = raw_by_id[centered["id"]]
        comparisons.append(
            {
                "id": centered["id"],
                "query": centered["query"],
                "expected": centered["expected"],
                "rawRank": raw["rank"],
                "centeredRank": centered["rank"],
                "rankChange": raw["rank"] - centered["rank"],
                "rawTargetScore": raw["targetScore"],
                "centeredTargetScore": centered["targetScore"],
                "rawTop1": raw["top5"][0]["name"],
                "centeredTop1": centered["top5"][0]["name"],
                "centeredTop5": centered["top5"],
            }
        )

    raw_metrics = metrics(raw_results)
    centered_metrics = metrics(centered_results)
    centering_ablation = []
    for fraction in (0.0, 0.25, 0.5, 0.75, 1.0):
        fraction_results = evaluate_scores(cases, documents, scores_at_center_fraction(fraction))
        centering_ablation.append({"centerFraction": fraction, **metrics(fraction_results)})

    both_top1 = sum(item["rawRank"] == 1 and item["centeredRank"] == 1 for item in comparisons)
    raw_only_top1 = sum(item["rawRank"] == 1 and item["centeredRank"] != 1 for item in comparisons)
    centered_only_top1 = sum(item["rawRank"] != 1 and item["centeredRank"] == 1 for item in comparisons)
    neither_top1 = len(comparisons) - both_top1 - raw_only_top1 - centered_only_top1
    report = {
        "model": "intfloat/multilingual-e5-small, QInt8 ONNX conversion",
        "documentCount": len(documents),
        "queryCount": len(cases),
        "indexTextNormalization": (
            "Strip file extensions, strip .editor.json from Task names, and strip CsvData plus .csv from CSV "
            "names and binding types before embedding. Stored/opened file names remain unchanged."
        ),
        "scoring": "0.7 * identity cosine similarity + 0.3 * metadata cosine similarity",
        "centering": (
            "Subtract the corpus mean separately for identity and metadata vectors from both corpus and query "
            "vectors, then L2-normalize. Query vectors are not used to calculate either center."
        ),
        "identityCenterNorm": float(np.linalg.norm(identity_center)),
        "metadataCenterNorm": float(np.linalg.norm(metadata_center)),
        "identityMeanDocumentCosineBefore": off_diagonal_mean_cosine(identity_vectors),
        "identityMeanDocumentCosineAfter": off_diagonal_mean_cosine(centered_identity_vectors),
        "metadataMeanDocumentCosineBefore": off_diagonal_mean_cosine(metadata_vectors),
        "metadataMeanDocumentCosineAfter": off_diagonal_mean_cosine(centered_metadata_vectors),
        "raw": raw_metrics,
        "centered": centered_metrics,
        "centeringAblation": centering_ablation,
        "improvedQueryCount": sum(item["rankChange"] > 0 for item in comparisons),
        "unchangedQueryCount": sum(item["rankChange"] == 0 for item in comparisons),
        "worsenedQueryCount": sum(item["rankChange"] < 0 for item in comparisons),
        "top1PairedCounts": {
            "bothCorrect": both_top1,
            "rawOnlyCorrect": raw_only_top1,
            "centeredOnlyCorrect": centered_only_top1,
            "neitherCorrect": neither_top1,
        },
        "rawKindMetrics": kind_metrics(raw_results, documents),
        "centeredKindMetrics": kind_metrics(centered_results, documents),
        "modelLoadMs": model_load_ms,
        "embeddingMs": embedding_ms,
        "comparisons": comparisons,
    }

    base.REPORT_ROOT.mkdir(parents=True, exist_ok=True)
    JSON_REPORT_PATH.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    lines = [
        "# BbxEditor 50 关键词重中心化检索评测",
        "",
        f"- 文档：{len(documents)}；冻结关键词：{len(cases)}",
        "- 索引名称已移除文件扩展名、`.editor.json`，CSV 名称和绑定类型还移除了 `CsvData`",
        "- 中心只由文档向量计算，查询不参与中心计算",
        f"- Identity 文档间平均余弦：{report['identityMeanDocumentCosineBefore']:.4f} → "
        f"{report['identityMeanDocumentCosineAfter']:.4f}",
        f"- Metadata 文档间平均余弦：{report['metadataMeanDocumentCosineBefore']:.4f} → "
        f"{report['metadataMeanDocumentCosineAfter']:.4f}",
        "",
        "| 指标 | 未中心化 | 重中心化 | 变化 |",
        "|---|---:|---:|---:|",
        f"| Top-1 | {raw_metrics['top1Accuracy']:.0%} | {centered_metrics['top1Accuracy']:.0%} | "
        f"{centered_metrics['top1Accuracy'] - raw_metrics['top1Accuracy']:+.0%} |",
        f"| Top-3 | {raw_metrics['top3Accuracy']:.0%} | {centered_metrics['top3Accuracy']:.0%} | "
        f"{centered_metrics['top3Accuracy'] - raw_metrics['top3Accuracy']:+.0%} |",
        f"| Top-5 | {raw_metrics['top5Accuracy']:.0%} | {centered_metrics['top5Accuracy']:.0%} | "
        f"{centered_metrics['top5Accuracy'] - raw_metrics['top5Accuracy']:+.0%} |",
        f"| MRR | {raw_metrics['meanReciprocalRank']:.3f} | {centered_metrics['meanReciprocalRank']:.3f} | "
        f"{centered_metrics['meanReciprocalRank'] - raw_metrics['meanReciprocalRank']:+.3f} |",
        f"| 目标相对最佳干扰项平均间隔 | {raw_metrics['meanTargetMargin']:+.4f} | "
        f"{centered_metrics['meanTargetMargin']:+.4f} | "
        f"{centered_metrics['meanTargetMargin'] - raw_metrics['meanTargetMargin']:+.4f} |",
        "",
        f"排名改善 {report['improvedQueryCount']} 条，不变 {report['unchangedQueryCount']} 条，"
        f"变差 {report['worsenedQueryCount']} 条。",
        f"Top-1 配对：共同正确 {both_top1}，仅未中心化正确 {raw_only_top1}，"
        f"仅重中心化正确 {centered_only_top1}，共同错误 {neither_top1}。",
        "",
        "## 中心移除比例探索",
        "",
        "> 此表使用同一批 50 条用例作事后分析，不能据此宣称 0.75 是已泛化的最优参数。",
        "",
        "| 中心移除比例 | Top-1 | Top-3 | MRR | 目标间隔 |",
        "|---:|---:|---:|---:|---:|",
    ]
    for item in centering_ablation:
        lines.append(
            f"| {item['centerFraction']:.2f} | {item['top1Accuracy']:.0%} | "
            f"{item['top3Accuracy']:.0%} | {item['meanReciprocalRank']:.3f} | "
            f"{item['meanTargetMargin']:+.4f} |"
        )
    lines.extend(
        [
        "",
        "## 按文件类型",
        "",
        "| 类型 | 数量 | 未中心化 Top-1 | 重中心化 Top-1 | 未中心化 Top-3 | 重中心化 Top-3 |",
        "|---|---:|---:|---:|---:|---:|",
        ]
    )
    centered_metrics_by_kind = report["centeredKindMetrics"]
    for kind in centered_metrics_by_kind:
        raw_kind = report["rawKindMetrics"][kind]
        centered_kind = centered_metrics_by_kind[kind]
        lines.append(
            f"| {kind} | {centered_kind['count']} | {raw_kind['top1Accuracy']:.0%} | "
            f"{centered_kind['top1Accuracy']:.0%} | {raw_kind['top3Accuracy']:.0%} | "
            f"{centered_kind['top3Accuracy']:.0%} |"
        )

    lines.extend(
        [
            "",
            "## 逐条结果",
            "",
            "| # | 关键词 | 目标 | 原排名 | 中心化排名 | 排名变化 | 中心化 Top-1 |",
            "|---:|---|---|---:|---:|---:|---|",
        ]
    )
    for item in comparisons:
        lines.append(
            f"| {item['id']} | {item['query']} | `{item['expected']}` | {item['rawRank']} | "
            f"{item['centeredRank']} | {item['rankChange']:+d} | `{item['centeredTop1']}` |"
        )
    MARKDOWN_REPORT_PATH.write_text("\n".join(lines) + "\n", encoding="utf-8")

    print(json.dumps({key: value for key, value in report.items() if key != "comparisons"}, ensure_ascii=False, indent=2))
    print(f"report={MARKDOWN_REPORT_PATH}")


if __name__ == "__main__":
    main()
