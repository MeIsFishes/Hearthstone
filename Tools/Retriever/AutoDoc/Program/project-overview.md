# Retriever 项目总览

Retriever 是一个面向本地 Markdown 文档的持久化关键词检索服务。每篇文档作为一个完整检索单元，使用 jieba 和 SQLite FTS5 BM25 建立索引，并通过 HTTP、CLI 与 MCP 提供搜索和索引管理能力。

项目包含以下系统：

- [文档数据库](document-database/document-database.md)：管理目录、全文档 chunk、revision、generation 和任务状态。
- [BM25](bm25/bm25.md)：维护倒排索引、活跃词表和目录级检索等价词，并负责全部文档召回与最终计分。
- [向量搜索](vector-search/vector-search.md)：索引后为词表主动生成近邻候选，并在关键词 OOV 时提供临时近邻扩展。
- [向量数据库](vector-database/vector-database.md)：使用 LanceDB 保存活跃词表的词级向量，不保存标题或正文向量。

主要功能包括全文档增量索引、generation 原子重建、结构化关键词搜索、每词三列表等价词表、命中文档候选抽样审核、OOV 近邻扩展、一次确认接受和三次独立否决、向量故障降级，以及可解释的搜索结果。
