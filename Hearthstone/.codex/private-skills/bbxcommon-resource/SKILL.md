---
name: bbxcommon-resource
description: 使用 ResourceApi 读取资源；修改资源或 Mod 底层时查阅。
---

# BbxCommon Resource

业务代码通过 `ResourceApi` 读取资源，不直接调用 `ResourceManager`。

资源 key 使用**不含路径与扩展名的文件名**。框架会统一索引 `Assets/Resources/` 与 `Mods/` 下的文件；同名文件可能来自多个来源。

## 基础调用

```csharp
TextAsset text = ResourceApi.LoadTextAsset("Readme");
List<TextAsset> texts = ResourceApi.LoadTextAssets("Readme");
Sprite icon = ResourceApi.LoadSprite("SkillFireball");
GameObject prefab = ResourceApi.LoadGameObject("ProjectileFireball");
```

- `LoadTextAsset` / `LoadSprite`：读取当前优先级最高的同名资源；未找到时返回 `null`。
- `LoadGameObject`：按 key 从 Resources 索引中读取并缓存 GameObject Prefab；同名资源包含不同 Unity 类型时会跳过非 GameObject 项，未找到可加载 Prefab 时返回 `null`。
- `LoadTextAssets`：按资源优先级读取全部同名文本；未找到时返回空列表。
- `GetFile` / `GetAllFile`：只需要来源和路径信息、不需要直接加载内容时使用。

避免依赖目录或扩展名区分同名资源；确定只应存在一份的资源必须使用唯一文件名。

CSV 与配置数据仍按 `config-data-design` 使用，不在业务层直接调用 `ResourceManager.LoadCsv`。

## 开发者文档

只有在修改资源索引、Mod、优先级、初始化流程或新增资源类型时，才读取 [资源管理与 Mod 底层](developer-docs/resource-manager-and-mods.md)。
