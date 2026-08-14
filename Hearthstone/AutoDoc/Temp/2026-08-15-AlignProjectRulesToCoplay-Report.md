# 项目规则切换至 Coplay 报告

## 任务结果

项目根规则现已明确以 CoplayDev MCP for Unity v10.0.0 为唯一 Unity Editor 自动化链路，并补齐标准 MCP Server、stdio、运行环境、会话工具暴露和端到端验收约束。所有检查项通过或不适用。

## 修改内容

- 在 `AGENTS.md` 中把 Coplay 声明强化为唯一实现，禁止与 Codely Bridge 或其他 Unity MCP 并存。
- 固定 Python Server 为 `mcpforunityserver==10.0.0`，入口为 `mcp-for-unity --transport stdio`。
- 要求 Windows 子进程提供 `PYTHONUTF8=1`、`PYTHONIOENCODING=utf-8` 和有效 `SystemRoot`，并优先使用绝对 `uvx.exe` 路径。
- 把恢复成功定义为 initialize、工具清单、Editor 实例发现、`manage_scene(get_active)`、`read_console(error)` 与当前 Codex 会话实际工具暴露全部成立。
- 增加会话工具快照未刷新时必须重载/新建会话的规则，保留禁止私有协议、桌面自动化、手写 Unity YAML 和标准 SDK 写调用的边界。

## 验证结果

- `Packages/manifest.json`：`com.coplaydev.unity-mcp` 固定到 Git tag `v10.0.0`。
- `Packages/packages-lock.json`：解析结果与 manifest 一致。
- `codex mcp get unityMCP`：服务启用，stdio；命令为绝对 `uvx.exe`，参数为固定 v10 Server 和 stdio，UTF-8/SystemRoot/默认实例环境已配置。
- `MAIN_AGENT.md`：已经使用 `unityMCP` 与标准 MCP 故障条件触发 `recover-unity-mcp`，无需修改。
- 生效的项目规则中没有把 Codely 声明为当前实现的残留。

## 框架边界与文档

- 只调整代理自动化规则，没有改变 GameStage、ECS、UI、资源导出或 Editor 配置源流程。
- 没有修改 Unity 资产、业务代码或任何 `.meta` 文件。
- 不改变游戏程序、美术或玩家可见现状，无需同步正式程序、美术或玩家设计文档。

## 偏差、风险与清理

- 当前 Codex 会话仍未暴露 `unityMCP` 工具；规则和 MCP 配置已经对齐，但需要重载或新建 Codex 会话才能让工具表重新注入。
- 无其他未解决风险。
- `AutoDoc/CleanupTempDocs.bat` 仅运行一次，退出码为 0。
