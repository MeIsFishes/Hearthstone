# EcsSingletonRawComponent 新建模板

## 使用场景

`EcsSingletonRawComponent` 是特殊的 `EcsRawComponent`：同一子类型在 `EcsDataList<T>` 中最多存在一份。它适合保存全局运行时状态，或用作唯一实体标记。

典型例子：

- 战斗全局状态、当前回合、计时器。
- 标记唯一玩家 Entity，通过 `GetSingletonRawComponent<T>().GetEntity()` 快速取回宿主。

## 最小模板

```csharp
using BbxCommon;

public class ExampleSessionSingletonRawComponent : EcsSingletonRawComponent
{
    public int RoundIndex;

    protected override void OnSingletonCollect()
    {
        RoundIndex = 0;
    }
}
```

## 必填字段和函数

- 业务字段：按模块需要声明全局运行时状态字段，例如 `RoundIndex`。`EcsSingletonRawComponent` 本身不规定必填字段。
- `OnSingletonCollect()`：如果单例中存在集合、事件、缓存引用、对象池对象、非默认值状态，必须重写并清理。原因是 SingletonRawComponent 仍然会被对象池复用。

## 可选字段和函数

- `OnSingletonAllocate()`：需要在分配时恢复默认状态，或初始化运行期缓存时再重写。
- `GetEntity()`：需要取得单例当前挂载的 `Entity` 时调用。

`EcsSingletonRawComponent` 子类不要重写普通 Component 的 `OnComponentAllocate()` / `OnComponentCollect()`；业务侧使用 Singleton 版本的钩子。

## 注意事项

- 同一 `EcsSingletonRawComponent` 子类型表示唯一数据，不要设计成需要多份实例的业务状态。
- 若字段使用 `ListenableVariable<T>`，在 `OnSingletonCollect()` 中先调用 `MakeInvalid()` 再重置值，规则与普通 RawComponent 相同。
- 类型需保留可访问的无参构造，不要把构造函数改成需要业务参数的形式。
- 如果同一类型需要被多个实体持有，不要继承 `EcsSingletonRawComponent`。
