# 战斗界面程序文档

## 1. 核心数据来源

### 1.1 Component

| Component | 战斗界面用途 |
| --- | --- |
| `BattleSessionSingletonRawComponent` | 提供双方卡牌 Entity、当前行动方、战斗结果、当前攻击者与当前目标，以及攻击表现序列号、活动标记和统一时钟 |
| `BattleCardRawComponent` | 提供卡牌编号、种类 ID、阵营、运行时等阶、攻击力、当前生命、存活状态，以及本场固定的入场攻击与入场生命基准 |

### 1.2 Csv和ScriptableObject配置项

`BattleCardItemController` 根据 `BattleCardRawComponent.CardNumber` 通过 `DataApi` 读取 `BattleCardCsvData` 的种类关联与原画资源键，再按 `CardTypeId` 读取 `BattleCardTypeCsvData` 的显示名称、攻击帧图集键与受击延迟。怪物原画和攻击图集都通过 `ResourceApi.LoadSprite()` 加载；攻击音效键与音效延迟由 `BattleSystem` 从同一类型配置读取，并通过 `AudioApi.Play()` 播放现有音效库资源。左上角编号直接使用运行时 Component 中的 `CardNumber`；基础卡框只读取 Component 中的 `Tier`，不根据阵营或配置 ID 自行推导。

当前界面未读取 ScriptableObject 配置。

## 2. UI界面

### 2.1 关联界面Controller列表

| Controller | View Prefab | 职责 |
| --- | --- | --- |
| `BattleController` | `Assets/Resources/Ui/BattleView.prefab` | 创建双方卡牌列表，显示无阵营文字标记的战斗状态和胜负结果 |
| `BattleCardItemController` | `Assets/Resources/Ui/BattleCardItem.prefab` | 绑定战斗 Entity 时刷新等阶卡框、名称、原画、编号、攻血及其相对入场基准的颜色、高亮和死亡遮罩；同一 Controller/Prefab 也可绑定备战卡池编号、出战槽或融合槽，并确保不同上下文换绑时完整清理状态 |

`BattleCardItemController` 通过唯一 Pre-load 映射 `Hearthstone.BattleCardItemController → Ui/BattleCardItem` 由 `UiList.AddItem<BattleCardItemController>()` 创建和回收。战斗双方列表与备战卡池、出战槽、融合槽使用同一映射和同一对象池；战斗 Entity 仅作为玩法数据句柄，不作为 UI View 或 Controller。

每个池化条目在初始化时只创建一次原生 `RawImage` 子层 `AttackFrameEffect`，之后所有攻击复用该对象。攻击开始时，攻击者沿竖向朝对方阵列使用正弦曲线前拱并退回原锚点；目标按 `4 × 2` UV 网格播放八帧透明图集，在配置的 `HitDelay` 时对原画做一次红色脉冲。战斗 System 负责按 `AttackAudioDelay` 触发音效，并使用 `Combat` 分组和 `BattleCardAttack` 并发键，最多同时三声且按并发数量衰减音量。换绑、回池、表现结束或配置缺失时会恢复原位置、原画颜色并清空图集，避免复用残影。

`BattleView.prefab` 的根尺寸为 `1920 × 1080`，`BoardBackground` 拉伸覆盖完整界面并使用 `BattleBoardBackground.png`。其后一层静态 `ParchmentAgingOverlay` 引用 `ParchmentAgingOverlay.png`，锚点范围为 `(0.055, 0.07)` 至 `(0.945, 0.93)`，以 `14%` 整体 Alpha 覆盖内侧羊皮纸区域且不接收射线；该 Sprite 与备战页面共用。敌方与玩家列表分别位于 `y = 224` 与 `y = -224`，列表尺寸均为 `900 × 360`，通过 `UiList.AreaFit` 和 `278 × 360` 槽位水平排列三张卡牌。Prefab 不再包含 `TitleText`、`EnemyLabel` 或 `PlayerLabel`；中央 `TurnText` 只显示“战斗进行中”，结果文本只显示“胜利”或“失败”。

卡面尺寸为 `250 × 360`。`ArtworkViewport` 使用 `RectMask2D` 覆盖卡面约 `89%` 宽、`82.5%` 高的主体区域，实际为 `222.5 × 297`；`ArtworkArea` 固定居中为 `210 × 297`，使用 `Image.Type.Simple` 且在 Prefab 静态配置和 Controller 绑定时都关闭 `preserveAspect`。这样普通编号卡导入后的 `1024 × 2048` Texture 与融合卡的 `1024 × 1536` Texture 都会生成相同的 `210 × 297` 最终网格，比例约 `0.707:1`，卡面左右各留 `20 px`，不会再因导入尺寸差异把普通卡横向压窄。`SkillArea` 扩大为卡面 `72%` 宽、`21%` 高的下部说明区，实际由 `160 × 45.72` 增至 `180 × 75.6`；子级说明文字区域为 `160 × 63.6`。`SkillArea/CardBasePattern` 是静态 TMP 装饰层，以 `12%` Alpha 的浅金色菱形、圆点和波纹在两侧形成稀疏底纹，并固定为第一个子级，名称和关键词继续绘制在其上方；该层不接收射线，也不参与 Controller 刷新。

