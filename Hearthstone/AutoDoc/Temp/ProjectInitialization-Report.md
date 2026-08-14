# 项目初始化报告

## 任务结果

项目已完成业务代码与配置骨架初始化，但整体初始化尚未完成。Unity Editor 资产创建、目标版本编译和 PlayMode 验证被当前已打开的 Unity Package Manager 错误窗口阻塞，未把这些项目写成已完成。

初始化前人工分类为“只有 BbxCommon 框架的空项目”：Unity 根标记完整，业务脚本与 Scene 均为 0，无 GameEngine、GameStage、ECS 或 MVC/UI 业务链路。项目身份固定为：

- 项目名：`Hearthstone`
- 根 namespace：`Hearthstone`
- 业务代码根目录：`Assets/Scripts/Hearthstone/`
- Unity 版本：`2022.3.62f3c1`
- 默认范围：asmdef、GameEngine、BaseStage、配置占位类型、ECS Singleton/System、UiScene、View、Controller、Editor Stage 入口

## 已创建的代码与配置

标准占位文件：

- `Assets/Scripts/Hearthstone/Hearthstone.asmdef`
- `Assets/Scripts/Hearthstone/Bootstrap/HearthstoneGameEngine.cs`
- `Assets/Scripts/Hearthstone/GameStage/PlaceholderStages.cs`
- `Assets/Scripts/Hearthstone/Config/ScriptableObject/PlaceholderSettingsData.cs`
- `Assets/Scripts/Hearthstone/Ecs/RawComponent/Singleton/PlaceholderStateSingletonRawComponent.cs`
- `Assets/Scripts/Hearthstone/Ecs/System/PlaceholderStateSystem.cs`
- `Assets/Scripts/Hearthstone/Ui/Scene/PlaceholderUiScene.cs`
- `Assets/Scripts/Hearthstone/Ui/View/PlaceholderView.cs`
- `Assets/Scripts/Hearthstone/Ui/Controller/PlaceholderController.cs`

Editor 接入代码：

- `Assets/Scripts/Hearthstone/Editor/Hearthstone.Editor.asmdef`
- `Assets/Scripts/Hearthstone/Editor/GameStage/InitialStageEntryAsset.cs`
- `Assets/Scripts/Hearthstone/Editor/ProjectInitializationBuilder.cs`：一次性构建器，等待当前 Unity 完成包解析后自动创建资产；资产创建并验证后应删除。

现状文档：

- `AutoDoc/ProjectOverview.md`

## 最小流程证据

静态调用链已经接通：

`HearthstoneGameEngine.OnAwake` → `RegisterSystemOrder(InputSystem, PlaceholderStateSystem, TaskSystem)` → `EnterInitialStageGroup` → `SetActiveGameStage(BaseStage)` → `InitializePlaceholderState.Load` 创建 Singleton → `PlaceholderStateSystem` 将 `Initialized` 设为 `true`。

BaseStage 卸载时，`InitializePlaceholderState.Unload` 移除同一 Singleton。占位 Controller 监听同一 ECS 状态，View 只保存 `TMP_Text` 引用，UiScene 只创建 `Main` Group。

## Subagent 关联审计

已完整读取：

- `.codex/agents/art-doc-writer.toml`
- `.codex/agents/design-doc-writer.toml`
- `.codex/agents/task-checker.toml`
- `.codex/agents/design-plan-code-reviewer.toml`
- `.codex/agents/design-plan-plan-reviewer.toml`

`.codex/project-files/agents/` 不存在，按空目录处理。全部 TOML 中配置的 skill 路径均存在；`design-doc-writer-agent-extension.md` 存在并明确引用现存的 `unit-design-docs/SKILL.md`。未发现无效项目级关联，未移除任何 TOML 表项。

## Unity 包与框架

基线包已存在，自动添加数量为 0：

- `com.unity.entities`：`1.0.0-pre.65`
- `com.unity.textmeshpro`：`3.0.6`
- `com.unity.ugui`：`1.0.0`

已核实 BbxCommon、CrossLibrary、UniTask 和 Odin Inspector 路径存在，并读取关键 GameEngine、GameStage、ECS 与 MVC API。未修改 `packages-lock.json`，未升级依赖。

