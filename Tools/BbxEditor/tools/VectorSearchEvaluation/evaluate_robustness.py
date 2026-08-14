from __future__ import annotations

import json
import time
from pathlib import Path

import numpy as np

import evaluate as base


CASES_PATH = Path(__file__).with_name("robustness-cases.json")
JSON_REPORT_PATH = base.REPORT_ROOT / "VectorSearchRobustness-Report.json"
MARKDOWN_REPORT_PATH = base.REPORT_ROOT / "VectorSearchRobustness-Report.md"


def score_queries(
    embedder: base.Embedder,
    documents: list[base.Document],
    identity_vectors: np.ndarray,
    metadata_vectors: np.ndarray,
    queries: list[str],
) -> tuple[np.ndarray, float]:
    started = time.perf_counter()
    query_vectors = embedder.encode(["query: " + query for query in queries])
    elapsed_ms = (time.perf_counter() - started) * 1000
    scores = 0.7 * (query_vectors @ identity_vectors.T) + 0.3 * (query_vectors @ metadata_vectors.T)
    return scores, elapsed_ms


def rank_result(
    query: str,
    expected: str,
    scores: np.ndarray,
    documents: list[base.Document],
) -> dict:
    order = np.argsort(-scores)
    target_index = next(index for index, document in enumerate(documents) if document.name == expected)
    rank = int(np.where(order == target_index)[0][0]) + 1
    target_score = float(scores[target_index])
    best_other_score = max(float(scores[index]) for index in range(len(documents)) if index != target_index)
    return {
        "query": query,
        "expected": expected,
        "rank": rank,
        "targetScore": round(target_score, 6),
        "targetVsBestOtherMargin": round(target_score - best_other_score, 6),
        "top3": [
            {
                "name": documents[index].name,
                "kind": documents[index].kind,
                "score": round(float(scores[index]), 6),
            }
            for index in order[:3]
        ],
    }


def cosine(left: np.ndarray, right: np.ndarray) -> float:
    return float(left @ right)


