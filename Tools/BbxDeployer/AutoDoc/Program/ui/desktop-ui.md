# 英文桌面界面

## 1. 模块说明

桌面界面使用 .NET 8 WPF，入口为 `Views/MainWindow.xaml`，状态与命令位于 `ViewModels/MainViewModel.cs`。所有用户可见文案为英文。

主窗口只保留一个 `Projects` 大面板：

- Main Project 与其他项目连续显示为同一组卡片，不再使用独立分区标题或目标操作栏。Main Project 只通过蓝色卡片、`MAIN PROJECT` 标签和 `Source` 标识区分。
- 每张目标卡右侧提供 `Select as Source`；卡片内的 `Project Details` 展开区显示游戏目录并提供自身的修改与移除操作。
- 面板内部下方显示 Preview 与同步摘要、进度和可折叠 Details。
- 左下角固定为 `Settings`；右下角固定为 `Preview` 和 `Sync Now`。
- 每个目标项目显示有色 Preview 状态：蓝色 `New Project`、橙色 `Wait for Sync`、绿色 `Synchronized`、红色 `Warning`。需要创建 Unity 工程的 `New Project` 卡片会在 Preview 后显示本机版本下拉框；Warning 卡片可展开目标较新的风险文件及双方修改时间。

`SettingsDialog` 管理 `Transfer Directories` 列表。列表中的每个配置组始终参与同步，不提供单独的 Include 开关。每个组可包含多个白名单路径，每条目录白名单分别维护多条手工黑名单规则；内置配置也可包含 `AGENTS.md` 等单文件白名单。`{GameProject}` 代表每个项目名称不同的 Unity 游戏目录。例如 `Tools` 指向项目根下的 Tools，而 `{GameProject}/.codex` 会分别解析到 Main Project 和各目标自己的游戏目录。

## 2. 对外接口

主窗口命令：

- `Add Project...`：添加项目根并自动发现其中唯一的 Unity 游戏目录；空仓库会打开目标编辑框，默认使用“仓库根/仓库名”作为新游戏目录。此时不检测 Unity 安装。尚无 Main Project 时，只有有效 Unity 工程可以成为来源。
- `Select as Source`：把对应目标提升为 Main Project，并把原 Main Project 放回目标列表。
- 目标卡 `Project Details > Change...`：重新选择该卡片对应的项目根。
- 目标卡 `Project Details > Remove from List`：只从同步列表移除该卡片，不删除磁盘文件。
- `Settings`：打开相对转移目录配置。
- `Preview`：异步扫描源路径，再以 Main Project 为准比较文件是否缺失、大小是否相同及最后修改时间。全程使用从 0 到 100 单调前进的确定进度，不再显示不确定滚动条。
- `Sync Now`：Preview 无阻断错误后启用；确认框绑定主窗口。取消确认会显示 `Sync cancelled` 和“未修改文件”的明确信息，并保留当前 Preview；确认后，新目标先由所选 Unity Editor 创建，再覆盖 Main Project 的 package manifest/lock 和普通同步目录。

设置窗口命令：

- `Add Directory...`：添加一个转移目录组，再通过文件夹选择器加入一个或多个白名单路径。
- `Edit...`：对所有组统一开放；同一窗口维护名称、多个白名单路径和每条白名单自己的多个黑名单路径。白名单区和黑名单区都只提供 `Add Path...` 与 `Remove`，按钮之间保留固定间距。
- `Remove`：删除当前配置组，包括原来的内置组；不删除磁盘文件。

## 3. 调用链路

1. 窗口分别加载共享同步项配置和本机项目列表；没有本机项目列表时从程序位置推断 Main Project 根。旧合并配置会自动拆分迁移。
2. 项目选择通过 `ProjectLocator.CreateContextFromProjectRoot` 发现内部 Unity 游戏目录。
3. 每张目标卡的详情命令以该卡片自身作为参数，不依赖列表当前选中项；执行 `Select as Source` 时，目标与当前 Main Project 交换角色。
4. Settings 通过 `SyncPathTemplate` 把文件夹选择结果转换为相对项目根路径；当前选中的白名单决定下方正在编辑的黑名单集合。旧配置的单路径及排除规则自动作为第一条白名单读取。保存设置和生成 Preview 时都会把所有配置组规范为启用状态。
5. 项目角色、转移目录或排除规则变化会使旧 Preview 失效，目标状态显示 `Preview Required`。
6. 单击 `Preview` 后先完成所有项目状态判定；没有 `New Project` 时不调用 Unity Editor 发现器。
7. 至少存在一个 `New Project` 时才读取 Hub 安装信息。需要创建工程的卡片显示版本下拉框，默认匹配 Main Project 的版本；切换后当前同步计划和本机项目列表立即更新，不要求重新 Preview。
8. 其余目标按文件时间和大小显示 `Wait for Sync` 或 `Synchronized`；目标任一文件较新时 `Warning` 优先，并通过卡片内 Expander 暴露风险文件。
9. Preview 无阻断错误时启用 `Sync Now`；确认框以主窗口为 Owner，避免隐藏到其他窗口后。取消时界面明确显示同步未开始且没有文件被修改；确认后先使用所选 Editor 创建工程并覆盖 package 配置，再开始普通覆盖同步。没有可选版本时可使用兼容 create-only 引导。
10. Preview 将源路径扫描映射到 0–40%，把“纳入文件数 × 目标数”的精确比较映射到 40–95%，Unity Editor 检测和收尾使用 95–100%；各阶段不回退。Sync Now 把后台快照复核映射到 0–10%，文件复制映射到 10–100%；Unity 外部进程创建工程期间显示不确定活动状态，完成后恢复确定进度。同步成功固定收尾至 100%；取消或失败保留当前进度，并立即用摘要和 Details 显示明确结果，不再保留 `Sync in progress` 文案。
11. Preview、版本切换、同步或窗口关闭时分别保存同步项和本机项目列表；项目绝对路径不会写入共享同步项文件。

## 4. 数据来源

- `SettingsRepository` 合并程序根目录 `BbxDeployer.sync-items.json` 中的转移目录与 `BbxDeployer.projects.json` 中的 Main Project、目标项目；后者不提交 Git，但由 Deployer 在同一电脑的项目副本之间同步。
- `ProjectLocator` 返回的项目根与 Unity 游戏目录。
- `UnityEditorLocator` 从 Hub 设置和标准安装目录返回的可用 Unity 版本。
- `SyncPreview` 返回的文件、排除、新增/覆盖、路径完整度、项目状态、风险文件、警告和错误。
- `PreviewProgress` 返回计数/比较阶段、已完成文件数、总文件数和百分比。
- `SyncProgress` 返回的当前目标、目录、文件、阶段进度和是否处于不确定活动状态。
- `SyncResult` 返回的逐目标状态和日志路径。

界面不直接枚举或复制同步文件。项目根、白名单目录和黑名单目录使用 Windows 原生文件夹选择对话框；不提供额外 `.gitignore` 导入入口。

## 5. 与其他模块的依赖

- 依赖同步引擎、设置仓库、`SyncPathTemplate` 和对话框服务。
- `ObservableObject`、`RelayCommand` 与 `AsyncRelayCommand` 提供 MVVM 通知和命令。
- WPF View 不包含同步规则；界面只管理项目根和相对目录模板。
