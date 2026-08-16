# Hearthstone UI 美术文档

## 1. 文档范围

本文档记录当前主要 UI 界面的二维视觉构成、通用风格分组与实际图片资产使用情况。项目级 UI 形状、线条、色彩和纹理规则以 `AutoDoc/Art/Style/art-style-overview.md` 为准；模块专属表现以对应模块美术文档为准。

当前信息来自 `Assets/Resources/Ui/` 下实际 Prefab、`Assets/Resources/Art/` 下 Sprite 以及 Unity Editor 中的 Prefab 引用检查。主菜单使用做旧羊皮纸与两侧哥布林板绘壁画组成的无文字全屏背景，并叠加低饱和绘制标题与湿润羊皮纸悬停纹理；战斗界面以基础 Image、TextMesh Pro、木质金边战场底板、透明做旧层、透明刀刻分隔线、右下角羽毛笔与印章、左上角越界匕首、五张怪物原画、一张按等阶着色的透明卡框、三张属性/编号透明 Sprite，以及整合固定文字的一张胜利横幅和两张结果面板共同构成；备战界面以独立页面与交互 Sprite 为主，标题框、页签、继续、融合、智能推荐和推荐项选择统一复用低饱和羊皮纸木框控件，并在智能推荐弹窗复用战场底板。动态文字统一使用 Noto Sans SC，字体资产不计入下方二维图片资产表。

## 2. UI 通用资产分组

### 2.1 UI-STYLE-001 做旧羊皮纸奇幻卡牌界面

**风格说明**：整体介于陈旧羊皮纸与克制的经典奇幻卡牌游戏质感之间。竖向轻薄直边矩形等阶卡框、窄金属包边、旧木与羊皮纸底板、低明度宽裕说明区和宝石质感属性徽章构成基础语言；中性银白卡框由界面着色为铜、银、金、传奇四阶或备战悬停黄，敌我阵营不再改变基础框色。战斗与备战羊皮纸表面共用一张透明做旧层，以低透明度的零星淡斑、水渍边和短划痕打破大面积平整底色，主要内容区仍保持清晰。结算 UI 增加轻油画笔触、磨损布料、低饱和古金与黑铁细边，蓝金用于胜利、暗红黑用于失败，避免塑料高光、圆润玩具感和过量装饰。Loading 使用同一木质、暖金和深蓝语言的低照度过渡变体。

**适用范围**：当前主菜单、图鉴、战斗界面、备战界面、共享卡牌及 Loading 界面。

