# 备战界面程序文档

## 1. 核心数据来源

### 1.1 Component

| Component | 备战界面用途 |
| --- | --- |
| `RunStateSingletonRawComponent` | 提供按卡号分组的全部持有卡副本、最多六个出战槽、当前已解锁槽位数和 `Revision`；卡池按副本读取永久攻击、最大生命、关键词、运行时等阶与表现来源卡号 |
| `PreparationSessionSingletonRawComponent` | 提供当前轮编号、动态摸牌快照、四个融合素材槽、融合批次状态和 `FusionRevision` |
| `PreparationContinueSingletonRawComponent` | 提供 Continue Button 的 Ready/Waiting 状态 |

### 1.2 Csv和ScriptableObject配置项

卡池、出战槽、融合槽和融合揭晓先按持有实例的 `PresentationCardNumber` 读取 `BattleCardCsvData` 的种类关联与原画资源键，再读取对应 `BattleCardTypeCsvData` 的显示名称；普通卡和双卡、三卡结果的表现编号等于实际卡号，四卡结果的表现编号由规则层在融合时按最高点数三张素材确定。实际结果卡号仍用于编号、持有与槽位身份，实例攻血、词条和等阶仍用于卡面数值、说明与边框。智能推荐卡面只读取素材实例。`BattleCardCsvData` 在 CSV 读取阶段已把排序后的 2～4 项基础类型公式登记为内存查询键；界面通过玩法评估与推荐查询结果读取素材编号和、合法组合及是否可融合，不自行推导玩法合法性。`99` 读取同表中的封印类型与 `FusionCard_099` 原画。关键词文本由当前实例关键词和 `BattleKeywordCsvData` 的显示配置生成。界面当前不直接读取 ScriptableObject 配置。

## 2. UI界面

### 2.1 关联界面Controller列表

| Controller | View Prefab | 职责 |
| --- | --- | --- |
| `PreparationController` | `Assets/Resources/Ui/PreparationView.prefab` | 在常规列表、推荐虚拟列表、备战奖励列表和融合揭晓列表中创建对象池条目，按编号与副本序号展开牌库，刷新页签、当前点数、剩余点数、融合与智能推荐按钮和拖放结果，维护全部推荐数据与视口附近的可见行，并逐帧驱动精确 99 的当前点数闪亮、奖励卡依次发放与收纳、素材收束、结果旋转缩放、全屏闪白和融合结果收纳状态 |
| `FusionRecommendationItemController` | `Assets/Resources/Ui/FusionRecommendationItem.prefab` | 表示一条推荐组合；持久复用四个共享卡片条目，按当前组合只显示 2～4 张横排素材卡面与素材已选标记，不创建结果卡条目，并把行右侧“选择”按钮回调交给页面 Controller |
| `BattleCardItemController` | `Assets/Resources/Ui/BattleCardItem.prefab` | 同一预加载卡片同时服务战斗列表、备战卡池、出战槽、融合槽、融合推荐、备战奖励和融合结果揭晓；推荐绑定显示运行实例卡面与素材标记，但禁用拖拽、投放和悬停输入，并把滚轮转发给推荐 ScrollRect；备战奖励直接绑定本轮奖励实例；揭晓素材绑定直接读取融合事务快照。奖励卡与融合结果卡只在等待确认时开启卡面悬停和词条说明 |

