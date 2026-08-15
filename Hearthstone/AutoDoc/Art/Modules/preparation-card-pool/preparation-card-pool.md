# 备战卡池界面美术文档

## 1. 风格与布局

备战界面沿用 `UI-STYLE-001` 的红、蓝、金奇幻卡牌语言。上区使用暖色羊皮纸和木金结构承载标题、奖励提示及三个战斗槽；下区使用深蓝卡池面板承载 7 列卡位和纵向滚动条。文字和编号由 `NotoSansSC-Dynamic SDF` TMP FontAsset 叠加，不写入位图；该字体覆盖标题、奖励反馈、分区文字及现有五种中文卡名。

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

所有位图由 Builder 统一校验为 Single Sprite、Alpha Is Transparency、无 Mipmap、Clamp。分隔线和轨道素材带透明画布留白，Builder 通过独立 Rect 与非等比显示范围保证可见轮廓；这两项只用于装饰框线，不承载图形比例语义。

## 3. Unity 资产

- 页面：`Assets/Resources/Ui/PreparationView.prefab`
- 卡池条目：`Assets/Resources/Ui/PreparationCardItem.prefab`
- 槽位条目：`Assets/Resources/Ui/PreparationSlotItem.prefab`
- UI 编辑场景：`Assets/Scenes/Ui/Preparation.unity`
- 导出资产：`Assets/Resources/Ui/Preparation.asset`
