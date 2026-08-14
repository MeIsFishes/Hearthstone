# EcsRawComponent 新建模板

## 使用场景

`EcsRawComponent` 是挂在 `Entity` 上的类对象数据容器。它适合保存每个实体各自拥有的运行时状态，例如属性、请求列表、动作状态、临时计数等。

如果同一类型数据全局只允许一份，改用 `EcsSingletonRawComponent`。如果只是封装多组件访问，改用 `EcsRawAspect`。

## 最小模板

```csharp
using BbxCommon;

public class ExampleStatsRawComponent : EcsRawComponent
{
    public int MaxHp;
    public int CurHp;

    protected override void OnComponentCollect()
    {
        MaxHp = 0;
        CurHp = 0;
    }
}
```

## 必填字段和函数

- 业务字段：按模块需要声明运行时状态字段，例如 `MaxHp`、`CurHp`。`EcsRawComponent` 本身不规定必填字段。
- `OnComponentCollect()`：如果组件中存在集合、事件、缓存引用、对象池对象、非默认值状态，必须重写并清理。原因是 RawComponent 会被对象池复用，旧状态不能泄漏到下一次使用。

## 可选字段和函数

- `OnComponentAllocate()`：需要在组件分配时恢复默认状态，或初始化运行期缓存时再重写。
- 辅助方法：可以写少量用于修改自身数据的方法，但应由 System、Baker 或其它明确入口显式调用。
- `GetEntity()`：需要从组件反查所属 `Entity` 时调用，不需要自行保存 Entity 字段。

## 可监听字段

只有 UI、StageListener 或其它观察者确实需要响应变化时，才把字段声明为 `ListenableVariable<T>`：

```csharp
public class ExampleStatsRawComponent : EcsRawComponent
{
    public int CurHp => CurHpVariable.Value;
    public readonly ListenableVariable<int> CurHpVariable = new(0);

    public void SetCurHp(int value)
    {
        CurHpVariable.SetValue(value);
    }

    protected override void OnComponentCollect()
    {
        CurHpVariable.MakeInvalid();
        CurHpVariable.SetValue(0);
    }
}
```

- `SetValue` 仅在值变化时发送 Dirty。
- 引用类型内部发生变化但引用本身未变时，修改完成后手动调用 `SetDirty()`。
- `MakeInvalid()` 会发送 Invalid 并移除所有监听者。Component 会进入对象池，因此必须在回收钩子中调用。
- 如果需要“受到伤害”等语义事件，而不是单纯的字段变化，可让 Component 实现 `IListenable` 并按事件枚举派发；回收时清空其 `MessageHandler`。

## 注意事项

- Component 是数据容器，不作为每帧逻辑入口。
- 集合、事件、缓存引用、对象池对象等必须在 `OnComponentCollect()` 中清空或释放；简单数值字段也建议重置为默认值。
- `ListenableVariable<T>` 必须在 `OnComponentCollect()` 中 `MakeInvalid()`，避免监听者继续引用已经回池的数据。
- 类型需保留可访问的无参构造，不要把构造函数改成需要业务参数的形式。