`CardFrameOverlay`、`AttackerHighlight` 与 `TargetHighlight` 统一引用中性银白 `CardFrame-v3.png`。`BattleCardItemController` 只加载这一张 Sprite，再通过 `Image.color` 将基础框显示为铜 `#B87333`、银 `#C0CCD8`、金 `#E7A93B` 或传奇紫 `#B25CFF`；攻击者与目标层也复用同一 Sprite 并使用各自状态色。三个框的左右和上边贴合 `250 × 360` 卡面，底边使用 `offsetMin.y = 24` 上移，实际矩形为 `250 × 336`；同级索引都低于生命、攻击与编号标志。

左上角 `58 × 38` 的 `CardNumberBadge` 与其 TMP 子文本已经固化在 Prefab 静态层级并由 View 持有序列化引用，不再由 Controller 运行时创建。左下 `HealthBadge` 使用 `60 × 60` 的 `HealthDropBadge.png`，锚点为左下、中心位置 `(30, 30)`；右下 `AttackBadge` 使用 `60 × 60` 的无剑 `AttackBadgeFrame.png`，锚点为右下、中心位置 `(-30, 30)`。两个徽章位于卡框之后绘制的前景层，底部 `24 px` 位于上移后的框线下方并完整露出；数值使用 `30` 号粗体 TMP 和深色 `Outline`。绑定战斗 Entity 时，攻击与生命分别比较 `EntryAttack`、`EntryHealth`：当前值较低使用红 `#FF5C5C`，相等使用白 `#FFFFFF`，较高使用蓝 `#58B0FF`。非战斗的备战卡池、出战槽、融合槽没有绑定战斗基准，统一恢复白色；换绑、隐藏或回池也会先恢复白色，避免颜色残留。敌我双方 View 根节点保持单位旋转，不再使用方形阵营底色；池化换绑或关闭时恢复单位旋转、清空名称、隐藏编号并移除原画 Sprite。

该静态布局、说明底纹、相关 Sprite 的 Single/Alpha/Mipmap/WrapMode 导入约束，以及备战卡池、出战槽、融合槽需要的空态、投放高亮、素材角标、拖拽和悬停输入，都由一一对应的 `BattleCardItemUiBuilder.Build()` 维护。悬停与拖拽共用卡片根节点的 `CardBackground` 射线面和同一个 `UiEventListener`，避免独立子输入层优先截获 PointerDown/Drag；悬停射线、拖拽监听和备战投放组件在 Prefab 中默认关闭，Controller 只在备战对应上下文中按需开启。战斗绑定始终关闭悬停与拖拽，因此战斗卡只保留运行时等阶色及攻击/目标状态色，鼠标经过不会切换黄色。

`BattleView.prefab` 与 `BattleCardItem.prefab` 中的 7 个 TMP 文本统一引用 `Assets/Resources/Fonts/NotoSansSC-Dynamic SDF.asset`。该字体资产使用 Dynamic population 与 Multi Atlas，预置当前战斗中文字符，并允许技能说明在运行时补充其他简体中文字形；源字体为同目录的 `NotoSansSC-VF.ttf`。

### 2.2 每个Controller监听的Component变量

| Controller | 监听来源 | 响应 |
| --- | --- | --- |
| `BattleController` | `BattleSessionSingletonRawComponent.CurrentSide` | 保持中央状态为“战斗进行中”；战斗结束后清空状态文字 |
| `BattleController` | `BattleSessionSingletonRawComponent.Result` | 刷新空状态、“胜利”或“失败”，并在结算后清空行动状态 |
| `BattleCardItemController` | `BattleCardRawComponent.CurrentHealth` | 刷新生命数字，并相对 `EntryHealth` 更新红白蓝颜色 |
| `BattleCardItemController` | `BattleCardRawComponent.AttackValue` | 刷新攻击数字，并相对 `EntryAttack` 更新红白蓝颜色 |
| `BattleCardItemController` | `BattleCardRawComponent.IsAlive` | 控制死亡遮罩 |
| `BattleCardItemController` | `BattleSessionSingletonRawComponent.CurrentAttacker` | 控制攻击者高亮 |
| `BattleCardItemController` | `BattleSessionSingletonRawComponent.CurrentTarget` | 控制目标高亮 |
| `BattleCardItemController` | `BattleSessionSingletonRawComponent.AttackPresentationSequence` | 读取攻击者种类配置，初始化本次前拱、目标帧图集、延迟音效与闪红表现 |

### 2.3 不同Controller之间的跳转关系

`BattleUiScene` 创建 `BattleController` 后，后者在两个 `UiList` 中创建卡牌条目 Controller。战斗结果首次终结时由 StageListener 切换 GameStage Group，并非由 UI Controller 发起；BattleStage 卸载时整页与条目按 UI 框架生命周期关闭并回池，随后 PreparationStage 打开备战页。

## 3. 所属GameStage

战斗界面属于 `BattleStage`，使用 `BattleUiScene`、`EBattleUiGroup.Main` 和 `Assets/Resources/Ui/Battle.asset`。导出资产中的 View Prefab 路径为 `Ui/BattleView`，默认显示。当前视觉调整只修改 View Prefab 内部静态结构与图片引用，没有改变 UI 编辑场景、UiGroup、DefaultShow、场景级 Position/Scale/Pivot 或导出路径，因此导出 Asset 保持不变。
