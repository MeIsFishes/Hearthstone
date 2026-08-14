---
name: bbxcommon-ecs
description: 说明 BbxCommon ECS 的业务入口、可新建类型、生命周期与维护文档。
---

# BbxCommon ECS

## 1. 模块概览

BbxCommon ECS 是建立在 Unity DOTS `Entity`、`World`、`SystemBase` 之上的运行时数据与逻辑框架。它把游戏对象拆成三类概念：

- `Entity`：实体句柄，本身不携带业务含义，用来挂载数据。
- `EcsRawComponent`：基于类的运行时数据容器，挂在 `Entity` 上。
- `EcsSystemBase` / `EcsMixSystemBase`：逻辑入口，通过迭代一类 `EcsData` 批量驱动状态变化。

业务侧通常用它承载运行时状态和每帧逻辑。静态配置、表格、全局资源数据应优先走 Data / 配置管线；ECS Component 保存会随游戏运行变化的状态。与 DOTS 的边界是：BbxCommon 复用 DOTS 的 `Entity`、`IComponentData`、`SystemBase` 和 system group，但 `EcsRawComponent`、`EcsRawAspect`、`EcsDataGroup`、`EcsDataList<T>` 是 BbxCommon 自己组织的类对象存储。

更多底层说明：

- [底层类关系](developer-docs/class-relationships.md)
- [存储结构](developer-docs/storage-structure.md)
- [Entity 生命周期](developer-docs/entity-lifecycle.md)
- [System 注册与执行](developer-docs/system-registration-and-execution.md)
- [与 DOTS 和 GameStage 的关系](developer-docs/dots-and-game-stage.md)

## 2. API接口

业务代码优先通过 `EcsApi` 和 `Entity` 扩展方法使用 ECS，不直接访问 `EcsDataManager`、`EcsEntityManager`、`EcsDataGroup`、`EcsDataList<T>`。

最简 ECS 业务逻辑以 Entity、RawComponent 和 System 为核心，System 由 GameStage 托管，整体顺序由 GameEngineBase 统一声明：

- `Entity` 表示一个运行时对象，只承担身份与数据挂载点的职责。
- `EcsRawComponent` 保存这个对象会在运行过程中变化的状态，不编写驱动逻辑。
- `EcsMixSystemBase` 派生的 System 在 `OnSystemUpdate()` 中通过 `GetEnumerator<T>()` 取得同类数据并批量处理，不在 System 内保存单个 Entity 的权威状态。
- `GameStage` 持有 System，并根据 System 应按帧还是按固定帧运行，分别通过 `AddUpdateSystem<T>()` 或 `AddFixedUpdateSystem<T>()` 注册。
- `GameEngineBase` 通过 `RegisterSystemOrder(...)` 统一声明不同 System 之间的运行先后关系。

因此，最小业务实现通常只需要定义一种 `EcsRawComponent` 保存状态、定义一个 `EcsMixSystemBase` 派生类处理状态，在 Stage 中加入该 System，再为运行时 Entity 添加对应 RawComponent。Entity 不再使用时应主动销毁。

这个模型主要使用以下接口。

实体生命周期：

- `EcsApi.CreateEntity(string group = "", EntityID entityID = new EntityID())`
- `EcsApi.DestroyEntity(Entity entity)`、`EcsApi.DestroyEntity(EntityID entityID)`、`entity.Destroy()`
- 需要把池化 GameObject 从 Entity 分离但不销毁对象时，使用 `entity.UnbindGameObject()`；随后再归还对象池并销毁 Entity。

运行时数据：

- `entity.AddRawComponent<T>()`、`entity.HasRawComponent<T>()`、`entity.GetRawComponent<T>()`、`entity.RemoveRawComponent<T>()`

需要让 UI 或阶段事件逻辑响应字段变化时：

- 使用 `ListenableVariable<T>.SetValue(value)` 修改值并发送 Dirty；若 `T` 是集合等引用类型、只修改了对象内部内容，则修改后手动调用 `SetDirty()`。
- Component 回收前对其持有的每个 `ListenableVariable<T>` 调用 `MakeInvalid()`，通知消费者目标失效并清除监听，随后再重置字段或释放集合。
- 需要表达“受到伤害”“进入某状态”等比字段变化更具体的事件时，让 Component 实现 `IListenable`，通过事件枚举派发语义事件；回收时清空消息监听。
- 非每帧逻辑如果只需在 Stage 存活期间响应上述事件，使用 `StageListenerBase`，在 `InitListener()` 中调用 `AddVariableDirtyListener`、`AddVariableInvalidListener` 或 `AddListener`，并通过 `GameStage.AddStageListener<T>()` 挂载。

只在确实存在消费者时引入监听；普通运行时字段仍使用普通字段。UI Controller 的监听方式见 `bbxcommon-ui`。

System 与 Stage 注册：

- `GameStage.AddUpdateSystem<T>() where T : EcsSystemBase, new()`
- `GameStage.AddFixedUpdateSystem<T>() where T : EcsSystemBase, new()`
- `EcsMixSystemBase.GetEnumerator<T>() where T : EcsData`
- `GameEngineBase<TEngine>.RegisterSystemOrder(params Type[] systemTypes)`

## 3. 业务类

业务侧常见需要新建以下类型：