## 验证结果

已通过：

- 初始化脚本前置扫描：业务脚本 0、Scene 0，判定为 FrameworkOnlyShell。
- 标准模板 dry-run 复查：9 个目标文件全部为 `unchanged`，无冲突且不会再写入。
- 初始化后扫描：识别唯一业务根目录、1 个 GameEngine、1 个 Stage 创建/激活链路、Singleton、System、UiScene、View、Controller、配置和 IStageLoad 信号。
- 两个 asmdef 均通过 JSON 解析；直接引用覆盖代码和公开类型链。
- 框架边界静态审计通过：未访问内部管理器，未复制 Stage/UI/ECS 生命周期，未手写资源导出产物。

未通过：

- Unity 资产构建未执行。
- Unity 业务程序集编译未完成。
- Main Scene、Stage、ECS 状态、UI、卸载与重入的 PlayMode 验证未完成。

阻塞证据：匹配版本 Unity 已经由用户打开该项目。批处理实例因项目锁被拒绝；当前 Editor 随后报告既有依赖 `com.boxqkrtm.ide.cursor` 的 Git 下载连接被重置并停在 Package Manager 窗口。单独执行 `git ls-remote` 已能成功访问同一仓库，说明网络目前恢复，但需要在现有窗口触发 Retry 或关闭 Editor 后再由批处理继续。

## 待由 Unity 生成的资产

一次性构建器计划通过 Unity Editor API 创建并校验：

- `Assets/Scenes/Main.unity`
- `Assets/Scenes/Ui/Placeholder.unity`
- `Assets/Resources/Bootstrap.prefab`
- `Assets/Resources/Ui/CanvasProto.prefab`
- `Assets/Resources/Ui/PlaceholderView.prefab`
- `Assets/Resources/Ui/Placeholder.asset`
- `Assets/Resources/Editor/InitialStageEntry.asset`
- Build Settings 首位启用 `Assets/Scenes/Main.unity`
- `Assets/Resources/ResourcesDictionary.json`

这些目标当前全部不存在，没有手写任何 Scene、Prefab、`.asset` 或 `.meta`。Unity 导入时还会自行生成相关 `.meta`、项目文件与包解析产物；BbxCommon 会按其流程维护 ScriptableObject/预加载/LoadingTime 数据。

## Placeholder 替换方向

- `PlaceholderStateSingletonRawComponent`：由首个真实运行时权威状态 Component 替换。
- `PlaceholderStateSystem`：由首个真实规则 System 替换并登记到对应 Stage/System 顺序。
- `PlaceholderStages` 与 `InitializePlaceholderState`：由真实 Base/模式 Stage 工厂及成对初始化项替换。
- `PlaceholderSettingsData`：由首个真实配置类型与资产替换。
- `PlaceholderUiScene`、`PlaceholderView`、`PlaceholderController`：由首个真实页面、UiGroup 与 MVC 配对替换。
- `InitialStageEntryAsset`：保留为初始 Group 入口，或在真实默认入口稳定后由具名真实 Group 入口替换。

## 偏差与未解决风险

- 仓库没有 Git 元数据，无法用 diff 独立证明任务前状态；改动范围以初始化前扫描和当前文件清单为依据。
- `ProjectInitializationBuilder.cs` 因 Editor 阻塞尚未运行和删除；它带当前场景安全检查，不会覆盖已保存场景或包含未知对象的未保存场景。
- 未通过目标 Unity 编译前，不能排除 Editor API 或程序集引用层面的编译问题。
- 未完成 PlayMode 卸载/重入验证前，不能宣称完整初始化完成。

## 文档与清理

- 创建并复核 `AutoDoc/ProjectOverview.md`，只记录当前代码骨架事实。
- 玩家视角设计和美术文档不适用：当前没有真实玩家功能或美术资产。
- 未读取或修改 `AutoDoc/DesignPlan/`。
- `AutoDoc/CleanupTempDocs.bat` 仅运行一次，退出码为 `0`；清理阈值未触发删除。
