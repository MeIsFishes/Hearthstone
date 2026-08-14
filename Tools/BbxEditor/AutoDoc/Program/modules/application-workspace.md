# 应用外壳与工作区

## 1. 模块说明

本模块负责 WPF 应用启动、全局设置、统一元数据目录、多文档集合、当前文档、Context、节点类型选择、最近文件、文件命令、只读策划案查看和应用日志。主要实现为 `MainWindow`、`ExplorerControl`、`DesignPlanViewerControl`、`SettingsWindow`、`TaskSelectionWindow`、`LogDetailsWindow`、`MainViewModel`、`DocumentViewModel`、`ApplicationLog`、`DialogService` 与 `SettingsService`。它按文档类型创建 Timeline、行为树、CSV、BbxScriptableObject 或 Design Plan ViewModel，不实现具体编辑规则。

## 2. 对外接口

- `MainViewModel.Documents`、`CurrentDocument`：打开文档集合与当前标签。
- `NewTimelineCommand`、`NewBehaviorTreeCommand`、`NewCsvCommand`：创建 Timeline、行为树或 CSV。
- `OpenCommand`、`SaveCommand`、`SaveAsCommand`：打开和保存入口。
- `RecentFileMenuItems`：文件菜单“打开最近”的动态条目集合，显示文件名并以完整路径作为提示。
- `AppSettings.RecentDocumentPaths`：按最近使用顺序持久化的文档路径列表；`RecordRecentDocument`、`RemoveRecentDocument` 和 `NormalizeRecentDocuments` 负责置顶去重、删除与数量约束。
- `MetadataPath`、`ReloadCatalogCommand`：Task、CSV 与 BbxScriptableObject 统一元数据目录及刷新入口。
- `SelectedContext`、`SelectedTask`：Context 和 Inspector 的共享选择状态。
- `MainViewModel.SelectTask`：按调用方谓词筛选元数据任务并打开节点类型选择器。
- `IDialogService`：打开/保存文件、带建议文件名的新建路径、选择目录、节点类型选择、确认、外部文件变更冲突选择和消息对话框。
- `SettingsWindow`：元数据路径、目录浏览、重新导入和当前文档 Context 的配置入口。
- `TaskSelectionWindow`：候选任务搜索、类型/标签/注释展示和节点类型确认入口。
- `ProjectFileIndexService.StartAsync`：后台扫描合法目录并持续发布可编辑文件快照。
- `ExplorerModFilters`、`ExplorerFileTypeFilters`、`ExplorerSearchText`：按模组、文件类型和文件名过滤 Explorer 快照。
- `DesignPlanIndexService.Scan`、`LoadContent`、`ResolveAssociatedDocumentPath`、`DesignPlanDates`：仅扫描当前游戏项目 `AutoDoc/DesignPlan` 下合法 `YYYY.MM.DD` 日期目录的直接 `.md` 子文件，解析并从展示正文移除严格的 `title/state/priority` 基础头与可选 `plan/review` 关联头，按游戏工程根目录解析主端 `AutoDoc/...` 关联路径，并生成默认展开的日期树。
- `DesignPlanSearchText`、`DesignPlanSearchService`：按 title 优先、文件名次之完成 literal-first 搜索，并为共享向量语料提供 title 语义名与文件名兜底。
- `SelectedDesignPlanFile`、`OpenDesignPlanCommand`：将单击选择路由到预览页签，将双击或 Open 路由到固定只读页签。
- `MainViewModel.OpenAssociatedDesignPlan`：接收解析后的 Plan/Review 本地路径及 `Plan: <策划案名>` 或 `Review: <策划案名>` 页签标题，绕过日期索引并通过非 preview 打开链路创建或切换到固定只读页签。
- `DesignPlanIndexService.FindLinkedDocument`、`MainViewModel.TryOpenDesignPlanLink`：按本地文件 URI 在完整策划案索引中定位 Markdown 链接目标，并以固定页签打开。
- `DesignPlanDirectoryWatch`：监听固定策划案目录中的新增、修改、删除和重命名，防抖后重建树、搜索候选与共享向量语料。
- `MarkdownRenderService`：以 Markdig CommonMark/GFM 与高级扩展生成完整 HTML，并把相对图片链接按 Markdown 文件目录解析为本地 URI。
- `OpenDocumentFileWatch`：为每个已有磁盘文件的打开页签监听创建、写入、删除与重命名，并生成内容指纹用于去重。
- `DocumentViewModel.OnPinned`、`Dispose`：页签从预览转为固化时启动页签级功能，并在关闭、替换或退出时释放对应运行时资源。
- `ApplicationLog.Entries`、`SummaryText`：按 Info、Warning、Error 保存进程内日志，并生成固定单行的 `logs, warnings, errors` 计数。
- `LogDetailsWindow`：点击主窗口底部计数条后打开的非模态日志明细窗口；显示时间、级别、消息和来源文件。

