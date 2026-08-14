---
name: task-doc-writer
description: 为 Task 节点、Context 和 Task 图编写文档时使用。
---

# Task 文档编写

本 skill 用于维护项目内 Task 系统的三类业务文档：Task 节点文档、TaskContext 文档、Task 图集文档。

文档只记录当前项目中已经存在或本次已经完成的事实，不记录尚未实现的预期方案。信息必须能从代码、Task json 配置、已有文档或本次修改中确认。

## 1. 概念对应关系

Task 设计中的概念按 `bbxcommon-task` 定义：

- `Task图集`：从业务功能角度归为同一组的一批 Task 图，例如一个完整技能、AI 行为、关卡触发流程或演出流程。
- `Task图`：一个可被 Task key 单独索引和运行的 Timeline 或行为树配置，对应同名 `.task.setting`、`.json`、`.editor.json`。
- `Task节点`：Task 图里的单个可配置执行单元。

Task 文档中的对应关系：

- `Task图集文档`：记录一个 Task 图集，一篇文档内可以包含入口图、关联图和复用图等多张 Task 图。
- `Task图集总文档`：记录每篇 Task 图集文档的入口 Task 图和外部调用来源。
- `通用子图总文档`：记录可跨多个图集复用的单张 Task 图；它不是 Task 图集文档。
- `Task节点文档`：记录一个 Task 节点类。
- `TaskContext文档`：记录一个 TaskContext 类。

不要把“Task 图集文档”和“Task 图”混用：`AutoDoc/Task/TaskGraph/` 下的一篇模块文档通常不是单张图，而是一组由入口图串起的 Task 图。

## 2. 文档分类

Task 文档分为三类，输出目录固定如下：

- 业务 Task 节点文档：`AutoDoc/Task/TaskNode/`
- 底层 Task 节点文档：`AutoDoc/Task/TaskNode/BbxCommon/`
- TaskContext 文档：`AutoDoc/Task/TaskContext/`
- Task 图集文档：`AutoDoc/Task/TaskGraph/`

节点文档必须按代码归属分目录：`BbxCommon` 命名空间下的底层或通用 Task 节点单独放入 `TaskNode/BbxCommon/` 子目录，业务命名空间节点仍放在 `TaskNode/` 根目录。不得把底层与业务节点文档混放；总文档中的节点文档路径必须填写相对于 `TaskNode/` 的路径。

Task 节点目录下必须额外维护一篇总文档，用于记录每个 Task 节点文档的相对路径、节点类全名、节点类型、用途分类和一句话用途说明。

TaskContext 目录下必须额外维护一篇总文档，用于记录每个 Context 文档名、Context 类全名、是否抽象、用途分类和一句话用途说明。

TaskContext 目录下还维护一篇通用子图总文档，用于记录可跨技能、AI、事件、演出或初始化流程复用的 Task 图。

Task 图集目录下必须额外维护一篇总文档，用于记录每个 Task 图集文档名、入口 Task 图和该图集的 Task 调用来源。

生成或更新文档时，只更新与当前任务相关的文档；不要为了补齐目录而创建空文档。

文档的详细格式分别见：

- [Task 节点文档说明](task-node-doc.md)
- [Task 节点总文档说明](task-node-index-doc.md)
- [TaskContext 文档说明](task-context-doc.md)
- [TaskContext 总文档说明](task-context-index-doc.md)
- [通用子图总文档说明](reusable-subgraph-index.md)
- [Task 图集文档说明](task-graph-doc.md)
- [Task 图集总文档说明](task-graph-index-doc.md)

## 3. 通用边界

不要记录：

- 尚未落地的未来节点、未来 Context、未来 Task 图或未来 Task 图集。
- 与 Task 无关的完整玩法设计。
- BbxCommon Task 底层实现细节；底层说明应放在 `bbxcommon-task` skill。
- 无法从代码、配置或本次修改确认的信息。
- 三类文档格式之外的额外章节。

## 4. 与 Task 图的关系

Task 图集文档以“Task 图集”为归档单位，而不是简单以单个 json 文件为单位。

一个 Task 图集通常从一个入口 Task 图开始运行；如果运行过程中会通过 `RunTask`、`TaskApi.RunTask` 或等价接口打开其它 Task 图，这些被打开的图属于同一篇 Task 图集文档的“关联图”。例如一个火球术入口图在命中和未命中时分别打开两个不同 Task 图，则该火球术图集文档需要同时记录入口图、命中图和未命中图。
