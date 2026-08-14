# Unity MCP 故障矩阵

只读取并执行与当前证据匹配的分支。A 至 H 主要针对标准 MCP for Unity；Codely Bridge 适配器未暴露时使用 I 分支。命令示例面向 Windows PowerShell；路径和版本以项目规则为准。

## A. 当前会话没有 Unity MCP 工具

证据：本次已经明确选择 Unity MCP 通道，并检查当前运行环境提供的完整工具目录，既没有直接 Unity 工具，也没有可发现的延迟 Unity 工具。一般 Unity Editor 任务未选择 MCP 时不进入本分支；仅在顶层提示或初始 schema 列表中没有看到 `unityMCP`，也不构成本分支证据。

1. 先检查环境实际提供的完整工具发现机制：有 `tool_search` 时搜索 `unityMCP`；有延迟工具目录时枚举它；有 `functions.exec` / `ALL_TOOLS` 时筛选 `mcp__unityMCP__*`。这些机制并非每个客户端都有，只使用当前环境正式提供的入口。
2. 如果发现延迟工具，读取 `manage_scene` 与 `read_console` 的实际 schema；环境支持 MCP resources 时先读取 `unityMCP` 的 instances/editor state，再执行两项只读调用。成功则退出本分支，结论应为“工具可用但未在顶层展开”，不得重载、重装或继续按缺失处理。
3. 完整目录确实没有 Unity 工具时，运行 `codex mcp list` 和 `codex mcp get unityMCP`。
4. 若未注册，先完成 B、C 分支，再用绝对 `uvx.exe` 路径注册。
5. 若已经启用，确认当前会话是否早于配置创建。Codex 工具目录可能在会话启动时冻结；继续发送消息或刷新环境信息不构成热加载。按 H 分支验通后端后，启动新会话验证，当前会话要求用户重载。
6. 不得为了让旧会话出现工具而编写私有适配器或直接调用 Unity Socket。

推荐 stdio 注册形态：

```powershell
$uvx = '<absolute-path-to-uvx.exe>'
codex mcp add unityMCP `
  --env "SystemRoot=$env:SystemRoot" `
  --env 'PYTHONUTF8=1' `
  --env 'PYTHONIOENCODING=utf-8' `
  --env 'UNITY_MCP_DEFAULT_INSTANCE=<project-name-or-Name@hash>' `
  -- $uvx --from 'mcpforunityserver==10.0.0' mcp-for-unity --transport stdio
