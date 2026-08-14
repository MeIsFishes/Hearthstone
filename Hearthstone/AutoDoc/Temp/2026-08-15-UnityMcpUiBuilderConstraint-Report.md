# Unity MCP 与 UiBuilder 项目约束调整报告

## 任务结果

任务完成。项目不再强制使用 Unity MCP 操作 Unity Editor；MCP 现在是可选通道，只有本次明确选用 MCP 时才进入连接恢复与专属验收流程。`bbxcommon-ui` 已增加 UiBuilder 创建或更新 UI Prefab 的完整规则。

## 修改内容

- `AGENTS.md`：把 Unity MCP 从强制通道改为可选通道；保留选用 MCP 时只允许 CoplayDev v10.0.0、必须真实验证连接等约束；继续禁止手写 Unity YAML、私有协议和未经允许的桌面 UI 自动化。
- `.codex/agents/MainAgent/MAIN_AGENT.md`：一般 Unity 任务不再因 MCP 缺失而强制进入恢复流程；只有明确选用 MCP 时才触发 `recover-unity-mcp`。
- `.codex/private-skills/recover-unity-mcp/SKILL.md` 与 `references/failure-matrix.md`：恢复流程的适用前提收敛为“本次明确选用 MCP”，并允许一般任务退出恢复流程后改用项目允许的其他 Editor 操作通道。
- `.codex/private-skills/bbxcommon-ui/SKILL.md`：新增 §2.7，并同步普通页面、Hud、UiScene 创建与验收步骤。

## UiBuilder 规则

- 固定路径：`Assets/Scripts/<项目名>/Ui/Editor/`；本项目示例为 `Assets/Scripts/Hearthstone/Ui/Editor/`。
- 每个 Prefab 对应一个独立 `<Prefab名>UiBuilder`，最终构建入口一一对应。
- 提供公开静态 `Build()`，要求可重复执行并正确保存静态层级、组件和序列化引用。
- 不添加 `[MenuItem]`，不通过初始化或资源导入回调自动执行，不注册项目菜单项。
- 选择 UiBuilder 流程后，由项目固定 Unity MCP 的 `execute_code` 直接调用完整类型名的 `Build()`。
- Builder 只作为 Editor 配置源，不替代 View/Controller、Resources、UiScene、`UiSceneExporter` 或框架生命周期。

## 检查项结果与证据

全部适用检查项通过。玩家视角设计文档、美术文档和程序现状文档同步均判定为不适用：本次没有改变玩家体验、美术资产、运行时 UI、接口或 GameStage，仅调整代理约束与开发流程 skill。详细逐项证据见同名 Checklist。

## 验证结果

- 静态断言通过：可选 MCP、主代理恢复触发、UiBuilder 路径、一对一 Builder、无菜单、`execute_code`、`Build()` 和禁止手写 YAML 均存在。
- `bbxcommon-ui` 的既有相对引用全部可访问。
- 两个修改后的 skill frontmatter 保持有效，`name` 为英文，`description` 简短，正文为中文。
- 未修改代码或 Unity 资产，因此未启动 Unity、未编译项目、未进入 Play Mode。

## 执行偏差与未解决风险

无执行偏差。当前只建立流程约束，没有新增实际 UiBuilder，也没有迁移既有 Editor Builder；后续某个 UI Prefab 选择 UiBuilder 时再按一对一规则落地并通过 MCP 调用、验收。

## 文档处理

已读取并核对玩家视角设计、美术和程序文档格式 skill，以及现有战斗 UI 程序文档和 UI 美术文档。它们记录当前玩家体验、资产与运行时实现，不承载本次代理工作流，因此未修改正式现状文档。

## 清理结果

结束审计后仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0；清理阈值未触发删除，本任务 Checklist 保留。报告在清理完成后创建。
