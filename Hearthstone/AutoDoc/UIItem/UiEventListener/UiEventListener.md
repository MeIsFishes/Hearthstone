# UiEventListener

## 1. 组件用途

`UiEventListener` 用于把 Unity 指针事件统一封装成可注册和移除的回调。

适合用于按钮以外的 UI 指针交互，例如悬停、拖拽、点击区域、指针移动监听。

## 2. 基本使用流程

1. 在需要接收射线事件的 UI GameObject 上挂载 `UiEventListener`。
2. 确保该 GameObject 或其图形组件能参与 UI 射线检测。
3. 在 Controller 或其他 UI 组件中调用 `AddCallback` 注册事件。
4. 不再需要监听时调用 `RemoveCallback` 移除事件。

## 3. 配置项

### OnPointerDown

- 描述：指针按下时触发的回调。
- 默认行为：未赋值时不执行任何回调。
- 配置值说明：传入 `UnityAction<PointerEventData>`。

### OnPointerUp

- 描述：指针抬起时触发的回调。
- 默认行为：未赋值时不执行任何回调。
- 配置值说明：传入 `UnityAction<PointerEventData>`。

### OnPointerEnter

- 描述：指针进入对象时触发的回调。
- 默认行为：未赋值时不执行任何回调。
- 配置值说明：传入 `UnityAction<PointerEventData>`。

### OnPointerExit

- 描述：指针离开对象时触发的回调。
- 默认行为：未赋值时不执行任何回调。
- 配置值说明：传入 `UnityAction<PointerEventData>`。

### OnPointerClick

- 描述：指针点击时触发的回调。
- 默认行为：未赋值时不执行任何回调。
- 配置值说明：传入 `UnityAction<PointerEventData>`。

### OnPointerMove

- 描述：指针在对象上移动时触发的回调。
- 默认行为：未赋值时不执行任何回调。
- 配置值说明：传入 `UnityAction<PointerEventData>`。

### OnDrag

- 描述：拖拽时触发的回调。
- 默认行为：未赋值时不执行任何回调。
- 配置值说明：传入 `UnityAction<PointerEventData>`。

## 4. 常用 API

- `AddCallback(EUiEvent uiEvent, UnityAction<PointerEventData> callback)`：注册指定事件回调。
- `RemoveCallback(EUiEvent uiEvent, UnityAction<PointerEventData> callback)`：移除指定事件回调。

`EUiEvent` 可用值：`PointerDown`、`PointerUp`、`PointerEnter`、`PointerExit`、`PointerClick`、`PointerMove`、`Drag`。

## 5. 使用示例

```csharp
m_View.EventListener.AddCallback(EUiEvent.PointerClick, OnClick);
m_View.EventListener.RemoveCallback(EUiEvent.PointerClick, OnClick);
```
