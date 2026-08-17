# Hearthstone UI 美术文档

## 1. 文档范围

本文档记录当前主要 UI 界面的二维视觉构成、通用风格分组与实际图片资产使用情况。项目级 UI 形状、线条、色彩和纹理规则以 `AutoDoc/Art/Style/art-style-overview.md` 为准；模块专属表现以对应模块美术文档为准。

当前信息来自 `Assets/Resources/Ui/` 下实际 Prefab、`Assets/Resources/Art/` 下 Sprite 以及 Unity Editor 中的 Prefab 引用检查。主菜单使用做旧羊皮纸与两侧哥布林板绘壁画组成的无文字全屏背景，并叠加低饱和绘制标题与湿润羊皮纸悬停纹理；战斗界面以基础 Image、TextMesh Pro、木质金边战场底板、透明做旧层、透明刀刻分隔线、右下角羽毛笔与印章、左上角越界匕首、五张怪物原画、一张按等阶着色的透明卡框、三张属性/编号透明 Sprite，以及结算时的半透明深灰蒙板和三张已经绘入结算文字的结果图片共同构成；普通胜利和最终胜利为蓝金横幅，失败为同尺寸横向轮廓的红黑破损战旗横幅。备战界面以独立页面与交互 Sprite 为主，标题框、页签、继续、融合、智能推荐和推荐项选择统一复用低饱和羊皮纸木框控件，并在智能推荐弹窗复用战场底板；首次备战的新手引导继续使用灰色压暗层、薄木框做旧羊皮纸面板、共享完整卡面和一张无文字无箭头的双排战斗顺序插画。动态文字统一使用 Noto Sans SC，结算文字已烘焙进位图；字体资产不计入下方二维图片资产表。

## 2. UI 通用资产分组

### 2.1 UI-STYLE-001 做旧羊皮纸奇幻卡牌界面

**风格说明**：整体介于陈旧羊皮纸与克制的经典奇幻卡牌游戏质感之间。竖向圆角哥特金属等阶卡框、连续上下横梁、轻微镂空四角、旧木与羊皮纸底板、低明度宽裕说明区和宝石质感属性徽章构成基础语言；中性银白卡框由界面着色为铜、银、金、传奇四阶或备战悬停黄，敌我阵营不再改变基础框色。战斗与备战羊皮纸表面共用一张透明做旧层，以低透明度的零星淡斑、水渍边和短划痕打破大面积平整底色，主要内容区仍保持清晰。胜利与最终胜利结算使用轻油画笔触、皇家蓝布料、暖金细边和羊皮纸铭牌组成的透明横幅；失败结算沿用相同横向形状和铭牌布局，改用暗红破损战旗、黑铁旧铜、裂盾与交叉残剑。三张成品都把楷书感金色浮雕文字直接绘入图片，避免塑料高光、圆润玩具感和运行时文字叠加。Loading 使用同一木质、暖金和深蓝语言的低照度过渡变体。

**适用范围**：当前主菜单、图鉴、战斗界面、备战界面、备战新手引导、共享卡牌及 Loading 界面。

