---
name: bbxcommon-task
description: 说明 BbxCommon Task 的使用、节点模板与底层流程。
---

# BbxCommon Task

## 1. 模块概览

Task 的定义是执行，并以一个确定条件结束的托管逻辑。它的驱动方式包括 Timeline 、行为树等。而 Timeline 和行为树本身也是一种 Task （例如 Timeline 可以理解为一种在确定时间点结束的逻辑）。

为了复用，行为树和 Timeline 里的节点也都是 Task 。

本项目中常用三个层级描述 Task 设计：

- `Task图集`：从业务功能角度归为同一组的一批 Task 图。一个完整技能、一个完整 AI 行为、一个关卡触发流程或一段演出流程，通常都是一个 Task 图集。图集可以只有一张入口图，也可以包含入口图、RunTask 打开的关联图和可复用子图。
- `Task图`：一个可被 Task key 单独索引和运行的 Timeline 或行为树配置。一个 Task 图对应一组同名配置文件，运行时从入口根节点开始执行。
- `Task节点`：Task 图里的单个可配置执行单元。节点可以是行为树驱动节点、Timeline 节点、一次性动作、持续动作、条件节点或业务动作节点；每个节点通过字段从固定值、Context 或 Blackboard 读取输入。

设计时先判断 Task 图集，再拆出其中的 Task 图，最后为每张 Task 图选择和配置 Task 节点。

业务侧在以下场景优先考虑 Task：

- 需要把多个动作按时间、顺序、选择、循环或并行组合起来。
- 需要让配置或编辑器决定逻辑图，代码只提供可复用节点。
- 需要在能力、AI、流程事件、演出等系统中复用同一套运行图能力。
- 需要把运行时输入与逻辑图解耦，用 `TaskContextBase` 或 Blackboard 注入本次执行数据。

模块边界：

- Task 负责组织和驱动托管逻辑，不负责保存长期业务状态。长期状态通常放在 ECS RawComponent、配置数据或其它业务模块里。
- Task 可以读写 ECS、资源、UI 或业务数据，但不应绕过这些模块已有 API 去维护底层状态。
- Task 的运行由框架统一驱动，业务侧通常只需要创建 Context、按 key 创建 Task，然后启动根节点。

更多底层说明：

- [运行时生命周期](developer-docs/runtime-lifecycle.md)
- [配置与反序列化](developer-docs/config-and-deserialization.md)
- [驱动节点](developer-docs/drive-nodes.md)
- [导出与编辑器数据](developer-docs/export-and-editor.md)

## 2. API接口

业务代码优先通过 `TaskApi` 使用 Task。其它框架内部细节在主流程中不要直接访问。

最简 Task 业务逻辑由 Context、Task key 和 Task 图组成：

- `TaskContextBase` 保存本次执行需要的输入，同一张 Task 图可以用不同 Context 重复运行。
- Task key 用来选择已经配置好的 Task 图，图中的节点共享本次 Context，并可通过 Blackboard 传递运行时数据。
- `TaskApi.RunTask(...)` 创建并启动根 Task，框架持续驱动节点，直到根 Task 成功或失败。
- 业务侧需要感知整张图结束时，通过返回的 `TaskBase.OnFinished` 接收完成通知；长期业务状态仍应保存在 ECS 或其它业务模块中。

这个模型主要使用以下接口：

- `TaskApi.CreateContext<T>() where T : TaskContextBase, new()`：从对象池创建本次运行的 Context。
- `TaskApi.RunTask(string key, TaskContextBase context)`：根据 key 和 Context 启动根 Task，并返回该根 Task。
- `TaskContextBase.ApplyBlackBoardInjection(TaskBlackboardInjection)`：把 CSV 单单元格声明的一组有类型初始值一次写入 Blackboard。
- `TaskBase.OnFinished`：接收根 Task 的完成通知。

最小启动示例见 [run-skill.md](code-example/run-skill.md)。

运行前置条件：

- `key` 必须能索引到一个 Task 配置；使用配置文件时，传入不含扩展名的文件名。
- 传入的 Context 类型必须与 Task 配置绑定的 Context 类型一致。
- Task 节点字段、Context 字段和 Blackboard key 必须与 Task 模板中配置的字段来源匹配。

## 3. 业务类

业务侧通常需要按职责新建以下类：

- `TaskBase` 子类：普通节点，适合需要 `OnEnter`、每帧 `OnUpdate`、`OnExit` 的行为。模板见 [task-node.md](new-class-examples/task-node.md)。
- `TaskOnceBase` 子类：一次性节点，适合立即结算伤害、治疗、写 Blackboard、派发请求等行为。模板见 [task-once.md](new-class-examples/task-once.md)。
- `TaskDurationBase` 子类：持续节点，适合按 `Duration` 和 `Interval` 周期执行的行为。模板见 [task-duration.md](new-class-examples/task-duration.md)。
- `TaskConditionBase` 子类：条件节点，挂在其它 Task 的 EnterCondition、Condition 或 ExitCondition 上。模板见 [task-condition.md](new-class-examples/task-condition.md)。
- `TaskContextBase` 子类：一次 Task 运行的输入容器，保存本次运行的实体、事件、资源 key、触发源等上下文。模板见 [task-context.md](new-class-examples/task-context.md)。

## 4. 主要类的生命周期

### 4.1 TaskContextBase

- 创建：通过 `TaskApi.CreateContext<T>()` 从对象池分配。
- 填充：启动 Task 前写入本次运行需要的 Context 字段；这些字段适合作为稳定输入。
- 使用：Task 节点字段可以从 Context 读取固定输入，也可以从 Context 的 Blackboard 读取动态参数。
- 回收：Task 结束后不会自动回收外部持有的 Context；业务侧若创建了 Context，需要在合适时机回池或由上层生命周期统一管理。

### 4.2 TaskNode

- 创建：通过配置文件创建根 Task 时，框架按配置实例化所需节点。
- 字段读入：节点字段可来自固定值、Context 或 Blackboard；Blackboard 来源适合动态参数。
- 运行：节点进入后执行自身生命周期；普通节点可持续运行，一次性节点会立即结束，持续节点会按时间和间隔执行。
- 结束：节点返回成功或失败后退出；根 Task 结束时触发 `OnFinished`。
- 清理：业务节点必须在对应回收钩子里清空字段、集合、缓存引用和对象池对象。
