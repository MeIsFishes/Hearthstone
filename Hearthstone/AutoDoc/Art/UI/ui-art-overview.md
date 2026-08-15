# Hearthstone UI 美术文档

## 1. 文档范围

本文档记录当前主要 UI 界面的二维视觉构成、通用风格分组与实际图片资产使用情况。项目级 UI 形状、线条、色彩和纹理规则以 `AutoDoc/Art/Style/art-style-overview.md` 为准；模块专属表现以对应模块美术文档为准。

当前信息来自 `Assets/Resources/Ui/` 下实际 Prefab、`Assets/Resources/Art/` 下 Sprite 以及 Unity Editor 中的 Prefab 引用检查。主菜单使用做旧羊皮纸与两侧哥布林板绘壁画组成的无文字全屏背景；战斗界面以基础 Image、TextMesh Pro、木质金边战场底板、透明做旧层、透明刀刻分隔线、五张怪物原画、一张按等阶着色的透明卡框、三张属性/编号透明 Sprite，以及一张胜利横幅和两张结果面板共同构成；备战界面以独立页面与交互 Sprite 为主，并在智能推荐弹窗复用战场底板；Loading 界面使用一张无文字的全屏静态背景。文字统一使用 Noto Sans SC，字体资产不计入下方二维图片资产表。

## 2. UI 通用资产分组

### 2.1 UI-STYLE-001 明亮红蓝金奇幻卡牌界面

**风格说明**：竖向轻薄直边矩形等阶卡框、窄金属包边、木质与羊皮纸底板、低明度宽裕说明区和宝石质感属性徽章；中性银白卡框由界面着色为铜、银、金、传奇四阶或备战悬停黄，敌我阵营不再改变基础框色。战斗与备战羊皮纸表面共用一张透明做旧层，以低透明度的零星淡斑、水渍边和短划痕打破大面积平整底色，主要内容区仍保持清晰。Loading 使用同一木质、暖金和深蓝语言的低照度过渡变体。

**适用范围**：当前主菜单、战斗界面、备战界面、两者卡牌及 Loading 界面。

| 资产或资产组 | 类型 | 通用用途 | 项目内路径 | 尺寸 / 格式 | 状态与视觉变体 | 复用约束 |
| --- | --- | --- | --- | --- | --- | --- |
| 可着色卡面边框 | 边框 | 战斗与备战怪物卡的完整外轮廓、等阶装饰层和攻击/目标高亮轮廓 | `Assets/Resources/Art/BattleCards/UI/CardFrame-v3.png` | `1024 × 1536` / PNG | 同一中性银白 Sprite 由 `Image.color` 生成铜 `#B87333`、银 `#C0CCD8`、金 `#E7A93B`、传奇紫 `#B25CFF` 和备战悬停黄 `#FFD230`；黄色悬停只用于备战已持有卡 | 保持直边矩形轻薄轮廓和真实 Alpha；基础框与高亮层共用同一 Sprite，左右与上边贴合卡面，底边相对卡底上移；框层位于属性和编号标志下方 |
| 攻击力红金外框 | 属性徽章 | 战斗卡右下角攻击力底框 | `Assets/Resources/Art/BattleCards/UI/AttackBadgeFrame.png` | `1254 × 1254` / PNG | 当前单一无剑红金盾框版本；覆盖的 TMP 数字按入场基准使用红、白、蓝三态 | 保持正方形比例、真实 Alpha 和完整盾形轮廓；中央留出深红数值区，不绘制剑、数字或文字 |
| 生命值血滴徽章 | 属性徽章 | 战斗卡左下角生命值底框 | `Assets/Resources/Art/BattleCards/UI/HealthDropBadge.png` | `1254 × 1254` / PNG | 当前单一绿金血滴版本；覆盖的 TMP 数字按入场基准使用红、白、蓝三态 | 保持正方形比例和完整血滴轮廓；数字不写入图片，红 `#FF5C5C` 表示降低、白 `#FFFFFF` 表示相等、蓝 `#58B0FF` 表示提高 |
| 卡牌编号六边形框 | 编号底框 | 战斗卡左上角编号衬底 | `Assets/Resources/Art/BattleCards/UI/CardNumberBadgeHex.png` | `384 × 256` / PNG | 当前单一深灰金属版本 | 保持 `3:2` 比例和透明外缘；白色编号由 TMP 叠加，不写入图片 |
| 木质金边羊皮纸底板 | 面板 / 背景 | 战斗棋盘全屏底板、备战智能推荐弹窗与智能推荐悬浮说明框 | `Assets/Resources/Art/BattleCards/UI/BattleBoardBackground.png` | `1792 × 1008` / PNG | 当前单一暖色羊皮纸版本；战斗全屏显示，推荐弹窗按 `16:9` 等比区域显示，悬浮说明框横向压缩并叠加暖棕色调和深棕描边 | 保持木质金边和中央低干扰羊皮纸区域；悬浮说明框允许横向适配短文本，但不得叠加白色内容底块 |
| 羊皮纸做旧层 | 透明表面纹理 | 战斗棋盘与备战上区的共享羊皮纸旧化 | `Assets/Resources/Art/Preparation/UI/ParchmentAgingOverlay.png` | `1672 × 941` / PNG | 当前单一低对比淡褐色版本；战斗与备战分别以 `24%`、`18%` 整体 Alpha 叠加 | 必须保持真实透明背景、稀疏不对称分布和中央低干扰；不得烘入底色、边框、文字或规则性重复纹样 |

