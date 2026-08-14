# UiTweenInteractable

## 1. 组件用途

`UiTweenInteractable` 用于根据 Tween 曲线结果切换 `CanvasGroup.interactable`。

适合用于淡入动画完成后启用交互、淡出开始或结束时禁用交互的 UI。

## 2. 基本使用流程

1. 在 Tween 控制节点上挂载 `UiTweenInteractable`。
2. 设置 `Duration` 和 `Curve`。
3. 使用自动搜索或手动填写 `CanvasGroup` 目标。
4. 在 Controller 中调用播放控制 API。

## 3. 配置项

### Duration

- 描述：Tween 播放时长。
- 默认行为：使用 Inspector 当前保存的浮点数。
- 配置值说明：填写秒数；大于 `0` 时按时间播放。

### Curve

- 描述：用于判断 interactable 开关的曲线。
- 默认行为：使用 Inspector 当前保存的 `AnimationCurve`。
- 配置值说明：曲线采样值大于 `0.99` 时设置 `interactable = true`，小于 `0.01` 时设置 `interactable = false`。

### AutoSearch

- 描述：控制是否自动搜索 `CanvasGroup`。
- 默认行为：默认值为 `true`。
- 配置值说明：
  - `true`：PreInit 时从 `TransformRootOverride` 下搜索单个 `CanvasGroup`；找不到时自动添加。
  - `false`：使用手动填写的 `TweenTargets`。

### TransformRootOverride

- 描述：自动搜索目标时使用的根节点。
- 默认行为：为空时使用当前组件所在 Transform。
- 配置值说明：填写一个 Transform。

### TweenTargets

- 描述：实际切换 interactable 的目标组件列表。
- 默认行为：`AutoSearch` 为 `true` 时自动填充。
- 配置值说明：目标应为 `CanvasGroup`。

### Description

- 描述：Inspector 中展示用途说明的只读字段。
- 默认行为：不参与运行时逻辑。
- 配置值说明：无需配置。

## 4. 常用 API

- `Play()`
- `Pause()`
- `Continue()`
- `Stop()`
- `ApplyTime(float time)`

## 5. 使用示例

```csharp
m_View.InteractableTween.Play();
```
