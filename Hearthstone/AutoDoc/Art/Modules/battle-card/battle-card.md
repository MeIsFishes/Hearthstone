# 战斗卡牌美术模块文档

## 1. 模块范围

本模块覆盖战斗界面的木质金边羊皮纸底板、共享羊皮纸做旧层、中央刀刻分隔线、双方卡牌的怪物原画、99 号封印锁位原画、100～148 号双卡与三卡融合原画、说明区域、词条悬浮木板、可着色直边矩形等阶卡框、嘲讽盾牌轮廓、左上角编号底框、生命与攻击徽章、伤害数字爆炸底板、冲锋号角与远射弓箭图标、攻击/目标高亮、死亡遮罩、五种攻击帧特效，以及胜利横幅、失败面板和整局胜利面板，也记录战斗场景与怪物卡面的现有概念资源。界面与卡面基础静态层级分别保存在 `Assets/Resources/Ui/BattleView.prefab` 和 `Assets/Resources/Ui/BattleCardItem.prefab`，运行 Sprite 位于 `Assets/Resources/Art/BattleCards/`，概念参考位于 `Assets/Art/ConceptArt/`。

## 2. 模块风格

战斗羊皮纸表面以 `24%` 整体 Alpha 叠加共享的透明做旧层，使零星淡斑、水渍边和短划痕比备战区域更明显，大部分中央区域仍保持低细节。敌我卡列之间不放置常驻文字，改用一条暖棕色透明刀刻痕：凹槽深棕、切边仅带克制浅金高光，形状略有断续且两端收尖，以 `58%` Alpha 显示，避免抢过卡牌和结果横幅。

可运行界面使用正俯视、上下两排对称卡位和大面积浅暖羊皮纸留白，深棕木框、暖金嵌边、四角装饰与少量蓝宝石点缀界定战场。敌方三张与玩家三至六张卡牌都以 `278 px` 中心距横向排开，并随实际数量整体居中；`250 px` 宽卡面之间保留约 `28 px` 的视觉间隙。敌我双方共用一张轻薄中性银白直边矩形框，基础框按运行时等阶着色为铜 `#B87333`、银 `#C0CCD8`、金 `#E7A93B` 或传奇紫 `#B25CFF`；备战悬停时同一框临时着色为黄 `#FFD230`。攻击者与目标覆盖层继续使用独立状态色，不承担敌我或等阶识别。在 `250 × 360` 卡面根内，基础框、攻击者高亮与目标高亮共用同一 Sprite，左右和上边贴合卡面，底边上移 `24 px`，同时位于属性和编号标志下方。嘲讽单位额外使用正面、对称的银灰与暗钢空心盾牌轮廓；原始透明图为 `1086 × 1448`，运行时铺满 `292 × 408` 区域、中心向下偏移 `14 px` 并置于卡面最底层。轮廓相对卡面左右各露出约 `21 px`、上方约 `10 px`、下方约 `38 px`，横向相对 `278 px` 空卡槽每侧轻微超出约 `7 px`。怪物原始图片均为 `2:3` 竖向环境构图；运行卡面将所有来源的立绘统一绘制为居中的 `210 × 297` 矩形，最终比例约 `0.707:1`，相对标准 `2:3` 略宽约 `6.1%`，在卡面左右各留 `20 px`，以减少空隙并消除不同导入尺寸造成的横向压窄。下方说明底板为 `180 × 75.6`，在名称与关键词文字后方使用 `12%` 不透明度的浅金色菱形、圆点和波纹组成两侧稀疏装饰，中部保持留空。左上角编号采用 `58 × 38` 的横向六边形深灰金属底框与居中白色粗体数字；左下生命使用 `60 × 60` 绿金血滴，右下攻击使用 `60 × 60` 无剑红金盾框，两者叠加 `30` 号粗体描边数字。战斗中每项数字低于自身入场基准时使用红 `#FF5C5C`，高于入场基准时使用蓝 `#58B0FF`，相等及所有备战展示使用白 `#FFFFFF`；颜色只作用于 TMP 数字，不改变徽章位图。伤害爆炸底板上的实际伤害数字使用红 `#D22020` 与深红描边，和黄色底板形成高对比。

备战继续复用同一 `250 × 360` 卡面；出战列表以 `205 px` 中心距排列，但空槽和占用卡面都缩放为 `180.4 × 259.776`，总缩放约 `0.7216`，相邻可见槽位之间保留约 `24.6 px`；融合槽保持 `190 × 273.6` 与 `0.76` 缩放。卡框、原画、说明与属性徽章保持同一比例并在槽内居中，不裁掉卡面边缘。出战区域不显示“出战槽位”标题和标题装饰线。

