# 去除 Unity MCP 操作要求任务报告

## 任务结果

已完成。项目规则不再要求、优先或固定使用 MCP 操作 Unity；普通 Unity Editor 任务可以使用当前环境正式提供且项目允许的 Editor、Editor API/脚本或自动化通道，不因 MCP 不可用而搜索、安装、配置、恢复或阻塞。

保留 `recover-unity-mcp` 作为用户当次明确要求使用、诊断或恢复 Unity MCP 时的可选能力。现有 `Packages/` MCP 包也予以保留，因为用户要求去掉的是操作要求，而不是卸载可选包或删除诊断能力。

## 修改文件

- `AGENTS.md`：删除 CoplayDev v10 固定实现、Server、环境、连接、恢复与验收要求，改为通道中立规则。
- `.codex/agents/MainAgent/MAIN_AGENT.md`：取消 skill/代理自行选用 MCP 与自动恢复入口，仅保留用户当次明确要求时的路由。
- `.codex/private-skills/bbxcommon-ui/SKILL.md`：移除 UiScene/UiBuilder 的 MCP 前置检查、优先调用和失败恢复要求，改为使用实际可用的允许通道。
- `.codex/private-skills/recover-unity-mcp/SKILL.md`：触发条件收敛为用户当次明确要求；不再假设项目指定实现或版本。
- `.codex/private-skills/recover-unity-mcp/references/failure-matrix.md`：实现、版本、服务名与安装动作改由实际配置和用户本次授权决定。
- `.codex/private-skills/recover-unity-mcp/references/readonly-sdk-probe.md`：正式配置依据改为实际配置和用户本次要求，不再引用项目默认实现。

## 检查项与证据

1. **通过——MCP 要求已移除。** 根规则不再包含固定包、版本、服务配置、专属连接恢复或只读验收步骤；主代理与 UI skill 不再自动选择或优先 MCP。
2. **通过——非 MCP 框架边界保留。** 仍禁止手写 Scene、Prefab、`.asset` YAML、私有 Bridge 协议和未经允许的桌面 UI 自动化；GameStage、ECS、UI、资源导出、UiBuilder 与 Editor 配置源流程保持不变。
3. **通过——可选诊断能力边界。** `recover-unity-mcp` 只能由用户当次明确请求触发，不得由普通项目 skill 或代理自行触发；安装、实现切换与版本变更需要用户本次明确要求。
4. **通过——Skill 结构。** 既有目录、名称、frontmatter 和主代理索引保持有效；`recover-unity-mcp` description 长度为 21 个字符；相对 Markdown 引用均可访问。
5. **通过——无关修改与 `.meta`。** 本任务未修改游戏代码、配置、Unity 资产或 `.meta`；工作区其他既有修改保持原状。
6. **不适用——正式项目文档。** 已读取 `design-doc-format`、`art-doc-writer`、`program-doc-format`。本次没有玩家体验、美术、玩法代码、运行时 UI 或 GameStage 现状变化，因此无需同步 `AutoDoc/Design/`、`AutoDoc/Art/`、`AutoDoc/Program/`。

## 验证结果

- `git diff --check`：通过，仅有 Git 的 LF/CRLF 工作区提示，无补丁或空白错误。
- 生效入口检索：`AGENTS.md` 无 CoplayDev、v10、Server 或 MCP 专属工具调用；`bbxcommon-ui` 无 `unityMCP`、`execute_code`、`manage_scene`、`read_console` 或恢复入口。
- Skill 静态校验：frontmatter 与相对链接通过；未发现缺失引用。
- `.meta` 检查：`git status --short -- "*.meta"` 无输出。
- Unity/游戏验证：不适用；本次仅修改代理规则与 skill 文本，未改源码或 Unity 资产。

## 执行偏差与未解决风险

无执行偏差，无已知未解决风险。历史 `AutoDoc/Temp/` 报告和 Plan 中可能记录过去曾使用 MCP 的事实或旧要求；这些是历史任务产物，不是当前生效的项目规则，未为本次任务篡改历史记录。

## 文档处理与清理结果

正式设计、美术、程序文档均判定不适用。任务检查清单已逐项复核。`AutoDoc/CleanupTempDocs.bat` 仅执行一次，退出码为 0；清理后 `AutoDoc/Temp/` 有 91 个 Markdown 文件，低于 500 文件阈值，因此未删除文件。报告在清理后创建。
