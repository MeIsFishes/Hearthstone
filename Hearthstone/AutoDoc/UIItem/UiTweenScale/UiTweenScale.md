# UiTweenScale

## 1. 组件用途

`UiTweenScale` 用于对 UI 对象的 `localScale` 做 Tween。

适合用于按钮按下反馈、图标弹出、提示缩放等效果。

## 2. 基本使用流程

1. 在 Tween 控制节点上挂载 `UiTweenScale`。
2. 设置 `Duration`、`Curve`、`MinValue`、`MaxValue` 和 `ScaleType`。
3. 使用自动搜索查找 `UiTransformSetter` 或 `Transform`，或手动填写目标。
4. 在 Controller 中调用播放控制 API。

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

- 描述：缩放起始值或相对缩放起始倍率。
- 默认行为：使用 Inspector 当前保存的 `Vector3`。
- 配置值说明：`ScaleType` 为 `AbsoluteScale` 时表示起始 localScale；为 `RelativeScale` 时表示乘在当前 localScale 上的起始倍率。

### MaxValue

- 描述：缩放目标值或相对缩放目标倍率。
- 默认行为：使用 Inspector 当前保存的 `Vector3`。
- 配置值说明：`ScaleType` 为 `AbsoluteScale` 时表示目标 localScale；为 `RelativeScale` 时表示乘在当前 localScale 上的目标倍率。

### ScaleType

- 描述：控制缩放值按绝对值还是相对倍率解释。
- 默认行为：使用 Inspector 当前保存的枚举值。
- 配置值说明：
  - `RelativeScale`：把 Tween 值作为相对倍率乘到当前 localScale 上。
  - `AbsoluteScale`：直接把 Tween 值作为 localScale。

### AutoSearch

- 描述：控制是否自动搜索目标。
- 默认行为：默认值为 `true`。
- 配置值说明：
  - `true`：自动搜索 `UiTransformSetter`，没有时再使用 `Transform`。
  - `false`：使用手动填写的 `TweenTargets`。

### TransformRootOverride

- 描述：自动搜索目标时使用的根节点。
- 默认行为：为空时使用当前组件所在 Transform。
- 配置值说明：填写一个 Transform。

### TweenTargets

- 描述：实际应用缩放 Tween 的目标组件列表。
- 默认行为：`AutoSearch` 为 `true` 时自动填充。
- 配置值说明：目标通常为 `UiTransformSetter`；没有时可使用 `Transform`。

## 4. 常用 API

- `Play()`
- `Pause()`
- `Continue()`
- `Stop()`
- `ApplyTime(float time)`

## 5. 使用示例

```csharp
m_View.PopScaleTween.Play();
```
