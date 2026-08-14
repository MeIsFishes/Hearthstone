# 策划案 Warning 与验收重试流程修改报告

## 任务结果

已加入 `Warning` 状态，并把验收失败后的处理固定为最多两趟修正回路。每趟由执行子代理修改、code reviewer 对本趟差异复审一次，并在不存在代码硬阻塞时由主代理重新验收。任一趟主干通过即可进入 `Completed`；首次验收和两趟修正后主干仍未通过时形成正式失败 Review 并进入 `Warning`。

`Completed` 现在只要求主干美术资产、主干程序功能和使核心链路可用的关键回归通过。少数真实的非主干边界项验收困难、未执行或未通过时不阻塞完成，但必须在正式 Review 的 `## 3. 待决策技术项` 中记录。

## 修改范围

- `.codex/private-skills/design-plan-doc/SKILL.md`：状态列表扩展为五态；定义 `Warning` 与五行文件头；更新 `Completed` 的主干通过语义。
- `.codex/private-skills/project-state-preflight/design-plan-writing.md`：让写作流程识别 `Warning`，实质修订时恢复 `In Design`，禁止写作流程设置实施终态。
- `.codex/private-skills/project-state-preflight/design-plan-implementation.md`：加入 Warning 选案与重开规则、两趟验收修正闭环、复审编号、主干门槛、正式失败 Review、待决策技术项和两种终态处理。

没有修改 `design-plan-code-reviewer.toml`：其既有契约已经支持由主代理提供唯一 round 并保持只读代码审查职责，本次只需在实施流程中规定何时再次调用和次数上限。

## 检查项与证据

- 通过：合法状态为 `In Design`、`Todo`、`In Progress`、`Warning`、`Completed`。
- 通过：`Warning` 与 `Completed` 使用包含最终 Plan/Review 路径的严格五行文件头；其他三态使用三行文件头。
- 通过：范围选案不自动纳入 `Warning`；明确重开时要求二次确认，并在制定 Plan 前切换回 `In Progress`、移除旧追溯行。
- 通过：首次主干验收失败后最多两趟修正，首次验收不占修正次数；第二趟后禁止自动发起第三趟。
- 通过：每趟包含执行子代理修改和一次 code reviewer 复审；复审 round 从步骤 d 最后编号继续递增且不得覆盖。
- 通过：复审成立项仍交执行子代理在当前趟修正，主代理只核对；本趟不追加第二次 reviewer。仍有代码硬阻塞时不进入实际验收，并在 attempt 中明确记录。
- 通过：无代码硬阻塞时由主代理亲自重新验收失败主干和必要回归；子代理不执行验收、不判验收通过、不编写正式 Review。
- 通过：主干通过即可 `Completed`；首次验收和两趟修正后主干仍未通过才进入 `Warning`。
- 通过：主干/非主干必须在首次验收前根据策划目标和核心链路证据分类，不得因失败或困难事后降级。
- 通过：少数非主干边界验收困难不触发修正回路、不阻塞 `Completed`，但不得写成通过。
- 通过：正式 Review 第三章更名为 `## 3. 待决策技术项`；明确禁止把主干失败移入该章规避验收。
- 通过：中间失败使用 `initial`、`retry-1`、`retry-2` 临时 attempt；次数耗尽后由主代理形成自足的正式失败 Review，不能仅依赖可能被清理的临时证据。
- 通过：`Completed` 与 `Warning` 都要求 Plan、正式 Review、Review 结论和文件头相符；`Warning` 不算依赖完成。
- 通过：既有 Git 清洁工作区和代理关闭门槛继续覆盖两种终态。
- 通过：写作流程实质修改 `Warning` 时恢复 `In Design`，且不会自行设置实施状态。
- 通过：没有增加平行状态机、验收器或新审查代理；未创建、编辑或删除 `.meta`。
- 不适用：本次没有改动游戏代码、美术资产或玩家可见现状，无需同步 Program、Art、Design 文档。

## 验证结果

官方 `skill-creator/scripts/quick_validate.py` 已尝试运行，但当前 Python 环境缺少 `PyYAML`，报错 `ModuleNotFoundError: No module named 'yaml'`。等价本地校验确认：frontmatter 精确匹配、description 为 21 个字符、目录名合法、UTF-8 无 BOM、SKILL.md 为 190 行，22 项状态与流程语义断言全部通过；旧四态、仅 Completed 可追溯、失败不复审、失败禁止正式 Review 和旧章节名等残留检索结果为 0。

按 `skill-creator` 要求进行了只读前向测试。测试代理能够正确推导 `Completed` 与 `Warning` 两条路径，并指出 attempt 命名、round 连续编号、总则复审措辞、复审意见归属、事后降级和临时证据依赖等歧义；这些问题均已修正。最后一处“代码硬阻塞时是否实际重验”的冲突也已改为安全分支，并由本地断言复核。

项目根目录不是 Git worktree，`git status --short` 与 `git rev-parse --show-toplevel` 返回 `not a git repository`；因此无法为本次 skill 修改提供 Git 差异或待提交列表，未初始化或改变 Git 状态。策划案实施流程中“批次结束时 `git status --short` 必须无输出”的既有门槛仍保留。

## 偏差与未解决风险

- 偏差：官方快速校验器因环境依赖缺失未运行成功，已由等价结构、语义断言和独立前向测试补充验证。
- 未解决风险：无已知规则冲突。`Warning` 的重新实施仍要求用户明确指定并二次确认，避免失败策划案在范围执行中被自动重开。

## 文档与清理结果

本次仅调整 Codex 流程文档，不改变游戏当前状态文档。结束审计后仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0；清理前后 `AutoDoc/Temp/` Markdown 数量均为 18，未触发删除。
