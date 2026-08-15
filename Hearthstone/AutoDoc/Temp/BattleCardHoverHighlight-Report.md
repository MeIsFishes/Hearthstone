# 卡牌鼠标悬停边框高亮任务报告

## 结果

已将战斗与备战共享卡牌的基础框、攻击者框和目标框统一为同一张中性银白 `CardFrame-v3` Sprite，并通过 `Image.color` 显示敌方红 `#D23730`、我方/备战蓝 `#3773EB`。备战阶段已持有卡在鼠标进入时切换为黄 `#FFD230`，移出时恢复蓝色。

悬停和拖拽均受“备战绑定且条目有卡”开关控制。共享 Prefab 中悬停监听、悬停射线、拖拽、拖拽监听和投放组件默认关闭；战斗绑定不启用悬停或拖拽。回池、换绑、空槽切换或关闭备战交互时会清理悬停状态，避免黄色串到下一张卡。

## 主要产物

- `Assets/Resources/Art/BattleCards/UI/CardFrame-v3.png`：中性银白透明统一边框。
- `Assets/Resources/Ui/BattleCardItem.prefab`：新增独立透明 `HoverInput`，并保存默认关闭的备战交互状态。
- `Assets/Scripts/Hearthstone/Ui/View/BattleCardItemView.cs`：序列化持有 `CardHoverInput` 与 `CardHoverListener`。
- `Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs`：统一 Sprite、三色表、备战限定悬停/拖拽开关及对象池清理。
- `Assets/Scripts/Hearthstone/Ui/Editor/BattleCardItemUiBuilder.cs`：维护 HoverInput、统一框 Sprite 和 Prefab 默认交互状态。
- `Assets/Scripts/Hearthstone/Tests/Editor/BattleRulesTests.cs`：覆盖单 Sprite、颜色表、序列化悬停输入和备战限定默认状态。

## 图片生成与透明通道处理

- 使用方式：内置 `imagegen` 图片编辑。
- 输入参考：项目原 `Assets/Resources/Art/BattleCards/UI/CardFrame-v3.png`。
- 提示词要点：`Precise asset edit: preserve the exact 1024x1536 frame geometry and transparent silhouette; convert all baked red/blue/gold faction colors to neutral silver-white grayscale suitable for Unity Image.color tinting; no text, no extra ornament, no background.`
- imagegen 中间输出：`C:\Users\黄昕玮\.codex\generated_images\01a003ff-91ce-7393-b52f-13987067d9d7\exec-3fefde80-1a83-4c28-8939-48ca4d9bffa8.png`。
- 最终项目路径：`D:\Git\Hearthstone\Hearthstone\Assets\Resources\Art\BattleCards\UI\CardFrame-v3.png`。
- 偏差与处理：两次生成结果均未正确保留透明 Alpha，第一次带棋盘格背景，第二次带白底。最终取第二次生成的中性灰阶 RGB，并合并原项目 Sprite 的 Alpha 通道，保持原轮廓与 GUID。验证尺寸为 `1024 × 1536`，中心与角落 Alpha 均为 `0`。

## 检查项状态与证据

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 备战悬停、战斗禁用 | 通过 | `SetHoverEnabled(preparationMode && occupied)` 同时控制 Listener 与 Image 射线；Prefab 默认关闭悬停、拖拽和 Interactor |
| 敌红我蓝、悬停黄 | 通过 | Controller 颜色表为 `#D23730`、`#3773EB`、`#FFD230` |
| 同一边框素材 | 通过 | Prefab 三个框均引用 `CardFrame-v3`；Controller 不再引用 `CardFrameBlue-v2` |
| 对象池状态清理 | 通过 | `ResetBinding()` 和关闭悬停时复位 `m_IsHovered` 并重新应用默认色 |
| View/Controller/Builder 边界 | 通过 | Image 与 Listener 均由 View 序列化引用；事件注册在 Controller Init；静态层级由一一对应 Builder 重建 |
| 框下沿与属性图标层级 | 通过 | Prefab 结构与测试确认框底 `offsetMin.y = 24`，属性徽章位于框之后绘制 |
| 文档同步 | 通过 | 已同步玩家视角设计、美术 UI/模块和战斗/备战程序 UI 文档 |
| `.meta` 约束 | 通过 | 本任务未创建、编辑或删除 `.meta`；历史蓝框资源保留但运行时无引用 |
| 并行工作保护 | 通过 | 未回退工作区中与本任务无关的资源、奖励逻辑、字体和临时文档改动 |

## 验证

- Unity 标准脚本校验：Controller、View、Builder 为 `0 warning / 0 error`；测试脚本为 `0 error`，仅有通用空检查建议。
- Unity 编译与 Console：最终刷新完成，Console `0 error`。
- 目标 Editor 测试：`3/3` 通过。
  - `BattleCardHoverUsesUnifiedFramePaletteAndPreparationOnlyInteraction`
  - `BattleCardPrefabRaisesFrameBottomAndRendersStatsAboveIt`
  - `PreparationPoolAndSlotsUseSharedBattleCardAndMatchItsAspectRatio`
- 相关规则回归：`RunCardRulesTests`、`BattleKeywordRulesTests`、`PreparationContinueTests` 合计 `33/33` 通过。
- 完整 `BattleRulesTests` 仍有一项既有失败：测试期望默认原画键 `Boar`，当前 CSV 为 `Boar_001`；与本次 UI 修改无关。
- 按项目默认验证边界未进入 Play Mode。

## 清理结果

任务结束前只运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 `0`。清理完成后创建本报告。

## 未解决风险

- 未进行 Play Mode 实际鼠标移动和拖拽手感验收；当前结论基于脚本校验、Prefab 结构检查和 Editor 测试。
- `CardFrameBlue-v2.png` 作为历史兼容资源仍保留，但当前 Prefab、Controller 与运行测试均不依赖它。