| 资产或资产组 | 类型 | 通用用途 | 项目内路径 | 尺寸 / 格式 | 状态与视觉变体 | 复用约束 |
| --- | --- | --- | --- | --- | --- | --- |
| 可着色卡面边框 | 边框 | 战斗与备战怪物卡的完整外轮廓、等阶装饰层和攻击/目标高亮轮廓 | `Assets/Resources/Art/BattleCards/UI/CardFrame-v3.png` | `1024 × 1536` / PNG | 同一中性银白 Sprite 由 `Image.color` 生成铜 `#B87333`、银 `#C0CCD8`、金 `#E7A93B`、传奇紫 `#B25CFF` 和备战悬停黄 `#FFD230`；黄色悬停只用于备战已持有卡 | 保持直边矩形轻薄轮廓和真实 Alpha；基础框与高亮层共用同一 Sprite，左右与上边贴合卡面，底边相对卡底上移；框层位于属性和编号标志下方 |
| 攻击力红金外框 | 属性徽章 | 战斗卡右下角攻击力底框 | `Assets/Resources/Art/BattleCards/UI/AttackBadgeFrame.png` | `1254 × 1254` / PNG | 当前单一无剑红金盾框版本；覆盖的 TMP 数字按入场基准使用红、白、蓝三态 | 保持正方形比例、真实 Alpha 和完整盾形轮廓；中央留出深红数值区，不绘制剑、数字或文字 |
| 生命值血滴徽章 | 属性徽章 | 战斗卡左下角生命值底框 | `Assets/Resources/Art/BattleCards/UI/HealthDropBadge.png` | `1254 × 1254` / PNG | 当前单一绿金血滴版本；覆盖的 TMP 数字按入场基准使用红、白、蓝三态 | 保持正方形比例和完整血滴轮廓；数字不写入图片，红 `#FF5C5C` 表示降低、白 `#FFFFFF` 表示相等、蓝 `#58B0FF` 表示提高 |
| 卡牌编号六边形框 | 编号底框 | 战斗卡左上角编号衬底 | `Assets/Resources/Art/BattleCards/UI/CardNumberBadgeHex.png` | `384 × 256` / PNG | 当前单一深灰金属版本 | 保持 `3:2` 比例和透明外缘；白色编号由 TMP 叠加，不写入图片 |
| 木质金边羊皮纸底板 | 面板 / 背景 | 战斗棋盘全屏底板、备战智能推荐弹窗与智能推荐悬浮说明框 | `Assets/Resources/Art/BattleCards/UI/BattleBoardBackground.png`、`BattleBoardBackgroundAged.png` | 均为 `1672 × 941` / PNG | 原版由备战推荐与悬浮说明框复用；战斗专用做旧版增加不规则水渍、磨痕、短划痕与边缘积垢并全屏显示 | 保持木质金边和中央可读区域；战斗使用专用做旧版，其他复用区域继续使用原版，悬浮说明框允许横向适配短文本但不得叠加白色内容底块 |
| 羊皮纸做旧层 | 透明表面纹理 | 战斗棋盘与备战上区的共享羊皮纸旧化 | `Assets/Resources/Art/Preparation/UI/ParchmentAgingOverlay.png` | `1672 × 941` / PNG | 当前单一低对比淡褐色版本；战斗与备战分别以 `42%`、`18%` 整体 Alpha 叠加 | 必须保持真实透明背景、稀疏不对称分布和中央低干扰；不得烘入底色、边框、文字或规则性重复纹样 |
| 中世纪羊皮纸交互框 | 按钮 / 页签 / 标题框 | 备战阶段标题、页签、继续、融合、智能推荐与推荐项选择 | `Assets/Resources/Art/Common/UI/MedievalParchmentControl.png` | `2048 × 768` / PNG | 单一低饱和旧羊皮纸、深胡桃木与暗古铜 Sprite；悬停、按下、禁用和页签选中状态由界面着色 | 保持真实 Alpha；允许按横向控件尺寸拉伸，文字始终使用深棕色叠加；不得重新加入红漆、宝石或塑料高光 |

## 3. 当前主要 UI 界面

