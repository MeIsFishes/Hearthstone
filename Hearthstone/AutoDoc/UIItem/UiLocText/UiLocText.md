# UiLocText

## 1. 组件用途

`UiLocText` 用于根据本地化 key 设置同一 GameObject 上的 `TMP_Text` 或 `Text` 文本。

适合用于静态 UI 文案、可变参数本地化文本和需要打开界面时自动刷新语言文本的控件。

## 2. 基本使用流程

1. 在带有 `TMP_Text` 或 `Text` 的 GameObject 上挂载 `UiLocText`。
2. 在 `LocKey` 中填写本地化 key。
3. UI 打开时组件会自动应用 `LocKey`。
4. 运行时需要切换文本时调用 `SetLocText`。

## 3. 配置项

### LocKey

- 描述：用于查询本地化文本的 key。
- 默认行为：为空时打开 UI 不修改文本。
- 配置值说明：填写可被 `LocApi.GetLocText` 解析的字符串 key。

### TmpText

- 描述：组件缓存的 TMP 文本对象。
- 默认行为：编辑器 PreInit 时自动获取同一 GameObject 上的 `TMP_Text`。
- 配置值说明：通常不手动填写；存在 TMP 文本时优先使用该字段。

### LegacyText

- 描述：组件缓存的 Unity Legacy Text 对象。
- 默认行为：同一 GameObject 上没有 `TMP_Text` 时，编辑器 PreInit 自动获取 `Text`。
- 配置值说明：通常不手动填写；仅在使用 Unity Legacy Text 时生效。

## 4. 常用 API

- `SetLocText(string key, params object[] args)`：立即按 key 获取本地化文本并写入文本组件；`args` 会作为格式化参数传入。

## 5. 使用示例

```csharp
m_View.TitleText.SetLocText("ui_title_stage", stageIndex);
```
