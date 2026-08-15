# 备战界面程序文档

## 1. 核心数据来源

### 1.1 Component

| Component | 备战界面用途 |
| --- | --- |
| `RunStateSingletonRawComponent` | 提供已持有卡实例、三个出战槽和 `Revision`；卡池卡读取永久攻击、最大生命、关键词与运行时等阶 |
| `PreparationSessionSingletonRawComponent` | 提供四个融合素材槽、当前奖励快照、融合批次状态和 `FusionRevision` |
| `PreparationContinueSingletonRawComponent` | 提供 Continue Button 的 Ready/Waiting 状态 |

### 1.2 Csv和ScriptableObject配置项

卡池、出战槽和融合槽按卡牌编号读取 `BattleCardCsvData` 的种类关联、原画资源键和 `FusionRecipeTypeIds`，再读取 `BattleCardTypeCsvData.DisplayName`。`BattleCardCsvData` 在 CSV 读取阶段已把排序后的 2～4 项基础类型公式登记为内存查询键，界面通过玩法评估结果取得 `100~213` 的具体结果卡号和名称；`99` 读取同表中的封印类型与 `FusionCard_099` 原画。关键词文本由当前实例关键词和 `BattleKeywordCsvData` 的显示配置生成。界面当前不直接读取 ScriptableObject 配置。

## 2. UI界面

### 2.1 关联界面Controller列表

| Controller | View Prefab | 职责 |
| --- | --- | --- |
| `PreparationController` | `Assets/Resources/Ui/PreparationView.prefab` | 在常规列表和融合揭晓列表中统一创建 `BattleCardItemController`，刷新页签、融合提示、按钮与拖放结果，并逐帧驱动成功揭晓动画 |
| `BattleCardItemController` | `Assets/Resources/Ui/BattleCardItem.prefab` | 同一预加载卡片同时服务战斗列表、备战卡池、出战槽、融合槽和融合结果揭晓；按绑定模式切换边框颜色、悬停、空态、融合素材源占位、“已出战”状态、拖放来源与投放目标，揭晓绑定关闭交互并只显示结果卡内容 |

卡池 `Content` 使用 `UiList.ConstantSlot/Horizontal`，以 `220 × 316.8` 槽位按 7 列承载卡牌。页面打开时默认按内部卡号遍历并创建 `01~213` 的 213 个条目；传奇类型在总览循环中从 149 开始动态分配连续显示编号；卡池左上角 `OwnedOnlyToggle` 默认关闭，切换为开启后仍先沿完整总览计算显示编号，再跳过 `RunStateSingletonRawComponent.HasCard()` 为假的编号，因此筛选结果保持升序且传奇编号不会因筛选重新变化并复用原 `UiList`。每次切换筛选都会回收旧条目、按实际条目数重算行数和 Content 高度，再基于新高度调用 `UiList.RefreshLayout()` 重排全部现存条目，随后停止惯性并把滚动位置复位到顶部；因此勾选和取消筛选都不会沿用切换前的旧坐标或把卡片留在裁剪区外。共享的 `250 × 360` `BattleCardItem.prefab` 在卡池、出战槽和融合槽分别使用 `0.8`、`0.88`、`0.76` 等比缩放，对应 `200 × 288`、`220 × 316.8`、`190 × 273.6`，并保持 `25:36` 比例。卡池面板尺寸为 `1780 × 630`，顶部筛选行占用 `42 px`，内部滚动区为 `1650 × 510` 并下移 `10 px`，不创建奖励提示或卡池容量标题。`CardPoolPanel/BluePanelPattern` 是位于 `ScrollRect` 之前的静态装饰层，尺寸为 `1600 × 500`；它使用 24 条无 Sprite 的 `Image` 线段组成 6 个空心菱形，并以 12 个小方形 `Image` 旋转形成菱点。线段颜色为浅蓝 `RGBA(0.72, 0.88, 1, 0.075)`，小点 Alpha 为 `0.05`，全部关闭 `raycastTarget`，因此不会参与卡池滚动、悬停或拖放命中。

