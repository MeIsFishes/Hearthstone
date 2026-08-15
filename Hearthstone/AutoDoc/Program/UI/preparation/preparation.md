# 备战界面程序文档

## 1. 页面结构

`PreparationView.prefab` 是静态页面 Prefab，包含背景、标题、奖励提示、三个战斗槽、卡池面板和纵向滚动区域。卡池 `Content` 使用 `UiList.ConstantSlot/Horizontal`，按 7 列、14 行固定承载 `01~98`；Viewport 同时使用不显示遮罩图元的标准 `Mask` 与 `RectMask2D`，分别约束动态条目的 stencil 与矩形裁切，战斗槽列表位于滚动区域之外。Builder 从 `1400` 宽 Viewport 除以 7 推导 `200 × 300` 的 `2:3` 条目和 `1400 × 4200` Content；滚动轨道保持在 `46 × 500` Scrollbar 矩形内。

页面使用三个一一对应的 View/Controller/Prefab：

| Controller | Prefab | 职责 |
| --- | --- | --- |
| `PreparationController` | `Assets/Resources/Ui/PreparationView.prefab` | 创建 98 个卡池条目和 3 个槽位条目，监听 Run state Revision 并刷新表现 |
| `PreparationCardItemController` | `Assets/Resources/Ui/PreparationCardItem.prefab` | 固定绑定一个编号，显示空态或持有卡的原画、名称和永久攻血 |
| `PreparationSlotItemController` | `Assets/Resources/Ui/PreparationSlotItem.prefab` | 固定绑定一个槽位，显示空/占用状态并处理拖放目标高亮和提交 |

两个动态条目 Prefab 已通过公开 Pre-load 导出流程登记为 `Ui/PreparationCardItem` 与 `Ui/PreparationSlotItem`，由 `UiList` 的条目池创建和回收。

页面、卡池条目和槽位条目的全部 TMP 文本统一引用 `Assets/Resources/Fonts/NotoSansSC-Dynamic SDF.asset`。Builder 在生成 Prefab 前验证并补入当前页面标题、奖励反馈、分区标题和五种中文卡名所需字形；数字和中文均由 TMP 渲染，不使用图片文字或运行时替代字体。

## 2. 交互

持有卡池条目和占用槽位启用 `UiDragable` requester；空池位与空槽位禁用拖动。三个槽位的 `UiInteractor` 始终保留 responder 能力。释放到有效槽位时，槽位 Controller 将来源编号和目标槽索引提交给 `RunCardRules.TryPlaceCard()`；释放到其他区域不写状态。

拖动期间框架把 requester 提升到顶层。拖动结束后 `TurnBackWhenDragEnd` 与 `OnBackFromTop` 恢复原层级位置，两个 UiList 重新执行固定槽布局；Revision 变化后页面统一刷新卡池、槽位和高亮。框架 `UiInteractor` 在当前帧未命中 responder 或 EndDrag 时都会向旧目标发送一次 `InteractorTouchEnd` 并清空触摸目标，因此连续拖向同一槽仍会重新建立高亮。

## 3. 编辑场景与导出

`Assets/Scenes/Ui/Preparation.unity` 包含一个 Screen Space Overlay Canvas、CanvasScaler、GraphicRaycaster 和唯一 `UiSceneExporter`。Exporter 的 `FullUiGroupType` 为 `Hearthstone.EPreparationUiGroup`，`Main` 组下保存 connected `PreparationView.prefab` 实例。

导出结果是 `Assets/Resources/Ui/Preparation.asset`，运行时由 `PreparationUiScene` 和 PreparationStage 加载。三个 Builder 位于 `Assets/Scripts/Hearthstone/Ui/Editor/`，通过 `UiApi.EditorOperation.PreInitializeView/ExportPreloadedView` 的公开强类型 Editor API 可重复生成三个 Prefab，不依赖私有反射。Prefab 临时根使用 PreviewScene，`PreparationUiSceneBuilder` 使用 Additive 场景生成和导出 UI，两条链路都保留原活动场景及其未保存状态。
