# 怪物战斗词条与融合继承实施 Plan

## 1. 需求明确

### 1.1 需求对齐

**验收方式（策划案第 6.1 节，优先记录）**：`FUNC-01`～`FUNC-10` 全部使用“流程log验收”。主代理通过正式 `GameStageEntryLauncher` 进入 `RunStateStage + PreparationStage`，以真实卡池、融合槽、出战槽和融合按钮生成所需产物，再通过生产 `HearthstoneGameEngine.EnterBattleStageGroup(...)` 进入三卡位自动战斗；每趟从启动时间隔离 `[BattleKeyword]` 日志。日志逐次记录卡牌当前词条、目标候选掩码与最终槽位、冲锋前后攻血、主目标/相邻目标/攻击者的结算前后值、主伤害/爆裂伤害/反伤、延迟死亡提交和胜负。随机目标场景按固定前置状态重复实际战斗，只有同时取得所有要求目标槽的本次运行证据才通过。`ART-01` 不以流程日志代替，主代理检查实际 Prefab、字体与 `0/1/4` 词条静态编排。

预计四趟流程：K1（五类普通怪物映射、单/多嘲讽、远射奇数与 1 攻）、K2（爆裂边缘/中间、空位/死亡位、远射+爆裂连续取整）、K3（冲锋连续两次、四词条组合与统一死亡/胜负）、K4（重复/不同/无词条素材融合、固定顺序显示与产物进战斗）。每趟保存到 `AutoDoc/Temp/monster-combat-keywords-trip-k<序号>-flow-log.md`，保留“操作步骤→关键 Log→预期→实际→原编号”映射。

四趟都使用 Unity Editor 通过现有 `PreparationStageEntryAsset` 类型创建的专属正式 Entry 资产；资产由两案 a～d 后的唯一串行整合者创建，不改写或复用融合/继续案现有 Entry。每个 Entry 仍只提交强类型奖励批次并进入正式 Preparation Group；玩家必须在 Preparation 正式编成 UI 中逐槽放入表内卡牌（`Empty` 保持空槽），提交后场景 DTO 只允许以同槽同 CardNumber 补充可复现战斗状态，校验不一致即拒绝进战斗。进入 Battle 时只向生产 `BattleStageStartupData.WithScenario(BattleScenarioStartupData)` 传强类型场景输入，禁止验收脚本直接写 Component、Entity、随机数或生命。每个子场景结束后停止 Play，下一子场景从对应 Entry 新启动；运行时不满足判定即如实失败，不在 Play 中修状态。

**K1 Entry 与可复现子场景**：

| 项 | 精确输入/判定 |
| --- | --- |
| Entry | `Assets/Resources/Editor/MonsterKeywordsK1Entry.asset`；BatchId=`monster-keywords-k1-001`；奖励 `5=5/30`、`6=1/30`、`2=4/30`、`82=2/100`、`8=3/100`。加上默认首次持有 `4/1/40`，Preparation 卡池实际覆盖类型1～5；奖励卡号和攻血均由真实批次写 Run state |
| K1-A 单嘲讽 | 玩家槽 `[82=2/100/100,5=5/30/30,40=0/100/100]`，敌方槽 `[40=25/100/100,44=25/100/100,45=25/100/100]`，seed=`0x4B310001`；场景输入的玩家 CardNumber 必须逐槽等于 UI 提交阵容，词条仍从 Run instance 取。82存活期间每次敌方行动目标只能为槽0；其被累计4次敌方攻击结算死亡后，下一次敌方候选恢复为玩家其它全部存活槽 |
| K1-B 远射5攻 | 玩家槽 `[5=5/30/30,Empty,Empty]`，敌方槽 `[40=4/50/50,9=4/50/50,44=4/50/50]`，seed=`0x4B310002`；9号中槽嘲讽强制主目标，预期主伤害2、反伤0 |
| K1-C 远射1攻 | 玩家槽 `[6=1/30/30,Empty,Empty]`，敌方同K1-B，seed=`0x4B310012`；预期主伤害0、反伤0且 ActionIndex 递增 |
| K1-D 多嘲讽 | 玩家槽 `[82=0/100/100,4=0/100/100,40=0/100/100]`，敌方槽 `[40=0/100/100,44=0/100/100,45=0/100/100]`；依次使用 seed `0x4B310003`、`0x4B310013`、`0x4B310023`，每个 seed 最多观察12次敌方选目标。取得任一 seed 下槽0和槽1都实际出现且槽2从未出现即通过；三次固定 seed 都未满足则 `FUNC-03` 失败，不无限重试 |
| 恢复 | 每个子场景 Stop Play；专属 Entry 资产不改；新 Play 重新创建 Run state，因此同 BatchId 在新 Run 内正常首次应用。K1 结束后确认 Edit Mode、Console 无新增 Error |

**K2 Entry 与可复现子场景**：

