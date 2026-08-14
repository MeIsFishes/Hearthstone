# 存储结构

## 两套存储

BbxCommon ECS 对 `EcsRawComponent` 和 `EcsRawAspect` 使用两套并行存储：

- 按 Entity 附着：`EcsDataManager.m_EcsDataGroups` 保存每个 Entity 对应的 `EcsDataGroup`。
- 按类型迭代：`EcsDataList<T>` 保存某一类 `EcsData` 的活跃实例，供 `GetEnumerator<T>()` 使用。

`EcsData.Entity` 指向所属 Entity，`EcsData.Index` 缓存其在 `EcsDataList<T>` 中的位置。

## EcsDataGroup

`EcsDataGroup` 是单个 Entity 的 Raw 数据集合：

- `Entity Entity`
- `List<EcsRawComponent> RawComponents`
- `List<EcsRawAspect> RawAspects`

`EcsDataManager.m_EcsDataGroups` 使用 DOTS `Entity.Index` 作为 List 下标，因此查找 Entity 对应数据组时不走哈希。

## RawComponent 存储

`EcsDataGroup.RawComponents` 使用 `ClassTypeId<EcsRawComponent, T>.Id` 作为下标。每个 RawComponent 子类型首次访问时获得一个类型 id，随后可以用该 id 直接定位槽位。

添加 RawComponent 的核心步骤：

1. `ObjectPool<T>.Alloc()` 分配对象。
2. 写入当前 Entity 的 `EcsDataGroup.RawComponents[typeId]`。
3. 设置 `comp.Entity = entity`。
4. 加入 `EcsDataList<T>`。

这种结构牺牲一部分引用槽位内存，换取按 Entity + 类型访问 RawComponent 时的直接索引。

## RawAspect 存储

`EcsDataGroup.RawAspects` 是普通 List。添加时直接 `Add`，查询或删除时遍历判断类型。

旧文档给出的设计理由是：单个 Entity 上 Aspect 数量通常较少，业务侧也很少按 Entity 直接查询 Aspect；Aspect 主要用于按类型迭代，因此不使用 `ClassTypeId` 预留大量槽位。

当前公开 API 没有暴露 `entity.GetRawAspect<T>()`，但底层 `EcsDataManager.GetRawAspect<T>()` 存在并用于内部激活/反激活逻辑。

## EcsDataList<T>

`EcsDataList<T>` 是每种 `EcsData` 类型独立的静态列表：

- `m_EcsDatas`：保存活跃数据的 `ObjRef<T>`。
- `m_DeletedDatas`：迭代期间待删除的槽位下标。

使用 `ObjRef<T>` 的目的，是让对象回收到池后，迭代器能识别空引用并延迟清理对应槽位。

## 删除与反激活

`RemoveRawComponent<T>()` / `RemoveRawAspect<T>()` 会先从 Entity 的 `EcsDataGroup` 移除对象，并调用 `CollectToPool()`。对应 `EcsDataList<T>` 中的槽位不会立刻被同步删除，而是在下一次 `GetEnumerator<T>()` 时发现空引用并清理。

`Deactivate()` / `DeactiveRawComponent<T>()` / `DeactiveRawAspect<T>()` 会设置 `RequestDeactive = true`。迭代器发现后不返回该对象，并在迭代结束时移除槽位。

对象已经被迭代器清理到 `Active == false` 之后，再 `Activate()` 时，底层会把该对象重新加入 `EcsDataList<T>`，并恢复 `Active` / `RequestDeactive`。如果刚调用 `Deactivate()` 但尚未经过迭代清理，当前代码不会用 `Activate()` 取消这次 `RequestDeactive`。

## Singleton 存储

`EcsSingletonRawComponent` 通过内部接口 `IEcsSingletonData` 标记。`EcsDataList<T>.AddEcsData()` 发现该标记后，会限制同一 `T` 的列表中最多保留一个有效对象。

`EcsDataManager.AddSingletonRawComponent<T>()` 实际调用 `AddRawComponent<T>(m_SingletonRawComponentEntity)`，默认宿主 Entity 由 GameEngine 初始化时创建并通过 `SetSingletonRawComponentEntity()` 设置。
