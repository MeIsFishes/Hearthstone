# ECS 基础体系

## 目录

1. 基线目标
2. 类型选择
3. 最小可运行流程
4. Component
5. System
6. Aspect 与 GameObject
7. 实体创建和释放
8. 执行顺序与验证

## 1. 基线目标

初始化 ECS 不是预建大量组件目录，而是建立一条能运行、能观察、能卸载的流程：

```text
Stage Load
  → 创建 Entity/Singleton
  → 添加并初始化 RawComponent
  → 必要时绑定 GameObject、创建 Aspect
  → Stage 注册 System
  → System 迭代并修改状态
  → UI/其它 System 观察状态
  → Stage Unload 销毁 Entity/Singleton
```

实现前完整读取 [bbxcommon-ecs](../../bbxcommon-ecs/SKILL.md) 和其中与所选类型对应的新类模板。当前框架源码优先于本概览。

## 2. 类型选择

| 需求 | 类型 |
|---|---|
| 每个实体各自一份运行时数据 | `EcsRawComponent` |
| 全局最多一份运行时状态 | `EcsSingletonRawComponent` |
| 多个数据源的访问门面 | `EcsRawAspect` |
| 每帧/FixedUpdate 规则 | `EcsMixSystemBase` |
| 场景 GameObject 驱动的简单 Entity 初始化 | `EcsBaker` |
| 复杂生成、池化、跨 Stage 创建 | `EcsApi` + EntityCreator/IStageLoad |
| 静态配置 | DataApi 管线，不是 Component |

## 3. 最小可运行流程

如果首个真实模块已经确定，优先选择能观察到的真实状态，例如：

- 应用会话的阶段/就绪状态；
- 首个模式的计时或流程状态；
- 首个可交互实体的状态。

如果产品尚无任何运行时状态可定义，使用本 skill 的 `PlaceholderStateSingletonRawComponent` 与 `PlaceholderStateSystem`。占位状态只表示“基础初始化是否运行”，不虚构角色、战斗或关卡等领域概念。

默认占位流程包含：

1. `PlaceholderStateSingletonRawComponent`：保存可监听的 `Initialized`；
2. `PlaceholderStateSystem`：把 `Initialized` 改为 `true`；
3. `InitializePlaceholderState : IStageLoad`：创建和释放单例；
4. `BaseStage.AddUpdateSystem<PlaceholderStateSystem>()`；
5. GameEngine 通过 `RegisterSystemOrder(typeof(InputSystem), typeof(PlaceholderStateSystem), typeof(TaskSystem))` 登记顺序；
6. `PlaceholderController` 监听同一 `ListenableVariable<bool>`。

首个真实 Component/System 接通后，删除占位注册和类型。不要永久保留 `Example`、`Dummy`、`Test` 或 `Placeholder` 业务文件。

## 4. Component

普通 Component 示例：

```csharp
using BbxCommon;

namespace ProjectName
{
    public sealed class HealthRawComponent : EcsRawComponent
    {
        public int Current;
        public int Maximum;

        protected override void OnComponentCollect()
        {
            Current = 0;
            Maximum = 0;
        }
    }
}
```

单例示例：

```csharp
using BbxCommon;

namespace ProjectName
{
    public sealed class AppSessionSingletonRawComponent : EcsSingletonRawComponent
    {
        public readonly ListenableVariable<bool> Ready = new(false);

        protected override void OnSingletonCollect()
        {
            Ready.SetValue(false);
        }
    }
}
```

写 Component 时：

- 保存权威运行时数据，不执行每帧规则；
- 对 UI/其它观察者需要监听的字段使用当前框架的 `ListenableVariable<T>` 或实现 `IListenable`；
- 所有集合、事件、缓存引用和非默认状态在 Collect 钩子清理；
- 保留无参构造；
- 不缓存可通过 `GetEntity()` 获取的宿主 Entity；
- 单例子类使用 `OnSingletonAllocate/Collect`，普通组件使用 `OnComponentAllocate/Collect`。

## 5. System

最小 System：

```csharp
using BbxCommon;
using Unity.Entities;

namespace ProjectName
{
    [DisableAutoCreation]
    public partial class AppSessionSystem : EcsMixSystemBase
    {
        protected override void OnSystemUpdate()
        {
            var session = GetSingletonRawComponent<AppSessionSingletonRawComponent>();
            if (session == null)
                return;

            // 执行真实、可解释的状态规则。
        }
    }
}
```

规则：

- 业务 System 加 `[DisableAutoCreation]`，由 Stage 管理；
- 业务逻辑写在 `OnSystemUpdate()`，不要重写被基类密封的 DOTS 生命周期；
- 按一条数据变换/规则职责拆分，避免 `MainSystem`；
- 在 `OnSystemCreate/Destroy` 成对管理 System 缓存；
- System 依赖数据不存在是否允许，必须明确。若生命周期保证存在，可快速失败；若跨 Stage 可选，则安全跳过；
- 不用 Stage 的 Add 顺序或 DOTS `UpdateBefore` / `UpdateAfter` 表达稳定执行顺序；把 System 类型加入 GameEngine 的 `RegisterSystemOrder(typeof(...), ...)` 列表。

## 6. Aspect 与 GameObject

只有以下条件之一成立才创建 Aspect：

- System 总是共同使用多个 RawComponent；
- 需要缓存绑定 GameObject 上的 Transform、Animator 等；
- 希望只迭代满足一组依赖的实体。

创建顺序必须是：创建 Entity → 添加依赖 Component → 绑定 GameObject（若需要）→ `CreateRawAspect<T>()`。Aspect 只封装访问，不保存新的权威状态。

GameObject 生命周期和 Entity 生命周期必须定义所有权：

- Entity 拥有实例：销毁 Entity 时确认绑定对象如何清理；
- Scene 拥有对象：Scene 卸载时 `EcsBaker` 是否销毁 Entity；
- 对象池拥有对象：归还池时停用/移除 ECS 数据，避免旧引用泄漏。

## 7. 实体创建和释放

简单场景对象可用 `EcsBaker`；复杂业务建议集中到 `<Feature>EntityCreator` 或 Stage 的加载项：

```text
CreateEntity
  → AddRawComponent
  → 填初值
  → BindGameObject（条件）
  → CreateRawAspect（条件）
```

释放必须能从创建点反向追踪：

- `EcsApi.DestroyEntity` / `entity.Destroy()`；
- `EcsApi.RemoveSingletonRawComponent<T>()`；
- 按 group 创建时，必要时按 group 重置；
- Stage Unload 不得遗留单例、监听、对象池引用和 Hud 绑定。

## 8. 执行顺序与验证

多个 System 有先后依赖时，在 GameEngine 初始化阶段通过 `RegisterSystemOrder(typeof(...), ...)` 统一登记。已登记类型在各自的 Update 或 FixedUpdate 组内按列表顺序执行；未登记类型保持原相对顺序并追加到末尾。Stage 的 `AddUpdateSystem` / `AddFixedUpdateSystem` 只决定归属，不决定跨 Stage 的最终顺序。

验证至少覆盖：

- Stage 加载后 Component/Singleton 只存在一份预期实例；
- System 确实由目标 Stage 创建，未被 DOTS 自动重复创建；
- GameEngine 类型列表覆盖所有需要稳定顺序的 System，未登记项位于预期的末尾；
- 状态变化符合一条明确规则；
- UI/观察者读取同一份 Model；
- Stage 卸载后 Entity、单例、监听与绑定对象被清理；
- 重新加载 Stage 不继承上一次池化数据。
