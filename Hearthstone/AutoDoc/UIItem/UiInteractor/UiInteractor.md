# UiInteractor

## 1. 组件用途

`UiInteractor` 用于让 UI 对象在拖拽过程中与其他 `UiInteractor` 对象产生触碰、触碰结束和交互回调。

适合用于卡牌拖到目标区域、道具拖到槽位、拖拽对象与容器交互等 UI。

## 2. 基本使用流程

1. 在可交互 UI 对象上挂载 `UiInteractor`。
2. 设置 `TransformOverride`，决定本组件自身射线目标范围。
3. 如交互由拖拽触发，启用 `AutoInitUiDragable` 并指定或自动查找 `UiDragableRef`。
4. 在 Controller 中通过 `Wrapper` 设置交互回调和 `ExtraInfo`。

## 3. 配置项

### TransformOverride

- 描述：搜索当前交互对象自身 Graphic 射线目标的根节点。
- 默认行为：为空时使用当前组件所在 Transform。
- 配置值说明：填写一个 Transform；其子级 Graphic 会被视作自身范围，拖拽结束检测时会忽略这些自身对象。

### AutoInitUiDragable

- 描述：控制是否自动接入 `UiDragable` 的拖拽回调。
- 默认行为：默认值为 `true`。
- 配置值说明：
  - `true`：初始化时从 `TransformOverride` 子级查找 `UiDragableRef`，并监听拖拽与结束拖拽。
  - `false`：不自动接入拖拽，需要外部自行调用交互相关逻辑。

### UiDragableRef

- 描述：用于驱动交互检测的 `UiDragable`。
- 默认行为：`AutoInitUiDragable` 为 `true` 且为空时，从 `TransformOverride` 子级查找。
- 配置值说明：填写同一交互对象或子级上的 `UiDragable`。

### OnInteractorTouch

- 描述：拖拽其他交互对象触碰到当前对象时触发。
- 默认行为：未赋值时不执行回调。
- 配置值说明：传入 `UnityAction<Interactor>`，参数为触碰当前对象的交互对象。

### OnInteractorTouchEnd

- 描述：拖拽对象离开当前触碰对象时触发。
- 默认行为：未赋值时不执行回调。
- 配置值说明：传入 `UnityAction<Interactor>`，参数为离开当前对象的交互对象。

### Wrapper

- 描述：运行时访问交互信息和注册回调的入口。
- 默认行为：PreInit 时自动绑定当前组件。
- 配置值说明：通过 `m_View.Interactor.Wrapper` 访问。

## 4. 常用 API

- `Wrapper.ExtraInfo`：存储当前交互对象的业务附加信息。
- `Wrapper.OnInteractorTouch`：被拖拽对象触碰时触发。
- `Wrapper.OnInteractorTouchEnd`：触碰结束时触发。
- `Wrapper.OnInteract`：交互成立时触发，参数为 requester 和 responder。
- `Wrapper.OnInteractWith`：交互成立时触发，参数为另一个交互对象。
- `SetInteractFlags(...)`：设置交互标记，用于与 Interactor 系统配合。

## 5. 使用示例

```csharp
m_View.CardInteractor.Wrapper.ExtraInfo = cardData;
m_View.CardInteractor.Wrapper.OnInteractWith += target =>
{
    TryUseCardOn(target.ExtraInfo);
};
```
