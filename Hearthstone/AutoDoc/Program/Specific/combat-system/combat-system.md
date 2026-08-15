# 战斗系统程序文档

## 1. 核心数据来源

### 1.1 Component

| Component | 职责 |
| --- | --- |
| `BattleCardRawComponent` | 保存单张卡牌的编号、种类 ID、阵营、运行时等阶、槽位、攻击力与最大生命、当前生命和存活状态，并保存初始化完成时的 `EntryAttack`、`EntryHealth` 作为本场固定显示基准；玩家攻血与等阶来自 Run state 永久实例，敌方攻血随机生成并从种类默认值取得等阶 |
| `BattleSessionSingletonRawComponent` | 保存双方卡牌 Entity、行动游标、结果、随机数和行动状态；攻击表现期间还保存序列号、时钟、伤害提交点、攻击双方与待结算伤害快照；另保存本场已分配的备战奖励批次与防重复切换标记 |
| `RunStateSingletonRawComponent` | 跨 Battle/Preparation Group 保存玩家持有卡永久实例和三个战斗槽 |

### 1.2 Csv和ScriptableObject配置项

| 配置 | 路径 | 数据组 | 用途 |
| --- | --- | --- | --- |
| `BattleCardTypeCsvData` | `Assets/Resources/Config/BattleCardTypeCsvData.csv` | `GameEngineDefault` | 按种类 ID 提供显示名称、默认等阶、基础类型的生命与攻击整数闭区间、攻击帧图集资源键、现有音效库资源键、音效延迟和受击延迟；`100~213` 融合类型的攻血和初始词条为空，战斗读取其融合实例值 |
| `BattleCardCsvData` | `Assets/Resources/Config/BattleCardCsvData.csv` | `GameEngineDefault` | 按卡牌编号提供种类 ID、原画资源键和排序后的基础类型融合公式；当前包含普通卡 `1~98`、封印分隔位 `99` 和 114 张独立融合卡 `100~213`；其中 15 张双卡、34 张三卡、65 张四卡，公式在读取时同步建立内存索引 |

当前战斗系统未读取业务 ScriptableObject 配置。项目同时包含框架所需的空注册资产 `Assets/Resources/BbxCommon/ScriptableObjectAssets.asset`；`GameStage` 以该资产作为 Stage 数据加载入口，资产缺失时不会继续加载同组 CSV。

## 2. 战斗流程驱动

### 2.1 System

`BattleSystem` 是核心更新 System。空闲时按 `ActionInterval` 递减行动倒计时；倒计时归零后选择本次攻击双方并创建一段挂起的攻击表现。表现期间推进会话时钟，到达 `AttackAudioDelay` 时通过 `AudioApi` 播放一次现有资源库音效，到达 `HitDelay` 才写入双方及溅射生命，到达完整表现时长后才提交存活状态、胜负、行动游标和阵营切换；战斗结束后停止继续结算。

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
4. `BattleSystem.OnSystemUpdate()` 按固定间隔选择攻击者与随机存活目标，发布攻击表现序列，并在会话时钟到达配置时点后写入伤害；完整表现结束前不推进下一单位。
5. `BattleController` 和卡牌条目 Controller 通过 ModelWrapper 监听会话与卡牌 Component，刷新行动方、结果、攻击、生命、存活状态、高亮以及卡面攻击表现；攻血数字分别和 Component 中的入场基准比较后显示红、白或蓝色。
6. 战斗终结后 StageListener 自动进入备战；离开 BattleStage 时关闭 UI、销毁双方卡牌 Entity并移除战斗会话单例，Run state 保留。

## 3. 战斗结算链路

### 3.1 单场战斗开始与结束

`InitializeBattleRuntime` 先创建 `BattleSessionSingletonRawComponent`，使用基于当前 UTC ticks 的非零随机种子初始化。首次我方编号顺序为 `[4, 1, 40]`，其攻血只随机一次并持久保存；后续我方编号与攻血来自 Run state 三槽。敌方编号保持 `[5, 2, 9]` 并在每场战斗随机攻血。任一编号或种类配置缺失、类型关联失配或卡牌创建失败时，已创建的双方卡牌和会话单例会被清理后重新抛出异常。

每次行动开始前与攻击表现完成后都根据存活掩码调用 `BattleRules.EvaluateResult()`。敌方无存活卡牌时为玩家胜利；玩家无存活卡牌时为敌方胜利；结果确定后 `BattleSystem` 不再执行行动。表现中途离场时，会话回收会清空挂起状态和监听变量。

### 3.2 回合或阶段推进

行动方从玩家开始。每方通过独立游标从左到右寻找下一张存活卡牌，选中攻击者后游标移到下一槽位。会话内的 `Unity.Mathematics.Random` 先在卡牌创建时生成攻血，再在行动时从对方存活卡牌中选择目标。`AttackPresentationSequence` 唤醒 UI；`AttackPresentationElapsed` 由 System 推进。完整时长取前拱时长、八帧图集时长、音效延迟、受击延迟加闪红时长四者的最大值。完成且战斗未结束时才切换阵营并重新设置行动间隔。

### 3.3 伤害与治疗结算

攻击选择时先通过 `BattleRules.ResolveKeywordDamage()` 生成主伤害、反击伤害与溅射伤害快照，但不立即写入卡牌。会话时钟到达 `HitDelay` 后，`BattleSystem` 使用 `SetCurrentHealthWithoutAliveCommit()` 同时更新攻击者、主目标和相邻目标生命；表现结束时再调用 `CommitAliveState()` 同步 `IsAlive`，随后检查胜负。这样生命变化与目标闪红共用受击时点，而死亡、胜负和下一行动都等待动画结束。

当前无治疗、护盾、减伤、暴击或其他伤害修正。

### 3.4 Buff与状态结算

当前无。

### 3.5 单位创建与销毁

卡牌通过 `EcsApi.CreateEntity("BattleCard")` 创建并挂载池化 `BattleCardRawComponent`。玩家初始化复制 Run state 实例的永久攻击、最大生命和运行时等阶，敌方从种类表整数闭区间生成攻血并复制种类默认等阶；常规入口把当前生命设为最大生命，显式场景入口可传入已有当前生命。所有入口最终在 `InitializeValues()` 中把当时的攻击和实际当前生命分别记录为 `EntryAttack`、`EntryHealth`，后续伤害或属性提升不改写基准。

Stage 卸载时逐一调用 `EcsApi.DestroyEntity()`。Component 回收时先 Invalid 攻击、当前生命和存活监听，再重置卡牌编号、种类 ID、阵营、等阶、槽位、入场基准与所有当前数值。

## 4. 所属GameStage

| GameStage | 内容 |
| --- | --- |
| `RunStateStage` | 整局玩家卡牌实例和阵容；与 BattleStage 同组且在进入 PreparationStage 时继续存活 |
| `BattleStage` | `InitializeBattleRuntime` LoadItem、`BattleSystem` Update System、结果 StageListener、`BattleUiScene` 与 `Ui/Battle` 导出资产 |

两张卡牌 CSV 均属于全局 `GameEngineDefault` 数据组。引擎先初始化资源索引并通过 `ScriptableObjectAssets` 注册入口加载该组数据，再进入 `BattleStage`。
