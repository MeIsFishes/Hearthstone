# 战斗界面程序文档

## 1. 核心数据来源

### 1.1 Component

| Component | 战斗界面用途 |
| --- | --- |
| `BattleSessionSingletonRawComponent` | 提供动态玩家卡牌 Entity、三个敌方 Entity、本轮编号、是否最终轮、延迟后的正式战斗结果、当前攻击者与当前目标，以及攻击表现序列号、活动标记和统一时钟 |
| `BattleCardRawComponent` | 提供实际卡牌编号与类型、表现来源编号与类型、阵营、运行时等阶、攻击力、当前生命、存活状态、战斗词条，以及本场固定的入场攻击与入场生命基准 |

### 1.2 Csv和ScriptableObject配置项

`BattleCardItemController` 根据 `BattleCardRawComponent.PresentationCardNumber` 通过 `DataApi` 读取 `BattleCardCsvData` 的原画资源键，再按 `PresentationCardTypeId` 读取 `BattleCardTypeCsvData` 的显示名称与攻击帧图集键，并从 `BattleKeywordCsvData` 读取实际词条的名称、说明和显示顺序。怪物原画和攻击图集都通过 `ResourceApi.LoadSprite()` 加载；攻击音效键列表、音效延迟列表、音量列表与闪红延迟列表由 `BattleSystem` 从同一表现类型配置读取，每个音效通过 `AudioApi.Play()` 单独播放。普通卡及双卡、三卡结果的表现身份与实际身份一致；四卡结果使用融合时点数最高三张素材对应的三卡表现。左上角编号仍直接使用 Component 中的实际 `CardNumber`，攻血、词条与传奇等阶同样保留四卡结果数据；基础卡框只读取 Component 中的 `Tier`，不根据阵营或表现配置自行推导。

当前界面未读取 ScriptableObject 配置。

## 2. UI界面

### 2.1 关联界面Controller列表

| Controller | View Prefab | 职责 |
| --- | --- | --- |
| `BattleController` | `Assets/Resources/Ui/BattleView.prefab` | 创建双方卡牌列表；正式胜负出现时暂停缩放时间，以未缩放时间驱动结算横幅并切换对应的带字 Sprite，在横幅入场后等待屏幕任意位置点击，再进入下一轮备战或主菜单 |
| `BattleCardItemController` | `Assets/Resources/Ui/BattleCardItem.prefab` | 绑定战斗 Entity 时刷新等阶卡框、名称、词条及其悬浮说明、原画、编号、攻血及其相对入场基准的颜色、嘲讽盾牌轮廓、高亮，以及带做旧暗红断剑的死亡卡面遮罩，并播放伤害浮字、数值上升过渡、冲锋号角与远射弓箭反馈；同一 Controller/Prefab 也可绑定备战卡池编号、出战槽、融合槽、融合揭晓或融合推荐卡，并确保不同上下文换绑时完整清理状态 |

`BattleCardItemController` 通过唯一 Pre-load 映射 `Hearthstone.BattleCardItemController → Ui/BattleCardItem` 由 `UiList.AddItem<BattleCardItemController>()` 创建和回收。战斗双方列表与备战卡池、出战槽、融合槽使用同一映射和同一对象池；战斗 Entity 仅作为玩法数据句柄，不作为 UI View 或 Controller。

每个池化条目在初始化时只创建一次原生 `RawImage` 子层 `AttackFrameEffect`，之后所有攻击复用该对象。攻击开始时，攻击者沿竖向朝对方阵列使用正弦曲线前拱并退回原锚点；目标按 `4 × 2` UV 网格播放八帧透明图集，并在 `HitDelays` 的每个时点叠加一次红色脉冲；多个脉冲重叠时取当前最强值。共享攻击表现时钟当前按 `0.8` 倍速推进，因此前拱、图集帧、各段红色脉冲、首段伤害与战斗 System 按 `AttackAudioDelays` 触发的多段音效保持同步。每个音效使用同索引 `AttackAudioVolumes`，并使用 `Combat` 分组和 `BattleCardAttack` 并发键，最多同时三声且按并发数量衰减音量。换绑、回池、表现结束或配置缺失时会恢复原位置、原画颜色并清空图集，避免复用残影。

