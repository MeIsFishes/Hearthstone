# Recover Unity MCP 经验补充报告

## 任务结果

已把本次 MCP 失效处理经验补入既有底层 skill `recover-unity-mcp`，保留其与主代理的既有关联，没有新建重复 skill。所有检查项通过或不适用，无未通过项。

## 修改内容

- 更新 `.codex/private-skills/recover-unity-mcp/SKILL.md`：扩展触发描述；把恢复链路同时适配“正式客户端适配器”和“标准 MCP Server”；增加日志脱敏、会话工具快照、PATH 快照、只读探针和 Harness 禁止规则。
- 更新 `.codex/private-skills/recover-unity-mcp/references/failure-matrix.md`：补充会话冻结、PATH 快照、瞬时端口证据、标准 SDK 子进程/关闭异常，以及 Codely Bridge 正常但客户端适配工具未暴露的分支。
- 新增 `.codex/private-skills/recover-unity-mcp/references/readonly-sdk-probe.md`：集中记录中文 Windows 用户路径、ASCII PowerShell 诊断启动器、只读调用白名单、关闭噪声判定、会话重载和日志安全经验。
- 将原“当前项目默认 Coplay v10.0.0”的表述修正为仅在项目明确采用 MCP for Unity 时使用的示例，避免覆盖当前项目 `AGENTS.md` 指定的 Codely Bridge 事实。

## 检查项与证据

- **类型与关联：通过。** 目标仍位于 `.codex/private-skills/recover-unity-mcp/`；主代理索引仍包含该 skill。
- **元数据：通过。** `name` 为英文小写连字符格式；中文 `description` 为 32 个字符。
- **条件拆分：通过。** 核心流程位于 `SKILL.md`；故障矩阵与复杂只读探针按证据条件读取。
- **经验覆盖：通过。** 已覆盖工具快照冻结、Unicode 路径 `WinError 2`、`cmd.exe` 乱码、PATH 进程快照、只读探针边界、关闭噪声、端口弱证据、Editor 状态保护和日志脱敏。
- **框架边界：通过。** Codely 只允许正式客户端适配工具；标准 SDK 只读探针不允许执行 Unity 写操作；未引入私有 Socket、桌面自动化、手写资产 YAML 或 gameplay Harness。
- **文件范围：通过。** skill 目录仅增加一个条件参考 Markdown；无 `.meta`、README 或变更日志。未修改游戏代码和资源。
- **正式文档同步：不适用。** 本次不改变玩家可见游戏现状、程序模块或美术资产，无需更新 `AutoDoc/Program/`、`AutoDoc/Art/` 或 `AutoDoc/Design/`。

## 验证结果

- `skill-creator/scripts/quick_validate.py`：通过，输出 `Skill is valid!`。
- 校验器依赖：系统 Python 缺少 PyYAML，使用已安装的 `uv run --with pyyaml` 临时提供依赖，没有修改项目或全局 Python 环境。
- `description` 长度：32，满足中文不超过 40 字。
- `SKILL.md` 引用：`failure-matrix.md` 与 `readonly-sdk-probe.md` 均存在。
- 主代理关联：存在。
- skill 目录 `.meta` 文件数：0。

## 执行偏差与未解决风险

- 项目目录当前不是可用 Git worktree，无法用 `git diff` 生成差异证据；已按本次精确目标逐文件复核修改范围。
- 本次仅维护恢复流程文档，没有重新触发或修改 Unity Editor 实时状态，也没有用诊断探针执行 Unity 写操作。
- 无未解决风险。

## 文档与清理

- 未创建或修改正式程序、美术、玩家设计或策划案文档。
- `AutoDoc/CleanupTempDocs.bat` 仅运行一次，退出码为 0。
- 清理后保留本检查清单与本报告。
