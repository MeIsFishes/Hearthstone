---
name: config-data-design
description: 说明如何使用 DataApi，以及如何设计新的配置数据（BbxScriptableObject 与 CsvData 的选型）。
---

# 配置数据设计

静态配置走 **Data**，运行时状态走 ECS **Component**；静态配置的读写入口是 **`DataApi`**，用法见 **[DataApi.md](DataApi.md)**。

## BbxScriptableObject 与 CsvData 怎么选

- **同一种配置在逻辑上只有一份**（全游戏单例）→ 用 **`BbxScriptableObject`**，在 `OnLoad` 里登记进 `DataApi`（见 **[BbxScriptableObject.md](BbxScriptableObject.md)**）。
- **多行数据、每行同一套字段**（表驱动）→ 用 **`CsvDataBase` + CSV**，每行在 `ReadLine` 里登记进 `DataApi`（见 **[CsvData.md](CsvData.md)**）。

## 同目录参考

- **[DataApi.md](DataApi.md)** — `SetData` / `GetData`、键类型、释放与遍历。
- **[BbxScriptableObject.md](BbxScriptableObject.md)** — 创建与登记 SO、`LoadingType`、Stage 数据组。
- **[CsvData.md](CsvData.md)** — 声明 `CsvDataBase`、`ReadLine`、加载约定。
