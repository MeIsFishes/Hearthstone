# Unity 工程创建

## 1. 模块说明

本模块负责发现 Unity Hub 管理的本机 Editor，并为安全的空目标目录创建完整 Unity 工程。Unity Hub CLI 没有稳定的创建工程接口，因此真正的创建动作使用所选 Editor 官方支持的 `Unity.exe -createProject`。模块只处理新工程；已有有效 Unity 工程不会启动 Editor。

## 2. 对外接口

- `IUnityEditorLocator.DiscoverInstalledEditors()`：可测试的 Editor 发现接口。
- `UnityEditorLocator.DiscoverInstalledEditors()`：读取 Hub 的 `secondaryInstallPath.json`，并扫描系统标准 `Unity/Hub/Editor` 目录，按版本降序返回去重结果。
- `UnityEditorLocator.FindByVersion(version)`：把配置中的版本解析为当前可用安装。
- `IUnityProjectCreator.CreateAsync(editor, unityProjectRoot, token)`：可测试的异步创建接口。
- `UnityProjectCreator.CreateAsync(...)`：使用 `-batchmode -quit -createProject <path> -logFile <path>` 启动 Unity，并验证结果。

## 3. 调用链路

1. Preview 先完成全部目标状态判定；没有 `NewProject` 时不调用 `IUnityEditorLocator`。
2. 至少存在一个 `NewProject` 时才取得本机 Editor 列表。安全空目录默认选择主项目 `ProjectVersion.txt` 中的版本，不可用时选择列表中的首个版本。
3. 需要创建工程的目标卡片显示版本下拉框；所选版本写入 `ProjectContext.UnityEditorVersion`、当前 `TargetPreview` 和本机 `BbxDeployer.projects.json`。
4. Preview 只记录可执行路径和 manifest/lock 覆盖清单，不启动 Unity 或创建目录。
5. Sync Now 调用 `IUnityProjectCreator`；Unity 成功退出且生成有效项目后，执行器原子覆盖主项目的 `Packages/manifest.json` 与可选 `packages-lock.json`。
6. 创建失败时记录 Unity 日志路径和末尾信息，该目标停止，其他目标继续。

## 4. 数据来源

- `%AppData%/UnityHub/secondaryInstallPath.json`。
- `%ProgramFiles%/Unity/Hub/Editor` 与 `%ProgramFiles(x86)%/Unity/Hub/Editor`。
- 各版本目录下的 `Editor/Unity.exe`。
- 主项目 `ProjectSettings/ProjectVersion.txt`、`Packages/manifest.json` 和可选 `packages-lock.json`。
- Unity 创建日志：`%TEMP%/BbxDeployer/UnityLogs/`。

## 5. 与其他模块的依赖

- `SyncPlanner` 仅在检测到 `NewProject` 后使用 `IUnityEditorLocator`，并把结果随 Preview 返回。
- UI 在对应项目卡片填充版本下拉框，并把切换结果写回当前创建计划。
- `SyncExecutor` 通过 `IUnityProjectCreator` 创建工程，接口可在测试中替换为假实现。
- `ProjectLocator` 提供安全路径、空目录和最终 Unity 工程有效性检查。