## 3. 调用链路

`App` 创建 `MainWindow`，窗口构造 `MainViewModel` 并绑定命令。构造阶段只加载轻量设置与最近文件，不启动目录扫描或向量模型。`MainWindow.OnContentRendered` 在首帧显示后调用 `MainViewModel.InitializeAsync`；初始化在 Dispatcher 空闲阶段加载 Task/CSV/BbxScriptableObject 元数据，等待 Explorer 首轮索引完成，最后才启动独立向量 worker。窗口快速关闭时通过 `_disposed` 状态停止后续阶段，避免初始化任务在工作区释放后继续运行。

`WorkspaceDocumentService` 根据 `.editor.json`、`.csv` 或 `.asset` 分发打开与保存；`AddDocument` 创建对应 ViewModel，WPF DataTemplate 根据当前页签自动选择编辑器。Design Plan 不进入可编辑文件分发器：工作区初始化或 `GameProjectPath` 变化时扫描固定日期目录并安装目录 watcher；选中文档后读取 UTF-8 Markdown，以 title 作为页签标题，从浏览正文移除 `title/state/priority` 及可选 `plan/review` 文件头，把解析后的绝对关联路径写入只读 `DesignPlanDocument`，并创建不可保存的 `DesignPlanDocumentViewModel`。标签切换同步 Task Context、Task Inspector 或数据文档辅助信息。

主菜单“配置”打开 `SettingsWindow`；路径字段写入 `SettingsService`，浏览目录后立即重新加载元数据，Context 下拉框写入当前文档。配置窗口关闭不影响已加载文档。

主窗口不常驻显示任务目录。Timeline 或行为树发起添加操作时，DocumentViewModel 调用 `MainViewModel.SelectTask` 提供场景筛选谓词；MainViewModel 从 TaskCatalog 生成候选集，再由 `IDialogService.SelectTask` 打开 `TaskSelectionWindow`。用户确认后返回单个 `TaskDefinition`，调用方才创建任务实例或节点；取消选择不修改文档。

保存命令调用 `WorkspaceDocumentService.Save`，再由具体 codec 保存；关闭脏文档前通过 `IDialogService.Confirm` 获取确认。普通状态通过 `MainViewModel.SetStatus` 写成 Info 日志，结构化 `Diagnostic` 保持各自的 Warning/Error 级别写入 `ApplicationLog`。主窗口底部只绑定单行计数，不再呈现或换行展开消息正文；点击计数打开共享同一日志集合的 `LogDetailsWindow`，新日志会继续更新到已打开窗口中。

Explorer 单击文件时立即打开一个可替换的预览页签；用户点击中央编辑区或页签后将其固定。双击 Explorer 文件条目或点击其 Open 按钮会把已有预览立即固化，尚未打开时则直接创建固定页签；双击列表空白区或滚动条不执行打开。切换到其他 Explorer 文件只替换仍未固定且未修改的预览页签，文件菜单显式打开和最近文件也始终使用固定页签。

Explorer 左侧分为 Files 与 Design Plan 两个页签。Design Plan 只接受真实有效的 `YYYY.MM.DD` 日期目录并按日期倒序显示；同日文档先按 Todo、In Progress、In Design、Warning、Completed 排序，相同状态再按 P0、P1、P2 排序，完全同级时以标题和文件名稳定排序。旧 `YYYY.MM` 目录、无效日期、子目录中的 Markdown 和非 `.md` 文件不会进入树。单击创建共享预览，双击文档子节点或 Open 固定页签；条目 hover 使用与 Files 一致的深色高亮，不显示路径 tooltip。state 和 priority 在 Explorer 条目及中心页签以彩色徽标呈现：In Design 灰、Todo 蓝、In Progress 浅棕、Warning 黄、Completed 绿，P0 红、P1 橙、P2 黄。只读策划案不启用 Save/Save As，也不写入最近文件。

