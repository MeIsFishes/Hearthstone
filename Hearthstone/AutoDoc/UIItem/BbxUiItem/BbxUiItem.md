# BbxUiItem

## 1. 组件用途

`BbxUiItem` 是 BbxCommon 自定义 UI 组件的抽象基类，用于让通用 UI 组件具有一致的类型入口。它不提供独立表现或交互，也不能直接挂载；具体组件继承它并按需要实现 UI 生命周期接口。

## 2. 基本使用流程

1. 新建需要挂载到 UI GameObject 的 BbxCommon 通用组件。
2. 让组件继承 `BbxUiItem`。
3. 按组件职责实现 `IUiPreInit`、`IUiInit`、`IUiOpen`、`IUiShow`、`IUiUpdate`、`IUiHide`、`IUiClose` 或 `IUiDestroy` 中需要的生命周期接口。
4. 通过具体组件配置和使用功能，不创建 `BbxUiItem` 实例。

## 3. 配置项

`BbxUiItem` 当前没有 Inspector 配置项、公开字段或 Wrapper；所有可配置内容由具体派生组件提供。

## 4. 常用 API

`BbxUiItem` 当前没有供业务直接调用的公开 API。业务代码应使用具体派生组件提供的公开 API。

## 5. 使用示例

```csharp
public sealed class UiExampleItem : BbxUiItem, IUiInit
{
    void IUiInit.OnUiInit(UiControllerBase uiController)
    {
        // 在具体组件中接入需要的 UI 生命周期。
    }
}
```
