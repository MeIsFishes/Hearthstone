# 策划案验收结构与实现流程修改报告

## 任务结果

已完成策划案验收结构、实现验收编排、Review agent 契约和 Git 完成门槛更新。

策划案正文现在使用 `## 6. 验收`，固定包含 `### 6.1 验收方式`、`### 6.2 美术资产验收`、`### 6.3 程序功能验收`。验收方式限定为“流程log验收”“游戏内截图”“仅功能验收”，未指定时默认流程log验收。原可选“实现方式建议”顺延为第 7 章，仍只在用户明确要求时出现。

实现策划案的验收阶段现在强制先确认并行列表与进度、识别可合并验收的策划案、逐案列出验收方式和列表、规划统一验收趟次，再正式验收并逐篇把材料交给 Review agent 生成独立验收 Review。整个批次最终只有在 `git status --short` 无输出时才能报告完成。

## 修改文件

- `.codex/private-skills/design-plan-doc/SKILL.md`
- `.codex/private-skills/project-state-preflight/SKILL.md`
- `.codex/private-skills/project-state-preflight/design-plan-writing.md`
- `.codex/private-skills/project-state-preflight/design-plan-implementation.md`
- `.codex/agents/design-plan-plan-reviewer.toml`
- `.codex/agents/design-plan-code-reviewer.toml`

## 检查项结果与证据

- 通过：正文结构改为六个必选二级章，第 6 章含三个固定验收小节，可选实现建议为第 7 章。
- 通过：三种验收方式及默认流程log验收在策划案、写作流程、实施 Plan、Plan 审查、代码审查和最终 Review 中一致。
- 通过：`ART-` 与 `FUNC-` 编号继续分开维护；合并验收强制保留“验收趟次 → 策划案 → case → 原编号”映射。
- 通过：实现流程完整覆盖用户要求的五项验收顺序，前四项未完成时禁止正式验收。
- 通过：相同场景和操作链可合并成一趟验收，但每篇策划案必须分别提交材料并生成独立 Review 文档。
- 通过：最终 Review 第 2 章同步改为“验收方式、美术资产验收、程序功能验收”三个小节。
- 通过：并行组或批次提交后，以及任务报告创建后，均要求 `git status --short` 无输出；任务外遗留改动不得被自动提交、修改或丢弃，只能阻塞完成并上报。
- 通过：两个既有 agent 的名称、简短描述、权限边界、输出路径、skill 引用和主代理注册保持有效；职责、描述、路径和类型未变化，无需修改注册表。
- 通过：未创建、编辑或删除 `.meta` 文件，未建立新的 skill、agent、验收或 Git 流程。
- 不适用：正式程序、美术和玩家视角设计文档同步。本次没有改变游戏当前实现或玩家可见内容。

## 验证结果

- Python `tomllib` 成功解析两份修改后的 agent TOML。
- 两份 SKILL 的等价 frontmatter 校验通过：只含 `name` 与 `description`，名称/目录合法，description 分别为 21、30 字，正文分别为 177、60 行。
- 两个 agent 的名称格式、description 长度、全部 skill 引用路径和主代理注册均校验通过。
- 契约检查全部通过：单一验收章、三个小节、三种方式、默认值、可选第 7 章、五项验收流程、合并映射、逐篇 Review 和 Git 清洁门槛均存在。
- 全量旧规则检索未发现旧 `## 6. 美术资产验收`、`## 7. 程序功能验收`、七个必选章、可选第八章或旧 Review“验证结果”标题。
- 官方 `skill-creator/scripts/quick_validate.py` 已尝试运行，但当前 Python 环境缺少 `PyYAML`，报 `ModuleNotFoundError: No module named 'yaml'`；已按该脚本源码中的实际校验项完成等价检查。
- 当前目录不是 Git worktree，无法使用 Git diff 做本次修改范围校验；已通过明确路径清单、旧规则全量检索和文件引用校验完成替代审计。

## 偏差与未解决风险

- 偏差：官方 skill 校验脚本因环境依赖缺失未能启动；等价检查和 TOML 解析均通过。
- 历史正式策划案不会因规范变更自动批量迁移；下一次实质修订或重新实施时按新结构迁移并保留原 `ART-`、`FUNC-` 编号。
- 无其他已知未解决风险。

## 文档处理与清理

- 未修改任何正式策划案或当前现状文档。
- `AutoDoc/CleanupTempDocs.bat` 仅运行一次，退出码 0；清理前后均为 7 份临时 Markdown，未触发删除。
