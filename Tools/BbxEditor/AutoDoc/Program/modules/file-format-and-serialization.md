# 文件格式与序列化

## 1. 模块说明

本模块负责读取历史 `.editor.json`、保存可继续编辑的旧格式文件，并同时导出游戏使用的旧运行时 JSON。Core 通过 `System.Text.Json` 和显式映射处理旧格式，不使用平台反射恢复 UI 对象。一个文档始终对应同 basename 的 `.editor.json` 与 `.json` 两份文件。

## 2. 对外接口

- `DocumentFileService.Open`：打开旧 Timeline 或 NodeGraph `.editor.json` 并协调元数据。
- `DocumentFileService.Save`：验证并成对写入 editor/runtime 文件。
- `LegacyEditorImporter.Import`、`LegacyEditorWriter.Serialize`：旧编辑格式与领域文档互转。
- `RuntimeExporter.Export`、`LegacyRuntimeWriter.Serialize`：领域文档到旧运行时格式。
- `LegacyCollectionValueCodec`：List 与 JsonApi Dictionary 的兼容编码、严格解码和有限迁移。

## 3. 调用链路

打开时，`DocumentFileService` 先检查 `.editor.json` 路径，再调用 importer 按 `Default.TypeInfo.FullType` 识别 Timeline 或 NodeGraph，随后由 `TaskReconciler` 按当前元数据更新字段。保存时先 Reconcile，再调用 `DocumentValidator`；无错误后分别生成 editor JSON 和 runtime JSON。

`AtomicWritePair` 把两份内容写入临时文件，备份已有目标后再成对替换；任何一步失败都会恢复原文件并清理临时文件。Dictionary 只在早期 `%||%` 数据结构完整时迁移为 JsonApi JSON，损坏内容保留并产生诊断。

## 4. 数据来源

- 历史 Timeline/NodeGraph `.editor.json`。
- 内存中的 TimelineDocument、BehaviorTreeDocument 和 TaskCatalog。
- `Default.TypeInfo`、旧私有字段名、`Godot.Vector2` 兼容标签和集合编码。
- 同 basename 的游戏运行时 `.json` 输出路径。

## 5. 与其他模块的依赖

本模块依赖 Core 领域模型、验证器、导出器和契约，不依赖 WPF 或 Godot 运行时。工作区调用它完成文件操作，Timeline 与行为树只维护领域状态。旧 JSON 中出现 `Godot.Vector2` 只是必须保留的协议字符串，不代表当前应用依赖 Godot。
