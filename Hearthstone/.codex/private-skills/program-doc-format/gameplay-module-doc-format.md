# 玩法模块文档格式

玩法模块文档用于记录非 UI、非 GameStage 的玩法功能模块。

路径限定：通用玩法模块文档统一放在 `AutoDoc/Program/Gameplay/` 下，推荐路径为 `AutoDoc/Program/Gameplay/<doc-name>/<doc-name>.md`。特殊玩法模块文档不使用本路径，应按对应特殊格式放入 `AutoDoc/Program/Specific/`。

文档必须包含且仅包含以下章节；如果某一节当前没有内容，也保留章节标题，并写“当前未发现”或“当前无”。

## 1. 核心数据来源

### 1.1 Component

列出该玩法模块直接读取、写入或依赖的 Component。

### 1.2 Csv和ScriptableObject配置项

列出该玩法模块读取或依赖的 Csv 配置和 ScriptableObject 配置项。

## 2. 逻辑驱动

### 2.1 System

列出驱动该玩法模块主要逻辑的 System。

#### 2.1.1 重要的System顺序依赖

说明该玩法模块中重要 System 之间的执行顺序，以及为什么需要遵守这个顺序。

### 2.2 StageListener

列出该玩法模块关联的 StageListener。

### 2.3 关联Task启动入口

说明该模块关联到哪些task启动入口，随后逐一说明每个task入口从哪张表或配置读取 task key，以及关联到哪个 TaskContext。

### 2.4 调用链路梳理

梳理该玩法模块从入口到核心逻辑执行的主要调用链路。

## 3. 所属GameStage

列出该玩法模块所属或依赖的 GameStage。
