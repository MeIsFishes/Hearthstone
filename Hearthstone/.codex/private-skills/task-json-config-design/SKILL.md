---
name: task-json-config-design
description: 按给定节点配置 Task 图中间文件。
---

# Task Json 配置设计

本 skill 用于把已经完成节点设计的 Task 图集合设计文档转写成 `.task.setting` 中间文件，并通过项目提供的转换脚本生成运行时 `task.json` 和编辑器 `task.editor.json`。

本 skill 不负责设计新的 Task 节点、TaskContext、TaskCondition 或 Blackboard key。节点设计属于前置步骤；如果输入中缺少节点清单、字段定义、Context 字段或 Blackboard key，不要临时编造，应回到前置节点设计流程补齐。

执行前必须参考 `bbxcommon-task` skill 中的配置与反序列化说明，并读取本次任务涉及的 Task 节点文档、TaskContext 文档和 Task 图入口约束。本文只定义通用配置流程；具体格式见：

- [行为树配置设计](behavior-tree-config.md)：当 `.task.setting` 的 `TaskType` 为 `BehaviorTree` 时读取。
- [Timeline 配置设计](timeline-config.md)：当 `.task.setting` 的 `TaskType` 为 `Timeline` 时读取。

## 1. 输入前提

开始配置前必须已经获得：

1. 目标 Task key 或目标文件路径。若未单独提供 key，则 `.task.setting` 文件名去掉扩展名后即为 key。
2. Task 图类型：`BehaviorTree` 或 `Timeline`。
3. 绑定 Context 类型。按现有 Task 配置习惯，`.task.setting` 中写 Context 短类名，例如 `TaskContextActiveSkill`。
4. 给定节点清单：节点名、节点类型、字段列表和字段含义。
5. 节点之间的连接关系、条件引用或 Timeline 时间项。
6. 字段来源：`Value`、`Context` 或 `Blackboard`。
7. 所有 Context 字段和 Blackboard key 的来源说明。
8. 如果一次需求包含多张 Task 图，必须先获得 `AutoDoc/Temp/` 下的 Task 图集合设计文档，并从中读取每张图之间的入口、关联、后续关系。

若以上信息不足以配置，不要进入猜测式配置。

查阅现有 Task 文档时，按以下顺序读取：

1. 先读 `AutoDoc/Task/TaskNode/TaskNodeIndex.md`，确认可用 Task 节点、节点类全名和用途分类。
2. 需要确认字段名、字段类型、逻辑可空性或枚举含义时，再读 `AutoDoc/Task/TaskNode/` 下对应单节点文档。
3. 需要确认 `Context` 来源字段时，先读 `AutoDoc/Task/TaskContext/TaskContextIndex.md` 定位已绑定 Context 的文档，再读 `AutoDoc/Task/TaskContext/` 下对应单 Context 文档。
4. 修改现有 Task 图或复用现有设计时，先读 `AutoDoc/Task/TaskGraph/TaskGraphIndex.md`，再读对应 Task 图模块文档，确认入口图、关联图和调用来源。
5. 文档与配置不一致时，以当前 `.task.setting` 或由 `.json` 反向生成的 `.task.setting` 为配置修改基准，并在输出中说明需要同步文档。

## 2. 文件职责

每张 Task 图目前最多对应 3 个同名配置文件：

- `<TaskKey>.task.setting`：人工维护的 Task 图中间配置，也是 Codex 设计和修改 Task 图时优先编辑的文件。
- `<TaskKey>.json`：Unity 运行时读取的 Task 配置，表达 `BbxCommon.TaskGroupInfo`，由转换脚本从 `.task.setting` 生成。
- `<TaskKey>.editor.json`：Task 编辑器状态文件，保存编辑器需要的节点位置、连线表现、时间轴编辑数据等，由转换脚本生成。

只需要人工配置 `.task.setting` 的原因：