99 号封印位使用独立的深色锻铁巨门原画：中央盾形巨锁、交叉粗链和暗红蜡封构成小尺寸下仍清晰的禁止通行轮廓，边缘以克制的橙红炉火勾勒材质。画面不含人物、怪物、文字和卡框，避免被误读为可获得卡牌；运行时沿用共享卡框并着色为低饱和深灰。

100～148 号融合卡各使用一张独立的 `1024 × 1536` 竖向原画。画面保持明亮、高饱和的奇幻手绘质感，并按角色身份在草地、林地、雪地、城镇或城墙环境中分散取景，避免整批使用同一背景。哥布林与野猪同时出现时，哥布林以骑兵身份呈现；食人魔与野猪同时出现时通常以肉棒、骨头或烤肉架表达食人魔已经吃掉野猪，147 号“野猪驱使者”改为一名食人魔在地面指挥两只活野猪冲锋且不出现食物线索，148 号“野猪王骑兵”则由单个食人魔骑乘单只巨型野猪王。食人魔与弓箭组合按兵种功能区分为重弓狙击、塔盾攻城弩、食人魔承载弓手、爆炸弩炮、骑猪侦猎与床弩协作，避免只替换人数的雷同构图。所有融合原画均不含文字、编号、卡框、UI 或水印。

149～213 号四卡传奇结果不使用一套静态指定的独立表现。每次融合按四张素材点数取最高三张，并复用它们对应的 100～148 号三卡融合原画、名称和攻击帧表现；传奇紫卡框、攻血与词条仍来自四卡结果实例。由此同一四卡公式可以随实际素材点数组合显示不同的现有三卡视觉版本，不需要为四卡结果再维护一批重复原画或攻击图集。

攻击特效采用透明底、无文字与无水印的明亮奇幻手绘光效。每张图集为 `4 × 2` 排列的八帧序列，按左上到右下播放；特效覆盖卡面主体而不改变卡框和属性徽章。剑痕以冷白弧光为主，箭矢保持从斜上方射入的方向性，小型爆炸使用橙黄火光，小型与大型击打分别以较克制和更厚重的冲击环、碎屑与火花区分力度。

卡面即时反馈使用高对比、透明底且适合缩小显示的单体图形。伤害数字底板为横向黄色爆炸星芒，中央留出完整数字区；冲锋使用带红色飘带的黄铜号角；远射使用粗轮廓的金棕短弓与单支银色短箭，箭头保持小巧并以蓝色箭羽作唯一冷色点缀，整体更接近符号图标而非完整武器插画。号角与弓箭在运行时均以 `96 × 96` 显示。词条悬浮窗复用战场木质金边底板，整体叠加棕色调并使用深棕描边，内部使用深棕粗体中文说明。

## 3. UI 资产风格

| UI 资产或资产组 | UI 风格分组 ID | 分组名称 | 适用界面或区域 |
| --- | --- | --- | --- |
| 战斗卡牌等阶边框、嘲讽盾牌与属性徽章 | `UI-STYLE-001` | 明亮红蓝金奇幻卡牌界面 | 战斗界面的双方卡牌 |
| 共享羊皮纸做旧层 | `UI-STYLE-001` | 明亮红蓝金奇幻卡牌界面 | 战斗棋盘羊皮纸表面；与备战上区复用同一 Sprite |
| 中央刀刻分隔线 | `UI-STYLE-001` | 明亮红蓝金奇幻卡牌界面 | 战斗界面中央敌我卡列之间；透明无字装饰层 |
| 战斗攻击帧特效 | `UI-STYLE-001` | 明亮红蓝金奇幻卡牌界面 | 受击卡面主体；五种怪物各使用一张八帧透明图集 |
| 伤害、冲锋与远射即时反馈 | `UI-STYLE-001` | 明亮红蓝金奇幻卡牌界面 | 生命徽章上方与卡面上中部的短时浮动反馈 |
| 词条悬浮说明窗 | `UI-STYLE-001` | 明亮红蓝金奇幻卡牌界面 | 卡牌悬停时显示的棕色木板说明窗 |
| 战斗胜利横幅 | `UI-STYLE-001` | 明亮红蓝金奇幻卡牌界面 | 单场玩家胜利时从左滑入、停留并向右滑出的中央横幅；图片无字，中文由 TMP 叠加 |
| 失败与整局胜利面板 | `UI-STYLE-001` | 明亮红蓝金奇幻卡牌界面 | 战斗失败或最终轮胜利后的重开弹窗；图片无字，标题、说明和按钮由 TMP 叠加 |