Design Plan 搜索框为空时使用日期分组以及状态、优先级排序。输入搜索词后切换为不带日期分组的扁平结果列表，先按 title 精确、文件名精确、title 包含、文件名包含排列 literal 候选，再按向量相关度补充最多 5 个未被 literal 命中的候选；原有日期、状态与优先级排序在搜索期间不参与结果顺序，清空搜索框后恢复。向量语义名优先使用 title；缺失合法 title 时，索引器回退的文件名成为语义名。Files、Task 和 Design Plan 的名称每次共同提交给 `VectorSearchCoordinator.SynchronizeNames`，所以目录 watcher 发现新增时建立缺失向量，删除或改名时移除不再属于任一来源的缓存项。

`DesignPlanViewerControl` 在 Markdown 浏览器上方按关联字段条件显示暗蓝色 `Open Plan` 与 `Open Review` 按钮；点击后用当前策划案 title 生成 `Plan: <策划案名>` 或 `Review: <策划案名>`，再调用 `OpenAssociatedDesignPlan`。不属于日期索引的 Plan/Review Markdown 也通过 `OpenDesignPlan(..., preview: false)` 创建固定只读页签；`TabTitleOverride` 在文件 watcher 重载时保留关联页签名，已打开目标只切换页签，预览目标先固定，失效路径或读取失败通过工作区状态记录。浏览器将 Markdown 交给 `MarkdownRenderService`，支持标题、列表、任务项、表格、引用、代码、脚注、删除线、自动链接和内嵌 HTML 等 Markdig 扩展。标准图片语法中的相对路径以策划案文件所在目录为基准解析，因此可访问主端 `AutoDoc/media/design-plan`；绝对、`file:`、`http:`、`https:` 与数据 URI 保持原义。由于 `NavigateToString` 页面会阻止 `file:` 链接发出导航事件，渲染器将本地 Markdown 链接包装为内部 HTTPS 导航 URI；点击后由浏览视图还原真实本地 URI。若路径存在于完整策划案索引中，则通过工作区固定打开对应只读页签；普通正文中未索引的本地文件仍交给系统默认程序，网络链接保持原地址并交给系统默认程序。

创建或显式打开的文档在加入工作区时直接调用 `OnPinned`；预览文档只在用户固化后调用。行为树利用该入口启动页签级临时节点向量索引。关闭文档、替换预览、外部重载替换旧 ViewModel 或应用退出时统一调用 `Dispose`，确保页签级取消令牌和内存索引不会越过文档生命周期。

`AddDocument` 为磁盘上已存在的文件创建独立 `OpenDocumentFileWatch`，350ms 防抖后比较 SHA-256 内容指纹。普通可编辑文档的外部写入由 `WorkspaceDocumentService.Open` 重新解析；只读 Design Plan 则重新解析文件头与 Markdown 正文。两者都在原索引位置替换 ViewModel，保持当前选择与预览状态。可编辑文档有本地修改时通过 `ExternalFileChangeWindow` 选择 Reload 或 Keep Local；只读策划案直接重载。文件被外部删除时不关闭页签，只保留内存内容并提示；编辑器自身保存后立即重建 watcher 和指纹，不把本次保存识别为外部变更。关闭、替换页签或退出时释放 watcher。

文档成功打开或保存后，`MainViewModel` 调用 `AppSettings.RecordRecentDocument` 将规范化完整路径移到列表首位，按 Windows 路径大小写不敏感去重并保留最多 10 条，再由 `SettingsService` 持久化。文件菜单“打开最近”直接调用统一的打开链路；若用户点击的路径已不存在，工作区显示提示、从最近列表移除该项并保存设置。启动时不主动删除失效路径，以免临时离线磁盘导致记录丢失。

## 4. 数据来源

