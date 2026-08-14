# 策划案 Warning 与验收重试流程检查清单

- [x] 通过：状态枚举已扩展为五态；`Warning` 精确定义为首次验收失败且完成两趟修正后主干仍未通过。
- [x] 通过：`Warning` 与 `Completed` 使用包含 `plan:`、`review:` 的五行文件头；其余三态保持三行文件头。
- [x] 通过：范围选案排除 `Warning`；明确指定时与 `Completed` 一样要求二次确认，步骤 a 重开为 `In Progress` 并移除旧追溯行。
- [x] 通过：首次验收失败后进入最多两趟修正；每趟由执行子代理修改、code reviewer 复审，并在无代码硬阻塞时由主代理重新验收。硬阻塞时明确记录本趟未进入实际验收。
- [x] 通过：首次验收不计入修正次数；每趟只调用一次针对性 code reviewer，第二趟后禁止自动发起第三趟。
- [x] 通过：修正 code review 仍只审查代码、配置和资源接入，不执行验收、不判断验收通过、不编写正式 Review。
- [x] 通过：任一实际重新验收后主干通过即结束失败回路并进入 `Completed`；两趟耗尽后主干仍未通过才进入 `Warning`。
- [x] 通过：完成门槛仅要求主干美术资产、主干程序功能和关键回归通过；分类必须在首次验收前确定，禁止因失败或困难事后降级。
- [x] 通过：少数非主干边界项证据困难、成本不成比例、未执行或未通过均不阻塞 `Completed`，但不得写成通过。
- [x] 通过：正式 Review 第三章已改为 `## 3. 待决策技术项`，覆盖非主干边界验收困难和真正需要用户决策的技术风险，无内容时省略。
- [x] 通过：修正次数未耗尽时只写 `initial`、`retry-1`、`retry-2` attempt；耗尽后由主代理形成正式失败 Review，再设置 `Warning`。
- [x] 通过：两种终态均校验最终 Plan、正式 Review、Review 结论与状态一致；步骤 h 的提交、Git 清洁和代理关闭门槛继续适用，`Warning` 不算依赖完成。
- [x] 通过：写作流程识别 `Warning`，实质修订恢复 `In Design`，且不得设置 `In Progress`、`Warning` 或 `Completed`。
- [x] 通过：旧四态、仅 Completed 追溯、验收失败不复审、失败禁止正式 Review、旧第三章名等精确旧假设检索结果为 0。
- [x] 通过：沿用既有 a 至 h、执行代理、code reviewer 和主代理验收职责，没有新增平行状态机、验收器或审查代理。
- [x] 通过：官方 `quick_validate.py` 因环境缺少 `PyYAML` 无法运行；等价检查确认 frontmatter、21 字 description、目录名、UTF-8 无 BOM、190 行和 22 项关键语义全部通过。独立前向测试发现的歧义已逐项修正。
- [x] 通过：目标目录未发现 `.meta`；本次仅修改三个策划案流程文件和任务文档。未改游戏代码、美术资产或玩家现状，Program、Art、Design 文档同步不适用。
- [x] 通过：已逐项复核；`AutoDoc/CleanupTempDocs.bat` 仅运行一次并以 0 退出，Markdown 数量 18→18；已创建 `AutoDoc/Temp/design-plan-warning-retry-Report.md`。
