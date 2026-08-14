# UiBuilder MCP 完整工具发现规则报告

## 任务结果

任务完成。`bbxcommon-ui` 的 UiBuilder 流程现已明确要求先通过当前代理环境的完整工具列表发现 Unity MCP，发现可调用工具后优先使用 MCP `execute_code` 执行 Builder。

## 修改内容

- Builder 执行前检查完整工具列表，而不是只看顶层提示或初始工具列表。
- 支持通过 `tool_search`、延迟工具目录和 `functions.exec` 的 `ALL_TOOLS` 发现工具。
- 按 `mcp__unityMCP__` 前缀识别 Unity MCP，重点查找 `execute_code`，并在调用前加载实际 schema。
- 发现可调用的 Unity MCP 后，优先用 `execute_code` 调用完整类型名的静态 `Build()`。
- 只有完整列表确认没有工具，或按实际 schema 调用失败时，才进入 `recover-unity-mcp` 或允许的替代流程。

## 检查与验证

全部适用检查项通过。静态检索确认完整工具列表、三种发现入口、Unity MCP 前缀、`execute_code`、schema、优先调用和失败分支均已写入 §2.7.5—§2.7.7。

框架边界保持不变：UiBuilder 仍是一对一的 Editor 配置源，不替代 View/Controller、Resources、UiScene 或 `UiSceneExporter`，也不注册菜单项。

## 文档处理

已读取玩家视角设计、美术和程序文档格式 skill。本次只修改代理工具发现与 Editor 调用规则，没有玩家体验、美术资产或运行时程序现状变化，因此正式现状文档均不适用。

## 执行偏差与风险

无执行偏差。本次没有实际 Builder 需要执行，因此只验证 skill 文本，没有连接 Unity MCP、启动 Unity 或修改任何 Unity 资产。

## 清理结果

结束审计后仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0；清理阈值未触发删除，Checklist 保留。报告在清理完成后创建。
