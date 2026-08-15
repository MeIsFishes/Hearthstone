# 备战界面程序文档

## 1. 页面结构

`PreparationView.prefab` 是静态页面 Prefab，包含背景、标题、左上奖励提示、右上 Continue Button、“出战/融合”页签、互斥的战斗操作区与融合操作区、共享卡池和纵向滚动区域。Continue 与两个操作区、卡池互为页面根兄弟，使用标准 Button SpriteSwap 四态；Waiting 时同 Rect 的透明 `ContinueWaitingInputBlocker` 捕获重复点击而不再次调用生产入口。卡池 `Content` 使用 `UiList.ConstantSlot/Horizontal`，按 7 列、15 行固定承载 `01~99`，最后一行保留 99 的固定位置；Viewport 同时使用标准 `Mask` 与 `RectMask2D`。Builder 从 `1400` 宽 Viewport 推导 `200 × 300` 的 `2:3` 条目和 `1400 × 4500` Content。

页面使用三个一一对应的 View/Controller/Prefab：

| Controller | Prefab | 职责 |
| --- | --- | --- |
| `PreparationController` | `Assets/Resources/Ui/PreparationView.prefab` | 创建 99 个卡池条目、3 个出战槽和 4 个融合槽，监听 Run/Fusion Revision 并刷新页签、合计与按钮 |
| `PreparationCardItemController` | `Assets/Resources/Ui/PreparationCardItem.prefab` | 固定绑定一个编号，显示空态、完整持有卡及融合素材选中标记 |
| `PreparationSlotItemController` | `Assets/Resources/Ui/PreparationSlotItem.prefab` | 固定绑定一个槽位，显示空/占用状态并处理拖放目标高亮和提交 |
| `PreparationFusionSlotItemController` | `Assets/Resources/Ui/PreparationFusionSlotItem.prefab` | 固定绑定一个融合槽，处理池→槽、槽→槽、替换和悬停反馈 |

三个动态条目 Prefab 已通过公开 Pre-load 导出流程登记为 `Ui/PreparationCardItem`、`Ui/PreparationSlotItem` 与 `Ui/PreparationFusionSlotItem`，由 `UiList` 的条目池创建和回收。

页面、卡池条目和槽位条目的全部 TMP 文本统一引用 `Assets/Resources/Fonts/NotoSansSC-Dynamic SDF.asset`。Builder 在生成 Prefab 前验证并补入当前页面标题、奖励反馈、分区标题和五种中文卡名所需字形；数字和中文均由 TMP 渲染，不使用图片文字或运行时替代字体。

## 2. 交互

持有卡池条目和占用槽位启用 `UiDragable` requester；空池位与空槽位禁用拖动。出战页提交 `RunCardRules.TryPlaceCard()`。融合页把普通持有卡拖入 4 个融合槽，支持槽间移动和替换；把融合槽卡拖回共享池会取消选择。空池位、99 作为素材、重复素材、无效槽和禁用融合点击只记录结构化拒绝，不写状态。

融合面板同时显示素材表达式、当前和与目标 `99`。标准 Unity `Button` 仅在 2～4 张互异素材、编号和为 99 且结果卡尚未持有时可用；成功后素材卡与其出战槽同步清空，99 池位显示合计后的永久攻血。

拖动期间框架把 requester 提升到顶层。拖动结束后 `TurnBackWhenDragEnd` 与 `OnBackFromTop` 恢复原层级位置，两个 UiList 重新执行固定槽布局；Revision 变化后页面统一刷新卡池、槽位和高亮。框架 `UiInteractor` 在当前帧未命中 responder 或 EndDrag 时都会向旧目标发送一次 `InteractorTouchEnd` 并清空触摸目标，因此连续拖向同一槽仍会重新建立高亮。

## 3. 编辑场景与导出

`Assets/Scenes/Ui/Preparation.unity` 包含一个 Screen Space Overlay Canvas、CanvasScaler、GraphicRaycaster 和唯一 `UiSceneExporter`。Exporter 的 `FullUiGroupType` 为 `Hearthstone.EPreparationUiGroup`，`Main` 组下保存 connected `PreparationView.prefab` 实例。

导出结果是 `Assets/Resources/Ui/Preparation.asset`，运行时由 `PreparationUiScene` 和 PreparationStage 加载。四个 Prefab Builder 位于 `Assets/Scripts/Hearthstone/Ui/Editor/`，通过 `UiApi.EditorOperation.PreInitializeView/ExportPreloadedView` 的公开强类型 Editor API 可重复生成页面和三个条目 Prefab。Prefab 临时根使用 PreviewScene，`PreparationUiSceneBuilder` 使用 Additive 场景生成和导出 UI，两条链路都保留原活动场景及其未保存状态。