Prefab 静态持有三组附加反馈层：生命徽章上方的 `92 × 70` 黄色爆炸状伤害底板与红 `#D22020` 深红描边 TMP 数字、卡面上中偏左的 `96 × 96` 冲锋号角、卡面上中偏右的 `96 × 96` 远射弓箭。生命下降时 Controller 按监听前后值差显示 `-伤害值`，底板和数字向上移动 `54 px` 并渐隐；新的攻击表现序列只在当前卡牌是攻击者且具备对应词条时启动图标，图标向上移动 `84 px` 并渐隐，同一张卡可以同时显示两种词条反馈。伤害与词条反馈都在独立计时器上运行，但每帧同样乘以 `BattleRules.AttackPresentationPlaybackSpeed`，当前整体为 `0.8` 倍速。换绑、隐藏和回池时会还原位置、透明度、文字与显隐状态。

`BattleView.prefab` 的根尺寸为 `1920 × 1080`，`BoardBackground` 拉伸覆盖完整界面并使用战斗专用的 `BattleBoardBackgroundAged.png`；该底图内侧直接带有可见水渍、磨痕、短划痕、边缘积垢及右下少量黑褐墨点与短擦痕，未影响仍被其他界面复用的原版 `BattleBoardBackground.png`。其后一层静态 `ParchmentAgingOverlay` 引用 `ParchmentAgingOverlay.png`，以 `42%` Alpha 覆盖内侧羊皮纸区域且不接收射线；该 Sprite 与备战页面共用，但战斗叠加强度高于备战的 `18%`。`UpperLeftDagger` 使用 `BattleCornerDagger.png`，锚定左上角、尺寸 `420 × 630`、位置 `(220, -250)`，通过 `localScale.x = -1` 让刀尖朝向左下，主体几乎完整露出，由根画布上缘裁去一小段手柄顶部；`LowerRightQuillStamp` 使用 `BattleCornerQuillStamp.png`，锚定右下角、尺寸 `320 × 213`、位置 `(-100, 110)`，组合图内的羽毛笔相对印章向右下微移。两张透明装饰都不接收射线，并在敌我卡牌列表之前创建，使卡牌始终覆盖角落物件。中央静态 `BattleCenterDivider` 为 `1260 × 160`，引用透明 `BattleCenterDividerCarving.png`，以 `58%` Alpha 横向拉伸显示刀刻痕且不接收射线。敌方与玩家列表分别位于 `y = 285` 与 `y = -285`，列表尺寸均为 `1680 × 360`，通过 `UiList.AreaFit/Horizontal` 和 `278 × 360` 槽位水平排列；敌方和玩家均按当前实际数组创建二至六张卡。`PopulateCards()` 完成动态条目创建与绑定后调用公开的 `RefreshLayout()`，因此两排均以 `278 px` 固定中心距排开，并随实际数量整体居中。Prefab 不包含 `TitleText`、`EnemyLabel`、`PlayerLabel`、`TurnText` 或 `ResultText`；战斗过程中央没有文字。

