# UiItemIndex

## 1. 文档用途

本文档收录 BbxCommon 当前已有的 UI 组件，并按使用定位分为底层组件和业务组件。

底层组件一般不直接作为业务 UI 使用，而是辅助业务组件功能实现；业务组件可以直接挂载、配置和调用，用来完成明确的 UI 表现或交互需求。

## 2. 底层组件

| 组件 | 应用场景 |
|------|----------|
| `BbxUiItem` | BbxCommon 自定义 UI 组件的抽象基类，用于统一组件类型；不直接挂载。 |
| `UiEventListener` | 将 Unity 指针事件统一转成可注册的回调，通常被拖拽、点击、交互类组件复用。 |
| `UiTransformSetter` | 用优先级请求统一管理 UI 的位置、旋转和缩放，通常辅助拖拽、列表和 Tween 避免 Transform 设置冲突。 |

## 3. 业务组件

| 组件 | 应用场景 |
|------|----------|
| `UiList` | 运行时创建 UI Controller 列表项，并支持固定槽位、区域自适应或调用方手动定位。 |
| `UiOptional` | 管理一组按钮的单选、多选和选中回调。 |
| `UiDragable` | 让 UI 对象响应指针事件并支持拖拽。 |
| `UiInteractor` | 让可拖拽 UI 与其他 UI 对象产生触碰和交互回调。 |
| `UiLocText` | 根据本地化 key 自动设置 Text 或 TMP_Text 文本。 |
| `UiTweenAlpha` | 对 CanvasGroup 或 Graphic 的透明度做 Tween。 |
| `UiTweenColor` | 对 Graphic 的颜色做 Tween。 |
| `UiTweenGroup` | 统一播放、倒放、暂停或停止一组 Tween。 |
| `UiTweenInteractable` | 在 Tween 曲线两端切换 CanvasGroup.interactable。 |
| `UiTweenPos` | 对 UI 对象的 localPosition 做 Tween。 |
| `UiTweenRaycastTarget` | 在 Tween 曲线两端切换射线检测开关。 |
| `UiTweenScale` | 对 UI 对象的 localScale 做 Tween。 |
