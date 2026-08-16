# 备战卡池编成程序文档

## 1. 运行状态与输入

`RunStateStage` 是一次游戏过程中的持久 Stage。战斗组与备战组分别为 `RunStateStage + BattleStage`、`RunStateStage + PreparationStage`，因此两组切换不会回收整局状态。

`RunStateSingletonRawComponent` 是唯一权威状态，使用编号索引数组保存 `1~213` 的首张持有卡实例，并以按编号分组的附加实例列表保存同编号第二张及后续副本；长度为 6 的数组按卡号保存最大阵容，`UnlockedBattleSlotCount` 保存当前轮累计解锁的 `2~6` 个槽位，BatchId 到 canonical payload 指纹的账本记录已经应用的摸牌批次。`99` 是永不写入持有状态的封印分隔位，`100~213` 是融合结果实例。每张副本分别保存结果卡号、表现来源卡号、永久攻击力、永久最大生命、词条集合和运行时等阶；普通卡及双卡、三卡结果的表现来源默认等于自身卡号，四卡结果在融合提交时记录动态选出的三卡版本。`GetCardCopyCount()` 与 `GetCardInstance(cardNumber, copyIndex)` 提供副本读取，`Revision` 在一次有效整局写入后递增，供界面监听。

备战启动数据为 `PreparationRoundStartupData`，携带本轮编号、累计已解锁槽位数、动态长度的 `PreparationRewardBatchStartupData` 和 `EnemyBattlePreviewStartupData`；正式战斗启动数据为 `BattleStageStartupData`，携带同一轮编号，并可携带显式验收 Scenario，或 Continue 点击瞬间的 `2~6` 槽玩家阵容快照与进入备战前已经生成的敌方快照。敌方快照保存关卡号、生成种子、敌方目标选择随机状态和每个槽位的完整 `RunCardInstanceData`。正常 Continue 保持 `Scenario=null`，BattleStage 的初始化 LoadItem 从两侧快照创建 Entity，不重读可变的 Run battle slots，也不重新抽取敌方配置或属性。奖励批次包含非空 BatchId 和任意非负数量的 `RewardCardGrantStartupData`；每个 grant 包含卡牌编号、永久攻击力和永久最大生命。构造与跨 Stage 传递均使用防御性深拷贝。

`PreparationRewardBatchFactory.CreateRandom()` 以可注入的 `Unity.Mathematics.Random` 状态和本轮 `DrawCardCount` 生成批次。它完整检查 `BattleCardCsvData` 的 `1~98` 普通卡及其 `BattleCardTypeCsvData` 引用，从当前已持有编号之外执行无放回随机抽样，并按抽中卡牌的类型范围分别生成攻击力和最大生命值，因此同批卡号互异。可用编号不足本轮配置的摸牌数时，随机工厂不会构造不完整批次。

## 2. 奖励应用

`RunCardRules.ApplyRewardBatch()` 实现批次原子性与幂等性：

- 相同 BatchId 且不可变 payload 指纹一致时返回 `AlreadyApplied`，即使对应卡牌已经被融合消耗也不会重发；不增加持有数且不递增 Revision。
- 相同 BatchId 但 payload 不同会在写入前拒绝。
- 新 BatchId 的全部 grant 会作为独立实例写入；同批重复编号或 Run state 已持有编号都追加为该编号的后续副本，不覆盖首张实例。
- 全部校验通过后一次写入本轮完整批次、记录 BatchId，并仅递增一次 Revision；`GetOwnedCardCount()` 按副本总数计数。

`InitializePreparationRuntime` 先把本轮累计槽位数单调写入 Run state，再通过两张现有卡牌 CSV 验证并应用本轮动态摸牌批次。`PreparationSessionSingletonRawComponent` 保存当前 `BattleNumber`、奖励快照、下一场敌方阵容快照、是否首次应用及 4 个融合素材槽；切换页签保留，离开 PreparationStage 时回收，不影响 Run state。备战界面的敌方预览列表只读取该快照，不持有第二份随机生成逻辑。

## 3. 编成规则

不可变玩法契约集中在 `RunCardRules`：初始 2 个、最多 6 个战斗槽，普通卡编号 `1~98`、封印位 `99`、融合目标编号和 `99`、融合卡编号 `100~213`、四卡传奇起始内部编号 `149`、每行 7 张、无额外副本时基础 31 行、卡面 `25:36`。每轮摸牌数由 `BattleProgressionCsvData` 配置；当前第 1 轮摸 3 张，第 2～5 轮每轮摸 5 张，累计槽位依次为 `2、3、4、5、6`。牌库按副本展开后行数由实际条目数派生。基础卡牌种类固定为 `1~5`，类型 5 的巨魔在融合公式中最多出现两次。

