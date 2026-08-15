# UiDragable

## 1. 组件用途

`UiDragable` 用于让 UI 对象响应指针进入、停留、离开、开始拖拽、拖拽中和结束拖拽事件，并在拖拽中移动 UI 对象。组件会把指针屏幕坐标换算到当前 UI 平面，因此可用于启用了 `CanvasScaler` 的界面。

适合用于卡牌拖拽、道具拖放、可拖动面板和需要拖拽回调的 UI。

## 2. 基本使用流程

1. 在需要拖拽的 UI GameObject 上挂载 `UiDragable`。
2. 配置拖拽结束是否回原位、拖拽偏移和触发拖拽开始/结束的事件。
3. 指定 `EventListener`；为空时组件会自动添加。
4. 在 Controller 中通过 `Wrapper` 注册拖拽相关回调。

## 3. 配置项

### TurnBackWhenDragEnd

- 描述：控制拖拽结束后是否在归还原父节点后返回拖拽开始时的局部位置。
- 默认行为：`false` 时结束拖拽后停留在当前位置或其他组件设置的位置。
- 配置值说明：
  - `true`：拖拽结束后归还原父节点，并向 `UiTransformSetter` 发送一次恢复原始局部位置的请求。
  - `false`：拖拽结束时只移除拖拽位置请求。

### AlwaysRelativeOffset

- 描述：控制拖拽时是否使用固定指针偏移。
- 默认行为：默认值为 `true`。
- 配置值说明：
  - `true`：拖拽位置等于指针位置减去 `RelativeOffset`。
  - `false`：保持按下瞬间对象与指针之间的原始偏移。

### RelativeOffset

- 描述：`AlwaysRelativeOffset` 为 `true` 时使用的指针偏移。
- 默认行为：使用 Inspector 当前保存的 `Vector2`。
- 配置值说明：`x` 和 `y` 表示对象局部坐标系中相对指针位置的偏移；运行时会随对象及其父级的缩放和旋转换算到 UI 世界坐标。零值表示对象中心跟随指针。

### SetWhenDown

- 描述：控制按下并开始拖拽时是否立即应用 `RelativeOffset`。
- 默认行为：默认值为 `true`。
- 配置值说明：
  - `true`：开始拖拽时立刻移动到相对指针的位置。
  - `false`：等拖拽事件持续触发时再移动。

### EventListener

- 描述：接收指针事件的 `UiEventListener`。
- 默认行为：为空时 PreInit 自动在同一 GameObject 上添加一个 `UiEventListener`。
- 配置值说明：填写参与射线检测的对象上的 `UiEventListener`；拖拽移动作用于该对象上的 `UiTransformSetter`，并使用该对象所属 Canvas 完成屏幕坐标换算。

### EventBeginDrag

- 描述：触发开始拖拽的事件。
- 默认行为：默认值为 `PointerDown`。
- 配置值说明：可选 `EUiEvent` 中任一值；常用 `PointerDown` 或 `Drag`。

### EventEndDrag

- 描述：触发结束拖拽的事件。
- 默认行为：默认值为 `PointerUp`。
- 配置值说明：可选 `EUiEvent` 中任一值；常用 `PointerUp`。

### Wrapper

- 描述：运行时注册拖拽事件回调的入口。
- 默认行为：PreInit 时自动绑定当前组件。
- 配置值说明：通过 `m_View.Dragable.Wrapper` 访问各类回调。

## 4. 常用 API

`Wrapper` 提供以下回调：

- `OnPointerEnter`
- `OnPointerStay`
- `OnPointerExit`
- `OnBeginDrag`
- `OnDrag`
- `OnEndDrag`
- `OnBackFromTop`

## 5. 使用示例

```csharp
m_View.CardDragable.Wrapper.OnEndDrag += eventData =>
{
    TryDropCard(eventData.position);
};
```
