# GameStage文档格式

GameStage文档用于记录某个 GameStage 的职责、加载卸载时机、组合关系和所包含的逻辑项。

路径限定：GameStage 文档统一放在 `AutoDoc/Program/GameStage/` 下，推荐路径为 `AutoDoc/Program/GameStage/<doc-name>/<doc-name>.md`。

文档必须包含且仅包含以下章节；如果某一节当前没有内容，也保留章节标题，并写“当前未发现”或“当前无”。

## 1. 该GameStage所代表的逻辑含义

说明该 GameStage 在项目中代表的逻辑范围和运行阶段含义。

## 2. 系统中与哪些GameStage组合

列出该 GameStage 会与哪些其他 GameStage 组合启用或协作。

## 3. 在何时加载、卸载

说明该 GameStage 的加载时机、卸载时机和触发入口。

## 4. LoadItem项

列出该 GameStage 关联的 LoadItem 项。

## 5. 逻辑项

概述该 GameStage 包含的运行逻辑。

### 5.1 System列表和简要功能概述

列出该 GameStage 关联的 System，并简要说明各自功能。

### 5.2 StageListener列表和简要功能概述

列出该 GameStage 关联的 StageListener，并简要说明各自功能。

### 5.3 可能启用的Task流程和简要功能概述

列出该 GameStage 可能启用的 Task 流程，并简要说明各自功能。

## 6. 关联UI

列出该 GameStage 关联的 UI 界面或 Controller。

## 7. 读取的配置数据

列出该 GameStage 直接或间接读取的 Csv 配置和 ScriptableObject 配置。
