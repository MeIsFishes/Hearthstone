---
name: game-stage
description: 设计 GameStage、编写入口并进入特定游戏 StageGroup 时使用。
---

# GameStage

## 适用范围

在设计、创建或修改 `GameStage`，编写运行时 `StageGroup` 入口，或创建可从 Game Stage Editor 窗口或 Editor 脚本直接进入特定游戏场景的入口文件时使用本规范。

## 使用路线

先区分本次要处理的层次，再读取对应说明：

1. **设计单个 GameStage**：先读 [how-to-create-game-stage.md](./how-to-create-game-stage.md)，再按需读 [how-to-add-items-in-game-stage.md](./how-to-add-items-in-game-stage.md)，确定 Stage 的职责边界以及需要挂载的场景、数据、System、UI、监听器和初始化项。只要涉及 UiScene，还必须执行 `bbxcommon-ui` 的“新增/修改 UiScene”流程，核对 UI 编辑场景与实际导出 Asset 后才能注册。
2. **为 Stage 定义必需输入**：当 Stage 缺少关卡、角色、队伍或敌方配置等外部选择就无法运行时，读 [stage-startup-data-convention.md](./stage-startup-data-convention.md)，定义 `XxxStageStartupData`、Stage 工厂快照和一次性运行时初始化。
3. **进入特定游戏场景（StageGroup）**：在 GameEngine 上编写具名 Group 入口，先构造完整 Stage 集合，再调用一次 `StageWrapper.SetActiveGameStage(...)`。这里的“游戏场景”指一组应同时活跃的业务 Stage，不等同于单个 Unity Scene。
4. **编写 Editor 入口文件**：需要从 Game Stage 窗口或 agent Editor 脚本携带指定输入直接进入某个 StageGroup 时，必须读 [stage-entry-window.md](./stage-entry-window.md)，创建 `XxxStageEntryAsset.cs` 和位于 `Assets/Resources/Editor/` 的入口配置资产。入口构建类通过 `CreateStageGroupBuildCallback()` 携带 Editor-only 函数回调，可以在回调中等待特殊启动条件，而不改变运行时 Group 入口。

三层职责不可混用：`GameStage` 声明一个作用域，运行时 Group 入口声明完整活跃集合，Editor 入口文件只编辑输入、构造 StartupData 并调用该运行时 Group 入口。

## 框架层与项目层

- **BbxCommon 运行时框架**提供 `GameStage` 生命周期、`StageWrapper.SetActiveGameStage(...)` 和完整 Stage 集合切换能力。
- **BbxCommon Editor 框架**提供 `GameStageEntryAsset`、`GameStageEntryLauncher`、`GameStageWindow`、入口资产创建与目录协议，以及校验、保存、`SessionState` 和跨域 Play Mode 派发。
- **具体项目**只定义 Stage 工厂、具名 StageGroup 组合方法、强类型 StartupData、`XxxStageEntryAsset` 业务适配类和入口资产。
- 通用进入能力缺失时应补到 `Assets/Scripts/BbxCommon/`；禁止在项目程序集复制 Launcher、runner、入口发现、资产创建或跨域派发逻辑。

## 核心模型

**GameStage** 是一组作用域逻辑项（ECS System、UiScene、DataGroup、场景、`IStageLoad` 等）的容器。**当前所有活跃 Stage 所包含项的并集，就是当前游戏中生效的全部逻辑项集合。**

设计原则是**多个独立 Stage 组合**：不同 Stage 承载不同职责（如常驻的 BaseStage、关卡内的 LevelStage），通过 **`StageWrapper.SetActiveGameStage`** 声明当前应活跃的业务 Stage 集合，框架自动卸载不在列表中的业务 Stage（反向顺序），再加载列表中尚未加载的 Stage（正向顺序）。框架内部的 **Game Engine Stage** 不属于业务声明集合，并会被自动保留，以持续提供 `InputSystem`、`TaskSystem` 和引擎默认数据。

