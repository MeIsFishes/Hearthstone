# 同步引擎

## 1. 模块说明

同步引擎位于 `Core/` 与 `Services/`，负责项目识别、路径映射、黑名单求值、Preview、目标预检、覆盖复制、取消和结果日志。它不依赖 WPF 类型，可由测试项目直接调用。

同步采用 overlay 语义：创建缺失目录，覆盖同名文件，保留目标独有文件。每个目标独立执行；单个目标失败后继续处理其他目标。文件先复制到目标同目录临时文件，再以单文件替换完成覆盖，不创建长期备份。

Preview 会统计每个目标已存在和缺失的配置目录，并以主项目文件为准比较目标文件：

- 缺失目录数大于存在目录数时为 `NewProject`。
- 其余目标只要存在缺失文件、主项目文件较新，或修改时间相同但大小不同，即为 `WaitForSync`。
- 全部路径存在，且文件大小与修改时间均一致时为 `Synchronized`。
- 任一目标文件修改时间晚于主项目对应文件时为 `Warning`，该状态优先，并记录风险文件的相对路径和双方 UTC 修改时间。

空目标仓库可作为 `NewProject`。目标游戏目录必须是现有仓库根的直接子目录。Preview 先判定全部目标状态，仅在至少一个目标为 `NewProject` 后发现本机 Unity Editor；安全空目标默认使用主项目版本，并把可选版本返回 UI。Sync Now 委托所选 Editor 生成完整工程，再覆盖主项目的 `Packages/manifest.json` 和可选 `Packages/packages-lock.json`。未发现 Editor 时保留兼容引导：创建 `Assets`、`Packages`、`ProjectSettings` 并 create-only 复制主项目设置。

内置同步项：

| 名称 | 源与目标相对路径 | 特殊规则 |
|---|---|---|
| `Shared Tools` | `RepositoryRoot/Tools` | 默认读取仓库与内部 `.gitignore`；共享 `BbxDeployer.sync-items.json`；精确豁免并同步 Git 忽略的 `BbxDeployer.projects.json`，但仍排除其原子保存临时文件 |
| `BbxCommon Source` | `UnityProjectRoot/Assets/Scripts/BbxCommon`、`UnityProjectRoot/AutoDoc/UIItem` | 同步 BbxCommon 源码及共用 UIItem 文档，不额外同步白名单根同级 `.meta` |
| `Odin Inspector` | `UnityProjectRoot/Assets/Plugins/Sirenix` | 不额外同步白名单根同级 `.meta` |
| `Codex Project Configuration` | `UnityProjectRoot/.codex`、`UnityProjectRoot/AGENTS.md`、`UnityProjectRoot/AutoDoc/CleanupTempDocs.bat` | 同步 Codex 配置、代理入口及 Skill 所依赖的临时文档清理脚本 |

每个同步项是一个路径组，`WhitelistPaths` 可保存多个 `SyncPathEntry`。Entry 可以指向目录或单文件；目录 Entry 是独立白名单根，并保存自己的 `ManualExcludePatterns`。界面通过文件夹选择器加入该根内部任意深度的多个黑名单路径，因此一个白名单的黑名单不会误作用到同组其他白名单。手工黑名单优先于 `.gitignore` 的重新纳入规则。Planner 始终为每条白名单加载仓库根到白名单之间以及白名单内部的 `.gitignore`，不提供关闭开关。

旧配置中的 `SourceBase`、`SourceRelativePath`、`TargetBase`、`TargetRelativePath` 和同步项级 `ManualExcludePatterns` 继续保留。没有 `WhitelistPaths` 时，`SyncItemPathExpander` 会把这些字段转换成唯一的有效 Entry；保存新的分组配置时也回填第一条 Entry，避免破坏配置兼容性。旧 `AdditionalIgnoreFiles` 字段可被读取，但 Planner 会清空并忽略，避免已删除的导入功能继续隐式生效。

设置列表中的全部同步项始终参与 Preview 和 Sync。`Enabled` 字段仅为兼容已有配置保留；设置加载、保存和 Planner 创建快照时都会将其规范为 `true`，界面不提供停用目录的入口。

设置界面统一使用相对项目根的目录模板。普通路径（如 `Tools`）以 `RepositoryRoot` 为基准；以 `{GameProject}/` 开头的路径（如 `{GameProject}/Assets/Scripts/BbxCommon`）以每个项目自己的 `UnityProjectRoot` 为基准。`SyncPathTemplate` 在界面模板与内部 `PathBaseKind` 间转换，因此不同游戏主体名称不需要出现在同步配置中。

规则匹配支持普通排除、顺序 `!` 重新纳入、`/` 根锚定、目录结尾 `/`、`*`、`?`、字符范围、`**` 与转义前缀。规则按来源目录解析。白名单目录内部已有的 Unity `.meta` 仍作为普通文件并使用对应资产路径求值，使被排除的资产不会留下孤立 `.meta`；不会再额外加入白名单根同级的 companion `.meta`。