Prefab 最上层静态保存默认隐藏的 `ResultBackdrop` 与 `ResultBanner`。`ResultBackdrop` 全屏拉伸，使用 RGB `0.18/0.18/0.18`、Alpha `0.62` 的纯色深灰蒙板并接收射线；`ResultBanner` 位于其后续同级绘制层，根节点直接持有 `Image`，不包含结果 TMP 或 `Label` 子节点。View 序列化保存蒙板 `Image` 与 `BattleVictoryBannerText.png`、`BattleDefeatBannerText.png`、`BattleFinalVictoryBannerText.png` 三张 Sprite；普通胜利与最终胜利沿用当前蓝金横幅底图，失败使用同尺寸横向轮廓的暗红破损战旗、黑铁旧铜、裂盾和交叉残剑，三张图片已经分别绘入“胜利”“失败”“最终胜利”。Controller 在任一种结果开始时显示蒙板并按结果切换 `Image.sprite`，在 `1200 × 720` 最大区域内按 Sprite 原始宽高比调整横幅根尺寸，再从 `x=-1450` 开始用 SmoothStep 和未缩放时间在 `0.24 s` 内移到中央；到达后停止动画并开放一次全屏左键继续。有效点击锁定消费状态后通过 `AudioApi.Play()` 以 `0.7` 音量播放唯一资源键 `click1`，随后才执行备战或主菜单分流。Prefab 不再包含旧 `ResultPopup`、竖向结果文字组件或返回按钮。Controller 会在跳转前及页面关闭兜底隐藏蒙板、恢复进入结算前保存的时间倍率，并复位横幅、位置与点击状态。

`BattleView` 当前不包含结果按钮。结算继续由 `BattleController` 直接检测一次全屏左键输入，不要求命中具体 UI 控件；内部消费标记阻止重复响应，也确保 `click1` 不会重复播放。

卡面尺寸为 `250 × 360`。三个卡框层使用全拉伸锚点、`offsetMin = (0, 24)`、`offsetMax = (0, 0)`，最终矩形为 `250 × 336`。`ArtworkViewport` 使用相同锚点，并在卡框主体四边额外内缩 `2 px`，其 `offsetMin = (2, 26)`、`offsetMax = (-2, -2)`，最终矩形为 `246 × 332`。Viewport 使用带原始 `CardArtworkRoundedMask.png` 的 `Image + Mask(showMaskGraphic = false)`，已移除旧 `RectMask2D`；`ArtworkArea` 与默认隐藏的 `DeadOverlay` 是其直接子级，二者都以零偏移完全拉伸到该 Viewport，因此卡面和死亡蒙板同步适配遮罩显示尺寸。`ArtworkArea` 使用 `Image.Type.Simple`、关闭 `preserveAspect` 和 `useSpriteMesh`；`DeadOverlay` 使用 `62%` Alpha 的纯黑色并位于原画上方，内部唯一子级 `BrokenSwordIcon` 以 `156 × 156` 居中显示做旧暗红填充、细黑描边的 `DeathBrokenSwordIcon.png`，保持原始纵横比且不接收射线，旧 `DeadText` 已移除。圆角 Mask 同时裁切原画和死亡蒙板，名称、编号、生命、攻击与说明区不被蒙板覆盖。遮罩与当前卡框原图均为 `1024 × 1536`，由 Builder 校验 Single Sprite、Alpha、Clamp、无 Mipmap、NPOT None、至少 2048 最大尺寸和 Uncompressed；遮罩继续使用原始完整圆角轮廓，最小布局内缩负责把它限制在卡框之内。`SkillArea`、编号、生命、攻击和其他反馈层都是 Viewport 的同级或其他分支，不参与原画遮罩。`SkillArea` 为卡面 `72%` 宽、`21%` 高的下部说明区，实际为 `180 × 75.6`；子级说明文字区域为 `160 × 63.6`。名称使用 `16` 号、最大 `18` 号自动缩放；词条使用 `17` 号、最小 `10` 号自动缩放，并采用 `TextAlignmentOptions.Top` 在词条区域顶部水平居中。多个词条由规则层以顿号连接，TMP 只在可用宽度不足时自动换行。`SkillArea/CardBasePattern` 是静态 TMP 装饰层，以 `12%` Alpha 的浅金色菱形、圆点和波纹在两侧形成稀疏底纹，并固定为第一个子级，名称和关键词继续绘制在其上方；该层不接收射线，也不参与 Controller 刷新。

