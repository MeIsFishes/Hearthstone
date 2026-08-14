# 存档系统程序文档格式

存档系统程序文档属于特殊玩法模块文档，用于记录存档数据结构、GameStage 加载卸载、LoadItem/LateLoadItem 触发点、保存读取流程和状态恢复链路。

只在当前任务上下文涉及存档系统时检查或编写。

文档 `.md` 文件名和文件夹路径必须使用英文。

路径限定：存档系统程序文档是特殊程序文档，统一放在 `AutoDoc/Program/Specific/save-system/save-system.md`。

文档必须包含且仅包含以下章节；如果某一节当前没有内容，也保留章节标题，并写“当前未发现”或“当前无”。

## 1. 核心数据来源

### 1.1 存档数据结构

列出存档文件、存档对象、存档槽位、版本号、校验字段等数据结构。

### 1.2 Component

列出存档系统在保存或恢复时读取、写入或依赖的 Component。

### 1.3 Csv和ScriptableObject配置项

列出存档系统读取或依赖的 Csv 配置和 ScriptableObject 配置项。

## 2. GameStage加载卸载链路

### 2.1 相关GameStage

列出存档系统所属或依赖的 GameStage，以及这些 GameStage 在存档、读档、切换场景或恢复状态时的职责。

### 2.2 LoadItem和LateLoadItem

列出与存档读取、状态恢复、配置加载、场景恢复、UI恢复相关的 LoadItem 和 LateLoadItem，并说明各项负责内容。

### 2.3 加载顺序与依赖

说明读档或进入游戏时，各 GameStage、LoadItem、LateLoadItem 之间的加载顺序和关键依赖。

### 2.4 卸载与清理

说明切换存档、退出单局、返回主界面或切换 GameStage 时，哪些存档相关状态会被卸载、清理或保留。

## 3. 存档读写链路

### 3.1 手动存档流程

说明手动存档入口、可用条件、数据收集和写入链路。

### 3.2 自动存档流程

说明自动存档触发点、节流或覆盖规则、失败处理和提示链路。

### 3.3 读档与状态恢复

说明读档入口、数据反序列化、GameStage 激活、LoadItem/LateLoadItem 执行和状态恢复链路。

### 3.4 版本兼容与异常处理

说明存档版本迁移、缺失字段、损坏存档或读取失败的处理链路。

## 4. 辅助逻辑项

### 4.1 System

列出存档系统涉及的 System。仅记录与存档触发、数据同步或恢复状态直接相关的 System，不把 System 作为本文档主线。

### 4.2 StageListener

列出存档系统关联的 StageListener。

### 4.3 关联Task启动入口

说明该模块关联到哪些task启动入口，随后逐一说明每个task入口从哪张表或配置读取 task key，以及关联到哪个 TaskContext。
