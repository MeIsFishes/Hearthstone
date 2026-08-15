# 备战卡池编成程序文档

## 1. 运行状态与输入

`RunStateStage` 是一次游戏过程中的持久 Stage。战斗组与备战组分别为 `RunStateStage + BattleStage`、`RunStateStage + PreparationStage`，因此两组切换不会回收整局状态。

`RunStateSingletonRawComponent` 是唯一权威状态，使用编号索引数组保存 `1~98` 的持有卡实例，以长度为 3 的数组保存战斗阵容，并记录已经应用的奖励 BatchId。卡实例保存永久攻击力和永久最大生命；`Revision` 在一次有效整局写入后递增，供界面监听。

正式战斗启动数据为 `BattleStageStartupData`，其中携带一个 `PreparationRewardBatchStartupData`。奖励批次包含非空 BatchId 和恰好 5 个编号互异的 `RewardCardGrantStartupData`；每个 grant 包含卡牌编号、永久攻击力和永久最大生命。构造与跨 Stage 传递均使用防御性深拷贝。

## 2. 奖励应用

`RunCardRules.ApplyRewardBatch()` 实现批次原子性与幂等性：

- 相同 BatchId 且持久实例与 grant 一致时返回 `AlreadyApplied`，不增加持有数且不递增 Revision。
- 新 BatchId 的 5 张卡必须全部未持有；任一编号已持有时，在任何写入发生前抛出异常。
- 全部校验通过后一次写入 5 个实例、记录 BatchId，并仅递增一次 Revision。

`InitializePreparationRuntime` 在应用前还通过两张现有卡牌 CSV 验证编号、种类引用和攻血范围。`PreparationSessionSingletonRawComponent` 只保存当前页面需要的 5 张奖励快照和是否首次应用；离开 PreparationStage 时回收该单例，不影响 Run state。

## 3. 编成规则

不可变玩法契约集中在 `RunCardRules`：3 个战斗槽、卡牌编号 `1~98`、每行 7 张、14 行、卡面 `2:3`、每批 5 张。

`TryPlaceCard()` 仅接受已持有卡和有效槽索引。卡牌从池中放入槽位时会替换目标卡；已上阵卡移至另一槽时先清空原槽再落入目标槽，因此同一编号不会占据两个槽。无效目标不改变状态。

## 4. Stage 链路

首次进入战斗时，旧默认我方编号 `{4,1,40}` 仅初始化一次并持久化到 Run state。之后玩家战斗 Entity 始终从 Run state 的三个槽位实例创建，当前生命以永久最大生命初始化；敌方继续按既有编号配置随机生成攻血。

`BattleResultPreparationStageListener` 监听战斗结果。结果第一次离开 `InProgress` 时，监听器先设置本场切换标记，再把 Battle session 中的奖励批次交给正式 `EnterPreparationStageGroup()`，在同一次 Play 会话内自动进入备战。当前备战页没有返回战斗的按钮。

`HearthstoneStageGroupTransitionCoordinator` 明确记录 requested、loading、active 三种状态。相同 Group/批次内容在加载期间会被合并；冲突请求只保留最新请求，并在框架 `OnStageLoadingCompleted()` 通知当前批次落定后再提交下一完整 StageGroup，避免向正在消费的操作队列追加重复 Load/Unload。

## 5. 主要文件

- 规则与状态：`Assets/Scripts/Hearthstone/Ecs/System/RunCardRules.cs`、`Assets/Scripts/Hearthstone/Ecs/RawComponent/Singleton/RunStateSingletonRawComponent.cs`
- 启动数据：`Assets/Scripts/Hearthstone/GameStage/BattleStageStartupData.cs`
- Stage：`Assets/Scripts/Hearthstone/GameStage/RunStateStages.cs`、`PreparationStages.cs`
- 自动切换：`Assets/Scripts/Hearthstone/GameStage/BattleResultPreparationStageListener.cs`
- 隔离入口：`Assets/Resources/Editor/PreparationStageEntry.asset`
