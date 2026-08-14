# Retriever

Retriever 是一个本地 Markdown 关键词检索服务。每篇文档作为一个完整 chunk，文档召回和最终分数始终由 jieba + SQLite FTS5 BM25 产生；词向量用于索引后建立备选等价词，以及给 OOV 关键词寻找临时近邻。

## 开发环境

项目要求 Python 3.12：

```powershell
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -e ".[dev]"
.\.venv\Scripts\python.exe -m pytest -q
```

启用索引后备选等价词生成和 OOV 语义近邻，需要安装可选向量依赖：

```powershell
.\.venv\Scripts\python.exe -m pip install -e ".[vector]"
```

未配置 `embedding`、缺少可选依赖或模型加载失败时，服务会自动退化为 BM25，不影响已知关键词和已确认检索等价词。

## 最小使用流程

```powershell
.\.venv\Scripts\retriever.exe config init
.\.venv\Scripts\retriever.exe service start
.\.venv\Scripts\retriever.exe index build --directory "D:\Docs"
.\.venv\Scripts\retriever.exe search --keyword "角色技能" --keyword "冷却时间" --k 3
.\.venv\Scripts\retriever.exe service stop
```

旧的单个 `query` 参数仍然兼容。结构化关键词优先：

```json
POST /v1/search
{
  "keywords": ["大模型", "知识检索"],
  "k": 3,
  "directory": "D:\\Docs"
}
```

响应包含完整文档、各 BM25 通道贡献、OOV 状态、候选扩展和 `review_requests`。审核项来自命中文档中部分单词的备选等价词列表，并带有来源文档 chunk。Agent 判断后调用 `POST /v1/synonym-feedback`：一次肯定转入等价词列表；前两次否定留在备选列表，第三次转入非等价词列表；`unsure` 不计数。

## 索引和数据

每篇 Markdown 固定对应一个 chunk，`chunk_key` 为相对路径。旧 `max_chars`、`overlap_chars` 和 `preserve_english_words` 配置仍可读取，但不再影响索引。

BM25 写入同时维护 active 词表和以单词为主记录的三列表等价词表。索引完成后，系统使用已存词向量主动计算近邻并填充备选列表；词向量保存在 `search/term-vectors/`，不生成标题、正文或其他文档向量。

`GET /v1/equivalence-terms` 返回每个词的 `equivalent_terms`、`candidate_equivalent_terms` 和 `non_equivalent_terms`。兼容接口 `GET /v1/synonyms` 同时返回相同结构。

`retriever index rebuild --directory "D:\Docs"` 会建立并校验未激活 generation，再原子切换整个目录。索引格式版本变化后，下一次目录扫描会重建旧文档。

## MCP

```powershell
.\.venv\Scripts\retriever.exe mcp --config retriever.json
```

MCP 暴露两个工具：

- `search_documents`：使用 `keywords` 搜索完整文档并返回审核请求。
- `submit_synonym_feedback`：提交 `equivalent`、`not_equivalent` 或 `unsure`。

## 关系管理

```powershell
.\.venv\Scripts\retriever.exe synonym list --status accepted
.\.venv\Scripts\retriever.exe synonym feedback --search-id "..." --directory-id 1 --query-term "大模型" --candidate-term "语言模型" --verdict equivalent
.\.venv\Scripts\retriever.exe synonym reset --directory-id 1 --first-term "大模型" --second-term "语言模型"
```

## 文档

- [项目总览](AutoDoc/Program/project-overview.md)
- [文档数据库](AutoDoc/Program/document-database/document-database.md)
- [BM25](AutoDoc/Program/bm25/bm25.md)
- [向量搜索](AutoDoc/Program/vector-search/vector-search.md)
- [向量数据库](AutoDoc/Program/vector-database/vector-database.md)
