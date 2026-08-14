---
name: bbxcommon-localization
description: 配置语言 CSV，并通过 LocApi 与 UiLocText 使用本地化。
---

# BbxCommon Localization

## 配置

在 `LocLanguageList.csv` 登记语言：

```csv
Id,Name,CsvName
zh_CN,简体中文,Loc_zh_CN
en_US,English,Loc_en_US
```

每种语言再提供对应翻译表：

```csv
Key,Text
PLAYER_HP,生命值：{0}/{1}
```

第一条登记的语言会成为默认语言。资源 key 与 Mod 合并规则见 `bbxcommon-resource`。

## 基础调用

```csharp
string title = LocApi.GetLocText("UI_TITLE");
string hp = LocApi.GetLocText("PLAYER_HP", currentHp, maxHp);

LocApi.SetCurrentLanguage("en_US");
string current = LocApi.GetCurrentLanguage();
IReadOnlyList<LocLanguageInfo> languages = LocApi.GetLanguageList();
```

找不到译文时，`GetLocText` 返回传入的 key。

UI 文本优先挂 `UiLocText`：静态文本填写 `LocKey`；动态参数文本调用 `SetLocText(key, args)`。组件在 UI 打开时应用文本，**切换语言不会自动刷新已经打开的 UI**，需要重新打开或主动调用 `SetLocText`。

## 开发者文档

只有在修改语言加载、CSV 类型、切换流程、Mod 合并或 `UiLocText` 底层时，才读取 [本地化加载与扩展](developer-docs/localization-loading-and-extension.md)。