| 项 | 精确输入/判定 |
| --- | --- |
| Entry/融合 | `Assets/Resources/Editor/MonsterKeywordsK2Entry.asset`；BatchId=`monster-keywords-k2-001`；奖励 `14=2/20`、`20=2/20`、`30=2/20`、`35=1/20`、`54=0/20`。玩家用实际 UI 融合 `14+20+30+35=99`，真实结果必须为 Attack=7、MaxHealth=80、Keywords=`LongShot|Blast` |
| K2-A 中槽/同批死亡提交 | 玩家槽 `[99=RunState/CurrentHealth=80,Empty,Empty]`；敌方槽 `[40=2/20/20,9=4/3/3,44=2/1/1]`；seed=`0x4B320001`。中槽9嘲讽强制目标：主伤害3使主目标归零，爆裂仍对左右各结算1并使槽2归零；槽0剩19；三份生命变化完成后才同批提交槽1/2死亡 |
| K2-B 边缘/空位不跨越 | 从同一 Entry 新 Play 重做真实融合；玩家同K2-A；敌方槽 `[9=4/20/20,Empty,44=2/20/20]`；seed=`0x4B320002`。边缘槽0嘲讽强制目标，主伤害3；槽1为空，槽2不得因爆裂受伤 |
| K2-C 中槽/既有死亡位 | 从同一 Entry 新 Play 重做真实融合；玩家同K2-A；敌方槽 `[40=2/20/20,9=4/20/20,44=2/20/0]`；seed=`0x4B320003`。中槽9嘲讽强制目标，槽0受爆裂1，启动时已死亡的槽2保持0且不返伤、不被重复提交 |
| 恢复 | 每个子场景 Stop Play，不沿用已受伤 Entity；每次从 Entry 重做真实融合，禁止直接构造99或写词条。三次都核对材料消耗、99永久值和正式 Battle Entity |

**K3 Entry 与可复现子场景**：

| 项 | 精确输入/判定 |
| --- | --- |
| Entry/融合 | `Assets/Resources/Editor/MonsterKeywordsK3Entry.asset`；BatchId=`monster-keywords-k3-001`；奖励 `2=1/30`、`14=1/30`、`82=1/30`、`5=0/50`、`8=0/50`。加默认持有1号后，用实际 UI 融合 `1+2+14+82=99`，真实结果为四词条；1号永久攻血使用本次 Run 的实际值，事务日志必须列出并据此求出99永久攻血，不伪造固定总值 |
| K3-A 冲锋两次/死亡友军 | 玩家槽 `[99=RunState/CurrentHealth=MaxHealth,5=0/50/0,8=0/50/50]`，其中5号以强类型 CurrentHealth=0 作为已死亡友军；敌方槽 `[40=0/100/100,44=0/100/100,45=0/100/100]`，seed=`0x4B330001`。99嘲讽使敌方攻击只选99；观察99完成两次实际攻击，存活99/8累计+2/+2，死亡5不变；每次99按冲锋后的Attack依次执行远射、爆裂和反伤门控 |
| K3-B 四词条/新死亡与胜负顺序 | 从同一 Entry 新 Play 重做真实融合；玩家槽 `[99=RunState/CurrentHealth=MaxHealth,5=0/50/50,8=0/50/50]`；敌方槽 `[40=2/100/10,9=2/100/1,44=2/100/10]`，seed=`0x4B330002`。中槽9嘲讽强制99攻击它；冲锋后远射主伤害使中槽死亡，爆裂按原中槽波及两侧，统一提交死亡/结果；战斗未结束后敌方下一次攻击因99嘲讽只选99，取得敌我双方均实际攻击的日志 |
| 恢复 | 每个子场景 Stop Play 后 Run state/Entity 全部回收；不把场景显式 CurrentHealth 或本场冲锋增益写回永久实例。每次从 Entry 重做真实融合；K3 Review 用融合事务的实际1号数值复算，不把随机范围当固定验收值 |

**K4 Entry 与可复现子场景**：

| 项 | 精确输入/判定 |
| --- | --- |
| Entry/融合 | `Assets/Resources/Editor/MonsterKeywordsK4Entry.asset`；BatchId=`monster-keywords-k4-001`；奖励 `2=2/20`、`56=3/20`、`5=1/20`、`8=1/20`、`82=1/20`。加默认持有1号（Charge）与40号（None），用实际 UI 融合 `1+2+40+56=99`：2/56重复贡献Blast，1贡献Charge，40不贡献，结果必须精确为 `Blast|Charge`；永久攻血按实际1/40快照求和 |
| K4-A 产物展示/进战斗 | 玩家槽 `[99=RunState/CurrentHealth=MaxHealth,Empty,Empty]`；敌方槽 `[40=0/100/100,44=0/100/100,45=0/100/100]`；seed=`0x4B340001`。Preparation 99卡面与正式 Battle Entity 都显示/使用同一 `Blast|Charge`，无重复Blast、无None |
| 条件不可达边界 | 不把99放回素材槽。开发测试直接调用纯 `UnionKeywords`，输入一个已去重多词条集合与其它集合，证明幂等；正式流程只证明首次真实融合并集。Review 明确“99再次作素材”被既有配方禁止、未执行 |
| 恢复 | Stop Play；确认素材消耗与出战槽清理只发生一次、未改 Batch 指纹账本，Edit Mode Console 无新增 Error |

**美术资产验收覆盖（与程序功能分开）**：

| 编号 | 主干 | 资产落点与编排位置 | 检视方式与通过证据 |
| --- | --- | --- | --- |
| `ART-01` | 是 | `Assets/Resources/Ui/BattleCardItem.prefab` 的既有下部说明底板、怪物名称和新增词条信息组；同一信息组缩放规则同步到 `PreparationCardItem.prefab`、`PreparationSlotItem.prefab`、`PreparationFusionSlotItem.prefab` | 在实际 Prefab/Builder 产物中分别填入 `0/1/4` 个词条：无词条只留名称和空词条区；单词条完整可读；四词条按“嘲讽、远射、爆裂、冲锋”稳定排列，无截断、重叠、越界或不可辨认缩字。核对名称、词条、底板层级，以及卡框、原画、编号、攻血徽章仍保持既有方向 |