Preview 不再预先遍历整棵目录树查找 `.gitignore`。祖先规则先建立扫描会话，嵌套 `.gitignore` 在同一次文件遍历进入对应目录时加载；规则变化后更新已编译匹配集合。单文件 Entry 只加载其父目录之前的规则并直接加入清单，不枚举父目录。目录枚举直接复用 `FileSystemInfo` 返回的属性判断文件、目录与重解析点，不再为每个条目额外调用 `File.GetAttributes`。如果目录已被当前规则或手工黑名单排除，就不再进入，因此 `.venv`、`dist`、构建产物等大型目录不会造成无效扫描。

BbxCommon 同步会预检：

- 源与目标 Unity 主版本；
- `com.unity.entities` `1.0.0-pre.65`；
- `com.unity.textmeshpro` `3.0.6`；
- `com.unity.ugui` `1.0.0`；
- Odin Inspector 同步项或目标现有安装。

普通同步项不包含 `Packages/manifest.json`；仅新工程创建完成后，使用主项目 manifest 和 lock 文件覆盖 Unity 生成的对应文件。`Assets/Resources/BbxCommon`、资源索引、Library、Temp、IDE 工程文件等目标生成内容不加入内置同步项。

## 2. 对外接口

- `ProjectLocator.IsUnityProject(path)`：根据 `Assets`、manifest 和 ProjectVersion 判断 Unity 根。
- `ProjectLocator.DiscoverUnityProjects(repositoryRoot)`：发现仓库直接子目录中的 Unity 项目，不依赖游戏名称。
- `ProjectLocator.CreateContextFromProjectRoot(path)`：从项目根或其 Unity 游戏目录生成完整项目上下文。
- `ProjectLocator.CreateDestinationContextFromProjectRoot(path)`：从有效工程根生成目标上下文，或为无 Unity 工程的空仓库生成可编辑的引导上下文。
- `ProjectLocator.CanBootstrapUnityProject(context)`：确认目标游戏目录是现有仓库根的直接子目录。
- `ProjectLocator.CanCreateUnityProject(context)`：确认目标游戏目录缺失或为空，可交给 Unity 创建。
- `ProjectLocator.ReadUnityVersion(path)`：读取工程的 Editor 版本。
- `ProjectLocator.CreateDefaultSyncItems()`：创建 Shared Tools、BbxCommon、Odin 和 Codex 四个内置同步项。
- `SyncPathTemplate.ToProjectRelativePath/ApplyProjectRelativePath`：转换 `{GameProject}` 相对目录模板。
- `PathService.ResolveInside(basePath, relativePath)`：规范化并阻止相对路径逃出基准目录。
- `IgnoreRuleLoader.Load(source, item, whitelistRoot, token)`：加载手工与 `.gitignore` 规则及规则文件快照。
- `IgnoreRuleLoader.BeginScan(...)` / `IgnoreRuleScanSession.EnterDirectory(...)`：在文件遍历中增量加载嵌套 `.gitignore`，避免独立预扫描。
- `PathInclusionEvaluator.IsIncluded(path, rules)`：计算文件最终纳入状态。
- `SyncPlanner.CreatePreviewAsync(...)`：创建不可变执行清单、排除统计、路径完整度、文件时间比较和逐目标预检结果。
- `SyncExecutor.ExecuteAsync(preview, progress, token)`：在后台复核快照、报告分阶段进度并执行多目标覆盖同步。
- `SettingsRepository.LoadAsync/SaveAsync`：合并读取并分别原子保存可执行文件同目录的共享同步项配置与本机项目列表；内容未变化时跳过写入以保持快照时间戳，首次遇到旧 `BbxDeployer.settings.json` 时自动拆分迁移。

主要数据模型：

- `ProjectContext`：仓库根、Unity 项目根、显示名称和新建工程所选 Unity 版本。
- `SyncItem`：一个始终纳入同步的路径组，保存多个白名单 Entry、兼容单路径字段和 `.gitignore` 设置；旧 companion `.meta` 字段仅保留配置兼容性，不参与规划。
- `SyncPathEntry`：一条项目根相对白名单目录或文件路径；目录可带独立手工黑名单集合。
- `IgnoreRule`：规则文本、类型、来源目录、顺序与反向标记。
- `SyncPreview`：源与规则快照、最终文件清单、排除统计、目标统计、惰性发现的 Unity Editor 列表、警告和错误。
- `PreviewProgress`：文件扫描/比较阶段、已完成文件数、总文件数和分阶段确定百分比；显式百分比用于保证整个 Preview 单调前进。
- `TargetPreview`：目标路径计数、待同步文件数、`TargetSyncStatus`、Unity 创建/兼容引导信息和较新目标文件明细。
- `SyncResult`：逐目标复制量、失败/取消状态和日志路径。

## 3. 调用链路

