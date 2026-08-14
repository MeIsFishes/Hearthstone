# 战斗系统程序文档

## 1. 核心数据来源

### 1.1 Component

| Component | 职责 |
| --- | --- |
| `BattleCardRawComponent` | 保存单张卡牌的编号、种类 ID、阵营、槽位、随机生成的攻击力与最大生命、当前生命和存活状态；当前生命与存活状态可监听 |
| `BattleSessionSingletonRawComponent` | 保存双方卡牌 Entity、各自行动态游标、当前行动方、结果、当前攻击者/目标、随机数、行动倒计时和行动次数 |

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

当前无。

### 2.3 关联Task启动入口

当前无。

### 2.4 调用链路梳理

1. `HearthstoneGameEngine.OnAwake()` 进入 `BattleStage`。
2. `BattleStage.InitializeBattleRuntime.Load()` 创建战斗会话，并按 `BattleRules.GetCardNumber()` 为双方三个槽位逐一从 `DataApi` 取得编号配置与对应种类配置后创建卡牌 Entity；攻血在种类配置的整数闭区间内生成。
3. `BattleUiScene` 通过 `Ui/Battle` 的 `UiSceneAsset` 创建 `BattleView`；`BattleController` 通过两个 `UiList` 创建 `BattleCardItemController` 并绑定卡牌 Entity。
4. `BattleSystem.OnSystemUpdate()` 按固定间隔选择攻击者与随机存活目标，执行同时伤害并更新会话状态。
5. `BattleController` 和卡牌条目 Controller 通过 ModelWrapper 监听会话与卡牌 Component，刷新行动方、结果、生命、存活状态和高亮。
6. 离开 Stage 时关闭 UI、销毁双方卡牌 Entity，并移除战斗会话单例。

## 3. 战斗结算链路

### 3.1 单场战斗开始与结束

`InitializeBattleRuntime` 先创建 `BattleSessionSingletonRawComponent`，使用基于当前 UTC ticks 的非零随机种子初始化，再按槽位读取配置并创建双方卡牌。当前我方编号顺序为 `[4, 1, 40]`，敌方为 `[5, 2, 9]`，因此六个槽位覆盖五种怪物。任一编号或种类配置缺失、类型关联失配或卡牌创建失败时，已创建的双方卡牌和会话单例会被清理后重新抛出异常。

每次行动前后都根据存活掩码调用 `BattleRules.EvaluateResult()`。敌方无存活卡牌时为玩家胜利；玩家无存活卡牌时为敌方胜利；结果确定后 `BattleSystem` 不再执行行动。

### 3.2 回合或阶段推进

行动方从玩家开始。每方通过独立游标从左到右寻找下一张存活卡牌，行动后游标移到下一槽位。会话内的 `Unity.Mathematics.Random` 先在卡牌创建时生成攻血，再在行动时从对方存活卡牌中选择目标。行动完成且战斗未结束时切换阵营。

### 3.3 伤害与治疗结算

`BattleRules.ResolveSimultaneousDamage()` 同时计算攻击者与目标的新生命：双方当前生命分别减去对方攻击力并限制到不低于零。`BattleCardRawComponent.SetCurrentHealth()` 再限制到最大生命范围并同步 `IsAlive`。

当前无治疗、护盾、减伤、暴击或其他伤害修正。

### 3.4 Buff与状态结算

当前无。

### 3.5 单位创建与销毁

卡牌通过 `EcsApi.CreateEntity("BattleCard")` 创建并挂载池化 `BattleCardRawComponent`。初始化从编号表复制 `CardNumber` 与 `CardTypeId`，再从种类表的整数闭区间生成攻击和最大生命；当前生命设为最大生命，存活状态设为 true。

Stage 卸载时逐一调用 `EcsApi.DestroyEntity()`。Component 回收时先 Invalid 当前生命和存活监听，再重置卡牌编号、种类 ID、阵营、槽位与所有数值。

## 4. 所属GameStage

| GameStage | 内容 |
| --- | --- |
| `BattleStage` | `InitializeBattleRuntime` LoadItem、`BattleSystem` Update System、`BattleUiScene` 与 `Ui/Battle` 导出资产 |

两张卡牌 CSV 均属于全局 `GameEngineDefault` 数据组。引擎先初始化资源索引并通过 `ScriptableObjectAssets` 注册入口加载该组数据，再进入 `BattleStage`。
