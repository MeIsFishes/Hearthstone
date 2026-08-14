# System 注册与执行

## System 基类链路

`EcsSystemBase` 继承 DOTS `SystemBase`。它密封 DOTS 生命周期，并转发到 BbxCommon 生命周期：

- DOTS `OnCreate()` -> `OnSystemCreate()`
- DOTS `OnUpdate()` -> `OnSystemUpdate()`
- DOTS `OnDestroy()` -> `OnSystemDestroy()`

`OnUpdate()` 内部会为每个 System 实例持有独立的 `DebugApi.ProfilerData`，记录该实例最近一次执行的性能采样。采样以 `try/finally` 包围 `OnSystemUpdate()`，即使业务逻辑抛出异常也会结束本次采样。不同 Stage 中的同类型实例或不同命名空间下的同名 System 不会共享计时对象。

`EcsMixSystemBase` 在此基础上提供 Raw 数据访问：

- `GetSingletonRawComponent<T>()`
- `GetEnumerator<T>()`

## 为什么需要 DisableAutoCreation

业务 System 继承 DOTS `SystemBase` 后，理论上可能被 DOTS 自动创建。BbxCommon 的运行方式是由 `GameStage` 手动创建并托管 System，因此业务 System 模板带有：

```csharp
[DisableAutoCreation]
public partial class ExampleSystem : EcsMixSystemBase
{
}
```

如果忘记注册到 `GameStage`，打了 `[DisableAutoCreation]` 的 System 不会执行。

## GameStage 注册

业务 Stage 使用：

```csharp
stage.AddUpdateSystem<ExampleSystem>();
stage.AddFixedUpdateSystem<ExampleFixedSystem>();
```

底层行为：

- `AddUpdateSystem<T>()` 调用 `m_EcsWorld.CreateSystemManaged<T>()`，并放入 `m_UpdateSystems`。
- `AddFixedUpdateSystem<T>()` 调用 `m_EcsWorld.CreateSystemManaged<T>()`，并放入 `m_FixedUpdateSystems`。

System 实例在调用 `AddUpdateSystem<T>()` / `AddFixedUpdateSystem<T>()` 时创建，但要等 Stage 加载到 Tick 阶段、加入对应更新组后才开始随更新组执行。

## Stage 加载时加入 DOTS system group

`GameStage.LoadStage()` 的 Tick 阶段调用 `OnLoadStageTick()`：

- `m_UpdateSystems` 加入 `UpdateSystemGroup`。
- `m_FixedUpdateSystems` 加入 `FixedUpdateSystemGroup`。
- 每类 System 全部加入后，对应 group 立即调用 `RefreshSystemUpdateOrder()`。

Stage 卸载时，`OnUnloadStageTick()` 会从对应 group 移除这些 System，并以剩余实例立即重建执行顺序。

## 执行顺序

派生 GameEngine 通过 `GameEngineBase<TEngine>` 的 `protected` 方法登记全局类型顺序：

```csharp
protected override void OnAwake()
{
    RegisterSystemOrder(
        typeof(EarlySystem),
        typeof(LateSystem));
}
```

登记规则：

1. 多次调用会继续追加到同一份顺序表，而不是覆盖之前的登记；每次调用后都会同步两个 group，并立即重排其中已有的 System。
2. 登记类型按类型表中的先后顺序优先执行。
3. 没有登记的 System 保持当前更新组中的原相对顺序，并统一追加到已登记 System 之后。
4. Update 和 FixedUpdate group 复用同一份类型顺序表；某个类型只会在实际包含它的 group 中参与排序，两类 System 仍按各自更新频率执行。
5. Stage 加载和卸载会即时触发对应 group 重排，因此多个 Stage 的 System 也服从同一份登记表。
6. 参数数组不能为 `null`，元素不能为 `null`，类型必须是继承 `EcsSystemBase` 的具体闭合类型，且同一类型不能重复登记；违反约束会抛出参数异常。整批参数会先完成校验再写入顺序表。

`UpdateSystemGroup` 与 `FixedUpdateSystemGroup` 都继承 `GameEngineOrderedSystemGroup`。该基类在 `OnCreate()` 中设置 `EnableSystemSorting = false`，不采用 DOTS `UpdateBefore` / `UpdateAfter` Attribute 排序，而是移除并按上述结果重新加入 System。因此业务 System 仍需 `[DisableAutoCreation]`，但不得再用 `UpdateBefore` / `UpdateAfter` 表达顺序。

## 迭代行为

`GetEnumerator<T>()` 来自 `EcsDataList<T>.GetEnumerator()`：

1. 依次读取 `m_EcsDatas`。
2. 如果对象为空或 `RequestDeactive == true`，记录到 `m_DeletedDatas`。
3. 否则 `yield return data`。
4. 迭代结束后统一 `RemoveDeletedDatas()`。

因此，迭代期间移除或反激活对象不会立即修改正在遍历的 List，而是延迟到枚举结束后清理。
