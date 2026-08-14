# UiTweenColor

## 1. 组件用途

`UiTweenColor` 用于对 `Graphic.color` 做颜色 Tween。

适合用于按钮状态变化、图标闪烁、提示文本变色等效果。

## 2. 基本使用流程

1. 在 Tween 控制节点上挂载 `UiTweenColor`。
2. 设置 `Duration`、`Curve`、`MinValue`、`MaxValue`。
3. 设置 `SearchTarget`。
4. 使用自动搜索或手动填写 `TweenTargets`。
5. 在 Controller 中调用播放控制 API。

## 3. 配置项

### Duration

- 描述：Tween 播放时长。
- 默认行为：使用 Inspector 当前保存的浮点数。
- 配置值说明：填写秒数；大于 `0` 时按时间播放。

### Curve

- 描述：Tween 采样曲线。
- 默认行为：使用 Inspector 当前保存的 `AnimationCurve`。
- 配置值说明：曲线值用于在 `MinValue` 与 `MaxValue` 之间插值。

### MinValue

- 描述：颜色起始值。
- 默认行为：使用 Inspector 当前保存的 `Color`。
- 配置值说明：填写播放起点颜色。

### MaxValue

- 描述：颜色目标值。
- 默认行为：使用 Inspector 当前保存的 `Color`。
- 配置值说明：填写播放终点颜色。

### AutoSearch

- 描述：控制是否自动搜索 `Graphic` 目标。
- 默认行为：默认值为 `true`。
- 配置值说明：
  - `true`：PreInit 时从 `TransformRootOverride` 下搜索并覆盖 `TweenTargets`。
  - `false`：使用手动填写的 `TweenTargets`。

### TransformRootOverride

- 描述：自动搜索目标时使用的根节点。
- 默认行为：为空时使用当前组件所在 Transform。
- 配置值说明：填写一个 Transform；搜索范围为该节点及其子级。

### TweenTargets

- 描述：实际应用颜色 Tween 的目标组件列表。
- 默认行为：`AutoSearch` 为 `true` 时自动填充。
- 配置值说明：目标应为 `Graphic`。

### SearchTarget

- 描述：控制搜索一个还是多个 `Graphic`。
- 默认行为：使用 Inspector 当前保存的枚举值。
- 配置值说明：
  - `Single`：只使用第一个找到的 `Graphic`。
  - `Multiple`：收集子级中所有 `Graphic`。

## 4. 常用 API

- `Play()`
- `Pause()`
- `Continue()`
- `Stop()`
- `ApplyTime(float time)`

## 5. 使用示例

```csharp
m_View.HighlightColorTween.Play();
```
