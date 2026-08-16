# 主菜单版本号报告

## 结果

主菜单右下角已新增低对比深棕色版本文字，当前显示 `v0.1.0`。项目 `PlayerSettings.bundleVersion` 已同步为 `0.1.0`；页面打开时由 `Application.version` 刷新显示，后续只需修改构建版本即可同步界面。

## 实现与资产

- `MainMenuView` 新增版本 TMP 引用。
- `MainMenuController` 在页面打开时读取 `Application.version`。
- `MainMenuViewUiBuilder` 在右下角生成无射线、右对齐版本文字。
- 已通过 Builder 重建主菜单 Prefab、编辑场景和 UiSceneAsset。
- 已同步开始界面设计、主菜单程序和主菜单/UI 美术文档。

## 验证

- 主菜单与图鉴相关 EditMode：`14/14` 通过。
- 全量 EditMode：`101/105` 通过。以下 4 项既有玩法测试在当前工作树独立执行也失败，与本次版本文字和项目版本修改无调用关系：
  - `BattleKeywordRulesTests.ScenarioDataSupportsEmptyAndExplicitSlotsWithDefensiveCopies`
  - `BattleRulesTests.LivingAttackerSequenceWrapsFromLeftToRight`
  - `PreparationContinueTests.ContinueLineup_DefensivelyCapturesSparseSlots(3)`
  - `RunCardRulesTests.TryPlaceCard_ReplacesAndMovesWithoutDuplicates`
- 最终重新编译后 Unity Console：`0` error。
- 未进入 Play Mode。

## 清理

本任务结束前已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，随后创建本报告。
