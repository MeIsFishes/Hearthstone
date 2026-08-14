# Main Agent Rules

这是一个空项目模板。主代理只需要关注两类项目内规则：

1. TempPlan 步骤记录规则：`.codex/skills/temp-plan-recording/SKILL.md`
2. 程序文档写作规则：`.codex/skills/program-doc-format/SKILL.md`

如果你是子代理，只遵循你的 `.toml` 指令以及主代理直接提供给你的任务上下文。

## 使用规则

1. 当用户要求分步执行、分阶段确认，或明确要求记录步骤时，先读取并遵循 `temp-plan-recording`。
2. 当用户要求编写、更新或整理 `AutoDoc/Program/` 下的程序文档时，先读取并遵循 `program-doc-format`。
3. 需要了解项目时，可以先阅读 `AutoDoc/` 下的文档。
4. 进行了较大代码、结构或功能改动后，应同步检查并更新相关文档。
5. 除上述规则外，本空项目不预设额外 skill、agent、项目流程或子代理注册表。

# TempPlan Step Recording

当用户或某个 skill 明确要求分步执行、分阶段执行，或者希望在步骤之间确认时，请遵循 `.codex/skills/temp-plan-recording/SKILL.md`。

核心规则：

1. 在开始实质性工作之前，创建一个新的 TempPlan 文件，路径为项目根目录下的 `AutoDoc/Temp/`。
2. 将计划步骤以检查清单的形式写入 TempPlan，步骤描述尽量完整。
3. 每完成一步，都要更新 TempPlan，将该步骤标记为完成，并补充简短总结。
4. 每完成一步并更新 TempPlan 后，立即执行项目根目录下的 `AutoDoc/CleanupTempDocs.bat`。
5. 如果下一步存在歧义、缺少必要信息，或需要用户做决定，就在执行该步骤前停下来询问用户。
6. 当进行到某个步骤时，如果有 skill 等来源要求插入步骤，则在 TempPlan 中的当前步骤后插入对应步骤。

# Program Documentation

编写或更新程序文档时，请读取并遵循 `.codex/skills/program-doc-format/SKILL.md`。

程序文档默认写入 `AutoDoc/Program/`。不再按文档类型细分子目录；如需为单篇文档创建子目录，推荐路径为 `AutoDoc/Program/<doc-name>/<doc-name>.md`。

项目总文档固定路径为 `AutoDoc/Program/project-overview.md`。

文档路径与 `.md` 文件名使用英文；标题和正文可以使用用户的语言。
