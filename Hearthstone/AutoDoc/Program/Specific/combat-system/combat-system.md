# 战斗系统程序文档

## 1. 核心数据来源

### 1.1 Component

| Component | 职责 |
| --- | --- |
| `BattleCardRawComponent` | 保存单张卡牌的实际编号与类型、表现来源编号与类型、阵营、运行时等阶、槽位、攻击力与最大生命、当前生命、存活状态和 `Keywords`，并保存初始化完成时的 `EntryAttack`、`EntryHealth` 作为本场固定显示基准；玩家攻血、等阶、词条和表现来源来自 Run state 永久实例，敌方攻血随机生成且实际身份与表现身份一致 |
| `BattleSessionSingletonRawComponent` | 保存动态长度的玩家卡牌 Entity、固定三个敌方 Entity、本轮编号与是否最终轮、行动游标、结果、随机数和行动状态；攻击表现期间还保存序列号、时钟、伤害提交点、攻击双方与待结算伤害快照；结果阶段保存 `0.5 s` 延迟结算和胜利横幅演出倒计时，并以 `OutcomePresentationCompleted` 通知非最终轮继续推进 |
| `RunStateSingletonRawComponent` | 跨 Battle/Preparation Group 保存玩家持有卡永久实例、最多六个战斗槽及当前已解锁槽位数 |

### 1.2 Csv和ScriptableObject配置项

| 配置 | 路径 | 数据组 | 用途 |
| --- | --- | --- | --- |
| `BattleCardTypeCsvData` | `Assets/Resources/Config/BattleCardTypeCsvData.csv` | `GameEngineDefault` | 按种类 ID 提供显示名称、默认等阶、基础类型的生命与攻击整数闭区间、攻击帧图集资源键、现有音效库资源键、音效延迟和受击延迟；`100~213` 融合类型的攻血和初始词条为空，战斗读取其融合实例值；四卡玩家实例的名称与攻击表现按表现来源类型读取对应三卡配置 |
| `BattleCardCsvData` | `Assets/Resources/Config/BattleCardCsvData.csv` | `GameEngineDefault` | 按卡牌编号提供种类 ID、原画资源键和排序后的基础类型融合公式；当前包含普通卡 `1~98`、封印分隔位 `99` 和 114 张独立融合卡 `100~213`；其中 15 张双卡、34 张三卡、65 张四卡，公式在读取时同步建立内存索引 |
| `BattleKeywordCsvData` | `Assets/Resources/Config/BattleKeywordCsvData.csv` | `GameEngineDefault` | 按单一词条提供显示名称、玩家说明、显示顺序、伤害倍率、溅射距离、属性成长和反击抑制参数 |
| `BattleProgressionCsvData` | `Assets/Resources/Config/BattleProgressionCsvData.csv` | `GameEngineDefault` | 按连续 `BattleNumber` 配置该轮新增槽位数与摸牌数；第 1 轮必须为 `3/3`，累计槽位上限为 6；没有下一行时当前胜利视为整局胜利 |

当前战斗系统未读取业务 ScriptableObject 配置。项目同时包含框架所需的空注册资产 `Assets/Resources/BbxCommon/ScriptableObjectAssets.asset`；`GameStage` 以该资产作为 Stage 数据加载入口，资产缺失时不会继续加载同组 CSV。

## 2. 战斗流程驱动

### 2.1 System

`BattleSystem` 是核心更新 System。开战时按 `ActionInterval = 0.75 s` 递减初始倒计时；倒计时归零后选择本次攻击双方并创建一段挂起的攻击表现。表现配置通过攻击者的 `PresentationCardTypeId` 读取，因此四卡结果使用融合时确定的三卡版本帧图集、音效与受击时点，不读取静态四卡类型行作为权威表现；伤害、词条与等阶仍取四卡运行实例。表现期间以 `TimeApi.DeltaTime × AttackPresentationPlaybackSpeed` 推进共享会话时钟，当前速度系数为 `0.8`；帧动画、前拱、`AttackAudioDelay`、`HitDelay` 和闪红都读取这条时间轴。到达 `AttackAudioDelay` 时通过 `AudioApi` 播放一次现有资源库音效，到达 `HitDelay` 才写入双方及溅射生命，到达完整表现时长后才提交存活状态、行动游标和阵营切换。若一方此时没有存活单位，会话进入 `ResultSettlementDelay = 0.5 s` 的挂起结果状态并停止攻击；倒计时结束后才写入正式结果。玩家胜利还会由 System 推进 `1.16 s` 的横幅表现计时，完成后发布 `OutcomePresentationCompleted`；战斗继续时把下一次行动倒计时设置为 `AttackEndWaitDuration = 1.25 s`。

