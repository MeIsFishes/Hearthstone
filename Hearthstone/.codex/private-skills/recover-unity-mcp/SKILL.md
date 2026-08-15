---
name: recover-unity-mcp
description: 用户明确要求时诊断恢复Unity MCP。
---

# 恢复 Unity MCP

本 skill 只在用户当次明确要求使用、诊断或恢复 Unity MCP 时使用。项目 skill 和代理不得自行把 MCP 选为强制或优先通道，也不得仅因 MCP 不可用而触发本 skill 或判定一般 Unity Editor 任务阻塞。

## 恢复目标

恢复 `Codex 会话 -> 当前环境提供的客户端适配器或标准 MCP Server -> Editor Bridge -> 当前 Unity 项目` 的完整链路。以项目允许的 Unity 工具完成端到端只读调用作为完成条件；不得把包已安装、配置已写入、端口已监听或工具名已出现单独判定为恢复成功。

## 执行顺序

1. 先读取项目根目录 `AGENTS.md`，确认 Unity 资产操作边界；再从现有 MCP 配置、`Packages/manifest.json` 和 `Packages/packages-lock.json` 识别实际实现、版本与传输。项目不指定 Unity MCP 实现或版本，不得把本 skill 的示例或历史配置当作项目要求。
2. 保护 Editor 状态。先确认 Unity 是否由用户运行、是否有未保存 Scene/Prefab/Asset；未经许可不得关闭、重启或强制终止用户正在使用的 Editor。
3. 记录原始证据：当前会话工具清单、`codex mcp list/get`、Unity 进程、`Packages/manifest.json`、`Packages/packages-lock.json`、Editor 日志、`uv/uvx` 路径与版本。Editor 日志只提取与本次故障相关的行并脱敏；不得回显许可证、访问令牌或其他凭据。先诊断再修改。
4. 判断故障层级：
   - 当前会话没有 Unity MCP 工具；
   - MCP Server 没有注册或启动失败；
   - Server 与 Editor 已可端到端通信，但当前 Codex 会话的工具快照没有刷新；
   - Unity 包没有解析或编译失败；
   - Server 已提供工具，但没有发现 Unity 实例；
   - 标准 SDK 只读探针的子进程启动或关闭阶段失败；
   - 工具已经启动，但被审批策略取消；
   - 工具调用成功，但返回业务错误。
5. 命中具体错误文本或层级后，读取 [故障矩阵](./references/failure-matrix.md)，只执行匹配分支及其前置检查。
6. 每次只修复最靠前的断点，再从链路起点重新验证；不要同时重装包、改传输、改配置和重启 Editor，避免丢失因果证据。

## 工具不可用排查

“工具未显示”“MCP 未注册”“Server 未启动”“未发现 Editor 实例”和“工具调用失败”是不同故障，不得只看某一层就宣布 Unity MCP 不可用。按下面顺序取得证据，命中后停止向后猜测：

1. **检查当前运行环境的完整工具目录。** 不得把提示中顶层展示的工具列表当作完整目录。若环境提供 `tool_search`、延迟工具目录或代码执行器内的 `ALL_TOOLS`，先从完整目录搜索 `unityMCP`；在 `functions.exec` / `ALL_TOOLS` 形态下枚举名称以 `mcp__unityMCP__` 开头的工具。只有完整目录也没有直接或延迟 Unity 工具时，才能暂记为“当前会话未暴露”。
2. **验证延迟工具是否真的可调用。** 找到延迟工具后读取所需工具的实际 schema；若环境提供 MCP resources，先列出 `unityMCP` resources 并读取实例或 Editor state，再执行明确只读调用，例如 `manage_scene(action="get_active")` 和 `read_console(action="get", types=["error"])`。调用成功即证明当前会话已加载并可用，不得因为顶层列表未展开而要求重载或重装。
3. **检查 MCP 注册层。** 完整目录确实无工具时，运行 `codex mcp list` 与 `codex mcp get unityMCP`，核对 enabled、命令、版本、transport、环境变量和绝对可执行路径。未注册或配置错误只说明客户端到 Server 的入口有问题，不能据此判断 Unity 包状态。
4. **检查 Server 握手与工具清单。** 注册健康但会话无工具时，使用产品支持的 MCP initialize / `tools/list` 或本 skill 允许的标准 SDK 只读探针验证后端。后端健康而当前会话目录仍无工具，才判定为会话工具快照未刷新，并要求重载或新建会话。
5. **检查 Editor 实例层。** Server 有工具但返回 `No Unity Editor instances found` 时，确认 Editor 进程、Unity 包解析与编译、传输模式、官方 Bridge、实例目录和当前实例选择；多实例时使用准确的 `Name@hash` 或 `set_active_instance`。
6. **分类调用失败。** 工具已经开始执行后，区分连接错误、审批取消、参数/schema 错误与 Unity 业务错误。`user cancelled MCP tool call` 证明调用到达 MCP 层；业务错误证明链路可用但请求或 Editor 状态不满足条件，二者都不应触发重装。

