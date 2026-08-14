# 与 DOTS 和 GameStage 的关系

## 复用 DOTS 的部分

BbxCommon ECS 当前复用 Unity DOTS 的：

- `Unity.Entities.Entity`
- `World.DefaultGameObjectInjectionWorld`
- `EntityManager.CreateEntity()` / `DestroyEntity()`
- `IComponentData`
- `SystemBase`
- `ComponentSystemGroup`
- `SimulationSystemGroup`
- `FixedStepSimulationSystemGroup`
- `DisableAutoCreation`

这让业务可以同时使用 DOTS `IComponentData` 与 BbxCommon 的 `EcsRawComponent`。

## BbxCommon 自己管理的部分

以下内容由 BbxCommon 自己组织，不是 DOTS 原生存储：

- `EcsRawComponent`
- `EcsSingletonRawComponent`
- `EcsRawAspect`
- `EcsDataGroup`
- `EcsDataList<T>`
- `GameObjectRawComponent`
- `EntityIDComponent`

这些类对象通过 `EcsDataManager` 管理，并使用对象池回收。它们不会成为 DOTS archetype 中的紧凑内存组件。

## GameEngine 初始化

`GameEngineBase.OnAwakeEcsWorld()` 负责把 BbxCommon ECS 接入 DOTS World：

1. 保存默认 World。
2. 取得 DOTS `SimulationSystemGroup`，创建 BbxCommon 自定义 `UpdateSystemGroup` 并加入其中。
3. 取得 DOTS `FixedStepSimulationSystemGroup`，创建 BbxCommon 自定义 `FixedUpdateSystemGroup` 并加入其中。
4. 创建默认单例 Entity，并交给 `EcsDataManager`。

没有看到 BbxCommon 自己创建独立 World；当前代码使用 `World.DefaultGameObjectInjectionWorld`。

两个 BbxCommon 自定义 group 都关闭 DOTS Attribute 排序。GameEngine 通过 `RegisterSystemOrder(params Type[] systemTypes)` 把同一份显式类型顺序同步给两者；这份顺序只决定各 group 内部的相对位置，不会改变 Update 与 FixedUpdate 各自的更新频率。

## GameStage 的职责

`GameStage` 是 System、场景、UI、DataGroup、StageListener 和加载项的作用域容器。对 ECS 来说，它负责：

- 创建业务 System 实例。
- 在 Stage 加载 Tick 阶段把 System 加入 DOTS system group。
- 在 Stage 卸载 Tick 阶段移除 System。
- 通过 `AddUpdateSystem<T>()` / `AddFixedUpdateSystem<T>()` 区分普通帧更新和固定步更新。
- 每次加载或卸载后，按 GameEngine 的显式类型顺序即时重排对应 group；未登记类型保持原相对顺序并排在末尾。

Stage 加载顺序中，Tick 位于 DataGroup 加载之后、StageListener 之前。卸载顺序相反。

业务 System 仍需要 `[DisableAutoCreation]`，确保实例只由 GameStage 创建与管理；`UpdateBefore` / `UpdateAfter` 不再参与这些 group 的执行顺序。

## 与配置数据的边界

配置数据 skill 的规则是：静态配置走 `DataApi`，运行时状态走 ECS Component。

在 ECS 代码中常见组合是：

- Stage 通过 `AddDataGroup()` 加载配置。
- Entity 创建或 Baker `Bake()` 时从配置写入 RawComponent 初始值。
- System 在运行时只读配置、读写 RawComponent。

不要把表格型静态配置长期塞进 `EcsRawComponent` 作为唯一来源；RawComponent 更适合保存运行期可变状态或从配置拷贝出的实例状态。

## 与 GameObject 的关系

BbxCommon 没有继承或包装 DOTS Entity，而是通过 `GameObjectRawComponent` 把 Entity 与 GameObject 关联：

```csharp
entity.BindGameObject(gameObject);
var go = entity.GetGameObject();
```

`EcsRawAspect.GetGameObjectComponent<T>()` 依赖这个绑定。`EcsBaker` 会自动绑定自身所在 GameObject，手动创建 Entity 时需要业务侧显式绑定。
