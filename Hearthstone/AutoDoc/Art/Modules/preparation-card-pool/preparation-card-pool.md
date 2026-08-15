# 备战卡池界面美术文档

## 1. 风格与布局

备战界面沿用 `UI-STYLE-001` 的红、蓝、金奇幻卡牌语言。上区以红金页签切换三个出战槽与四个融合素材槽、合计面板和融合按钮；下区使用深蓝卡池面板承载 7 列卡位和纵向滚动条。文字和编号由 `NotoSansSC-Dynamic SDF` TMP FontAsset 叠加，不写入位图。

完整卡面复用 BattleCards 的卡框、编号六边形、攻击/生命徽章和编号对应原画，但由独立 Preparation Prefab 编排，不修改战斗卡牌 Prefab。

## 2. 专属位图

专属 PNG 均位于 `Assets/Resources/Art/Preparation/UI/`：

| 资产 | 用途 |
| --- | --- |
| `PreparationPageBackground.png` | 16:9 页面背景 |
| `PreparationStageTitleFrame.png` | 顶部标题底框 |
| `PreparationRewardPanel.png` | 本轮奖励提示底框 |
| `PreparationSectionLine.png` | 战斗槽标题两侧分隔线 |
| `PreparationBattleSlotFrame.png` | 三个战斗槽的空态框 |
| `PreparationCardPoolPanel.png` | 深蓝卡池外框 |
| `PreparationPoolEmptySlot.png` | 未持有编号空态 |
| `PreparationScrollTrack.png` | 纵向滚动轨道 |
| `PreparationScrollThumb.png` | 滚动滑块 |
| `PreparationScrollArrow.png` | 上下方向装饰箭头；下箭头由 Image 旋转复用 |
| `PreparationDropHighlight.png` | 有效槽位悬停高亮，Builder 使用 0.72 Alpha |
| `PreparationTabIdle.png`、`PreparationTabSelected.png` | 出战/融合页签的未选与带金色结构指示的选中底框 |
| `PreparationFusionSlotFrame.png` | 四个融合素材槽的空态框 |
| `PreparationFusionSumPanel.png` | 素材表达式、当前和及目标 99 的底框 |
| `PreparationFusionButtonDisabled.png`、`PreparationFusionButtonEnabled.png`、`PreparationFusionButtonPressed.png` | 融合按钮三态 SpriteSwap |
| `PreparationMaterialSelected.png` | 共享池条目的融合素材选中角标 |
| `PreparationContinueButtonIdle.png` | 右上继续按钮常态红金外框 |
| `PreparationContinueButtonHighlighted.png` | 继续按钮悬停高亮态 |
| `PreparationContinueButtonPressed.png` | 继续按钮按压态 |
| `PreparationContinueButtonWaiting.png` | 已接收请求后的降饱和等待态 |

所有位图由 Builder 统一校验为 Single Sprite、Alpha Is Transparency、无 Mipmap、Clamp。分隔线和轨道素材带透明画布留白，Builder 通过独立 Rect 与非等比显示范围保证可见轮廓；这两项只用于装饰框线，不承载图形比例语义。

## 3. Unity 资产

- 页面：`Assets/Resources/Ui/PreparationView.prefab`
- 卡池条目：`Assets/Resources/Ui/PreparationCardItem.prefab`
- 槽位条目：`Assets/Resources/Ui/PreparationSlotItem.prefab`
- 融合槽条目：`Assets/Resources/Ui/PreparationFusionSlotItem.prefab`
- UI 编辑场景：`Assets/Scenes/Ui/Preparation.unity`
- 导出资产：`Assets/Resources/Ui/Preparation.asset`

99 号专属原画为 `Assets/Resources/Art/BattleCards/FusionCard_099.png`。它不含卡框、编号或文字，在池、融合与战斗卡面中继续与既有卡框、灰色编号六边形和攻血徽章组合。
