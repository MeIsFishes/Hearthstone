# CSV 与 BbxScriptableObject 编辑器

## 1. 模块说明

本模块为 BbxEditor 提供 CSV 与 BbxScriptableObject 两种数据文档。Unity 游戏端是类型元数据的唯一权威来源；BbxEditor 不加载业务程序集。普通 CSV 可以无类型打开，`.asset` 则必须通过 MonoScript GUID 匹配到导出的 BbxScriptableObject 类型后才能编辑。

CSV 编辑器保留表头、表头后的两行契约注释、未知列、UTF-8 BOM 与换行风格，并按元数据验证基础类型、枚举、分号分隔数组、Color、Vector2/3/4、TaskBlackboardInjection、必填和唯一约束。第一行契约注释必须为与表头等量的英文字段说明：仅当该行按英文逗号拆分后的数量与表头一致时，说明才按位置应用到各列，并以较小字号显示在列名下方和右侧 Inspector 字段中。第二行必须使用 `// Associated: TableA, TableB` 或 `// Associated: None`；两行不作为数据行显示或参与字段值校验。右上角 `Associated Tables` 菜单把第二行声明的逻辑关系解析为当前工作区中的物理 CSV 文件；同一表在 Native 和多个 Mod 中存在时分别列出，标签始终显示表名和 Mod，仅在元数据类名与表名不一致时补充类名。Color 使用单单元格 `#RRGGBB` 或 `#RRGGBBAA`，各十六进制通道对应主端 Unity `ColorUtility` 的 0–255 字节值；Vector 使用单单元格 `x;y`、`x;y;z` 或 `x;y;z;w`；TaskBlackboardInjection 使用单单元格 `Key,Type,Value;...`，支持 bool/int/long/float/double/string 与 `\\`、`\,`、`\;` 内层转义。表头关闭排序，单击后可直接改名；点击编辑器内其他任意位置时会提交当前 CSV 编辑并移除原输入焦点。右键 `Turn To…` 可在该列中按行定位。定位窗口始终把精确和字面匹配放在前面，仅当列具有导出的 `String` 类型元数据且全局向量模型 Ready 时，才追加去重后的语义结果。点击任意单元格或行后，当前行进入与 Task 共用的右侧 Inspector 入口；CSV 策略不显示 Task 的 Source、Context 或 Blackboard，普通字段直接编辑 Value，Array 字段显示可增删和排序的结构化元素，Vector2/3/4 显示 X/Y/Z/W 分量，Color 显示十六进制值、颜色预览并可打开系统调色板，TaskBlackboardInjection 则按条目显示 Key、Type 和 Value，其中 Type 使用下拉框选择并允许增删及排序。BbxScriptableObject 编辑器只改写元数据声明的标量、简单数组和嵌套对象叶子字段，保留未识别的 Unity YAML 内容。

## 2. 对外接口

- `CsvDocumentCodec.Open`、`Save`、`Validate`：解析、原子保存和验证 CSV 文档。
- `CsvDocumentCodec.GetFieldDescriptions`：按英文逗号拆分首行注释，列数与表头一致时返回去除首个 `//` 标记并按列对齐的字段说明，否则返回空集合。
- `CsvDocument.HeaderComments`：按原顺序保存表头后的字段说明与 Associated 注释，保存时写回表头和数据行之间。
- `CsvAssociationContract.TryParse`：统一解析和验证 Associated 注释，`None` 返回空表名列表。
- `BbxMetadataCatalog.FindCsvByTableName`：按逻辑表名取得导出的 CSV 类型元数据及其 `TableNames`。
- `CsvAssociationTargetResolver.Resolve`：把 Associated 表名、元数据和未筛选的项目文件索引解析为可打开或带原因的禁用目标。
- `TaskBlackboardInjectionCodec.TryParse`、`Serialize`：把单元格读写为带 Key、类型和规范值的结构化集合，并拒绝重复 Key、未知类型和非法值。
- `ScriptableObjectDocumentCodec.Open`、`Save`、`Validate`：按脚本 GUID 与字段元数据读写受限的 Unity YAML。
- `CsvEditorControl`：动态创建单元格编辑列、可编辑表头、表头右键菜单和右上角 Associated 表菜单；表头为空或重名时拒绝提交。
- `MainViewModel.ResolveAssociatedCsvTargets`、`OpenAssociatedCsv`：从最新工作区状态生成导航目标，并通过既有固定页签打开链路跳转。
- `CsvDocumentViewModel.SelectedRow`、`InspectorControl.CsvDocument`、`CsvRow`：把 DataGrid 当前行传入共享 Inspector。
- `CsvArrayValueCodec.Decode`、`Encode`：在 Inspector 元素集合与 CSV 单元格的分号协议之间转换，并保留中间空元素。
- `CsvInspectorValueCodec`：在 Inspector 的 Vector 分量和分号协议之间转换，解析 `#RRGGBB` / `#RRGGBBAA` 字节颜色，并在调色板改色时保留已有 Alpha 通道。
- `InspectorControl.ApplyCsvValue`：CSV 策略的统一回写入口；实际值变化时更新 `CsvCell.Value` 并通知工作区。
- `CsvColumnSearchWindow`：显示列内搜索结果并返回所选行；精确值优先于包含匹配，向量结果位于字面结果之后。
- `MainViewModel.RankCsvColumnValuesAsync`、`VectorSearchCoordinator.RankCsvColumnValuesAsync`：为 String 列提供按缓存分区的向量排序。