def main() -> None:
    cases = base.load_json(CASES_PATH)
    documents = base.build_documents()
    available = {document.name for document in documents}
    expected = {
        group["target"] for group in cases["paraphraseGroups"]
    } | {
        side["expected"]
        for pair in cases["contrastPairs"]
        for side in (pair["left"], pair["right"])
    }
    missing = expected - available
    if missing:
        raise RuntimeError(f"Evaluation targets are absent from the current index: {sorted(missing)}")

    load_started = time.perf_counter()
    embedder = base.Embedder()
    model_load_ms = (time.perf_counter() - load_started) * 1000

    index_started = time.perf_counter()
    identity_vectors = embedder.encode([document.identity for document in documents])
    metadata_vectors = embedder.encode([document.summary for document in documents])
    index_ms = (time.perf_counter() - index_started) * 1000

    paraphrase_inputs = [
        (query, group["target"])
        for group in cases["paraphraseGroups"]
        for query in group["queries"]
    ]
    paraphrase_scores, paraphrase_ms = score_queries(
        embedder,
        documents,
        identity_vectors,
        metadata_vectors,
        [query for query, _ in paraphrase_inputs],
    )
    paraphrase_results = [
        rank_result(query, target, paraphrase_scores[index], documents)
        for index, (query, target) in enumerate(paraphrase_inputs)
    ]

    contrast_inputs = [
        (pair["name"], side_name, side["query"], side["expected"])
        for pair in cases["contrastPairs"]
        for side_name, side in (("left", pair["left"]), ("right", pair["right"]))
    ]
    contrast_scores, contrast_ms = score_queries(
        embedder,
        documents,
        identity_vectors,
        metadata_vectors,
        [query for _, _, query, _ in contrast_inputs],
    )
    flat_contrast_results = [
        {
            "pair": pair_name,
            "side": side_name,
            **rank_result(query, target, contrast_scores[index], documents),
        }
        for index, (pair_name, side_name, query, target) in enumerate(contrast_inputs)
    ]
    contrast_results = []
    for pair in cases["contrastPairs"]:
        sides = [result for result in flat_contrast_results if result["pair"] == pair["name"]]
        contrast_results.append(
            {
                "name": pair["name"],
                "left": next(result for result in sides if result["side"] == "left"),
                "right": next(result for result in sides if result["side"] == "right"),
                "bothTop1AndFlipped": all(result["rank"] == 1 for result in sides)
                and sides[0]["top3"][0]["name"] != sides[1]["top3"][0]["name"],
            }
        )

    probe_texts = [
        value
        for probe in cases["relationProbes"]
        for value in (probe["anchor"], probe["synonym"], probe["antonym"], probe["unrelated"])
    ]
    probe_vectors = embedder.encode(["query: " + text for text in probe_texts])
    probe_results = []
    for index, probe in enumerate(cases["relationProbes"]):
        anchor, synonym, antonym, unrelated = probe_vectors[index * 4 : index * 4 + 4]
        synonym_score = cosine(anchor, synonym)
        antonym_score = cosine(anchor, antonym)
        unrelated_score = cosine(anchor, unrelated)
        probe_results.append(
            {
                **probe,
                "synonymSimilarity": round(synonym_score, 6),
                "antonymSimilarity": round(antonym_score, 6),
                "unrelatedSimilarity": round(unrelated_score, 6),
                "synonymMinusAntonym": round(synonym_score - antonym_score, 6),
                "antonymMinusUnrelated": round(antonym_score - unrelated_score, 6),
                "synonymCloserThanAntonym": synonym_score > antonym_score,
                "antonymCloserThanUnrelated": antonym_score > unrelated_score,
            }
        )

    out_of_domain_scores, out_of_domain_ms = score_queries(
        embedder,
        documents,
        identity_vectors,
        metadata_vectors,
        cases["outOfDomainQueries"],
    )
    out_of_domain_results = []
    for query, scores in zip(cases["outOfDomainQueries"], out_of_domain_scores, strict=True):
        order = np.argsort(-scores)
        out_of_domain_results.append(
            {
                "query": query,
                "top1": documents[order[0]].name,
                "top1Score": round(float(scores[order[0]]), 6),
                "top2": documents[order[1]].name,
                "top2Score": round(float(scores[order[1]]), 6),
                "top1Margin": round(float(scores[order[0]] - scores[order[1]]), 6),
            }
        )

    retrieval_results = paraphrase_results + flat_contrast_results
    valid_top_scores = [result["top3"][0]["score"] for result in retrieval_results]
    valid_top_margins = [
        result["top3"][0]["score"] - result["top3"][1]["score"] for result in retrieval_results
    ]
    out_of_domain_top_scores = [result["top1Score"] for result in out_of_domain_results]
    out_of_domain_top_margins = [result["top1Margin"] for result in out_of_domain_results]
    report = {
        "model": "intfloat/multilingual-e5-small, QInt8 ONNX conversion",
        "scoring": "0.7 * identity cosine similarity + 0.3 * metadata cosine similarity",
        "documentCount": len(documents),
        "paraphraseQueryCount": len(paraphrase_results),
        "paraphraseTop1Accuracy": sum(result["rank"] == 1 for result in paraphrase_results)
        / len(paraphrase_results),
        "paraphraseTop3Accuracy": sum(result["rank"] <= 3 for result in paraphrase_results)
        / len(paraphrase_results),
        "contrastQueryCount": len(flat_contrast_results),
        "contrastTop1Accuracy": sum(result["rank"] == 1 for result in flat_contrast_results)
        / len(flat_contrast_results),
        "contrastTop3Accuracy": sum(result["rank"] <= 3 for result in flat_contrast_results)
        / len(flat_contrast_results),
        "contrastPairCount": len(contrast_results),
        "contrastPairFlipSuccess": sum(result["bothTop1AndFlipped"] for result in contrast_results)
        / len(contrast_results),
        "allRetrievalTop1Accuracy": sum(result["rank"] == 1 for result in retrieval_results)
        / len(retrieval_results),
        "allRetrievalTop3Accuracy": sum(result["rank"] <= 3 for result in retrieval_results)
        / len(retrieval_results),
        "randomTop1Baseline": 1 / len(documents),
        "randomTop3Baseline": min(3, len(documents)) / len(documents),
        "relationProbeCount": len(probe_results),
        "synonymCloserThanAntonymRate": sum(result["synonymCloserThanAntonym"] for result in probe_results)
        / len(probe_results),
        "antonymCloserThanUnrelatedRate": sum(result["antonymCloserThanUnrelated"] for result in probe_results)
        / len(probe_results),
        "meanSynonymSimilarity": sum(result["synonymSimilarity"] for result in probe_results)
        / len(probe_results),
        "meanAntonymSimilarity": sum(result["antonymSimilarity"] for result in probe_results)
        / len(probe_results),
        "meanUnrelatedSimilarity": sum(result["unrelatedSimilarity"] for result in probe_results)
        / len(probe_results),
        "outOfDomainQueryCount": len(out_of_domain_results),
        "meanValidQueryTop1Score": sum(valid_top_scores) / len(valid_top_scores),
        "meanOutOfDomainTop1Score": sum(out_of_domain_top_scores) / len(out_of_domain_top_scores),
        "meanValidQueryTop1Margin": sum(valid_top_margins) / len(valid_top_margins),
        "meanOutOfDomainTop1Margin": sum(out_of_domain_top_margins) / len(out_of_domain_top_margins),
        "modelLoadMs": model_load_ms,
        "indexEmbeddingMs": index_ms,
        "paraphraseBatchMs": paraphrase_ms,
        "contrastBatchMs": contrast_ms,
        "outOfDomainBatchMs": out_of_domain_ms,
        "paraphraseResults": paraphrase_results,
        "contrastResults": contrast_results,
        "relationProbeResults": probe_results,
        "outOfDomainResults": out_of_domain_results,
    }

    base.REPORT_ROOT.mkdir(parents=True, exist_ok=True)
    JSON_REPORT_PATH.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    lines = [
        "# BbxEditor 向量搜索稳健性评测",
        "",
        f"- 近义/改写查询：{len(paraphrase_results)}，Top-1 "
        f"{report['paraphraseTop1Accuracy']:.0%}，Top-3 {report['paraphraseTop3Accuracy']:.0%}",
        f"- 最小语义对照查询：{len(flat_contrast_results)}，Top-1 "
        f"{report['contrastTop1Accuracy']:.0%}，Top-3 {report['contrastTop3Accuracy']:.0%}",
        f"- 对照组两侧均正确翻转：{sum(result['bothTop1AndFlipped'] for result in contrast_results)}"
        f"/{len(contrast_results)} ({report['contrastPairFlipSuccess']:.0%})",
        f"- 所有检索查询：{len(retrieval_results)}，Top-1 {report['allRetrievalTop1Accuracy']:.0%}，"
        f"Top-3 {report['allRetrievalTop3Accuracy']:.0%}",
        f"- 24 选 1 随机基线：Top-1 {report['randomTop1Baseline']:.1%}，"
        f"Top-3 {report['randomTop3Baseline']:.1%}",
        f"- 词义探针均值：近义 {report['meanSynonymSimilarity']:.4f}，"
        f"反义 {report['meanAntonymSimilarity']:.4f}，无关 {report['meanUnrelatedSimilarity']:.4f}",
        f"- 近义比反义更近：{report['synonymCloserThanAntonymRate']:.0%}；"
        f"反义比无关词更近：{report['antonymCloserThanUnrelatedRate']:.0%}",
        f"- 有效查询 Top-1 均值：{report['meanValidQueryTop1Score']:.4f}；"
        f"域外查询 Top-1 均值：{report['meanOutOfDomainTop1Score']:.4f}",
        f"- 有效查询 Top-1/Top-2 间隔均值：{report['meanValidQueryTop1Margin']:.4f}；"
        f"域外查询间隔均值：{report['meanOutOfDomainTop1Margin']:.4f}",
        "",
        "## 近义、口语与英文改写",
        "",
        "| 查询 | 目标 | 排名 | 相似度 | Top-1 |",
        "|---|---|---:|---:|---|",
    ]
    for result in paraphrase_results:
        lines.append(
            f"| {result['query']} | `{result['expected']}` | {result['rank']} | "
            f"{result['targetScore']:.4f} | `{result['top3'][0]['name']}` |"
        )

    lines.extend(
        [
            "",
            "## 最小语义对照组",
            "",
            "| 对照 | 查询 | 目标 | 排名 | Top-1 |",
            "|---|---|---|---:|---|",
        ]
    )
    for pair in contrast_results:
        for side in (pair["left"], pair["right"]):
            lines.append(
                f"| {pair['name']} | {side['query']} | `{side['expected']}` | {side['rank']} | "
                f"`{side['top3'][0]['name']}` |"
            )

    lines.extend(
        [
            "",
            "## 近义词、反义词与无关词探针",
            "",
            "| 锚点 | 近义词 | 反义词 | 无关词 | 近义相似度 | 反义相似度 | 无关相似度 |",
            "|---|---|---|---|---:|---:|---:|",
        ]
    )
    for result in probe_results:
        lines.append(
            f"| {result['anchor']} | {result['synonym']} | {result['antonym']} | {result['unrelated']} | "
            f"{result['synonymSimilarity']:.4f} | {result['antonymSimilarity']:.4f} | "
            f"{result['unrelatedSimilarity']:.4f} |"
        )
    lines.extend(
        [
            "",
            "## 域外负样本",
            "",
            "| 查询 | 被迫返回的 Top-1 | 分数 | Top-1/Top-2 间隔 |",
            "|---|---|---:|---:|",
        ]
    )
    for result in out_of_domain_results:
        lines.append(
            f"| {result['query']} | `{result['top1']}` | {result['top1Score']:.4f} | "
            f"{result['top1Margin']:+.4f} |"
        )
    MARKDOWN_REPORT_PATH.write_text("\n".join(lines) + "\n", encoding="utf-8")

    print(json.dumps({key: value for key, value in report.items() if not key.endswith("Results")}, ensure_ascii=False, indent=2))
    print(f"report={MARKDOWN_REPORT_PATH}")


if __name__ == "__main__":
    main()
