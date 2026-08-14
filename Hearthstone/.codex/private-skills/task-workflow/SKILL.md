---
name: task-workflow
description: 设计 Task 图集或编写 Task 节点的开发流程。
---

# Task 工作流

当用户要求设计、创建、修改、接入或部署 Task 时，主代理使用本 skill 作为流程入口。Task 图设计、代码确认、节点实现、配置转换和文档维护都由主代理推进；不要把 `task_checker` 当作 Task 图集设计或实现入口，它只用于准备新增节点前的现有节点方案检查。

本 skill 只规定主流程和责任边界。具体概念、设计格式、配置格式、转换脚本用法和文档格式，进入对应 skill 后按其说明执行，不在这里重复展开。

## 1. 相关知识索引

根据传入的需求，按需阅读以下 skill 文档：

- `task-context-node-design`：`.codex/private-skills/task-context-node-design/SKILL.md`，用于设计 Task 图集方案、判断图拆分、选择 Timeline/行为树并记录缺失节点规格。
- `task-json-config-design`：`.codex/private-skills/task-json-config-design/SKILL.md`，用于创建或修改 `.task.setting`，以及执行正向/反向转换。
- `task-doc-writer`：`.codex/private-skills/task-doc-writer/SKILL.md`，用于维护 Task 节点、TaskContext、Task 图集和通用子图文档。
- `bbxcommon-task`：`.codex/private-skills/bbxcommon-task/SKILL.md`，用于了解 Task 节点代码模板、生命周期和底层 Task 使用方式。

## 2. 允许范围

读代码时，如果发现 Task 节点、Context、Task 图配置或运行入口的代码事实与现有文档不符，可以即时修正文档。文档修正仍要遵循 `task-doc-writer` 的格式，并只修正能从代码、配置或本次修改确认的事实。

## 3. 设计并实现 Task 图集流程

将以下条目加入当前任务检查清单，供结束审计逐项复核；不以清单限制实际执行顺序：

1. 读取本 skill 的相关知识索引中必要的 Task skill 和正式文档，确认当前任务涉及的 Task 图集和 Context。
2. 明确本次需求对应哪些 Task 图集；为每个图集确定入口 Task 图、绑定 `TaskContext`、调用来源和配置目录。

随后对于每一个 Task 图集，把以下要求作为子检查项；实际工作按配置依赖组织，不因编号逐项暂停：

1. 如果修改已有 Task 图，先用 .bat 文件把配置文件反向转为 `.task.setting`；如果是新建 Task 图，则先确定目标 Task key 和输出路径。
2. 按 `task-context-node-design` 设计 Task 图集方案，并把方案输出到 `AutoDoc/Temp/`。设计时需要能说明每张图的类型、节点、结构或时间项、字段赋值和跨图调用。
3. 如果发现有游戏逻辑端暂未支持的功能，则回到游戏逻辑端支持该项功能。如果该逻辑功能较复杂，则须按照 `project-state-preflight` 的流程进行完整实现。
4. 如果准备新增或修改 Task 节点，先启动 `task_checker`，让它只阅读当前 Task 文档，检查是否存在不新增节点、只用现有节点拼出需求的实现方式。子代理返回后不要盲目采用其方案；主代理需要比较现有节点拼接方案与新增节点方案的合理性、可读性、复用性和维护成本，尤其是新增节点可能只在这一处使用时，应倾向于优先考虑现有节点方案。
5. 如设计发现现有节点、Context、条件或持续节点仍不足，先记录缺失规格，再按 `bbxcommon-task` 的模板补代码，并按 `task-doc-writer` 更新对应正式文档。
6. 如果实际设计中还有缺漏，则重复步骤 2-5，直到设计方案完整且可行。
7. 将设计方案按 `task-json-config-design` 转写或修改为 `.task.setting`，并执行该 skill 规定的转换与校验。
8. 按 `task-doc-writer` 更新 Task 图集、Task 节点、TaskContext、通用子图和相关索引文档。

## 4. 只修改或新增 Task 节点流程

当需求只涉及修改或新增 Task 节点，不需要设计或调整 Task 图集时，将以下条目加入当前任务检查清单，供结束审计逐项复核：

1. 读取 `bbxcommon-task` 中对应的 Task 节点代码模板和生命周期说明，确认应继承 `TaskBase`、`TaskOnceBase`、`TaskDurationBase` 还是 `TaskConditionBase`。
2. 编写或修改 Task 节点代码，并在代码事实与现有节点文档不一致时同步修正文档。
3. 按 `task-doc-writer` 为该 Task 节点新增或更新正式文档，并维护 Task 节点总文档。