`OwnedOnlyToggle` 的静态层级由 Builder 写入 Prefab：整行 `240 × 42` 的根 Image 位于卡池面板局部坐标 `(-770, 285)`，其左边缘与 `1780 px` 宽面板的左边缘同为 `-890`；该 Image 同时作为命中面并引用 `PreparationTabIdle` 蓝金窄横框，其内部包含 `38 × 38` 的方形 `PreparationTabSelected` 底板、TMP 浅金色对勾和“查看拥有”标签。两份文件都沿用已经无引用的旧页签资源文件名与 GUID，当前页签仍只使用 V2。View 只保存 Toggle 与标签引用，Controller 注册 `onValueChanged` 并维护页面生命周期内的筛选布尔值。页面关闭、重新打开时筛选恢复为未勾选。

卡池 `ScrollRect` 的 Prefab 基础 `scrollSensitivity` 为 `45`，`PreparationController.OnUiInit()` 在每个页面实例首次初始化时乘以 `1.5`，实际灵敏度为 `67.5`。共享卡片 Controller 实现 `IScrollHandler`；只有绑定为备战卡池条目时才把卡片根节点命中的滚轮事件显式转发给页面 `ScrollRect`。空槽内旧的透明点击子层关闭监听器和 `Graphic.raycastTarget`，不会再截获滚轮，因此鼠标位于已持有卡或空卡位上都可滚动，而战斗绑定、出战槽和融合槽不会启用该转发。页面不再监听 `ScrollRect.onValueChanged`，滚动过程不会逐帧打印页面状态诊断日志。

三个列表都通过唯一的 `BattleCardItemController → Ui/BattleCardItem` Pre-load 映射交给 `UiList` 创建和回收。共享战斗卡 Prefab 内静态保存卡池未持有态、出战空槽、融合空槽、投放高亮、旧素材角标兼容对象、右上角“已出战”文字、`UiDragable` 与 `UiInteractor`；悬停和拖拽共用卡片根节点的 `CardBackground` 射线面与同一个 `UiEventListener`。卡池空槽的旧透明点击子层仍为序列化兼容对象，但运行时关闭监听器和射线命中。这些备战交互在 Prefab 中默认关闭。Controller 仅在“备战绑定且当前条目有卡”时开启同一根输入面的悬停与拖拽；融合页中已经放入素材槽的卡池条目按空态显示并关闭自身拖拽，但保留根节点滚轮和作为回拖目标的投放响应。战斗绑定时关闭悬停、拖拽和全部备战状态。`UiDragable` 把共享 View 从顶层归还原父节点后，页面先调用三个 `UiList.RefreshLayout()` 计算实际槽位；条目随后读取刷新后的局部坐标，并通过公开 `UiTransformSetter.PosWrapper` 以优先级 `3000` 提交同一坐标的单帧请求，覆盖拖拽组件排队的旧位置。它不再写死局部零点，因此 7 列布局中央的第 4 列不会额外叠入被拖拽卡。旧的三个备战条目类型、Prefab 和 Builder 仅作为现有序列化资产的兼容壳保留，没有 Pre-load 映射，活跃页面不会创建或刷新它们。

卡池未持有态、融合页已选素材的卡池原位和未占用融合槽都由 `PreparationPoolEmptySlot.png` 完整覆盖共享卡根区域。该 `1024 × 1536` PNG 内部直接包含浅灰木板、稀疏横向拼缝、极细冷银边与蓝灰角饰，只有轮廓外为透明 Alpha；根节点 `CardBackground` 在空态也保持透明，不再从空槽图外侧露出一圈浅灰纯色。状态刷新先清空三种空态，再按当前绑定只打开一种；融合素材从槽位拖回卡池时，规则层先清除对应 Fusion session 槽并递增 `FusionRevision`，页面统一刷新后原卡面恢复，源融合槽切换为浅灰空态。真实卡面内容完成显示后再提交占用态，拖拽回落或同帧刷新不会同时保留空态和卡面。

卡池条目绑定到编号 `99` 时进入专用锁定态：不显示普通未持有空槽，读取 `FusionCard_099` 与“融合封印”名称，隐藏攻血徽章，以深灰共享框呈现；条目的 `UiInteractor`、拖拽和空位点击反馈保持禁用，只允许根输入面提供黄色悬停描边。编号 `100~213` 仍按普通卡位处理；`100~148` 融合获得后读取与卡号同名的独立 `FusionCard_100`～`FusionCard_148` 原画，`149~213` 为四卡传奇区，未持有时显示编号空槽，融合获得后显示其独立名称、原画复用项和实例属性。

