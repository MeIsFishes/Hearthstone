---
name: task-context-node-design
description: 设计 Task 图方案与缺失节点。
---

# Task 图与节点设计

本 skill 用于在编写 `.task.setting` 之前，先确定 Task 图怎么设计、现有节点是否够用、是否需要补充 Task 节点或 Context 字段。

执行时只分两步：

1. 读取 [设计阶段](design-stage.md)，完成 Task 图方案设计和缺失节点规格记录。
2. 读取 [输出阶段](output-stage.md)，把最终 Task 图方案写成 `AutoDoc/Temp/` 下的文本设计文档，交给 `task-json-config-design` 继续转写为 `.task.setting`。

如果设计过程中发现需要新增或修改 Task 节点、Context、条件节点或持续节点，先把缺失项写入临时设计文档；代码实现由主代理按 `task-workflow` 和 `bbxcommon-task` 继续处理。

如果设计中出现可复用子图，按 `task-doc-writer` 中的 [通用子图总文档](../task-doc-writer/reusable-subgraph-index.md) 维护索引。