## 3. 当前主要 UI 界面

| 界面名称 | 界面用途 | 主要视觉区域 | UI 风格分组 ID | 使用的通用资产 | 专属美术资产及路径 |
| --- | --- | --- | --- | --- | --- |
| 主菜单 | 显示《99升变》名称并提供新一局入口 | 做旧羊皮纸铺满横屏，左侧持剑盾哥布林朝右、右侧弓箭哥布林朝左，两者均为低对比中世纪板绘壁画；中央留白叠加深棕金边游戏名与红金开始按钮 | `UI-STYLE-001` | 红金按钮四态 Sprite | `Assets/Resources/Art/MainMenu/UI/MainMenuCover.png` |
| 战斗界面 | 展示三张敌方卡、随轮次解锁的三至六张玩家卡、卡牌编号、战斗状态与胜负结果 | 做旧强度更明显的木质金边羊皮纸战场底板、中央无文字的暖棕刀刻分隔线、上下两排按 `278 px` 中心距横向排开并随数量整体居中的正向竖向卡牌、按铜银金传奇着色的主体窄框、框外左生命/右攻击徽章、按入场基准呈红白蓝三态的属性数字、主体原画、放大的底部说明区、左上角灰色六边形编号框，以及左入右出的蓝金胜利横幅、红黑失败弹窗和蓝金整局胜利弹窗 | `UI-STYLE-001` | 羊皮纸做旧层、可着色卡面边框、卡牌编号六边形框、攻击力红金外框、生命值血滴徽章 | 战场底板：`Assets/Resources/Art/BattleCards/UI/BattleBoardBackground.png`；刀刻分隔线：`Assets/Resources/Art/BattleCards/UI/BattleCenterDividerCarving.png`；五张怪物原画：`Assets/Resources/Art/BattleCards/*.png`；结果资产：`Assets/Resources/Art/BattleCards/Result/BattleVictoryBanner.png`、`BattleDefeatPanel.png`、`RunVictoryPanel.png` |
| 备战界面 | 展示按副本展开的 `01~213` 卡池、随轮次解锁的三至六个战斗槽和四个融合素材槽，并支持编成、融合、智能推荐与继续本轮战斗 | 带稀疏做旧痕迹的暖色羊皮纸上区、无“出战槽位”标题的出战区域、相邻槽间保留约 `24.6 px` 空隙的缩小出战卡位、深蓝滚动卡池、同编号连续排列的四阶着色共享卡面、卡牌进入较小出战槽与融合槽时保持比例缩小并居中的槽内构图、红金双页签、融合右侧两列控制区、使用加宽白色木纹底板的当前点数/剩余点数、左侧黑色中文说明与右侧独立数字、精确 99 闪光与超额红色数字反馈、略大的融合按钮、智能推荐三态按钮及其暖棕木纹悬浮说明框、无顶部标题提示的木质金边羊皮纸推荐弹窗、横排共享卡面组合、素材已选高亮标记、行右侧红金选择按钮、右上红金继续按钮、纵向滚动条、槽位高亮，以及分隔普通卡与融合卡的 99 号锻铁巨锁原画 | `UI-STYLE-001` | 羊皮纸做旧层、战场底板、卡面边框、卡牌编号六边形框、攻击力红金外框、生命值血滴徽章、卡牌原画 | `Assets/Resources/Art/Preparation/UI/*.png` 中 23 张专属页面与交互 Sprite；推荐弹窗与悬浮说明框复用 `Assets/Resources/Art/BattleCards/UI/BattleBoardBackground.png`；99 号封印原画 `Assets/Resources/Art/BattleCards/FusionCard_099.png` |
| Loading 界面 | 在初次加载和战斗/备战阶段切换期间遮挡底层画面 | 深棕雕花木质卡桌、六张蓝金卡背、中央蓝色水晶与环形金属法阵、暖色酒馆器物、四周深色暗角 | `UI-STYLE-001` | 使用同组木质、暖金、深蓝与宝石视觉语言，不复用单独图片 | `Assets/Resources/Art/Loading/UI/HearthstoneLoadingBackground.png` |

## 4. UI 美术资产缺失