| 界面名称 | 界面用途 | 主要视觉区域 | UI 风格分组 ID | 使用的通用资产 | 专属美术资产及路径 |
| --- | --- | --- | --- | --- | --- |
| 主菜单 | 显示《99升变》名称，并提供新一局与图鉴入口 | 做旧羊皮纸铺满横屏，左侧持剑盾哥布林朝右、右侧弓箭哥布林朝左；中央上方叠加低饱和旧铜与暗木绘制标题，“开始游戏”和“图鉴”常态只有深棕文字，悬停或按下时共用半透明灰褐湿润羊皮纸痕迹；右上角调试清除入口为无底框红字，右下角版本号为无底框黑色小字 | `UI-STYLE-001` | — | `Assets/Resources/Art/MainMenu/UI/MainMenuCover.png`、`MainMenuTitle.png`、`MainMenuStartHoverWetParchment.png` |
| 卡牌图鉴 | 浏览永久解锁状态并放大查看卡面 | 复用备战页暖色羊皮纸背景、深蓝卡池面板与七列共享卡面；左上角放置低饱和羊皮纸“返回”按钮，右上角以同类木框承载右对齐的深棕“已解锁 k/n”，两者均保持在卡池面板上层；未解锁项使用 99 号封印锁面但不显示“融合封印”文字；预览层以纯灰黑半透明蒙板压暗背景，不增加卡牌灰色底板 | `UI-STYLE-001` | 中世纪羊皮纸交互框、卡面边框、卡牌编号六边形框、攻击力红金外框、生命值血滴徽章 | 页面与卡池资产复用 `Assets/Resources/Art/Preparation/UI/PreparationPageBackground.png`、`PreparationCardPoolPanel.png`；锁面复用 `Assets/Resources/Art/BattleCards/FusionCard_099.png` |
| 战斗界面 | 展示二至六张敌方卡、随轮次解锁的二至六张玩家卡、卡牌编号、战斗状态与胜负结果 | 带可见水渍、磨痕、短划痕和边缘积垢的木质金边羊皮纸战场底板、中央无文字的暖棕刀刻分隔线、右下角羽毛笔与印章、左上角刀尖朝向左下、带轻微灰褐污渍与失光且主体几乎完整的旧匕首、上下两排按 `278 px` 中心距横向排开并随数量整体居中的正向竖向卡牌、按铜银金传奇着色的主体窄框、框外左生命/右攻击徽章、按入场基准呈红白蓝三态的属性数字、主体原画、放大的底部说明区、左上角灰色六边形编号框，以及左入右出的蓝金旧布胜利横幅、暗红黑旧木失败弹窗和蓝金旧木整局胜利弹窗；匕首仅手柄顶端轻微越界，固定标题与说明已绘入完整面板，底部共用独立“返回主菜单”旧木红皮按钮 | `UI-STYLE-001` | 羊皮纸做旧层、可着色卡面边框、卡牌编号六边形框、攻击力红金外框、生命值血滴徽章 | 战场底板：`Assets/Resources/Art/BattleCards/UI/BattleBoardBackgroundAged.png`；刀刻分隔线：`Assets/Resources/Art/BattleCards/UI/BattleCenterDividerCarving.png`；角落装饰：`Assets/Resources/Art/BattleCards/UI/BattleCornerQuillStamp.png`、`Assets/Resources/Art/BattleCards/UI/BattleCornerDagger.png`；五张怪物原画：`Assets/Resources/Art/BattleCards/*.png`；结果资产：`Assets/Resources/Art/BattleCards/Result/BattleVictoryBannerAged.png`、`BattleDefeatPanelAged.png`、`RunVictoryPanelAged.png`、`ReturnToMainMenuButtonAged.png` |
| 备战界面 | 展示按副本展开的 `01~213` 卡池、随轮次解锁的三至六个战斗槽和四个融合素材槽，并支持编成、融合、智能推荐与继续本轮战斗 | 带稀疏做旧痕迹的暖色羊皮纸上区、按 `185 px` 步距排列的缩小出战卡位、深蓝滚动卡池、四阶着色共享卡面、黄色“已出战”旧木板标记，以及由同一低饱和旧羊皮纸、深胡桃木、暗古铜控件统一承载的阶段标题、双页签、继续、融合、智能推荐和推荐项选择；页签选中、悬停、按下与禁用通过克制的明度/透明度变化表达，不再引用亮红金框 | `UI-STYLE-001` | 中世纪羊皮纸交互框、羊皮纸做旧层、战场底板、卡面边框、卡牌编号六边形框、攻击力红金外框、生命值血滴徽章、卡牌原画 | `Assets/Resources/Art/Preparation/UI/*.png` 中的专属页面与交互 Sprite；共用控件 `Assets/Resources/Art/Common/UI/MedievalParchmentControl.png`；99 号封印原画 `Assets/Resources/Art/BattleCards/FusionCard_099.png` |
| Loading 界面 | 在初次加载和战斗/备战阶段切换期间遮挡底层画面 | 深棕雕花木质卡桌、六张蓝金卡背、中央蓝色水晶与环形金属法阵、暖色酒馆器物、四周深色暗角 | `UI-STYLE-001` | 使用同组木质、暖金、深蓝与宝石视觉语言，不复用单独图片 | `Assets/Resources/Art/Loading/UI/HearthstoneLoadingBackground.png` |

## 4. UI 美术资产缺失