**程序功能验收覆盖（实际运行行为）**：

| 编号 | 主干 | 功能落点 | 流程操作、日志与通过条件 |
| --- | --- | --- | --- |
| `FUNC-01` | 是 | `BattleCardTypeCsvData.InitialKeyword`、`RunCardInstanceData.Keywords`、`BattleCardRawComponent.Keywords` 与四类卡面 Controller | K1 同时查看并让五类普通怪物进战斗；初始化和 Presentation 日志逐一证明战士=嘲讽、弓手=远射、投弹手=爆裂、野猪=冲锋、食人魔=None，且同一 Entity 的 UI 文本集合等于战斗集合 |
| `FUNC-02` | 是 | `BattleRules.FilterTargetCandidateMask` 与 `BattleSystem` 目标选择 | K1 防守方仅一个存活嘲讽并另有非嘲讽；全部攻击日志的 CandidateMask 只含该槽，死亡提交后下一次候选恢复为全部有效槽 |
| `FUNC-03` | 是 | 同一候选过滤与会话随机数 | K1 防守方布置两个高生命嘲讽和一个高生命非嘲讽，重复实际行动；所有目标都属于两嘲讽槽，日志必须实际出现两个嘲讽槽且从未出现非嘲讽槽；开发测试同时证明候选 ordinal 不被固定映射到单槽 |
| `FUNC-04` | 是 | `BattleRules.ScaleDamageFloor`、反伤门控与原子结算 | K1 让当前攻击力 5 的远射单位命中有攻击力的目标；主伤害=2、CounterDamage=0，攻击者不因本次主目标反伤掉血 |
| `FUNC-05` | 是 | 同上 | K1 让当前攻击力 1 的远射单位攻击；主伤害=0，但 ActionIndex、目标选择和整次攻击完成日志仍提交，CounterDamage=0 |
| `FUNC-06` | 是 | `BattleRules` 相邻槽计算、`BattleSystem` 三槽伤害快照与延迟死亡 | K2 分别取得边缘和中间主目标；日志证明只伤害槽号差 1 的存活单位，空/死亡位不跨越、主目标不重复、相邻目标不反伤，主目标先降至 0 时爆裂仍按原槽完成后统一提交死亡 |
| `FUNC-07` | 是 | `BattleCardRawComponent.ApplyBattleStatGain` 与冲锋阶段 | K3 让冲锋单位实际攻击两次并保留一个死亡友军；每次先记录存活友军 Attack/MaxHealth/CurrentHealth 各 +1，攻击者本次伤害使用新攻击力，死亡友军不变，两次后存活单位累计 +2/+2 且 CurrentHealth≤MaxHealth |
| `FUNC-08` | 是 | `RunCardRules.TryFuse` 的词条并集、`FusionTransactionSnapshot` 与卡面刷新 | K4 使用重复词条、不同词条和食人魔组成合法 99：事务日志列出每份素材当前集合，结果为去重并集，无 `None`，结果卡面按固定顺序显示；结果编入战斗后 Entity 集合一致 |
| `FUNC-09` | 是 | 远射主伤害→爆裂半伤→反伤门控顺序 | K2 使用当前攻击力 7 的远射+爆裂产物命中两侧均存活的中间目标；主伤害=3、两侧各=1、攻击者主目标反伤=0、相邻反伤=0 |
| `FUNC-10` | 是 | 冲锋→嘲讽目标过滤→远射→爆裂→反伤→死亡/胜负提交的统一链 | K3 使用四词条产物，让敌我双方各完成攻击直到至少一次死亡；日志中四个词条各执行一次且顺序唯一，不提前移除死亡槽、不重复伤害或错误结束战斗 |

`FUNC-08` 中“融合产物后续继续参与融合”是条件性边界：当前已完成的融合公开契约明确禁止 99 号结果再次成为素材，本篇又明确不改融合配方，因此生产玩家流程不存在该前置场景。实现仍让并集函数接受任意已经去重的词条集合，并用定向测试证明集合幂等；不得为取得流程日志而解除 99 素材禁令。正式 Review 应把该不可达条件与既有契约证据写清，但不把其伪装成已执行的玩家流程。

**关键旧功能回归**：

| 编号 | 主干 | 回归点与证据 |
| --- | --- | --- |
| `RGR-01` | 是 | 原左右轮转攻击者、敌我交替、存活跳过、结果终止与三槽位置语义不变；无词条双方仍得到与旧 `ResolveSimultaneousDamage` 等价的互伤结果 |
| `RGR-02` | 是 | 现有 1～99 持有/三槽、奖励 Batch 原子幂等、99 唯一、素材消耗与出战槽同步清理不变；词条字段纳入卡实例值语义和融合事务，但不纳入 Batch payload 指纹，也不恢复已消耗素材 |
| `RGR-03` | 是 | 远射只屏蔽本次主目标反伤，不免疫之后主动攻击或爆裂；爆裂相邻单位绝不反伤；0 伤害仍推进一次有效行动 |
| `RGR-04` | 是 | 冲锋只改本场 `BattleCardRawComponent`，不回写 `RunCardInstanceData` 永久攻血；离开 BattleStage 后增益随 Entity 回收，下一战仍从永久值初始化 |
| `RGR-05` | 是 | Battle/Preparation 动态条目继续通过 `UiList` 预载和回池；View 只保存引用，Controller 监听 Component/刷新表现，不运行时拼装静态卡面，不新增通用 `BbxUiItem` |
| `RGR-06` | 是 | 既有 `int BattleCardRawComponent.Attack` 读取/直接赋值和相关测试保持源码兼容；生产初始化/冲锋同步 `AttackValue`，Controller 首次绑定同步镜像，旧无词条调用不因新增监听改变攻击数值 |
| `RGR-07` | 是 | 所有既有 `BattleStageStartupData` 构造、`CreateDefault()`、Continue正常下一关均保持 `Scenario=null`；此时玩家仍从Run三槽、敌方仍用`5/2/9`、seed仍由UTC生成，默认3v3日志/行为与改动前一致。不同非null Scenario canonical key不得被Coordinator误合并 |