## 4. 图标规格

| 图标或图标组 | 画布尺寸 | 主体占比与安全区 | 形状与线条 | 配色 | 背景 / Alpha | 视觉变体 |
| --- | ---: | --- | --- | --- | --- | --- |
| 攻击力红金外框 | `1254 × 1254` | 主体约占画布 `82%`，四周保留透明安全区，中央保留大面积数值区 | 正面、对称的盾形红宝石底板，完整暖金包边与侧叶装饰，不含剑主体 | 深红宝石、暖金 | 真实 Alpha 透明 | 当前单一无剑版本 |
| 生命值血滴徽章 | `1254 × 1254` | 主体约占画布 `82%`，四周保留透明安全区 | 正面血滴轮廓、金属包边与少量叶形装饰，中央留数值区 | 祖母绿、暖金、亮绿色高光 | 真实 Alpha 透明 | 当前单一版本 |
| 嘲讽盾牌轮廓 | `1086 × 1448` | 运行时铺满 `292 × 408` 并下移 `14 px`；置于 `250 × 360` 卡面后左右各露约 `21 px`、上方约 `10 px`、下方约 `38 px` | 正面、对称、宽肩收尖的空心金属盾牌；中心无徽记和填充 | 冷银灰、暗钢、克制白色边缘高光 | 中心与轮廓外均为真实 Alpha 透明 | 当前单一版本 |
| 伤害数字爆炸底板 | `1448 × 1086` | 横向主体约占画布 `82%`，中央保留大面积数字区 | 12～16 个尖角组成的不对称漫画冲击星芒，边缘清晰 | 柠檬黄、暖橙、细暗琥珀边 | 轮廓外真实 Alpha 透明 | 当前单一无字版本 |
| 冲锋号角图标 | `1254 × 1254` | 紧凑主体约占画布 `78%`，四周保留透明安全区 | 向右上微扬的侧视弯号，短皮革握把与小红飘带 | 黄铜金、琥珀阴影、深红点缀 | 真实 Alpha 透明 | 当前单一版本 |
| 远射弓箭图标 | `1254 × 1254` | 近方形主体约占画布 `70%`，宽透明安全区，缩至 `64 px` 仍可辨识 | 偏直立的粗轮廓短弓与一支横向短箭；箭头小巧、负形清楚 | 金棕短弓、银色箭身、克制蓝色箭羽 | 真实 Alpha 透明 | 当前单一简化符号版本 |
| 剑痕攻击帧图集 | `1536 × 1024` | `4 × 2` 八帧，剑光集中于单帧中央安全区 | 冷白弧形剑痕由淡入、划过到消散 | 冷白、淡蓝、少量金色火花 | 真实 Alpha 透明 | 哥布林战士 |
| 箭矢攻击帧图集 | `1774 × 887` | `4 × 2` 八帧，斜向轨迹完整保留 | 箭矢从斜上方射入卡面并产生木质碰撞碎屑 | 木棕、冷白轨迹、暖色火花 | 真实 Alpha 透明 | 哥布林弓手 |
| 小型爆炸帧图集 | `1774 × 887` | `4 × 2` 八帧，爆心与烟尘位于卡面中央 | 小型火球迅速扩张后收束为烟尘 | 橙黄、暗橙、灰烟 | 真实 Alpha 透明 | 哥布林投弹手 |
| 小型击打帧图集 | `1774 × 887` | `4 × 2` 八帧，紧凑冲击环与少量碎屑 | 轻量冲击星芒由集中到消散 | 暖白、浅黄、少量棕色碎屑 | 真实 Alpha 透明 | 野猪 |
| 大型击打帧图集 | `1774 × 887` | `4 × 2` 八帧，冲击环和碎屑覆盖范围更大 | 厚重冲击波、裂纹感碎屑与火花逐层扩散 | 暖白、金黄、橙棕 | 真实 Alpha 透明 | 食人魔 |

## 5. 人物规格

## 6. 场景规格

## 7. 物件规格

## 8. 参考图片

