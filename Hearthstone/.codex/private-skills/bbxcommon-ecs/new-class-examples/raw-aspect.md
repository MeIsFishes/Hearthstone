# EcsRawAspect 新建模板

## 使用场景

`EcsRawAspect` 是若干数据源的访问门面。它本身继承 `EcsData`，可以被 System 迭代，但权威状态仍应放在 `EcsRawComponent`、`EcsSingletonRawComponent` 或 GameObject 上的 Unity `Component` 中。

适合使用 Aspect 的情况：

- 一个 System 总是需要同一组 RawComponent。
- 需要缓存 GameObject 上的 `Transform`、`CharacterController` 等引用。
- 希望只对创建过某个 Aspect 的实体执行特定逻辑。

## 最小模板

```csharp
using BbxCommon;
using UnityEngine;

public class ExampleMovementRawAspect : EcsRawAspect
{
    private ExampleStatsRawComponent m_Stats;
    private Transform m_Transform;

    public int CurHp => m_Stats.CurHp;
    public Vector3 Position => m_Transform.position;

    protected override void CreateAspect()
    {
        m_Stats = GetRawComponent<ExampleStatsRawComponent>();
        m_Transform = GetGameObjectComponent<Transform>();
    }
}
```

## 必填字段和函数

- 依赖缓存字段：为 Aspect 要组合访问的 RawComponent、Singleton 或 Unity `Component` 声明字段，例如 `m_Stats`、`m_Transform`。
- `CreateAspect()`：必须重写，并在这里取得所有必需依赖。原因是 Aspect 创建后通常会被 System 直接迭代使用，依赖必须在创建阶段准备好。

## 可选字段和函数

- 只读属性或方法：用于向 System 暴露更清晰的数据访问入口，例如 `CurHp`、`Position`。
- `OnCollect()`：只有 Aspect 自身缓存了需要释放的引用或临时集合时才重写；通常不需要。

`CreateAspect()` 中常用的辅助方法：

- `GetRawComponent<T>() where T : EcsRawComponent`
- `GetSingletonRawComponent<T>() where T : EcsSingletonRawComponent`
- `GetGameObjectComponent<T>() where T : Component`
- `GetEntity()` 继承自 `EcsData`

## 注意事项

- `CreateAspect()` 在 `entity.CreateRawAspect<T>()` 调用过程中立即执行。
- 创建 Aspect 前必须先添加它依赖的 RawComponent，并确保 GameObject 已绑定。
- Aspect 字段用于缓存依赖引用，不要在 Aspect 中新增需要长期保存的权威状态。
- 如果 Aspect 自身缓存需要清空，可谨慎重写 `OnCollect()` 并调用 `base.OnCollect()`。
