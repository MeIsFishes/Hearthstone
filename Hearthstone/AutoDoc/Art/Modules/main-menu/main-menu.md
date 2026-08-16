# 主菜单美术模块文档

## 1. 模块范围

本模块记录《99升变》主菜单的全屏背景、羊皮纸表面、两侧哥布林壁画、独立绘制标题，以及“开始游戏”“图鉴”共用的悬停水渍。标题是透明背景图片；两个中央入口本体均为界面文字，常态不显示底板。右上角临时“清除数据”使用红色文字和透明点击区，不增加图片底框；右下角版本号使用黑色小字，不增加独立图片资产。

## 2. 模块风格

主体为暖棕、象牙色和旧金色的做旧羊皮纸，包含纸张纤维、折痕、磨损边缘与低对比角花。中央约六成宽度保持干净留白。左侧持剑盾哥布林朝右，右侧弓箭哥布林朝左；两者使用后中世纪木板蛋彩画式的平面轮廓、矿物颜料、细小龟裂和颜料脱落，以褪色半透明壁画的层级退居于界面内容。独立标题使用平均饱和度约 `0.19` 的旧黄铜灰、烟褐和暗木浮雕，避免亮金；开始入口的悬停底纹平均饱和度约 `0.04`，只表现灰褐湿痕和羊皮纸纤维扩散。

## 3. UI 资产风格

| UI 资产或资产组 | UI 风格分组 ID | 分组名称 | 适用界面或区域 |
| --- | --- | --- | --- |
| 主菜单羊皮纸背景、绘制标题与湿润悬停纹理 | `UI-STYLE-001` | 做旧羊皮纸奇幻卡牌界面 | 《99升变》主菜单背景、标题、开始游戏与图鉴入口 |

## 4. 图标规格

当前无独立图标资产。

## 5. 人物规格

当前无可拆分的人物图片；两名哥布林是封面背景的固定壁画内容。

## 6. 场景规格

当前无可拆分的场景图片；羊皮纸表面与边缘装饰与封面合成为同一资产。

## 7. 物件规格

当前无独立物件资产。

## 8. 参考图片

| 参考图片 | 来源或项目内路径 | 参考特征 | 适用范围 |
| --- | --- | --- | --- |
| ![主菜单封面](../../../../Assets/Resources/Art/MainMenu/UI/MainMenuCover.png) | `Assets/Resources/Art/MainMenu/UI/MainMenuCover.png` | 做旧羊皮纸、中央留白、两侧相向板绘哥布林 | 主菜单全屏背景 |
| ![主菜单标题](../../../../Assets/Resources/Art/MainMenu/UI/MainMenuTitle.png) | `Assets/Resources/Art/MainMenu/UI/MainMenuTitle.png` | 低饱和旧黄铜、烟褐、暗木浮雕与轻油画纹理 | 主菜单上方游戏名 |
| ![开始按钮悬停水渍](../../../../Assets/Resources/Art/MainMenu/UI/MainMenuStartHoverWetParchment.png) | `Assets/Resources/Art/MainMenu/UI/MainMenuStartHoverWetParchment.png` | 低饱和灰褐色湿痕、纤维扩散和透明软边 | “开始游戏”鼠标悬停与按下底纹 |
| ![哥布林战士原画](../../../../Assets/Resources/Art/BattleCards/GoblinWarrior.png) | `Assets/Resources/Art/BattleCards/GoblinWarrior.png` | 护甲、剑盾和哥布林体态 | 左侧壁画造型参考 |
| ![哥布林弓手原画](../../../../Assets/Resources/Art/BattleCards/GoblinArcher.png) | `Assets/Resources/Art/BattleCards/GoblinArcher.png` | 弓箭、头带和哥布林体态 | 右侧壁画造型参考 |

## 9. 目前已有资产列表

| 资产名称 | 项目内路径 | 图片内容与用途 | 尺寸 / 比例 | 文件格式 |
| --- | --- | --- | --- | --- |
| `MainMenuCover.png` | `Assets/Resources/Art/MainMenu/UI/MainMenuCover.png` | 主菜单羊皮纸全屏背景 | `1672 × 941 px` / 约 `16:9` | PNG |
| `MainMenuTitle.png` | `Assets/Resources/Art/MainMenu/UI/MainMenuTitle.png` | 独立绘制的低饱和“99升变”透明标题 | `1672 × 941 px` / 约 `16:9` | PNG |
| `MainMenuStartHoverWetParchment.png` | `Assets/Resources/Art/MainMenu/UI/MainMenuStartHoverWetParchment.png` | “开始游戏”和“图鉴”悬停与按下时共用的灰褐湿润羊皮纸纹理 | `2048 × 768 px` / 约 `8:3` | PNG |
