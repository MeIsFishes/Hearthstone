# 卡牌图鉴界面程序文档

## 1. 核心数据来源

### 1.1 Component

图鉴本身不依赖 ECS Component。备战界面打开时会读取 `RunStateSingletonRawComponent` 中当前拥有的卡牌，用于兼容登记功能上线前已经进入本局的卡。

### 1.2 Csv和ScriptableObject配置项

`BattleCardCsvData` 提供卡号、原画键和融合配方；`BattleCardTypeCsvData` 提供卡名、等阶、基础属性与初始词条。图鉴循环当前卡号范围，排除 99 号分隔位与配方素材数为 4 的融合结果，因此当前得到 147 个卡位。右上角文本由 Controller 刷新为“已解锁 k/147”，左上角为返回按钮。融合卡显示数据由 `BattleCardSimulationFactory.CreateDeterministic()` 生成：公共工厂按配方模拟普通素材并调用 `RunCardRules.TryCreateFusionResultInstance()`，图鉴与敌方融合卡生成不再各自维护一份算法。

## 2. UI界面

### 2.1 关联界面Controller列表

| Controller | View / Prefab | 职责 |
| --- | --- | --- |
| `CardCollectionController` | `CardCollectionView` / `Assets/Resources/Ui/CardCollectionView.prefab` | 填充卡牌列表、显示收集计数、打开预览、播放收入口袋动画并返回主菜单 |
| `BattleCardItemController` | `BattleCardItemView` / `Assets/Resources/Ui/BattleCardItem.prefab` | 复用标准卡面；绑定图鉴锁定/解锁状态、点击回调、滚轮转发和词条悬停说明 |

静态页面结构由 `CardCollectionViewUiBuilder` 生成。计数底框在卡池之后生成，保证渲染层级位于卡池面板之上。卡池和预览卡均通过 `UiList` 创建共享 `BattleCardItemController`，使用框架对象池，不在运行时搭建整页静态层级。图鉴锁定绑定会在复用 99 号封印图像后额外清空卡名、词条与 Tooltip 文本，不影响备战卡池的 99 号分隔位。点击卡牌时，卡牌项把自身 `RectTransform` 随卡号回传；`CardCollectionController.OpenPreview()` 先检查过渡状态、来源位置和永久解锁记录，通过后创建预览条目、进入 `m_Opening` 状态并调用 `AudioApi.Play("click_001", 0.7f)`。因此锁定卡、空来源以及预览打开或收纳期间的重复点击不会播放。预览根节点先定位到该世界坐标并使用卡池 `0.8` 倍缩放，再在 `0.28` 秒内以三次缓出移动至屏幕中心并放大到 `2.0` 倍。打开阶段禁用蒙板确认；到达中心后才允许触发原有 `0.36` 秒收纳动画，将卡移动到实际蒙板底边 `x=0` 并缩至 `0.3`。

`CardCollectionView` 初始化时由 `UiViewBase` 为“返回”和预览遮罩等直属 `Button` 统一注册一次 `click_001`、音量 `0.7` 的点击音。统一监听挂在 `Button.onClick`，禁用状态不触发；共享卡牌条目使用自定义 PointerClick 而不是 `Button`，所以由上述成功打开预览入口显式播放同一段点击音，不在共享卡牌 Controller 中对备战或战斗上下文全局播放。

### 2.2 每个Controller监听的Component变量

两个 Controller 都不监听 Component 变量。

### 2.3 不同Controller之间的跳转关系

`MainMenuController` 显示图鉴并隐藏自身；图鉴返回时重新显示主菜单并隐藏自身。点击未锁定的卡只在图鉴内部显示预览蒙板；点击卡外空白处触发收纳动画，动画结束后仍停留在图鉴。

## 3. 所属GameStage

| GameStage | UI 导出资产 | UI 编辑场景 |
| --- | --- | --- |
| `MainMenuStage` | `Assets/Resources/Ui/MainMenu.asset` | `Assets/Scenes/Ui/MainMenu.unity` |