卡池 `Content` 使用 `UiList.ConstantSlot/Horizontal`，以 `220 × 316.8` 槽位按 7 列承载卡牌。页面打开时 `OwnedOnlyToggle` 默认开启；总览仍先沿内部卡号 `01~213` 计算显示编号与从 149 开始连续分配的传奇编号，再跳过 `RunStateSingletonRawComponent.HasCard()` 为假的编号，因此默认列表只创建当前已拥有卡牌和全部副本，并保持升序、同号连续且传奇编号不因筛选变化。玩家取消勾选后才创建完整编号总览；切换筛选仍复用原 `UiList`。每次切换筛选都会回收旧条目、按实际条目数重算行数和 Content 高度，再基于新高度调用 `UiList.RefreshLayout()` 重排全部现存条目，随后停止惯性并把滚动位置复位到顶部；因此勾选和取消筛选都不会沿用切换前的旧坐标或把卡片留在裁剪区外。共享的 `250 × 360` `BattleCardItem.prefab` 在卡池使用 `0.8` 等比缩放，对应 `200 × 288`；出战条目先以 `205 × 295.2` 列表槽位保持 `205 px` 中心距，再把可见卡位尺寸乘以 `0.88`，按约 `0.7216` 总缩放显示为 `180.4 × 259.776`，相邻可见卡位间保留约 `24.6 px` 空隙；融合槽继续按实际槽位得到 `0.76` 缩放和 `190 × 273.6` 尺寸。每次换绑先恢复单位缩放，再应用新上下文尺寸，因此从槽位回到卡池、战斗或融合揭晓时不会残留旧比例。卡池面板尺寸为 `1780 × 630`，顶部筛选行占用 `42 px`，内部滚动区为 `1650 × 510` 并下移 `10 px`，不创建固定摸牌提示或卡池容量标题。`CardPoolPanel/BluePanelPattern` 是位于 `ScrollRect` 之前的静态装饰层，尺寸为 `1600 × 500`；它使用 24 条无 Sprite 的 `Image` 线段组成 6 个空心菱形，并以 12 个小方形 `Image` 旋转形成菱点。线段颜色为浅蓝 `RGBA(0.72, 0.88, 1, 0.075)`，小点 Alpha 为 `0.05`，全部关闭 `raycastTarget`，因此不会参与卡池滚动、悬停或拖放命中。

`OwnedOnlyToggle` 的静态层级由 Builder 写入 Prefab：整行 `240 × 42` 的根 Image 位于卡池面板局部坐标 `(-770, 285)`，其左边缘与 `1780 px` 宽面板的左边缘同为 `-890`；该 Image 同时作为命中面并引用 `PreparationTabIdle` 蓝金窄横框，其内部包含 `38 × 38` 的方形 `PreparationTabSelected` 底板、TMP 浅金色对勾和“查看拥有”标签。两份文件都沿用已经无引用的旧页签资源文件名与 GUID，当前页签仍只使用 V2。View 只保存 Toggle 与标签引用，Controller 注册 `onValueChanged` 并维护页面生命周期内的筛选布尔值。Prefab 与 Controller 打开态均把筛选初始化为已勾选；页面关闭、重新打开时恢复为只看拥有。

卡池 `ScrollRect` 的 Prefab 基础 `scrollSensitivity` 为 `45`，`PreparationController.OnUiInit()` 在每个页面实例首次初始化时乘以 `1.5`，实际灵敏度为 `67.5`。共享卡片 Controller 实现 `IScrollHandler`；只有绑定为备战卡池条目时才把卡片根节点命中的滚轮事件显式转发给页面 `ScrollRect`。空槽内旧的透明点击子层关闭监听器和 `Graphic.raycastTarget`，不会再截获滚轮，因此鼠标位于已持有卡或空卡位上都可滚动，而战斗绑定、出战槽和融合槽不会启用该转发。页面不再监听 `ScrollRect.onValueChanged`，滚动过程不会逐帧打印页面状态诊断日志。

出战列表根尺寸为 `1260 × 320`，使用 `UiList.ConstantSlot/Horizontal` 和 `205 × 295.2` 固定布局步长，可容纳六槽上限；共享条目的空槽与占用卡面都按 `0.88` 填充比例缩小，因此步长之间存在可见空隙。出战操作区不再创建 `BattleSlotHeader`，没有“出战槽位”文字及其左右装饰线。`PreparationController` 只按 `RunStateSingletonRawComponent.UnlockedBattleSlotCount` 创建三至六个出战条目，尚未解锁的槽位不会生成；拖放仍由 `RunCardRules.TryPlaceCard()` 对当前解锁上限做权威校验。每轮进入页面前，`InitializePreparationRuntime` 已应用 `PreparationRoundStartupData` 中的累计槽位数与动态摸牌批次，因此同一 Prefab 会随轮次呈现不同出战上限，不显示固定摸牌数量标题。

