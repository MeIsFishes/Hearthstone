# CrossLibrary 跨工程契约

## 1. 模块说明

CrossLibrary 是 BbxEditor 与 Unity 游戏侧需要保持一致的历史协议来源。`.NET Core` 使用自身的稳定契约模型读取元数据并输出旧协议，不直接引用 Unity 程序集；SmokeTests 会链接 Unity 项目中的实际 `JsonApi`/LitJson 源码执行 Dictionary 往返，防止协议漂移。

CrossLibrary 唯一源码位于 `../../BbxEditor/Assets/Scripts/BbxCommon/CrossLibrary/`。与编辑器直接相关的是任务导出类型、字段值来源、运行时任务组、集合分隔符和 JsonApi 字典形状。

## 2. 对外接口

- Core `TaskDefinition`、`TaskContextDefinition`、`TaskEnumDefinition`：元数据的稳定内存表示。
- Core `RuntimeTaskValue`、`RuntimeTaskGroup`：运行时导出的稳定内存表示。
- `TaskContractConstants`：任务标签和 List 分隔符 `%||%`。
- CrossLibrary `JsonApi.SerializeToString`、`DeserializeFromString`：Dictionary JSON 字符串协议。
- CrossLibrary `TaskValueInfo.AddFieldInfo(..., Dictionary<TKey,TValue>)` 与 Unity `TaskBase.ReadDictionary`：写入和读取 Dictionary 常量。

## 3. 调用链路

Unity 导出器生成带 `Default.TypeInfo` 的任务、Context 和 Enum JSON。`.NET TaskCatalog` 解析为 Core 契约，Inspector 和两个编辑器只依赖 Core 类型。保存时 `RuntimeExporter` 生成运行时模型，`LegacyRuntimeWriter` 输出 CrossLibrary 兼容 JSON。

Dictionary 由 `.NET LegacyCollectionValueCodec` 生成 JsonApi 字典形状，Unity 运行时使用同一 JsonApi 还原。SmokeTests 直接编译 Unity 的 JsonApi/LitJson 源码执行真实往返，因此无需维护 BbxEditor 侧源码副本。

## 4. 数据来源

- `../../ExportedTaskInfo/` 中的 Unity 导出 JSON。
- Unity 项目中的 CrossLibrary 公共源码。
- 历史 `.editor.json` 和游戏运行时 JSON 中的类型信息、字段来源与集合编码。
- Unity `TaskBase` 的 List/Dictionary 运行时读取规则。

## 5. 与其他模块的依赖

任务元数据、Inspector、持久化和运行时导出依赖这些协议语义；Core 本身不依赖 CrossLibrary 程序集、Unity 或 WPF。SmokeTests 在编译期依赖相邻 Unity 项目的 JsonApi/LitJson 源文件和运行时 `TaskBase` 源码位置。
