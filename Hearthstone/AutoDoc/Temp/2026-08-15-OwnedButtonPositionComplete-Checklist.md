# “查看拥有”按钮位置调整完成检查清单

## 1. 用户验收项

- [通过] Unity 重载 Prefab 后确认按钮位于 `(-770, 285)`；`240 px` 宽按钮与 `1780 px` 宽卡池面板左边缘均为 `-890`。
- [通过] Unity 实际读取尺寸仍为 `240 × 42`、Y 仍为 `285`、标签仍为“查看拥有”，Toggle 与 Label 序列化引用均有效。

## 2. 实现与验证

- [通过] 修改前后 Unity 均完成编译，最终清空 Console 后错误为 0。
- [通过] `PreparationViewUiBuilder` 仅把 `OwnedOnlyToggle` 坐标从 `(-720, 285)` 改为 `(-770, 285)`；同步更新直接依赖的测试断言。
- [通过] 已通过 Unity MCP 成功调用 `Hearthstone.PreparationViewUiBuilder.Build()`，返回 `PreparationView rebuilt`，Prefab 已实际重建。
- [通过] Unity `AssetDatabase` 重载验证根 `PreparationView`、Toggle/Label 引用、坐标、尺寸、标签和左右边缘关系均正确。
- [通过] EditMode 测试 `Hearthstone.Tests.RunCardRulesTests.PreparationSharedCardAndResourcesAreFullyExported` 通过；未进入 Play Mode。
- [通过] 相关 `.meta` 状态无变化；保留工作区原有大量改动，仅修改本任务直接相关的 Builder、Prefab、测试与三类现状文档。

## 3. 框架边界审计

- [通过] 沿用一一对应的 `PreparationViewUiBuilder` 与 View Prefab，没有新增运行时布局补丁。
- [通过] Prefab 由 Builder 经 Unity Editor API 重建，未手写 Prefab 或 `UiSceneAsset` YAML，未绕过 BbxCommon UI 生命周期与导出流程。
- [不适用] 仅改变页面内部子控件坐标，不涉及 UiScene Group、默认显隐、整体位置、缩放或 Pivot，无需重新导出 UiScene。
- [通过] 未新增函数、字段或一次性抽象。

## 4. 文档同步审计

- [通过] 已完整读取 `design-doc-format`、`art-doc-writer`、`art-module-format`、`program-doc-format` 与 `ui-screen-doc-format`，并核对备战卡池相关现状文档。
- [通过] 玩家视角设计文档已同步：补充“查看拥有”横框左边缘与卡池面板左边缘贴齐。
- [通过] 美术文档已同步：在模块风格中记录蓝金窄横框与卡池面板左边缘贴齐。
- [通过] 程序文档已同步：记录 `OwnedOnlyToggle` 局部坐标 `(-770, 285)`、尺寸与 `-890` 左边缘关系。

## 5. 结束审计

- [通过] 已按 Builder、Prefab、测试、Unity 实际读取结果、Console、Editor 状态和三类文档逐项复核。
- [通过] 已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0。
- [通过] 清理后已创建对应 `2026-08-15-OwnedButtonPositionComplete-Report.md`。