- BbxEditor 程序目录下的 `settings.json`，包含游戏项目目录、元数据目录、最后文档路径和最多 10 条最近文档路径。
- Unity 导出的元数据目录。
- 用户打开的旧 `.editor.json`、CSV 和已识别的 BbxScriptableObject `.asset`。
- `ObservableCollection<DocumentViewModel>` 中的进程内工作区状态。
- 打开文件当前磁盘内容的长度与 SHA-256 指纹，仅保存在对应页签的运行时 watcher 状态中。
- Explorer 的异步索引快照和本地 `vector-index.json` 向量缓存。
- 当前游戏项目 `AutoDoc/DesignPlan/YYYY.MM.DD/*.md` 的严格 `title/state/priority` 基础头、可选 `plan/review` 关联头、正文、目录变更事件，以及正文直接引用的本地或远程图片；关联头中的主端 `AutoDoc/...` 路径以当前游戏工程根目录为基准。
- `ApplicationLog` 的进程内日志条目；日志在本次应用运行期间累计，不持久化到磁盘。
- 固化行为树页签持有的临时节点搜索向量；预览页签不创建，关闭后释放，不持久化。

## 5. 与其他模块的依赖

本模块依赖 Core 的 `TaskCatalog`、`Diagnostic`、领域文档、`DocumentFileService` 和 `DesignPlanIndexService`，依赖 WPF 的命令、数据绑定、Dispatcher、FileSystemWatcher、只读 `WebBrowser`、系统默认文件关联和系统对话框；Markdown 到 HTML 的转换依赖 Markdig。Plan/Review 按钮复用工作区 Design Plan 页签与文件 watcher，不依赖 Explorer 日期索引；普通正文中的未索引链接仍依赖系统默认文件关联。Timeline、行为树和 Inspector 依赖工作区提供当前文档、任务定义和 Context，但彼此不直接依赖。`LogDetailsWindow` 只依赖 `ApplicationLog` 的可观察集合，不接管业务诊断的生成。

`ProjectFileIndexService.StartAsync` 接收游戏工程根目录、`AppSettings.ExplorerDirectories` 和文件分类函数。当前配置只包含 `Assets/Resources` 与 `Mods`；索引器在这些根目录下分别使用 `*.editor.json`、`*.csv`、`*.asset` 搜索模式，不再先枚举图片、音频、`.meta` 等无关文件。候选文件在后台交给 `WorkspaceDocumentService.Open` 做真实解析，只有可打开的 Task、CSV 和 BbxScriptableObject 才进入 `ExplorerFiles`。

索引服务为合法目录安装 FileSystemWatcher，并以 250ms 防抖处理创建、修改、删除和重命名事件。快照事件可能从后台线程发出，`MainViewModel` 必须通过 WPF Dispatcher 更新 ObservableCollection；窗口关闭时释放索引服务和 watcher。

`IndexedProjectFile.ModName` 按相对路径计算：Resources 与 Mods/Native 为 `Native`，其他 Mods 一级子目录名为模组名。Explorer 过滤只改变呈现集合，不重复扫描磁盘。向量搜索依赖独立 worker 进程和共享模型目录；工作区把规范化 Files 文件名、Task 类型名和 Design Plan title 合并为同一语料集合。`VectorSearchCoordinator.SynchronizeNames` 对比 `vector-index.json`：已有名称跳过，新增名称分批 embedding，不再属于任一来源的名称删除。Files、Design Plan 和 Task 节点选择共用该向量空间、重中心化语料与 worker，退出时同步释放进程。

`PortablePath` 负责相对路径的保存和解析。`MainViewModel` 以 `settings.json` 所在目录为固定基准解析 `gameProjectPath` 与 `metadataPath`，目录选择器返回的绝对路径也会转换回相对配置。主窗口将 ExplorerControl 放在加宽的左栏、文档 TabControl 放在可伸缩中央栏、Inspector 放在右栏；`ExplorerSearchText` 变化时只按 `IndexedProjectFile.FileName` 过滤当前快照，不触发磁盘重扫。

Design Plan 扫描与目录 watcher 独立于 Files 的可编辑文件索引和 Mod/类型过滤，但两者共享向量协调器和打开文档 watcher 基础设施。它只以解析后的 `gameProjectPath` 为根定位固定 `AutoDoc/DesignPlan`，不会读取 BbxEditor 自身文档、其他游戏目录、`AutoDoc/Design`、`AutoDoc/Program` 或 `AutoDoc/Temp`。
