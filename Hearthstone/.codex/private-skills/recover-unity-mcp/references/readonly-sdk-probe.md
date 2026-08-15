# 标准 MCP SDK 只读探针

仅在项目实际使用标准 MCP Server，且出现下列任一证据时读取本文件：

- `codex mcp get` 显示配置健康，但当前 Codex 会话没有 Unity 工具；
- 需要区分 Server、Editor Bridge 与当前会话工具快照三个层级；
- 中文 Windows 用户目录下，标准 MCP SDK 启动绝对 `uvx.exe` 路径时报 `WinError 2` 或命令行乱码；
- initialize、`tools/list` 或只读调用已有成功结果，但关闭 stdio 时出现 `BrokenResourceError` 等异常。

## 探针范围

按顺序执行并分别记录结果：

1. initialize；
2. `tools/list`，只验证所需工具是否存在，不永久断言工具总数；
3. `manage_scene(action="get_active")`；
4. `read_console(action="get", types=["error"], count="5")`；
5. 仅在需要验证脚本文本且确认不落盘时执行 `validate_script`。

任一工具存在混合读写语义或副作用不明确时，不在探针中调用。探针成功只能证明后端链路可用；当前 Codex 会话没有暴露 Unity 工具时，仍不得执行 Unity 写操作。

## 中文 Windows 用户路径

在部分 Windows MCP SDK 子进程实现中，直接把含中文用户名的绝对 `uvx.exe` 路径作为 `command` 可能返回 `FileNotFoundError: [WinError 2]`，即使该文件真实存在且可由 PowerShell 直接运行。经 `cmd.exe /c` 转发还可能发生代码页乱码。此时：

1. 先用 `Test-Path` 与绝对路径 `uvx --version` 证明文件存在，避免误判为未安装。
2. 用 ASCII 路径的 `powershell.exe` 作为**诊断探针的启动器**；在 PowerShell 子进程内部通过 `$env:APPDATA` 和 `Join-Path` 解析 `uvx.exe`，避免由 SDK 直接传递含中文的可执行文件路径。
3. 先发现实际 Scripts 目录和 Python 版本，再构造相对 `$env:APPDATA` 的路径；不得盲目硬编码 `Python312` 或复制某台机器的用户目录。
4. 给子进程传入 `PYTHONUTF8=1`、`PYTHONIOENCODING=utf-8` 和有效的 `SystemRoot`。
5. 该 PowerShell 包装仅用于标准 SDK 只读探针。Codex 正式 MCP 配置仍以实际配置、用户本次明确要求和 `codex mcp get` 的启动结果为准；项目不提供默认实现或版本，不得为了绕过当前会话而另建长期代理层。

若项目使用 Codely Bridge 且当前环境没有 Codely/Codex 客户端适配工具，不适用此探针；应报告“Editor Bridge 可用但客户端适配工具未暴露”，而不是自行实现私有 TCP 客户端。

## PATH 快照

Windows 用户 PATH、当前 Codex 进程 PATH 与 Unity 启动时 PATH 是三份可能不同的快照：

- 用户 PATH 已包含 Scripts 目录，只说明未来启动的进程可以继承它；
- 当前 Codex 进程的 `Get-Command uv` 仍可失败，不代表绝对路径配置失效；
- Unity 在 PATH 更新前已启动时，Unity 内检测仍可能失败，需要先保护状态再由用户重启或安全重启；
- 只要 `codex mcp get` 使用绝对 `uvx.exe` 且 initialize 成功，不要为“统一 PATH”重复安装 uv 或重写健康配置。

## 成功结果与关闭噪声

把每一步的调用结果与会话关闭结果分开记录。stdio 会话可能在 initialize、`tools/list` 和只读工具均成功后，于退出阶段抛出 `anyio.BrokenResourceError`、`ClosedResourceError` 或异常组。这类异常不应抹掉已经取得的端到端成功证据，但也不能静默吞掉：

1. 对 initialize、工具清单和每个只读调用分别保存布尔状态与简短结果。
2. 在 finally/关闭阶段单独捕获并归类资源已关闭异常，避免让探针仅以进程退出码覆盖调用结果。
3. 只有调用阶段失败、结果不完整，或关闭异常表明 Server 在调用完成前断开时，才把对应链路步骤判为失败。
4. 报告中写明“调用成功、关闭有噪声”或“调用失败”，不要只写“探针退出码 1”。

## 会话工具快照

若只读探针已成功发现 Unity 实例并读取活动 Scene/Console，而当前会话工具清单仍没有 Unity 工具，则断点位于 Codex 会话暴露层。对已经启动的会话，继续发送消息、刷新日期或重新检查配置都不等于工具热加载。要求用户重载或新建 Codex 会话，并在新会话重新检查工具清单；在此之前只报告后端已验通，不报告当前会话已恢复。

## 日志与端口证据

- Editor 日志只筛选包解析、Bridge 启动、实例注册和本次调用相关行；在展示或写报告前脱敏令牌、许可证和用户凭据。
- `Get-NetTCPConnection` 某一时刻未看到默认端口，只是一份瞬时证据。Editor 日志显示 Bridge 已启动且端到端只读调用成功时，不得用单次端口快照推翻更强证据。
- 仅在 Editor 状态安全时，才可按项目允许的官方入口启动或重启 Bridge；例如 MCP for Unity 的 `MCPForUnity.Editor.McpCiBoot.StartStdioForCi`。不得擅自关闭用户有未保存内容的 Editor。
