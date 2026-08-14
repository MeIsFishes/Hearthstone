# 策划案 In Progress 状态修改检查清单

- [x] 通过：在策划案合法状态列表中加入 `In Progress`，定义为已经进入实现流程、尚未达到 `Completed` 门槛。
- [x] 通过：`In Progress` 使用严格三行文件头，`plan:`、`review:` 明确只允许出现在 `Completed` 文件头。
- [x] 通过：实施步骤 a 规定在制定 Plan 前先切换状态；候选筛选、调查、排序和分组明确不得改状态。
- [x] 通过：`Todo`、已明确纳入实施的 `In Design`、经二次确认重新实施的 `Completed` 均在步骤 a 切换；`Completed` 同时移除旧追溯行。
- [x] 通过：范围执行纳入 `In Progress`，先盘点 a 至 h 的既有证据并从最早未完成门槛续跑，不重复创建已有 Plan。
- [x] 通过：步骤 a 至步骤 g 成功前保持 `In Progress`；Plan、审查、实现、验收或正式 Review 中断、失败、阻塞均不回退 `Todo`。
- [x] 通过：写作流程明确实质修订 `In Progress` 文档时恢复 `In Design`，且写作流程不得设置 `In Progress`。
- [x] 通过：相关 skill 的旧三态枚举、旧三行文件头描述和只纳入 `Todo` 假设检索结果为 0。
- [x] 通过：沿用既有 `state` 文件头和实施 a 至 h 流程，没有引入第二套状态系统或无关资源。
- [x] 通过：`design-plan-doc` frontmatter 精确匹配既有名称与描述，目录名合法，正文 179 行；未新增脚本、资源目录或代理配置。
- [x] 通过：目标目录未发现 `.meta` 文件，本次只修改三个相关流程文件及任务检查文档。
- [x] 通过：官方 `quick_validate.py` 因环境缺少 `PyYAML` 无法运行；等价校验确认 UTF-8 无 BOM、frontmatter、目录名、行数和 10 项关键状态规则全部通过。
- [x] 不适用：本次只修改 Codex 策划案流程 skill，不改变游戏程序、美术资产或玩家可见设计现状，无需同步 Program、Art、Design 文档。
- [x] 通过：已逐项复核；`AutoDoc/CleanupTempDocs.bat` 仅运行一次并以 0 退出，Markdown 数量 16→16；已创建 `AutoDoc/Temp/design-plan-in-progress-state-Report.md`。
