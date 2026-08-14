# Entity 生命周期

## GameEngine 初始化

`GameEngineBase.OnAwakeEcsWorld()` 会：

1. 取得 `World.DefaultGameObjectInjectionWorld`。
2. 创建 GameEngine 托管的 `UpdateSystemGroup` 与 `FixedUpdateSystemGroup`，分别接入 DOTS `SimulationSystemGroup` 与 `FixedStepSimulationSystemGroup`；两个组关闭 DOTS Attribute 排序并接收同一份显式类型顺序表。
3. 创建一个默认单例 Entity。
4. 调用 `EcsDataManager.SetSingletonRawComponentEntity()` 记录默认单例宿主。

这意味着 `EcsApi.AddSingletonRawComponent<T>()` 依赖 GameEngine 的 ECS World 初始化已经完成。

## 创建 Entity

业务入口：

```csharp
var entity = EcsApi.CreateEntity("group");
```

底层链路：

1. `EcsApi.CreateEntity()` 调用 `EcsEntityManager.CreateEntity()`。
2. 按 group 取得或创建 `UniqueIdGenerator` 和 group 字典。
3. 检查传入 `EntityID` 是否冲突。
4. 通过 DOTS `EntityManager.CreateEntity()` 创建 Entity。
5. 如未传入有效 id，则生成新的 `EntityID`。
6. 分配并初始化 `EcsDataGroup`。
7. 添加 `EntityIDComponent` 并写入 id。
8. 将 `EntityID -> Entity` 写入 group 字典。

## 绑定 GameObject

业务入口：

```csharp
entity.BindGameObject(gameObject);
```

底层会添加内部 `GameObjectRawComponent`，并保存 `GameObject` 引用。`entity.GetGameObject()` 从该组件读取 GameObject；如果组件不存在，返回 `null`。

`EcsBaker` 会在 `Awake()` 中自动执行绑定。

## 添加数据

RawComponent 添加后会同时进入：

- Entity 对应的 `EcsDataGroup.RawComponents`
- 类型对应的 `EcsDataList<T>`

RawAspect 创建后会：

- 写入 `EcsDataGroup.RawAspects`
- 调用 `aspect.Create()`，再执行子类 `CreateAspect()`
- 写入类型对应的 `EcsDataList<T>`

## 销毁 Entity

业务入口：

```csharp
EcsApi.DestroyEntity(entity);
entity.Destroy();
EcsApi.DestroyEntity(entityID);
```

按 Entity 销毁时，`EcsEntityManager.DestroyEntity(Entity)` 会：

1. 读取 `EntityIDComponent` 得到 id。
2. 调用 `entity.ClearHud()` 关闭并清空 HUD。
3. 调用 `entity.GetGameObject().Destroy()` 销毁关联 GameObject。
4. 调用 `EcsDataManager.DestroyEntity(entityID)` 回收 RawComponent、RawAspect、EcsDataGroup。
5. 调用 DOTS `EntityManager.DestroyEntity(entity)`。
6. 从 group 字典移除 id。

按 `EntityID` 销毁时，会先查到 Entity，再执行相近的清理流程。

## 回收 Raw 数据

`EcsDataManager.DestroyEntity(entityID)` 会遍历该 Entity 的：

- `RawComponents`
- `RawAspects`

逐个从 `EcsDataGroup` 移除并调用 `CollectToPool()`。组件的 `OnComponentCollect()` 或单例组件的 `OnSingletonCollect()` 会在回收过程中触发。

## ResetEntitiesByGroup

`EcsApi.ResetEntitiesByGroup(group)` 会销毁该 group 字典中的实体，并重置对应 `UniqueIdGenerator` 计数器。使用时要注意不要在遍历同一 group 字典的业务逻辑中交叉调用销毁。
