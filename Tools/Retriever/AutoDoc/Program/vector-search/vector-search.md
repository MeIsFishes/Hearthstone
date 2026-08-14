# 向量搜索

## 1. 模块说明

向量搜索只处理词汇，不处理文档。索引完成后，系统为 BM25 active 词表中的合格词生成归一化 embedding，并主动为每个词计算近邻，把词对写入双方的备选等价词列表。

搜索完成后，系统只从命中文档实际包含的词中抽取部分备选词对，生成带文档 chunk 证据的 `review_requests`。Agent 根据当前文档目录的检索语境提交 `equivalent`、`not_equivalent` 或 `unsure`。已确认等价或非等价的词对不会再次进入备选或再次询问。

指定目录中不存在的 OOV 关键词仍可在查询时生成向量、找到词表近邻并低权重参与本次 BM25；该词对同时加入三列表模型，只有产生命中文档后才可能被审核。

模型或向量数据库不可用时，系统返回告警并继续执行原始关键词和已确认等价词的 BM25 搜索。

## 2. 对外接口

- `Embedder.dimension/version/encode`：统一的词向量编码协议。
- `SentenceTransformerEmbedder`：生产语义模型实现，输出归一化向量。
- `HashingEmbedder`：测试用确定性向量，不提供生产语义质量。
- `TermVectorSearch.candidate_pairs`：使用已存词向量批量生成索引后近邻词对，不重复编码词汇。
- `RetrieverManager.search_detailed`：执行搜索后，从命中文档词项的备选列表构造审核请求。
- `RetrieverManager.submit_synonym_feedback`：提交幂等 Agent 判断。
- HTTP `POST /v1/search`、`POST /v1/synonym-feedback`：搜索与反馈入口。
- MCP `search_documents`、`submit_synonym_feedback`：Agent 搜索和反馈工具。

## 3. 调用链路

```text
索引完成 -> active 词向量近邻 -> 双向备选等价词列表
关键词搜索 -> exact/accepted/OOV provisional BM25 -> 完整文档结果
命中文档的 document_terms -> 对应单词备选列表 -> 限量 review_requests
Agent 反馈 -> 转入等价词列表，或累计三次后转入非等价词列表
```

默认每个词保留最多 3 个索引近邻，每次搜索最多返回 5 个审核词对，相似度阈值为 `0.72`，审核冷却为 24 小时。

## 4. 数据来源

- `EmbeddingConfig`：模型名称、设备和批大小；配置为 `null` 时禁用词向量。
- `RETRIEVER_MODEL_DIR`：本地模型目录覆盖。
- 冻结程序内的 `models/<model-name>` 模型目录。
- BM25 active 词表、三列表等价词表、命中文档词项和目录 ID。
- LanceDB 返回的词项、目录 ID 和 cosine similarity。
- 搜索请求的 `query` 或 `keywords`。

## 5. 与其他模块的依赖

- 生产模型依赖可选的 sentence-transformers 与 PyTorch。
- 依赖向量数据库保存和查询词级 embedding。
- 依赖 BM25 判断 OOV、执行全部文档召回并保存反馈。
- 不读取或生成标题向量、正文向量，也不参与文档向量排序。
