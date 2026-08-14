# 战斗场景与五怪物卡实现报告

## 1. 任务结果

任务已完成。战斗界面已调整为俯视、上下各三卡的简约布局；五种怪物已建立独立配置与运行原画。根据用户追加要求，卡牌 Prefab 已进一步接入红金卡面边框、红色剑形攻击徽章和绿色血滴生命徽章，并通过上半区裁切视窗放大怪物原画主体。

本轮用户明确要求不进入游戏测试，因此最终验收仅在 Edit Mode 完成 Prefab、Sprite、配置、编译、测试与 Console 静态检查。

## 2. 主要产物

### 2.1 运行美术资源

- 五张怪物原画：`Assets/Resources/Art/BattleCards/` 下的 `GoblinWarrior.png`、`GoblinArcher.png`、`GoblinBomber.png`、`Boar.png`、`Ogre.png`，均为 `1024 × 1536` PNG。
- 卡面边框：`Assets/Resources/Art/BattleCards/UI/CardFrame-v2.png`，`1024 × 1536` PNG，中央与四角为 Alpha 透明。
- 攻击徽章：`Assets/Resources/Art/BattleCards/UI/AttackSwordBadge.png`，`1254 × 1254` PNG，透明背景。
- 生命徽章：`Assets/Resources/Art/BattleCards/UI/HealthDropBadge.png`，`1254 × 1254` PNG，透明背景。

### 2.2 Unity 资产

- `Assets/Resources/Ui/BattleView.prefab`：扩大浅色战场底板，双方 UiList 上下布置，中央保留回合与结果区域。
- `Assets/Resources/Ui/BattleCardItem.prefab`：新增 `ArtworkViewport`、`RectMask2D` 与 `CardFrameOverlay`；攻击/生命徽章改用独立 Sprite。
- 原画视窗覆盖卡面顶部 66%，内部原画按 `208 × 312` 保持 `2:3`，在 `208 × 199.2` 视窗中放大裁切；说明区位于底部 34%。
- Prefab 最终层级包含 14 个对象，`BattleCardItemView.ArtworkArea` 仍引用实际原画 Image；死亡、攻击者和目标覆盖层保留。

### 2.3 配置与逻辑

- `BattleCardCsvData` 新增 `DisplayName`、`ArtworkKey`，CSV 建立五种怪物的名称、原画键、攻血与说明。
- 初始阵容为我方 `[哥布林战士、野猪、食人魔]`、敌方 `[哥布林弓手、哥布林投弹手、哥布林战士]`，一次静态配置覆盖全部五种怪物。
- 卡牌 Controller 通过 `DataApi` 读取配置、通过 `ResourceApi.LoadSprite()` 加载原画；解绑时清空 Sprite 与文字并恢复正向状态。

## 3. 图像生成方式与最终提示词

使用内置 `image_gen` 模式生成，未使用 CLI/API fallback。三个资产分别生成并复制到项目，随后由 Unity MCP 导入为单图 Sprite。

### 3.1 卡面边框

> 生成一张 2:3 竖向透明游戏卡框 Sprite；红色漆木与暖金金属、拱形顶边、少量宝石装饰；正面、对称、边缘厚度统一；中央大窗口和框外均为真实透明；不含原画、徽章、文字、数字或品牌标识。

### 3.2 攻击徽章

> 生成一张透明正方形攻击属性徽章 Sprite；单柄直立奇幻剑、红色珐琅盾底和暖金包边，中央预留动态数字区域，缩小到 48–64 px 仍有清晰轮廓；不含文字、数字、场景或品牌标识。

### 3.3 生命徽章

> 生成一张透明正方形生命属性徽章 Sprite；祖母绿血滴、暖金包边和少量叶形装饰，中央预留动态数字区域，缩小到 48–64 px 仍有清晰轮廓；不含文字、数字、场景或品牌标识。

## 4. 验证结果

- Unity MCP：CoplayDev MCP for Unity `v10.0.0` 链路可用。
- 编译：源码刷新完成，Console 无编译错误。
- EditMode 测试：完整测试 `13/13` 通过。
- Sprite：八张运行图片均由 Unity 导入；卡框和两个徽章的 Prefab Sprite 引用已由 Unity 组件读取确认。
- Alpha：三个新增 UI PNG 均为 `Format32bppArgb`，四角 Alpha 采样均为 `0`；卡框中心 Alpha 采样为 `0`。
- Prefab：`BattleCardItem.prefab` 静态层级、视窗裁切、Sprite 路径、`72 × 72` 属性徽章和 View 引用检查通过。
- 场景与 Console：活动场景为 `Main`、未脏；最终错误 Console 为 `0`。
- Play Mode：按用户明确要求未在本轮执行，运行时视觉构图与战斗对象回收验收标记为不适用。

## 5. 框架边界审计

- 配置继续通过 `DataApi`，图片继续通过 `ResourceApi`，重复卡牌继续由 `UiList` 和现有 Controller 生命周期管理。
- 静态卡面结构全部保存在 View Prefab，没有在 Controller 中运行时创建卡框或徽章对象。
- 本次未改变 UiGroup、DefaultShow、页面整体 Position/Scale/Pivot 或导出路径，因此无需重导 `Battle.asset`。
- Unity 资产通过 Unity MCP 编辑并保存，未手写 Scene、Prefab、`.asset` 或 `.meta`。
- 工作目录不包含 `.git`，无法使用 Git diff；已按实际操作的脚本、配置、资源、Prefab 与文档路径逐项复核。

## 6. 文档处理

- 玩家视角：更新 `AutoDoc/Design/Specific/combat-system/combat-system.md`。
- 美术：更新 `AutoDoc/Art/Style/art-style-overview.md`、`AutoDoc/Art/UI/ui-art-overview.md`、`AutoDoc/Art/Modules/battle-card/battle-card.md`。
- 程序：更新 `AutoDoc/Program/Specific/combat-system/combat-system.md`、`AutoDoc/Program/UI/battle/battle.md`。
- 第二版战斗场景与怪物卡面概念图继续作为当前主参考；运行资源与概念参考的状态已经分开记录。

## 7. 偏差与未解决风险

- 按用户最新指示未进入游戏测试。Prefab 与资源静态状态已经验证，但未采集本轮运行画面。
- 攻击徽章与生命徽章采用高细节手绘 Sprite；最终在目标分辨率下的主观细节密度仍应由后续人工视觉评审决定，但不影响当前资源引用和布局完整性。

## 8. 清理结果

已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 `0`；随后创建本报告。
