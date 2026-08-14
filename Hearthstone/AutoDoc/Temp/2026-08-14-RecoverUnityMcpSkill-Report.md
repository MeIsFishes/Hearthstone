# Unity MCP 恢复底层 Skill 任务报告

## 任务结果

已创建底层 Skill `recover-unity-mcp`，并关联到主代理。主代理在 Unity MCP 工具未暴露、Server 启动或握手失败、Unity 实例未发现、工具调用失败时，必须先读取该 Skill，完成按证据恢复与端到端只读验证后再继续 Unity 任务。

## 产物

- `.codex/private-skills/recover-unity-mcp/SKILL.md`：恢复目标、诊断顺序、项目默认实现、验证闭环和禁止绕过边界。
- `.codex/private-skills/recover-unity-mcp/references/failure-matrix.md`：按证据区分会话工具冻结、包解析、uv、UTF-8 握手、实例发现和审批取消。
- `.codex/private-skills/recover-unity-mcp/agents/openai.yaml`：Skill 界面元数据与默认提示。
- `.codex/agents/MainAgent/MAIN_AGENT.md`：新增强制触发规则和第 18 项 Skill 索引。

## 检查项结果与证据

- **通过——类型与关联**：路径位于 `.codex/private-skills/`，只关联主代理，未修改 subagent 配置。
- **通过——命名与描述**：目录和 frontmatter 名称均为 `recover-unity-mcp`；中文 description 长度 28，不超过 40 字。
- **通过——条件拆分**：主流程保留在 `SKILL.md`，故障分支位于单层引用 `references/failure-matrix.md`。
- **通过——恢复覆盖**：明确覆盖工具未暴露、Codex 配置、Unity 包、uv/PATH、stderr UTF-8、stdio Bridge、实例选择与审批取消。
- **通过——验证闭环**：要求 initialize、tools/list、实例发现、活动场景读取和 Console 查询全部成功；不以配置或端口存在代替端到端验证。
- **通过——框架边界**：禁止私有 Socket、其他 MCP 并存、桌面自动化、手写 Unity 资产和诊断客户端资产写操作。
- **通过——主代理接入**：`MAIN_AGENT.md` 中路径真实存在，沿用现有索引，没有创建平行体系。
- **通过——范围审计**：未修改业务代码、Unity 资产或 `.meta`；新 Skill 下 `.meta` 数量为 0。

## 验证结果

- 使用 `skill-creator/scripts/quick_validate.py` 验证，输出 `Skill is valid!`。
- 使用 PyYAML 读取 `agents/openai.yaml`，确认 UTF-8、YAML 结构和 `$recover-unity-mcp` 默认提示有效。
- 检查 Skill 与主代理索引无 `TODO` 残留，全部相对路径存在。
- 独立前向测试正确处理“tools/list 成功但找不到 Unity 实例，且 Scene 未保存”的场景：保留用户 Editor 状态、拒绝重启和绕过，仅建议在现有 Editor 内启动 stdio 后重新执行只读闭环；未发现实质歧义。

## 执行偏差

- `init_skill.py` 在当前 Windows 控制台编码下生成的 `agents/openai.yaml` 含非法 UTF-8。已精确删除该初始化产物并用补丁重建为 UTF-8。
- `quick_validate.py` 的运行环境最初缺少 PyYAML，且 Python 默认 GBK 无法读取中文 Skill。最终由 uv 临时提供 PyYAML，并设置 `PYTHONUTF8=1`、`PYTHONIOENCODING=utf-8` 后通过；项目未新增 Python 依赖。
- 一次补充 YAML 断言因 PowerShell 展开 `$recover` 误报；改用 `chr(36)` 构造美元符号后验证通过，文件内容未发生问题。

## 未解决风险

- Codex MCP 工具清单通常在会话启动时冻结。Skill 已要求配置变化后用新会话验证并明确提示重载，但无法让既有会话热更新工具。
- 混合读写 MCP 工具在非交互 `approval=never` 环境中可能被取消。Skill 已要求把该情况与链路失败区分，并禁止为验证使用危险的全局免审批参数。

## 文档与清理

本任务只涉及底层 Skill 和主代理规则，不需要同步程序、美术或玩家视角现状文档。任务结束前只运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 0；清理后 Temp Markdown 数量为 20，未达到 500 文件阈值，因此没有删除历史临时文档。
