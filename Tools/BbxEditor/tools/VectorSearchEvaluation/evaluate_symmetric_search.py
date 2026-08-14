from __future__ import annotations

import json
import time
from collections import defaultdict
from pathlib import Path

import numpy as np

import evaluate as base


CASES_PATH = Path(__file__).with_name("centered-search-cases.json")
JSON_REPORT_PATH = base.REPORT_ROOT / "VectorSearchSymmetric50-Report.json"
MARKDOWN_REPORT_PATH = base.REPORT_ROOT / "VectorSearchSymmetric50-Report.md"


def with_prefix(text: str, prefix: str) -> str:
    _, separator, content = text.partition(":")
    return f"{prefix}: {content.strip()}" if separator else f"{prefix}: {text.strip()}"


def clean_name(document: base.Document) -> str:
    semantic_name = base.semantic_document_name(Path(document.name), document.kind)
    return base.readable_identifier(semantic_name)


def evaluate_scores(cases: list[dict], documents: list[base.Document], scores: np.ndarray) -> list[dict]:
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
                "top3": [
                    {
                        "name": documents[index].name,
                        "kind": documents[index].kind,
                        "score": round(float(row[index]), 6),
                    }
                    for index in order[:3]
                ],
            }
        )
    return results


def metrics(results: list[dict]) -> dict:
    return {
        "top1Accuracy": sum(result["rank"] == 1 for result in results) / len(results),
        "top3Accuracy": sum(result["rank"] <= 3 for result in results) / len(results),
        "top5Accuracy": sum(result["rank"] <= 5 for result in results) / len(results),
        "meanReciprocalRank": sum(1 / result["rank"] for result in results) / len(results),
        "meanTargetScore": sum(result["targetScore"] for result in results) / len(results),
        "meanTargetMargin": sum(result["targetVsBestOtherMargin"] for result in results) / len(results),
    }


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
    query_vectors = embedder.encode(["query: " + case["query"] for case in cases])

    asymmetric_identity_vectors = embedder.encode([document.identity for document in documents])
    asymmetric_metadata_vectors = embedder.encode([document.summary for document in documents])

    symmetric_identity_vectors = embedder.encode(
        [with_prefix(document.identity, "query") for document in documents]
    )
    symmetric_metadata_vectors = embedder.encode(
        [with_prefix(document.summary, "query") for document in documents]
    )
    symmetric_name_vectors = embedder.encode(
        ["query: " + clean_name(document) for document in documents]
    )
    embedding_ms = (time.perf_counter() - embedding_started) * 1000

    asymmetric_scores = 0.7 * (query_vectors @ asymmetric_identity_vectors.T) + 0.3 * (
        query_vectors @ asymmetric_metadata_vectors.T
    )
    symmetric_two_channel_scores = 0.7 * (query_vectors @ symmetric_identity_vectors.T) + 0.3 * (
        query_vectors @ symmetric_metadata_vectors.T
    )
    symmetric_name_scores = query_vectors @ symmetric_name_vectors.T
    mixed_name_metadata_scores = 0.7 * symmetric_name_scores + 0.3 * (
        query_vectors @ asymmetric_metadata_vectors.T
    )

    variants = {
        "asymmetricTwoChannel": evaluate_scores(cases, documents, asymmetric_scores),
        "symmetricTwoChannel": evaluate_scores(cases, documents, symmetric_two_channel_scores),
        "symmetricNameOnly": evaluate_scores(cases, documents, symmetric_name_scores),
        "symmetricNameAsymmetricMetadata": evaluate_scores(
            cases, documents, mixed_name_metadata_scores
        ),
    }
    variant_metrics = {name: metrics(results) for name, results in variants.items()}
    variant_kind_metrics = {
        name: kind_metrics(results, documents) for name, results in variants.items()
    }

    result_by_variant_and_id = {
        name: {result["id"]: result for result in results} for name, results in variants.items()
    }
    comparisons = []
    for case in cases:
        asymmetric = result_by_variant_and_id["asymmetricTwoChannel"][case["id"]]
        symmetric_two = result_by_variant_and_id["symmetricTwoChannel"][case["id"]]
        symmetric_name = result_by_variant_and_id["symmetricNameOnly"][case["id"]]
        mixed = result_by_variant_and_id["symmetricNameAsymmetricMetadata"][case["id"]]
        comparisons.append(
            {
                **case,
                "asymmetricRank": asymmetric["rank"],
                "symmetricTwoChannelRank": symmetric_two["rank"],
                "symmetricNameOnlyRank": symmetric_name["rank"],
                "mixedRank": mixed["rank"],
                "asymmetricTop1": asymmetric["top3"][0]["name"],
                "symmetricTwoChannelTop1": symmetric_two["top3"][0]["name"],
                "symmetricNameOnlyTop1": symmetric_name["top3"][0]["name"],
                "mixedTop1": mixed["top3"][0]["name"],
                "symmetricNameOnlyTargetScore": symmetric_name["targetScore"],
                "symmetricNameOnlyMargin": symmetric_name["targetVsBestOtherMargin"],
                "symmetricNameOnlyTop3": symmetric_name["top3"],
            }
        )

    report = {
        "model": "intfloat/multilingual-e5-small, QInt8 ONNX conversion",
        "documentCount": len(documents),
        "queryCount": len(cases),
        "indexTextNormalization": (
            "Strip file extensions, strip .editor.json from Task names, and strip CsvData plus .csv from CSV "
            "names and binding types before embedding. Stored/opened file names remain unchanged."
        ),
        "centeringApplied": False,
        "variants": {
            "asymmetricTwoChannel": "query prefix for search; passage prefix for identity and metadata; 70/30",
            "symmetricTwoChannel": "query prefix for search, identity, and metadata; 70/30",
            "symmetricNameOnly": "query prefix for both search keywords and clean file name; cosine only",
            "symmetricNameAsymmetricMetadata": (
                "query prefix for clean file name matching, passage prefix for metadata; 70/30"
            ),
        },
        "metrics": variant_metrics,
        "kindMetrics": variant_kind_metrics,
        "modelLoadMs": model_load_ms,
        "embeddingMs": embedding_ms,
        "comparisons": comparisons,
    }

    base.REPORT_ROOT.mkdir(parents=True, exist_ok=True)
    JSON_REPORT_PATH.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    labels = {
        "asymmetricTwoChannel": "非对称双通道",
        "symmetricTwoChannel": "对称双通道",
        "symmetricNameOnly": "对称纯文件名",
        "symmetricNameAsymmetricMetadata": "名称对称 + 元数据非对称",
    }
    lines = [
        "# BbxEditor 50 关键词对称文本匹配评测",
        "",
        f"- 文档：{len(documents)}；冻结关键词：{len(cases)}",
        "- 所有名称均已删除文件扩展名、`.editor.json` 和 CSV 的 `CsvData`",
        "- 本轮不做重中心化",
        "",
        "| 方案 | Top-1 | Top-3 | Top-5 | MRR | 目标间隔 |",
        "|---|---:|---:|---:|---:|---:|",
    ]
    for name in (
        "asymmetricTwoChannel",
        "symmetricTwoChannel",
        "symmetricNameOnly",
        "symmetricNameAsymmetricMetadata",
    ):
        item = variant_metrics[name]
        lines.append(
            f"| {labels[name]} | {item['top1Accuracy']:.0%} | {item['top3Accuracy']:.0%} | "
            f"{item['top5Accuracy']:.0%} | {item['meanReciprocalRank']:.3f} | "
            f"{item['meanTargetMargin']:+.4f} |"
        )

    lines.extend(
        [
            "",
            "## 按文件类型",
            "",
            "| 类型 | 数量 | 非对称 Top-1 | 对称双通道 Top-1 | 对称文件名 Top-1 | 混合 Top-1 | "
            "非对称 Top-3 | 对称双通道 Top-3 | 对称文件名 Top-3 | 混合 Top-3 |",
            "|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|",
        ]
    )
    for kind in variant_kind_metrics["asymmetricTwoChannel"]:
        asymmetric = variant_kind_metrics["asymmetricTwoChannel"][kind]
        symmetric_two = variant_kind_metrics["symmetricTwoChannel"][kind]
        symmetric_name = variant_kind_metrics["symmetricNameOnly"][kind]
        mixed = variant_kind_metrics["symmetricNameAsymmetricMetadata"][kind]
        lines.append(
            f"| {kind} | {asymmetric['count']} | {asymmetric['top1Accuracy']:.0%} | "
            f"{symmetric_two['top1Accuracy']:.0%} | {symmetric_name['top1Accuracy']:.0%} | "
            f"{mixed['top1Accuracy']:.0%} | "
            f"{asymmetric['top3Accuracy']:.0%} | {symmetric_two['top3Accuracy']:.0%} | "
            f"{symmetric_name['top3Accuracy']:.0%} | {mixed['top3Accuracy']:.0%} |"
        )

    lines.extend(
        [
            "",
            "## 逐条结果",
            "",
            "| # | 关键词 | 目标 | 非对称排名 | 对称双通道排名 | 对称文件名排名 | 混合排名 | 混合 Top-1 |",
            "|---:|---|---|---:|---:|---:|---:|---|",
        ]
    )
    for item in comparisons:
        lines.append(
            f"| {item['id']} | {item['query']} | `{item['expected']}` | {item['asymmetricRank']} | "
            f"{item['symmetricTwoChannelRank']} | {item['symmetricNameOnlyRank']} | {item['mixedRank']} | "
            f"`{item['mixedTop1']}` |"
        )
    MARKDOWN_REPORT_PATH.write_text("\n".join(lines) + "\n", encoding="utf-8")

    print(json.dumps({key: value for key, value in report.items() if key != "comparisons"}, ensure_ascii=False, indent=2))
    print(f"report={MARKDOWN_REPORT_PATH}")


if __name__ == "__main__":
    main()
