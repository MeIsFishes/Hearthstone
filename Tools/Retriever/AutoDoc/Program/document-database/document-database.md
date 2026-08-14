# 文档数据库

## 1. 模块说明

文档数据库使用 SQLite `catalog.db` 保存 Retriever 的权威文档状态，管理目录、文档、revision、全文档 chunk、active generation、重建 generation 和索引任务。

每篇 Markdown 固定对应一个 chunk。`chunk_key` 等于相对路径，`body` 保存未经切分的完整 Markdown，`content_revision` 由全文内容和索引格式指纹生成。旧字符分块配置仅兼容读取，不再影响索引。

Catalog 只决定哪些 revision 和 chunk 处于 active 状态，不保存 BM25 token、检索等价词或词向量。

## 2. 对外接口

- `Catalog.add_directory/list_directories/get_directory/remove_directory`：注册、读取和移除文档目录。
- `Catalog.prepare_revision/activate_revision/fail_revision`：准备、激活或标记失败的单文档 revision。
- `Catalog.begin_rebuild/stage_rebuild_document/generation_references/activate_rebuild/fail_rebuild`：暂存、校验并原子切换目录 generation。
- `Catalog.active_chunks/active_chunk_map/active_directory_references`：返回当前可搜索的全文档 chunk 和引用。
- `Catalog.delete_document/document_references/prune_inactive_revisions`：删除文档并清理旧 revision。
- `Catalog.create_task/finish_task/fail_task/recent_tasks`：记录索引任务阶段与错误。
- `RetrieverManager.index_file/index_directory/rebuild/clear/status`：应用层索引与维护入口。

## 3. 调用链路

增量写入：

```text
目录扫描或文件监听
-> 读取完整 Markdown
-> parse_markdown 生成唯一全文档 chunk
-> Catalog.prepare_revision
-> 写入 BM25 与文档词项
-> Catalog.activate_revision
-> 刷新活跃词表和词向量
-> 原子写入 document.md.vmeta
```

目录重建先写入未激活 generation，校验 Catalog 与 BM25 引用数量后切换 active generation，再刷新词表、清理旧搜索引用和旧 revision。切换前失败不会影响当前可搜索 generation。

## 4. 数据来源

- `storage_dir/manager/catalog.db`：Catalog 主数据库。
- `directories`、`documents`：目录、文档路径、修改时间、状态和 active revision。
- `document_revisions`、`chunks`、`document_revision_chunks`：revision、完整正文及映射。
- `rebuild_generations`、`rebuild_generation_documents`：目录重建状态。
- `index_tasks`：任务阶段、成功或失败状态及错误。
- Markdown UTF-8 源文件与相邻的 `document.md.vmeta`。
- `AppConfig.index_format_fingerprint()`：全文档算法版本和 BM25 分词配置的 SHA-256 指纹。
- `runtime/database.lock`：防止活动 Manager 使用期间物理删除数据库。

## 5. 与其他模块的依赖

- 依赖 `paths` 生成稳定相对路径，依赖 `vmeta` 保存源文件快照。
- Manager 协调 Catalog、BM25 活跃引用、词表和词向量。
- BM25 使用 Catalog 的 active 引用过滤可能残留的旧索引行。
- HTTP、CLI、MCP 与文件监听通过 Manager 访问，不直接修改 Catalog。
