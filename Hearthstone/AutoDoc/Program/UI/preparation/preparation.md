# 备战界面程序文档

## 1. 核心数据来源

### 1.1 Component

| Component | 备战界面用途 |
| --- | --- |
| `RunStateSingletonRawComponent` | 提供已持有卡实例、三个出战槽和 `Revision`；卡池卡读取永久攻击、最大生命与关键词 |
| `PreparationSessionSingletonRawComponent` | 提供四个融合素材槽、当前奖励快照、融合批次状态和 `FusionRevision` |
| `PreparationContinueSingletonRawComponent` | 提供 Continue Button 的 Ready/Waiting 状态 |
| `RunProgressionSingletonRawComponent` | 仅用于页面状态日志中的当前战斗序号和 BattleStage 创建次数 |

### 1.2 Csv和ScriptableObject配置项

卡池、出战槽和融合槽按卡牌编号读取 `BattleCardCsvData` 的种类关联与原画资源键，再读取 `BattleCardTypeCsvData.DisplayName`。关键词文本由当前实例关键词和 `BattleKeywordCsvData` 的显示配置生成。界面当前不直接读取 ScriptableObject 配置。

## 2. UI界面

### 2.1 关联界面Controller列表

| Controller | View Prefab | 职责 |
| --- | --- | --- |
| `PreparationController` | `Assets/Resources/Ui/PreparationView.prefab` | 创建 99 个卡池条目、3 个出战槽和 4 个融合槽，刷新页签、合计、按钮与拖放结果 |
| `BattleCardItemController` | `Assets/Resources/Ui/BattleCardItem.prefab` | 同一预加载卡片同时服务战斗列表和备战卡池；备战模式显示蓝金玩家卡面、空态、素材角标并处理卡池拖拽 |
| `PreparationSlotItemController` | `Assets/Resources/Ui/PreparationSlotItem.prefab` | 显示出战槽空/占用状态并处理放置、替换和悬停高亮 |
| `PreparationFusionSlotItemController` | `Assets/Resources/Ui/PreparationFusionSlotItem.prefab` | 显示融合槽并处理池→槽、槽→槽、替换和悬停高亮 |

卡池 `Content` 使用 `UiList.ConstantSlot/Horizontal`，以 `200 × 288` 槽位按 7 列、15 行承载 `01~99`；共享的 `250 × 360` `BattleCardItem.prefab` 在备战模式使用 `0.8` 等比缩放，因此卡池显示尺寸正好为 `200 × 288`。出战槽卡和融合槽卡的根尺寸分别为 `220 × 316.8`、`190 × 273.6`，三处均保持战斗卡片的 `25:36` 比例。卡池不再创建或刷新独立 `PreparationCardItem` 视觉条目。

`BattleCardItemController → Ui/BattleCardItem`、`PreparationSlotItemController → Ui/PreparationSlotItem` 和 `PreparationFusionSlotItemController → Ui/PreparationFusionSlotItem` 均通过 Pre-load 映射交给 `UiList` 创建和回收。共享战斗卡 Prefab 内静态保存备战空态、素材角标、`UiDragable` 与 `UiInteractor`；Controller 按绑定上下文启用对应状态，战斗绑定时关闭全部备战交互。

`PreparationViewUiBuilder`、`BattleCardItemUiBuilder`、`PreparationSlotItemUiBuilder` 和 `PreparationFusionSlotItemUiBuilder` 分别维护各自 Prefab。`BattleCardItemUiBuilder` 同时预初始化共享卡片上的 BbxCommon 交互组件；页面与条目不在运行时拼装静态层级。

### 2.2 每个Controller监听的Component变量

| Controller | 监听来源 | 响应 |
| --- | --- | --- |
| `PreparationController` | `RunStateSingletonRawComponent.Revision` | 统一刷新卡池持有状态、永久数值、出战槽和融合结果 |
| `PreparationController` | `PreparationSessionSingletonRawComponent.FusionRevision` | 刷新融合槽、素材角标、表达式、合计和融合按钮 |
| `PreparationController` | `PreparationContinueSingletonRawComponent.State` | 切换 Continue Button 可交互状态与重复点击阻挡层 |

卡池条目在页面统一刷新时读取当前 Run/Preparation session；它不为备战数据额外挂监听。持有卡启用拖拽，未持有卡只保留编号和空态点击反馈。出战页通过 `RunCardRules.TryPlaceCard()` 提交槽位变化；融合页通过 `RunCardRules` 的素材与融合入口提交状态，不在 UI 中直接写玩法数据。

### 2.3 不同Controller之间的跳转关系

`PreparationUiScene` 创建 `PreparationController`，后者在三个 `UiList` 中创建共享卡池卡、出战槽和融合槽 Controller。“出战/融合”页签只切换两个操作区，卡池始终保留。Continue Button 调用游戏引擎的下一战入口；成功切换 StageGroup 后备战页和全部条目按 UI 生命周期关闭并回池，由下一场 `BattleUiScene` 再次从同一 `BattleCardItemController` 预加载池创建战斗卡。

## 3. 所属GameStage

备战界面属于 `PreparationStage`，使用 `PreparationUiScene`、`EPreparationUiGroup.Main` 和 `Assets/Resources/Ui/Preparation.asset`。`Assets/Scenes/Ui/Preparation.unity` 保存 connected `PreparationView.prefab` 实例及 `UiSceneExporter` 配置。此次变化只修改页面 Prefab 内部列表尺寸与共享条目类型，没有改变 UiGroup、DefaultShow、场景级 Position/Scale/Pivot 或导出路径，因此导出 Asset 保持不变。