`TryPlaceCard()` 仅接受已持有卡和有效槽索引。卡牌从池中放入槽位时会替换目标卡；已上阵卡移至另一槽时先清空原槽再落入目标槽，因此同一编号不会占据两个槽。`TryRemoveCardFromBattleSlot()` 只在来源槽仍保存预期卡号时清空该槽并递增 `Revision`，用于拖离原出战槽后退回牌库；若卡已成功移到其他槽，预期卡号校验会阻止回调再次移除。无效目标不改变状态。

`BattleCardCsvData` 的 `FusionRecipeTypeIds` 以 `List<int>` 保存排序后的基础类型 ID。CSV 读取每一条 `100~213` 融合卡时，会把 2～4 项公式编码成与正卡号不冲突的负整数键并登记进 `DataApi`，运行时以 O(1) 查询结果；交换素材顺序不会产生不同键。表中共有 15 条双卡、34 条三卡和 65 条四卡公式；仍不登记包含三张及以上巨魔的组合。每个融合结果使用与卡号相同的独立类型 ID；类型表的攻血范围和初始词条留空，并按配方长度配置银、金、传奇默认等阶。编号 `100~148` 的 `ArtworkKey` 使用 `FusionCard_100`～`FusionCard_148`，与同名 Resources 图片一一对应；资源字典沿用既有自动索引，Controller 仍只通过 `ResourceApi.LoadSprite()` 读取原画。四卡结果不把自身配置行当作权威表现：规则层根据实际四张素材计算一个 `100~148` 三卡表现来源，卡面与战斗从该来源读取原画、名称和攻击表现。

融合页通过 4 个 Preparation session 槽保存 2～4 个互异卡号且已持有的普通卡；同编号存在多个副本时仍只能同时选择该编号一次。`EvaluateFusion()` 统一派生素材数、编号和、公式采用数量、结果卡号、表现来源卡号与阻断原因；素材数有效后必须先满足 `CardNumberSum == FusionTargetCardNumberSum`，即编号和严格等于 `99`，低于或高于目标都返回 `CardNumberSumNotExact`，不会进入配方提交。精确命中后才对全部素材类型做无序规范化，并以完整组合查询对应公式。四卡评估保留卡号与类型的配对关系，按卡号升序排序后丢弃最低点数的一张，再用其余三张的基础类型查询对应三卡融合卡号作为 `PresentationCardNumber`；完整四类型仍用于查询四卡结果。若任一结果不存在、四卡表现所需三卡版本不存在、结果已经持有，或材料包含 99/融合卡，则拒绝提交。`TryFuse()` 复用同一评估结果完成全部校验和攻血溢出检查，再一次性消耗每个素材卡号的首张副本、清除其占用的出战槽、生成对应的 `100~213` 结果卡并清空融合槽；若被消耗卡号仍有副本，下一张副本会提升为该编号首张实例并继续保留在牌库。结果的永久攻击与最大生命为全部素材永久值之和，词条为全部素材词条的规范化并集；运行时等阶由实际材料数直接写为银、金或传奇，不由 UI 或敌我关系推导。四卡结果只把表现来源切换到最高点数三张对应的三卡版本，结果卡号、四张素材属性、词条和传奇等阶都不降为三卡。已应用奖励账本不会被融合改写。

`FindFusionRecommendations()` 是融合规则的只读查询入口。调用方传入当前 Run state、Preparation session 与可复用结果列表；入口先清空结果。融合槽为空时，候选覆盖全部已持有普通卡，并按目标材料数 2、3、4 依次枚举互异组合；融合槽非空时，当前槽内卡号组成固定集合，候选只遍历其余已持有普通卡，并枚举补齐后的组合。只有编号和正好为 `99` 且复用 `EvaluateFusion()` 得到 `CanFuse` 的组合才写入 `FusionRecommendationData`。因此推荐会同时排除不存在的配方与已经持有的结果卡；非空查询还保证每条结果包含全部已选素材。数据内卡号升序保存，材料数和结果卡号一并保留给选择时的权威复核；推荐窗口只渲染素材卡，不把结果卡号输出成额外卡面。查询本身不修改 Run state、融合槽或 Revision。

`TryApplyFusionRecommendation()` 是推荐选择的权威写入口。它把推荐中的 2～4 个卡号复制到临时四槽数组，重新通过 `EvaluateFusion()` 校验当前持有状态、精确 `99`、配方与目标结果，再一次性替换 `FusionSlotCardNumbers` 并只递增一次 `FusionRevision`；重复选择已经完全一致的组合返回 `NoChange`。界面不逐槽写入素材，因此不会暴露中间半成品组合或产生多次状态刷新。

## 4. Stage 链路

