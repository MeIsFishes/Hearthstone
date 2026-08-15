# 战斗界面程序文档

## 1. 核心数据来源

### 1.1 Component

| Component | 战斗界面用途 |
| --- | --- |
| `BattleSessionSingletonRawComponent` | 提供动态玩家卡牌 Entity、三个敌方 Entity、本轮编号、是否最终轮、延迟后的正式战斗结果、当前攻击者与当前目标，以及攻击表现序列号、活动标记和统一时钟 |
| `BattleCardRawComponent` | 提供实际卡牌编号与类型、表现来源编号与类型、阵营、运行时等阶、攻击力、当前生命、存活状态、战斗词条，以及本场固定的入场攻击与入场生命基准 |

### 1.2 Csv和ScriptableObject配置项

`BattleCardItemController` 根据 `BattleCardRawComponent.PresentationCardNumber` 通过 `DataApi` 读取 `BattleCardCsvData` 的原画资源键，再按 `PresentationCardTypeId` 读取 `BattleCardTypeCsvData` 的显示名称与攻击帧图集键，并从 `BattleKeywordCsvData` 读取实际词条的名称、说明和显示顺序。怪物原画和攻击图集都通过 `ResourceApi.LoadSprite()` 加载；攻击音效键、音效延迟与受击延迟由 `BattleSystem` 从同一表现类型配置读取，并通过 `AudioApi.Play()` 播放现有音效库资源。普通卡及双卡、三卡结果的表现身份与实际身份一致；四卡结果使用融合时点数最高三张素材对应的三卡表现。左上角编号仍直接使用 Component 中的实际 `CardNumber`，攻血、词条与传奇等阶同样保留四卡结果数据；基础卡框只读取 Component 中的 `Tier`，不根据阵营或表现配置自行推导。

当前界面未读取 ScriptableObject 配置。

## 2. UI界面

### 2.1 关联界面Controller列表

| Controller | View Prefab | 职责 |
| --- | --- | --- |
| `BattleController` | `Assets/Resources/Ui/BattleView.prefab` | 创建双方卡牌列表；玩家胜利时驱动蓝金横幅左入、停留、右出，失败时显示失败重开弹窗，最终轮胜利时在横幅后显示整局胜利重开弹窗 |
| `BattleCardItemController` | `Assets/Resources/Ui/BattleCardItem.prefab` | 绑定战斗 Entity 时刷新等阶卡框、名称、词条及其悬浮说明、原画、编号、攻血及其相对入场基准的颜色、嘲讽盾牌轮廓、高亮和死亡遮罩，并播放伤害浮字、数值上升过渡、冲锋号角与远射弓箭反馈；同一 Controller/Prefab 也可绑定备战卡池编号、出战槽、融合槽、融合揭晓或融合推荐卡，并确保不同上下文换绑时完整清理状态 |

`BattleCardItemController` 通过唯一 Pre-load 映射 `Hearthstone.BattleCardItemController → Ui/BattleCardItem` 由 `UiList.AddItem<BattleCardItemController>()` 创建和回收。战斗双方列表与备战卡池、出战槽、融合槽使用同一映射和同一对象池；战斗 Entity 仅作为玩法数据句柄，不作为 UI View 或 Controller。

每个池化条目在初始化时只创建一次原生 `RawImage` 子层 `AttackFrameEffect`，之后所有攻击复用该对象。攻击开始时，攻击者沿竖向朝对方阵列使用正弦曲线前拱并退回原锚点；目标按 `4 × 2` UV 网格播放八帧透明图集，在配置的 `HitDelay` 时对原画做一次红色脉冲。共享攻击表现时钟当前按 `0.8` 倍速推进，因此前拱、图集帧、红色脉冲、伤害与战斗 System 按 `AttackAudioDelay` 触发的音效保持同步。音效使用 `Combat` 分组和 `BattleCardAttack` 并发键，最多同时三声且按并发数量衰减音量。换绑、回池、表现结束或配置缺失时会恢复原位置、原画颜色并清空图集，避免复用残影。

