# 运行时生命周期

## 入口

业务侧通过 `TaskApi.CreateTask()`、`TaskApi.RunTask()` 或 `TaskBase.Run()` 把 Task 加入运行队列。

内部入口是 `TaskManager`：

- `NewEnterTasks`：当帧新加入、尚未 Enter 的 Task。
- `RunningTasks`：已经进入并持续运行的 Task。
- `m_Tasks`：按 key 缓存的 Task 桥接模板。

业务代码不直接访问 `TaskManager`。

## GameEngine 注册 TaskSystem

GameEngine 内部 Stage 会调用：

```csharp
stage.AddGameEngineLateUpdateSystem<TaskSystem>();
```

因此 TaskSystem 运行在 GameEngine late update 侧。业务 Stage 不需要自己注册 `TaskSystem`。

## 每帧驱动顺序

`TaskSystem.OnSystemUpdate()` 的核心顺序是：

1. 遍历 `NewEnterTasks`，按加入顺序调用 `task.Enter()`。
2. 把这些 Task 加入 `RunningTasks`，状态标记为 `NewEnter`。
3. 遍历 `RunningTasks`：
   - `NewEnter` 首帧调用 `task.Update(0)`。
   - 后续帧调用 `task.Update(TimeApi.DeltaTime)`。
4. 记录本帧返回 `Succeeded` 或 `Failed` 的 Task 下标。
5. 对结束 Task 按加入顺序调用 `Exit()`。
6. 对结束 Task 按加入顺序调用 `OnFinished`。
7. 将结束根 Task 及其本次配置图创建的全部子节点回收到各自对象池，并从 `RunningTasks` 移除。

## TaskBase 内部生命周期

`TaskBase.Enter()`：

1. 状态置为 `Running`。
2. 逐个执行 EnterCondition。任一失败则当前 Task 失败，且不执行 `OnEnter()`。
3. 对普通 Condition 执行 `Enter()`。
4. 读取 Blackboard 来源字段。Blackboard 可能在运行期间变化，所以每次进入都重新读。
5. 调用业务重写的 `OnEnter()`。

`TaskBase.Update(deltaTime)`：

1. 如果已成功或失败，直接返回当前状态。
2. 逐个更新 Condition。任一失败则当前 Task 失败。
3. 逐个更新 ExitCondition。任一成功则当前 Task 成功。
4. 调用业务重写的 `OnUpdate(deltaTime)`。

`TaskBase.Exit()`：

1. 退出 Condition 和 ExitCondition。
2. 根据状态调用 `OnSucceeded()` 或 `OnFailed()`。
3. 调用业务重写的 `OnExit()`。

## 回池

Task 结束后，`TaskSystem` 会对根 Task 调用 `CollectToPool()`。根节点持有本次配置图反序列化产生的节点集合，回收根节点时会先把其余节点逐个归还对应类型对象池，再回收节点集合与根节点自身；无效根节点配置也会回收已经创建的全部节点。

根节点持有的节点集合是配置图节点的唯一回收所有者。Condition 连接点、驱动节点连接点和 Timeline 条目只保存运行时引用，清理时只能清空引用，不能再次对节点调用 `CollectToPool()`；否则同一节点会重复进入对象池，并可能被后续并行 Task 图同时分配。

`TaskBase.OnCollect()` 会：

- 回收根节点持有的其余 Task 图节点与节点集合。
- 重置 EnterCondition、Condition、ExitCondition 连接点。
- 清空 `TaskValueInfo`、`TaskContext`、`OnFinished`、`SourceTaskKey`。
- 调用业务重写的 `OnTaskCollect()`。

通用对象池会记录实例是否已经在池中。重复调用 `CollectToPool()` 时不会再次执行回收钩子或加入新的池槽，并在 Editor 下输出警告；该保护用于阻止实例别名扩散，但不能代替 Task 图的单一回收所有权。

修改 Task 回收链路后至少检查：两张使用同类节点的 Task 图并行运行时实例互不相同；其中一张图结束后，另一张图仍继续更新；Condition、Timeline 和无效根配置路径均不会产生重复回收警告。

业务节点必须在 `OnTaskCollect()`、`OnConditionCollect()` 或 `OnContextCollect()` 中清理：

- public 字段。
- `List`、`Dictionary`、`HashSet` 等集合。
- 缓存的 Entity、GameObject、Component、Controller。
- 事件、委托和对象池对象。

## Context 生命周期边界

`TaskManager.CreateTask()` 会把同一个 Context 绑定到生成出的节点上，但 Task 结束时只回收 Task 节点，不自动回收外部创建的 Context。

业务侧应明确 Context 回收归属：

- 如果 Context 只服务一次 Task，通常在 `OnFinished` 后回收。
- 如果 Context 由上层流程复用，应由上层流程在结束时回收。
- 如果 Context 持有对象池对象或集合，必须保证最终调用 `CollectToPool()`。
