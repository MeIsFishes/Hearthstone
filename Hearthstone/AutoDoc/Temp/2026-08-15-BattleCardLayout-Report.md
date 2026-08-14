# 战斗场景竖向卡牌改造方案报告

## 1. 任务结果

- 结果：通过。
- 任务类型：要求出方案。
- 最终方案：`AutoDoc/Temp/Plan/2026-08-15-BattleCardLayout-Plan.md`。
- 本轮未修改游戏代码、CSV、Prefab、Scene、UiSceneAsset 或其他 Unity 资产。

## 2. 需求确认结果

- 玩家卡牌为 `220 × 320` 竖向卡面；敌方整张卡牌旋转 `180°`，文字、数值和状态层同样倒置。
- 上半部分为无 Sprite 的原画预留区，下半部分为技能说明区。
- 本地左下角显示当前血量，本地右下角显示攻击力。
- `SkillDescription` 为空时不显示文字，但通过 `BattleCardCsvData.csv` 预留字段。
- 卡面表现由 `BattleCardItemController` 管理并由现有 `UiList` 创建/回收；Entity 只保留玩法权威数据，不承担 UI 生命周期。

## 3. 检查项结果与证据

- 需求覆盖：通过。证据见 Plan §1.1。
- 数据边界：通过。CSV/DataApi 保存静态配置，`BattleCardRawComponent` 保存运行时状态，Controller 只查询与刷新；证据见 Plan §2。
- UI 框架：通过。静态结构落在 `BattleCardItem.prefab` / `BattleView.prefab`，动态条目沿用 `UiList.AddItem<BattleCardItemController>()`；证据见 Plan §4。
- 美术完整性：通过。项目无可复用业务图片，本次使用基础 Image 完成占位并保留正式素材替换入口；证据见 Plan §5。
- GameStage：通过。只修改已有 `InitializeBattleRuntime` 的配置读取，不新增 Stage、System 或 UiScene；证据见 Plan §6。
- 实现顺序与 Todo：通过。Plan §7 的八个步骤与八项 Todo 名称、顺序一致。
- 框架边界：通过。未设计运行时拼静态卡面、手写 Prefab/Scene/UiSceneAsset、直接访问底层 Manager 或由 UiController 保存战斗权威状态。

## 4. 验证结果

- 已确认现有代码入口：`BattleCardRawComponent`、`BattleSessionSingletonRawComponent`、`BattleCardItemView/Controller`、`BattleView/Controller`、`BattleUiScene`、`BattleStages`、`HearthstoneGameEngine`。
- 已确认现有资产路径：`Assets/Scenes/Ui/Battle.unity`、`Assets/Resources/Ui/BattleView.prefab`、`BattleCardItem.prefab`、`Battle.asset`。
- 已确认 CSV 框架加载顺序：`GameEngineDefault` 数据在派生引擎 `OnAwake()` 之前加载，适合供 `BattleStage.InitializeBattleRuntime` 查询；未把配置放入会晚于 LoadItem 执行的 BattleStage DataGroup。
- 已检查 Plan Markdown 标题：大章为 1–7 连续编号，无空章节。
- 本轮为方案任务，未执行编译、EditMode、Unity Editor 结构检查或 Play Mode 验证。

## 5. 执行偏差

- 无需求偏差。
- 根据用户补充，将“卡牌使用 UiController”明确解释为卡面表现和生命周期使用 `BattleCardItemController`；为遵守项目 ECS 边界，攻击、血量和阵营等玩法权威数据仍保留在 Entity 的 `BattleCardRawComponent` 中。
- 因 View Prefab 路径、UiGroup、DefaultShow、场景级 Position/Scale/Pivot 均不计划变化，方案不修改 `Battle.unity`、不重新导出 `Battle.asset`；实施期只编辑两个现有 View Prefab。

## 6. 未解决风险

- `220 × 320` 卡面、两排列表间距和中央信息区需要实施期在 Unity Editor 的 `1920 × 1080` 参考分辨率下核验；当前方案未读取 Editor 实时布局。
- 当前会话未进行 `unityMCP` 连接验收。后续实现 Prefab 时必须先按项目规则确认 MCP 链路，再操作和验收 Unity 资产。
- 默认不进入 Play Mode，因此敌方整卡倒置后的最终观感与长技能文本效果需要用户另行授权游戏内验证，或由 Editor 预览先行验收。

## 7. 文档处理与清理结果

- 未修改 `AutoDoc/Program/`、`AutoDoc/Art/`、`AutoDoc/Design/`；这些目录当前无现状文档。
- 未搜索或读取 `AutoDoc/DesignPlan/`。
- `AutoDoc/CleanupTempDocs.bat` 仅执行一次，退出码为 `0`。
- 清理后 `AutoDoc/Temp/` 顶层 Markdown 数量为 `38`，低于清理阈值，未删除文件。
