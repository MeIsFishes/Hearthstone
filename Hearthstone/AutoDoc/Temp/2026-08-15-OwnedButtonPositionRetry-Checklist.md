# “查看拥有”按钮位置调整重试检查清单

## 1. 用户验收项

- [未通过] 当前会话原生 Unity MCP 仍返回 `Transport closed`，无法执行 Builder，按钮继续位于 `(-720, 285)`。
- [不适用] 未落地任何按钮修改，尺寸、Y 坐标、文字、视觉样式和交互保持原状。

## 2. MCP 与实现验证

- [未通过] 当前会话原生 `manage_scene` 与 `read_console` 同时返回 `Transport closed`；标准 SDK 只读探针则两项均成功，证明断点只在本 Codex 会话传输层。
- [通过] 未关闭、重启或进入 Play Mode；只读探针确认活动场景 `Main` 未脏，Console 为 0 条错误。
- [未通过] 为避免 Builder 与 Prefab 不一致，未修改 Builder；当前仍为 `(-720, 285)`。
- [未通过] 未修改测试；目标几何关系仍已确认：`-770 - 240/2 = -890`，与卡池面板左边缘一致。
- [未通过] 原生 MCP 写入通道不可用，未执行 `PreparationViewUiBuilder.Build()`。
- [未通过] Prefab 未重建，仍为坐标 `(-720, 285)`、尺寸 `240 × 42`。
- [未通过] 无已落地改动可测，未运行 EditMode 测试；只读 Console 检查通过。
- [通过] 未修改项目代码、Prefab、测试或 `.meta`；未触碰无关项目文件。

## 3. 框架边界审计

- [通过] 未留下框架外实现；后续仍须由一一对应的 `PreparationViewUiBuilder` 经原生 MCP 重建 View Prefab。
- [通过] 未修改 Controller，未手写 Prefab 或 `UiSceneAsset` YAML。
- [不适用] 目标只改变页面内部子控件坐标，不影响 UiScene 的 Group、默认显隐、整体位置、缩放或 Pivot，无需重新导出。

## 4. 文档同步审计

- [通过] 已完整读取 `design-doc-format`、`art-doc-writer`、`program-doc-format` 基础 skill。
- [不适用] 实际玩家界面未改变，不更新玩家视角现状文档。
- [不适用] 实际布局未改变，不更新美术现状文档。
- [不适用] Builder 与 Prefab 均未改变，不更新程序现状文档。

## 5. 结束审计

- [通过] 已按原生 MCP 调用、只读探针和项目文件状态逐项复核。
- [未通过] Unity 重启未刷新当前 Codex 会话持有的 stdio 连接；必须新建或重载 Codex 会话后才能继续。
- [通过] 已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 0；清理后临时区共有 170 个 Markdown，未达到裁剪阈值。
- [通过] 清理后已创建 `2026-08-15-OwnedButtonPositionRetry-Report.md`。
