---
name: program-doc-format
description: 编写或更新 AutoDoc/Program 文档时提供格式索引。
---

# Program Doc Format

本 skill 是 AutoDoc 程序文档格式的主入口。程序文档分为通用程序文档和特殊程序文档。通用程序文档必须且只能属于玩法模块文档、UI界面文档、GameStage文档三类，并严格遵循对应格式文件。

特殊程序文档是对特定玩法模块的程序文档格式约束，本质上仍属于玩法模块文档，但可以在通用玩法模块章节基础上增加该模块必要的专门章节。

## 文档路径规则

1. 通用玩法模块文档统一放在 `AutoDoc/Program/Gameplay/` 下。
2. UI 系统文档统一放在 `AutoDoc/Program/UI/` 下。
3. GameStage 文档统一放在 `AutoDoc/Program/GameStage/` 下。
4. 特殊程序文档统一放在 `AutoDoc/Program/Specific/` 下，不放入 `Gameplay/`、`UI/` 或 `GameStage/`。
5. 每篇文档应使用英文子目录承载，推荐路径为 `AutoDoc/Program/<doc-name>.md`；特殊程序文档推荐路径为 `AutoDoc/Program/Specific/<doc-name>.md`。

虽然项目应当逐步具备下列特殊程序文档，但仍然遵循“用户做到哪写到哪”的原则。检查文档缺失或编写文档时，只确认用户当前任务上下文、代码与配置或外部代理传入范围相关的部分；不要因为某类特殊文档在全项目中尚不存在，就在无关任务中报告缺失或主动补写。

创建或重命名文档时，文档 `.md` 文件名和文件夹路径必须使用英文，避免系统层面的路径错误。文档标题和正文可以继续使用用户的语言。

## 通用写作约束

1. 以代码和配置确认当前实现；与现有文档冲突时回到代码核对。
2. 只记录当前已经实现且可由代码或配置确认的行为；预期修改、待实现内容和未来计划一律不写入文档。
3. 围绕模块职责、数据、入口、接口、流程和功能边界组织内容，不按源码文件逐个走读。
4. 按玩法模块、UI 界面或 GameStage 划分文档；范围过大时按同类模块边界拆分。
5. 只处理当前任务相关文档，名称、路径、类型和接口必须与项目实际内容一致。

## 通用格式索引

1. 玩法模块文档
   - path: `.codex/private-skills/program-doc-format/gameplay-module-doc-format.md`
   - AutoDoc path: `AutoDoc/Program/Gameplay/<doc-name>/<doc-name>.md`
   - 用途：记录非 UI、非 GameStage 的玩法功能模块。
2. UI界面文档
   - path: `.codex/private-skills/program-doc-format/ui-screen-doc-format.md`
   - AutoDoc path: `AutoDoc/Program/UI/<doc-name>/<doc-name>.md`
   - 用途：记录 UI 界面、界面逻辑和界面跳转关系。
3. GameStage文档
   - path: `.codex/private-skills/program-doc-format/game-stage-doc-format.md`
   - AutoDoc path: `AutoDoc/Program/GameStage/<doc-name>/<doc-name>.md`
   - 用途：记录某个 GameStage 的职责、加载卸载时机、组合关系和所包含的逻辑项。

## 特殊程序文档格式索引

1. 战斗系统程序文档
   - path: `.codex/private-skills/program-doc-format/combat-system/combat-system.md`
   - AutoDoc path: `AutoDoc/Program/Specific/combat-system/combat-system.md`
   - 用途：记录战斗系统的数据、驱动逻辑、战斗流程、结算链路和相关 GameStage。
2. 局外养成系统程序文档
   - path: `.codex/private-skills/program-doc-format/meta-progression/meta-progression.md`
   - AutoDoc path: `AutoDoc/Program/Specific/meta-progression/meta-progression.md`
   - 用途：记录局外养成系统的数据、解锁与提升逻辑、货币流转、存取档影响和相关 GameStage。
3. 剧情系统程序文档
   - path: `.codex/private-skills/program-doc-format/story-system/story-system.md`
   - AutoDoc path: `AutoDoc/Program/Specific/story-system/story-system.md`
   - 用途：记录剧情系统的数据、触发逻辑、播放流程、条件依赖和相关 GameStage。
4. 存档系统程序文档
   - path: `.codex/private-skills/program-doc-format/save-system/save-system.md`
   - AutoDoc path: `AutoDoc/Program/Specific/save-system/save-system.md`
   - 用途：记录存档系统的数据、保存与读取流程、自动存档触发和相关 GameStage。

## 使用规则

1. 编写或更新文档时，先判断目标文档类型，再读取并严格遵循对应格式文件。
2. 如果目标文档属于特殊程序文档，先读取对应特殊格式文件；特殊格式文件已经包含通用玩法模块文档的核心结构，不需要再额外套用 `gameplay-module-doc-format.md`。
3. 如果目标文档不属于特殊程序文档，则按通用格式索引选择玩法模块文档、UI界面文档或 GameStage 文档。
4. 最终文档必须包含且仅包含对应格式文件规定的章节；如果特殊格式文件允许按模块情况省略章节，则按特殊格式文件执行。
5. 创建、重命名或整理程序文档时，必须按“文档路径规则”放入对应目录。