Prefab 静态持有三组附加反馈层：生命徽章上方的 `92 × 70` 黄色爆炸状伤害底板与红 `#D22020` 深红描边 TMP 数字、卡面上中偏左的 `96 × 96` 冲锋号角、卡面上中偏右的 `96 × 96` 远射弓箭。生命下降时 Controller 按监听前后值差显示 `-伤害值`，底板和数字向上移动 `54 px` 并渐隐；新的攻击表现序列只在当前卡牌是攻击者且具备对应词条时启动图标，图标向上移动 `84 px` 并渐隐，同一张卡可以同时显示两种词条反馈。伤害与词条反馈都在独立计时器上运行，但每帧同样乘以 `BattleRules.AttackPresentationPlaybackSpeed`，当前整体为 `0.8` 倍速。换绑、隐藏和回池时会还原位置、透明度、文字与显隐状态。

`BattleView.prefab` 的根尺寸为 `1920 × 1080`，`BoardBackground` 拉伸覆盖完整界面并使用 `BattleBoardBackground.png`。其后一层静态 `ParchmentAgingOverlay` 引用 `ParchmentAgingOverlay.png`，以 `24%` Alpha 覆盖内侧羊皮纸区域且不接收射线；该 Sprite 与备战页面共用，但战斗叠加强度高于备战的 `18%`。中央静态 `BattleCenterDivider` 为 `1260 × 160`，引用透明 `BattleCenterDividerCarving.png`，以 `58%` Alpha 横向拉伸显示刀刻痕且不接收射线。敌方与玩家列表分别位于 `y = 285` 与 `y = -285`，列表尺寸均为 `1680 × 360`，通过 `UiList.AreaFit/Horizontal` 和 `278 × 360` 槽位水平排列；敌方创建三张卡，玩家按会话数组创建三至六张卡。`PopulateCards()` 完成动态条目创建与绑定后调用公开的 `RefreshLayout()`，因此两排均以 `278 px` 固定中心距排开，并随实际数量整体居中。Prefab 不包含 `TitleText`、`EnemyLabel`、`PlayerLabel`、`TurnText` 或 `ResultText`；战斗过程中央没有文字。

Prefab 最上层静态保存默认隐藏的 `VictoryBanner` 与 `ResultPopup`。横幅根为 `1040 × 360`，引用 `BattleVictoryBanner.png` 并叠加“战斗胜利”TMP；Controller 从 `x=-1450` 开始，用 SmoothStep 在 `0.24 s` 内移到中央，停留 `0.68 s`，再在 `0.24 s` 内移到 `x=1450` 后隐藏。`ResultPopup` 使用全屏半透明射线遮罩，中央面板根据结果动态加载 `BattleDefeatPanel.png` 或 `RunVictoryPanel.png`，标题、说明和“重新开始”均由 TMP 精确渲染；显示时在 `0.18 s` 内从 `0.9` 倍、Alpha 0 平滑过渡到单位缩放、Alpha 1。按钮会先禁用自身，再调用 `HearthstoneGameEngine.RestartRun()`，页面关闭或重新打开时统一复位显隐、位置和按钮状态。

卡面尺寸为 `250 × 360`。`ArtworkViewport` 使用 `RectMask2D` 覆盖卡面约 `89%` 宽、`82.5%` 高的主体区域，实际为 `222.5 × 297`；`ArtworkArea` 固定居中为 `210 × 297`，使用 `Image.Type.Simple` 且在 Prefab 静态配置和 Controller 绑定时都关闭 `preserveAspect`。这样普通编号卡导入后的 `1024 × 2048` Texture 与融合卡的 `1024 × 1536` Texture 都会生成相同的 `210 × 297` 最终网格，比例约 `0.707:1`，卡面左右各留 `20 px`，不会再因导入尺寸差异把普通卡横向压窄。`SkillArea` 扩大为卡面 `72%` 宽、`21%` 高的下部说明区，实际由 `160 × 45.72` 增至 `180 × 75.6`；子级说明文字区域为 `160 × 63.6`。名称使用 `16` 号、最大 `18` 号自动缩放；词条使用 `17` 号、最小 `10` 号自动缩放，并采用 `TextAlignmentOptions.Top` 在词条区域顶部水平居中。多个词条由规则层以顿号连接，TMP 只在可用宽度不足时自动换行。`SkillArea/CardBasePattern` 是静态 TMP 装饰层，以 `12%` Alpha 的浅金色菱形、圆点和波纹在两侧形成稀疏底纹，并固定为第一个子级，名称和关键词继续绘制在其上方；该层不接收射线，也不参与 Controller 刷新。

