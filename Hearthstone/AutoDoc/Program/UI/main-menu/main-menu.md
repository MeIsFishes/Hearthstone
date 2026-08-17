# 主菜单界面程序文档

## 1. 核心数据来源

### 1.1 Component

当前无。主菜单在本局 `RunStateStage` 创建之前显示，不读取、写入或监听玩法 Component。

### 1.2 Csv和ScriptableObject配置项

当前无。界面静态结构来自 `Assets/Resources/Ui/MainMenuView.prefab`；背景、左右哥布林动作图集、标题与悬停纹理分别来自 `MainMenuParchmentBackground.png`、`MainMenuGoblinWarriorFrames.png`、`MainMenuGoblinArcherFrames.png`、`MainMenuTitle.png` 和 `MainMenuStartHoverWetParchment.png`。

## 2. UI界面

### 2.1 关联界面Controller列表

| Controller | View / Prefab | 职责 |
| --- | --- | --- |
| `MainMenuController` | `MainMenuView` / `Assets/Resources/Ui/MainMenuView.prefab` | 处理开始新一局、打开图鉴、退出游戏、清除图鉴数据，以及两侧哥布林的显示期逐帧动画 |
| `CardCollectionController` | `CardCollectionView` / `Assets/Resources/Ui/CardCollectionView.prefab` | 默认隐藏；由图鉴按钮显示，并负责返回主菜单 |

`MainMenuView` 持有标题 Image、左右哥布林 Image、各自 12 帧 Sprite 数组及 12 个逐帧二维位置补偿、“开始游戏”“图鉴”“退出游戏”与“清除数据”按钮引用，以及左下角版本 TMP 引用。`Cover` 使用右下带卷边的无人物羊皮纸背景并拉伸到 `1920 × 1080` 参考画布，不接收射线。左右人物的锚点分别为 `(0, 0.5)`、`(1, 0.5)`，基础位置分别为 `(255, -45)`、`(-255, -45)`，容器均为 `560 × 760`；Image 保持比例、不接收射线，并通过 `MainMenuSilhouetteKey` 材质把动作图集中的近白背景显示为透明。标题使用 `820 × 300` 的保持比例 Image，中心位于 `(0, 285)`。三个中央按钮关闭 Navigation，中心位置从上到下为 `(0, 100)`、`(0, -50)`、`(0, -190)`，文字字号统一为 `44`；常态透明并共用湿润羊皮纸悬停纹理，前两个按钮使用灰褐色着色，退出按钮使用红色着色。右上角“清除数据”使用透明点击区和红色 TMP 文字。页面打开时版本文字从 `Application.version` 刷新，以锚点 `(0, 0)`、位置 `(148, 38)` 的黑色左对齐小字显示，当前为 `v0.1.0`。

两张动作图集均以 `6 × 2` 名义网格承载 12 帧，但部分人物轮廓实际跨过 `256 px` 竖向格线，不能按固定网格硬切。构建器为战士和弓手分别保存逐帧审计后的完整主体水平边界，并在左右各保留 `6 px` 安全区；每个 Sprite 保持整行 `512 px` 高度，宽度随主体在 `222~245 px` 之间变化，从而把跨格轮廓归还给正确帧并排除相邻帧残片。构建器以第 0 帧为基准扫描每个实际 Sprite Rect 内的可见剪影：纵向取脚底基线，横向只取脚底上方 `80 px` 的站立带中心，避免剑尖、弓梢等会动外轮廓干扰锚点；各帧 Sprite 中心差和实际显示缩放一并计入二维补偿数组。Controller 首次显示时缓存两侧基础位置，之后在换帧时同步应用对应补偿，因此图格中的换行偏移和整体横向漂移不会造成角色上下或左右跳动。页面显示时 Controller 把两侧人物同步复位到第 0 帧，此后以每帧 `0.3` 秒连续正向播放到第 11 帧并立即倒序回到第 0 帧，再立即进入下一轮正向播放，端点不额外停顿。页面隐藏或关闭时停止更新；再次显示时重新从第 0 帧开始。逐帧更新只替换既有 Image 的 Sprite 引用并写入 `RectTransform.anchoredPosition`，不创建临时对象或集合。

`UiViewBase` 首次初始化时会遍历当前 View 直属层级内包括 inactive 对象在内的全部 `Button`，为每个按钮注册一次 `click_001`、音量 `0.7` 的统一点击音；嵌套子 View 的按钮由其自身初始化，避免重复注册。按钮音直接挂在 `Button.onClick`，因此只随有效点击触发，禁用按钮不会播放。

### 2.2 每个Controller监听的Component变量

`MainMenuController` 和 `CardCollectionController` 当前都不监听 Component 变量。

### 2.3 不同Controller之间的跳转关系

`MainMenuStage` 加载 `Ui/MainMenu` 导出资产后默认显示 `MainMenuController`，同一 UiScene 内的 `CardCollectionController` 已打开但默认隐藏。“图鉴”通过 `UiApi` 显示收藏页并隐藏主菜单；“返回”执行反向切换。“开始游戏”调用 `HearthstoneGameEngine.StartNewRun()`；StageGroup 切换后两页随 `MainMenuStage` 一起卸载。“退出游戏”调用 `Application.Quit()` 结束当前游戏程序。

## 3. 所属GameStage

| GameStage | UI 导出资产 | UI 编辑场景 |
| --- | --- | --- |
| `MainMenuStage` | `Assets/Resources/Ui/MainMenu.asset` | `Assets/Scenes/Ui/MainMenu.unity` |