排查记录至少写明：检查的是顶层展示还是完整工具目录、是否发现延迟工具、`codex mcp get` 结果、initialize / `tools/list` 结果、实例发现结果、两项只读调用结果。工具数量和具体注入位置属于运行环境实现细节，不得硬编码为永久断言。

## 实现识别

从当前项目和 Codex 的实际配置读取 Unity 包来源与版本、MCP 服务名、Server 包与版本、入口、传输及所需环境变量。项目不提供默认实现或固定值；不得为了套用故障矩阵中的命令示例而安装、替换或升级现有实现。任何安装、实现切换或版本变更都必须来自用户本次明确要求，并先核对影响范围。

Codely Editor 侧是 TCP Bridge；若当前运行环境没有提供相应客户端适配工具，包与 Bridge 正常也不等于 Codex 已获得可调用工具。

## 验证闭环

按顺序取得证据，全部通过后才能报告恢复成功：

1. Unity 包已从 `manifest.json` 解析进 `packages-lock.json`，Editor 日志没有包解析或新增编译错误。
2. 实际配置所需的运行时可被新启动的进程发现。仅在实现依赖 `uv/uvx` 时检查 Windows 用户 PATH 与当前 Codex、Unity 进程的旧 PATH 快照；MCP 配置使用绝对 `uvx.exe` 且实际启动成功时，不得仅因当前进程 `Get-Command uv` 失败而重复安装或改坏健康配置。
3. 标准 MCP 实现通过 `codex mcp get <name>` 确认服务启用、命令和版本；Codely 等需要客户端适配器的实现则确认当前运行环境实际暴露了对应工具，不能用 Editor 包存在替代这一证据。
4. 受支持的新客户端会话完成握手和工具清单读取；不要要求工具数量永久固定。
5. Server 能发现当前 Unity 实例，并成功执行 `manage_scene(action="get_active")`。
6. `read_console(action="get", types=["error"])` 成功返回；若存在错误，区分既有错误与本次恢复新增错误。
7. 当前 Codex 会话能从完整工具目录发现并实际调用直接或延迟 Unity 工具。若后端健康但只有新会话能发现工具，明确要求重载当前会话，不得声称已热更新。

当第 3 步正常、当前会话仍无工具，或标准 SDK 探针出现中文用户路径、子进程启动、关闭噪声时，读取[标准 SDK 只读探针](./references/readonly-sdk-probe.md)。该探针是诊断与验收手段，不是后续 Unity 操作通道。

## 边界

- 只使用当前代理环境正式暴露的客户端适配工具、标准 MCP、官方包入口和项目允许的配置源；不得自行实现或直接调用 Editor Bridge 私有 Socket 协议，不得换用桌面 UI 自动化。
- 不得用临时 Python/PowerShell 客户端执行 Unity 资产写操作来绕过当前会话未暴露工具。标准 SDK 只读探针只能用于诊断和验收。
- 只读探针可执行 initialize、`tools/list`、`manage_scene(action="get_active")`、`read_console` 和不落盘的 `validate_script`；其他工具必须先确认没有副作用。在本次 MCP 恢复流程内，任何 Scene、Prefab、GameObject、组件、Play Mode、Build Settings 或资产修改都必须等待当前代理会话获得项目允许的 Unity MCP 工具。若任务允许其他 Editor 操作通道，可以退出恢复流程并改按对应项目规则执行，但不得把非 MCP 操作表述为 MCP 已恢复。
- 无论是否使用 MCP，都不得手写 Scene、Prefab 或 `.asset` YAML。
- 不得创建、运行或保留与恢复链路无关的 gameplay Harness 来证明 MCP 可用；源码测试与 MCP 端到端验收是两类不同证据。
- 不得擅自安装另一套 Unity MCP 与当前实际配置并存。发现旧实现时先确认精确目标和删除授权；项目不要求代理主动清理或切换实现。
- `user cancelled MCP tool call` 表示工具调用经过了 MCP 层但被审批策略阻止，不等于握手失败；不要因此重装服务。
- 无法在不丢失用户 Editor 状态的情况下恢复时，报告已确认的断点和所需用户动作，停止破坏性尝试。