| 资产或资产组 | 类型 | 通用用途 | 项目内路径 | 尺寸 / 格式 | 状态与视觉变体 | 复用约束 |
| --- | --- | --- | --- | --- | --- | --- |
| 可着色圆角卡面边框 | 边框 | 战斗与备战怪物卡的完整外轮廓、等阶装饰层和攻击/目标高亮轮廓 | `Assets/Resources/Art/BattleCards/UI/CardFrameRoundedSubtleOpenCornersPreview.png` | `1024 × 1536` / PNG | 同一中性银白 Sprite 由 `Image.color` 生成铜 `#B87333`、银 `#C0CCD8`、金 `#E7A93B`、传奇紫 `#B25CFF` 和备战悬停黄 `#FFD230`；黄色悬停只用于备战已持有卡 | 保持上下横梁连续、四角只轻微镂空和真实 Alpha；基础框与高亮层共用同一 Sprite，框层位于属性和编号标志下方；原画遮罩的显示区域相对框体四边内缩 `2 px` |
| 卡牌原画圆角遮罩 | Alpha 遮罩 | 战斗与备战共享卡面的怪物原画裁切 | `Assets/Resources/Art/BattleCards/UI/CardArtworkRoundedMask.png` | `1024 × 1536` / PNG | 当前单一圆角轮廓 | 保留原始圆角 Alpha 轮廓和非压缩导入设置；运行时在 `250 × 336` 卡框主体内四边内缩 `2 px`，显示为 `246 × 332`，原画满铺该遮罩区域；不裁切名称、词条、编号、攻血等 UI |
| 攻击力红金外框 | 属性徽章 | 战斗卡右下角攻击力底框 | `Assets/Resources/Art/BattleCards/UI/AttackBadgeFrame.png` | `1254 × 1254` / PNG | 当前单一无剑红金盾框版本；覆盖的 TMP 数字按入场基准使用红、白、蓝三态 | 保持正方形比例、真实 Alpha 和完整盾形轮廓；中央留出深红数值区，不绘制剑、数字或文字 |
| 生命值血滴徽章 | 属性徽章 | 战斗卡左下角生命值底框 | `Assets/Resources/Art/BattleCards/UI/HealthDropBadge.png` | `1254 × 1254` / PNG | 当前单一绿金血滴版本；覆盖的 TMP 数字按入场基准使用红、白、蓝三态 | 保持正方形比例和完整血滴轮廓；数字不写入图片，红 `#FF5C5C` 表示降低、白 `#FFFFFF` 表示相等、蓝 `#58B0FF` 表示提高 |
| 卡牌编号六边形框 | 编号底框 | 战斗卡左上角编号衬底 | `Assets/Resources/Art/BattleCards/UI/CardNumberBadgeHex.png` | `384 × 256` / PNG | 当前单一深灰金属版本 | 保持 `3:2` 比例和透明外缘；白色编号由 TMP 叠加，不写入图片 |
| 木质金边羊皮纸底板 | 面板 / 背景 | 战斗棋盘全屏底板、备战智能推荐弹窗与智能推荐悬浮说明框 | `Assets/Resources/Art/BattleCards/UI/BattleBoardBackground.png`、`BattleBoardBackgroundAged.png` | 均为 `1672 × 941` / PNG | 原版由备战推荐与悬浮说明框复用；战斗专用做旧版增加不规则水渍、磨痕、短划痕、边缘积垢及右下少量黑褐墨点并全屏显示 | 保持木质金边和中央可读区域；战斗使用专用做旧版，其他复用区域继续使用原版，悬浮说明框允许横向适配短文本但不得叠加白色内容底块 |
| 羊皮纸做旧层 | 透明表面纹理 | 战斗棋盘与备战上区的共享羊皮纸旧化 | `Assets/Resources/Art/Preparation/UI/ParchmentAgingOverlay.png` | `1672 × 941` / PNG | 当前单一低对比淡褐色版本；战斗与备战分别以 `42%`、`18%` 整体 Alpha 叠加 | 必须保持真实透明背景、稀疏不对称分布和中央低干扰；不得烘入底色、边框、文字或规则性重复纹样 |
| 中世纪羊皮纸交互框 | 按钮 / 页签 / 标题框 | 备战阶段标题、页签、继续、融合、智能推荐与推荐项选择 | `Assets/Resources/Art/Common/UI/MedievalParchmentControl.png` | `2048 × 768` / PNG | 单一低饱和旧羊皮纸、深胡桃木与暗古铜 Sprite；悬停、按下、禁用和页签选中状态由界面着色 | 保持真实 Alpha；允许按横向控件尺寸拉伸，文字始终使用深棕色叠加；不得重新加入红漆、宝石或塑料高光 |

## 3. 当前主要 UI 界面

