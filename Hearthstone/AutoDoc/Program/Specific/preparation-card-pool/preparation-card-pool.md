# 备战卡池编成程序文档

## 1. 运行状态与输入

`RunStateStage` 是一次游戏过程中的持久 Stage。战斗组与备战组分别为 `RunStateStage + BattleStage`、`RunStateStage + PreparationStage`，因此两组切换不会回收整局状态。

`RunStateSingletonRawComponent` 是唯一权威状态，使用编号索引数组保存 `1~213` 的持有卡实例，以长度为 3 的数组保存战斗阵容，并用 BatchId 到 canonical payload 指纹的账本记录已经应用的奖励。`99` 是永不写入持有状态的封印分隔位，`100~213` 是融合结果实例。卡实例保存永久攻击力、永久最大生命、词条集合和运行时等阶；`Revision` 在一次有效整局写入后递增，供界面监听。

正式战斗启动数据为 `BattleStageStartupData`，其中携带关卡号、一个 `PreparationRewardBatchStartupData`，并可携带显式验收 Scenario 或 Continue 点击瞬间的三槽防御性阵容快照。正常 Continue 保持 `Scenario=null`，BattleStage 的初始化 LoadItem 从快照创建 0～3 张玩家 Entity，不重读可变的 Run battle slots。奖励批次包含非空 BatchId 和恰好 5 个编号互异的 `RewardCardGrantStartupData`；每个 grant 包含卡牌编号、永久攻击力和永久最大生命。构造与跨 Stage 传递均使用防御性深拷贝。

`PreparationRewardBatchFactory.CreateRandom()` 以可注入的 `Unity.Mathematics.Random` 状态生成批次。它完整检查 `BattleCardCsvData` 的 `1~98` 普通卡及其 `BattleCardTypeCsvData` 引用，从调用方标记为不可用的编号之外执行无放回随机抽样，并按抽中卡牌的类型范围分别生成攻击力和最大生命值。可用编号不足 5 个时不会构造不完整批次。

## 2. 奖励应用

`RunCardRules.ApplyRewardBatch()` 实现批次原子性与幂等性：

- 相同 BatchId 且不可变 payload 指纹一致时返回 `AlreadyApplied`，即使对应卡牌已经被融合消耗也不会重发；不增加持有数且不递增 Revision。
- 相同 BatchId 但 payload 不同会在写入前拒绝。
- 新 BatchId 的 5 张卡必须全部未持有；任一编号已持有时，在任何写入发生前抛出异常。
- 全部校验通过后一次写入 5 个实例、记录 BatchId，并仅递增一次 Revision。

`InitializePreparationRuntime` 在应用前通过两张现有卡牌 CSV 验证编号和种类引用。`PreparationSessionSingletonRawComponent` 保存当前页面需要的 5 张奖励快照、是否首次应用及 4 个融合素材槽；切换页签保留，离开 PreparationStage 时回收，不影响 Run state。

## 3. 编成规则

不可变玩法契约集中在 `RunCardRules`：3 个战斗槽、普通卡编号 `1~98`、封印位 `99`、融合卡编号 `100~213`、四卡传奇起始内部编号 `149`、每行 7 张、31 行、卡面 `25:36`、每批 5 张。基础卡牌种类固定为 `1~5`，类型 5 的巨魔在融合公式中最多出现两次。

`TryPlaceCard()` 仅接受已持有卡和有效槽索引。卡牌从池中放入槽位时会替换目标卡；已上阵卡移至另一槽时先清空原槽再落入目标槽，因此同一编号不会占据两个槽。无效目标不改变状态。

`BattleCardCsvData` 的 `FusionRecipeTypeIds` 以 `List<int>` 保存排序后的基础类型 ID。CSV 读取每一条 `100~213` 融合卡时，会把 2～4 项公式编码成与正卡号不冲突的负整数键并登记进 `DataApi`，运行时以 O(1) 查询结果；交换素材顺序不会产生不同键。表中共有 15 条双卡、34 条三卡和 65 条四卡公式；仍不登记包含三张及以上巨魔的组合。每个融合结果使用与卡号相同的独立类型 ID；类型表的攻血范围和初始词条留空，并按配方长度配置银、金、传奇默认等阶，攻击表现继续复用基础种类配置。编号 `100~148` 的 `ArtworkKey` 使用 `FusionCard_100`～`FusionCard_148`，与同名 Resources 图片一一对应；资源字典沿用既有自动索引，Controller 仍只通过 `ResourceApi.LoadSprite()` 读取原画。

