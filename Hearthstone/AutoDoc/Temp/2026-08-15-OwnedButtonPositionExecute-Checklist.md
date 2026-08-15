# “查看拥有”按钮位置调整执行检查清单

## 1. 用户验收项

- [未通过] 目标位置已确认应为 `(-770, 285)`，但 Unity 当前存在 7 个无关编译错误，更新后的 Builder 无法装载，Prefab 仍为 `(-720, 285)`。
- [不适用] 本次未落地按钮资产修改，现有 Y 坐标、尺寸、文字、视觉样式和交互保持原状。

## 2. 实现与验证

- [未通过] 曾将 Builder 的目标坐标改为 `(-770, 285)`；发现项目无法编译后已撤回该单行改动，避免 Builder 与 Prefab 不一致。
- [未通过] Unity MCP 连接和 Editor 状态正常，但强制脚本刷新后 Console 报 7 个 `BattleRulesTests.cs` 编译错误：测试引用旧的 `AttackAudioKey`、`AttackAudioDelay`、`HitDelay`，而 `BattleCardTypeCsvData` 已改为复数数组字段。
- [未通过] 因项目编译失败，Unity 无法装载更新后的 Builder；未调用旧程序集中的 `Hearthstone.PreparationViewUiBuilder.Build()`，避免重建出旧坐标。
- [未通过] Prefab 未重建，文件核对仍为坐标 `(-720, 285)`、尺寸 `240 × 42`。
- [未通过] 项目测试程序集无法编译，不能运行受影响的 Editor 测试；未进入 Play Mode。
- [通过] 未创建、编辑或删除 `.meta`；只新增本任务临时清单/报告，未覆盖无关工作区改动。

## 3. 框架边界审计

- [通过] 未留下运行时布局补丁；Builder 与 Prefab 最终都保持旧坐标 `(-720, 285)`。
- [通过] 未手写 Prefab 或 `UiSceneAsset` YAML，未绕过 BbxCommon UI 生命周期与导出流程。
- [不适用] 未落地页面内部坐标变化；目标本身不涉及 UiScene Group、默认显隐、整体位置、缩放或 Pivot，后续完成时无需重新导出。
- [通过] 未新增函数、字段或一次性抽象。

## 4. 文档同步审计

- [通过] 已完整读取 `design-doc-format`、`art-doc-writer`、`program-doc-format`，并核对备战卡池玩家、美术和程序现状文档。
- [不适用] 玩家视角设计文档：实际玩家界面未改变，不更新现状文档。
- [不适用] 美术文档：实际 Prefab 布局未改变，不更新现状文档。
- [不适用] 程序文档：Builder 与 Prefab 最终均保持旧坐标，不更新现状文档。

## 5. 结束审计

- [通过] 已按 Builder/Prefab 文件、Unity MCP Editor 状态和 Console 编译诊断逐项复核。
- [通过] 已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0。
- [通过] 清理后已创建对应 `2026-08-15-OwnedButtonPositionExecute-Report.md`。
