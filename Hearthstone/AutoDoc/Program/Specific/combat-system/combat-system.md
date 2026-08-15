# 战斗系统程序文档

## 1. 核心数据来源

### 1.1 Component

| Component | 职责 |
| --- | --- |
| `BattleCardRawComponent` | 保存单张卡牌的编号、种类 ID、阵营、槽位、攻击力与最大生命、当前生命和存活状态；玩家攻血来自 Run state 永久实例，敌方攻血随机生成 |
| `BattleSessionSingletonRawComponent` | 保存双方卡牌 Entity、行动游标、结果、随机数和行动状态，以及本场已分配的备战奖励批次与防重复切换标记 |
| `RunStateSingletonRawComponent` | 跨 Battle/Preparation Group 保存玩家持有卡永久实例和三个战斗槽 |

### 1.2 Csv和ScriptableObject配置项

| 配置 | 路径 | 数据组 | 用途 |
| --- | --- | --- | --- |
| `BattleCardTypeCsvData` | `Assets/Resources/Config/BattleCardTypeCsvData.csv` | `GameEngineDefault` | 按种类 ID 提供五种怪物的显示名称、生命整数闭区间和攻击整数闭区间 |
| `BattleCardCsvData` | `Assets/Resources/Config/BattleCardCsvData.csv` | `GameEngineDefault` | 按卡牌编号提供种类 ID 与原画资源键；当前连续包含编号 `1~98`，五种怪物各占 `19~20` 个编号 |

当前战斗系统未读取业务 ScriptableObject 配置。项目同时包含框架所需的空注册资产 `Assets/Resources/BbxCommon/ScriptableObjectAssets.asset`；`GameStage` 以该资产作为 Stage 数据加载入口，资产缺失时不会继续加载同组 CSV。

## 2. 战斗流程驱动

### 2.1 System

`BattleSystem` 是核心更新 System。它按 `ActionInterval` 递减行动倒计时，倒计时归零时执行一次自动行动；战斗结束后停止继续结算。

#### 2.1.1 重要的System顺序依赖

`HearthstoneGameEngine` 将执行顺序登记为 `InputSystem → BattleSystem → TaskSystem`。当前 `BattleSystem` 不依赖输入或 Task 的战斗数据，但通过统一登记保证其在主更新组中的稳定位置。

### 2.2 StageListener

`BattleResultPreparationStageListener` 监听会话结果。结果首次终结时，它把本场启动数据中的奖励批次交给游戏引擎，并自动切换到 `RunStateStage + PreparationStage`。

### 2.3 关联Task启动入口

当前无。

### 2.4 调用链路梳理

1. `HearthstoneGameEngine.OnAwake()` 创建持久 `RunStateStage`，并用明确的 `BattleStageStartupData` 进入 `RunStateStage + BattleStage`。
2. `BattleStage.InitializeBattleRuntime.Load()` 创建战斗会话。首次运行将旧玩家阵容 `{4,1,40}` 的随机攻血写入 Run state 一次；玩家 Entity 从 Run state 三槽的永久实例创建，敌方仍按配置随机生成。
3. `BattleUiScene` 通过 `Ui/Battle` 的 `UiSceneAsset` 创建 `BattleView`；`BattleController` 通过两个 `UiList` 创建 `BattleCardItemController` 并绑定卡牌 Entity。
4. `BattleSystem.OnSystemUpdate()` 按固定间隔选择攻击者与随机存活目标，执行同时伤害并更新会话状态。
5. `BattleController` 和卡牌条目 Controller 通过 ModelWrapper 监听会话与卡牌 Component，刷新行动方、结果、生命、存活状态和高亮。
6. 战斗终结后 StageListener 自动进入备战；离开 BattleStage 时关闭 UI、销毁双方卡牌 Entity并移除战斗会话单例，Run state 保留。

## 3. 战斗结算链路

### 3.1 单场战斗开始与结束

`InitializeBattleRuntime` 先创建 `BattleSessionSingletonRawComponent`，使用基于当前 UTC ticks 的非零随机种子初始化。首次我方编号顺序为 `[4, 1, 40]`，其攻血只随机一次并持久保存；后续我方编号与攻血来自 Run state 三槽。敌方编号保持 `[5, 2, 9]` 并在每场战斗随机攻血。任一编号或种类配置缺失、类型关联失配或卡牌创建失败时，已创建的双方卡牌和会话单例会被清理后重新抛出异常。

每次行动前后都根据存活掩码调用 `BattleRules.EvaluateResult()`。敌方无存活卡牌时为玩家胜利；玩家无存活卡牌时为敌方胜利；结果确定后 `BattleSystem` 不再执行行动。

### 3.2 回合或阶段推进

行动方从玩家开始。每方通过独立游标从左到右寻找下一张存活卡牌，行动后游标移到下一槽位。会话内的 `Unity.Mathematics.Random` 先在卡牌创建时生成攻血，再在行动时从对方存活卡牌中选择目标。行动完成且战斗未结束时切换阵营。

### 3.3 伤害与治疗结算

`BattleRules.ResolveSimultaneousDamage()` 同时计算攻击者与目标的新生命：双方当前生命分别减去对方攻击力并限制到不低于零。`BattleCardRawComponent.SetCurrentHealth()` 再限制到最大生命范围并同步 `IsAlive`。

当前无治疗、护盾、减伤、暴击或其他伤害修正。

### 3.4 Buff与状态结算

当前无。

### 3.5 单位创建与销毁

卡牌通过 `EcsApi.CreateEntity("BattleCard")` 创建并挂载池化 `BattleCardRawComponent`。玩家初始化复制 Run state 实例的永久攻击与最大生命，敌方从种类表整数闭区间生成；双方当前生命都设为最大生命，存活状态设为 true。

Stage 卸载时逐一调用 `EcsApi.DestroyEntity()`。Component 回收时先 Invalid 当前生命和存活监听，再重置卡牌编号、种类 ID、阵营、槽位与所有数值。

## 4. 所属GameStage

| GameStage | 内容 |
| --- | --- |
| `RunStateStage` | 整局玩家卡牌实例和阵容；与 BattleStage 同组且在进入 PreparationStage 时继续存活 |
| `BattleStage` | `InitializeBattleRuntime` LoadItem、`BattleSystem` Update System、结果 StageListener、`BattleUiScene` 与 `Ui/Battle` 导出资产 |

两张卡牌 CSV 均属于全局 `GameEngineDefault` 数据组。引擎先初始化资源索引并通过 `ScriptableObjectAssets` 注册入口加载该组数据，再进入 `BattleStage`。