引擎 `OnAwake()` 不创建本局持久状态，也不在此时读取卡牌配置。Game Engine Stage 完成 `GameEngineDefault` 数据组加载后，首次 `OnStageLoadingCompleted()` 只请求单独的 `MainMenuStage`。玩家点击“开始游戏”时，`StartNewRun()` 才创建 `RunStateStage` 并调用 `BeginPreparationForBattle(1)`，读取 `BattleProgressionCsvData` 第 1 行，随机摸 3 张卡、累计解锁 2 槽并请求 `RunStateStage + PreparationStage`。MainMenu 与 Preparation Group 确认加载完成后都调用 `AudioApi.SetBgm("Lobby")`；同曲请求复用正在播放的 BGM，因此首轮备战不会重启大厅曲，战斗结算进入后续备战时则从结算曲切回大厅曲。

玩家战斗 Entity 始终从 Continue 捕获的当前 `2~6` 个已解锁槽位实例创建，当前生命以永久最大生命初始化；因此 `100~213` 的融合卡使用融合得到的永久攻血、词条和运行时等阶，不读取其空白类型数值。玩家 Entity 同时保留实际结果编号/类型与表现编号/类型，四卡结果的卡面和攻击表现读取三卡表现类型，战斗效果仍读取融合实例词条和属性。随机摸牌仍只从 `1~98` 的既有编号与类型 `1~5` 配置生成。进入每轮备战前，引擎以独立种子从 `EnemyLineupCsvData` 的同关多行中抽取一整行：基础卡直接按对应类型范围生成攻血，融合卡先按配方生成基础素材再复用共享融合合成函数，生成完成后的完整敌方实例与随机状态共同写入 `EnemyBattlePreviewStartupData`。

备战页的 Continue Button 调用 `TryEnterNextBattleStageGroup()`：它捕获本轮全部已解锁槽位与永久卡牌值，把 Preparation session 中用于预览的同一敌方实例快照传入正式战斗，并只发起一次同轮编号的 Battle StageGroup 切换，不在此时摸下一轮卡。BattleStage 优先使用该快照的敌方槽位与攻血、词条、等阶，并从快照保存的后继随机状态继续敌方目标选择，因此预览和实战不会因二次随机而分叉。非最终轮玩家胜利的横幅演出完成后，`BattleResultPreparationStageListener` 调用 `BeginPreparationForBattle(BattleNumber + 1)`；引擎读取下一行轮次配置、从 `1~98` 中排除 Run state 当前持有编号后无放回生成该轮摸牌批次和下一场敌方快照，再进入下一轮备战。失败与最终轮胜利保留在 BattleStage 的结算弹窗；玩家点击“返回主菜单”后请求单独的 `MainMenuStage`，不在结算弹窗内直接重建本局。

`HearthstoneStageGroupTransitionCoordinator` 明确记录 requested、loading、active 三种状态。相同 Group/完整 canonical key 在加载期间会被合并；Preparation 与 Battle key 都纳入敌方快照种子、随机状态和全部卡牌实例，Battle key 另外包含奖励 payload、Scenario 或 Continue 的动态玩家槽位快照。冲突请求只保留最新请求，并在当前 StageGroup 加载完成后再提交下一完整 StageGroup。底层沿用 `GameEngineBase` 的普通切换顺序：反向卸载不再需要的业务 Stage，再正向加载目标 Stage。

## 5. 主要文件

- 规则与状态：`Assets/Scripts/Hearthstone/Ecs/System/RunCardRules.cs`、`Assets/Scripts/Hearthstone/Ecs/RawComponent/Singleton/RunStateSingletonRawComponent.cs`
- 卡牌、公式、轮次与敌方阵容配置：`Assets/Scripts/Hearthstone/Config/Csv/BattleCardCsvData.cs`、`BattleCardTypeCsvData.cs`、`BattleProgressionCsvData.cs`、`Assets/Resources/Config/BattleCardCsvData.csv`、`BattleCardTypeCsvData.csv`、`BattleProgressionCsvData.csv`、`EnemyLineupCsvData.csv`
- 启动数据：`Assets/Scripts/Hearthstone/GameStage/BattleStageStartupData.cs`
- 运行入口：`Assets/Scripts/Hearthstone/Bootstrap/HearthstoneGameEngine.cs`
- Stage：`Assets/Scripts/Hearthstone/GameStage/RunStateStages.cs`、`PreparationStages.cs`、`BattleStages.cs`
- 敌方预览 UI：`Assets/Scripts/Hearthstone/Ui/View/PreparationView.cs`、`Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs`、`Assets/Scripts/Hearthstone/Ui/Editor/PreparationViewUiBuilder.cs`
- 结果推进与返回主菜单：`Assets/Scripts/Hearthstone/GameStage/BattleResultPreparationStageListener.cs`、`Assets/Scripts/Hearthstone/Bootstrap/HearthstoneGameEngine.cs`
- 隔离入口：`Assets/Resources/Editor/PreparationStageEntry.asset`