1. UI 从 `BbxDeployer.projects.json` 读取 Main Project 和目标 `ProjectContext`，从 `BbxDeployer.sync-items.json` 读取全部路径组及其白名单模板；旧合并配置在加载时自动拆分。Preview 在建立文件快照前先持久化待保存变更，后续相同内容不会再次改写时间戳。
2. `SyncItemPathExpander` 将每个组展开成逐白名单的启用快照，并把该 Entry 的黑名单附加到对应快照；旧单路径配置在此自动适配。
3. `SyncPlanner` 验证源、目标数量和 Unity 根。
4. 每个展开项解析白名单根，`IgnoreRuleLoader.BeginScan` 加载该白名单的手工规则以及仓库和嵌套 `.gitignore`；外部导入规则不进入快照。内置 Shared Tools 对精确路径 `Tools/BbxDeployer/BbxDeployer.projects.json` 只绕过 Git-ignore 规则，使同机项目共享列表；手工黑名单仍可排除它，其他文件不获得豁免。
5. 目录 Entry 由 `FileTreeEnumerator` 使用一次目录枚举读取条目和属性；进入目录时扫描会话加载该层 `.gitignore`，排除目录立即停止递归。单文件 Entry 直接生成一个文件快照。`PathInclusionEvaluator` 对两种 Entry 都使用当前已编译规则计算纳入状态。
6. Planner 阻止多个白名单或同步项写入同一目标路径，不额外生成 companion `.meta` 清单。
7. 源文件扫描阶段按已完成白名单及当前文件计数推进 0–40%；收集完成后，以“纳入文件数 × 目标数”作为比较总量，在 40–95% 内报告精确文件进度，并生成目标状态与风险清单。
8. 仅当至少一个目标为 `NewProject` 时调用 Editor 发现器；发现器读取 Unity Hub 的 `secondaryInstallPath.json` 以及标准安装目录。安全空目标按已保存版本、主项目版本、版本列表首项的顺序选择默认 Editor，并生成 Unity 创建计划。
9. `ProjectValidator` 检查目标重复/重叠、Unity 与包依赖、Odin 和磁盘空间；缺失且将由引导创建的版本和 manifest 不作为错误。
10. UI 可在 New Project 卡片切换 Editor，并直接更新当前 Preview 的创建路径与根配置。
11. Preview 展示后，`SyncExecutor` 在线程池中再次检查全部源文件、Unity 引导源文件以及 Preview 已记录的 `.gitignore` 文件大小和时间戳，避免阻塞 WPF 界面或再次遍历整个目录树；该阶段按校验项数量持续报告 0–10%。
12. Executor 先调用 Unity 创建工程并覆盖包配置，或执行兼容引导，再逐目标执行普通覆盖同步；Unity 创建失败时该目标不继续复制，且不终止其他目标。
13. Preview 比较按完成文件数计算百分比。Sync 的文件复制阶段按完成文件数映射到 10–100%；Unity 创建期间报告不确定活动状态，成功后固定收尾至 100%。取消会停止下一个文件，已完成文件不回滚，结果标明 partial changes；确认框取消则不启动同步、不修改文件。
14. 执行结果写入 `%LocalAppData%/BbxDeployer/logs/`。

## 4. 数据来源

- 源与目标文件系统。
- 主项目 `ProjectSettings`、`Packages/manifest.json` 与可选 `Packages/packages-lock.json`。
- Unity Hub 的辅助安装目录设置与本机默认 Editor 安装目录。
- `Packages/manifest.json`，只读依赖版本。
- `ProjectSettings/ProjectVersion.txt`，只读 Unity 版本。
- 仓库及白名单内部自动发现的 `.gitignore`。
- 可执行文件同目录的 `BbxDeployer.sync-items.json`，保存可提交并可同步的 Transfer Directories。
- 可执行文件同目录的 `BbxDeployer.projects.json`，保存每台电脑不同的 Main Project、Targets 和绝对路径；Git 仍忽略该文件，避免跨电脑提交，但 Deployer 的内置 Shared Tools 对最终文件设有精确豁免，使同一电脑的项目副本保持一致。原子临时文件不豁免。
- 兼容读取的旧 `BbxDeployer.settings.json`；迁移成功后由两个新文件替代。
- Preview 中记录的文件与规则快照。

测试项目 `Tests/BbxDeployer.Tests.csproj` 在系统临时目录创建模拟仓库。`Tests/Fixtures/PreviewStatusFixture.cs` 使用固定 UTC 时间生成一个主工程与四种目标工程；其他测试覆盖 Preview 多目标文件总数、Sync 文件百分比、Unity 惰性发现、主项目版本默认值、卡片切换、Editor 创建、兼容引导、嵌套规则、overlay、快照失效、设置持久化、依赖阻断和路径越界。

## 5. 与其他模块的依赖

- 桌面 UI 只通过本模块进行文件读取、Preview 和写入。
- 使用 .NET 文件系统、正则表达式、JSON、异步流与取消 API，无第三方运行库。
- `ProjectValidator` 依赖当前 BbxCommon 的 Unity/UPM 基线。
- `SyncExecutor` 只接受无阻断错误的 `SyncPreview`，不直接读取可变 UI 状态。
- 新目标的工程创建依赖 Unity 工程创建模块；普通同步和已有项目不启动 Unity。
