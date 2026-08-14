# BbxEditor .NET 重写计划

> 状态说明：`.NET 10/WPF` 重写、一级目录迁移和旧工程清理均已完成。当前 solution、源码、测试和发布目录直接位于 BbxEditor 根目录；本文保留为实施与验收记录。

## 1. 目标与默认假设

重写目标是移除 Godot 运行时与场景资源依赖，把 BbxEditor 建成结构清晰、可测试、可独立发布的 .NET 桌面应用，同时保持 Unity 导出类信息、现有编辑文件和游戏运行时任务文件的兼容性。

默认技术决策如下：

- 目标运行时采用 .NET 10 LTS。微软当前支持策略显示 .NET 10 的支持期到 2028-11-14；实施时始终使用最新的 10.0.x 补丁版本。[.NET 官方支持策略](https://dotnet.microsoft.com/en-us/platform/support/policy)
- Windows 桌面优先，UI 主方案采用 WPF。WPF 是 .NET 的 Windows 专用桌面 UI 框架，具备 XAML、数据绑定、命令、模板和 2D 图形能力，适合属性面板、Timeline 与节点画布。[WPF 官方概览](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/)
- 若必须支持 macOS/Linux，只替换 Presentation 层并重新评估 Avalonia 等跨平台框架；Domain、Application、Contracts 和 Infrastructure 不得依赖 WPF。
- `.NET` 工程是当前唯一实现，旧 UI 工程、场景、脚本和资源已删除。

## 2. 必须保持的兼容边界

### 2.1 Unity 导出类信息

新编辑器必须继续读取 `TaskExportInfo`、`TaskContextExportInfo`、`TaskEnumExportInfo` 三类导出文件，保留以下语义：

- TaskTypeName、TaskFullTypeName、Tags、Comment、FieldInfos。
- TaskExportTypeInfo 的泛型嵌套，以及 `TaskConnectPoint.Single/Multiple`。
- Context 字段和枚举候选值。
- 多标签筛选当前采用“至少命中一个”的 OR 语义。

### 2.2 游戏运行时文件

在游戏加载器一并迁移之前，`.json` 必须继续输出 `TaskGroupInfo` 旧协议：

- `RootTaskId`、`BindingContextFullType`、整数 ID 到 `TaskValueInfo` 的映射。
- 字段值继续使用 `ETaskFieldValueSource + string Value`。
- List 与连接点值继续使用 `%||%` 分隔。
- Timeline 继续输出根 `TaskTimeline`、TimelineItemInfos 和三组条件引用。
- 行为树继续保持每个字段连接列表的顺序。
- JSON 形状继续兼容 `Default.TypeInfo` 和旧集合/字典编码；保留旧 JsonApi 将字符串 `"null"` 作为空引用哨兵的语义，Dictionary 的 string 键和值不允许使用该字面量。

旧格式应由独立的 Legacy 适配器负责，不允许散落在 ViewModel 或控件中。

### 2.3 编辑器文件

首版只读写历史 `.editor.json`，不引入 `schemaVersion: 2` 或其他新协议。旧格式由独立 Legacy importer/writer 负责，继续保持 `Default.TypeInfo`、私有字段名、`Godot.Vector2` 协议标签和旧集合/字典形状；保存采用临时文件和备份，确保 editor/runtime 两份文件成对提交。

## 3. 目标解决方案结构

最终目录结构：

```text
BbxEditor/
  BbxEditor.Net.sln
  Directory.Build.props
  src/
    BbxEditor.Core/
    BbxEditor.Wpf/
  tests/
    BbxEditor.SmokeTests/
  artifacts/
    win-x64/
    BbxEditor.Net-win-x64.zip
  AutoDoc/
```

各项目职责：

- `BbxEditor.Core`：集中 Contracts、Domain、Application、Infrastructure 命名空间，承载领域模型、校验、旧协议读写、运行时导出和设置，不引用 UI。
- `BbxEditor.Wpf`：窗口、工作区标签、对话框、Inspector、任务选择器、Timeline 视图和行为树画布。
- `BbxEditor.SmokeTests`：验证元数据数量、Context 名称兼容、集合值编码、真实旧 editor 往返、TaskId/RootTaskId 和成对保存。

Contracts 不抽成 Unity 共享程序集；编辑器与 Unity 两边分别维护，通过 Golden File 和游戏集成测试检查一致性。

## 4. 领域模型设计

### 4.1 共享编辑模型

用显式模型替代当前 `EditorModel` 静态 Facade：

- `EditorSession`：打开文档集合、当前文档、最近路径和当前选择。
- `TaskCatalog`：任务、Context、枚举的只读索引及导入诊断。
- `TaskInstance`：TaskType、字段值集合，不持有 UI 节点。
- `FieldValue`：保留 Value/Context/Blackboard 来源，并对常量提供类型化编辑值；导出旧协议时再格式化成字符串。
- `DocumentBase`：DocumentId、FilePath、BindingContextType、IsDirty、FormatVersion。
- `SelectionService`：保存 NodeId/TimelineItemId，不保存 WPF Control 引用。

所有数值解析与输出使用 invariant culture。加载时记录未知类型、重复任务名、缺失 Context、字段类型不匹配等诊断，不静默返回 null。

### 4.2 Timeline 模型

- `TimelineDocument.Items` 为有序集合。
- 每个 `TimelineItem` 有稳定 ItemId、TaskInstance、StartTime、Duration 和三组有序条件。
- `Duration < 0` 明确定义为 Endless；MaxTime 只由有限结束时间计算。
- 移动、删除、新增条件直接更新模型并统一标记脏状态；首版不提供撤销/重做。
- 运行时转换器独立分配连续 TaskId，不依赖 UI 顺序之外的隐含状态。

### 4.3 行为树模型

不要继续以 `Dictionary<string, List<GraphNodeLineEditData>>` 作为核心模型。建议使用：

- `BehaviorNode`：稳定 NodeId、显示名、TaskInstance、Position。
- `BehaviorEdge`：SourceNodeId、SourcePortKey、TargetNodeId、Order。
- `PortDefinition`：EnterCondition、Condition、ExitCondition 或具体 ConnectPoint 字段。
- 内存中使用稳定 Guid；导出旧 `.editor.json` 或运行时文件时再映射为旧节点名和唯一整数 TaskId，根节点不要求排在第一个。

领域校验至少包含：唯一根节点、根类型、单父节点、Single 输出上限、端口类型匹配、禁止自连接、环检测、不可达节点、重复 Order 和悬空连接。错误阻止运行时导出，警告允许保存编辑文件。

## 5. UI 设计

### 5.1 应用外壳

- 主窗口采用 MVVM，顶部命令对应 New Timeline、New Behavior Tree、Open、Save、Save As、Delete、Settings。
- 文档标签使用可关闭 TabControl；超过可见范围时使用滚动/下拉，不复制当前固定 6 个标签的限制。
- 全局命令绑定替换 `BbxButton + InputApi`；保存、打开、删除等命令统一管理 CanExecute。
- 设置使用 Options/Settings service，保存 ExportInfoPath、LastSaveTargetPath 和窗口布局；路径不写入仓库默认配置。

### 5.2 Inspector 与任务选择器

- 使用 DataTemplate 按 FieldEditorKind 显示文本、数字、布尔、枚举、列表、Null、Context、Blackboard 控件。
- Context 候选项同时做类型兼容过滤；不兼容值显示诊断而非静默接受。
- List 与 Dictionary 使用可增删排序的结构化控件；只支持基础类型和枚举，不支持集合嵌套或类对象。Dictionary 在旧 Value 字符串中嵌入 CrossLibrary JsonApi 的完整字典 JSON，并由 Unity `TaskBase.ReadDictionary` 使用相同 JsonApi 还原；编辑器严格检查泛型类型、连续键值对和类型转换后的重复键，只迁移结构完整的早期 `%||%` 数据。
- 任务选择器保留标签、搜索、分页/虚拟化和注释预览，筛选规则写成可单元测试的 Query 对象。

### 5.3 Timeline 视图

- 左侧显示任务与条件折叠区，右侧使用可缩放时间标尺和区间条；两者共享垂直滚动。
- StartTime/Duration 既可精确输入，也可在时间条上拖动；条身调整开始时间，右端调整时长。首版不增加吸附配置。
- 排序与时间位置是两个独立概念，运行时 ID 仍按列表顺序稳定生成。

### 5.4 行为树画布

- 使用自定义 WPF Canvas/FrameworkElement 实现缩放、平移、节点拖动、端口命中测试、Bezier 连线和框选。
- 节点/边只绑定 ViewModel；控件销毁不影响领域数据。
- 连接拖动到空白处时保留当前“按端口类型打开任务选择器并自动连接”的高效交互。
- 子节点顺序显示在边或目标输入处，并提供上移、下移或拖动排序。

## 6. 分阶段实施

### 阶段 0：冻结基线与决策（S）

工作内容：

- 收集 `ExportedTaskInfo`、Timeline `.editor.json/.json` 和行为树 `.editor.json/.json` 作为脱敏测试语料。
- 与游戏侧确认 RootTaskId、BindingContextFullType 是否实际要求完整类型名、JSON 属性顺序是否无关。
- 确认 Windows-only 或跨平台、Unity 版本、是否需要新旧编辑器双向互操作。
- 记录现有编辑器的功能矩阵和关键截图/操作录像。

退出标准：每类文件至少有正常、边界和损坏样例；兼容边界有明确负责人确认。

### 阶段 1：解决方案骨架与契约测试（M）

工作内容：

- 建立上述项目结构、依赖方向、代码规范和 CI。
- 从 CrossLibrary 只提取任务契约，不迁移未使用的 CSV、本地化、对象池和 LitJSON。
- 实现 Legacy JSON token reader/writer，优先使用 `System.Text.Json` 自定义转换器复现旧 JSON 形状。
- 为 22 个当前导出文件和现有运行时文件建立 golden tests。

退出标准：全部导出样例可读取；旧运行时文件读写后经 JSON 语义比较等价；Contracts 不引用 WPF/Godot/Unity。

### 阶段 2：元数据、文档与转换核心（L）

工作内容：

- 实现 TaskCatalog 导入、重复/损坏文件诊断和手动刷新。
- 实现共享 TaskInstance/FieldValue、Context 与枚举校验。
- 实现 Legacy editor 文件到新 Domain 的映射和字段协调。
- 分别实现 TimelineRuntimeExporter 与 BehaviorTreeRuntimeExporter。
- 使用临时文件 + 原子替换完成双文件保存，避免只成功一半。

退出标准：无 UI 也能通过测试加载旧 editor 文件并导出与基线语义等价的 runtime JSON；失败不会覆盖原文件。

### 阶段 3：应用外壳、任务选择器与 Inspector（L）

工作内容：

- 完成 WPF 主窗口、多文档标签、设置、文件对话框和命令。
- 完成任务选择器、绑定 Context、Inspector 与字段控件。
- 完成 IsDirty 和关闭未保存提示；不引入 Document command stack 或撤销/重做。

退出标准：可以新建/打开/另存为两类空文档，编辑全部现有字段类型，重启后设置正确恢复。

### 阶段 4：Timeline 编辑器迁移（L）

工作内容：

- 实现 Timeline 列表、标尺、区间条和 Inspector 联动。
- 实现任务/条件增删排序、折叠、负 Duration 和 MaxTime。
- 补充时间条拖动；保留精确数值输入，不要求吸附与撤销/重做。

退出标准：当前 Timeline 样例可无损载入；编辑后导出文件通过游戏侧加载测试；新增、删除、排序、三类条件、无限持续和时间条拖动可用。

### 阶段 5：行为树编辑器迁移（XL）

工作内容：

- 实现节点画布、端口、连接、缩放/平移和节点布局持久化。
- 实现 Action/Drive/Condition 筛选、拖线新建、Single/Multiple 限制和子节点排序。
- 实现根、环、不可达节点和悬空连接诊断。
- 对旧节点名/字段连接列表做双向映射。

退出标准：真实行为树样例可无损载入；连接顺序与条件引用导出正确；所有结构校验有单元测试和可定位的 UI 提示。

### 阶段 6：切换、清理与发布（M）

工作内容：

- 用真实 Mod 目录进行并行运行和批量兼容检查。
- 完成崩溃日志、自动备份、发布打包、升级说明和用户操作文档。
- 已删除旧 UI 工程，仅保留 Legacy importer/writer 对历史文件协议的兼容。
- 发布目录和压缩包统一放在 BbxEditor 一级 `artifacts/`。

退出标准：功能矩阵全部通过，运行时文件由游戏集成测试覆盖，发布包可独立启动；历史文件回退依赖保存时生成的备份，而不是旧编辑器。

## 7. 测试策略

### 7.1 契约与 Golden File 测试

- 读取全部 Unity 导出元数据并核对任务数、类型、字段、标签、Context 和枚举。
- 读取旧 Timeline 与行为树 editor 文件，映射到 Domain 后核对关键数据。
- 导出 runtime JSON，以解析后的 token tree 比较语义；另加游戏加载器集成测试。
- 固定测试 `%||%`、Dictionary 中禁止的字符串 `"null"`、旧字典键、损坏/缺项字典、类型转换后重复键、私有字段名和嵌套泛型。

### 7.2 领域测试

- 字段新增/删除协调保留旧值，类型变化产生诊断。
- Value/Context/Blackboard 三种来源和 invariant 数字转换。
- Timeline 的 MaxTime、Endless、任务/条件 ID 分配和三类条件引用。
- 行为树的根节点、Single/Multiple、单父、排序、环、不可达与删除节点后的边清理。

### 7.3 应用与 UI 测试

- ViewModel 命令、CanExecute、脏状态和关闭确认。
- 文件保存失败、只读目录、路径不存在、损坏 JSON、重复元数据。
- Timeline 拖动和行为树端口命中测试以可测试的几何服务实现。
- 少量端到端 UI 自动化覆盖打开、修改、保存、重新打开。

### 7.4 性能基线

- 元数据导入、打开大文件、保存、画布首次渲染和缩放帧率。
- 目标至少覆盖 500 节点/1000 连线和 1000 Timeline 项的压力样例；若实际项目规模更大，以真实 P95 文件重新定标。

## 8. 已发现的迁移风险

- 当前节点图保存中，在识别 `TaskBtRoot` 后使用了自增后的 `taskId` 设置 RootTaskId，存在索引偏移嫌疑。必须以游戏实际加载结果确认正确语义，不能直接复制实现。
- `JsonApi` 依赖类型全名和私有字段名；类重命名会使旧文件失效。Legacy reader 应使用稳定映射表，而不是 `Type.GetType`。
- 当前 JSON API 捕获异常后可能返回 null，外层仍可能继续并提示成功。新保存流程必须返回显式 Result，并以事务方式提交两个文件。
- 当前 Context 引用不做字段类型匹配，Blackboard 也没有 schema；重写增加诊断时要允许旧数据先载入再修复。
- 当前行为树没有环、不可达节点和严格根检查；加入校验可能暴露历史文件问题，应区分 Warning 与 Blocking Error。
- 当前节点名和字段名用英文句点拼接为连接键；若名称含句点会无法解析。新模型使用稳定 ID，Legacy 适配器只在边界处理旧键。
- 当前静态 EventBus、EditorModel 和热键树使测试与生命周期耦合；重写不得把这些静态模式搬到新架构。
- CrossLibrary 包含大量编辑器未使用代码。整包迁移会延续 Unity 条件编译、第三方源码和无关维护成本，应以引用清单做白名单迁移。

## 9. 验收清单

- [x] 可读取全部现有 Unity 导出类信息，错误文件有明确诊断。
- [x] 可读取现有 Timeline 和行为树 `.editor.json`。
- [x] 两类编辑器在数据模型、ViewModel 和视图上保持分离。
- [x] 首版支持的字段类型、值来源、Context、枚举、List 和 Dictionary 可编辑；复杂对象明确不支持。
- [x] Timeline 的时间、顺序、三类条件和无限持续可按旧协议导出。
- [x] 行为树的端口、连接顺序、根和条件引用可按旧协议导出。
- [ ] runtime JSON 通过 golden tests 和游戏加载器集成测试。
- [ ] 保存为原子操作，具备脏状态、未保存提示、备份和恢复路径。
- [x] 核心领域与文件转换测试不启动 UI 即可运行。
- [x] 新应用不引用 Godot，Core 不引用 WPF。
- [ ] 发布包、升级说明、回退方案和用户操作文档齐备。

## 10. 已确认的首版决策

1. 使用 .NET 10 LTS、Windows WPF，并保持当前 UI 方向。
2. 只读写旧 `.editor.json` 和旧 runtime 协议，不启用 v2。
3. TaskId 必须唯一；RootTaskId 指向实际根节点，根节点不要求排在首位。
4. 行为树保持唯一根、单父、无环的严格树语义。
5. Context 同时接受短名和完整名；未来优先保存元数据完整名，显示继续采用去命名空间和 `TaskContext` 前缀的规则。
6. List/Dictionary 结构化编辑只支持基础类型和枚举，不支持复杂对象嵌套。
7. Timeline 支持拖动，行为树支持端口拖线；首版不提供撤销/重做。
8. Contracts 与 Unity 两边分别维护，通过 Golden File 与游戏集成测试防止漂移。