卡片基础框、攻击者框和目标框统一引用中性银白 `CardFrame-v3`。共享卡面的 `ArtworkArea` 固定为居中的 `210 × 297`，不按导入 Texture 的宽高比缩放，因此普通卡、融合卡以及卡池、出战槽、融合槽、战斗列表都会得到相同的稍宽立绘构图和 `20 px` 卡面侧边距。备战已持有卡的基础框只读取 `RunCardInstanceData.Tier`，通过 `Image.color` 显示铜 `#B87333`、银 `#C0CCD8`、金 `#E7A93B` 或传奇紫 `#B25CFF`；卡片根输入监听器收到 Pointer Enter 后切换为黄 `#FFD230`，Pointer Exit 后恢复对应等阶色，同一监听器继续把 PointerDown、Drag 与 PointerUp 交给 `UiDragable`。换绑、回池或关闭交互时同步清理悬停标记，避免复用条目残留黄色。

`PreparationViewUiBuilder` 维护页面、蓝色底纹、筛选控件与三个列表尺寸。在全屏 `PreparationPageBackground` 之后创建静态 `ParchmentAgingOverlay`，引用与战斗页面相同的 `ParchmentAgingOverlay.png`；该层尺寸为 `1700 × 380`、位置为 `(0, -270)`，以 `18%` 整体 Alpha 覆盖上方羊皮纸且不接收射线。卡池浅色花纹和“查看拥有”控件同样由 Builder 固化在 Prefab，不由 Controller 运行时拼装。顶部 `TitleFrame` 为 `580 × 110`，其中“备战阶段”TMP 字号为 `46`、对齐为 `Center`，文字矩形相对框心上移 `2 px`。Builder 将 `BattleTab`、`FusionTab` 固定在木质幕布左上角的 `(215, -58)`、`(525, -58)`，并分别引用 `PreparationTabSelectedV2`、`PreparationTabIdleV2`；两者标签矩形均为 `250 × 70`，字号为 `31`、对齐为 `Center`，相对页签几何中心上移 `4 px` 以补偿 Sprite 的透明边距。出战列表锚点位于 `(0, -130)`，与页签和下方卡池框均保留间距。Controller 切页时通过 `ResourceApi.LoadSprite()` 交换两态资源。Continue Button 只保存“继续”主文字，其 `250 × 64` 标签矩形使用 `Center` 对齐并相对按钮几何中心上移 `3 px`；View 不再保存奖励文字或辅助文字引用。页面内中文统一引用 `NotoSansSC-SemiBold Dynamic SDF`；`BattleCardItemUiBuilder` 也把共享卡面文字绑定到同一字体资产，在卡片右上角静态保存 `92 × 30` 的 `PreparationDeployedState`，以 `18 px`、Top Right 对齐和棕色 `#8B5226` 显示“已出战”，并让融合空槽引用与卡池一致的 `PreparationPoolEmptySlot.png`。同一 Builder 还在 `SkillArea` 最底层维护一个不接收射线的 `CardBasePattern` 静态 TMP 装饰层，以 `12%` Alpha 的浅金色稀疏纹样服务战斗、卡池、出战槽和融合槽。该 Builder 唯一维护四种上下文共用的卡片层级与 BbxCommon 交互组件。旧 Builder 入口只转调共享 Builder，不再生成独立卡面；页面与条目不在运行时拼装静态层级。

`PreparationViewUiBuilder` 还在页面最上层静态保存 `FusionRevealOverlay`。该层包含可阻挡射线的灰色遮罩、`250 × 360` 的中央 `CardRoot`、封印正面、反向旋转 `180°` 的蓝金卡背、裁切闪光和一个 `UiList.Manual` 结果卡容器。页面打开时从既有 `BattleCardItemController → Ui/BattleCardItem` 预加载映射创建一个结果卡条目，关闭或回收时仍由 `UiList` 统一释放，不运行时创建静态层级或自行管理对象池。

融合规则返回 `Applied` 后，`PreparationController` 通过 `BindFusionReveal()` 把刚生成的卡号绑定到结果条目，并在 `OnUiUpdate()` 中驱动逐帧时间轴。逻辑时长仍由淡入 `0.18 s`、入场延迟 `0.24 s`、旋转 `1.5 s`、闪光 `0.55 s`、停留 `0.8 s` 和淡出 `0.3 s` 组成，但每帧只按 `deltaTime × 0.8` 推进，因此实际总播放时间约为 `4.24 s`。卡根从 `0.72` 倍、纵向 `-35` 的位置悬浮放大到 `1.28` 倍，再在旋转进度达到 `75%`、真实结果出现以前平滑缩回 `1` 倍；过程中持续轻微上下浮动。Y 轴从 `0°` 旋转到 `360°`，`90°~270°` 显示反向卡背，`270°` 后才启用真实结果卡面。完整转回正面后，闪光从卡面 X=`-260` 移到 `260`，Alpha 使用正弦曲线淡入淡出。