| 参考图片 | 来源或项目内路径 | 参考特征 | 适用范围 |
| --- | --- | --- | --- |
| 炉石传说官方卡牌库 | https://hearthstone.blizzard.com/en-us/cards | 上部主视觉、下部说明、底部属性的信息层级 | 战斗卡面布局 |
| 战斗场景原画第二版 | `Assets/Art/ConceptArt/battle-scene-concept-v2.png` | 正俯视、上下各三卡、浅色简约底板与 UI 主导的攻击反馈 | 当前战斗场景构图与界面方向 |
| 运行战场底板 | `Assets/Resources/Art/BattleCards/UI/BattleBoardBackground.png` | 从主参考构图提取的无卡牌木质金边羊皮纸面板 | 当前战斗界面背景 |
| 羊皮纸做旧层 | `Assets/Resources/Art/Preparation/UI/ParchmentAgingOverlay.png` | 真实透明背景、稀疏淡褐旧化痕迹和中央低干扰留白 | 战斗棋盘与备战上区共享表面纹理 |
| 中央刀刻分隔线 | `Assets/Resources/Art/BattleCards/UI/BattleCenterDividerCarving.png` | 暖棕凹槽、浅金切边、断续手绘轮廓与真实透明背景 | 战斗界面中央敌我分隔 |
| 怪物卡面原画第二版 | `Assets/Art/ConceptArt/monster-card-fronts-concept-v2.png` | 五种怪物、约三分之二人物占比、明亮奇幻环境与轻量笔触 | 当前怪物主视觉与卡框方向 |
| 99 号封印锁位 | `Assets/Resources/Art/BattleCards/FusionCard_099.png` | 深色锻铁门、中央巨锁、交叉锁链和红色封印形成不可获得的明确语义 | 备战卡池普通卡与融合卡之间的固定分隔位 |
| 100～148 号融合卡原画 | `Assets/Resources/Art/BattleCards/FusionCard_100.png` ～ `FusionCard_148.png` | 对应各自无序融合配方的独立角色、坐骑与环境构图 | 备战卡池、融合揭晓与战斗共享卡面 |
| 嘲讽盾牌轮廓 | `Assets/Resources/Art/BattleCards/UI/TauntShieldOutline.png` | 银灰与暗钢空心盾牌、真实透明中心、无文字与徽记 | 嘲讽卡牌最底层状态轮廓 |
| 伤害数字爆炸底板 | `Assets/Resources/Art/BattleCards/UI/DamageNumberBurst.png` | 黄色橙边漫画爆炸星芒、中央无字留白、真实透明背景 | 生命徽章上方伤害浮字底板 |
| 冲锋号角图标 | `Assets/Resources/Art/BattleCards/UI/ChargeHornIcon.png` | 黄铜弯号、皮革握把和小红飘带，真实透明背景 | 冲锋词条发动反馈 |
| 远射弓箭图标 | `Assets/Resources/Art/BattleCards/UI/LongShotBowIcon.png` | 简化粗轮廓短弓、单支银箭和蓝色箭羽，真实透明背景 | 远射词条发动反馈 |
| 战斗胜利横幅 | `Assets/Resources/Art/BattleCards/Result/BattleVictoryBanner.png` | 深皇家蓝珐琅、古金包边和尖形飘带端构成明确横向速度感；中央无字留白 | 单场胜利中央横幅 |
| 战斗失败面板 | `Assets/Resources/Art/BattleCards/Result/BattleDefeatPanel.png` | 暗红珐琅、陈旧铁件、破盾轮廓和克制余烬；中央无字留白 | 失败与重新开始弹窗 |
| 整局胜利面板 | `Assets/Resources/Art/BattleCards/Result/RunVictoryPanel.png` | 皇家蓝珐琅、放射暖金和宝石庆典点缀；中央无字留白 | 全部轮次完成与重新开始弹窗 |

## 9. 目前已有资产列表