三个列表都通过唯一的 `BattleCardItemController → Ui/BattleCardItem` Pre-load 映射交给 `UiList` 创建和回收。共享战斗卡 Prefab 内静态保存卡池未持有态、出战空槽、融合空槽、投放高亮、旧素材角标兼容对象、右上角“已出战”文字、`UiDragable` 与 `UiInteractor`；悬停和拖拽共用卡片根节点的 `CardBackground` 射线面与同一个 `UiEventListener`。卡池空槽的旧透明点击子层仍为序列化兼容对象，但运行时关闭监听器和射线命中。这些备战交互在 Prefab 中默认关闭。Controller 仅在“备战绑定且当前条目有卡”时开启同一根输入面的悬停与拖拽；融合页中已经放入素材槽的卡池条目按空态显示并关闭自身拖拽，但保留根节点滚轮。战斗绑定时关闭悬停、拖拽和全部备战状态。`UiDragable` 使用 `RectTransformUtility.ScreenPointToWorldPointInRectangle()` 把指针屏幕坐标换算到当前 Canvas 的 UI 平面，再驱动共享 View 的世界位置，因此 CanvasScaler 非 1 时仍保持按下点偏移。拖拽结束后，组件先把共享 View 从顶层归还原 Controller 父节点，再恢复拖拽开始时记录的 View 局部位置；`UiList.RefreshLayout()` 只负责排列其持有的外层 Controller，不再由业务 Controller 把释放点形成的 View 局部偏移重新写回。融合槽来源条目随后用 `RectTransformUtility.RectangleContainsScreenPoint()` 判断释放点是否仍位于 `FusionSlotList` 区域；区域外统一调用 `TryRemoveFusionMaterial()` 退回牌库，区域内则保留原槽回落或此前已经完成的槽间移动。融合槽来源不会再触发出战槽投放。素材清除与统一刷新发生在顶层拖拽节点归还之后，不会在 Pointer Up 回调中提前把仍在拖动的实例切换为空槽。旧的三个备战条目类型、Prefab 和 Builder 仅作为现有序列化资产的兼容壳保留，没有 Pre-load 映射，活跃页面不会创建或刷新它们。

卡池未持有态、融合页已选素材的卡池原位和未占用融合槽都由 `PreparationPoolEmptySlot.png` 完整覆盖共享卡根区域。该 `1024 × 1536` PNG 内部直接包含浅灰木板、稀疏横向拼缝、极细冷银边与蓝灰角饰，只有轮廓外为透明 Alpha；根节点 `CardBackground` 在空态也保持透明，不再从空槽图外侧露出一圈浅灰纯色。状态刷新先清空三种空态，再按当前绑定只打开一种；融合素材在融合槽区域外松手时，拖拽节点先归还原父节点，规则层再清除对应 Fusion session 槽并递增 `FusionRevision`，页面统一刷新后原卡面恢复，源融合槽切换为浅灰空态。真实卡面内容完成显示后再提交占用态，拖拽回落或同帧刷新不会同时保留空态和卡面。

卡池条目绑定到编号 `99` 时进入专用锁定态：不显示普通未持有空槽，读取 `FusionCard_099` 与“融合封印”名称，隐藏攻血徽章，以深灰共享框呈现；条目的 `UiInteractor`、拖拽和空位点击反馈保持禁用，只允许根输入面提供黄色悬停描边。编号 `100~213` 仍按普通卡位处理；`100~148` 融合获得后读取与卡号同名的独立 `FusionCard_100`～`FusionCard_148` 原画。`149~213` 为四卡传奇区，未持有时显示编号空槽；融合获得后保留四卡实际编号、实例属性、词条和传奇框，同时从实例的表现来源卡号读取点数最高三张素材所对应的三卡名称与原画。

