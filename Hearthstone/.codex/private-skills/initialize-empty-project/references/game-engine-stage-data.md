# GameEngine、Stage 与数据

## 目录

1. 架构职责
2. 最小 GameEngine
3. Stage 组合
4. IStageLoad
5. 数据组
6. 启动顺序
7. 检查清单

## 1. 架构职责

在 BbxCommon 中：

- GameEngine 是唯一启动与 Stage 组合入口；
- GameStage 是 System、UI、数据、Scene、监听与加载项的生命周期容器；
- `IStageLoad` 负责非 System 对象的成对创建/释放；
- DataGroup 负责静态配置加载；
- ECS System 执行运行时规则。

不要让 GameEngine 直接承担实体更新、UI 刷新或玩法状态机。

## 2. 最小 GameEngine

在写代码前读取当前 `GameEngineBase<T>` 源码和 [game-stage](../../game-stage/SKILL.md)，以当前 API 为准。典型职责：

```csharp
using BbxCommon;

namespace ProjectName
{
    public sealed class ProjectGameEngine : GameEngineBase<ProjectGameEngine>
    {
        private GameStage m_BaseStage;
        private GameStage m_InitialModeStage;

        protected override void OnAwake()
        {
            RegisterSystemOrder(
                typeof(InputSystem),
                typeof(AppSessionSystem),
                typeof(TaskSystem));

            EnterInitialStageGroup();
        }

        public void EnterInitialStageGroup()
        {
            m_BaseStage ??= ProjectStages.CreateBaseStage(this);
            m_InitialModeStage ??= ProjectStages.CreateInitialModeStage(this);
            StageWrapper.SetActiveGameStage(m_BaseStage, m_InitialModeStage);
        }
    }
}
```

如果当前已形成项目使用 `LoadStage` 而非集合式切换，沿用已有方式并说明原因。新建项目必须至少创建并调用一个具名 Group 入口，优先使用 `SetActiveGameStage` 表达“当前应活跃的 Stage 集合”，便于卸载不再需要的模式 Stage。

GameEngine 必须由 Bootstrap Scene 中的 GameObject 挂载。脚本存在但未挂场景仍属于占位。

## 3. Stage 组合

首次初始化至少提供一个 `EnterInitialStageGroup()`。Group 入口是运行时组合边界：在调用 `SetActiveGameStage` 之前准备所有必需 StartupData、创建本组 Stage，并一次性声明完整集合。没有外部输入的占位 BaseStage 不创建空 StartupData。

如果 BbxCommon 提供 Stage 入口窗口，还应为该初始 Group 准备一个 Editor Group 入口配置。Editor 资产只负责构造 StartupData 并调用同一个运行时 Group 入口，不复制 Stage 组合代码；资产必须由 Unity Editor 创建到 `Assets/Resources/Editor/`，不能手写 YAML。

建议拆为：

- BaseStage：常驻、跨模式的全局数据/系统；
- ModeStage：主菜单、战斗、关卡等模式专属内容；
- Overlay/Feature Stage：只有明确跨模式组合需求时再增加。

组装文件可用静态工厂，避免无状态的 `GameStage` 子类：

```csharp
using BbxCommon;

namespace ProjectName
{
    public static class ProjectStages
    {
        public static GameStage CreateBaseStage(ProjectGameEngine engine)
        {
            var stage = engine.StageWrapper.CreateStage("BaseStage");
            stage.AddDataGroup("Base");
            stage.AddLoadItem<InitializeAppSession>();
            stage.AddUpdateSystem<AppSessionSystem>();
            return stage;
        }

        public static GameStage CreateInitialModeStage(ProjectGameEngine engine)
        {
            var stage = engine.StageWrapper.CreateStage("InitialModeStage");
            // 有真实模式时再 AddScene / SetUiScene / AddDataGroup / AddSystem。
            return stage;
        }
    }
}
```

如果 Stage 需要自定义数据或重写职责，才创建 `GameStage` 子类并通过泛型 `CreateStage<T>()` 初始化。

Stage 加载项的一般顺序为：早期 LoadItem → Scene → UiScene → DataGroup → ECS System → StageListener → LateLoadItem；卸载反向执行。需要依赖配置、Scene、UI 或 System 已就绪的初始化放 LateLoadItem，不要仅靠添加顺序猜测。

## 4. IStageLoad

`IStageLoad` 的 `Load` 与 `Unload` 必须成对。适合：

- 创建/销毁 ECS 单例或一组实体；
- 实例化/销毁非 Scene 托管对象；
- 注册/解除外部回调；
- 在数据与 System 就绪后启动一次流程。

示意：

```csharp
private sealed class InitializeAppSession : IStageLoad
{
    public void Load(GameStage stage)
    {
        EcsApi.AddSingletonRawComponent<AppSessionSingletonRawComponent>();
    }

    public void Unload(GameStage stage)
    {
        EcsApi.RemoveSingletonRawComponent<AppSessionSingletonRawComponent>();
    }
}
```

如果 Component 已由别处创建，加载项不得重复创建。卸载只释放本加载项拥有的资源。

## 5. 数据组

静态配置与运行时状态分离：

- `BbxScriptableObject`：同类型通常一份；`OnLoad()` 中 `DataApi.SetData(this)`；
- `CsvDataBase<T>`：同结构多行；`ReadLine()` 中按键写入 DataApi；
- ECS Component：随运行变化的权威状态。

DataGroup 的各处名称和读取方式必须对应一致：

1. 配置声明的 LoadingType/GroupName 或 `GetDataGroup()`；
2. Stage 的 `AddDataGroup("同名组")`；
3. 资源登记/ResourcesDictionary；
4. 业务读取的 DataApi key；
5. Stage 卸载后的释放语义。

用户未定义静态配置时，创建 `PlaceholderSettingsData` 类型作为写法示例，但不创建对应 `.asset`、CSV 或 DataGroup。只有真实字段和消费者确定后才创建配置资产和数据组。

## 6. 启动顺序

推荐可追踪顺序：

```text
Bootstrap Scene
  → ProjectGameEngine.Awake
  → BbxCommon 内部 GameEngine Stage
  → EnterInitialStageGroup
  → SetActiveGameStage(Project BaseStage, InitialModeStage)
  → Data / Scene / UiScene / ECS System
  → LateLoadItem 发起首个业务流程
```

不要在静态构造函数、任意 MonoBehaviour 的 Awake 或 Controller 的 Init 中偷偷创建全局业务状态。

## 7. 检查清单

- [ ] 场景中只有一个项目 GameEngine。
- [ ] GameEngine 不含玩法更新逻辑。
- [ ] 当前 Stage 集合明确，退出模式时能卸载。
- [ ] 至少一个初始 Group 入口已被 GameEngine 实际调用，且入口通过一次 `SetActiveGameStage` 声明完整集合。
- [ ] Group 所需 StartupData 在 Stage 创建前已经构造，且只由初始化项消费后转成 ECS 状态。
- [ ] Stage 入口框架可用时，至少一份初始 Editor Group 入口资产已创建，或无法操作 Editor 的待办已明确记录。
- [ ] 每个 System 归属一个明确 Stage。
- [ ] 每个 IStageLoad 的所有权和反向释放明确。
- [ ] DataGroup 名称从配置到 Stage 一致。
- [ ] 必须后置的初始化使用 LateLoadItem。
- [ ] 多 System 顺序通过 GameEngine 的 `RegisterSystemOrder(typeof(...), ...)` 类型列表表达，而非 DOTS 顺序 Attribute 或 Stage 的 Add 顺序。
- [ ] 未登记 System 是否允许落在已登记项末尾已经确认。