**开发期定向测试（不替代正式验收）**：新增独立 `BattleKeywordRulesTests.cs`，不与“继续”案共享 `BattleRulesTests.cs`。覆盖 CSV 四词条完整性与类型映射、固定显示序、taunt mask、整数连续减半、0 伤害、边缘/中间相邻槽、延迟死亡、冲锋存活过滤和上限、融合去重/None/嵌套集合幂等、99 禁止为素材、无词条旧互伤等价、`int Attack` 旧读写与 `AttackValue` 初始化/冲锋/Bind同步、Component 回收 Invalid/清集合、场景 DTO 深拷贝/空槽/非法值/默认null兼容，以及四类 Prefab 的 `0/1/4` 布局。共享字体字形和资源索引只由串行整合者验证，不由关键词测试改写资产。

确定边界：本篇不调整基础攻血区间、行动顺序、融合编号/配方/唯一性、奖励内容、继续按钮、下一关、怪物种类或战场背景；不新增词条等级/层数、主动选目标、动画、音效或第二套战斗状态。策划第 7 章的“每次攻击和受击开启 Task”经现状核验不采用：当前攻击是 `BattleSystem` 每 0.75 秒触发的一次同步原子事务，词条没有跨帧时序、图配置或复用子图需求；强行拆成两个 Task 会使目标、生命、死亡和胜负出现可观察中间态，并要求新的 Context/图集/节点/回收链。TaskSystem 顺序和 Task 公开契约保持不变；若未来加入跨帧演出，再另案把已完成的纯结算结果交给 Task 表现层消费。

## 2. 数据部分

### 2.1 涉及到的数据概览

| 数据 | 唯一权威来源 | 产生者 | 消费者 | 生命周期 |
| --- | --- | --- | --- | --- |
| 词条定义、显示名、顺序与效果参数 | 新增 `BattleKeywordCsvData` + `DataApi` | `GameEngineDefault` CSV | 规则、卡面格式化、配置校验 | 全局 DataGroup |
| 普通怪物初始词条 | `BattleCardTypeCsvData.InitialKeyword` | 类型 CSV | 奖励实例、首次阵容、敌方 Entity 初始化 | 全局 DataGroup |
| 玩家卡当前可继承词条集合 | `RunCardInstanceData.Keywords` | 初始类型映射或融合并集 | Preparation UI、玩家 Battle Entity、后续融合规则 | 整局 Run state；融合会创建新值 |
| 本场实际词条集合 | `BattleCardRawComponent.Keywords` | 玩家永久实例或敌方初始类型 | BattleSystem、Battle UI、流程日志 | 当前 Battle Entity |
| 冲锋后的本场攻血 | 同一 `BattleCardRawComponent` | BattleSystem | 后续攻击、UI、胜负日志 | 当前战斗；不回写 Run state |
| 单次结算快照 | `BattleActionResolutionData` 只读值 | `BattleRules`/BattleSystem | 本次写入与日志 | 单次调用局部值，不持久化、不建平行状态 |
| 可复现战斗场景输入 | 新增独占源码 `BattleScenarioStartupData` + `BattleCardSlotStartupData` | 正式 Entry/验收调用方 | `BattleStageStartupData` 快照、`InitializeBattleRuntime` | 单个 Battle Stage；默认生产入口为 null |

`EBattleKeyword` 使用 `[Flags]` 且只允许 `None/Taunt/LongShot/Blast/Charge` 四位；集合存值，显示顺序从配置派生并验证四项唯一。`None` 始终为零值，不参与并集、显示或效果。所有集合在写入卡实例和 Entity 前通过 `BattleKeywordRules.Normalize/Validate`，避免未知位进入 UI 与结算。

`BattleCardSlotStartupData` 是强类型不可变 DTO：`IsOccupied`、`CardNumber`、`StatSource(RunState/Explicit)`、`Attack`、`MaxHealth`、`CurrentHealth`，并提供规范 `Empty`；`BattleScenarioStartupData` 防御性持有恰好3个玩家槽、3个敌方槽和非零 `RandomSeed`。空槽必须全零；占用槽要求有效配置、Attack≥0、MaxHealth>0、0≤CurrentHealth≤MaxHealth。玩家 `RunState` 模式逐槽读取真实实例；玩家 `Explicit` 模式仍要求 CardNumber 已拥有且等于 UI 提交的对应 Run battle slot，只覆盖本场攻血/当前生命，Keywords 始终从该 Run instance 复制；敌方显式模式按 CardNumber 配置取得类型与初始词条，仅攻血/当前生命来自 DTO。这样 Entry 能准备空槽、死亡位和可控攻血，但不能伪造融合词条或绕过实际编成。

### 2.2 新增数据列表

#### 2.2.1 新增CsvData类

