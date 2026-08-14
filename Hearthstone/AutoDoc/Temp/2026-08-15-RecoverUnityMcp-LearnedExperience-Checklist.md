# Recover Unity MCP 经验补充检查清单

- [x] **通过**：仅维护既有底层 skill `.codex/private-skills/recover-unity-mcp/`；`MAIN_AGENT.md` 中原有关联仍可匹配，未创建平行 skill 或索引。
- [x] **通过**：`name` 保持 `recover-unity-mcp`；`description` 更新后为 32 个字符，未超过中文 40 字限制，并覆盖未暴露、启动、握手、实例和调用失败。
- [x] **通过**：核心恢复顺序保留在 `SKILL.md`，按证据执行的故障分支保留在 `references/failure-matrix.md`，新增复杂只读探针说明独立放入条件引用。
- [x] **通过**：`readonly-sdk-probe.md` 已说明后端验通但当前会话工具快照未刷新时，新消息或环境刷新不构成热加载，必须重载或新建 Codex 会话。
- [x] **通过**：已记录中文用户路径下 `WinError 2`、`cmd.exe` 乱码及 ASCII PowerShell 诊断启动器；明确先发现实际 Scripts/Python 路径，禁止硬编码机器路径或 Python 版本。
- [x] **通过**：已区分用户 PATH、当前 Codex PATH 和 Unity 启动时 PATH；绝对 `uvx.exe` 已成功启动时禁止重复安装或破坏健康配置。
- [x] **通过**：已限定只读探针为 initialize、tools/list、`manage_scene(get_active)`、`read_console(error)` 和不落盘的 `validate_script`，并禁止所有 Unity 写操作绕行。
- [x] **通过**：已要求分开记录调用结果与关闭结果，并把 `BrokenResourceError`、`ClosedResourceError` 等关闭噪声单独归类，不能仅以进程退出码覆盖成功调用。
- [x] **通过**：已记录端口快照属于弱证据，并要求仅在 Editor 状态安全时使用项目允许的官方入口；示例入口限定为 MCP for Unity。
- [x] **通过**：核心流程和探针参考均要求筛选、脱敏日志，禁止回显许可证、访问令牌和用户凭据。
- [x] **通过**：边界已明确禁止用无关 gameplay Harness 证明 MCP 可用，并禁止把 SDK 只读探针当成 Unity 资产修改通道。
- [x] **通过**：两条 Markdown 引用均经 `Test-Path` 验证存在；skill 目录无新增 README、变更日志或 `.meta` 文件，`.meta` 数量为 0。
- [x] **通过**：使用 `skill-creator/scripts/quick_validate.py` 校验；因系统 Python 缺少 PyYAML，改由 `uv run --with pyyaml` 临时提供依赖，结果为 `Skill is valid!`。
- [x] **通过**：框架边界审计确认 Codely 必须使用正式客户端适配工具，标准 MCP for Unity 才使用标准 SDK 探针；没有加入私有 Socket、桌面自动化、手写资产 YAML 或平行恢复实现。
- [x] **不适用**：本次只修改底层 skill 的 3 个 Markdown 文件和任务文档；目录并非可用 Git worktree，已按精确修改目标复核文件清单。未修改游戏代码、资源或玩家可见行为，无需同步程序、美术或玩家设计现状文档。
- [x] **通过**：仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0；清理后已创建同任务名 `*-Report.md`。
