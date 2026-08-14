# Unity MCP 恢复底层 Skill 检查清单

## 用户要求与类型

- [x] 通过：底层 skill 已创建于 `.codex/private-skills/recover-unity-mcp/`。
- [x] 通过：只修改主代理索引与触发规则；未修改任何 subagent 配置。
- [x] 通过：`SKILL.md` 与 `references/failure-matrix.md` 已覆盖诊断、恢复、边界和验证闭环。

## Skill 内容

- [x] 通过：文件夹与 frontmatter 均为 `recover-unity-mcp`，符合英文小写连字符规则。
- [x] 通过：description 为“Unity MCP未暴露、启动或实例连接失败时诊断恢复。”，共 28 个字符。
- [x] 通过：中文指令正文与故障矩阵分别覆盖工具清单冻结、uv、UTF-8、实例发现和审批取消。
- [x] 通过：正文要求先保留证据、定位最靠前断点、每次只做一项最小修复，并以端到端调用为准。
- [x] 通过：边界明确禁止旧桥接并存、私有 Socket、桌面自动化、手写 Unity 资产与诊断客户端写操作。
- [x] 通过：条件分支位于单层引用 `references/failure-matrix.md`，主文件明确要求命中证据后读取。
- [x] 通过：验证闭环包含包解析、uv、Codex 注册、initialize/tools/list、活动场景和 Console。
- [x] 通过：`agents/openai.yaml` 已以 UTF-8 重建，界面名称、说明与默认提示均匹配 Skill。

## 主代理关联

- [x] 通过：主代理现有索引新增第 18 项 `recover-unity-mcp`。
- [x] 通过：主代理开头新增强制触发规则，覆盖工具未暴露、启动、握手、实例发现和调用失败。
- [x] 通过：主文件、引用文件和 agents metadata 均存在，主代理沿用既有索引，没有创建平行体系。

## 验证与框架边界

- [x] 通过：在 `PYTHONUTF8=1` 且由 uv 临时提供 PyYAML 的环境下运行 `quick_validate.py`，输出 `Skill is valid!`。
- [x] 通过：Skill 只允许标准 MCP、官方包入口和只读 SDK 探针；前向测试也拒绝私有协议、桌面自动化和资产写绕过。
- [x] 通过：本任务只新增 Skill 文件并修改主代理索引与临时检查清单；新 Skill 下 `.meta` 数量为 0，未修改业务代码或 Unity 资产。

## 结束审计

- [x] 通过：已重新读取清单并逐项写入文件、校验命令或前向测试证据。
- [x] 通过：只运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 0；清理后 Temp Markdown 数量为 20，未达到删除阈值。
- [x] 通过：清理后已创建 `2026-08-14-RecoverUnityMcpSkill-Report.md`，记录产物、验证、偏差、风险和清理结果。
