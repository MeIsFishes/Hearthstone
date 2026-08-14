# Main Agent Navigation

如果你是主代理，请读取项目根目录下的 `.codex/agents/MainAgent/MAIN_AGENT.md` 并遵循其中规则。

如果你是子代理，只遵循你的 `.toml` 指令以及主代理直接提供给你的任务上下文。

# Modifying Files Authorization

即使你处于 plan mode，修改用户或 skill 明确列出的文件、任务检查清单、任务报告，以及 `AutoDoc/` 下的文件，始终是被允许的。

执行项目任务时无需修改 `.meta` 文件；不要创建、编辑或删除任何 `.meta` 文件。

# Unity Editor 操作通道 / 可选 MCP for Unity

读取或修改 Unity Editor 实时状态（Scene、Prefab、GameObject、组件、Console、Play Mode、Build Settings、烘焙或 Unity 资产）时，不强制使用 MCP。可以根据任务和当前环境使用 Unity Editor 正常操作、项目约定的 Editor API/Editor 脚本（包括适用 UI 流程中的 `UiBuilder`），或当前会话正式暴露且项目允许的自动化通道。Unity MCP 不可用本身不构成一般 Unity 任务的硬阻塞；应改用任务允许的其他通道，并如实记录实际操作与验收方式。

无论选择哪种通道，都不得手写 Scene、Prefab 或 `.asset` YAML，不得自行调用 Editor Bridge 私有协议或擅自改用桌面 UI 自动化；Unity 资产仍须遵循项目既有 GameStage、ECS、UI、资源导出和 Editor 配置源流程。纯源码、普通文本配置和只读文件检查仍按本项目既有文件与 skill 规则执行。

当用户明确要求使用 Unity MCP，或任务已经选择 MCP 作为本次操作通道时，本项目只允许 CoplayDev 官方开源 `MCP for Unity` `v10.0.0`。Unity 包由 `Packages/manifest.json` 通过官方 Git 仓库固定版本加载，包名为 `com.coplaydev.unity-mcp`；Codex 侧使用名为 `unityMCP` 的标准 MCP 服务配置。不得同时启用 Codely Bridge 或其他 Unity MCP 实现。

`MCP for Unity` 由 Unity Editor 包和独立 Python MCP Server 共同组成。Server 固定使用 `mcpforunityserver==10.0.0`，入口为 `mcp-for-unity --transport stdio`；Windows 环境必须传入 `PYTHONUTF8=1`、`PYTHONIOENCODING=utf-8` 和有效的 `SystemRoot`。`unityMCP` 配置优先使用 `uvx.exe` 的绝对路径，避免依赖当前 Codex 进程的旧 PATH 快照。

只有 Unity 包已解析、MCP Server 完成 initialize 和工具清单读取、Server 已发现当前 Editor 实例、端到端只读调用成功，并且 Codex 当前会话已实际加载 `unityMCP` 工具时，才能声称 Unity MCP 已连接；不得仅因包、配置、进程或端口存在就声称链路可用。

选择 MCP 通道后：

1. 先确认目标 Unity Editor 正在运行、`com.coplaydev.unity-mcp` 已解析且没有新增编译错误，并通过 `codex mcp get unityMCP` 核对 Server 的命令、固定版本、stdio 与环境配置。
2. 使用当前会话暴露的 MCP for Unity 工具执行操作；连接失效且存在刷新、重连或实例选择工具时，先刷新并选择当前项目实例后重试。需要启动 Editor Bridge 且 Editor 状态安全时，只使用包提供的官方入口，例如 `MCPForUnity.Editor.McpCiBoot.StartStdioForCi`。
3. 恢复或通过 MCP 验收时至少完成 `manage_scene(action="get_active")` 与 `read_console(action="get", types=["error"])` 两个只读调用。标准 MCP SDK 只读探针仅可用于诊断和验收，不得用于 Unity 资产写操作。
4. 当前会话没有暴露 `unityMCP` 工具、MCP Server 无法连接或重试仍失败时，明确报告该通道不可用，不得声称已经热更新。若用户或适用 skill 没有强制本次必须使用 MCP，改用项目允许的非 MCP 通道继续；若本次明确要求 MCP，则要求重载或新建 Codex 会话，或报告阻塞。
5. 通过 MCP for Unity 修改 Unity 资产时，MCP 只作为操作通道，不改变任何框架边界。

# Checklist And End-of-Task Audit

当 `project-state-preflight`、用户或某个 skill 要求使用任务检查清单时，请遵循以下规则：

1. 在开始实质性工作之前，在项目根目录 `AutoDoc/Temp/` 创建新的 `*-Checklist.md`，一次性写入当前已知的全部检查项。
2. 检查清单用于覆盖范围和结束审计，不是逐步执行器。执行期间可以按依赖关系调整顺序、批量处理或并行处理，不要求每完成一项就更新文件，也不因清单本身暂停。
3. 执行中发现新的必要检查项时，将其补入清单；不要求插入到“当前步骤”之后，也不要求重排后续编号。
4. 只有用户明确要求分阶段确认或在步骤之间等待时，才按用户指定的确认点暂停。普通 skill 清单不得自行引入逐步确认。
5. 任务结束前重新打开检查清单，逐项核对实际产物和验证证据，标记为通过、未通过或不适用；发现可修正缺口时先修正再复核。
6. 逐项复核后，只运行一次 `AutoDoc/CleanupTempDocs.bat`，再在 `AutoDoc/Temp/` 创建对应的 `*-Report.md`，记录任务结果、每项检查的状态与证据、验证结果、偏差、未解决风险和清理结果。即使任务阻塞或提前结束，也必须写报告说明未通过项。

# Subagent Authorization

当某个适用的 skill 或 `AGENTS.md` 明确要求或允许进行独立的子代理审查、委托处理，或并行 agent 工作时，可将其视为用户级授权，允许在该范围内使用 `spawn_agent`。

如果已启动的子代理明确要求启动其他子代理，主代理可以在该子代理请求的范围内代为启动对应子代理。

# Directory Tips

这是一个Unity游戏项目，如果你需要查找代码，只需要读取 `Assets/Scripts/`文件夹下的文件。

除非用户要求，否则默认不进入游戏验证。

大部分项目内内容的修改都只需要关注 `Assets/` 文件夹下的文件。

项目文档按类型保存在以下目录：

- 程序文档：`AutoDoc/Program/`
- 美术文档：`AutoDoc/Art/`
- 玩家视角设计文档：`AutoDoc/Design/`
- 策划案文档：`AutoDoc/DesignPlan/`
- 普通文档专用图片：`AutoDoc/media/`
- 策划案专用图片：`AutoDoc/DesignPlan/media/`

除 `AutoDoc/DesignPlan/` 外，正式项目文档永远只记录当前项目现状，不写入未来预期、计划功能、尚未实现内容或无法从当前项目确认的推测。`AutoDoc/DesignPlan/` 是受状态管理的策划案目录，可以记录尚未实现的设计意图，但不得作为当前项目现状的事实源。

正常情况下，`AutoDoc/DesignPlan/` 下的文档可能包含未来的期望或者过时的需求，通常没有参考价值。除非用户明确要求创建、修改、评审或追溯策划案，明确指定某篇策划案作为需求输入，或要求实现指定策划案，否则不要默认搜索、枚举或读取该目录。在非策划案写作任务中读取策划案时，必须通过代码、配置、资源和其他现状文档另行核验当前实现。

项目中许多文件是中文文件，需用UTF-8读取，避免读出乱码。

当对象适合复用且生命周期可以明确管理时，使用项目现有对象池，避免不必要的重复分配与 GC。
