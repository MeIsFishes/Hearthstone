# Hearthstone UI 美术文档

## 1. 文档范围

本文档记录当前主要 UI 界面的二维视觉构成、通用风格分组与实际图片资产使用情况。项目级 UI 形状、线条、色彩和纹理规则以 `AutoDoc/Art/Style/art-style-overview.md` 为准；模块专属表现以对应模块美术文档为准。

当前信息来自 `Assets/Resources/Ui/` 下实际 Prefab、`Assets/Resources/Art/` 下 Sprite 以及 Unity Editor 中的 Prefab 引用检查。战斗界面以基础 Image、TextMesh Pro、木质金边战场底板、五张怪物原画、红蓝两套透明卡框和三张属性/编号透明 Sprite 共同构成；备战界面使用独立页面与交互 Sprite；Loading 界面使用一张无文字的全屏静态背景。文字统一使用 Noto Sans SC，字体资产不计入下方二维图片资产表。

## 2. UI 通用资产分组

### 2.1 UI-STYLE-001 明亮红蓝金奇幻卡牌界面

**风格说明**：竖向轻薄直边矩形红金/蓝金阵营卡框、窄暖金金属包边、木质与羊皮纸底板、低明度宽裕说明区和宝石质感属性徽章；装饰细节集中在边缘或低对比环境区，主要内容区保持清晰。Loading 使用同一木质、暖金和深蓝语言的低照度过渡变体。

**适用范围**：当前战斗界面、备战界面、两者卡牌及 Loading 界面。

| 资产或资产组 | 类型 | 通用用途 | 项目内路径 | 尺寸 / 格式 | 状态与视觉变体 | 复用约束 |
| --- | --- | --- | --- | --- | --- | --- |
| 阵营卡面边框 | 边框 | 战斗与备战怪物卡的完整外轮廓、阵营装饰层和攻击/目标高亮轮廓 | `Assets/Resources/Art/BattleCards/UI/CardFrame-v3.png`、`Assets/Resources/Art/BattleCards/UI/CardFrameBlue-v2.png` | 均为 `1024 × 1536` / PNG | 敌方红金、我方蓝金两种视觉变体；攻击者与目标状态复用当前阵营变体 | 两个版本共用直边矩形轻薄轮廓和真实 Alpha；战斗基础框与高亮层贴合 `250 × 360` 卡面四边并覆盖整个卡面，备战卡片同样拉伸到各自完整卡面容器；框层位于属性和编号标志下方 |
| 攻击力红金外框 | 属性徽章 | 战斗卡右下角攻击力底框 | `Assets/Resources/Art/BattleCards/UI/AttackBadgeFrame.png` | `1254 × 1254` / PNG | 当前单一无剑红金盾框版本 | 保持正方形比例、真实 Alpha 和完整盾形轮廓；中央留出深红数值区，不绘制剑、数字或文字 |
| 生命值血滴徽章 | 属性徽章 | 战斗卡左下角生命值底框 | `Assets/Resources/Art/BattleCards/UI/HealthDropBadge.png` | `1254 × 1254` / PNG | 当前单一绿金血滴版本 | 保持正方形比例和完整血滴轮廓；数字由白色粗体 TMP 叠加，不写入图片 |
| 卡牌编号六边形框 | 编号底框 | 战斗卡左上角编号衬底 | `Assets/Resources/Art/BattleCards/UI/CardNumberBadgeHex.png` | `384 × 256` / PNG | 当前单一深灰金属版本 | 保持 `3:2` 比例和透明外缘；白色编号由 TMP 叠加，不写入图片 |

## 3. 当前主要 UI 界面

| 界面名称 | 界面用途 | 主要视觉区域 | UI 风格分组 ID | 使用的通用资产 | 专属美术资产及路径 |
| --- | --- | --- | --- | --- | --- |
| 战斗界面 | 展示双方卡牌、卡牌编号、战斗状态与胜负结果 | 木质金边羊皮纸战场底板、上下两排正向竖向卡牌、敌方红金/我方蓝金主体窄框、框外左生命/右攻击徽章、主体原画、放大的底部说明区、左上角灰色六边形编号框和中央状态文字 | `UI-STYLE-001` | 红蓝阵营卡面边框、卡牌编号六边形框、攻击力红金外框、生命值血滴徽章 | 战场底板：`Assets/Resources/Art/BattleCards/UI/BattleBoardBackground.png`；五张怪物原画：`Assets/Resources/Art/BattleCards/*.png` |
| 备战界面 | 展示本轮奖励、`01~99` 固定卡池、三个战斗槽和四个融合素材槽，并支持编成、融合与继续下一关 | 暖色羊皮纸上区、深蓝卡池、红金双页签、融合合计面板、融合三态按钮、右上红金继续四态按钮、素材角标、纵向滚动条和槽位高亮 | `UI-STYLE-001` | 卡面边框、卡牌编号六边形框、攻击力红金外框、生命值血滴徽章、卡牌原画 | `Assets/Resources/Art/Preparation/UI/*.png` 共 23 张专属页面与交互 Sprite；99 专属原画 `FusionCard_099.png` |
| Loading 界面 | 在初次加载和战斗/备战阶段切换期间遮挡底层画面 | 深棕雕花木质卡桌、六张蓝金卡背、中央蓝色水晶与环形金属法阵、暖色酒馆器物、四周深色暗角 | `UI-STYLE-001` | 使用同组木质、暖金、深蓝与宝石视觉语言，不复用单独图片 | `Assets/Resources/Art/Loading/UI/HearthstoneLoadingBackground.png` |

## 4. UI 美术资产缺失