融合页通过 4 个 Preparation session 槽保存 2～4 张互异且已持有的普通卡。`EvaluateFusion()` 统一派生素材数、编号和、公式采用数量、结果卡号与阻断原因；选择 2～4 张时都对全部素材类型做无序规范化，并以完整组合查询对应公式。若结果不存在、已经持有，或材料包含 99/融合卡，则拒绝提交。`TryFuse()` 先完成全部校验和攻血溢出检查，再一次性消耗全部 2～4 张素材、清除其占用的出战槽、生成对应的 `100~213` 结果卡并清空融合槽。结果的永久攻击与最大生命为全部素材永久值之和，词条为全部素材词条的规范化并集；运行时等阶由实际材料数直接写为银、金或传奇，不由 UI 或敌我关系推导。已应用奖励账本不会被融合改写。

## 4. Stage 链路

引擎 `OnAwake()` 只创建持久 RunStateStage，不在此时读取卡牌配置。Game Engine Stage 完成 `GameEngineDefault` 数据组加载后，首次 `OnStageLoadingCompleted()` 才创建随机首轮奖励并请求 `RunStateStage + BattleStage`，保证奖励工厂读取 `BattleCardCsvData` 与 `BattleCardTypeCsvData` 时配置已经进入 `DataApi`。

首次进入战斗时，旧默认我方编号 `{4,1,40}` 仅初始化一次并持久化到 Run state；首轮随机奖励在生成候选池时排除这三个编号。之后玩家战斗 Entity 始终从 Run state 的三个槽位实例创建，当前生命以永久最大生命初始化；因此 `100~213` 的融合卡使用融合得到的永久攻血、词条和运行时等阶，不读取其空白类型数值。敌方与随机奖励继续只从 `1~98` 的既有编号与类型 `1~5` 配置随机生成攻血。

`BattleResultPreparationStageListener` 监听战斗结果。结果第一次离开 `InProgress` 时，监听器先设置本场切换标记，再把 Battle session 中的随机奖励批次交给正式 `EnterPreparationStageGroup()`，在同一次 Play 会话内自动进入备战。备战页的 Continue Button 调用 `TryEnterNextBattleStageGroup()`：它从 `1~98` 中排除 Run state 当前持有的编号，无放回生成下一轮 5 张随机结算奖励，捕获当前三槽与永久卡牌值，并只发起一次正式 Battle StageGroup 切换。随机批次按目标战斗序号生成稳定 BatchId，具体 payload 随创建时的随机状态确定。

`HearthstoneStageGroupTransitionCoordinator` 明确记录 requested、loading、active 三种状态。相同 Group/完整 canonical key 在加载期间会被合并；Battle key 包含关卡号、奖励 payload、Scenario 或 Continue 三槽快照。冲突请求只保留最新请求，并在当前 StageGroup 加载完成后再提交下一完整 StageGroup。底层沿用 `GameEngineBase` 的普通切换顺序：反向卸载不再需要的业务 Stage，再正向加载目标 Stage。

## 5. 主要文件

- 规则与状态：`Assets/Scripts/Hearthstone/Ecs/System/RunCardRules.cs`、`Assets/Scripts/Hearthstone/Ecs/RawComponent/Singleton/RunStateSingletonRawComponent.cs`
- 卡牌与公式配置：`Assets/Scripts/Hearthstone/Config/Csv/BattleCardCsvData.cs`、`BattleCardTypeCsvData.cs`、`Assets/Resources/Config/BattleCardCsvData.csv`、`BattleCardTypeCsvData.csv`
- 启动数据：`Assets/Scripts/Hearthstone/GameStage/BattleStageStartupData.cs`
- 运行入口：`Assets/Scripts/Hearthstone/Bootstrap/HearthstoneGameEngine.cs`
- Stage：`Assets/Scripts/Hearthstone/GameStage/RunStateStages.cs`、`PreparationStages.cs`
- 自动切换：`Assets/Scripts/Hearthstone/GameStage/BattleResultPreparationStageListener.cs`
- 隔离入口：`Assets/Resources/Editor/PreparationStageEntry.asset`
