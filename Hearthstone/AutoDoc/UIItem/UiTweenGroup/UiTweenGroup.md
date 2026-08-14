# UiTweenGroup

## 1. 组件用途

`UiTweenGroup` 用于统一控制一组 `UiTweenBase` 组件的播放、倒放、暂停、继续和停止。

适合用于一个 UI 动画由多个 Tween 共同组成的场景，例如面板同时位移、缩放、淡入。

## 2. 基本使用流程

1. 在动画控制节点上挂载 `UiTweenGroup`。
2. 把需要一起控制的 Tween 组件加入 `Tweens`。
3. 在 Controller 中通过 `Wrapper` 或组件本身调用播放 API。
4. 使用完成回调处理动画结束后的逻辑。

## 3. 配置项

### Tweens

- 描述：由当前组统一控制的 Tween 列表。
- 默认行为：初始为空；编辑器按钮可收集子级 Tween。
- 配置值说明：填写 `UiTweenBase` 派生组件；加入组后不要再单独操作列表中的单个 Tween。

### OnPlayingFinishes

- 描述：正向播放完成时触发的回调。
- 默认行为：未赋值时不执行任何回调。
- 配置值说明：传入 `UnityAction`。

### OnPlayReverseFinishes

- 描述：倒放完成时触发的回调。
- 默认行为：未赋值时不执行任何回调。
- 配置值说明：传入 `UnityAction`。

### Wrapper

- 描述：运行时操作 Tween 组的入口。
- 默认行为：通过 `Wrapper` 转发到当前组件。
- 配置值说明：通过 `m_View.TweenGroup.Wrapper` 访问播放控制和完成回调。

## 4. 常用 API

- `Wrapper.Play()`：正向播放。
- `Wrapper.PlayReverse()`：倒向播放。
- `Wrapper.Stop()`：停止并回到当前播放方向的起始状态。
- `Wrapper.Pause()`：暂停。
- `Wrapper.Continue()`：继续。
- `Wrapper.Finished`：判断当前组是否已经播放结束。
- `Wrapper.OnPlayingFinishes`：注册正向播放完成回调。
- `Wrapper.OnPlayReverseFinishes`：注册倒放完成回调。

## 5. 使用示例

```csharp
m_View.OpenTweenGroup.Wrapper.OnPlayingFinishes += OnOpenFinished;
m_View.OpenTweenGroup.Wrapper.Play();
```