#### 2.1.1 重要的System顺序依赖

`HearthstoneGameEngine` 将执行顺序登记为 `InputSystem → BattleSystem → TaskSystem`。当前 `BattleSystem` 不依赖输入或 Task 的战斗数据，但通过统一登记保证其在主更新组中的稳定位置。

### 2.2 StageListener

`BattleResultPreparationStageListener` 监听 `OutcomePresentationCompleted`。只有非最终轮的玩家胜利会在横幅完整演出后调用 `BeginPreparationForBattle(BattleNumber + 1)`；失败与最终轮胜利留在当前 BattleStage，由结果弹窗等待玩家选择重新开始。

### 2.3 关联Task启动入口

当前无。

### 2.4 调用链路梳理

1. `HearthstoneGameEngine.OnAwake()` 不创建本局状态；`GameEngineDefault` 数据加载完成后先单独进入 `MainMenuStage`。玩家点击“开始游戏”后，引擎创建 `RunStateStage`、调用 `BeginPreparationForBattle(1)`，按轮次表生成首轮 3 张随机卡、累计解锁 3 槽，并进入 `RunStateStage + PreparationStage`。
2. Continue 捕获当前 `3~6` 个已解锁槽位的防御性阵容快照，进入同轮编号的 `RunStateStage + BattleStage`；`BattleStage.InitializeBattleRuntime.Load()` 按快照创建动态玩家 Entity，敌方仍创建三个默认 Entity，并按轮次表是否存在下一行写入 `IsFinalBattle`。
3. `BattleUiScene` 通过 `Ui/Battle` 的 `UiSceneAsset` 创建 `BattleView`；`BattleController` 通过两个 `UiList` 创建 `BattleCardItemController` 并绑定卡牌 Entity。
4. `BattleSystem.OnSystemUpdate()` 按各自实际数组长度选择攻击者与随机存活目标，发布攻击表现序列，以 `0.8` 倍速推进共享表现时钟，并在该时钟到达配置时点后写入伤害；完整表现结束前不推进下一单位，战斗继续时等待 `1.25 s`，一方耗尽时等待 `0.5 s` 后提交结果。
5. `BattleController` 和卡牌条目 Controller 通过 ModelWrapper 监听会话与卡牌 Component，刷新行动方、结果、攻击、生命、存活状态、高亮以及卡面攻击表现；卡牌首次绑定时根据表现来源编号与类型读取原画、名称和攻击帧，根据实际 `Keywords` 控制嘲讽盾牌轮廓并组合词条说明，攻血数字分别和 Component 中的入场基准比较后显示红、白或蓝色。`CurrentHealth` 下降时监听器按前后值差显示伤害浮字，攻血数值上升时播放双文本滑动过渡；新的攻击表现序列还会按攻击者实际词条触发冲锋号角与远射弓箭图标。
6. 玩家非最终轮胜利时，横幅从左入场、中央停留、向右离场，完成后 StageListener 进入下一轮备战；失败直接显示重开弹窗，最终轮胜利在横幅后显示整局胜利重开弹窗。重开会替换整局 RunStateStage 并从第 1 轮备战重新开始。

## 3. 战斗结算链路

### 3.1 单场战斗开始与结束

`InitializeBattleRuntime` 先创建 `BattleSessionSingletonRawComponent`，使用基于当前 UTC ticks 的非零随机种子初始化。玩家编号与永久攻血来自 Continue 捕获的当前 `3~6` 槽阵容，敌方编号保持 `[5, 2, 9]` 并在每场战斗随机攻血。任一编号或种类配置缺失、类型关联失配或卡牌创建失败时，已创建的双方卡牌和会话单例会被清理后重新抛出异常。

