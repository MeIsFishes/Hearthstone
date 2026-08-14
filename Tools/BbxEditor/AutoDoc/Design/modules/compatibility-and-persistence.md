# 旧协议兼容与持久化设计

## 模块说明

持久化模块只支持既有 `.editor.json` 和既有游戏运行时 JSON，不引入 v2、`schemaVersion` 或新网络协议。编辑模型与旧 JSON 结构通过独立 importer/writer 隔离，使 Core 和 WPF 不依赖 Godot 类型，同时保持历史文件中的类型名、私有字段名和 `Godot.Vector2` JSON 形状。

保存一个文档会生成同 basename 的 `.editor.json` 与 `.json`。前者保存可继续编辑的状态，后者只包含游戏加载所需的任务组。

## 文件流程

1. `DocumentFileService.Open` 调用旧格式 importer，识别 Timeline 或 NodeGraph 文档。
2. `TaskReconciler` 按当前元数据更新字段和类型，并只迁移结构完整的早期 Dictionary 数据。
3. 保存前由 `DocumentValidator` 检查字段、集合和行为树语义。
4. `RuntimeExporter` 生成运行时任务组；旧 editor writer 与 runtime writer 分别序列化。
5. 两份内容先写临时文件，再成对替换目标；失败时恢复备份，避免半保存状态。

## 数据来源

- 历史 Timeline/NodeGraph `.editor.json`。
- 内存中的 Timeline 或行为树文档。
- Unity 导出的元数据与 CrossLibrary 运行时契约。
- List 的 `%||%` 编码和 Dictionary 的 CrossLibrary JsonApi JSON。

## 兼容边界

- 只支持旧 editor/runtime 协议；不支持新协议自动升级。
- Dictionary 必须包含匹配的泛型类型信息和连续完整的 `N, Key`/`N, Value`。
- CrossLibrary 的唯一源码位于 Unity 项目；SmokeTests 直接链接实际 JsonApi/LitJson 源码验证 Dictionary 往返。
- 旧文件中的平台类型名仅作为协议字符串由 Legacy importer/writer 处理，不引入对应 UI 运行时依赖。
