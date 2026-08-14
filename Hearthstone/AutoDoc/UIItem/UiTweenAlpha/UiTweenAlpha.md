# UiTweenAlpha

## 1. 组件用途

`UiTweenAlpha` 用于对 `CanvasGroup.alpha` 或 `Graphic.color.a` 做 Tween。

适合用于界面淡入淡出、按钮高亮透明度变化、提示框显示隐藏等效果。

## 2. 基本使用流程

1. 在 Tween 控制节点上挂载 `UiTweenAlpha`。
2. 设置 `Duration`、`Curve`、`MinValue`、`MaxValue`。
3. 设置 `SearchType` 和目标搜索方式。
4. 使用自动搜索或手动填写 `TweenTargets`。
5. 在 Controller 中调用 `Play`、`Pause`、`Continue` 或 `Stop`。

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

- 描述：透明度最小值。
- 默认行为：使用 Inspector 当前保存的浮点数。
- 配置值说明：通常填写 `0` 到 `1`；`0` 为完全透明，`1` 为完全不透明。

### MaxValue

- 描述：透明度最大值。
- 默认行为：使用 Inspector 当前保存的浮点数。
- 配置值说明：通常填写 `0` 到 `1`；播放时按曲线在 `MinValue` 和 `MaxValue` 之间变化。

### AutoSearch

- 描述：控制是否自动搜索 Tween 目标。
- 默认行为：默认值为 `true`。
- 配置值说明：
  - `true`：PreInit 时从 `TransformRootOverride` 下搜索目标并覆盖 `TweenTargets`。
  - `false`：使用手动填写的 `TweenTargets`。

### TransformRootOverride

- 描述：自动搜索目标时使用的根节点。
- 默认行为：为空时使用当前组件所在 Transform。
- 配置值说明：填写一个 Transform；搜索范围为该节点及其子级。

### TweenTargets

- 描述：实际应用透明度 Tween 的目标组件列表。
- 默认行为：`AutoSearch` 为 `true` 时自动填充。
- 配置值说明：目标应为 `CanvasGroup` 或 `Graphic`。

### SearchType

- 描述：选择透明度作用于哪类组件。
- 默认行为：使用 Inspector 当前保存的枚举值。
- 配置值说明：
  - `CanvasGroup`：修改单个 `CanvasGroup.alpha`；找不到时可自动添加 `CanvasGroup`。
  - `Graphic`：修改 `Graphic.color.a`。

### SearchTarget

- 描述：`SearchType` 为 `Graphic` 时控制搜索一个还是多个目标。
- 默认行为：使用 Inspector 当前保存的枚举值。
- 配置值说明：
  - `Single`：只使用第一个找到的 `Graphic`。
  - `Multiple`：收集子级中所有 `Graphic`。

## 4. 常用 API

- `Play()`：从起点播放。
- `Pause()`：暂停。
- `Continue()`：继续播放。
- `Stop()`：停止并回到起点。
- `ApplyTime(float time)`：直接应用某个时间点的 Tween 值。

## 5. 使用示例

```csharp
m_View.FadeTween.Play();
```
