# 备战卡池编成程序文档

## 1. 运行状态与输入

`RunStateStage` 是一次游戏过程中的持久 Stage。战斗组与备战组分别为 `RunStateStage + BattleStage`、`RunStateStage + PreparationStage`，因此两组切换不会回收整局状态。

`RunStateSingletonRawComponent` 是唯一权威状态，使用编号索引数组保存 `1~99` 的持有卡实例，以长度为 3 的数组保存战斗阵容，并用 BatchId 到 canonical payload 指纹的账本记录已经应用的奖励。卡实例保存永久攻击力和永久最大生命；`Revision` 在一次有效整局写入后递增，供界面监听。

正式战斗启动数据为 `BattleStageStartupData`，其中携带关卡号、一个 `PreparationRewardBatchStartupData`，并可携带显式验收 Scenario 或 Continue 点击瞬间的三槽防御性阵容快照。正常 Continue 保持 `Scenario=null`，Battle hidden commit 从快照创建 0～3 张玩家 Entity，不重读可变的 Run battle slots。奖励批次包含非空 BatchId 和恰好 5 个编号互异的 `RewardCardGrantStartupData`；每个 grant 包含卡牌编号、永久攻击力和永久最大生命。构造与跨 Stage 传递均使用防御性深拷贝。

## 2. 奖励应用

`RunCardRules.ApplyRewardBatch()` 实现批次原子性与幂等性：

- 相同 BatchId 且不可变 payload 指纹一致时返回 `AlreadyApplied`，即使对应卡牌已经被融合消耗也不会重发；不增加持有数且不递增 Revision。
- 相同 BatchId 但 payload 不同会在写入前拒绝。
- 新 BatchId 的 5 张卡必须全部未持有；任一编号已持有时，在任何写入发生前抛出异常。
- 全部校验通过后一次写入 5 个实例、记录 BatchId，并仅递增一次 Revision。

`InitializePreparationRuntime` 在应用前通过两张现有卡牌 CSV 验证编号和种类引用。`PreparationSessionSingletonRawComponent` 保存当前页面需要的 5 张奖励快照、是否首次应用及 4 个融合素材槽；切换页签保留，离开 PreparationStage 时回收，不影响 Run state。

## 3. 编成规则

不可变玩法契约集中在 `RunCardRules`：3 个战斗槽、卡牌编号 `1~99`、每行 7 张、15 行、卡面 `2:3`、每批 5 张。

`TryPlaceCard()` 仅接受已持有卡和有效槽索引。卡牌从池中放入槽位时会替换目标卡；已上阵卡移至另一槽时先清空原槽再落入目标槽，因此同一编号不会占据两个槽。无效目标不改变状态。

融合页通过 4 个 Preparation session 槽保存 2～4 张互异且已持有的普通卡。`EvaluateFusion()` 统一派生素材数、编号和与阻断原因；只有编号和恰为 `99` 且尚未持有结果卡时可提交。`TryFuse()` 先完成全部校验和攻血溢出检查，再一次性消耗素材、清除素材占用的出战槽、生成 99 号卡并清空融合槽。99 的永久攻击与最大生命分别为素材永久值之和；已应用奖励账本不会被融合改写。

## 4. Stage 链路

首次进入战斗时，旧默认我方编号 `{4,1,40}` 仅初始化一次并持久化到 Run state。之后玩家战斗 Entity 始终从 Run state 的三个槽位实例创建，当前生命以永久最大生命初始化；因此 99 号卡使用融合得到的永久攻血，不读取类型 6 的随机范围。敌方继续只从 `1~98` 的既有编号与类型 `1~5` 配置随机生成攻血。

`BattleResultPreparationStageListener` 监听战斗结果。结果第一次离开 `InProgress` 时，监听器先设置本场切换标记，再把 Battle session 中的奖励批次交给正式 `EnterPreparationStageGroup()`，在同一次 Play 会话内自动进入备战。备战页的 Continue Button 调用 `TryEnterNextBattleStageGroup()`：它从 `BattleProgressionCsvData` 读取下一关固定结算奖励、捕获当前三槽与永久卡牌值，并只提交一次正式 Battle StageGroup 事务。

`HearthstoneStageGroupTransitionCoordinator` 明确记录 requested、loading、active 三种状态。相同 Group/完整 canonical key 在加载期间会被合并；Battle key 包含关卡号、奖励 payload、Scenario 或 Continue 三槽快照。冲突请求只保留最新请求，并在统一事务结果回调后再提交下一完整 StageGroup；失败恢复原 Active Group 与 Idle 按钮状态，允许同一目标重试。

## 5. 主要文件

- 规则与状态：`Assets/Scripts/Hearthstone/Ecs/System/RunCardRules.cs`、`Assets/Scripts/Hearthstone/Ecs/RawComponent/Singleton/RunStateSingletonRawComponent.cs`
- 启动数据：`Assets/Scripts/Hearthstone/GameStage/BattleStageStartupData.cs`
- Stage：`Assets/Scripts/Hearthstone/GameStage/RunStateStages.cs`、`PreparationStages.cs`
- 自动切换：`Assets/Scripts/Hearthstone/GameStage/BattleResultPreparationStageListener.cs`
- 隔离入口：`Assets/Resources/Editor/PreparationStageEntry.asset`
