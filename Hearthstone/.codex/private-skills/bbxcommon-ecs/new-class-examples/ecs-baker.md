# EcsBaker 新建模板

## 使用场景

`EcsBaker` 是一个 `MonoBehaviour`，用于在场景 GameObject 启动时创建 BbxCommon ECS Entity，并把 Inspector 中配置的初始值写入 ECS 数据。

适合：

- 场景里已有 GameObject，需要启动时挂接 ECS 数据。
- 简单实体的初始化依赖 Inspector 字段。

不适合：

- 生命周期完全由后台逻辑、对象池或复杂生成器控制的实体。
- 需要跨多个加载阶段精细管理创建和销毁顺序的实体。

## 最小模板

```csharp
using BbxCommon;
using UnityEngine;

public class ExampleUnitBaker : EcsBaker
{
    [SerializeField] private int m_MaxHp = 100;

    protected override void Bake()
    {
        var stats = AddRawComponent<ExampleStatsRawComponent>();
        stats.MaxHp = m_MaxHp;
        stats.CurHp = m_MaxHp;

        CreateRawAspect<ExampleMovementRawAspect>();
    }
}
```

## 必填字段和函数

- Inspector 字段：按初始化需要声明 `[SerializeField]` 字段，例如 `m_MaxHp`。没有需要从场景配置的值时可以不写。
- `Bake()`：必须重写。需要在这里添加 RawComponent、写入初始值，并在依赖准备好后创建 Aspect。

## 可选字段和函数

- `DestroyEntity`：需要 Baker 销毁时保留 Entity 的场景，显式设为 `false`；默认 `true`。
- `Entity`：需要把创建出的 Entity 传给其它初始化逻辑时读取。

Baker 内常用方法：

- `AddComponent<T>() where T : unmanaged, IComponentData`
- `AddComponent<T>(T componentData) where T : unmanaged, IComponentData`
- `AddRawComponent<T>() where T : EcsRawComponent, new()`
- `CreateRawAspect<T>() where T : EcsRawAspect, new()`

## 注意事项

- `CreateRawAspect<T>()` 应写在依赖的 `AddRawComponent<T>()` 和必要字段赋值之后。
- 如果 Aspect 依赖 GameObject 上的 Unity `Component`，确保该 Component 在 `Awake()` 时已经存在。
- `EcsBaker` 创建的 Entity 默认没有 group，也没有自定义 `EntityID`；如需 group 管理，考虑用 `EcsApi.CreateEntity(group, id)` 手动创建。
- `DestroyEntity == true` 会在 Baker 销毁时销毁 Entity，并触发 RawComponent / RawAspect 回收。
