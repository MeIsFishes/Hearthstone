# 新手引导界面程序文档

## 1. 核心数据来源

### 1.1 Component

当前无。界面内容为静态说明，第一页的示例卡通过共享卡牌 Controller 按卡号绑定，不读取运行中的 ECS Component。

### 1.2 Csv和ScriptableObject配置项

第一页由 `BattleCardItemController` 以卡号 `4` 调用现有图鉴绑定链路，读取 `BattleCardCsvData`、对应的 `BattleCardTypeCsvData` 和卡牌原画资源，生成一张包含编号、原画、名称、词条、生命和攻击的完整共享卡面。第二、三页说明文字和示意数据由 `NewPlayerGuideViewUiBuilder` 静态生成；当前无新手引导专用 CSV 或 ScriptableObject 配置。

## 2. UI界面

### 2.1 关联界面Controller列表

| Controller | View Prefab | 职责 |
| --- | --- | --- |
| `NewPlayerGuideController` | `Assets/Resources/Ui/NewPlayerGuideView.prefab` | 创建第一页共享卡牌条目，维护三页显隐、页码、上一页可用状态和末页关闭回调；灰色蒙板只阻挡底层输入，不注册关闭事件 |
| `BattleCardItemController` | `Assets/Resources/Ui/BattleCardItem.prefab` | 在第一页复用当前完整卡面 Prefab，并按图鉴展示语境填充示例卡的编号、原画、名称、词条、生命、攻击与卡框 |
| `PreparationController` | `Assets/Resources/Ui/PreparationView.prefab` | 首次进入备战时先从第一页打开引导并门禁奖励抽牌演出；融合页帮助按钮复用同一引导并直接显示第三页，关闭后只返回融合页 |

第三页的融合结果摘要、词条升级说明和策略提示均由 `NewPlayerGuideViewUiBuilder` 写入静态 Prefab，三者统一使用 `NotoSansSC-SemiBold Dynamic SDF` 字体资源。词条升级说明使用普通字重，明确相同基础词条叠加时会升级；策略提示使用 `31` 字号和 `FontStyles.Bold`，锚定在页面底边并保持 `20` 像素中心偏移，与上方规则说明保持独立层级。

### 2.2 每个Controller监听的Component变量

`NewPlayerGuideController` 不监听 Component。它只维护页面索引和关闭回调，并在关闭时清空 `CardPreviewList` 的对象池条目。

`BattleCardItemController` 在本界面使用 `BindCollection(4, true, null, null)` 的静态图鉴展示语境，不监听运行 Component，也不开放拖拽或战斗输入。

`PreparationController` 沿用备战界面原有的 `PreparationSessionSingletonRawComponent` 奖励批次状态；只有 `m_NewPlayerGuide` 为空时才进入奖励展示链路。引导是否已经触发由 `NewPlayerGuideSave` 的 `PlayerPrefs` 键决定，不属于 Component。

### 2.3 不同Controller之间的跳转关系

`PreparationController.OnUiOpen()` 完成备战基础刷新后先调用 `TryOpenNewPlayerGuide()`。若 `Hearthstone.NewPlayerGuide.PreparationBasicsV1` 尚未登记，则在备战 View 的父节点下打开 `NewPlayerGuideController`；只有打开成功后才登记触发标记。引导第一页默认显示，普通下一页按钮依次切到第二、第三页，上一页按钮在第一页禁用；第三页按钮显示“我知道了”，点击后触发关闭回调并关闭引导。灰色 `InputBlockingDimmer` 具有射线阻挡 Image，但没有 Button，因此点击弹窗外不会关闭。

首次引导的关闭回调先清空 `PreparationController` 保存的引导引用，再调用 `TryStartRewardReveal()`；本轮存在尚未展示的奖励时才开始原有抽牌动画。已经登记过引导时直接进入奖励判断。

融合卡槽下方的问号按钮同样通过 `UiApi.OpenUiController<NewPlayerGuideController>()` 打开既有引导，随后调用 `ShowPage(FusionPageIndex)` 直接把页面索引切到 `2`，显示第三页融合说明。该入口不检查或写入首次引导触发标记，并绑定只清空引导引用的关闭回调，因此关闭后返回当前融合页，不会调用奖励展示入口。备战 View 关闭时会移除回调并关闭仍存在的引导，不会错误启动奖励演出。

## 3. 所属GameStage

新手引导属于 `PreparationStage`，由 `PreparationController` 在 `PreparationUiScene` 的现有页面层级中按需打开。Prefab 通过 `PreLoadUiData` 登记 `NewPlayerGuideController → Ui/NewPlayerGuideView`，第一页继续通过既有 `BattleCardItemController → Ui/BattleCardItem` 预加载映射复用卡牌对象池。该界面不新增 UiScene、UiGroup 或场景资产。
