# 策划案实现方式建议章节 Skill 修改报告

## 任务结果

已完成。策划案正文现在保留七个必选章节，并新增按用户要求出现的可选第八章 `## 8. 实现方式建议`。用户没有要求实现方式时，整章跳过；用户明确要求时，只记录其指定或要求给出建议的范围。

## 修改文件

- `.codex/private-skills/design-plan-doc/SKILL.md`：定义可选第八章、允许范围、读取边界和维护审计规则。
- `.codex/private-skills/project-state-preflight/design-plan-writing.md`：同步策划案写作检查项，消除“固定七章”冲突。
- `.codex/private-skills/project-state-preflight/SKILL.md`：同步策划案框架边界审计的技术内容例外。

## 检查项结果与证据

- 通过：目标确认。修改的是已关联主代理的底层 `design-plan-doc` skill，未新增 skill、文件夹或 agent 关联。
- 通过：条件章节。规则明确未请求时不写标题、占位或“无”；明确请求时才追加第八章。
- 通过：内容边界。第八章不得主动读取代码、配置或程序文档，不得扩写其他技术主题，不得冒充实施 Plan、技术定案或完成证明。
- 通过：章节兼容。原第 6 章 `ART-` 与第 7 章 `FUNC-` 编号和后续实现流程保持不变。
- 通过：skill 元数据。`design-plan-doc` 名称、目录、description 与原有关联保持不变；名称符合 kebab-case，description 长度 21，不超过 40 个中文字符。
- 通过：直接依赖。已同步写作分支和预检主规则，无残留“七个固定章节”“六个章节”冲突表述。
- 通过：文件范围。没有创建、编辑或删除 `.meta` 文件；没有修改游戏代码、配置、资源或正式项目现状文档。
- 通过：框架边界。沿用既有策划案状态、归档、媒体、验收与 Plan/Review 流程，未建立平行体系。
- 不适用：程序、美术和玩家视角设计文档同步。本次不改变项目当前实现或玩家可见内容。

## 验证结果

- 手工等价结构校验通过：两份受影响 `SKILL.md` 均从合法 frontmatter 开始，只含 `name` 与 `description`；目录名与名称一致，名称格式和 description 长度有效。
- 条件规则检索通过：存在可选章标题、未请求跳过、禁止占位、禁止主动扩写及保留七个必选章节的明确规则。
- `.meta` 检索无结果。
- 官方 `skill-creator/scripts/quick_validate.py` 已尝试运行，但当前 Python 环境缺少 `PyYAML`，报 `ModuleNotFoundError: No module named 'yaml'`；因此改按该脚本源码中的全部实际校验项完成等价检查。

## 偏差与未解决风险

- 偏差：官方校验脚本因环境依赖缺失未能完成；等价检查已通过。
- 未解决风险：无已知内容风险。未安装全局 Python 依赖，避免为本次文档规则修改扩大环境变更范围。

## 文档处理与清理

- 无正式程序、美术或玩家视角设计文档需要同步。
- `AutoDoc/CleanupTempDocs.bat` 仅运行一次，退出码 0；清理前后均为 2 份临时 Markdown，未触发删除。