`KeywordTooltip` 是卡牌 Prefab 的默认隐藏静态子层，初始尺寸为 `368 × 156`，复用 `BattleBoardBackground.png` 并叠加棕色调和深棕 `Outline`；内部 `KeywordTooltipText` 使用 `16` 号深棕粗体、自动换行的中文说明。鼠标进入含词条卡牌时，Controller 按配置显示“词条名：说明”，根据实际内容把高度限制在 `112~240 px`，并把窗口临时移到所属 Canvas 顶层以避开滚动列表裁剪；卡牌靠近屏幕右侧时窗口自动放到左侧。鼠标移出、卡牌隐藏、换绑或回池时窗口关闭并归还 Prefab 原父节点。无词条卡牌不显示空窗口。

`CardFrameOverlay`、`AttackerHighlight` 与 `TargetHighlight` 统一引用中性银白 `CardFrame-v3.png`。`BattleCardItemController` 只加载这一张 Sprite，再通过 `Image.color` 将基础框显示为铜 `#B87333`、银 `#C0CCD8`、金 `#E7A93B` 或传奇紫 `#B25CFF`；攻击者与目标层也复用同一 Sprite 并使用各自状态色。三个框的左右和上边贴合 `250 × 360` 卡面，底边使用 `offsetMin.y = 24` 上移，实际矩形为 `250 × 336`；同级索引都低于生命、攻击与编号标志。

`TauntShieldOutline` 是 `BattleCardItem.prefab` 根节点的第一个静态子层，引用 `TauntShieldOutline.png`，显示矩形为 `292 × 408`、中心位置为 `(0, -14)`，关闭 `preserveAspect` 且不接收射线。Controller 在写入卡牌内容时使用 `BattleKeywordRules.Has(keywords, EBattleKeyword.Taunt)` 控制显隐，换绑、隐藏或回池时强制关闭，避免对象池复用残留。由于该层位于所有卡面子层之前，卡面会遮住盾牌中心；轮廓左右各露出约 `21 px`、上方约 `10 px`、下方约 `38 px`，横向相对 `278 px` 空卡槽每侧轻微超出约 `7 px`。

左上角 `58 × 38` 的 `CardNumberBadge` 与其 TMP 子文本已经固化在 Prefab 静态层级并由 View 持有序列化引用，不再由 Controller 运行时创建。左下 `HealthBadge` 使用 `60 × 60` 的 `HealthDropBadge.png`，锚点为左下、中心位置 `(30, 30)`；右下 `AttackBadge` 使用 `60 × 60` 的无剑 `AttackBadgeFrame.png`，锚点为右下、中心位置 `(-30, 30)`。两个徽章位于卡框之后绘制的前景层，底部 `24 px` 位于上移后的框线下方并完整露出；数值使用 `30` 号粗体 TMP 和深色 `Outline`。每个徽章还包含一份默认隐藏的旧值 TMP 快照；攻击或生命上升时，旧值向上移动 `24 px` 并渐隐，新值从下方 `18 px` 向原位滑动渐显，过渡时长为 `0.38 s` 并应用共享速度系数。绑定战斗 Entity 时，攻击与生命分别比较 `EntryAttack`、`EntryHealth`：当前值较低使用红 `#FF5C5C`，相等使用白 `#FFFFFF`，较高使用蓝 `#58B0FF`。非战斗的备战卡池、出战槽、融合槽没有绑定战斗基准，统一恢复白色；换绑、隐藏或回池也会先恢复白色，避免颜色残留。敌我双方 View 根节点保持单位旋转，不再使用方形阵营底色；池化换绑或关闭时恢复单位旋转、清空名称、隐藏编号并移除原画 Sprite。

