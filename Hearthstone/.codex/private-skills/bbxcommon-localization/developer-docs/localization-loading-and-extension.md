# 本地化加载与扩展

## 模块边界

- 对外 API：`Assets/Scripts/BbxCommon/CrossLibrary/Api/LocApi.cs`
- CSV 类型：`Assets/Scripts/BbxCommon/CrossLibrary/LocCsvData.cs`
- UI 组件：`Assets/Scripts/BbxCommon/Ui/Misc/UiLocText.cs`
- 引擎接入：`GameEngineStage.LoadLocTranslations`

## 启动流程

1. GameEngine 默认 DataGroup 加载 `LocLanguageList`。
2. `LocLanguageCsvData.ReadLine()` 通过 `LocApi.RegisterLanguage` 登记 `Id`、`Name`、`CsvName`。
3. 第一条登记记录把当前语言 id 设为默认值，但尚未加载翻译表。
4. `LoadLocTranslations.Load()` 把 `ResourceApi.LoadLocKeyTable` 赋给 `LocApi.LoadCsvByNameFunction`。
5. 对当前语言调用 `SetCurrentLanguage`，按其 `CsvName` 动态加载 `LocKeyCsvData`。
6. Game Engine Stage 卸载时清空当前翻译表和加载委托。

`LocKeyCsvData.GetTableNames()` 返回空数组，因此不会随普通 DataGroup 自动加载。

## CSV 与合并

语言列表固定列为 `Id,Name,CsvName`；翻译表固定列为 `Key,Text`。两个 CSV 类型都使用 `EDataLoad.Addition`，允许资源系统读取多个同名文件。

翻译行最终执行 `dictionary[key] = text`。当前资源列表按优先级从高到低读取，而后读到的重复 key 会覆盖先前值；因此修改 Mod 优先级或翻译覆盖策略时，必须用重复 key 实测最终结果，不能只依据“高优先级在列表前”推断译文结果。

当前语言列表允许重复 id，`ResolveCsvName` 使用第一个匹配项；设计 Mod 语言表时应保证 id 唯一。

## LocApi 行为

- `GetLocText(key)`：当前语言或 key 不存在时返回 key。
- `GetLocText(key, args)`：对查询结果执行 `string.Format`；占位符与参数不匹配会抛出格式异常。
- `SetCurrentLanguage(id)`：卸载上一语言字典，只保留当前语言。
- 传入未登记 id 时，当前 id 仍会更新，但不会加载翻译表；业务 UI 只应使用 `GetLanguageList()` 中的 id。
- `SetCurrentLanguage(null)`：清空所有已加载翻译并取消当前语言。
- `GetLanguageList()`：返回只读接口，不要修改其底层内容。

## UiLocText

`IUiPreInit.OnUiPreInit` 在 Editor 中缓存同一 GameObject 上的 `TMP_Text`，找不到时再缓存 Legacy `Text`。`IUiOpen.OnUiOpen` 应用 Inspector 中的 `LocKey`。

`SetLocText` 只更新当前组件，不保存格式参数用于下一次语言切换。若需要设置界面即时换语言，应统一刷新当前页面或重新为动态文本传入参数。

修改 `UiLocText` 时同步遵循 `bbxcommon-ui-item` 并更新 `AutoDoc/UIItem/UiLocText/UiLocText.md`。

## 修改检查

- 验证默认语言、合法切换、非法 id、缺失 key 与格式化参数。
- 验证同名语言列表和翻译表在 Native/Mod 下的实际合并顺序。
- 验证打开中的 UI、重新打开的 UI 和动态参数文本。
- 验证 Game Engine Stage 卸载后翻译表与委托已清理。
