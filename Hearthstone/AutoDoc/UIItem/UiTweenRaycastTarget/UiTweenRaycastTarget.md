# UiTweenRaycastTarget

## 1. 组件用途

`UiTweenRaycastTarget` 用于根据 Tween 曲线结果切换 `CanvasGroup.blocksRaycasts` 或 `Graphic.raycastTarget`。

适合用于动画期间禁用点击、淡入完成后开启射线检测、淡出时关闭射线检测的 UI。

## 2. 基本使用流程

1. 在 Tween 控制节点上挂载 `UiTweenRaycastTarget`。
2. 设置 `Duration`、`Curve` 和 `SearchType`。
3. 使用自动搜索或手动填写目标。
4. 在 Controller 中调用播放控制 API。

## 3. 配置项

### Duration

- 描述：Tween 播放时长。
- 默认行为：使用 Inspector 当前保存的浮点数。
- 配置值说明：填写秒数；大于 `0` 时按时间播放。

### Curve

- 描述：用于判断射线检测开关的曲线。
- 默认行为：使用 Inspector 当前保存的 `AnimationCurve`。
- 配置值说明：曲线采样值大于 `0.99` 时打开射线检测，小于 `0.01` 时关闭射线检测。

### AutoSearch

- 描述：控制是否自动搜索目标。
- 默认行为：默认值为 `true`。
- 配置值说明：
  - `true`：PreInit 时按 `SearchType` 自动搜索并覆盖 `TweenTargets`。
  - `false`：使用手动填写的 `TweenTargets`。

### TransformRootOverride

- 描述：自动搜索目标时使用的根节点。
- 默认行为：为空时使用当前组件所在 Transform。
- 配置值说明：填写一个 Transform。

### TweenTargets

- 描述：实际切换射线检测的目标组件列表。
- 默认行为：`AutoSearch` 为 `true` 时自动填充。
- 配置值说明：`SearchType` 为 `CanvasGroup` 时目标为 `CanvasGroup`；为 `Graphic` 时目标为 `Graphic`。

### SearchType

- 描述：选择切换哪类射线检测开关。
- 默认行为：使用 Inspector 当前保存的枚举值。
- 配置值说明：
  - `CanvasGroup`：切换单个 `CanvasGroup.blocksRaycasts`；找不到时可自动添加 `CanvasGroup`。
  - `Graphic`：切换子级多个 `Graphic.raycastTarget`。

## 4. 常用 API

- `Play()`
- `Pause()`
- `Continue()`
- `Stop()`
- `ApplyTime(float time)`

## 5. 使用示例

```csharp
m_View.RaycastTween.Play();
```
