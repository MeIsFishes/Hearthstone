# EcsMixSystemBase 新建模板

## 使用场景

`EcsMixSystemBase` 是当前业务侧最常用的 ECS System 基类。它继承 `EcsSystemBase`，间接继承 Unity DOTS `SystemBase`，并开放 RawComponent / RawAspect 迭代能力。

System 是逻辑入口：读取、修改 Component 或 Aspect 暴露的数据，驱动一条清晰的规则。

## 最小模板

```csharp
using BbxCommon;
using Unity.Entities;

[DisableAutoCreation]
public partial class ExampleUpdateSystem : EcsMixSystemBase
{
    protected override void OnSystemUpdate()
    {
        foreach (var stats in GetEnumerator<ExampleStatsRawComponent>())
        {
            // 业务逻辑
        }
    }
}
```

## 必填字段和函数

- `[DisableAutoCreation]`：业务 System 必须添加，避免 DOTS 自动创建一份不受 GameStage 管理的实例。
- `partial class`：建议保留，与 DOTS / 项目模板保持一致。
- `OnSystemUpdate()`：业务逻辑入口，必须在这里写每帧或每次 FixedUpdate 要执行的数据读取和状态修改。

## 可选字段和函数

- `OnSystemCreate()`：需要初始化 System 内部缓存时重写。
- `OnSystemDestroy()`：需要清理 System 内部缓存或释放事件订阅时重写。
- 私有辅助方法：用于拆分 `OnSystemUpdate()` 中的规则逻辑。

业务类不要直接重写 DOTS 的 `OnCreate()`、`OnUpdate()`、`OnDestroy()`。

## 注意事项

- 类型需保留可访问的无参构造，不要把构造函数改成需要业务参数的形式。
- `GetEnumerator<T>()` 只枚举活跃的 `EcsData`；被 `Deactivate()` 标记的数据不会继续作为有效项返回。
- 不要把所有玩法塞进一个巨型 System；按数据变换或规则职责拆分。
- 业务 System 仍必须保留 `[DisableAutoCreation]`，但不要再添加 `UpdateBefore` / `UpdateAfter`；System 的执行顺序由 GameEngine 的类型顺序表统一决定，未登记类型排在已登记类型之后。
