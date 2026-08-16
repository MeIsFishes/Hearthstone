# 备战运行时空槽实际尺寸修正报告

## 任务结果

- 已确认运行时使用的是 `PreparationBattleSlotEmptyState` / `PreparationFusionSlotEmptyState` 专用空态；此前仍显得很大，是因为四边仅内缩 `12 px`，视觉宽度只减少约 `10%`。
- 两个专用空态现改为四边内缩 `50 px`，继续复用 `PreparationPoolEmptySlot.png`。源矩形为 `150 × 260`，保持图片比例后的实际绘制区为 `150 × 225`。
- 出战空槽运行时可见尺寸约 `84.36 × 126.54`，相邻间距 `100.64 px`；融合空槽约 `93.48 × 140.22`，相邻间距 `96.52 px`。
- 只缩小空槽 Image；完整条目根、拖放命中、投放高亮、占用卡面、列表步长与对象池生命周期保持不变。

## 检查项与验证

| 检查范围 | 状态 | 证据 |
| --- | --- | --- |
| 真正生效的运行时空态 | 通过 | Controller 仅在对应槽位绑定且未占用时开启两个专用空态；测试检查显隐条件 |
| 空槽尺寸 | 通过 | Prefab 实测源矩形 `150 × 260`、绘制区 `150 × 225`，空槽宽度为占用卡面 `60%` |
| 相邻间距 | 通过 | 出战 `100.64 px`、融合 `96.52 px`，均保持明显正间距 |
| 交互与框架边界 | 通过 | 未修改条目根、射线面、拖拽组件、Interactor、UiList、对象池或预加载映射 |
| Builder 与 Prefab | 通过 | Unity Editor 执行 `BattleCardItemUiBuilder.Build()` 并保存 `BattleCardItem.prefab`；未手写 YAML，未修改 `.meta` |
| EditMode | 通过 | 定向测试 `1/1`，完整测试 `101/101` |
| 程序集构建 | 通过 | `Hearthstone.csproj`、`Hearthstone.Ui.Editor.csproj`、`Hearthstone.Tests.csproj` 均 `0` 错误；仅有既有依赖版本冲突警告 |
| Unity 状态 | 通过 | Console 清理后 `0` error；活动场景 `Main`，未 dirty，未进入 Play Mode |
| 现状文档 | 通过 | 玩家设计、美术模块和程序 UI 文档已同步实际尺寸、间距与运行时显隐链路 |
| UiScene 导出 | 不适用 | 导出字段、Group、整体 Transform 和场景连接未变化，`Preparation.asset` 无差异 |

## 偏差与风险

- 按项目默认未主动进入 Play Mode；尺寸结果通过 Builder 产物、运行时缩放参数、Unity Prefab 几何实测和完整 EditMode 回归确认。
- 本次没有新增资源或独立 UI；工作区已有其他改动均保持原状。

## 清理结果

- 检查清单已逐项复核。
- `AutoDoc/CleanupTempDocs.bat` 已且仅已运行一次，退出码 `0`；运行时临时 Markdown 共 `243` 篇，未达到阈值，未删除文件。
