# 备战空槽与点数文本内缩调整检查清单

- [通过] 核对共享空槽 `RectTransform` 当前实际边界、出战/融合槽缩放和相邻中心距。证据：Unity Prefab 实测卡根 `250 × 360`，两种槽位空态原为拉伸子节点；当前出战/融合步长分别为 `185 × 266.4`、`190 × 273.6`。
- [通过] 将出战与融合空槽图自身进一步内缩，保持两者继续复用 `PreparationPoolEmptySlot.png`。证据：`PreparationSlotEmptyInset = 12f`，两种空态矩形均为 `226 × 336`，未新增 Sprite。
- [通过] 确认占用卡片、拖放命中、投放高亮和列表布局不因空槽图内缩而改变。证据：仅两个空态子节点应用内缩；共享条目根、`UiDragable`、`UiInteractor`、投放高亮及 `UiList` 步长均沿用现有路径。
- [通过] 将“当前点数”的说明文本与数字文本向底板中部收拢，保留左/右对齐和清晰间隔。证据：标签边界 `-90~10`、数值边界 `10~80`，最宽预期值下实际字形间隔约 `14.56 px`。
- [通过] 将“剩余点数”的说明文本与数字文本向底板中部收拢，参数与当前点数组一致。证据：Builder 与测试对两组文本使用相同尺寸、位置和对齐。
- [通过] 扩展检查脚本。证据：`BattleRulesTests` 验证 `-24 × -24` 子节点尺寸增量及 `57.8976/49.1568 px` 相邻空槽间距；`RunCardRulesTests` 验证两组文本边界、外边距及 `10~20 px` 字形间隔。
- [通过] 通过一一对应的 `BattleCardItemUiBuilder` 与 `PreparationViewUiBuilder` 重建 Prefab。证据：Unity Builder 执行成功并写入 `BattleCardItem.prefab`、`PreparationView.prefab`；未手写 Prefab/Scene/Asset YAML。
- [不适用] UiScene 导出信息未变化。证据：未改 UiGroup、View 导出字段或场景连接，`Assets/Resources/Ui/Preparation.asset` 无工作区差异，故未重导。
- [未通过] 完整验证未能全绿，但本次范围验证通过。证据：定向 EditMode `2/2` 通过；完整 EditMode `92/93`，唯一失败为工作区另一任务的 `GeneratedResultPanelsAndPreparationDeployedStateReplaceRuntimeText`（期望 `BattleVictoryBannerAged`、Prefab 当前为 `BattleVictoryBanner`）；`Hearthstone.csproj`、`Hearthstone.Ui.Editor.csproj`、`Hearthstone.Tests.csproj` 均 `0` 错误；Unity 几何检查通过，Console 清理后 `0` error；活动场景 `Main`、未 dirty、未进入 Play Mode。
- [通过] 框架边界审计。证据：继续使用既有 View/Controller、共享 Prefab、`UiList`、对象池、预加载映射和两个 UiBuilder，无平行静态 UI 或运行时拼装绕行。
- [通过] 检查误改无关文件、遗漏直接依赖和一次性抽象。证据：本次范围仅涉及两个 Builder、对应两个 Editor 测试、生成 Prefab 与三篇现状文档；保留工作区既有其他改动，范围文件 `git diff --check` 通过。
- [通过] 玩家视角设计文档。证据：已完整读取 `design-doc-format`，同步空槽图自身内缩及点数文本向中部收拢的当前玩家可见体验。
- [通过] 美术文档。证据：已完整读取 `art-doc-writer` 与模块格式，同步 `226 × 336` 空槽边界、相邻间距和点数文本留白。
- [通过] 程序文档。证据：已完整读取 `program-doc-format` 与 UI 界面格式，同步 Builder 参数、缩放、边界与检查数据。
- [通过] 结束前逐项复核证据；清理脚本与报告将在本审计保存后执行。
