# “查看拥有”按钮位置调整检查清单

## 1. 用户验收项

- [未通过] 目标坐标已算出为 `(-770, 285)`，可让 240 px 宽按钮的左边缘与 1780 px 宽卡池面板左边缘同为 `-890`；但当前会话 Unity MCP 写入通道固定返回 `Transport closed`，Prefab 未重建，因此未落地。
- [不适用] 因未落地任何按钮改动，现有尺寸、文字、视觉样式和交互保持原状；恢复 MCP 后仍应只改 X 坐标。

## 2. 实现与验证

- [通过] 已定位 `PreparationView.prefab`、`PreparationViewUiBuilder`、`PreparationView`、`PreparationController`、布局测试和现有备战 UI 文档。
- [未通过] 曾在 Builder 中准备 X 坐标调整，但因无法用用户指定的 MCP 重建 Prefab，为避免配置源与产物不一致已撤回；未增加运行时布局补丁。
- [未通过] `PreparationViewUiBuilder.Build()` 的 MCP 调用返回 `Transport closed`，Prefab 仍保持 `(-720, 285)`。
- [通过] 已核算当前锚点/Pivot：面板半宽 890、按钮半宽 120，目标中心 X 为 `-890 + 120 = -770`；Y 无需变化。
- [通过] 已删除用户叫停的临时副本，撤回未落地源码/测试改动；未创建、编辑或删除项目 `.meta`，未触碰无关文件。
- [未通过] MCP 后端只读探针可读取活动场景 `Main` 且 Console 为 0 条错误，但当前会话原生 MCP 传输关闭，无法完成 Prefab 构建、结构测试与游戏内视觉验证。

## 3. 框架边界审计

- [通过] 未留下任何框架外实现；恢复后仍应通过现有 `PreparationViewUiBuilder` 与 MCP 更新 Prefab。
- [通过] 未修改 Controller，未手写 Prefab 或 `UiSceneAsset` YAML。
- [不适用] 目标仅是 `PreparationView` 内部子控件位置，不改变页面整体 Position、Scale、Pivot、UiGroup 或默认显隐，因此无需重新导出 UiScene。

## 4. 文档同步审计

- [通过] 已完整读取 `design-doc-format`、`art-doc-writer`、`program-doc-format` 基础 skill。
- [不适用] 已核对 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`；因实际玩家界面未改变，不更新现状文档。
- [不适用] 已核对 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md`；因实际布局未改变，不更新美术现状文档。
- [不适用] 已核对 `AutoDoc/Program/UI/preparation/preparation.md`；Builder 与 Prefab 均恢复原坐标，不更新程序现状文档。

## 5. 结束审计

- [通过] 已重新读取清单并按源码、Prefab、MCP 探针与临时目录证据逐项审计。
- [未通过] 当前会话 MCP 传输无法在不重载会话的条件下修复；后端已验通，所需外部动作是重载或新建 Codex 会话。
- [通过] 已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 0；清理后临时区共有 167 个 Markdown，未达到裁剪阈值。
- [通过] 清理后已创建 `2026-08-15-OwnedButtonPosition-Report.md`。
