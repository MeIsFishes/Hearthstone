# 主菜单界面程序文档

## 1. 核心数据来源

### 1.1 Component

当前无。主菜单在本局 `RunStateStage` 创建之前显示，不读取、写入或监听玩法 Component。

### 1.2 Csv和ScriptableObject配置项

当前无。界面静态结构来自 `Assets/Resources/Ui/MainMenuView.prefab`，背景来自 `Assets/Resources/Art/MainMenu/UI/MainMenuCover.png`。

## 2. UI界面

### 2.1 关联界面Controller列表

| Controller | View / Prefab | 职责 |
| --- | --- | --- |
| `MainMenuController` | `MainMenuView` / `Assets/Resources/Ui/MainMenuView.prefab` | 初始化“开始游戏”按钮，页面打开时恢复可交互状态，首次点击后禁用按钮并请求 GameEngine 开始新一局 |

`MainMenuView` 只持有游戏标题、开始按钮和按钮文字的组件引用。背景、“99升变”标题、“开始游戏”按钮及四态 Sprite 全部固化在 Prefab；`Cover` 拉伸到 `1920 × 1080` 参考画布且不接收射线，按钮关闭 Navigation。

### 2.2 每个Controller监听的Component变量

`MainMenuController` 当前不监听 Component 变量。

### 2.3 不同Controller之间的跳转关系

`MainMenuStage` 加载 `Ui/MainMenu` 导出资产后默认打开 `MainMenuController`。开始按钮调用 `HearthstoneGameEngine.StartNewRun()`；引擎新建本局持久状态并请求第 1 轮 `PreparationStage`。StageGroup 切换后主菜单 View/Controller 随 `MainMenuStage` 一起卸载，备战界面由 `PreparationUiScene` 创建。

## 3. 所属GameStage

| GameStage | UI 导出资产 | UI 编辑场景 |
| --- | --- | --- |
| `MainMenuStage` | `Assets/Resources/Ui/MainMenu.asset` | `Assets/Scenes/Ui/MainMenu.unity` |