## 3. 调用链路

`WorkspaceDocumentService` 按扩展名分发文件：`.csv` 进入 `CsvDocumentCodec`，`.asset` 先读取 MonoScript GUID 再进入 `ScriptableObjectDocumentCodec`。CSV 打开时先读取表头，再把紧随其后的 `//` 行提取为 `HeaderComments`，其余记录才建立可编辑数据行；保存时按表头、契约注释、数据行的顺序写回。验证器要求恰好两行契约注释、英文说明数与列数一致，并检查 Associated 行的固定前缀、名称合法性、去重和字母顺序。工作区按文档类型创建对应 ViewModel，WPF 隐式 DataTemplate 随当前页签选择编辑视图。

CSV 视图建立 DataContext 以及每次展开 `Associated Tables` 菜单时，调用工作区取得最新目标。Resolver 先通过 `CsvAssociationContract` 读取逻辑表名，再通过 `FindCsvByTableName` 取得元数据；元数据的全部 `TableNames` 构成候选文件名集合，并与未经过 Explorer 搜索、Mod 或类型筛选的完整索引匹配。当前文件被排除，Native 排在其他 Mod 之前；文件、元数据或索引不可用时保留英文禁用项和原因。可用项点击后调用既有非 preview `OpenDocument`，所以已打开文件只切换页签，新文件创建固定页签并继续获得最近文件记录和外部变动监听。

CSV 视图依据当前表头和 `CsvTypeMetadata.Columns` 动态建列，并通过 `GetFieldDescriptions` 取得位置对齐的首行说明。每个列头由可编辑列名和 10px 换行说明组成；同一说明传给共享 Inspector，在字段名下以 11px 次级文字显示，若导出元数据 Tooltip 与其相同则不重复追加。用户修改表头并离开输入框或按 Enter 后，控件检查空名称和重复名称，替换 `CsvDocument.Columns`、标记文档为脏并重建类型化列。主窗口捕获鼠标预览事件；当键盘焦点属于 CSV 视图且点击目标不在当前焦点元素内时，先提交 DataGrid 的单元格与行编辑，再清除原键盘焦点，因此表头、页签、Inspector 或空白区域点击都不会让旧输入继续激活。点击单元格时，DataGrid 的当前项同步到 `CsvDocumentViewModel.SelectedRow`；共享 Inspector 选择 CSV 策略，为整行逐列生成字段卡片。Boolean 和具有候选值的 Enum 使用 ComboBox，Vector2/3/4 使用等宽分量输入框，Color 使用文本框和调色板按钮，TaskBlackboardInjection 使用结构化条目面板，其余非 Array 类型使用 TextBox；只读元数据禁用写入。Vector 逐分量编辑后仍按 invariant-culture 浮点字符串以分号拼接；Color 调色板输出大写十六进制 RGB，原值为 `#RRGGBBAA` 时保留 Alpha 后缀。TaskBlackboardInjection 的 Type 下拉框提供 Bool、Int、Long、Float、Double、String 六项；每次有效修改经 `TaskBlackboardInjectionCodec.Serialize` 规范化并回写，空 Key、重复 Key 或与类型不兼容的 Value 会显示错误且暂不覆盖原单元格。原始单元格已经无效时，Inspector 保留原文编辑入口，修复并重新解析后才能返回结构化面板。普通字段不再依赖隐式 TwoWay 回写，而是统一调用 `ApplyCsvValue` 更新原始 `CsvCell`；单元格从 DataGrid 或其他入口变化时，订阅回调会反向刷新 Inspector 控件。

