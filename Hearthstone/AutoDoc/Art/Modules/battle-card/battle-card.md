# 战斗卡牌美术模块文档

## 1. 模块范围

本模块覆盖战斗界面的木质金边羊皮纸底板、双方卡牌的怪物原画、说明区域、红金/蓝金直边矩形阵营卡框、左上角编号底框、生命与攻击徽章、攻击/目标高亮和死亡遮罩，也记录战斗场景与怪物卡面的现有概念资源。界面与卡面基础静态层级分别保存在 `Assets/Resources/Ui/BattleView.prefab` 和 `Assets/Resources/Ui/BattleCardItem.prefab`，运行 Sprite 位于 `Assets/Resources/Art/BattleCards/`，概念参考位于 `Assets/Art/ConceptArt/`。

## 2. 模块风格

可运行界面使用正俯视、三对三对称卡位和大面积浅暖羊皮纸留白，深棕木框、暖金嵌边、四角装饰与少量蓝宝石点缀界定战场。敌方卡牌使用轻薄红金直边矩形框，我方卡牌使用同轮廓轻薄蓝金直边矩形框；在 `250 × 360` 卡面根内，基础框、攻击者高亮与目标高亮统一贴合四边并覆盖完整 `250 × 360` 卡面，包围主体立绘与下侧说明栏，同时位于属性和编号标志下方。五张原始怪物 Sprite 保留 `2:3` 竖向环境构图，Prefab 通过约 `82.5%` 高度的主体视窗展示原画并保持原始宽高比，不做非等比拉伸；下方说明底板为 `180 × 75.6`。左上角编号采用 `58 × 38` 的横向六边形深灰金属底框与居中白色粗体数字；左下生命使用 `60 × 60` 绿金血滴，右下攻击使用 `60 × 60` 无剑红金盾框，两者叠加 `30` 号白色粗体描边数字。

## 3. UI 资产风格

| UI 资产或资产组 | UI 风格分组 ID | 分组名称 | 适用界面或区域 |
| --- | --- | --- | --- |
| 战斗卡牌边框与属性徽章 | `UI-STYLE-001` | 明亮红蓝金奇幻卡牌界面 | 战斗界面的双方卡牌 |

## 4. 图标规格

| 图标或图标组 | 画布尺寸 | 主体占比与安全区 | 形状与线条 | 配色 | 背景 / Alpha | 视觉变体 |
| --- | ---: | --- | --- | --- | --- | --- |
| 攻击力红金外框 | `1254 × 1254` | 主体约占画布 `82%`，四周保留透明安全区，中央保留大面积数值区 | 正面、对称的盾形红宝石底板，完整暖金包边与侧叶装饰，不含剑主体 | 深红宝石、暖金 | 真实 Alpha 透明 | 当前单一无剑版本 |
| 生命值血滴徽章 | `1254 × 1254` | 主体约占画布 `82%`，四周保留透明安全区 | 正面血滴轮廓、金属包边与少量叶形装饰，中央留数值区 | 祖母绿、暖金、亮绿色高光 | 真实 Alpha 透明 | 当前单一版本 |

## 5. 人物规格

## 6. 场景规格

## 7. 物件规格

## 8. 参考图片

| 参考图片 | 来源或项目内路径 | 参考特征 | 适用范围 |
| --- | --- | --- | --- |
| 炉石传说官方卡牌库 | https://hearthstone.blizzard.com/en-us/cards | 上部主视觉、下部说明、底部属性的信息层级 | 战斗卡面布局 |
| 战斗场景原画第二版 | `Assets/Art/ConceptArt/battle-scene-concept-v2.png` | 正俯视、上下各三卡、浅色简约底板与 UI 主导的攻击反馈 | 当前战斗场景构图与界面方向 |
| 运行战场底板 | `Assets/Resources/Art/BattleCards/UI/BattleBoardBackground.png` | 从主参考构图提取的无卡牌木质金边羊皮纸面板 | 当前战斗界面背景 |
| 怪物卡面原画第二版 | `Assets/Art/ConceptArt/monster-card-fronts-concept-v2.png` | 五种怪物、约三分之二人物占比、明亮奇幻环境与轻量笔触 | 当前怪物主视觉与卡框方向 |

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
| `BattleBoardBackground.png` | `Assets/Resources/Art/BattleCards/UI/BattleBoardBackground.png` | 木质金边羊皮纸战场底板；不含卡牌、角色、文字或攻击轨迹，作为 `BattleView` 全屏背景 | `1672 × 941` / 约 `16:9` | PNG |
| `CardFrame-v2.png` | `Assets/Resources/Art/BattleCards/UI/CardFrame-v2.png` | 较厚红金拱形卡面边框历史变体；资源保留，基础框与攻击/目标高亮均不引用 | `1024 × 1536` / `2:3` | PNG |
| `CardFrameBlue.png` | `Assets/Resources/Art/BattleCards/UI/CardFrameBlue.png` | 较厚蓝金拱形卡面边框历史变体；资源保留，当前 Prefab 不引用 | `1024 × 1536` / `2:3` | PNG |
| `CardFrame-v3.png` | `Assets/Resources/Art/BattleCards/UI/CardFrame-v3.png` | 当前敌方轻薄红金直边矩形卡面边框；中央和框外真实透明，覆盖整张卡面 | `1024 × 1536` / `2:3` | PNG |
| `CardFrameBlue-v2.png` | `Assets/Resources/Art/BattleCards/UI/CardFrameBlue-v2.png` | 当前我方轻薄蓝金直边矩形卡面边框；与红框共用全卡面轮廓和真实 Alpha | `1024 × 1536` / `2:3` | PNG |
| `AttackSwordBadge.png` | `Assets/Resources/Art/BattleCards/UI/AttackSwordBadge.png` | 带剑红金攻击力徽章历史版本；资源保留，当前 Prefab 不引用 | `1254 × 1254` / `1:1` | PNG |
| `AttackBadgeFrame.png` | `Assets/Resources/Art/BattleCards/UI/AttackBadgeFrame.png` | 当前无剑红金攻击力外框，中央深红区域供粗体数字覆盖 | `1254 × 1254` / `1:1` | PNG |
| `HealthDropBadge.png` | `Assets/Resources/Art/BattleCards/UI/HealthDropBadge.png` | 绿色血滴生命值徽章，供 TMP 数字覆盖 | `1254 × 1254` / `1:1` | PNG |
| `CardNumberBadgeHex.png` | `Assets/Resources/Art/BattleCards/UI/CardNumberBadgeHex.png` | 深灰金属六边形编号底框，供左上角白色 TMP 编号覆盖 | `384 × 256` / `3:2` | PNG |

无版本后缀的概念图片保留为第一版探索稿，`-v2` 概念图片为当前主参考版本；运行界面使用独立战场底板、独立怪物 Sprite、红蓝两套轻薄透明直边矩形卡框、属性徽章和 Prefab 内静态编号底框，不直接裁切横向概念同页。