卡片基础框、攻击者框和目标框统一引用中性银白 `CardFrame-v3`。共享卡面的 `ArtworkArea` 固定为居中的 `210 × 297`，不按导入 Texture 的宽高比缩放，因此普通卡、融合卡以及卡池、出战槽、融合槽、战斗列表都会得到相同的稍宽立绘构图和 `20 px` 卡面侧边距。备战已持有卡的基础框只读取 `RunCardInstanceData.Tier`，通过 `Image.color` 显示铜 `#B87333`、银 `#C0CCD8`、金 `#E7A93B` 或传奇紫 `#B25CFF`；卡片根输入监听器收到 Pointer Enter 后切换为黄 `#FFD230`，Pointer Exit 后恢复对应等阶色，同一监听器继续把 PointerDown、Drag 与 PointerUp 交给 `UiDragable`。换绑、回池或关闭交互时同步清理悬停标记，避免复用条目残留黄色。

`PreparationViewUiBuilder` 维护页面、蓝色底纹、筛选控件与三个列表尺寸。在全屏 `PreparationPageBackground` 之后创建静态 `ParchmentAgingOverlay`，引用与战斗页面相同的 `ParchmentAgingOverlay.png`；该层尺寸为 `1700 × 380`、位置为 `(0, -270)`，以 `18%` 整体 Alpha 覆盖上方羊皮纸且不接收射线。卡池浅色花纹和“查看拥有”控件同样由 Builder 固化在 Prefab，不由 Controller 运行时拼装。顶部 `TitleFrame` 为 `580 × 110`，其中“备战阶段”TMP 字号为 `46`、对齐为 `Center`，文字矩形相对框心上移 `2 px`。Builder 将 `BattleTab`、`FusionTab` 固定在木质幕布左上角的 `(215, -58)`、`(525, -58)`，并分别引用 `PreparationTabSelectedV2`、`PreparationTabIdleV2`；两者标签矩形均为 `250 × 70`，字号为 `31`、对齐为 `Center`，相对页签几何中心上移 `4 px` 以补偿 Sprite 的透明边距。出战列表锚点位于 `(0, -130)`，与页签和下方卡池框均保留间距；融合素材标题与 `FusionSlotList` 使用相同的左上锚点，位置分别为 `(425, -40)` 与 `(420, -150)`，相较此前整体上移 `30 px` 并保持 `110 px` 的纵向间距。Controller 切页时通过 `ResourceApi.LoadSprite()` 交换两态资源。Continue Button 只保存“继续”主文字，其 `250 × 64` 标签矩形使用 `Center` 对齐并相对按钮几何中心上移 `3 px`；按钮关闭 Navigation，Pressed 与 Selected 均复用透明边缘的 Idle Sprite，Highlighted 和 Disabled 分别使用悬停与等待 Sprite。View 不再保存奖励文字或辅助文字引用。页面内中文统一引用 `NotoSansSC-SemiBold Dynamic SDF`；`BattleCardItemUiBuilder` 也把共享卡面文字绑定到同一字体资产，在卡片右上角静态保存 `92 × 30` 的 `PreparationDeployedState`，以 `18 px`、Top Right 对齐和棕色 `#8B5226` 显示“已出战”，并让融合空槽引用与卡池一致的 `PreparationPoolEmptySlot.png`。同一 Builder 还在 `SkillArea` 最底层维护一个不接收射线的 `CardBasePattern` 静态 TMP 装饰层，以 `12%` Alpha 的浅金色稀疏纹样服务战斗、卡池、出战槽和融合槽。该 Builder 唯一维护四种上下文共用的卡片层级与 BbxCommon 交互组件。旧 Builder 入口只转调共享 Builder，不再生成独立卡面；页面与条目不在运行时拼装静态层级。

`PreparationViewUiBuilder` 还在页面最上层静态保存 `FusionRevealOverlay`。该层包含可阻挡射线且挂有关闭 Button 的灰色全屏遮罩、一个全屏 `UiList.Manual` 素材卡容器、`250 × 360` 的中央 `CardRoot`、未知正面、反向旋转 `180°` 的卡背、一个 `UiList.Manual` 结果卡容器，以及位于最上层且不接收射线的全屏白色闪光。`CardRoot` 不再创建 `FloatingShadow` 局部灰色矩形；未知正面与卡背的根 Image 分别读取 `FusionRevealQuestionFace.png` 和 `FusionRevealCardBack.png`，两者只额外叠加共享 `CardFrame-v3` 暖金边，不再用运行时纯色 Image、TMP 问号或菱形子节点拼装卡面。两张 Preparation UI 纹理由 Builder 的 `LoadSprite()` 统一校验为 Single Sprite、Alpha Is Transparency、无 Mipmap 和 Clamp。页面打开时从既有 `BattleCardItemController → Ui/BattleCardItem` 预加载映射创建一个结果卡条目；每次融合时，素材容器按融合事务快照创建 2～4 个临时条目。两类条目都由 `UiList` 统一创建、清理和回收，不运行时创建静态层级或自行管理对象池。