- `EcsRawComponent` 子类：每个实体一份或多份运行时数据。模板见 [raw-component.md](new-class-examples/raw-component.md)。
- `EcsSingletonRawComponent` 子类：同一类型全局最多一份，可挂在默认单例 Entity 或指定 Entity 上。模板见 [singleton-raw-component.md](new-class-examples/singleton-raw-component.md)。
- `EcsRawAspect` 子类：组合多个 RawComponent、Singleton 或 GameObject 上的 Unity `Component`，作为访问门面。模板见 [raw-aspect.md](new-class-examples/raw-aspect.md)。
- `EcsMixSystemBase` 子类：读写 `EcsRawComponent` / `EcsRawAspect` 的业务逻辑入口。模板见 [mix-system.md](new-class-examples/mix-system.md)。
- `EcsBaker` 子类：用于场景 GameObject 启动时初始化 Entity、绑定对象并挂载数据。模板见 [ecs-baker.md](new-class-examples/ecs-baker.md)。

业务类的总体约束是：Component 放数据，System 做调度与批量逻辑，Aspect 做访问封装，Baker 只适合由场景 GameObject 驱动的简单 Entity 创建。

## 4. 主要类的生命周期

### 4.1 Entity

- 创建：通过 `EcsApi.CreateEntity()` 创建；也可以由 `EcsBaker.Awake()` 自动创建。
- 绑定：需要关联场景对象时，调用 `entity.BindGameObject(gameObject)`；`EcsBaker` 会自动绑定自身 GameObject。
- 解绑：池化表现回收前调用 `entity.UnbindGameObject()`，它会移除绑定 Component 并返回原 GameObject，而不会销毁该对象。
- 使用：创建后再添加 `EcsRawComponent`、`EcsRawAspect` 或 DOTS `IComponentData`。
- 销毁：通过 `EcsApi.DestroyEntity()` 或 `entity.Destroy()` 销毁；销毁时会清理 HUD、关联 GameObject、RawComponent、RawAspect 和 DOTS Entity。

详细过程见 [Entity 生命周期](developer-docs/entity-lifecycle.md)。

### 4.2 EcsRawComponent / EcsSingletonRawComponent

- 创建：通过 `entity.AddRawComponent<T>()` 或 `EcsApi.AddSingletonRawComponent<T>()` 从对象池分配。
- 初始化：对象分配后触发 `OnComponentAllocate()` 或 `OnSingletonAllocate()`；业务侧可在这里恢复默认状态。
- 使用：System 通过 `GetEnumerator<T>()` 批量读取普通组件，通过 `GetSingletonRawComponent<T>()` 读取单例组件。
- 停用：调用 `Deactivate()` 或对应 `Deactive*` API 后，迭代器会在枚举结束时延迟清理无效数据。
- 回收：Entity 销毁或组件移除时触发 `OnComponentCollect()` / `OnSingletonCollect()`；业务侧必须清空集合、事件、缓存引用和对象池对象。
- 可监听字段：回收钩子中先调用 `MakeInvalid()`，再恢复默认值；不要让池化后的 Component 保留上一轮监听者。

模板见 [raw-component.md](new-class-examples/raw-component.md) 和 [singleton-raw-component.md](new-class-examples/singleton-raw-component.md)。

### 4.3 EcsRawAspect

- 创建：通过 `entity.CreateRawAspect<T>()` 创建，并立即执行 `CreateAspect()`。
- 初始化：`CreateAspect()` 中拉取依赖的 RawComponent、Singleton 或 GameObject 上的 Unity `Component`。
- 使用：Aspect 可被 `EcsMixSystemBase.GetEnumerator<T>()` 迭代，适合作为多个数据源的访问门面。
- 移除：通过 `entity.RemoveRawAspect<T>()` 或 Entity 销毁回收；Aspect 不应保存权威状态。

创建 Aspect 前，必须先添加它依赖的组件并完成 GameObject 绑定。模板见 [raw-aspect.md](new-class-examples/raw-aspect.md)。

### 4.4 EcsSystemBase / EcsMixSystemBase

- 创建：通过 `GameStage.AddUpdateSystem<T>()` 或 `GameStage.AddFixedUpdateSystem<T>()` 创建并归入 Stage。
- 初始化：DOTS `OnCreate()` 被基类密封，并转发到 `OnSystemCreate()`。
- 运行：DOTS `OnUpdate()` 被基类密封，并转发到 `OnSystemUpdate()`；业务逻辑写在 `OnSystemUpdate()`。基类会记录最近一次执行耗时。
- 销毁：DOTS `OnDestroy()` 被基类密封，并转发到 `OnSystemDestroy()`。
- 排序：Stage 加载或卸载 System 时，对应 Update / FixedUpdate group 会立即按 GameEngine 登记表重排；未登记类型稳定追加到末尾。
- 卸载：Stage 卸载时，System 会从对应 DOTS system group 移除并触发重排。

业务 System 应标注 `[DisableAutoCreation]`，由 GameStage 手动管理。模板见 [mix-system.md](new-class-examples/mix-system.md)，详细执行机制见 [System 注册与执行](developer-docs/system-registration-and-execution.md)。

### 4.5 EcsBaker

- 创建：作为 `MonoBehaviour` 挂在场景 GameObject 上，由 Unity 创建。
- 初始化：`Awake()` 中创建 Entity、绑定当前 GameObject，然后调用业务重写的 `Bake()`。
- 使用：`Bake()` 中添加 RawComponent、DOTS Component，并在依赖就绪后创建 Aspect。
- 销毁：`OnDestroy()` 中如果 `DestroyEntity == true`，会销毁自身创建的 Entity。

`EcsBaker` 适合简单场景对象的 ECS 初始化；复杂生成、对象池或跨 Stage 生命周期的实体更适合手动使用 `EcsApi`。模板见 [ecs-baker.md](new-class-examples/ecs-baker.md)。