| 界面名称 | 界面用途 | 主要视觉区域 | UI 风格分组 ID | 使用的通用资产 | 专属美术资产及路径 |
| --- | --- | --- | --- | --- | --- |
| 主菜单 | 显示《99升变》名称，并提供新一局、图鉴与退出游戏入口 | 无人物的做旧羊皮纸铺满横屏，右下纸角向内上翻并带柔和阴影；左侧剑盾哥布林和右侧弓箭哥布林以独立深棕剪影、少量浅棕线条和极慢轻微动作朝向中央；中央上方叠加低饱和旧铜与暗木绘制标题，三个中央按钮以统一字号紧凑排列在标题下方，常态只有深棕文字，悬停或按下时共用半透明湿润羊皮纸痕迹，“开始游戏”和“图鉴”为灰褐色，“退出游戏”为红色；右上角调试清除入口为无底框红字，左下角版本号为无底框黑色小字 | `UI-STYLE-001` | — | `Assets/Resources/Art/MainMenu/UI/MainMenuParchmentBackground.png`、`MainMenuGoblinWarriorFrames.png`、`MainMenuGoblinArcherFrames.png`、`MainMenuTitle.png`、`MainMenuStartHoverWetParchment.png` |
| 卡牌图鉴 | 浏览永久解锁状态并放大查看卡面 | 复用备战页暖色羊皮纸背景、深蓝卡池面板与七列共享卡面；左上角放置低饱和羊皮纸“返回”按钮，右上角以同类木框承载右对齐的深棕“已解锁 k/n”，两者均保持在卡池面板上层；未解锁项使用五种深棕做旧木纹空槽，并叠加两条从左右两侧上下四点固定、在中央形成 X 形交叉的旧锻铁链，交叉点由暗铁旧铜挂锁压住，覆盖图背景保持真实透明；预览层以纯灰黑半透明蒙板压暗背景，不增加卡牌灰色底板 | `UI-STYLE-001` | 中世纪羊皮纸交互框、卡面边框、卡牌编号六边形框、攻击力红金外框、生命值血滴徽章 | 页面与卡池资产复用 `Assets/Resources/Art/Preparation/UI/PreparationPageBackground.png`、`PreparationCardPoolPanel.png`、`PreparationPoolEmptySlotAgedWood01.png`～`05.png`；图鉴锁定覆盖图为 `Assets/Resources/Art/CardCollection/UI/CardCollectionLockedPadlock.png` |
| 战斗界面 | 展示二至六张敌方卡、随轮次解锁的二至六张玩家卡、卡牌编号、战斗状态与胜负结果 | 带可见水渍、磨痕、短划痕、边缘积垢及右下少量黑褐墨迹的木质金边羊皮纸战场底板、中央无文字的暖棕刀刻分隔线、右下角末端带湿黑墨且羽管无金属连接件的传统羽毛笔与带轻微木柄磨耗、古金暗沉及灰褐残留的印章、左上角刀尖朝向左下、带轻微灰褐污渍与失光且主体几乎完整的旧匕首、上下两排按 `278 px` 中心距横向排开并随数量整体居中的正向竖向卡牌、按铜银金传奇着色的主体窄框、框外左生命/右攻击徽章、按入场基准呈红白蓝三态的属性数字、主体原画、放大的底部说明区、左上角灰色六边形编号框，以及结算时覆盖战场的 `62%` 不透明度深灰蒙板和分别绘入“胜利”“失败”“最终胜利”的三张结果图片；横幅位于蒙板上方，胜利与最终胜利使用蓝金横幅，失败使用同尺寸横向轮廓的红黑破损战旗横幅，不叠加结果文字或独立按钮 | `UI-STYLE-001` | 羊皮纸做旧层、可着色卡面边框、卡牌编号六边形框、攻击力红金外框、生命值血滴徽章 | 战场底板：`Assets/Resources/Art/BattleCards/UI/BattleBoardBackgroundAged.png`；刀刻分隔线：`Assets/Resources/Art/BattleCards/UI/BattleCenterDividerCarving.png`；角落装饰：`Assets/Resources/Art/BattleCards/UI/BattleCornerQuillStamp.png`、`Assets/Resources/Art/BattleCards/UI/BattleCornerDagger.png`；五张怪物原画：`Assets/Resources/Art/BattleCards/*.png`；结果资产：`Assets/Resources/Art/BattleCards/Result/BattleVictoryBannerText.png`、`BattleDefeatBannerText.png`、`BattleFinalVictoryBannerText.png` |
| 备战界面 | 展示按副本展开的 `01~213` 卡池、随轮次解锁的三至六个战斗槽和四个融合素材槽，并支持编成、融合、智能推荐与继续本轮战斗 | 无卷边的暖色羊皮纸上区、按 `185 px` 步距排列的缩小出战卡位、由完整页面背景直接提供且不叠加独立边框的深蓝滚动卡池、四阶着色共享卡面、黄色“已出战”旧木板标记，以及由同一低饱和旧羊皮纸、深胡桃木、暗古铜控件统一承载的阶段标题、双页签、继续、融合、智能推荐和推荐项选择；融合素材区不再显示标题文字，四槽下方居中的方形“？”帮助按钮同样复用该做旧羊皮纸木框，页签选中、悬停、按下与禁用通过克制的明度/透明度变化表达，不再引用亮红金框 | `UI-STYLE-001` | 中世纪羊皮纸交互框、羊皮纸做旧层、战场底板、卡面边框、卡牌编号六边形框、攻击力红金外框、生命值血滴徽章、卡牌原画 | `Assets/Resources/Art/Preparation/UI/PreparationPageBackground.png` 与同目录交互 Sprite；共用控件 `Assets/Resources/Art/Common/UI/MedievalParchmentControl.png`；99 号封印原画 `Assets/Resources/Art/BattleCards/FusionCard_099.png` |
| 备战新手引导 | 说明完整卡牌信息、敌我行动轮次与精确 99 融合规则 | 全屏半透明灰色压暗层、薄深胡桃木外框和带稀疏旧化纹理的浅暖羊皮纸主面板；第一页复用完整共享卡面并以短线标注属性，第二页在无箭头的双排人物卡图上由界面直接叠加 `1~8` 顺序号，第三页使用三个编号徽章和一张融合结果卡示意属性与词条叠加 | `UI-STYLE-001` | 羊皮纸做旧层、可着色卡面边框、卡牌编号六边形框、攻击力红金外框、生命值血滴徽章、卡牌原画 | 战斗顺序插画：`Assets/Resources/Art/Tutorial/UI/PreparationBattleTurnOrder.png` |
| Loading 界面 | 在初次加载和战斗/备战阶段切换期间遮挡底层画面 | 深棕雕花木质卡桌、六张蓝金卡背、中央蓝色水晶与环形金属法阵、暖色酒馆器物、四周深色暗角 | `UI-STYLE-001` | 使用同组木质、暖金、深蓝与宝石视觉语言，不复用单独图片 | `Assets/Resources/Art/Loading/UI/HearthstoneLoadingBackground.png` |

## 4. UI 美术资产缺失