`KeywordTooltip` 是卡牌 Prefab 的默认隐藏静态子层，初始尺寸为 `368 × 156`，复用 `BattleBoardBackground.png` 并叠加棕色调和深棕 `Outline`；内部 `KeywordTooltipText` 使用 `16` 号深棕粗体、自动换行的中文说明。鼠标进入含词条卡牌时，Controller 按配置显示“词条名：说明”，根据实际内容把高度限制在 `112~240 px`，并把窗口临时移到所属 Canvas 顶层以避开滚动列表裁剪；卡牌靠近屏幕右侧时窗口自动放到左侧。鼠标移出、卡牌隐藏、换绑或回池时窗口关闭并归还 Prefab 原父节点。无词条卡牌不显示空窗口。

`CardFrameOverlay`、`AttackerHighlight` 与 `TargetHighlight` 统一引用中性银白 `CardFrameRoundedSubtleOpenCornersPreview.png`。`BattleCardItemController` 通过 `ResourceApi.LoadSprite("CardFrameRoundedSubtleOpenCornersPreview")` 只加载这一张 Sprite，再通过 `Image.color` 将基础框显示为铜 `#B87333`、银 `#C0CCD8`、金 `#E7A93B` 或传奇紫 `#B25CFF`；攻击者与目标层也复用同一 Sprite 并使用各自状态色。三个框使用 `250 × 336` 矩形，`ArtworkViewport` 在其内部以 `246 × 332` 显示，并在同级顺序中位于卡框之前、生命、攻击与编号标志之前，因此卡框完整覆盖圆角原画边缘而属性 UI 保持前景显示。

`TauntShieldOutline` 是 `BattleCardItem.prefab` 根节点的第一个静态子层，引用 `TauntShieldOutline.png`，显示矩形为 `292 × 408`、中心位置为 `(0, -14)`，关闭 `preserveAspect` 且不接收射线。Controller 在写入卡牌内容时使用 `BattleKeywordRules.Has(keywords, EBattleKeyword.Taunt)` 控制显隐，换绑、隐藏或回池时强制关闭，避免对象池复用残留。由于该层位于所有卡面子层之前，卡面会遮住盾牌中心；轮廓左右各露出约 `21 px`、上方约 `10 px`、下方约 `38 px`，横向相对 `278 px` 空卡槽每侧轻微超出约 `7 px`。

左上角 `58 × 38` 的 `CardNumberBadge` 与其 TMP 子文本已经固化在 Prefab 静态层级并由 View 持有序列化引用，不再由 Controller 运行时创建。左下 `HealthBadge` 使用 `60 × 60` 的 `HealthDropBadge.png`，锚点为左下、中心位置 `(30, 30)`；右下 `AttackBadge` 使用 `60 × 60` 的无剑 `AttackBadgeFrame.png`，锚点为右下、中心位置 `(-30, 30)`。两个徽章位于卡框之后绘制的前景层，底部 `24 px` 位于上移后的框线下方并完整露出；数值使用 `30` 号粗体 TMP 和深色 `Outline`。每个徽章还包含一份默认隐藏的旧值 TMP 快照；攻击或生命上升时，旧值向上移动 `24 px` 并渐隐，新值从下方 `18 px` 向原位滑动渐显，过渡时长为 `0.38 s` 并应用共享速度系数。绑定战斗 Entity 时，攻击与生命分别比较 `EntryAttack`、`EntryHealth`：当前值较低使用红 `#FF5C5C`，相等使用白 `#FFFFFF`，较高使用蓝 `#58B0FF`。非战斗的备战卡池、出战槽、融合槽没有绑定战斗基准，统一恢复白色；换绑、隐藏或回池也会先恢复白色，避免颜色残留。敌我双方 View 根节点保持单位旋转，不再使用方形阵营底色；池化换绑或关闭时恢复单位旋转、清空名称、隐藏编号并移除原画 Sprite。

该静态布局、圆角原画遮罩、说明底纹、词条悬浮窗、相关 Sprite 导入约束，以及备战卡池、出战槽、融合槽需要的空态、投放高亮、素材角标、拖拽和悬停输入，都由一一对应的 `BattleCardItemUiBuilder.Build()` 维护。悬停与拖拽共用卡片根节点的 `CardBackground` 射线面和同一个 `UiEventListener`，避免独立子输入层优先截获 PointerDown/Drag；Prefab 中默认关闭输入，Controller 会为已显示的战斗卡和备战卡开启悬停，为备战对应上下文单独开启拖拽和投放。悬停仍会临时切换黄色等阶框，并在含词条时显示说明窗；说明窗本身不接收射线，不会阻断拖拽、点击或滚动。