| 类名 | 重要字段 | 登记与校验 |
| --- | --- | --- |
| `BattleKeywordCsvData` | `EBattleKeyword Keyword`、`DisplayName`、`DisplayOrder`、`DamageNumerator/Denominator`、`AffectRange`、`AttackGain/MaxHealthGain/CurrentHealthGain`、`PreventsCounterDamage` | `EDataLoad.Override`、`GameEngineDefault`，按枚举 int key 登记 `DataApi`；CSV 恰好四行且顺序唯一，分母>0、增益非负。远射=1/2且免反伤，爆裂=1/2且距离1，冲锋=1/1/1，嘲讽无伤害修正 |

### 2.3 原有数据类新增/删除字段

#### 2.3.1 原有Component类新增/删除字段

| Component/内嵌值 | 字段改动 | 生命周期与回收要求 |
| --- | --- | --- |
| `RunCardInstanceData` | 新增不可变 `EBattleKeyword Keywords`；构造、相等与 HashCode 都纳入规范化集合 | 普通实例在创建时从类型配置取初始词条；融合结果取素材当前集合并集；默认值仍无效。不能从 CardNumber=99/type6 反推融合结果词条 |
| `BattleCardRawComponent` | 新增 `EBattleKeyword Keywords` 与派生监听镜像 `ListenableVariable<int> AttackValue`；保留兼容的现有 `int Attack` 作为规则和旧调用方的唯一攻击权威，所有生产初始化/冲锋写入都经 `SetAttack` 同步镜像；保留 `MaxHealth` 和现有 CurrentHealth/IsAlive，新增本场增益与延迟生命/存活提交入口 | 玩家从 Run instance 复制词条/永久攻血，敌方从类型配置初始化；Controller Bind 前用 `SyncAttackValue()` 对齐可能由旧测试/调用方直接写入的 Attack；回收先 Invalid AttackValue/CurrentHealth/IsAlive，再清词条和数值，禁止池化残留 |
| `FusionMaterialSnapshot/FusionTransactionSnapshot` | 素材快照和结果卡同时包含规范化词条集合 | 只读防御性快照；供实际成功事务日志与测试复核，不成为第二份权威状态 |

#### 2.3.2 原有CsvData类新增/删除字段

| CsvData | 字段与配置改动 |
| --- | --- |
| `BattleCardTypeCsvData` | 新增 `EBattleKeyword InitialKeyword` 并在 `BattleCardTypeCsvData.csv` 增加同名列：类型1=Taunt、2=LongShot、3=Blast、4=Charge、5=None、6=None；加载时拒绝普通类型拥有多个初始词条或未知枚举 |

新 CSV 遵循两行英文列说明与 Associated 规范；两案a～d后的唯一串行整合者通过 Unity/项目资源导出流程把 key `BattleKeywordCsvData` 合并进资源索引，关键词执行代理不写索引且任何人都不手写 `ResourcesDictionary.json`。既有卡号 CSV 和1～99行不改。

## 3. 游戏逻辑部分

### 3.1 涉及到的游戏逻辑概览

保留 `BattleSystem` 为唯一行动驱动。一次 Action 依次执行：确认攻击者→冲锋写本场存活友军→构造存活/嘲讽候选 mask 并随机选主目标→按远射配置算主伤害→按主伤害算相邻爆裂→计算主目标反伤→先得到所有参与者的新生命快照→一次写入生命→统一刷新死亡→评估胜负/切边。主目标即使本次主伤害归零或致死，后续爆裂/反伤仍按动作开始时的槽位与快照完成。

不在 System 中保存卡牌权威状态，不按攻击分配集合、List 或临时 Entity；三槽通过标量/固定槽快照完成，避免每 0.75 秒 GC。`BattleKeywordRules` 只负责配置读取、集合规范化、固定显示和并集；纯数值/掩码算法放在现有 `BattleRules`，避免 UI、融合和战斗各复制一套规则。

新增普通静态逻辑类 `BattleKeywordRules`：读取/校验四行 DataApi 配置，规范化集合，按配置顺序格式化中文显示，计算素材词条并集；它不持有运行时卡牌状态，不负责行动调度，因此不属于 System 或 StageListener。

### 3.2 原有逻辑类改动

| 类名/文件 | 改动方向 |
| --- | --- |
| `BattleRules` | 新增存活+嘲讽候选过滤、配置分数向下取整、相邻槽判定与 `BattleActionResolutionData` 计算；保留旧游标、交替和结果规则。无词条输入必须与旧同时互伤结果等价 |
| `BattleSystem` | 把当前直接互伤替换为上述唯一顺序；冲锋只遍历当前行动方存活 Entity；目标随机只在过滤后的 mask 内；先计算全量再延迟提交死亡。每 Action 输出一组稳定 `[BattleKeyword]` 日志，包含 ActionIndex、双方/槽、候选、词条、增益、伤害、反伤、前后值、死亡与结果 |
| `RunCardRules.ApplyRewardBatch/InitializeFirstBattleLineup` | 普通持有实例创建时从卡号→类型配置解析初始词条并固定进实例；BatchId/payload 指纹仍只由原 grant 的编号/永久攻血组成，不因静态配置字段扩展改变历史幂等键 |
| `RunCardRules.TryFuse` | 成功前遍历素材 `RunCardInstanceData.Keywords` 取规范化并集，和攻血一起写入 99 结果；重复词条只保留一位，None 无贡献。其余预校验、原子消耗、出战槽清理、99 禁止素材/唯一性完全不变；输出可复核的融合词条事务日志 |
| `BattleCardRawComponent` | 保留 `int Attack` 兼容入口；初始化玩家/敌方词条；`SetAttack/SyncAttackValue` 维护可监听镜像，`ApplyBattleStatGain` 只通过同步入口写本场攻击；提供只写 CurrentHealth 的延迟入口和最后统一刷新 IsAlive 的提交入口。既有读取 `card.Attack`、对象初始化和测试无需批量改签名 |