Builder 同时在 `FusionRevealOverlay` 的下一层静态保存 `RewardRevealOverlay`，保证融合揭晓始终位于其上方。奖励层由 `78%` Alpha 的灰色全屏 Image、关闭 Navigation 的全屏确认 Button、CanvasGroup、全屏 `RewardCardList/UiList.Manual`，以及局部坐标 `(0, 270)`、界面尺寸 `620 × 225` 的 `RewardTitle` Image 组成；标题读取 `PreparationRewardTitle.png`，保持宽高比并关闭射线。奖励层默认关闭，但 inactive 列表仍由 `UiViewBase` 预初始化进 `BbxUiItems`。View 只保存需要由运行时控制的遮罩、CanvasGroup、确认 Button 和列表引用；静态标题不增加 View 字段。Controller 通过既有 `BattleCardItemController → Ui/BattleCardItem` 预加载映射创建本轮奖励条目，关闭遮罩时统一由 `UiList.ClearItems()` 回池，不在运行时搭建卡面或自行管理对象池。

同一 Builder 在融合揭晓层之前静态保存 `FusionRecommendationOverlay`。该层由全屏深色射线遮罩、`1240 × 700` 羊皮纸面板、关闭按钮，以及带纵向滚动条的 `1060 × 560` 结果区组成；不创建标题和提示节点。面板直接读取战斗场景的 `BattleBoardBackground.png`，再以 `14%` Alpha 叠加共享 `ParchmentAgingOverlay.png`；ScrollRect 的射线 Image 完全透明，Viewport 仅保留 `RectMask2D`，不存在会渲染成大片白色的空 Sprite Image。结果区以 `UiList.Manual` 作为 ScrollRect Content，并静态保存中央“无可用组合”文字。`FusionRecommendationItemUiBuilder` 独立维护 `970 × 224` 推荐行 Prefab：内部 `UiList.ConstantSlot` 固定四个横向素材卡位，不包含结果卡节点，右侧保存 `156 × 78`“选择”按钮；该 View 通过预加载映射进入 BbxCommon 对象池。融合右侧 `FusionSumPanel` 是 `610 × 250` 的两列静态控制区：第一列 X=`-150`，三排 Y=`82/0/-82`，依次为 `280 × 72` 当前点数底框、`280 × 72` 剩余点数底框和 `216 × 68` 智能推荐按钮；第二列第一排 X=`150` 放置 `300 × 82` 融合按钮。两个数值底框横向复用 `PreparationPoolEmptySlot` 白色木纹 Sprite；每块底框保存一项左对齐黑色中文标签和一项右对齐数字，两个按钮复用同一组三态 Sprite。智能推荐按钮还静态保存同对象 `UiEventListener` 和默认隐藏的子节点 `Tooltip`：提示框为 `460 × 94`、相对按钮中心位于 `(354, 0)`，复用暖棕着色的 `BattleBoardBackground` 与深棕 Outline，文本与背景均关闭射线。

融合右侧面板在 Prefab 中保存当前点数标签、当前点数数字、剩余点数标签和剩余点数数字四项 TMP 引用。Controller 每次刷新只改写两项数字；负数直接表示已经超过目标，两项中文标签保持左对齐黑色。当前点数超过 `99` 时仅数字使用 `FusionOverTargetColor` 红色与粗斜体；精确等于 `99` 且当前处于融合页时，`OnUiUpdate()` 只让当前点数数字在精确目标绿与白色之间循环插值，并在 `1.0~1.05` 倍之间轻微缩放。剩余点数数字固定使用黑色粗体，不参与红色或闪光状态。切换页签、页面隐藏、关闭或编号和离开 `99` 时统一停止并恢复当前点数数字的单位缩放；页面重新显示时从权威评估结果恢复状态。融合按钮的 `interactable` 只读取 `FusionEvaluationData.CanFuse`，因此按钮 SpriteSwap 与 `TryFuse()` 使用同一套精确 99 及其他合法性判定。

