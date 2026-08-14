# 项目规则切换至 Coplay 检查清单

- [x] **通过**：根 `AGENTS.md` 已明确 CoplayDev MCP for Unity v10.0.0 是唯一 Unity 自动化链路；包名和 Git 固定版本与 `manifest.json`、`packages-lock.json` 一致，官方 Editor 入口与当前 Unity 启动方式一致。
- [x] **通过**：规则已写明 `unityMCP`、`mcpforunityserver==10.0.0`、`mcp-for-unity --transport stdio`、绝对 `uvx.exe`、UTF-8/SystemRoot 环境和端到端只读验收。
- [x] **通过**：生效的项目规则不再要求 Codely 包、状态文件、TCP Bridge 或客户端适配器，并明确禁止与 Codely/其他 Unity MCP 并存。
- [x] **通过**：保留 Editor 未保存状态保护、官方刷新/重连/实例选择、会话重载，以及禁止私有协议、桌面自动化和手写 Unity YAML 的边界。
- [x] **通过**：`MAIN_AGENT.md` 已按 `unityMCP` 工具与标准 MCP 故障触发 `recover-unity-mcp`，索引一致，无需修改；恢复 skill 的 Coplay v10 示例与项目规则一致。
- [x] **通过**：搜索 `AGENTS.md` 与 `.codex/agents/` 后，没有把 Codely 声明为当前实现的残留。底层恢复 skill 保留 Codely 条件分支用于其他项目诊断，未误删通用知识。
- [x] **通过**：框架边界审计确认仅修改自动化通道规则；GameStage、ECS、UI、资源导出和 Editor 配置源要求保持不变。
- [x] **通过**：仅修改 `AGENTS.md` 与任务文档，没有修改 Unity 资产、代码或任何 `.meta` 文件；按项目默认不进入游戏验证。
- [x] **不适用**：此次为代理自动化规则调整，不改变游戏程序、美术或玩家可见现状；当前也无对应正式目录需要同步。
- [x] **通过**：仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0；清理后已创建同任务名报告。
