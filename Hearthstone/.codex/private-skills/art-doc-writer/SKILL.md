---
name: art-doc-writer
description: 编写2D图片美术风格、UI、模块与分类规格文档时使用。
---

# 美术文档写作规范

用本 skill 建立和维护 `AutoDoc/Art/` 下的美术文档，统一 2D 图片的视觉风格与生成规格。

## 文档类型与路径

1. **美术风格总文档**：记录整个项目共享的视觉语言。路径固定为 `AutoDoc/Art/Style/art-style-overview.md`。编写时读取 [美术风格总文档格式](references/art-style-overview-format.md)。
2. **UI 美术文档**：按美术风格分组记录项目通用 UI 资产、当前主要 UI 界面，以及当前项目已经引用但实际缺失的资产。路径固定为 `AutoDoc/Art/UI/ui-art-overview.md`。编写时读取 [UI 美术文档格式](references/ui-art-overview-format.md)。
3. **模块美术文档**：记录一组视觉主题一致的图片、UI 风格分组关联及逐项生成规格。路径固定为 `AutoDoc/Art/Modules/<module-name>/<module-name>.md`。编写时读取 [模块美术文档格式](references/art-module-format.md)。
4. **人物规格文档**：记录承担角色表现的主要主体规格，可包含角色、战机、坦克、机器人等多种分类。路径固定为 `AutoDoc/Art/Specifications/character-specifications.md`。编写时读取 [人物规格文档格式](references/character-specification-format.md)。
5. **场景规格文档**：记录背景、关卡空间和环境画面的多种分类规格。路径固定为 `AutoDoc/Art/Specifications/scene-specifications.md`。编写时读取 [场景规格文档格式](references/scene-specification-format.md)。
6. **物件规格文档**：记录道具、武器、装置、建筑部件和装饰物的多种分类规格。路径固定为 `AutoDoc/Art/Specifications/object-specifications.md`。编写时读取 [物件规格文档格式](references/object-specification-format.md)。

目录名和文件名使用英文小写、数字和连字符；标题和正文使用用户的语言。

按美术内容自身的统一性划分模块，可围绕一组界面、图标、人物、场景或主题图片建立文档。模块粒度以一组共同约束能够稳定指导图片生成为准。

每种规格文档可以定义多个分类；每个分类使用稳定的规格 ID，供模块美术文档引用。

所有美术文档只记录项目当前已经存在或可由资源、Prefab、配置确认的事实，不记录计划资产、未来风格或尚未落地的生成目标。

UI 美术文档根据美术风格把跨界面复用的资产划分为稳定分组；项目 UI 风格统一时只建立一个分组，并单独记录当前项目已有引用但实际缺失的资产。模块美术文档中的 UI 资产必须引用对应分组 ID。项目级 UI 视觉语言仍写入美术风格总文档，只属于单一视觉主题的详细生成规格仍写入对应模块美术文档，不在多处重复维护同一套规格。
