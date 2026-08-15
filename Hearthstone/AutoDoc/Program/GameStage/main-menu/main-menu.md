# 主菜单 GameStage 程序文档

## 1. 该GameStage所代表的逻辑含义

`MainMenuStage` 代表游戏完成全局数据加载后、玩家开始一局之前的主菜单阶段。该阶段不创建 `RunStateStage`，因此不存在已开始的本局卡牌或轮次状态。

## 2. 系统中与哪些GameStage组合

`MainMenuStage` 作为 `EHearthstoneStageGroup.MainMenu` 的唯一业务 Stage 单独激活，不与 `RunStateStage`、`PreparationStage` 或 `BattleStage` 同组。

## 3. 在何时加载、卸载

`HearthstoneGameEngine` 初次收到 Game Engine Stage 加载完成回调且当前没有已激活或已请求的 Hearthstone StageGroup 时，调用 `EnterMainMenuStageGroup()` 加载本 Stage。玩家在主菜单点击“开始游戏”后，`StartNewRun()` 重置 StageGroup 协调器、创建新 `RunStateStage` 并请求第 1 轮 `PreparationStage`；底层 Stage 批次在切换时卸载 `MainMenuStage`。

## 4. LoadItem项

当前无。

## 5. 逻辑项

`MainMenuStages.CreateMainMenuStage()` 创建具名 Stage，验证 UI Canvas 原型和 `Resources/Ui/MainMenu` 导出资产，然后把 `MainMenuUiScene` 登记到 Stage。StageGroup 协调器使用稳定请求键 `main-menu` 合并重复请求。

### 5.1 System列表和简要功能概述

当前无。

### 5.2 StageListener列表和简要功能概述

当前无。

### 5.3 可能启用的Task流程和简要功能概述

当前无。

## 6. 关联UI

| UiScene | Resources 资产 | 默认 View / Controller |
| --- | --- | --- |
| `MainMenuUiScene` | `Ui/MainMenu` | `MainMenuView` / `MainMenuController` |

## 7. 读取的配置数据

当前无。本 Stage 只读取 `UiSceneAsset`，不读取玩法 CSV 或业务 ScriptableObject。