- `.task.setting` 用节点名、字段来源、连接关系和 Timeline 时间项表达设计意图，适合阅读和维护。
- `.json` 内部使用运行时 id、桥接结构和序列化格式，适合程序加载，不适合人工维护。
- `.editor.json` 是编辑器视图状态，修改它不能可靠改变运行时语义。
- 转换脚本会从 `.task.setting` 生成同 key 的 `.json` 与 `.editor.json`，避免三份文件语义漂移。

当前转换脚本位于：

```text
.codex/private-skills/task-json-config-design/ConvertTaskSetting.bat
```

脚本方向：

- `0`：`<TaskKey>.task.setting -> <TaskKey>.json` 和 `<TaskKey>.editor.json`。
- `1`：`<TaskKey>.json -> <TaskKey>.task.setting`。

除非用户明确要求临时修复生成物，否则不要手写或人工维护 `task.json`、`task.editor.json`。修改 Task 图时优先修改 `.task.setting`，再运行转换脚本刷新生成物。

## 3. 如何阅读 `.task.setting`

阅读 `.task.setting` 时，先看文件名、再看顶层、最后看节点：

1. 文件名去掉 `.task.setting` 后就是 Task key，例如 `NormalAttack.task.setting` 的 key 是 `NormalAttack`。
2. 看顶层 `TaskType`，判断这张图是 `BehaviorTree` 还是 `Timeline`。
3. 看顶层 `BindingContext`，确认这张图运行时使用哪个 Context 短类名。
4. 看顶层 `Root`，找到入口节点名。
5. 看 `Nodes` 中每个节点的 `Name` 和 `Type`：`Name` 是本文件内引用名，`Type` 是实际 Task 节点完整类型名。
6. 看每个节点的 `Fields`：每个字段都用 `Source` 和 `Value` 表示取值方式；`Value` 是常量，`Context` 是 Context 字段名，`Blackboard` 是 Blackboard key。
7. 如果是行为树，读 `ConnectPoints` 了解父子节点顺序，读 `Conditions.Enter/During/Exit` 了解条件引用。
8. 如果是 Timeline，读 Timeline 节点的 `TimelineItems`，按 `StartTime`、`Duration`、`Node` 理解时间轴启动顺序；相同开始时间按数组从上到下执行。
9. 看到节点名引用时，只在同一文件的 `Nodes` 内解析；看到 Task key 字段或 `TaskNodeRunTask` 时，再把它视为可能打开另一张 Task 图。

## 4. 总流程

新建 Task 图时按以下步骤执行：

1. 确认目标 `.task.setting` 路径和 Task key。
2. 确认 `TaskType` 是 `BehaviorTree` 还是 `Timeline`。
3. 读取 `AutoDoc/Temp/` 下本次 Task 图集合设计文档，确认该图与其它图的入口、关联和后续关系。
4. 读取本次涉及的节点文档、Context 文档和必要的 Task 图文档，确认字段名、字段类型和字段来源。
5. 按 `TaskType` 读取对应子文档。
6. 创建 `.task.setting` 中间文件。
7. 运行项目提供的 `.bat` 转换脚本：`ConvertTaskSetting.bat 0 <folder> <taskKey>`，生成同 key 的 `task.json` 和 `task.editor.json`。
8. 检查生成结果是否能表达同一张 Task 图。

修改现有 Task 图时按以下步骤执行：

1. 确认目标 Task key、配置目录和现有 `.json` 路径。
2. 如果存在同 key `.task.setting`，直接读取它作为修改基准。
3. 如果不存在同 key `.task.setting`，先运行 `ConvertTaskSetting.bat 1 <folder> <taskKey>`，从现有 `.json` 反向生成 `.task.setting`，再读取生成的中间文件作为修改基准。
4. 读取相关 Task 图集合设计文档、节点文档、Context 文档和 Task 图文档，确认现有图的入口、关联、字段来源和节点赋值。
5. 基于中间文件生成修改方案并记录到 `AutoDoc/Temp/`；修改方案必须列出要新增、删除或改动的节点、字段、连接、条件或 Timeline 时间项。
6. 只修改 `.task.setting` 中与修改方案相关的节点、字段、连接、条件或 Timeline 时间项。
7. 运行 `ConvertTaskSetting.bat 0 <folder> <taskKey>` 刷新同 key 的 `task.json` 和 `task.editor.json`。
8. 对比修改前后的 `.task.setting` 语义，确认入口、关联图、节点列表和字段来源符合修改方案。

