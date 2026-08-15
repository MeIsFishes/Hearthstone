# 融合卡片揭晓动画检查清单

## 用户要求

- [x] **通过**：融合成功时显示一块覆盖画面的灰色遮罩。证据：`PreparationViewUiBuilder.CreateFusionReveal()` 创建全屏深灰遮罩，`CanvasGroup.blocksRaycasts = true`；Prefab 字段与层级测试通过。
- [x] **通过**：融合结果卡片出现在屏幕正中央并呈现悬空展示效果。证据：结果卡根节点锚定屏幕中心，尺寸为 `250 × 360`；动画由 `0.72` 倍、Y=`-35` 进入到 `1.22` 倍并叠加轻微正弦浮动。
- [x] **通过**：展示卡片从正面旋转到背面、再旋转回正面，完成一整圈旋转。证据：`PreparationController.UpdateFusionReveal()` 逐帧驱动 Y 轴 `0° → 360°`，`90°~270°` 切换为预置的 `180°` 卡背。
- [x] **通过**：卡片回到正面后，卡面出现一道闪光并揭晓融合结果。证据：真实结果卡仅在旋转进度达到 `270°` 后显示，旋转结束后闪光在 `0.55 s` 内从 X=`-260` 扫到 `260`。
- [x] **通过**：动画应为清晰、连贯的逐帧时序，并只在融合成功时触发。证据：仅 `RunCardRules.TryFuse()` 返回 `Applied` 后调用 `StartFusionReveal()`；`OnUiUpdate(deltaTime)` 驱动约 `3.39 s` 的连续时间轴。

## 实现与验证

- [x] **通过**：定位并复用现有融合成功触发链、卡牌视图、动画/协程或 Task 能力及资源加载方式。证据：复用现有 `TryFuse()` 结果、`BattleCardItemController`、预加载 `UiList` 与共享 `BattleCardItem.prefab`，没有另建结果卡体系。
- [x] **通过**：动画播放期间正确管理输入、层级、对象生命周期与重复触发，播放完成后完整恢复现场。证据：遮罩为页面最上层并阻挡射线，活动状态阻止重复融合；隐藏、关闭和时间轴结束均调用 `ResetFusionReveal()`；结果条目由 `UiList` 生命周期创建与释放。
- [x] **通过**：正反面切换时机与旋转角度一致，不提前泄露融合结果；闪光仅在正面揭晓阶段出现。证据：封印面 `<90°`、卡背 `90°~270°`、结果面 `>=270°`，闪光从完整旋转结束后才启用。
- [x] **通过**：检查新增字段和函数确有复用价值，删除不必要的一次性抽象和重复分配。证据：View 字段对应持久 Prefab 节点，动画使用常量与既有组件逐帧更新；结果卡只创建一个池化条目，没有每帧集合或对象分配。
- [x] **通过**：对直接相关代码执行可用的静态检查或编译验证；除非用户要求，不进入游戏验证。证据：Unity 强制刷新及脚本编译完成；完整 EditMode 测试 `65/65` 通过，耗时 `2.621 s`；按项目默认未进入 Play Mode。
- [x] **通过**：检查未误改无关文件、未遗漏直接依赖，且不创建、编辑或删除任何 `.meta` 文件。证据：最终审计覆盖 View、Controller、Builder、Prefab、测试和三类文档；本任务没有生成或修改 `.meta`。工作树中其他并行/既有改动均保留未回退。

## 框架边界审计

- [x] **通过**：实现未绕过项目已有 UI、资源、生命周期、动画/Task、对象池或编辑器配置入口，未在业务层平行实现框架已有能力。证据：遵循 `bbxcommon-ui`，静态层级由既有 Editor Builder 写入 Prefab，结果卡由 `UiList`/预加载映射管理，页面状态由 `OnUiInit/Update/Open/Hide/Close` 管理。
- [x] **不适用**：未发现需要修改底层框架的能力缺口。现有页面逐帧生命周期、静态 UI 节点和 `UiList` 已覆盖需求。

## 文档同步

- [x] **通过**：完整读取 `design-doc-format`，并更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`，记录玩家看到的遮罩、旋转、揭晓和恢复操作流程。
- [x] **通过**：完整读取 `art-doc-writer` 及模块格式参考，并更新 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md`，记录遮罩透明度、封印面、蓝金卡背、共享卡面和裁切闪光规范。
- [x] **通过**：完整读取 `program-doc-format` 及 UI 页面格式参考，并更新 `AutoDoc/Program/UI/preparation/preparation.md`，记录层级、复用关系、时间轴、输入阻挡和生命周期。

## 结束审计

- [x] **通过**：已重新读取本清单，逐项核对代码、Prefab、测试与文档证据。
- [x] **通过**：仅运行一次 `AutoDoc/CleanupTempDocs.bat`；退出码 `0`，清理前后均为 `139` 篇 Markdown，未触发删除。
- [x] **通过**：清理后创建同任务名的 `融合卡片揭晓动画-Report.md`，记录结果、证据、验证、偏差、风险、文档处理与清理结果。