Array 字段先通过 `CsvArrayValueCodec.Decode` 拆为本地元素集合。Boolean/Enum 元素使用 ComboBox，其他元素使用 TextBox；添加、删除、上下移动和修改元素后立即调用 `Encode`，再经 `ApplyCsvValue` 将分号字符串写回同一个单元格。回写触发 `CsvCell.PropertyChanged`，DataGrid 随即显示新值并将文档标记为脏；外部修改该单元格时，Array 编辑器会重新解码并刷新元素列表。只读 Array 隐藏集合操作并禁用元素编辑。

右键表头选择 `Turn To…` 时，控件提交当前单元格编辑并拍摄该列的行值；搜索窗口先立即刷新精确与包含匹配。String 列在 120 ms 防抖后调用工作区向量入口，协调器以“CSV 文件名-列头名”取得分区，删除不再存在的值、补齐缺失 embedding、按本列语料重中心化并排序；窗口按值映射回行号并去除已出现的字面结果。快速连续输入会取消过时查询，但 embedding worker 的单请求管道在请求写入后仍完整读取并校验对应响应，随后才向调用方报告取消，避免旧响应遗留并破坏后续请求顺序。确认后 DataGrid 选中目标单元格并滚动到该行，同时刷新右侧 CSV Inspector。

BbxScriptableObject 打开时只收集元数据声明的可编辑 YAML 路径；保存时将 bool 编码为 Unity `0/1`、将枚举名称映射为数值，并保留 Unity 对象引用结构及未知源行。当前不支持 `SerializeReference` 多态图、复杂对象数组、AnimationCurve 和 Gradient。

## 4. 数据来源

- 游戏工程中的 `.csv` 与 BbxScriptableObject `.asset` 文件；CSV 特殊值类型由 Unity 导出的 `Color`、`Vector2`、`Vector3`、`Vector4`、`TaskBlackboardInjection` Kind 判定。
- CSV 表头后的两行契约注释；第一行使用英文逗号分隔逐字段英文说明并按表头位置映射，第二行记录跨 CSV 键关联。
- Unity 导出的 `CsvTypeMetadata.TypeName`、`FullTypeName`、`TableNames` 和 `DataLoadType`，用于确认关联表的物理文件名和菜单显示信息。
- 项目文件索引中的全部 `IndexedProjectFile` CSV 项；`FullPath` 用于打开，`RelativePath` 用于消歧和 Tooltip，`ModName` 用于区分 Native 与 Mod 副本。
- 当前 CSV DataGrid 选中的 `CsvRow` 及其中的 `CsvCell.Value`。
- CSV Array 单元格以 `;` 作为元素分隔符；空字符串表示空集合，中间连续分隔符表示空元素。
- CSV Vector 单元格以 `;` 保存 2–4 个浮点分量；Color 单元格保存 0–255 字节通道的 `#RRGGBB` 或 `#RRGGBBAA`，与主端 `CsvDataBase.ParseVector*FromKey`、`ParseColorFromKey` 一致。
- 元数据目录 `Csv/`、`ScriptableObject/` 和 `Assets/asset-index.json`；CSV 按表名/类型名匹配，ScriptableObject 按 32 位 MonoScript GUID 匹配。
- 编辑器目录下的 `csv-vector-index.tmp.json`。它与 Explorer/Task 使用的 `vector-index.json` 分离，顶层按“CSV 文件名-列头名”存储列值向量；模型指纹变化时整体失效，同一列再次查询时按当前非空唯一值增量同步。
- 全局设置中已启用且有效的共享 embedding 模型，以及随 BbxEditor 生命周期运行的 embedding worker 进程。

## 5. 与其他模块的依赖

本模块依赖 Core 的元数据契约、CSV/Unity YAML 文档模型、`CsvArrayValueCodec`、`TaskBlackboardInjectionCodec`、诊断与原子文件写入；WPF 视图依赖工作区的当前文档、状态提示、共享 Inspector、Windows 系统调色板和向量协调器。Associated 导航额外依赖元数据目录、`ProjectFileIndexService` 的未筛选索引、既有 Mod 归属规则和工作区 `OpenDocument` 页签路由，但不依赖 Explorer 当前显示过滤结果。Inspector 的入口与视觉容器由 Task/Inspector 模块提供，CSV 策略消费本模块的行、单元格和字段类型，并负责把结构化 Array、Vector、Color 和 TaskBlackboardInjection 状态显式回写为单元格协议。向量协调器复用应用工作区创建的独立 worker，但 CSV 列缓存和重中心化语料不进入 Explorer/Task 的全局名称索引。Explorer、最近文件和外部文件变动监听依赖本模块的 codec 判断文件是否合法并在需要时重载已打开文档。