```

已存在错误配置时先用 `codex mcp remove unityMCP` 精确移除该项；不要重写整个用户 `config.toml`。

## B. Unity 包没有解析

证据：`manifest.json` 没有指定包、`packages-lock.json` 仍是旧实现、PackageCache 不存在，或 Editor 日志报告 UPM 错误。

1. 核对固定依赖：

```json
"com.coplaydev.unity-mcp": "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.0.0"
```

2. 让 Unity Package Manager 正常解析；不要复制 Git 包到 `Assets/`。
3. 等待脚本编译结束，检查 `error CS`、Package Manager Error 和异常。
4. 仅当旧 Codely 目录、状态文件或锁记录仍真实存在时处理旧实现；删除前遵循项目的精确目标与授权规则。

## C. 提示 uv package manager not found

证据：Unity 设置窗口或日志明确找不到 `uv/uvx`。

1. 先运行 `Get-Command uv,uvx`；再检查用户 Python Scripts 目录，不要仅凭当前进程 PATH 失败判定未安装。
2. 未安装时执行：

```powershell
python -m pip install --user --upgrade uv
```

3. 取得 `uv.exe` 与 `uvx.exe` 的真实父目录，将该目录去重后加入 Windows 用户 PATH。
4. 用绝对路径验证 `uv --version` 和 `uvx --version`。
5. Unity 只能继承启动时的环境。保存 Editor 状态后重启 Unity，或让用户重启；只修改注册表 PATH 而不重启 Editor 不算恢复。
6. Codex MCP 配置优先保存绝对 `uvx.exe` 路径，避免依赖当前 Codex 进程的旧 PATH 快照。
7. 用户 PATH 已更新、当前 Codex 进程仍找不到命令属于正常的进程快照差异。绝对路径配置已经 initialize 成功时，不得重复安装 uv 或仅为修复当前进程 PATH 而重写健康配置。

## D. MCP startup failed / stderr 不是有效 UTF-8

证据：`stream did not contain valid UTF-8`、initialize response 前连接关闭，或中文 Windows 用户名环境下 stderr 解码失败。

1. 不要重新安装 Unity 包。
2. 给 `unityMCP` 进程增加：
   - `PYTHONUTF8=1`
   - `PYTHONIOENCODING=utf-8`
   - `SystemRoot=<当前 SystemRoot>`
3. 用相同参数直接运行 `mcp-for-unity --help`，确认 Server 包可以启动。日志和 stderr 只提取相关行，输出前脱敏。
4. 启动新 MCP 会话重新执行 initialize；旧失败会话不作为复测对象。

## E. tools/list 成功，但 No Unity Editor instances found

证据：Server 已列出工具，调用返回未发现 Unity Editor 实例。

1. 确认 Unity 正在运行、当前项目已完全加载、包已编译。
2. 核对 Codex 与 Unity Editor 的传输模式一致。stdio 模式下检查 Editor 日志是否出现 `StdioBridgeHost started on port 6400`。
   单次 `Get-NetTCPConnection` 未看到端口只是瞬时弱证据，不能推翻 Editor 日志与成功的端到端只读调用。
3. 用户可通过 `Window -> MCP for Unity` 选择 stdio 并启动连接。
4. 无可用 Unity MCP 工具且 Editor 可以安全重启时，可使用官方入口启动 stdio：

```text
-executeMethod MCPForUnity.Editor.McpCiBoot.StartStdioForCi
```

5. 不得在用户有未保存内容时擅自重启 Editor。Agent 自己启动且未修改内容的 Editor 可先正常请求关闭，再以官方入口重启。
6. 多实例时使用 `set_active_instance`，或把 `UNITY_MCP_DEFAULT_INSTANCE` 设置为准确的 `Name@hash`；不要依赖模糊项目名。

## F. user cancelled MCP tool call

证据：日志已经显示 `unityMCP/<tool> started`，随后被取消。

1. 判定 MCP 注册与工具暴露至少已通过，不要重装包或 Server。
2. 检查是否在非交互 `approval=never` 会话中调用了混合读写工具，例如 `manage_scene`。
3. 在允许用户确认的正常 Codex 会话中重试明确的只读 action，或使用产品支持的自动审批模式；不得使用危险的全局免审批参数只为完成验证。
4. 标准 MCP SDK 只读探针可用于链路验收，但不能替代主会话执行后续 Unity 写操作。

## G. 完整验证示例

至少执行以下两个只读调用：

```text
manage_scene(action="get_active")
read_console(action="get", types=["error"], count="5")
```

成功结果必须包含当前活动 Scene 的名称与路径，并能正常返回 Console 查询结果。工具数量、日志格式和实例哈希可以随版本变化，不要硬编码为永久断言。

## H. 标准 SDK 只读探针启动或关闭异常

证据：`codex mcp get` 健康但当前会话无工具；直接启动含中文用户路径的 `uvx.exe` 返回 `WinError 2`；`cmd.exe` 显示路径乱码；或只读调用成功后 stdio 关闭阶段出现 `BrokenResourceError`。

1. 读取[标准 SDK 只读探针](./readonly-sdk-probe.md)。
2. 先证明 `uvx.exe` 真实存在且绝对路径可运行，再区分未安装、PATH 快照和 SDK 子进程路径传递故障。
3. 必要时仅为诊断使用 ASCII `powershell.exe` 启动器，并在子进程内通过环境变量解析用户 Scripts 路径；不要使用 `cmd.exe` 转发中文路径，不要硬编码用户目录或 Python 版本。
4. 分别记录 initialize、`tools/list`、`manage_scene(get_active)` 和 `read_console(error)` 的结果；关闭阶段资源异常单独归类，不能只凭最终退出码否定已经成功的调用。
5. 后端探针成功而当前工具清单仍为空时，要求重载或新建 Codex 会话。探针不得用于任何 Unity 写操作。

## I. Codely Bridge 正常但客户端适配工具未暴露

证据：项目规则指定 Codely；Unity 已解析指定包，项目状态文件和 Editor Bridge 状态正常，但当前代理工具清单没有 Codely/Codex Unity Tools。

1. 核对 Unity Editor 正在运行、指定 Codely 包已经解析、项目根目录状态文件存在，并只从脱敏日志中确认 Bridge 已启动。
2. Codely Editor 侧 TCP Bridge 不是可直接注册给 Codex 的标准 MCP Server。当前运行环境没有正式客户端适配工具时，必须报告“Bridge 正常、客户端适配层未暴露”。
3. 不得把 `codex mcp add`、标准 SDK 探针、私有 TCP 客户端、桌面 UI 自动化或另一套 Unity MCP 当作 Codely 适配器替代品。
4. 若适配工具仅在会话启动时注入，要求重载或新建支持该工具的 Codex 会话，并重新检查工具清单。没有实际只读工具调用成功前，不报告链路已恢复。