| 资产名称 | 项目内路径 | 图片内容与用途 | 尺寸 / 比例 | 文件格式 |
| --- | --- | --- | --- | --- |
| `battle-scene-concept.png` | `Assets/Art/ConceptArt/battle-scene-concept.png` | 三对三自动卡牌战斗的场景原画；用于战斗视觉方向参考，尚未接入运行界面 | `1672 × 941` / 约 `16:9` | PNG |
| `monster-card-fronts-concept.png` | `Assets/Art/ConceptArt/monster-card-fronts-concept.png` | 五种已确定怪物的卡面概念同页；用于怪物、卡框与油画笔触方向参考，尚未接入运行界面 | `1693 × 929` / 横向概念页 | PNG |
| `battle-scene-concept-v2.png` | `Assets/Art/ConceptArt/battle-scene-concept-v2.png` | 正俯视、UI 主导且简约的三对三战斗场景；当前主参考版本，尚未接入运行界面 | `1672 × 941` / 约 `16:9` | PNG |
| `monster-card-fronts-concept-v2.png` | `Assets/Art/ConceptArt/monster-card-fronts-concept-v2.png` | 五种怪物的明亮奇幻卡面同页，人物约占主视觉三分之二；当前主参考版本，尚未接入运行界面 | `1692 × 929` / 横向概念页 | PNG |
| `GoblinWarrior.png` | `Assets/Resources/Art/BattleCards/GoblinWarrior.png` | 哥布林战士独立运行卡面原画 | `1024 × 1536` / `2:3` | PNG |
| `GoblinArcher.png` | `Assets/Resources/Art/BattleCards/GoblinArcher.png` | 哥布林弓手独立运行卡面原画 | `1024 × 1536` / `2:3` | PNG |
| `GoblinBomber.png` | `Assets/Resources/Art/BattleCards/GoblinBomber.png` | 哥布林投弹手独立运行卡面原画 | `1024 × 1536` / `2:3` | PNG |
| `Boar.png` | `Assets/Resources/Art/BattleCards/Boar.png` | 野猪独立运行卡面原画 | `1024 × 1536` / `2:3` | PNG |
| `Ogre.png` | `Assets/Resources/Art/BattleCards/Ogre.png` | 食人魔独立运行卡面原画 | `1024 × 1536` / `2:3` | PNG |
| `FusionCard_099.png` | `Assets/Resources/Art/BattleCards/FusionCard_099.png` | 99 号固定封印锁位原画；锻铁巨门、盾形巨锁、交叉粗链与暗红封印，不作为可获得卡牌立绘 | `1024 × 1536` / `2:3` | PNG |
| `FusionCard_100.png`～`FusionCard_148.png` | `Assets/Resources/Art/BattleCards/` | 49 张双卡与三卡融合结果独立原画；文件编号与卡号、CSV `ArtworkKey` 一一对应 | 均为 `1024 × 1536` / `2:3` | PNG |
| `BattleBoardBackground.png` | `Assets/Resources/Art/BattleCards/UI/BattleBoardBackground.png` | 木质金边羊皮纸战场底板；不含卡牌、角色、文字或攻击轨迹，作为 `BattleView` 全屏背景，并经棕色调复用于卡牌词条悬浮窗 | `1672 × 941` / 约 `16:9` | PNG |
| `BattleCenterDividerCarving.png` | `Assets/Resources/Art/BattleCards/UI/BattleCenterDividerCarving.png` | 战斗中央使用的透明刀刻分隔线；暖棕凹槽、克制浅金切边、无文字与底板 | `2172 × 724` / 约 `3:1` 透明画布 | PNG |
| `ParchmentAgingOverlay.png` | `Assets/Resources/Art/Preparation/UI/ParchmentAgingOverlay.png` | 战斗与备战共用的透明羊皮纸做旧层；含低对比淡斑、水渍边和短划痕，不含底色与边框 | `1672 × 941` / 约 `16:9` | PNG |
| `CardFrame-v2.png` | `Assets/Resources/Art/BattleCards/UI/CardFrame-v2.png` | 较厚红金拱形卡面边框历史变体；资源保留，基础框与攻击/目标高亮均不引用 | `1024 × 1536` / `2:3` | PNG |
| `CardFrameBlue.png` | `Assets/Resources/Art/BattleCards/UI/CardFrameBlue.png` | 较厚蓝金拱形卡面边框历史变体；资源保留，当前 Prefab 不引用 | `1024 × 1536` / `2:3` | PNG |
| `CardFrame-v3.png` | `Assets/Resources/Art/BattleCards/UI/CardFrame-v3.png` | 当前共用的中性银白直边矩形卡面边框；中央和框外真实透明，由界面着色生成铜、银、金、传奇四阶与备战悬停色 | `1024 × 1536` / `2:3` | PNG |
| `TauntShieldOutline.png` | `Assets/Resources/Art/BattleCards/UI/TauntShieldOutline.png` | 嘲讽单位卡面后方的银灰暗钢空心盾牌轮廓；中心与背景真实透明，不含文字、徽记或水印 | `1086 × 1448` / 约 `3:4` | PNG |
| `DamageNumberBurst.png` | `Assets/Resources/Art/BattleCards/UI/DamageNumberBurst.png` | 生命徽章上方伤害数字使用的黄色橙边爆炸底板；中央无字，轮廓外真实透明 | `1448 × 1086` / 约 `4:3` | PNG |
| `ChargeHornIcon.png` | `Assets/Resources/Art/BattleCards/UI/ChargeHornIcon.png` | 冲锋发动时显示的黄铜弯号、皮革握把与红色飘带图标 | `1254 × 1254` / `1:1` | PNG |
| `LongShotBowIcon.png` | `Assets/Resources/Art/BattleCards/UI/LongShotBowIcon.png` | 远射发动时显示的简化短弓与单箭符号；小箭头、银色箭身与蓝色箭羽 | `1254 × 1254` / `1:1` | PNG |
| `CardFrameBlue-v2.png` | `Assets/Resources/Art/BattleCards/UI/CardFrameBlue-v2.png` | 蓝金卡框历史变体；资源保留，当前 Prefab 与 Controller 均不引用 | `1024 × 1536` / `2:3` | PNG |
| `AttackSwordBadge.png` | `Assets/Resources/Art/BattleCards/UI/AttackSwordBadge.png` | 带剑红金攻击力徽章历史版本；资源保留，当前 Prefab 不引用 | `1254 × 1254` / `1:1` | PNG |
| `AttackBadgeFrame.png` | `Assets/Resources/Art/BattleCards/UI/AttackBadgeFrame.png` | 当前无剑红金攻击力外框，中央深红区域供粗体数字覆盖 | `1254 × 1254` / `1:1` | PNG |
| `HealthDropBadge.png` | `Assets/Resources/Art/BattleCards/UI/HealthDropBadge.png` | 绿色血滴生命值徽章，供 TMP 数字覆盖 | `1254 × 1254` / `1:1` | PNG |
| `CardNumberBadgeHex.png` | `Assets/Resources/Art/BattleCards/UI/CardNumberBadgeHex.png` | 深灰金属六边形编号底框，供左上角白色 TMP 编号覆盖 | `384 × 256` / `3:2` | PNG |
| `BattleAttackSwordSlash.png` | `Assets/Resources/Art/BattleCards/Effects/BattleAttackSwordSlash.png` | 哥布林战士命中目标时播放的八帧冷白剑痕图集 | `1536 × 1024` / `4 × 2` 帧布局 | PNG |
| `BattleAttackArrowImpact.png` | `Assets/Resources/Art/BattleCards/Effects/BattleAttackArrowImpact.png` | 哥布林弓手从斜上方射入目标卡面的八帧箭矢图集 | `1774 × 887` / `4 × 2` 帧布局 | PNG |
| `BattleAttackSmallExplosion.png` | `Assets/Resources/Art/BattleCards/Effects/BattleAttackSmallExplosion.png` | 哥布林投弹手命中目标时播放的八帧小型爆炸图集 | `1774 × 887` / `4 × 2` 帧布局 | PNG |
| `BattleAttackSmallImpact.png` | `Assets/Resources/Art/BattleCards/Effects/BattleAttackSmallImpact.png` | 野猪命中目标时播放的八帧小型击打图集 | `1774 × 887` / `4 × 2` 帧布局 | PNG |
| `BattleAttackLargeImpact.png` | `Assets/Resources/Art/BattleCards/Effects/BattleAttackLargeImpact.png` | 食人魔命中目标时播放的八帧大型击打图集 | `1774 × 887` / `4 × 2` 帧布局 | PNG |
| `BattleVictoryBanner.png` | `Assets/Resources/Art/BattleCards/Result/BattleVictoryBanner.png` | 单场胜利使用的蓝金无字横幅；尖形两端强化横向滑动方向，中央供 TMP 叠加“战斗胜利” | `1983 × 793` / 约 `5:2` | PNG |
| `BattleDefeatPanel.png` | `Assets/Resources/Art/BattleCards/Result/BattleDefeatPanel.png` | 战斗失败重开弹窗使用的暗红铁金无字面板 | `1517 × 1037` / 约 `3:2` | PNG |
| `RunVictoryPanel.png` | `Assets/Resources/Art/BattleCards/Result/RunVictoryPanel.png` | 全部轮次完成重开弹窗使用的蓝金庆典无字面板 | `1519 × 1036` / 约 `3:2` | PNG |

无版本后缀的概念图片保留为第一版探索稿，`-v2` 概念图片为当前主参考版本；运行界面使用独立战场底板、独立怪物 Sprite、一张按运行时等阶着色的轻薄透明直边矩形卡框、属性徽章和 Prefab 内静态编号底框，不直接裁切横向概念同页。本次四阶变化复用既有中性框，没有新增四张重复边框位图。
