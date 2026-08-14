# UI界面文档格式

UI界面文档用于记录 UI 界面、界面逻辑和界面跳转关系。

路径限定：UI 系统文档统一放在 `AutoDoc/Program/UI/` 下，推荐路径为 `AutoDoc/Program/UI/<doc-name>/<doc-name>.md`。

文档必须包含且仅包含以下章节；如果某一节当前没有内容，也保留章节标题，并写“当前未发现”或“当前无”。

## 1. 核心数据来源

### 1.1 Component

列出该 UI 界面直接读取、写入或监听的 Component。

### 1.2 Csv和ScriptableObject配置项

列出该 UI 界面读取或依赖的 Csv 配置和 ScriptableObject 配置项。

## 2. UI界面

### 2.1 关联界面Controller列表

列出该 UI 界面关联的 Controller。

### 2.2 每个Controller监听的Component变量

按 Controller 分组列出其监听的 Component 变量。

### 2.3 不同Controller之间的跳转关系

说明不同 Controller 之间的打开、关闭、返回或跳转关系。

## 3. 所属GameStage

列出该 UI 界面所属或依赖的 GameStage。