每次行动开始前与攻击表现完成后都根据双方实际槽位数和存活掩码调用 `BattleRules.EvaluateResult()`。敌方无存活卡牌时为玩家胜利；玩家无存活卡牌时为敌方胜利。候选结果先写入 `PendingResult` 并启动 `0.5 s` 倒计时，期间不再执行行动；倒计时结束才更新 `Result`。玩家胜利的结果表现倒计时覆盖横幅进入 `0.24 s`、停留 `0.68 s`、离开 `0.24 s`，最终发布完成标志。表现中途离场时，会话回收会清空挂起状态和监听变量。

### 3.2 回合或阶段推进

行动方从玩家开始。每方通过独立游标从左到右寻找下一张存活卡牌，选中攻击者后游标移到下一槽位。会话内的 `Unity.Mathematics.Random` 先在卡牌创建时生成攻血，再在行动时从对方存活卡牌中选择目标。`AttackPresentationSequence` 唤醒 UI；`AttackPresentationElapsed` 由 System 按每帧时间的 `0.8` 倍推进，因此前拱、八帧图集、音效延迟、受击延迟与闪红时长整体保持同速。完整表现时长取上述各项时间轴终点的最大值；完成且战斗未结束时才切换阵营，并把下一次行动等待设置为 `1.25 s`。

### 3.3 伤害与治疗结算

攻击选择时先通过 `BattleRules.ResolveKeywordDamage()` 生成主伤害、反击伤害与溅射伤害快照，但不立即写入卡牌。会话时钟到达 `HitDelay` 后，`BattleSystem` 使用 `SetCurrentHealthWithoutAliveCommit()` 同时更新攻击者、主目标和相邻目标生命；卡牌 Controller 在 `CurrentHealth` 监听回调中用旧值减新值取得这一次实际扣除量，显示对应伤害浮字。表现结束时再调用 `CommitAliveState()` 同步 `IsAlive`，随后检查胜负。这样生命变化、伤害浮字与目标闪红共用受击时点，而死亡、胜负和下一行动都等待动画结束。

当前无治疗、护盾、减伤、暴击或其他伤害修正。

### 3.4 Buff与状态结算

当前无。

### 3.5 单位创建与销毁

卡牌通过 `EcsApi.CreateEntity("BattleCard")` 创建并挂载池化 `BattleCardRawComponent`。玩家初始化复制 Run state 实例的永久攻击、最大生命和运行时等阶，同时保留实际结果卡号/类型并从实例表现来源卡号解析表现类型；敌方从种类表整数闭区间生成攻血并复制种类默认等阶，实际身份与表现身份使用同一卡。常规入口把当前生命设为最大生命，显式场景入口可传入已有当前生命。所有入口最终在 `InitializeValues()` 中把当时的攻击和实际当前生命分别记录为 `EntryAttack`、`EntryHealth`，后续伤害或属性提升不改写基准。

Stage 卸载时逐一调用 `EcsApi.DestroyEntity()`。Component 回收时先 Invalid 攻击、当前生命和存活监听，再重置实际编号/类型、表现编号/类型、阵营、等阶、槽位、入场基准与所有当前数值。

## 4. 所属GameStage

| GameStage | 内容 |
| --- | --- |
| `RunStateStage` | 整局玩家卡牌实例、六槽阵容与已解锁槽位数；与 BattleStage 同组且在进入 PreparationStage 时继续存活，重开时整体替换 |
| `BattleStage` | `InitializeBattleRuntime` LoadItem、`BattleSystem` Update System、非最终胜利结果 StageListener、`BattleUiScene` 与 `Ui/Battle` 导出资产 |

三张卡牌相关 CSV 与一张轮次推进 CSV 均属于全局 `GameEngineDefault` 数据组。引擎先初始化资源索引并通过 `ScriptableObjectAssets` 注册入口加载该组数据，进入主菜单；玩家开始新一局后才从第 1 轮 `PreparationStage` 进入战斗流程。
