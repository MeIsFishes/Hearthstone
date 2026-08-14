# UiTweenPos

## 1. 组件用途

`UiTweenPos` 用于对 UI 对象的 `localPosition` 做 Tween。

适合用于面板滑入滑出、图标移动、列表项简单位移动画等效果。

## 2. 基本使用流程

1. 在 Tween 控制节点上挂载 `UiTweenPos`。
2. 设置 `Duration`、`Curve`、`MinValue`、`MaxValue` 和 `PosType`。
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

- 描述：位置起始值或相对偏移起始值。
- 默认行为：使用 Inspector 当前保存的 `Vector3`。
- 配置值说明：`PosType` 为 `AbsoluteLocalPos` 时表示起始 localPosition；为 `RelativeLocalPos` 时表示相对当前位置的起始偏移。

### MaxValue

- 描述：位置目标值或相对偏移目标值。
- 默认行为：使用 Inspector 当前保存的 `Vector3`。
- 配置值说明：`PosType` 为 `AbsoluteLocalPos` 时表示目标 localPosition；为 `RelativeLocalPos` 时表示相对当前位置的目标偏移。

### PosType

- 描述：控制位置值按绝对 localPosition 还是相对偏移解释。
- 默认行为：默认值为 `AbsoluteLocalPos`。
- 配置值说明：
  - `RelativeLocalPos`：在当前 localPosition 基础上叠加 Tween 值。
  - `AbsoluteLocalPos`：直接把 Tween 值作为 localPosition。

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

- 描述：实际应用位移 Tween 的目标组件列表。
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
m_View.PanelMoveTween.Play();
```