不新增 System、StageListener、TaskContext、Task 节点或 Task 图集。

## 4. UI部分

### 4.1 涉及到的UI部分概览

词条信息使用现有动态卡牌条目，不新增页面、Hud、UiScene 或 UiGroup。四类条目的 Prefab 静态层级在名称下加入独立 `KeywordText`；Controller 只调用同一 `BattleKeywordRules.FormatDisplayText`，不得根据类型自行拼词条。四词条使用固定两行/两列式可读排布，顺序仍为配置顺序；无词条将对象保持激活但文本为空或隐藏词条组，名称与底板位置不跳变。

`BattleCardItem` 监听本场 AttackValue Dirty；Preparation 三类条目读取永久实例 Keywords。关键词执行代理只修改四个一一对应 Builder并产出四个独占 Prefab；Preparation 三个 Controller→Prefab 的 PreLoad key/路径没有变化，执行代理只做 Pre-UiInit，不触发 `ExportPreloadedView`，不写共享 PreLoad。页面级场景位置、Group、DefaultShow 与导出 Asset 也不由关键词代理改；两案 a～d 后由唯一串行整合者统一核对/必要时重导 PreLoad、Preparation场景与导出 Asset，并证明现有 Connected Prefab 与 Resources 路径仍有效。

### 4.2 原有Ui/Hud改动

| View类名 | 对应页面/条目 | 新增或调整控件 |
| --- | --- | --- |
| `BattleCardItemView` | 战斗双方卡牌 | 把既有名称引用明确为 `NameText`，新增 `KeywordText`/词条组引用；保留原画、阵营框、编号、攻血、高亮、死亡遮罩 |
| `PreparationCardItemView` | 固定卡池持有卡 | 在 `OwnedState` 名称下新增 `KeywordText`，不遮编号、素材已选角标或攻血 |
| `PreparationSlotItemView` | 三个出战槽 | 在 `OccupiedState` 名称下新增 `KeywordText`，空态必须清文本 |
| `PreparationFusionSlotItemView` | 四个融合素材槽 | 在 `OccupiedState` 名称下新增 `KeywordText`，移动/替换/清空时同步刷新 |

| Controller类名 | 数据监听/刷新改动 |
| --- | --- |
| `BattleCardItemController` | 绑定 `BattleCardRawComponent.Keywords` 形成显示，Bind 时先 `SyncAttackValue()`，再监听 AttackValue Dirty 以显示冲锋后的攻击；换绑/回池清词条、AttackValue 监听与文本；仅在显示集合变化时记录 Presentation 日志 |
| `PreparationCardItemController` | 从 `RunState.CardInstances[number].Keywords` 刷新固定池条目；融合成功后随 Run Revision 显示 99 并集 |
| `PreparationSlotItemController` | 从所占 Run instance 刷新词条；空槽清文本，不保存副本 |
| `PreparationFusionSlotItemController` | 从素材 Run instance 刷新词条；替换/移除清旧文本，不把选择写回实例 |

不新增或修改通用 `BbxUiItem`，因此不产生 `AutoDoc/UIItem` 文档改动。`PreparationController/PreparationView/PreparationViewUiBuilder/PreparationView.prefab` 属于并行“继续”案，本案不编辑。

## 5. 美术部分

### 5.1 涉及到的美术表现概览

继续使用 `UI-STYLE-001`，本案只重排 Prefab 内 TMP 信息层级，不新增位图。战斗卡既有深色 `SkillArea`、红/蓝金卡框、原画、编号和攻血徽章，以及 Preparation 卡面的同系框架均直接复用。词条名使用项目 Noto Sans SC 字体、暖金/浅色高对比文字；名称字号大于词条，四词条仍在既有说明区安全区内完整显示。

### 5.2 美术资产完整性检查

| 资产或资产组 | 用途 | 候选已有资产及路径 | 复用结论 | 判断依据 | 缺失或不满足 | 处理方式 |
| --- | --- | --- | --- | --- | --- | --- |
| 卡牌说明底板与卡框 | 名称+词条信息承载 | `Assets/Resources/Ui/BattleCardItem.prefab/SkillArea`、`CardFrame-v3.png`、`CardFrameBlue-v2.png`；Preparation 三条目现有卡面层级 | 已有且直接复用 | 风格、对比度和下部说明语义与策划原型一致，扩大/重排 TMP 不需改位图 | 当前只有单一名称文本，缺独立词条组 | Builder 在同一静态底板内建立名称/词条层级，不覆盖任何 Sprite |
| 属性、编号、原画与状态覆盖 | 保持旧卡面识别 | `AttackBadgeFrame.png`、`HealthDropBadge.png`、`CardNumberBadgeHex.png`、`Assets/Resources/Art/BattleCards/*.png`、既有高亮/死亡层 | 已有且直接复用 | 本案不改变这些语义、规格或状态 | 无 | 核对 sibling 层级与引用不被文字重排破坏 |
| 中文词条文字 | 嘲讽/远射/爆裂/冲锋 | `Assets/Resources/Fonts/NotoSansSC-Dynamic SDF.asset` | 已有字体直接复用并补字形 | 与当前全部中文 UI 相同，Dynamic/Multi Atlas 能覆盖新字 | 需确认八个词条字形 | 两案 a～d 后由唯一串行整合者通过 TMP/Editor API 一次合并继续按钮与词条字符并验证，不生成图片文字、不替换字体；关键词执行代理不保存共享字体资产 |

