# 如何创建 GameStage 并挂到 GameEngine 上

## 1. 前提

- 项目中存在继承 **`GameEngineBase<TEngine>`** 的单例 **`MonoBehaviour`** 作为入口，通过 **`StageWrapper`** 创建并管理 Stage。
- 单个 **`GameStage`** 与当前 ECS **`World`** 绑定。多个 Stage 并存时，GameEngine 使用 **`RegisterSystemOrder(typeof(...), ...)`** 统一登记 System 执行顺序；Stage 的添加顺序只影响未登记 System 之间的末尾相对顺序。

## 2. 创建并配置

1. **`StageWrapper.CreateStage(string stageName)`**  
   得到 **`GameStage`**，再 **`AddScene` / `AddLoadItem` / `SetUiScene` / `AddUpdateSystem`** 等完成配置。

2. **`StageWrapper.CreateStage<T>(string stageName) where T : GameStage, new()`**  
   需要派生 **`GameStage`** 时用泛型创建；框架会调用 **`Init(stageName, ecsWorld)`**。

业务代码应再封装一层具名工厂，集中声明 Stage 的所有内容。若 Stage 有缺一不可的外部输入，工厂签名必须显式接收强类型 `XxxStageStartupData`，并在添加任何 Stage 项之前完成非空检查、结构校验和快照保存。

```csharp
public static GameStage CreateBattleStage(
    PakGameEngine engine,
    BattleStageStartupData startupData)
{
    if (startupData == null)
        throw new ArgumentNullException(nameof(startupData));

    var snapshot = startupData.CreateSnapshot();
    snapshot.ValidateStructure();

    var stage = engine.StageWrapper.CreateStage("BattleStage");
    stage.SetStageData(BattleStageStartupDataKey, snapshot);

    // AddScene / AddDataGroup / AddUpdateSystem / AddLateLoadItem ...
    return stage;
}
```

- 工厂接收的是业务启动契约，不是 Editor 专用配置资产。
- 工厂保存独立快照，调用方之后修改自己的集合或配置不会影响运行中的 Stage。
- 需要输入的 Stage 不提供无参工厂。正式游戏流程也应在组合 Stage 时显式构造生产输入；默认值属于正式入口的组装策略，不属于 Stage 内部兜底。
- 不为此引入 `GameStage<T>` 泛型基类。`GameStage` 仍负责生命周期，强类型由工厂、`StartupData` 和初始化项共同保证。
- 完整约束见 [stage-startup-data-convention.md](./stage-startup-data-convention.md)。

## 3. 编写 Group 入口并激活 Stage

使用 **`StageWrapper.SetActiveGameStage(params GameStage[] stages)`** 声明当前应活跃的业务 Stage 集合。每个可进入的组合必须由一个命名明确的 Group 入口封装；不要把 Stage 创建、StartupData 构造和 `SetActiveGameStage` 散落在 Controller 或多个事件回调中。框架会自动卸载不在列表中的业务 Stage（反向顺序），再加载列表中尚未加载的 Stage（正向顺序）。框架启动时创建的 **Game Engine Stage** 会自动保持活跃，不需要也不能由业务代码加入参数；模式切换不会因此中断其中的 `InputSystem`、`TaskSystem` 或引擎默认数据。

```csharp
// OnAwake 初始加载
protected override void OnAwake()
{
    RegisterSystemOrder(
        typeof(InputSystem),
        typeof(ExampleGameplaySystem),
        typeof(TaskSystem));

    EnterInitialStageGroup();
}

private void EnterInitialStageGroup()
{
    m_BaseStage = ProjectStages.CreateBaseStage(this);
    StageWrapper.SetActiveGameStage(m_BaseStage);
}

public void EnterLevelStageGroup(LevelStageStartupData startupData)
{
    var levelStage = ProjectStages.CreateLevelStage(this, startupData);
    StageWrapper.SetActiveGameStage(m_BaseStage, levelStage);
}
```

`RegisterSystemOrder` 接收 `EcsSystemBase` 派生类型。已登记类型在各自的 Update 或 FixedUpdate 组内按列表顺序执行；未登记类型保持原相对顺序并追加到末尾。通常应在创建和加载业务 Stage 之前完成登记。

对有必需输入的 Stage，先由当前正式入口或 Editor Group 入口构造启动数据，再调用运行时 Group 入口；不要在 `SetActiveGameStage` 之后补写零散的初始化状态：

```csharp
var battleStartupData = productionFlow.CreateBattleStartupData();
gameEngine.EnterBattleStageGroup(battleStartupData);
```

同一 Stage 实例重新激活时复用其已保存快照。若新一局需要不同输入，Group 入口必须创建新的 Stage 实例；不得修改旧实例的 StartupData 后假装重新初始化。

## 4. 与 `UiScene` 的配合

Stage 接入 UiScene 前先按 `bbxcommon-ui` 完成配置源与导出，顺序固定为：

1. 定义 UiGroup 枚举与 `UiSceneBase<T>`，在 `OnSceneInit` 创建全部 Group。
2. 创建或修改 UI 编辑场景，配置 Canvas、`UiSceneExporter`、`FullUiGroupType`、UiGroups 和 View Prefab 实例。
3. 从该场景导出 `UiSceneAsset` 并检查导出项；禁止手写或直接修改 `UiObjectDatas`。
4. 让 GameEngine 持有或按项目既有方式加载该导出 Asset。
5. 在 Stage 工厂中先通过 **`GetOrCreateUiScene<T>()`** 取得 **`UiSceneBase`**，再 **`stage.SetUiScene(uiScene, uiSceneAsset)`**。**`SetUiScene` 每个 Stage 只能调用一次**。
6. 从默认 Main 入口验证加载、默认显隐、卸载和再次进入。

缺少 UI 编辑场景或无法从该场景重新导出时，即使 Asset 和 `SetUiScene` 已存在，也不能把 UiScene 注册判为完成。
