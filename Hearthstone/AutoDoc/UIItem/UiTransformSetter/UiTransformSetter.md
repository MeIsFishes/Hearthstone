# UiTransformSetter

## 1. 组件用途

`UiTransformSetter` 用于用优先级请求统一管理 UI 对象的位置、旋转和缩放，避免多个组件同时修改 Transform 时互相覆盖。

适合用于 `UiList`、`UiDragable`、`UiTweenPos`、`UiTweenScale` 等多个组件可能同时控制同一 UI Transform 的场景。

## 2. 基本使用流程

1. 在需要统一控制 Transform 的 UI GameObject 上挂载 `UiTransformSetter`。
2. 通过 `PosWrapper`、`RotWrapper` 或 `ScaleWrapper` 添加持续请求或一次性请求。
3. 使用更高 priority 覆盖低优先级请求。
4. 持续请求不再需要时调用对应 Remove 方法移除。

## 3. 配置项

### DontRemove

- 描述：控制 PreInit 时是否保留当前 `UiTransformSetter`。
- 默认行为：未勾选时，冗余的 `UiTransformSetter` 可能在 `UiViewBase.PreUiInit` 阶段被移除。
- 配置值说明：
  - `true`：保留当前组件。
  - `false`：允许框架移除被判定为冗余的组件。

### PosWrapper

- 描述：位置请求入口。
- 默认行为：Open 时初始化请求列表，Close 时释放请求列表。
- 配置值说明：通过代码访问，用 priority 区分不同位置请求。

### RotWrapper

- 描述：旋转请求入口。
- 默认行为：Open 时初始化请求列表，Close 时释放请求列表。
- 配置值说明：通过代码访问，用 priority 区分不同旋转请求。

### ScaleWrapper

- 描述：缩放请求入口。
- 默认行为：Open 时初始化请求列表，Close 时释放请求列表。
- 配置值说明：通过代码访问，用 priority 区分不同缩放请求。

## 4. 常用 API

- `PosWrapper.AddPositionRequest(position, priority)`：添加持续世界坐标位置请求。
- `PosWrapper.AddLocalPositionRequest(position, priority)`：添加持续本地坐标位置请求。
- `PosWrapper.RemovePositionRequest(priority)`：移除持续位置请求。
- `PosWrapper.SetPositionOnce(position, priority)`：添加一帧世界坐标位置请求。
- `PosWrapper.SetLocalPositionOnce(position, priority)`：添加一帧本地坐标位置请求。
- `RotWrapper.AddRotationRequest(rotation, priority)`：添加持续旋转请求。
- `RotWrapper.RemoveRotationRequest(priority)`：移除持续旋转请求。
- `RotWrapper.SetRotationOnce(rotation, priority)`：添加一帧旋转请求。
- `ScaleWrapper.AddScaleRequest(scale, priority)`：添加持续缩放请求。
- `ScaleWrapper.RemoveScaleRequest(priority)`：移除持续缩放请求。
- `ScaleWrapper.SetScaleOnce(scale, priority)`：添加一帧缩放请求。

## 5. 使用示例

```csharp
m_View.CardTransformSetter.PosWrapper.AddLocalPositionRequest(targetPos, 100);
m_View.CardTransformSetter.PosWrapper.RemovePositionRequest(100);
```