智能推荐按钮在融合页始终可交互。按钮的 `UiEventListener` 在 Pointer Enter 时显示“智能寻找牌库中可以融合的组合”，Pointer Exit 时隐藏；点击打开推荐弹窗、切到出战页、页面隐藏或关闭时同样主动隐藏，避免模态层关闭后残留。点击后 Controller 调用 `RunCardRules.FindFusionRecommendations()`；空素材池查询全部合法组合，非空查询只保留包含全部当前素材的组合。Controller 保存完整 `FusionRecommendationData` 列表，其中结果卡号只供选择时重新校验，不交给推荐条目渲染；页面按 `236 px` 行距把 Content 扩展到全部结果高度，只创建“可见行数 + 1”的少量 `FusionRecommendationItemController`。滚动时依据 Content 的顶部偏移重绑定这些行并移动到对应逻辑索引，组合数量不会扩大活跃卡牌对象数。推荐行预先复用四个 `BattleCardItemController`，只显示当前组合实际使用的 2～4 张素材；已有素材打开 `PreparationMaterialSelectedState`，所有推荐卡关闭 `UiDragable`、`UiInteractor` 和悬停输入。结果为空时只显示中央“无可用组合”。每次打开都会停止惯性并把滚动位置复位到顶部。推荐遮罩在 Prefab 中默认关闭，但 `UiViewBase` 的 Editor 预初始化会包含 inactive 子层，将其 `FusionRecommendationList` 一并序列化进 `BbxUiItems`；页面打开时该列表因此正常执行 `IUiInit/IUiOpen`。选择组合写入融合槽并同步触发 `FusionRevision` 时，Controller 可以在同一调用链中关闭遮罩、通过 `UiList.ClearItems()` 回收推荐行，再完成统一刷新，不访问未初始化的列表内部状态。

点击某行“选择”后，页面把该行 `FusionRecommendationData` 交给 `RunCardRules.TryApplyFusionRecommendation()`，由规则层重新校验并一次性替换四个融合槽；UI 不逐槽写状态。成功或无变化后关闭弹窗，`FusionRevision` 监听刷新素材槽、卡池占位和点数。关闭按钮、切回出战页、页面隐藏/关闭、Run state 或其他 `FusionRevision` 变化同样会关闭弹窗并回收推荐行，避免展示过期组合。

融合规则返回 `Applied` 后同时提供 `FusionTransactionSnapshot`。`PreparationController` 通过 `BindFusionReveal()` 把刚生成的实际卡号绑定到结果条目，共享卡片 Controller 再从持有实例读取表现来源卡号；四卡揭晓因此显示最高点数三张对应的三卡名称与原画，同时保留实际编号、传奇框、四卡攻血和词条。素材条目通过 `BindFusionMaterialReveal()` 读取事务快照中的卡号、攻血和词条，在素材已经被规则层消耗后仍能还原融合前卡面。

页面在 `OnUiUpdate()` 中按 `deltaTime × 0.8` 驱动逐帧时间轴。逻辑时长由淡入 `0.24 s`、素材等待 `0.16 s`、素材收束 `0.95 s`、旋转前延迟 `0.18 s`、旋转 `2.35 s` 和全屏闪白 `0.48 s` 组成，实际自动演出约为 `5.15 s`。2～4 张素材按 `340 px` 水平间距铺开，带 `±42 px` 高低差与按中心索引计算的轻微旋角；脚本把各卡 `anchoredPosition` 插值到 `(0, 0)`，同时从 `0.78` 倍缩到 `0.05` 倍并转正。随后中央卡根从 `0.12` 倍放大到 `1.28` 倍、缩回 `0.82` 倍，再放大到动态结果尺寸；结果尺寸取 `2` 倍与“遮罩高度的三分之二除以卡高”两者较大值。Y 轴旋转 `720°`，每圈按局部角度在封印面与反向卡背之间切换。旋转结束后全屏白层的 Alpha 使用正弦曲线升降，在闪白峰值启用真实结果卡面。

