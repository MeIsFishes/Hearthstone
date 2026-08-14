# 驱动节点

## TaskTimeline

`TaskTimeline` 自身是一个 Task，用时间驱动子 Task。

关键配置：

- `Duration`：Timeline 总时长。小于 0 表示不因总时长结束。
- `TaskTimelineItemInfo.StartTime`：子 Task 开始时间。
- `TaskTimelineItemInfo.Duration`：子 Task 最长持续时间。小于 0 表示不由 Timeline 强制结束。
- `TaskTimelineItemInfo.Id`：子 Task 引用 id。

运行行为：

1. Timeline 进入后从时间 0 启动所有 `StartTime <= 0` 的子 Task。
2. 每帧推进已运行时间。
3. 到达子项开始时间时，调用子 Task `Enter()`。
4. 子 Task 自行成功或失败时退出。
5. 子项持续时间到达时，Timeline 会让子 Task 退出。
6. Timeline 总时长到达时返回成功。

适合用于演出流程、延迟结算、多段事件和并行表现。

## TaskBtRoot

`TaskBtRoot` 是行为树根节点风格的驱动节点。它持有一个 `TaskConnectPoint`，当前实现只驱动第一个子 Task。

适合让编辑器或配置工具以统一根节点包装行为树。

## TaskNodeSequence

`TaskNodeSequence` 按顺序执行多个子 Task。

运行行为：

- 进入时启动第一个子节点。
- 当前子节点返回 `Running` 时，Sequence 保持运行。
- 当前子节点结束后，启动下一个子节点。
- 同一次父节点更新内，当前子节点可使用本帧 `deltaTime`；随后新进入的子节点仍可立即更新，但只接收 `0`，避免同一帧时间被重复累计。
- 当前子节点失败时，Sequence 立即失败。
- 所有子节点结束后，Sequence 成功。

## TaskNodeSelector

`TaskNodeSelector` 从子节点中选择第一个 `CanEnter()` 成功的节点运行。

运行行为：

- 进入时依次检查子节点 EnterCondition。
- 第一个可进入节点会被 Enter。
- 如果没有可进入节点，Selector 更新时失败。
- 选中的子节点运行中则 Selector 运行中；选中节点成功则 Selector 成功。
- 选中节点失败时继续尝试下一个可进入节点；同一次父节点更新内的新分支只接收 `0`，不重复消费前一分支已经使用的本帧 `deltaTime`。
- 所有候选节点均失败或不可进入时，Selector 失败。

适合配置“从多个可选分支里选一个能进入的分支”。

## TaskNodeParallel

`TaskNodeParallel` 用于并行更新多个子 Task。

运行行为：

- 进入时应让所有子 Task 进入。
- 更新时逐个更新所有子 Task。
- 所有子 Task 都不再运行时，Parallel 成功。

当前代码的 `OnEnter()` 循环中调用的是 `Tasks.Tasks[0].Enter()`，而不是 `Tasks.Tasks[i].Enter()`。如果业务需要依赖该节点，建议先修正并验证。

## TaskNodeLoop

`TaskNodeLoop` 循环执行第一个子 Task。

关键配置：

- `LoopCount`：循环次数。小于 0 表示无限循环。

运行行为：

- 进入时启动子 Task。
- 子 Task 运行中则 Loop 运行中。
- 子 Task 结束后计数加一。
- 未达到循环次数则重新 Enter 子 Task。
- 新一轮子 Task 可在同一次父节点更新内立即执行，但只接收 `0`，不会再次消费本帧 `deltaTime`。
- 无限循环若连接全即时成功子树，每次父节点更新只完成一轮并返回 `Running`，避免单帧死循环。
- 达到循环次数后成功。

当前代码的 `OnEnter()` 循环同样重复 Enter 第一个子 Task。该节点语义上主要使用第一个子 Task，配置多个子 Task 时需要谨慎。

## TaskNodeTimer

`TaskNodeTimer` 用时间限制第一个子 Task。

关键配置：

- `Duration`：最大运行时间。
- `Tasks`：子 Task 连接点。

运行行为：

- 子 Task 先结束，则 Timer 成功。
- 计时达到 `Duration`，Timer 成功。
- 否则保持运行。

当前代码的 `OnEnter()` 循环也重复 Enter 第一个子 Task。使用前建议确认配置只连接一个子节点。

## TaskNodeWaitForTime

`TaskNodeWaitForTime` 在节点进入时把已等待时间重置为 0，并在更新中累计 `deltaTime`，达到配置的 `Time` 后成功。

- `Time`：本次进入节点需要等待的秒数；小于或等于 0 时首次更新即成功。
- 同一节点实例被 `TaskNodeLoop` 重复 Enter 时，每轮都会重新完整等待，不会沿用上一轮的累计时间。

## TaskConnectPoint

`TaskConnectPoint` 保存子 Task 引用。

- `Tasks`：运行时子 Task 实例。
- `TaskRefrenceIds`：反序列化阶段临时保存的节点引用 id。
- `ConnectPointType`：导出给编辑器的连接点类型，可区分 Single 和 Multiple。

驱动节点通过 `ReadConnectPoint()` 读取连接点，所有 Task 实例创建完后再统一把 id 转成节点实例引用。