GameStage 只声明自己包含哪些 ECS System；跨 Stage 的最终顺序由 `GameEngineBase.RegisterSystemOrder(typeof(...), ...)` 统一登记。已登记类型按列表顺序执行，未登记类型保持原相对顺序并追加到末尾；Update 与 FixedUpdate 在各自更新组内应用同一份顺序表。

## 进入特定游戏场景（StageGroup）

一组准备同时激活的业务 Stage 称为一个 **GameStage Group**。Group 是业务组合概念，不要求新增 `StageGroup` 运行时容器或泛型基类；每个可进入组合必须有一个命名明确的 **Group 入口**，例如 `EnterInitialStageGroup()`、`EnterBattleStageGroup(BattleStageStartupData startupData)`。

Group 入口是一次 Stage 切换的唯一业务边界，按以下顺序工作：

1. 从正式流程选择或 Editor 入口配置构造本次所需的全部 `XxxStageStartupData`；
2. 使用这些数据调用具名 Stage 工厂，得到本 Group 需要的全部 Stage 实例；
3. 调用一次 `StageWrapper.SetActiveGameStage(...)` 声明完整活跃集合。

禁止在 `SetActiveGameStage` 之后、Scene 加载过程中、System 首帧或任意 Controller 中补造 StartupData。切回已经创建的同一 Stage 实例时复用其创建时保存的快照；需要不同输入时应创建新的 StartupData 和新的 Stage 实例，不能修改旧快照。

## 启动数据基线

如果一个 Stage 缺少外部选择或初始值就无法正确运行，必须定义一个强类型的 **`XxxStageStartupData`**，作为入口与 Stage 运行环境之间的唯一中转对象；不为无输入 Stage 创建空数据类型，也不要求继承泛型 Stage 基类。

数据流固定为：

```text
正式入口默认值 / Editor Group 入口配置
    → Group 入口
    → XxxStageStartupData 快照
    → DataGroup / DataApi 解析正式配置
    → ECS Component 运行状态
    → System / UI
```

- `StartupData` 只保存入口必须决定、且 Stage 无法自行推导的信息，例如关卡 ID、我方单位 ID、敌方编成 ID；不复制 CSV 或 ScriptableObject 中已经存在的完整配置。
- Editor 配置资产只负责编辑和构造 `StartupData`，不得作为可变运行时对象传入 Stage。
- ECS Component 是可变运行状态的唯一来源。初始化完成后，System 与 UI 不得逐帧读取入口资产或把 Component 回写到 `StartupData`。
- 需要输入的 Stage 工厂必须显式接收 `StartupData`；不得用无参重载、隐藏硬编码或静默回退掩盖缺失输入。
- `StartupData` 只由 Group 切换边界构造或接收，Stage 工厂保存快照，专用初始化项消费一次；普通 System 与 UI 不逐帧读取它。
- 详细写法见 [stage-startup-data-convention.md](./stage-startup-data-convention.md)。

## 文档索引

| 文档 | 说明 |
|------|------|
| [how-to-create-game-stage.md](./how-to-create-game-stage.md) | 设计并新建 GameStage，以及编写运行时 Group 入口 |
| [how-to-add-items-in-game-stage.md](./how-to-add-items-in-game-stage.md) | Stage 内可添加项、API，以及 `LoadItem` 与 `LateLoadItem` |
| [stage-startup-data-convention.md](./stage-startup-data-convention.md) | Stage 有必需外部输入时的数据中转、校验和运行时初始化规范 |
| [stage-entry-window.md](./stage-entry-window.md) | 编写 `XxxStageEntryAsset.cs`，并创建可编辑、可直接运行的 Editor 入口配置 |

## 与其它 skill 的分工

- **ECS System** 注册、`DisableAutoCreation`：**`bbxcommon-ecs`**（子文档 `new-class-examples/mix-system.md`）
- **配置数据 / DataGroup 与 CSV、BbxScriptableObject**：**`config-data-design`**
- **UiScene / 页面级 UI**：**`bbxcommon-ui`**。GameStage 只消费由 UI 编辑场景通过 `UiSceneExporter` 生成的 Asset；没有编辑场景、Exporter 或可追溯导出来源时，不得用手写 Asset 完成注册。
