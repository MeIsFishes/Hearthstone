# 任务元数据、节点选择与 Inspector

## 1. 模块说明

本模块由 Core `TaskCatalog`、`TaskMetadataDirectoryResolver`、`TaskDefinition` 系列契约和 WPF `TaskSelectionWindow`、`InspectorControl` 组成。TaskCatalog 把 Unity 导出的 Task、TaskContext 与 Enum JSON 解析为只读目录；节点类型选择器按场景候选集提供搜索，MainViewModel 维护 Context 绑定。右侧只有一个 `InspectorControl` 入口，内部由策略按当前文档分派 Task 或 CSV 呈现；Task 策略根据当前 `TaskInstance` 的字段类型和来源动态生成编辑控件，CSV 策略的具体数据规则由数据编辑器模块负责。

ConnectPoint 不作为普通字段显示，由行为树端口负责。常量支持基础类型、枚举、List 和 Dictionary；集合元素只能是基础类型或枚举，不支持集合嵌套和类对象。

## 2. 对外接口

- `TaskCatalog.LoadFromDirectory`、`FindTask`、`FindContext`、`FindEnum`：元数据加载与查询。
- `TaskMetadataDirectoryResolver.Resolve`：从配置的 BbxEditor 元数据目录、本目录的 `Task` 子目录和同级 `ExportedTaskInfo` 中选择实际包含 Task 导出类型的目录。
- `MainViewModel.SelectTask`、`IDialogService.SelectTask`、`TaskSelectionWindow.FilteredTasks`：按场景筛选，通过字面与向量搜索排序并返回单个节点类型。
- `SelectedContext`、`BindingContextType`：文档 Context 绑定。
- `InspectorControl.Task`、`TimelineItem`、`Catalog`、`BindingContextType`：Task Inspector 的当前任务、可选时间信息和字段编辑输入。
- `InspectorControl.CsvDocument`、`CsvRow`：CSV Inspector 的当前文档与选中行输入。
- `TaskValueTypeSupport`、`LegacyCollectionValueCodec`：类型支持、标量校验和集合编辑协议。

## 3. 调用链路

工作区加载元数据目录后，先由 `TaskMetadataDirectoryResolver` 检查候选目录中是否存在 `TaskExportInfo`、`TaskContextExportInfo` 或 `TaskEnumExportInfo`。当前 `ExportedBbxEditorInfo` 与 `ExportedTaskInfo` 并列时自动选择后者，避免把 `ExportedBbxEditorInfo/Task` 中的任务文档当成元数据。TaskCatalog 再按 `Default.TypeInfo` 分流任务、Context 和枚举，并建立完整名/短名索引；遇到 `.editor.json`、带 `TaskInfos` 的运行时任务文档或旧编辑布局字段时静默跳过，其他确实未知的 JSON 仍产生诊断。创建 Timeline 项、条件或行为树节点时，DocumentViewModel 按入口规则筛选 TaskDefinition。选择窗口先按显示名、类型名、完整名、标签和注释产生精确与字面结果，再以去掉主端约定的 `TaskNode`、`TaskOnce`、`TaskDuration`、`TaskCondition` 及框架 `TaskBt`、`TaskTimeline` 前缀和 `Task` 等语义后缀的类型短名执行向量排序，去重后追加到字面结果之后。搜索输入以 120ms 防抖，并取消上一条未完成查询；向量索引尚未 Ready 或查询失败时仍保留字面搜索。确认后用返回的定义创建默认 `TaskInstance`。

`MainWindow` 将 Task 选择输入和当前 CSV 文档/行同时绑定到共享 Inspector；控件优先用 `CsvDocument` 选择 CSV 策略，否则选择 Task 策略。选中任务后，MainViewModel 更新 `SelectedTask`；选中 Timeline 时间项时同时更新 `SelectedTimelineItem`。Task 策略在灰色圆角字段卡片中根据 FieldValueSource 显示 Value、Context 或 Blackboard 编辑器；TimelineItem 存在时，在最上方先显示双向绑定的 StartTime 和 Duration 卡片。无任务选择时显示独立的空状态说明。Value 对基础类型使用文本或选项控件，对 List/Dictionary 使用可增删排序的结构化控件；动态生成的控件从全局 GrayTheme 获取语义画刷和图标按钮样式。CSV 策略复用集合卡片交互，并为 Vector2/3/4 生成带 X/Y/Z/W 标签的分量编辑器，为 Color 生成十六进制输入、颜色预览和系统调色板入口，为 TaskBlackboardInjection 生成可增删排序的 Key/Type/Value 条目，其中 Type 使用固定类型下拉框；这些特殊编辑器与 Array 一样显式编码后经 `ApplyCsvValue` 回写单元格，不复用 Task 的集合字符串协议。每次有效变化直接写回领域对象并标记文档为脏；黑板条目存在空 Key、重复 Key 或类型值错误时保留本地草稿并提示修正。保存前验证器再次检查类型和集合边界。

## 4. 数据来源

- 用户设置的 BbxEditor 元数据目录，以及与它同级的 `../../ExportedTaskInfo/` Task 导出目录。
- 当前文档的 `BindingContextType`。
- 当前 `TaskInstance.Fields` 的类型、注释、来源和值。
- 当前选中 `TimelineItem` 的 StartTime 与 Duration。
- 当前 CSV 文档与选中 `CsvRow`；字段类型来自 CSV 导出元数据。
- Enum 候选值和 Context 可引用字段。
- Task 类型短名经前后缀清理后的向量名称，以及工作区共享的 `vector-index.json`。

## 5. 与其他模块的依赖

本模块依赖 Core 契约、元数据解析和 `VectorSearchNameNormalizer`，WPF 控件依赖工作区提供当前文档、选择与可取消的向量排序回调。`TaskMetadataDirectoryResolver` 只通过 JSON 顶层类型识别目录，不依赖 WPF 或 Unity 程序集。Timeline、行为树和 CSV 编辑器共享同一个 Inspector 入口；Timeline 额外提供当前时间项，CSV 编辑器额外提供当前行。CSV 策略的直接 Value 编辑和脏状态同步依赖数据编辑器模块。Dictionary 与 Unity JsonApi 的兼容规则由 CrossLibrary 契约模块约束。