## 6. GameStage部分

本篇不新增 GameStage、System 注册或 UiScene 注册；新增的场景 DTO 位于关键词独占源码 `Assets/Scripts/Hearthstone/GameStage/BattleScenarioStartupData.cs`。`BattleStageStartupData.cs`、`BattleStages.cs` 和 Engine request key 已由 Continue 案独占，关键词执行代理不得修改。并行步骤 c 中先由关键词代理落定 DTO/`BattleCardRawComponent` 的公开初始化契约，再通知 Continue 执行方；Continue 执行方作为这些文件的唯一写入者串行接入，完成后双方各自按 Plan 做静态核对，禁止同时编辑或事后覆盖。

### 6.1 修改LoadItem和LateLoadItem项

| LoadItem项名 | 负责内容 | 所属GameStage |
| --- | --- | --- |
| `InitializeBattleRuntime` | 由 Continue 文件所有者接入 `BattleStageStartupData.Scenario`：null 时逐字保持旧默认三玩家/三敌人、UTC非零 seed 与 Run state 初始化；非null时校验3+3槽和 seed，空槽写 `Entity.Null`，占用槽只通过 `BattleCardRawComponent.InitializePlayer/InitializeEnemyScenario` 公开入口创建并设置显式 CurrentHealth，词条按 Run实例/类型配置取得。输出场景 canonical key、seed、每槽 CardNumber/Attack/MaxHealth/CurrentHealth/Keywords；失败时沿用现有原子清理 | 已有 `BattleStage` |

Continue 文件所有者同时给其正在扩展的 `BattleStageStartupData` 增加向后兼容 `WithScenario(BattleScenarioStartupData)`/只读 `Scenario`：所有既有构造和 `CreateDefault()` 得到 null，`CreateSnapshot()` 深拷贝；Engine 的 request key 纳入场景3+3槽与 seed 的 invariant canonical 内容，避免两个不同验收场景被 Coordinator 合并。默认继续按钮永远提交 null Scenario，不把验收 DTO 带入正常下一关。

## 7. 其他资产部分

### 7.1 涉及到的其他资产概览

不新增音频、视频、第三方包或字体文件。修改现有 TMP FontAsset 的字形图集，并新增一张普通文本 CSV；两者都走 Unity/项目公开导入与资源索引流程。

### 7.2 原有其他资产改动

| 资产路径 | 资产类型 | 当前用途 | 改动内容 |
| --- | --- | --- | --- |
| `Assets/Resources/Fonts/NotoSansSC-Dynamic SDF.asset` | TMP Dynamic FontAsset | Battle/Preparation 中文 | 关键词 Builder 只声明同一字体引用，不保存 FontAsset。两案 a～d 后由唯一串行整合者把“嘲讽远射爆裂冲锋”与 Continue 所需字符合并，经现有 TMP/Editor API一次补字并保存；不得由关键词执行代理改 `PreparationUiBuilderUtility` 或字体资产 |

## 8. 实现顺序建议

| 步骤/Todo | 实施内容 |
| --- | --- |
| 1 | 新增 `EBattleKeyword`、`BattleKeywordCsvData` 与 CSV，给 `BattleCardTypeCsvData` 增加 `InitialKeyword`，完成四行配置、类型1～6映射与 DataApi 解析；关键词代理不刷新共享资源索引。 |
| 2 | 新增独占 `BattleScenarioStartupData/BattleCardSlotStartupData` 强类型 DTO；扩展 `RunCardInstanceData`、融合快照与 `BattleCardRawComponent` 词条字段，保留兼容 `int Attack` 并增加同步 `AttackValue`，补本场增益、延迟生命/死亡提交和对称回收。 |
| 3 | 新增 `BattleKeywordRules` 的集合规范化、配置顺序显示和并集；扩展 `BattleRules` 的候选 mask、整数比例、相邻槽及无分配结算结构。 |
| 4 | 修改奖励/首次阵容词条初始化和 `TryFuse` 并集事务，保持 Batch 指纹、99 禁止素材、消耗/生成与 Revision 原子语义；输出融合词条日志。 |
| 5 | 修改 `BattleSystem` 为冲锋→嘲讽→远射→爆裂→反伤→延迟死亡/胜负的唯一结算链，并加入可隔离的结构化日志；保留行动游标与交替规则。 |
| 6 | 更新 Battle 及三个 Preparation 条目的 View/Controller，统一格式化词条并监听 AttackValue；空态、换绑和池回收必须清旧文本/监听。 |
| 7 | 更新四个一一对应 UiBuilder，只生成/修改四个独占 Prefab并做 Pre-UiInit；Preparation条目映射 key/path 不变，本阶段不调用 ExportPreloadedView、不补共享字体、不刷新 ResourcesDictionary、不导出 Preparation场景。 |
| 8 | 完成串行源码接入：关键词 DTO/卡牌公开初始化契约就绪后，仅由 Continue 执行方修改其独占 `BattleStageStartupData/BattleStages/HearthstoneGameEngine`，接入可选场景深拷贝、request key、空槽/显式攻血/CurrentHealth 与默认null兼容；两边都不改对方独占文件。此项必须在两案代码审查前完成。 |
| 9 | 新增独立 `BattleKeywordRulesTests.cs`，覆盖功能表、场景DTO、Attack兼容与RGR；完成关键词独占源码/Prefab的编译、目标 EditMode tests和静态检查。Continue侧测试覆盖其Stage接入；随后两案分别执行唯一一次代码审查。 |
| 10 | 两案 a～d 全部完成后进入唯一共享屏障：主代理指定单一整合者（默认持有Preparation页面导出的Continue执行方）创建K1～K4正式Entry资产，顺序执行必要Builder/PreLoad/Preparation Exporter、一次合并字体补字与资源索引刷新，保留双方Prefab/按钮/Sprite/CSV key；核对Battle/Preparation Connected Prefab、精确Resources路径、Console与等价导出。不手写Scene/Prefab/.asset YAML、索引或`.meta`。关键词执行代理不参与第二个写入通道。 |
| 11 | 同一唯一整合者/主代理串行同步 combat、battle UI、preparation UI、battle-card Art 与相关 Design 现状文档，只记录两案均已落地事实；执行代理不并发编辑共享现状文档。 |
| 12 | 由主代理按K1～K4及各子场景从专属正式Entry启动、通过生产WithScenario入口执行并保存流程日志，另做`ART-01`资产编排检查；逐项形成`ART/FUNC/RGR`结论，最后恢复Edit Mode且不修改专属Entry。 |

