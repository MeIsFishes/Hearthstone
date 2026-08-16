# 主菜单界面程序文档

## 1. 核心数据来源

### 1.1 Component

当前无。主菜单在本局 `RunStateStage` 创建之前显示，不读取、写入或监听玩法 Component。

### 1.2 Csv和ScriptableObject配置项

当前无。界面静态结构来自 `Assets/Resources/Ui/MainMenuView.prefab`；背景、标题与悬停纹理分别来自 `MainMenuCover.png`、`MainMenuTitle.png` 和 `MainMenuStartHoverWetParchment.png`。

## 2. UI界面

### 2.1 关联界面Controller列表

| Controller | View / Prefab | 职责 |
| --- | --- | --- |
| `MainMenuController` | `MainMenuView` / `Assets/Resources/Ui/MainMenuView.prefab` | 处理开始新一局、打开图鉴和清除图鉴数据 |
| `CardCollectionController` | `CardCollectionView` / `Assets/Resources/Ui/CardCollectionView.prefab` | 默认隐藏；由图鉴按钮显示，并负责返回主菜单 |

`MainMenuView` 持有标题 Image、“开始游戏”“图鉴”与“清除数据”按钮引用，以及右下角版本 TMP 引用。`Cover` 拉伸到 `1920 × 1080` 参考画布且不接收射线；标题使用 `820 × 300` 的保持比例 Image，中心位于 `(0, 285)`。“开始游戏”和“图鉴”关闭 Navigation 并以透明常态、低饱和湿润羊皮纸悬停底纹显示；右上角“清除数据”使用透明点击区和红色 TMP 文字。页面打开时版本文字从 `Application.version` 刷新，以黑色右对齐小字显示，当前为 `v0.1.0`。

`UiViewBase` 首次初始化时会遍历当前 View 直属层级内包括 inactive 对象在内的全部 `Button`，为每个按钮注册一次 `click_001`、音量 `0.7` 的统一点击音；嵌套子 View 的按钮由其自身初始化，避免重复注册。按钮音直接挂在 `Button.onClick`，因此只随有效点击触发，禁用按钮不会播放。

### 2.2 每个Controller监听的Component变量

`MainMenuController` 和 `CardCollectionController` 当前都不监听 Component 变量。

### 2.3 不同Controller之间的跳转关系

`MainMenuStage` 加载 `Ui/MainMenu` 导出资产后默认显示 `MainMenuController`，同一 UiScene 内的 `CardCollectionController` 已打开但默认隐藏。“图鉴”通过 `UiApi` 显示收藏页并隐藏主菜单；“返回”执行反向切换。“开始游戏”调用 `HearthstoneGameEngine.StartNewRun()`；StageGroup 切换后两页随 `MainMenuStage` 一起卸载。

## 3. 所属GameStage

| GameStage | UI 导出资产 | UI 编辑场景 |
| --- | --- | --- |
| `MainMenuStage` | `Assets/Resources/Ui/MainMenu.asset` | `Assets/Scenes/Ui/MainMenu.unity` |
