# 修改项目链路文档 Skill 核对报告

## 任务结果

已强化 `project-state-preflight` 的“修改项目”链路。今后修改项目（包括按照普通方案执行修改）完成后，必须把玩家视角设计、美术、程序三类现状文档核对作为结束门槛：先读取对应文档 skill，逐类判断影响；需要更新或新增时必须在本任务内实际完成，不能只留下建议或文档 Todo。

纯“要求出方案”链路保持只输出方案，不修改当前状态文档，避免把尚未实现的内容写成项目现状。

## 修改文件

- `.codex/private-skills/project-state-preflight/project-modification.md`

没有新增 skill、代理、脚本、格式文件或平行文档流程。

## 检查项与证据

- 通过：规则明确适用于普通项目修改和按普通方案执行修改。
- 通过：纯出方案任务仍使用 `solution.md`，不得修改当前状态文档。
- 通过：每次项目修改必须逐类核对玩家视角设计、美术、程序文档。
- 通过：必须完整读取 `.codex/private-skills/design-doc-format/SKILL.md`、`.codex/private-skills/art-doc-writer/SKILL.md`、`.codex/private-skills/program-doc-format/SKILL.md`，三个路径均已验证存在。
- 通过：当前模块存在更具体的项目级或底层文档 skill 时，要求读取其元数据、正文和所需格式引用，不套用无关格式。
- 通过：文档影响范围必须由实际代码、配置、资源、验证证据和玩家可见结果确定。
- 通过：任务 Checklist 必须为三类文档分别记录适用 skill、现有文档、现状变化、处理结论、目标路径和证据。
- 通过：需要更新的现有文档必须实际更新；形成新独立文档范围且缺少对应文档时必须实际新增。
- 通过：禁止只记录“建议更新”“后续补充”或遗留文档 Todo 后结束任务。
- 通过：确实不受影响的文档类别可以标记不适用，但必须说明实际修改范围和核验证据。
- 通过：新增与更新文档必须遵循对应 skill 的路径、格式、语言、媒体、范围和当前事实边界。
- 通过：普通现状文档同步不会默认读取或修改 `AutoDoc/DesignPlan/`。
- 通过：没有创建、编辑或删除 `.meta` 文件。
- 不适用：本次修改的是流程 skill，没有改变游戏代码、配置、资源或玩家可见现状，因此无需更新 Program、Art、Design 正式文档。

## 验证结果

- 13 项关键语义断言全部通过。
- 三个基础文档 skill 引用路径全部存在。
- 旧的“只按照 AGENTS 文档目录笼统同步”精确表述检索结果为 0。
- 修改文件为 UTF-8 且无 BOM，共 17 行。
- 首次官方校验受 Windows 默认 GBK 解码影响；设置 `PYTHONUTF8=1`、`PYTHONIOENCODING=utf-8` 后重新运行 `skill-creator/scripts/quick_validate.py`，结果为 `Skill is valid!`。
- 项目根目录不是 Git worktree，无法提供 Git 差异或待提交列表；未初始化或改变 Git 状态。

## 偏差与未解决风险

- 解释边界：根据上下文，本次把用户所说“修改方案时”落实为“按照普通方案执行项目修改时”，归入 `project-modification`；纯方案编写不会改变现状文档。
- 未解决风险：无已知规则冲突。

## 文档与清理结果

本次没有游戏现状变化，正式 Program、Art、Design 文档均判定不适用。结束审计后仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0；清理前后 `AutoDoc/Temp/` Markdown 数量均为 36，未触发删除。