如果转换脚本尚不存在或无法运行，只维护 `.task.setting`，并在输出中明确说明 `task.json` 与 `task.editor.json` 尚未刷新。

## 5. 通用 `.task.setting` 顶层字段

`.task.setting` 使用 JSON 格式，但扩展名固定为 `.task.setting`。

顶层必须包含：

- `TaskType`：`BehaviorTree` 或 `Timeline`。
- `BindingContext`：绑定 Context 的短类名。现有 `BindingContextFullType` 与 editor 的 `m_BindingContextType` 使用短类名，配置中不要改写成命名空间完整名。

行为树还必须包含：

- `Root`：根节点名。
- `Nodes`：节点列表。根节点也必须写在 `Nodes` 中。

Timeline 的必需字段见 Timeline 子文档。

不需要写 `TaskKey`。Task key 来自文件名。

不需要写 `Schema`。当前项目内格式由 `.task.setting` 扩展名和 `TaskType` 共同识别；以后需要兼容多版本时再考虑新增格式版本字段。

不需要写 `Editor`。编辑器数据由转换脚本生成。

## 6. 字段来源规则

每个普通字段都必须显式写出：

- `Source`：`Value`、`Context` 或 `Blackboard`。
- `Value`：固定值、Context 字段名或 Blackboard key。

规则：

- `Value` 来源可以使用 JSON 布尔值、数字、字符串或数组。
- `Source` 是 `ETaskFieldValueSource` 的枚举成员名，只写 `Value`、`Context`、`Blackboard`，不写枚举类型名。
- 节点字段如果是枚举，`Value` 只写枚举成员名，例如 `GreaterOrEqual`、`Strength`、`Intelligence`。
- 节点字段如果是枚举列表，`Value` 写 JSON 数组，数组元素仍然只写枚举成员名。
- 数组只表示列表常量，转换脚本负责转成底层 `%||%` 分隔格式。
- `Context` 来源的 `Value` 必须是绑定 Context 已注册字段。
- `Blackboard` 来源的 `Value` 必须是已确认存在写入来源的 key。
- 不要把连接点子节点写进普通字段；行为树连接点使用子文档定义的 `ConnectPoints`。
- 不要把条件节点放进普通连接点；条件引用使用子文档定义的 `Conditions`。

## 7. 校验清单

完成 `.task.setting` 后必须检查：

- `TaskType` 已明确。
- `BindingContext` 与启动入口创建的 Context 一致，且按现有配置习惯写短类名。
- 已读取相关 Task 节点和 Context 文档。
- 所有节点类型存在，且节点 `Type` 使用完整类型名。
- 所有字段 `Source` 和枚举字段值都只写枚举成员名。
- 所有字段名来自节点定义。
- 所有 `Context` 来源字段来自绑定 Context。
- 所有 `Blackboard` 来源 key 有明确写入来源。
- 所有节点引用、条件引用或 Timeline 引用都能解析到已声明节点。
- 没有把节点设计职责混入本配置步骤。
- 已说明转换脚本是否已运行，`task.json` 与 `task.editor.json` 是否已刷新。

## 8. 输出要求

完成配置设计或修改后，输出：

- 配置类型：`BehaviorTree` 或 `Timeline`。
- 来源设计文档路径，或说明本次是从既有 `.task.setting` / `.json` 反向生成结果开始修改。
- Task key 和 `.task.setting` 路径。
- `task.json` 路径与生成状态。
- `task.editor.json` 路径与生成状态。
- 绑定 Context。
- 入口图、关联图和后续图关系摘要。
- 根节点或 Timeline 根信息。
- 节点配置摘要。
- 字段来源摘要。
- 缺少的节点、Context 字段、Blackboard key 或转换脚本。
- 当前无法运行验证的风险。