`BattleCardItem.prefab` 中的 TMP 文本统一引用 `Assets/Resources/Fonts/NotoSansSC-SemiBold Dynamic SDF.asset`。该字体资产使用 Dynamic population 与 Multi Atlas，允许卡牌名称、词条与悬浮说明在运行时补充其他简体中文字形；源字体为同目录的 `NotoSansSC-SemiBold.ttf`。`BattleView.prefab` 的结算文字已经烘焙进 Sprite，不再依赖 TMP 字体。

### 2.2 每个Controller监听的Component变量

| Controller | 监听来源 | 响应 |
| --- | --- | --- |
| `BattleController` | `BattleSessionSingletonRawComponent.Result` | 结果由 `BattleSystem` 在单位耗尽后延迟 `0.5 s` 写入；三种结果切换对应的带字 Sprite、按原始比例调整横幅尺寸并启动入场，同时保存并暂停缩放时间，等待一次有效点击后恢复时间并继续 |
| `BattleCardItemController` | `BattleCardRawComponent.CurrentHealth` | 刷新生命数字并相对 `EntryHealth` 更新红白蓝颜色；下降时按前后值差播放黄色底板与红色数字的伤害浮字，上升时播放旧值与新值滑动过渡 |
| `BattleCardItemController` | `BattleCardRawComponent.AttackValue` | 刷新攻击数字并相对 `EntryAttack` 更新红白蓝颜色；上升时播放旧值与新值滑动过渡 |
| `BattleCardItemController` | `BattleCardRawComponent.IsAlive` | 存活时隐藏死亡层；阵亡时显示圆角卡面内的黑色半透明蒙板与中央做旧暗红、细黑描边断剑图标 |
| `BattleCardItemController` | `BattleSessionSingletonRawComponent.CurrentAttacker` | 控制攻击者高亮 |
| `BattleCardItemController` | `BattleSessionSingletonRawComponent.CurrentTarget` | 控制目标高亮 |
| `BattleCardItemController` | `BattleSessionSingletonRawComponent.AttackPresentationSequence` | 读取攻击者表现来源类型配置，初始化本次前拱、目标帧图集与闪红表现；攻击者有“冲锋”或“远射”时分别启动号角或弓箭浮动图标，并与 System 触发的延迟音效共用 `0.8` 倍速表现时钟 |

### 2.3 不同Controller之间的跳转关系

`BattleUiScene` 创建 `BattleController` 后，后者在两个 `UiList` 中创建卡牌条目 Controller。统一横幅进入中央后等待任意左键点击：非最终轮玩家胜利由 Controller 发布 `OutcomePresentationCompleted`，StageListener 再切换到下一轮 PreparationStage；失败和最终轮胜利由 Controller 请求主菜单 StageGroup。BattleStage 卸载时整页与条目按 UI 框架生命周期关闭并回池，同时兜底恢复结果暂停前的时间倍率。

## 3. 所属GameStage

战斗界面属于 `BattleStage`，使用 `BattleUiScene`、`EBattleUiGroup.Main` 和 `Assets/Resources/Ui/Battle.asset`。导出资产中的 View Prefab 路径为 `Ui/BattleView`，默认显示。`BattleViewUiBuilder.Build()` 一一对应维护战场背景、共享做旧层、两组角落装饰、中央刀刻分隔线、两组卡牌列表、结算灰色蒙板，以及三张带字结果 Sprite 与结算 `Image` 的序列化引用；这些变化只修改 View Prefab 内部静态结构与图片引用，没有改变 UI 编辑场景、UiGroup、DefaultShow、场景级 Position/Scale/Pivot 或导出路径，因此导出 Asset 保持不变。
