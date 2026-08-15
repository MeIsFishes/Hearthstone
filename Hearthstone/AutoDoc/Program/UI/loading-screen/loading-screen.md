# 加载界面程序文档

## 1. 核心数据来源

### 1.1 Component

当前无。加载界面不读取、写入或监听 ECS Component。

### 1.2 Csv和ScriptableObject配置项

当前无。加载界面不读取业务 CSV 或 ScriptableObject 配置，也不读取底层加载进度。

## 2. UI界面

### 2.1 关联界面Controller列表

| Controller | View Prefab | 职责 |
| --- | --- | --- |
| `LoadingController` | `Assets/Resources/Ui/LoadingView.prefab` | 在显示阶段把 Controller 根节点拉伸到引擎 Loading Canvas，并保持静态图片位于该分组最上层 |

`LoadingView.prefab` 只包含根 `LoadingView` 与 `Background` 图片。背景引用 `Assets/Resources/Art/Loading/UI/HearthstoneLoadingBackground.png`，Image 开启 Raycast 以遮挡加载期间的下层输入；`AspectRatioFitter.EnvelopeParent` 保持原图约 `16:9` 的比例并覆盖父级，允许在非 `16:9` 屏幕上裁切边缘。Prefab 默认隐藏，不包含文字、进度条、百分比或动态加载图标。

`LoadingViewUiBuilder.Build()` 是该 Prefab 的一一对应 Editor 配置源，负责 Sprite 导入约束、静态层级、全屏等比覆盖和 Pre-load 导出。当前 Pre-load 映射为 `Hearthstone.LoadingController → Ui/LoadingView`，`Resources.Load<GameObject>("Ui/LoadingView")` 可直接取得 Prefab。

### 2.2 每个Controller监听的Component变量

当前无。`LoadingController` 不创建 Model 监听，也不消费底层 `LoadingProgress`。

### 2.3 不同Controller之间的跳转关系

`HearthstoneGameEngine.OnAwake()` 通过 `SetLoadingUi<LoadingController>()` 把页面注册到 `UiGameEngineScene` 的 `Loading` 分组。底层 `GameEngineBase.StartLoading()` 在 Stage 操作批次开始前调用 `Show()`，在全部卸载与加载完成后调用 `Hide()`；加载界面不自行打开目标页面，也不参与战斗界面与备战界面的跳转决定。

## 3. 所属GameStage

加载界面不属于单一业务 GameStage。它由常驻 `GameEngineBase` 和 `UiGameEngineScene.Loading` 分组管理，覆盖初始 Stage 批次以及后续 Battle/Preparation StageGroup 切换，并复用同一个 Controller 实例完成显隐。