揭晓开始时通过 `AudioApi.Play("card-shuffle", options)` 播放约 `3.063 s` 的卡牌翻动音，初始音量 `0.55`、优先级 `96`；真实结果卡面首次出现的单帧通过防重复标记播放 `highUp` 上扬提示音，时长约 `0.549 s`、初始音量 `0.72`、优先级 `80`。两者使用 `UiFusionReveal` 分组、各自的并发键和 `MaxConcurrent = 1`；Controller 保存播放句柄，并在页面隐藏、关闭、重新开始或时间轴结束复位时调用公开 `AudioApi.Stop()`，异步加载期间停止同样安全。全屏遮罩在播放期阻挡页签、拖放、继续和重复融合输入；复位时同时恢复旋转、缩放、位置、显隐和音效触发状态。

### 2.2 每个Controller监听的Component变量

| Controller | 监听来源 | 响应 |
| --- | --- | --- |
| `PreparationController` | `RunStateSingletonRawComponent.Revision` | 统一刷新卡池持有状态、永久数值、出战槽和融合结果；筛选开启时先比较无分配的拥有集合快照，只在拥有成员发生变化时重建卡池 |
| `PreparationController` | `PreparationSessionSingletonRawComponent.FusionRevision` | 刷新融合槽、融合素材在卡池中的浅灰原位、表达式、公式命中结果和融合按钮 |
| `PreparationController` | `PreparationContinueSingletonRawComponent.State` | 切换 Continue Button 可交互状态与重复点击阻挡层 |

全部备战卡片条目在页面统一刷新时读取当前 Run/Preparation session；它们不为备战数据额外挂监听。卡池持有卡和被占用的槽位启用悬停与拖拽，未持有卡保留编号与浅灰空态但不启用独立点击层，99 号只显示锁定态，空出战/融合槽仍启用投放响应但不启用卡牌悬停。卡池条目遍历 `BattleSlotCardNumbers` 派生出战状态：已出战卡仍显示完整卡面，只在右上角开启棕色“已出战”；移槽或被替换后随 `Revision` 刷新，不产生卡池空槽。融合页中命中 `FusionSlotCardNumbers` 的素材则隐藏卡面并打开浅灰卡池空态；拖回卡池由既有 `TryRemoveFusionMaterial()` 清除 session 槽后恢复卡面。出战页通过 `RunCardRules.TryPlaceCard()` 提交槽位变化；融合页通过 `RunCardRules` 的素材与融合入口提交状态，不在 UI 中直接写玩法数据。结果文本在命中公式时显示结果编号与名称；四素材命中时额外显示“传奇”提示，无公式、结果已持有和素材不足分别显示对应阻断反馈。

### 2.3 不同Controller之间的跳转关系

`PreparationUiScene` 创建 `PreparationController`，后者在四个 `UiList` 中创建同一种共享卡片 Controller，并分别绑定为卡池、出战槽、融合槽或融合结果揭晓。“出战/融合”页签只切换两个操作区，卡池始终保留；融合揭晓层只在融合成功后的时间轴内显示。Continue Button 调用游戏引擎的下一战入口；成功切换 StageGroup 后备战页和全部条目按 UI 生命周期关闭并回池，由下一场 `BattleUiScene` 再次从同一 `BattleCardItemController` 预加载池创建战斗卡。

## 3. 所属GameStage

备战界面属于 `PreparationStage`，使用 `PreparationUiScene`、`EPreparationUiGroup.Main` 和 `Assets/Resources/Ui/Preparation.asset`。`Assets/Scenes/Ui/Preparation.unity` 保存 connected `PreparationView.prefab` 实例及 `UiSceneExporter` 配置。当前页签、按钮、字体和卡池基础尺寸位于页面 Prefab，卡池 Content 高度与公式结果提示由既有 Controller 在打开和刷新时更新；这些变化没有改变 UiGroup、DefaultShow、场景级 Position/Scale/Pivot 或导出路径，因此导出 Asset 保持不变。