该静态布局、说明底纹、词条悬浮窗、相关 Sprite 的 Single/Alpha/Mipmap/WrapMode 导入约束，以及备战卡池、出战槽、融合槽需要的空态、投放高亮、素材角标、拖拽和悬停输入，都由一一对应的 `BattleCardItemUiBuilder.Build()` 维护。悬停与拖拽共用卡片根节点的 `CardBackground` 射线面和同一个 `UiEventListener`，避免独立子输入层优先截获 PointerDown/Drag；Prefab 中默认关闭输入，Controller 会为已显示的战斗卡和备战卡开启悬停，为备战对应上下文单独开启拖拽和投放。悬停仍会临时切换黄色等阶框，并在含词条时显示说明窗；说明窗本身不接收射线，不会阻断拖拽、点击或滚动。

`BattleView.prefab` 与 `BattleCardItem.prefab` 中的 TMP 文本统一引用 `Assets/Resources/Fonts/NotoSansSC-SemiBold Dynamic SDF.asset`。该字体资产使用 Dynamic population 与 Multi Atlas，允许卡牌名称、词条与悬浮说明在运行时补充其他简体中文字形；源字体为同目录的 `NotoSansSC-SemiBold.otf`。

### 2.2 每个Controller监听的Component变量

| Controller | 监听来源 | 响应 |
| --- | --- | --- |
| `BattleController` | `BattleSessionSingletonRawComponent.Result` | 结果由 `BattleSystem` 在单位耗尽后延迟 `0.5 s` 写入；玩家胜利启动横幅，敌方胜利启动失败重开弹窗，并清空中央行动状态 |
| `BattleCardItemController` | `BattleCardRawComponent.CurrentHealth` | 刷新生命数字并相对 `EntryHealth` 更新红白蓝颜色；下降时按前后值差播放黄色底板与红色数字的伤害浮字，上升时播放旧值与新值滑动过渡 |
| `BattleCardItemController` | `BattleCardRawComponent.AttackValue` | 刷新攻击数字并相对 `EntryAttack` 更新红白蓝颜色；上升时播放旧值与新值滑动过渡 |
| `BattleCardItemController` | `BattleCardRawComponent.IsAlive` | 控制死亡遮罩 |
| `BattleCardItemController` | `BattleSessionSingletonRawComponent.CurrentAttacker` | 控制攻击者高亮 |
| `BattleCardItemController` | `BattleSessionSingletonRawComponent.CurrentTarget` | 控制目标高亮 |
| `BattleCardItemController` | `BattleSessionSingletonRawComponent.AttackPresentationSequence` | 读取攻击者表现来源类型配置，初始化本次前拱、目标帧图集与闪红表现；攻击者有“冲锋”或“远射”时分别启动号角或弓箭浮动图标，并与 System 触发的延迟音效共用 `0.8` 倍速表现时钟 |

### 2.3 不同Controller之间的跳转关系

`BattleUiScene` 创建 `BattleController` 后，后者在两个 `UiList` 中创建卡牌条目 Controller。非最终轮玩家胜利由 `BattleSystem` 的横幅表现倒计时完成后发布 `OutcomePresentationCompleted`，StageListener 再切换到下一轮 PreparationStage；失败和最终轮胜利不切 Stage，UI 弹窗中的按钮直接调用整局重开入口。BattleStage 卸载时整页与条目按 UI 框架生命周期关闭并回池。

## 3. 所属GameStage

战斗界面属于 `BattleStage`，使用 `BattleUiScene`、`EBattleUiGroup.Main` 和 `Assets/Resources/Ui/Battle.asset`。导出资产中的 View Prefab 路径为 `Ui/BattleView`，默认显示。`BattleViewUiBuilder.Build()` 一一对应维护战场背景、共享做旧层、中央刀刻分隔线、两组卡牌列表、胜利横幅和结果弹窗；这些变化只修改 View Prefab 内部静态结构与图片引用，没有改变 UI 编辑场景、UiGroup、DefaultShow、场景级 Position/Scale/Pivot 或导出路径，因此导出 Asset 保持不变。