Todo 判定：步骤1～12一一对应。出现未知词条位、类型映射在UI/战斗各写一份、融合从99类型配置反推词条、重复词条叠层、None被显示/继承、伤害逐个写入导致提前死亡、远射免疫非反伤、爆裂跨空位、冲锋回写永久值、Attack/AttackValue生产写入不同步、默认Battle Startup行为改变、验收直接改Component/Entity、关键词代理写共享导出物、两个代理同时写Stage或共享资产、Controller拼静态UI、手写Unity导出产物或用Task制造异步中间态时，回到对应步骤整改。

**与“备战阶段继续下一关”并行实施的所有权边界**：

| 范围 | 关键词案独占 | 继续案独占 | 合并策略 |
| --- | --- | --- | --- |
| 战斗/融合数据与规则 | `BattleKeywordCsvData*`、`BattleCardTypeCsvData.cs/.csv`、`BattleCardRawComponent.cs`、`BattleRules.cs`、`BattleSystem.cs`、`RunStateSingletonRawComponent.cs` 中 `RunCardInstanceData`、`RunCardRules.cs`、新 `BattleScenarioStartupData.cs` DTO与关键词测试 | `HearthstoneGameEngine.cs`、`BattleStageStartupData.cs`、`BattleStages.cs`、`PreparationStages.cs` 及下一关/失败回退新类型；继续案不要修改关键词文件 | 关键词方先稳定 DTO/卡牌公开初始化契约并停止写入；Continue方再作为唯一所有者把可选Scenario接入其Startup/Stage/request key。关键词方不编辑BattleStages，Continue方不复制词条解析；串行接入在代码审查前完成 |
| Preparation 页面 | `PreparationCardItem/SlotItem/FusionSlotItem` 的 View、Controller、Builder、Prefab | `PreparationView.cs`、`PreparationController.cs`、`PreparationViewUiBuilder.cs`、`PreparationView.prefab`、`Preparation.unity`、`Preparation.asset`、继续按钮资产 | 动态条目和页面根完全分离；继续案不要顺带格式化条目，关键词案不碰页面根/按钮/场景 |
| Battle UI | `BattleCardItemView/Controller/UiBuilder/prefab` | 无 | 可与继续案独立开发 |
| 测试 | 新建 `BattleKeywordRulesTests.cs`，只写独占测试/独占Prefab检查 | Continue案使用自己的新测试或现有 `BattleRulesTests/RunCardRulesTests` 并验证Scenario接入 | 不并发编辑同一测试文件；Stage跨契约分别在两侧测试，最终串行跑全集 |
| Unity 生成物 | 关键词执行代理只产出四个独占卡牌Prefab，不写共享PreLoad/索引/字体/Preparation导出 | Continue执行代理只产出其独占页面/Sprite；在被指定为唯一整合者前也不覆盖关键词Prefab | `PreLoadUiData.asset`、`ResourcesDictionary.json`、TMP FontAsset、`Preparation.unity/.asset`、全部共享Builder/Exporter执行只在两案a～d后由一个整合者一次完成并保留双方结果；不存在“关键词先导出、Continue再覆盖”的第二通道 |
| 正式文档与验收入口 | 关键词专属Plan/Review/临时证据；不创建正式Entry资产 | Continue专属Plan/Review/临时证据；不改关键词Entry定义 | K1～K4专属Entry资产和combat/preparation/battle UI/Art/Design现状文档都在a～d后由同一串行整合者/主代理创建或合并，执行代理不并发编辑；不改现有融合/继续Entry |

按以上边界，两案的独占 C# 与独占 Prefab 可以并行；关键词DTO→Continue独占Stage接入是步骤c内的单向串行交接；PreLoad/ResourcesDictionary/共享字体/Preparation导出/专属Entry资产/现状文档和正式验收统一延后到两案a～d后的单一共享屏障。若Continue Plan占用关键词独占文件，进入步骤c前按“Continue不修改卡实例/词条，关键词不修改Stage调度”的原则切分；任何共享生成物都不得由第二代理重新生成覆盖。