揭晓开始时通过 `AudioApi.Play("card-shuffle", options)` 播放约 `3.063 s` 的卡牌翻动音，初始音量 `0.55`、优先级 `96`；真实结果卡面在全屏闪白峰值首次出现的单帧通过防重复标记播放 `highUp` 上扬提示音，时长约 `0.549 s`、初始音量 `0.72`、优先级 `80`。两者使用 `UiFusionReveal` 分组、各自的并发键和 `MaxConcurrent = 1`；Controller 保存播放句柄，并在页面隐藏、关闭、重新开始或自动演出结束时调用公开 `AudioApi.Stop()`，异步加载期间停止同样安全。

自动演出结束后 Controller 停止逐帧更新时间轴，但不关闭遮罩；此时开启遮罩 Button 与 CanvasGroup 交互，并只为结果卡开启共享的悬停输入，因此关键词 Tooltip 会继续按普通卡面规则重挂到 Canvas 顶层。卡面输入层自身实现 Pointer 事件，会阻止点击继续命中父级 Button；卡面以外的点击才由全屏 Button 调用复位。复位会清理素材条目、关闭结果悬停、恢复旋转/缩放/位置/显隐与音效标记。页面不创建继续提示文字。全屏遮罩在自动演出和等待关闭期间始终阻挡页签、拖放、继续和重复融合输入。

结果卡面外点击不再立即复位，而是进入 `0.36 s` 的收纳段：Controller 从当前动态结果位置与缩放开始，用二次 Ease Out 把卡根移动到遮罩局部坐标 `x = 0`、刚好完全离开遮罩下边缘的位置，并把等比缩放精确收至 `0.3`；到达端点后才清理条目和关闭遮罩。收纳开始时播放 `handleSmallLeather`，音量 `0.68`、优先级 `82`、`MaxConcurrent = 1`。

页面打开或重新显示时，Controller 只在 `PreparationSessionSingletonRawComponent.WasNewlyApplied` 为真、奖励快照非空且当前批次尚未确认时启动备战奖励展示。奖励状态机依次为 `Dealing`、`AwaitingConfirm`、`Pocketing`：每张卡从遮罩底部 `x = 0`、`0.3` 倍开始，以 `0.14 s` 间隔依次启用，并在 `0.3 s` 内移动到中央横排的目标位置和 `0.82` 倍；横排卡间距为 `280 px`。最后一张到位后才开启全屏确认 Button 和各卡的标准悬停输入，卡面输入自身阻止事件继续命中父级 Button，因此只有卡面外点击会确认。确认后每张卡再按 `0.11 s` 间隔依次用二次 Ease Out 在 `0.34 s` 内滑向遮罩底部 `x = 0` 并缩至 `0.3` 倍；到达端点的条目立即隐藏，全部完成后记录已确认批次并清空回池。页面隐藏或关闭时直接复位状态、关闭遮罩、回收条目并停止该组音效。

每张奖励卡开始滑入时通过 `AudioApi.Play("card-place-1", options)` 播放约 `0.689 s` 的卡牌落位声，音量 `0.5`、优先级 `104`、`MaxConcurrent = 3`、并发音量衰减 `0.72`；每张奖励卡开始收纳时播放约 `0.338 s` 的 `handleSmallLeather`，音量 `0.58`、优先级 `92`、`MaxConcurrent = 3`、并发音量衰减 `0.7`。奖励发牌、奖励收纳与融合结果收纳共用 `UiPreparationCardAnimation` 分组，但使用独立并发键；重新开始、页面隐藏/关闭或状态复位时通过 `AudioApi.StopGroup()` 统一停止，异步加载期间同样安全。

### 2.2 每个Controller监听的Component变量

