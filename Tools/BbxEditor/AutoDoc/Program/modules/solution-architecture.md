# .NET/WPF 重写实现

## 1. 模块说明

解决方案直接位于项目一级目录，分为 `BbxEditor.Core`、`BbxEditor.Wpf` 和 `BbxEditor.SmokeTests`：Core 保存通用元数据契约、文档模型、Task 兼容读写、CSV/Unity YAML codec、验证和导出；WPF 提供应用外壳及四种文档视图；SmokeTests 验证编辑器侧格式兼容。源码、测试和发布产物分别位于 `src/`、`tests/`、`artifacts/`。

当前使用 .NET SDK 10.0.302，项目统一目标为 `net10.0`/`net10.0-windows`。桌面 UI 保持 Windows WPF；Timeline 与行为树拥有不同的文档、ViewModel 和视图，只有工作区、节点类型选择器、Inspector 和文件服务共享。首版不实现撤销/重做。

## 2. 对外接口

- `TaskCatalog.LoadFromDirectory`：读取 Unity 导出的 Task、Context 和 Enum JSON。
- `BbxMetadataCatalog.LoadFromDirectory`：读取 Unity 导出的 CSV、BbxScriptableObject 与资产索引元数据。
- `WorkspaceDocumentService.Open/Save`：统一分发 `.editor.json`、`.csv` 和 `.asset` 文档。
- `CsvDocumentCodec`、`ScriptableObjectDocumentCodec`：分别负责 CSV 与受限 Unity YAML 的读取、验证和原子保存。
- `DocumentFileService.Open`：只按旧 `Default.TypeInfo` editor 协议加载 `.editor.json`。
- `DocumentFileService.Save`：保存旧协议 `.editor.json`，验证后同时写旧协议 runtime `.json`。
- `TaskReconciler.Reconcile`：按最新任务定义补充、删除和更新字段。
- `DocumentValidator.Validate`、`RuntimeExporter.Export`：验证文档并分别转换 Timeline 或行为树。
- `LegacyCollectionValueCodec`：List 按 `%||%` 编码；Dictionary 生成/读取 CrossLibrary JsonApi 的完整字典 JSON，严格检查泛型类型信息、连续键值条目与类型转换后的键唯一性，并限制键和值为基础类型或枚举。仅结构完整的早期 `%||%` 键值交替数据会迁移；损坏数据保留原值并由校验器报错。
- WPF 主窗口：提供多文档、文件命令、配置入口、节点类型选择、Context 和 Inspector。

## 3. 调用链路

程序启动时，`MainViewModel` 从本机 AppData 加载设置，随后 `TaskCatalog` 和 `BbxMetadataCatalog` 导入类信息。用户打开文件后，`WorkspaceDocumentService` 根据扩展名和文件内容创建 Timeline、BehaviorTree、CSV 或 ScriptableObject 文档，主窗口再通过具体 ViewModel 的 DataTemplate 选择视图。

字段选择通过 Inspector 直接更新 `TaskInstance`；List/Dictionary 使用可增删排序的结构化控件，类对象和复杂集合嵌套会阻止导出。Timeline 操作维护有序时间项和三组条件，时间条支持拖动开始位置及右端调整时长。各添加入口按场景筛选 TaskCatalog 并弹出节点类型选择器；行为树画布维护节点位置及带端口、顺序的 Edge，支持端口拖线到节点，或在空白落点选择兼容类型后创建并连接节点。

行为树连接统一执行唯一根、根无父节点、单父、Single 输出上限、Condition 端口匹配和无环校验；导出按节点实际顺序分配唯一 TaskId，RootTaskId 指向实际根节点而不要求根排在首位。Context 读取兼容短名和完整名；元数据升级为完整名后保存完整名，UI 显示仍去掉命名空间与 `TaskContext` 前缀。保存时先协调字段并验证，再将旧 editor 与旧 runtime JSON 写入临时文件；两份文件都成功后才替换目标，失败时恢复备份。

## 4. 数据来源

- Unity 导出的 `TaskExportInfo`、`TaskContextExportInfo`、`TaskEnumExportInfo` JSON。
- 历史旧版 `.editor.json`；其中保留 `Godot.Vector2` 等兼容类型标签，但程序不依赖 Godot，也不读写 `schemaVersion: 2`。
- BbxEditor 程序目录下的 `settings.json`。
- Unity 导出的 CSV/BbxScriptableObject 元数据与资产索引。
- 内存中的 TimelineDocument、BehaviorTreeDocument、CsvDocument、ScriptableObjectDocument、TaskCatalog 和 BbxMetadataCatalog。
- 运行时输出仍使用旧 `TaskGroupInfo` JSON 协议。

## 5. 与其他模块的依赖

Core 只依赖 .NET 基础库，不依赖 Unity 或 WPF。WPF 项目依赖 Core，并使用 Windows Presentation Foundation；SmokeTests 依赖 Core，当前验证编辑器侧 Task、CSV、元数据、Unity YAML 和设置行为。Unity 导出端完成后，应恢复真实 CrossLibrary Dictionary 与 Unity 资产重导入的端到端约束。
