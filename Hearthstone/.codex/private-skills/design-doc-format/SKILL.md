---
name: design-doc-format
description: 约束特殊 AutoDoc 设计文档章节格式。
---

# Design Doc Format

创建或更新 `AutoDoc/Design/` 下的玩家视角设计文档时，使用本 skill。

本 skill 只约束下列特殊设计文档。特殊设计文档必须严格按照对应子文档给出的章节编写；如果某个章节对应模块不存在，则省略该章节并保持剩余章节顺序。未被本 skill 约束的其他设计文档，可以根据实际内容自行判断章节结构。

特殊设计文档统一放在 `AutoDoc/Design/Specific/` 下，不放在 `AutoDoc/Design/` 根部或普通模块目录下。每篇特殊设计文档应使用英文子目录承载，推荐路径为 `AutoDoc/Design/Specific/<doc-name>/<doc-name>.md`。

虽然项目应当逐步具备下列特殊设计文档，但仍然遵循“用户做到哪写到哪”的原则。检查文档缺失或编写文档时，只确认用户当前任务上下文或外部代理传入范围相关的部分；不要因为某类特殊文档在全项目中尚不存在，就在无关任务中报告缺失或主动补写。

创建或重命名文档时，文档 `.md` 文件名和文件夹路径必须使用英文，避免系统层面的路径错误。文档标题和正文可以继续使用用户的语言。

## 通用写作约束

1. 只描述玩家可见的界面、入口、操作、反馈、流程和功能边界。
2. 按玩家视角的模块或界面组织文档，不按单个需求点拆分。
3. 只记录当前已经实现且可由项目确认的玩家体验；用户需求或主代理描述仅用于定位核验范围，尚未落地的内容一律不写入文档。
4. 只修改当前任务直接相关的文档，保留其他模块和既有内容。
5. 正文使用用户的语言；代码类名、API、调用链和内部实现不写入设计文档。

## 参考图规则

所有玩家视角设计文档都可以在正文最后增加一个可选的“参考图”章节。没有能辅助理解的当前图片时，省略整个章节，不创建空章节或占位图。特殊设计文档即使受子格式约束，也允许在所有适用章节之后追加该章节；正文使用编号章节时，按保留章节连续编号。

参考图用于辅助理解当前已经实现的玩家体验，通常来自游戏验证、运行过程或对应界面的截图。图片必须与文档模块直接相关，并附上说明画面内容和参考重点的图注；它不是玩法原型图，也不用于记录尚未实现的设计。

文档专用图片统一保存在 `AutoDoc/media/design/<module-name>/` 下，模块名使用英文小写 kebab-case，图片文件名也使用英文小写 kebab-case。Markdown 使用从文档到图片的相对路径。美术文档直接展示 `Assets/` 中真实项目资产时可以继续引用原资产，不为文档重复复制；`AutoDoc/Temp/` 中的临时验证截图只有被正式设计文档采用时才复制或移动到 `AutoDoc/media/`。

更新设计文档时，检查“参考图”内每张图片是否仍反映正文所述的当前界面、流程和状态。过期、重复或已被新图取代的图片应从文档中移除或替换；删除 `AutoDoc/media/` 中的旧文件前，先检索所有正式文档，确认没有其他文档仍在引用。阅读设计文档时，如果参考图能帮助理解布局、交互或状态，可读取其中直接相关的图片；不要无差别加载同目录全部媒体。

## 使用规则

1. 先判断当前要创建或更新的文档是否属于下列特殊设计文档。
2. 如果属于特殊设计文档，必须读取对应子 md，并按其中章节格式编写。
3. 如果不属于特殊设计文档，不要强行套用这些格式。
4. 如果当前任务上下文涉及游戏整体定义，则应检查或编写游戏总览文档。
5. 如果当前任务上下文涉及开始界面，则应检查或编写开始界面文档。
6. 如果当前任务上下文涉及战斗环节，则应检查或编写战斗系统文档。
7. 如果当前任务上下文涉及局外养成系统，则应检查或编写局外养成系统文档。
8. 如果当前任务上下文涉及剧情系统，则应检查或编写剧情系统文档。
9. 如果当前任务上下文涉及存档系统，则应检查或编写存档系统文档。
10. 创建、重命名或整理特殊设计文档时，必须放入 `AutoDoc/Design/Specific/` 下对应英文子目录。
11. 创建或更新设计文档时，检查可选“参考图”章节及其媒体是否仍有效；阅读文档时按理解需要查看其中直接相关的参考图。

## 特殊文档格式

- [游戏总览](game-overview/game-overview.md)：`AutoDoc/Design/Specific/game-overview/game-overview.md`
- [战斗系统](combat-system/combat-system.md)：`AutoDoc/Design/Specific/combat-system/combat-system.md`
- [开始界面](start-screen/start-screen.md)：`AutoDoc/Design/Specific/start-screen/start-screen.md`
- [局外养成系统](meta-progression/meta-progression.md)：`AutoDoc/Design/Specific/meta-progression/meta-progression.md`
- [剧情系统](story-system/story-system.md)：`AutoDoc/Design/Specific/story-system/story-system.md`
- [存档系统](save-system/save-system.md)：`AutoDoc/Design/Specific/save-system/save-system.md`
