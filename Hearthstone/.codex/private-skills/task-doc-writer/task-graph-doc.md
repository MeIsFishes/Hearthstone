# Task 图集文档说明

Task 图集文档输出到：AutoDoc/Task/TaskGraph/

Task 图集文档以一个 Task 图集为一篇文档。Task 图集是从游戏功能视角能被理解为同一个流程的一组 Task 图，例如一个技能、一个关卡事件、一个 AI 行为、一个演出流程或一次初始化流程。

Task 图集名不要求和业务场景一一对应。`AutoDoc/Task/TaskGraph/` 下除图集文档外，还必须维护一篇总文档，用于记录每个 Task 图集文档名、入口 Task 图与 Task 调用来源。总文档格式见 [Task 图集总文档说明](task-graph-index-doc.md)。

Task 图集文档只允许包含以下章节：

1. Task图集概览
2. 入口图与关联图

## 1. Task图集概览

本章节从 Task 图集角度概述这组 Task 图做什么。

必须记录：

- 章节开头必须先列出入口图，格式为 `入口图：TaskKey`。
- 入口 Task 图的 Task key 和 json 路径。
- Task 图集代表的游戏功能。
- 绑定的 TaskContext 类型。
- 整体流程的主要分支、结束条件和对外可观察结果。

可以记录：

- 该图集是否会根据命中、未命中、成功、失败、阶段切换、条件判断等分支进入不同关联图。

不要记录：

- 每个节点的完整字段表；节点字段应写在 Task 节点文档。
- Context 每个字段的完整定义；Context 字段应写在 TaskContext 文档。
- 与本 Task 图集无关的玩法设计或未来计划。

## 2. 入口图与关联图

本章节列出同一 Task 图集内的所有 Task 图，包括入口图和运行中通过 `RunTask`、`TaskApi.RunTask` 或等价接口打开的关联图。

必须先标明入口图，再列出关联图。推荐格式：

```markdown
### 入口图：TaskKey

- json 路径：`...`
- 驱动类型：Timeline / 行为树
- 图内职责：...
- 节点摘要：...
- 字段来源摘要：...
- Blackboard来源摘要：...
- 可能打开的关联图：...

### 关联图：TaskKey

- json 路径：`...`
- 驱动类型：Timeline / 行为树
- 进入条件：...
- 图内职责：...
- 节点摘要：...
- 字段来源摘要：...
- Blackboard来源摘要：...
- 后续图：...
```

关联规则：

- 一个 Task 图集从一个入口图开始；入口图是外部游戏功能直接启动的 Task 图。
- 入口图运行过程中打开的其它 Task 图，属于同一篇文档的关联图。
- 关联图继续打开的 Task 图，也属于同一篇文档，直到该 Task 图集的 Task 图链路结束。
- 同一个 Task 图如果被多个 Task 图集复用，应在每篇相关图集文档中说明它在该 Task 图集内的职责；不要把无关图集合并成一篇大文档。
- 如果配置中存在 Task key 字段但当前无法确认是否会被 `RunTask` 打开，必须标记为“待确认关联”，不要直接归入已确认关联图。
- 举例：火球术图集有一个入口图，然后命中、未命中时分别有一张子 Task 图，应全部记录在同一篇图集文档中，并写清分支条件。

图列表中每个 Task key 都必须能对应到当前项目中的 Task json 配置。

为了让后续设计者不阅读代码也能复用或仿照现有图，每张图必须记录：

- `节点摘要`：列出本图主要节点名、节点类型和一句话用途；不需要展开节点字段定义。
- `字段来源摘要`：列出本图用到的关键字段赋值，例如 `PlayAnimation.Target <- Context.CasterEntityId`、`TakeDamage.DamageAttributes <- Value.[Intelligence]`。
- `Blackboard来源摘要`：列出本图读取或依赖的 Blackboard key 及其写入来源；如果未读取 Blackboard，写“未读取 Blackboard”；如果来源无法确认，写“待确认”。

驱动类型判断规则：

- 根节点或主要流程由 `TaskTimeline` 驱动时，写 `Timeline`。
- 根节点或主要流程由 `TaskBtRoot`、`TaskNodeSequence`、`TaskNodeSelector`、`TaskNodeParallel`、`TaskNodeLoop` 等行为树/组合节点驱动时，写 `行为树`。
- 如果同一 Task 图混合使用两类驱动，以入口根流程为准；无法确认时写 `待确认`。

通常文件夹下会有同名的多个 Task 配置：

*.task.setting
*.json
*.editor.json

这些文件是同一个 Task 图在不同阶段的配置文件，只需记录扩展名之前的文件名作为 Task 图名称和路径标准。
