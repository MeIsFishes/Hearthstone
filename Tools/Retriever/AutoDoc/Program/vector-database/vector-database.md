# 向量数据库

## 1. 模块说明

向量数据库使用 LanceDB `term_vectors` 表保存 BM25 active 词表的词级向量。每行包含目录 ID、标准化词项、embedding 指纹和向量，不保存 chunk key、标题、正文或文档向量。

目录词表刷新时只编码新增词，删除已退出 active 词表的词，并复用模型指纹一致的已有向量。不同模型名称使用不同持久化子目录；embedding 指纹还包含模型版本、维度和归一化策略。

LanceDB 是可选能力。初始化、同步或查询失败时，Manager 记录错误但不阻断 BM25 索引和搜索。

## 2. 对外接口

- `TermVectorSearch(database_dir, embedder, batch_size)`：连接 LanceDB 并确保 `term_vectors` 存在。
- `TermVectorSearch.embedding_fingerprint`：返回当前模型和维度的 SHA-256 指纹。
- `TermVectorSearch.replace_directory(directory_id, terms)`：增量编码新增词并删除失效词。
- `TermVectorSearch.nearest(text, directory_ids, limit, similarity_threshold)`：按目录执行 cosine 近邻查询并返回 similarity。
- `TermVectorSearch.candidate_pairs(directory_id, limit_per_term, similarity_threshold)`：使用表内已有向量为全部 active 词生成去重近邻词对。
- `TermVectorSearch.clear(directory_ids)`：按目录或全库清空词向量。
- `TermVectorSearch.count()`：返回当前词向量行数。
- `TermVectorIndex`：供 Manager 与测试替换实现使用的协议。

## 3. 调用链路

写入：

```text
Catalog 激活文档或 generation
-> BM25.refresh_lexicon
-> 选择 vector_eligible 词项
-> TermVectorSearch.replace_directory
-> 只编码缺失词
-> 写入 term_vectors
-> candidate_pairs 使用已存向量计算近邻词对
-> BM25 备选等价词列表
```

查询：

```text
未解决的 OOV 关键词
-> Embedder.encode
-> term_vectors cosine search
-> directory_id 和 embedding_fingerprint 预过滤
-> distance 转 similarity
-> 阈值过滤并返回近邻词
```

## 4. 数据来源

- `storage_dir/search/term-vectors/<model-key>/`：当前词向量持久化目录。
- `term_vectors`：`directory_id`、`term`、`embedding_fingerprint`、固定维度 `float32 vector`。
- BM25 `lexicon_terms.vector_eligible=1` 的目录词项。
- Embedder 的模型版本、维度和归一化输出。
- 旧 `storage_dir/search/vector/` 文档向量目录不再读写，可在确认无需回滚后人工删除。

## 5. 与其他模块的依赖

- 依赖可选的 LanceDB 和 PyArrow。
- 依赖向量搜索模块的 Embedder 生成词向量。
- 依赖 BM25 active 词表决定应存在的词项。
- 不依赖文档正文，也不决定文档得分；向量查询结果必须回到 BM25 才能召回文档。