| Controller | 监听来源 | 响应 |
| --- | --- | --- |
| `PreparationController` | `RunStateSingletonRawComponent.Revision` | 关闭可能过期的智能推荐弹窗并统一刷新卡池持有状态、永久数值、出战槽和融合结果；比较每个编号的副本数快照，任意模式下副本数变化都会按最新条目数重建卡池 |
| `PreparationController` | `PreparationSessionSingletonRawComponent.FusionRevision` | 关闭可能过期的智能推荐弹窗，刷新融合槽、融合素材在卡池中的浅灰原位、当前点数、剩余点数、精确 99 闪亮/超额红色状态、融合按钮和推荐按钮 |
| `PreparationController` | `PreparationContinueSingletonRawComponent.State` | 切换 Continue Button 可交互状态与重复点击阻挡层 |

全部备战卡片条目在页面统一刷新时读取当前 Run/Preparation session；它们不为备战数据额外挂监听。卡池重建时先按卡号升序遍历，再为 `GetCardCopyCount()` 返回的每个副本创建同号条目并传入连续 `copyIndex`；未持有编号只创建一个空态条目，所以同编号副本天然连续，后续编号与 Content 行数随实际条目数顺延。“查看拥有”模式跳过零副本编号，但保留全部副本。卡池持有卡和被占用的槽位启用悬停与拖拽，未持有卡保留编号与浅灰空态但不启用独立点击层，99 号只显示锁定态，空出战/融合槽仍启用投放响应但不启用卡牌悬停。卡池条目按副本序号读取各自 `RunCardInstanceData`，而出战槽和融合槽继续按卡号使用首张副本并保持原有同号唯一选择规则。卡池条目遍历 `BattleSlotCardNumbers` 派生出战状态：已出战卡仍显示完整卡面，只在右上角开启棕色“已出战”；移槽或被替换后随 `Revision` 刷新。融合槽来源在 `FusionSlotList` 区域外松手时由既有 `TryRemoveFusionMaterial()` 清除 session 槽后恢复卡面，区域内的原槽回落或槽间移动不会被误判为退回。出战页通过 `RunCardRules.TryPlaceCard()` 提交卡池/出战槽来源的槽位变化；融合页通过 `RunCardRules` 的素材与融合入口提交状态，不在 UI 中直接写玩法数据。右侧数值面板只显示当前编号和与距 99 的差值；按钮是否可用仍由规则层同时决定素材数、精确编号和、配方、结果持有状态与材料合法性。

### 2.3 不同Controller之间的跳转关系

`PreparationUiScene` 创建 `PreparationController`，后者在卡池、出战槽、融合槽、推荐结果、备战奖励和融合揭晓等 `UiList` 中创建对象池 Controller。推荐结果先由 `FusionRecommendationItemController` 表示一行，再在行内复用共享卡片 Controller。新奖励批次在页面可操作前先打开奖励模态层，玩家确认后卡牌依次收纳并返回备战页；“出战/融合”页签只切换两个操作区，卡池始终保留。智能推荐是同一页面内部的模态遮罩，关闭或选择后返回融合操作区，不打开其他页面；融合揭晓层在融合成功后显示，并在自动动画完成后继续停留，直到玩家点击结果卡外区域触发收纳动画，结果卡离开屏幕后才返回融合操作区。Continue Button 调用游戏引擎的下一战入口；成功切换 StageGroup 后备战页和全部条目按 UI 生命周期关闭并回池，由下一场 `BattleUiScene` 再次从共享预加载池创建战斗卡。

## 3. 所属GameStage

备战界面属于 `PreparationStage`，使用 `PreparationUiScene`、`EPreparationUiGroup.Main` 和 `Assets/Resources/Ui/Preparation.asset`。`Assets/Scenes/Ui/Preparation.unity` 保存 connected `PreparationView.prefab` 实例及 `UiSceneExporter` 配置。当前页签、按钮、字体、卡池基础尺寸和融合右侧两列布局位于页面 Prefab，卡池 Content 高度与融合当前点数/剩余点数由既有 Controller 在打开和刷新时更新；这些变化没有改变 UiGroup、DefaultShow、场景级 Position/Scale/Pivot 或导出路径，因此导出 Asset 保持不变。
