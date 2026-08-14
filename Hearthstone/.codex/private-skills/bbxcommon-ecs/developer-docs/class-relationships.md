# 底层类关系

## 业务入口

`EcsApi` 是业务侧主要入口。它把实体创建、RawComponent 操作、Singleton 操作、RawAspect 操作、DOTS `IComponentData` 操作封装成静态函数和 `Entity` 扩展方法。

业务代码不应直接依赖以下内部类：

- `EcsEntityManager`
- `EcsDataManager`
- `EcsDataGroup`
- `EcsDataList<T>`

## Entity 管理

`EcsEntityManager` 负责：

- 调用 DOTS `World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity()` 创建 `Entity`。
- 为每个 Entity 分配 `EcsDataGroup`。
- 添加 `EntityIDComponent`。
- 按 group 维护 `Dictionary<EntityID, Entity>`。
- 销毁 Entity 时清理 HUD、GameObject、BbxCommon ECS 数据和 DOTS Entity。

`EntityID` 是包含数值 id 与 group 字符串的结构体。`EcsApi.CreateEntity()` 未传入有效 `EntityID` 时，`EcsEntityManager` 会按 group 使用 `UniqueIdGenerator` 生成 id。

## 数据管理

`EcsData` 是 BbxCommon Raw 数据的基类，继承 `PooledObject`。它保存：

- `Entity`：所属实体。
- `Index`：当前对象在 `EcsDataList<T>` 中的位置。
- `Active` / `RequestDeactive`：控制迭代可见性。

`EcsRawComponent` 与 `EcsRawAspect` 都继承 `EcsData`：

- `EcsRawComponent` 是挂在 Entity 上的数据容器。
- `EcsSingletonRawComponent` 继承 `EcsRawComponent` 并实现内部标记接口 `IEcsSingletonData`。
- `EcsRawAspect` 是组合访问门面，创建时缓存当前 Entity、GameObject 和依赖数据。

## System 关系

`EcsSystemBase` 继承 DOTS `SystemBase`，并密封：

- `OnCreate()` -> `OnSystemCreate()`
- `OnUpdate()` -> `OnSystemUpdate()`
- `OnDestroy()` -> `OnSystemDestroy()`

`EcsMixSystemBase` 继承 `EcsSystemBase`，额外提供：

- `GetSingletonRawComponent<T>()`
- `GetEnumerator<T>() where T : EcsData`

当前业务 System 通常继承 `EcsMixSystemBase`。`EcsSystemBase` 保留了仅使用 DOTS `IComponentData` 的扩展空间。

## Baker 关系

`EcsBaker` 继承 `MonoBehaviour`。它在 Unity `Awake()` 中创建 Entity、绑定 GameObject 并调用 `Bake()`，让业务 Baker 能用受保护方法添加 DOTS Component、RawComponent 和 RawAspect。
