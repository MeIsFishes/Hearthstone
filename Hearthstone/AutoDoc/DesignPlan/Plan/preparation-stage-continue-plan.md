# 备战阶段继续下一关实施 Plan

> 后续变更：用户已取消“目标 Stage 加载失败时回滚并恢复备战阶段”的要求。本 Plan 中与事务式 Stage 调度、失败注入、回滚补偿和失败重试有关的内容均已失效；当前实现恢复为 `GameEngineBase` 普通 StageGroup 切换，现状以程序文档和代码为准。

## 1. 需求明确

### 1.1 需求对齐

**验收方式（策划案第 6.1 节，优先记录）**：只使用“流程log验收”。主代理通过 `GameStageEntryLauncher.Start(Assets/Resources/Editor/PreparationStageEntry.asset)` 进入正式 `RunStateStage + PreparationStage`，从本次启动时间隔离日志后，使用实际“出战/融合”页签 Button、卡池 ScrollRect、卡牌拖放和右上角继续 Button 完成玩家操作；成功推进必须调用生产 `HearthstoneGameEngine.TryEnterNextBattleStageGroup()`，不得由 Controller 直接创建 Stage、手动打开 Battle Scene 或建立 Play Mode 调度。稳定日志前缀使用 `[PreparationContinue]`，每次尝试记录 `AttemptId`、当前/目标关卡号、目标关卡配置行、点击前 3 个出战槽、4 个融合槽、持有数、Run/Fusion Revision、已应用奖励批次数、按钮状态、Stage 事务 prepare/commit/rollback 结果、目标 Battle 创建次数和实际玩家 Entity 槽位。预计 5 趟：A（两页签与卡池不同滚动位置的常驻状态）、B0（0 张阵容）、B1-C-settlement（1 张阵容、未确认融合素材、下一战终局及新奖励首次应用）、B3-D-success（3 张阵容并快速重复点击）、D-failure（以一次性 Editor-only LateLoadItem 令目标 Battle 失败、核对原备战状态并重试）。证据分别整理到 `AutoDoc/Temp/preparation-stage-continue-trip-a-flow-log.md`、`trip-b0-flow-log.md`、`trip-b1-c-settlement-flow-log.md`、`trip-b3-d-success-flow-log.md`、`trip-d-failure-flow-log.md`。

**美术资产验收覆盖（只做实际资产与编排检视，不以流程日志代替）**：

| 编号 | 主干 | 资产落点与编排位置 | 检视方式与通过证据 |
| --- | --- | --- | --- |
| `ART-01` | 是 | 新增 `PreparationContinueButtonIdle.png`；编入 `PreparationView.prefab/ContinueButton`，主文字“继续”和辅助文字“下一关”由 TMP 叠加 | 核对红色面板、暖金双层描边、切角轮廓、完整阴影和两级文字全部位于正式外框内；不得使用默认 Button、裸文字或现有页签/Fusion Button 的不匹配底框 |
| `ART-02` | 是 | 新增 `PreparationContinueButtonIdle/Highlighted/Pressed/Waiting.png` 四态同尺寸 Sprite；标准 Unity `Button` SpriteSwap 使用 | 逐态核对引用、尺寸、Pivot、文字位置不变；悬停有金边/亮度响应，按下有内压反馈，等待态明显降饱和且不可操作，四态不发生位置或尺寸跳动 |
| `ART-03` | 是 | `PreparationView.prefab` 页面根上的 `ContinueButton`，与 `BattleOperationRoot`、`FusionOperationRoot` 和 `CardPoolPanel` 为兄弟节点；Connected 页面实例位于 `Preparation.unity/EPreparationUiGroup.Main` | 核对右上锚点、两个页签下同一实例/同一 Rect、卡池 ScrollRect 外层级；不同滚动位置均不遮挡标题、奖励、战斗槽、融合槽或卡池，且不被滚动内容裁切 |

**程序功能验收覆盖（实际运行行为，与美术 case 分开）**：

| 编号 | 主干 | 功能落点 | 流程操作、日志与通过条件 |
| --- | --- | --- | --- |
| `FUNC-01` | 是 | 页面根 Continue Button、`PreparationController` 页签/滚动状态日志 | Trip A：在 Battle/Fusion 间往返并把池滚到顶部、中段、底部；每一步日志须记录同一 Button 实例仍 Active、`interactable=true`、右上锚点不变，当前关卡号、StageGroup 和创建次数不变 |
| `FUNC-02` | 是 | `TryEnterNextBattleStageGroup`、稀疏玩家阵容初始化 | Trip B0/B1-C-settlement/B3-D-success 分别以 `0/1/3` 个非空槽点击；每趟请求日志列出逐槽快照，事务成功并进入目标 Battle，不增加填满限制。0 槽允许创建空玩家 Entity 数组并按既有胜负规则结算，不在加载阶段报错 |
| `FUNC-03` | 是 | 不可变 `PreparationContinueTransactionSnapshot`、`BattleStageStartupData.BattleNumber`、Battle 初始化 | 三个成功趟次逐槽比较 Request 的提交阵容与目标 Battle `BattlePlayerEntity` 日志；非空槽编号/永久攻血完全一致，空槽保持 `Entity.Null`，无新增、丢失、补位或重复 |
| `FUNC-04` | 是 | Preparation session 的既有融合槽生命周期、事务快照和 Stage 卸载 | Trip B1-C-settlement：把 2 张素材放入融合槽但不点击融合，再点继续；Request 记录选择，成功事务的 Preparation Unload 清 Session，目标 Battle 只使用原 1 张出战卡；日志证明素材仍持有、无 99 产出、Run Revision 未因取消选择变化 |
| `FUNC-05` | 是 | Waiting 输入阻挡层、StageGroup Coordinator 和事务 AttemptId 去重 | Trip B3-D-success：同一帧/连续帧快速点击实际 Button；首击进入生产入口并置 Waiting，静态 `ContinueWaitingInputBlocker + UiEventListener` 随后接管输入，只记录 `DuplicateIgnored` 而不再次调用生产入口。日志必须恰有一个 `Request`，可有多个 `DuplicateIgnored`，同一 AttemptId 只产生一次事务提交、一次 Preparation 卸载、一次关卡号提交和一个目标 Battle 实例 |
| `FUNC-06` | 是 | BbxCommon 事务式 StageGroup、Editor-only 一次性 LateLoadItem 和 Continue state 回退 | Trip D-failure：不写 `RunStateSingletonRawComponent` 或 `PreparationSessionSingletonRawComponent`。在正式 Entry 启动后，通过只存在于 Editor 的 one-shot 注入 API把一次性 `ITransactionalStageLoad` 追加到目标 Battle LateLoad；它在正常 Battle runtime 已准备后抛出确定异常。随后点击实际 Continue Button，生产入口照常提交；事务须回滚目标 ECS/UI/Data等已完成项且不卸载原 Preparation，日志逐项证明关卡号、3 槽、4 融合槽、持有实例、奖励账本和 Revision 与点击前相同，Button 从 Waiting 恢复 Idle。验收后清空 hook，删除 OS/项目 Temp 下动态 C#、DLL、PDB 与附属文件，复查 `Assets/`、Git 和 Editor hook 均无注入残留，再次点击成功 |
| `FUNC-07` | 是 | `BattleProgressionCsvData`、Run progression、正式新奖励 Batch 和应用账本 | 继续前从 `DataApi` 精确取得目标 `BattleNumber=2` 配置 `battle-002-reward-001`，继续动作本身不应用奖励；目标 Battle 创建数和 `CurrentBattleNumber` 各只递增一次。Trip B1-C-settlement 继续运行至第 2 战终局并自动进入新 Preparation，日志须显示新 Batch 首次 `Applied`、恰好 5 张 `08/09/10/11/12`、账本数只增加 1；重复提交同 Batch 返回 `AlreadyApplied` 且不再发卡。Trip D-failure 的关卡号、创建数、账本数均不变 |

**关键旧功能回归**：

| 编号 | 主干 | 回归点与证据 |
| --- | --- | --- |
| `RGR-01` | 是 | Battle 终局仍由 `BattleResultPreparationStageListener` 只请求一次 Preparation；事务调度升级后，初始 Battle、Battle→Preparation、Preparation→Battle 与第 2 战终局→新 Preparation 均只保留 `RunStateStage` 一份，Stage/UiScene/session 加载卸载对称且 Console 无 Error |
| `RGR-02` | 是 | 既有 BatchId→canonical payload 账本、每批恰好 5 张、原子应用/幂等和融合消耗不变；第 2 战使用正式新 Batch，不复用当前 Batch。继续不修改账本、奖励实例或已完成 99，未确认融合选择只随 Preparation session 回收 |
| `RGR-03` | 是 | 3 个出战槽仍支持卡池→槽、槽→槽、占用替换与 0～3 个非空槽；Continue 只读取点击瞬间快照，不增加阵容规则或自动补卡 |
| `RGR-04` | 是 | 出战/融合页签、99 固定池、双 Mask 滚动、融合合计/Button、素材选择及四个融合槽保持现有布局与行为；新增 Continue 为页面根兄弟节点，不重建 Root 或拦截拖放 |
| `RGR-05` | 是 | 事务式 Stage 调度保留现有重复请求合并和“最新冲突请求串行”语义；prepare 不发布目标副作用，target commit 失败只回滚本次新增 Stage并恢复旧 Stage，旧卸载失败则目标保持唯一 Active、旧 Stage 永久隔离并完成其余 best-effort 清理，不发生双 Active 或关卡二次提交 |

确定边界：本篇只结束当前备战并进入使用现有 Battle 内容的下一关，不设计新敌人、随机奖励算法、战斗词条、融合规则、存档、确认弹窗或阵容填满限制。目标 Battle 缺少正式结算奖励输入就不能创建，因此新增最小表 `BattleProgressionCsvData`：每个 BattleNumber 唯一对应一个不可变、强类型、可追溯的五张结算奖励定义。当前只落第 2 关行 `2,battle-002-reward-001,08/09/10/11/12` 及精确永久攻血；继续入口只从 `DataApi` 查该行构造 `BattleStageStartupData`，不随机、不读取当前 session Batch、不复用旧 Batch，也不改变既有五张/原子/幂等语义。后续目标关卡无配置时在提交 Stage 事务前返回 `InvalidProgressionConfig` 并恢复 Button，不伪造兜底。

当前 BbxCommon `SetActiveGameStage` 会先卸载旧 Stage，目标 `LoadStage` 异常被记录后仍加入 Enabled 列表，无法满足 `FUNC-06`。本篇把它收敛为一个必要框架能力缺口，但不把完整 `LoadStage()` 错称为隔离 prepare：升级现有唯一调度器为显式 `Validate → Prepare → SuspendOld → CommitTargetHidden → PublishTarget → UnloadOld → Complete` 契约。Prepare 只建立不发布的资源/数据 overlay 与隐藏对象，不调用会写共享 ECS/奖励的业务 Load；CommitTargetHidden 期间旧 Stage 的 System、Listener 和输入被可恢复地暂停，目标 UI/Scene 根隐藏、System 不进更新组、Listener 不对外订阅，成功后才一次 Publish。目标失败按阶段账本和 item 补偿逆序回滚并 ResumeOld；旧 Stage 已开始 Unload 后失败不回滚到半销毁旧状态，目标保持唯一 Active，旧 Stage 永久隔离并 best-effort 执行剩余清理，结果标记 `CommittedWithCleanupErrors`。现有 `SetActiveGameStage` 继续委托同一调度实现，不保留第二套调度、兼容补丁或 Hearthstone 私有加载器；框架缺口、隔离限制、兼容性、每类副作用和回归证据必须进入正式 Review。

**与“怪物战斗词条与融合继承”并行实施的精确边界**：

| 所有权 | 独占文件/职责 | 禁止跨界 |
| --- | --- | --- |
| 本篇 Continue 执行方 | `Assets/Scripts/BbxCommon/GameFramework/GameEngineBase.cs`、`GameStage.cs` 及事务所需 Data/UI staging 内部；`HearthstoneGameEngine.cs`、`RunStateStages.cs`、`PreparationStages.cs`、`BattleStageStartupData.cs`、`BattleStages.cs`；新增 `BattleProgressionCsvData.cs/.csv`、Continue/Progression Component 与事务快照/结果；`PreparationView.cs`、`PreparationController.cs`、`PreparationViewUiBuilder.cs`、`PreparationUiBuilderUtility.cs`；Continue 四态 PNG、Preparation 页面 Prefab/编辑场景/导出 Asset | 不修改 `RunCardRules.cs`、`BattleRules.cs`、`BattleSystem.cs`、`BattleCardRawComponent.cs`、词条配置或卡牌词条展示 |
| 关键词案执行方 | 词条配置/集合、`RunStateSingletonRawComponent.cs` 的卡牌词条数据、`RunCardRules.cs` 融合继承、`BattleCardRawComponent.cs`、`BattleRules.cs`、`BattleSystem.cs`、Battle/Preparation 卡牌条目词条显示及其 Builder/Prefab | 不修改 Stage 调度、关卡号、Continue 状态、`PreparationView/Controller/ViewUiBuilder` 或 Continue Sprite；`BattleStages.cs` 保留给本篇，关键词初始化必须封装在卡牌数据/初始化公开入口中 |
| 串行资产整合屏障 | 两边源码与各自 Prefab 源完成后，关键词执行方停止 Unity 写操作并提交资产事实清单；由本篇 Continue 执行方作为唯一整合方，统一运行 Preparation View Builder、`PreparationUiSceneBuilder` 和 Exporter，并保存 `Preparation.unity`、`Preparation.asset`、`PreLoadUiData.asset`、`NotoSansSC-Dynamic SDF.asset`、`ResourcesDictionary.json`。若关键词案改变 PreparationCardItem/BattleCardItem，先完成其独占 Prefab并交清单再进入屏障 | 两个代理不得同时执行 Unity 导入/Builder/Exporter或并发保存上述共享生成资产；不得手工合并 YAML/.asset/.meta |
| 串行现状文档屏障 | 两个执行方只提交各自已验证事实清单，不在并行开发期编辑共享现状文档；由本篇 Continue 执行方作为唯一文档整合方，在代码/资产验收事实稳定后一次维护下列精确路径 | 关键词执行方不得自行编辑这些共享文档；整合方不得写入尚未落地或仅来自策划案的预期 |

串行现状文档的唯一清单为：`AutoDoc/Program/Specific/preparation-card-pool/preparation-card-pool.md`、`AutoDoc/Program/UI/preparation/preparation.md`、`AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`、`AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md`、`AutoDoc/Art/UI/ui-art-overview.md`、`AutoDoc/Program/Specific/combat-system/combat-system.md`、`AutoDoc/Program/UI/battle/battle.md`、`AutoDoc/Design/Specific/combat-system/combat-system.md`、`AutoDoc/Art/Modules/battle-card/battle-card.md`。关键词执行方的事实清单至少包含词条数据源、战斗结算顺序、融合继承、0/1/4 词条 UI 编排和实际资产路径；Continue 执行方的事实清单至少包含关卡配置、事务边界、失败回退、稀疏阵容、继续 UI 和实际资产路径。唯一整合方逐条通过代码/配置/资产/验证证据核验后再写。

按此边界，两个案子的纯源码与独占 Prefab/美术可以并行；共享 Unity 导入、Preparation 导出与上述现状文档必须各串行一次。若关键词实现确实需要改变 `BattleStages.cs` 的调用点，只由本篇执行方在整合屏障按其已落地公开初始化契约接一行调用，关键词方不直接编辑该文件。

## 2. 数据部分

### 2.1 涉及到的数据概览

| 数据 | 唯一权威来源 | 产生者 | 消费者 | 生命周期 |
| --- | --- | --- | --- | --- |
| 当前已成功进入的关卡号、成功创建 Battle 次数 | `RunProgressionSingletonRawComponent` | 初始/继续 Battle StageGroup 事务成功回调 | Continue 入口、流程日志、后续关卡输入 | 整局 `RunStateStage`；失败不写入 |
| Continue Button 状态 | `PreparationContinueSingletonRawComponent.State` | 页面点击置 Waiting；Stage 事务失败置 Idle；成功随 Preparation 卸载回收 | `PreparationController` ModelWrapper 监听 | 当前 `PreparationStage` |
| 单次继续事务输入 | `PreparationContinueTransactionSnapshot` 不可变值对象 | `TryEnterNextBattleStageGroup` 在首个有效点击瞬间深拷贝 | StageGroup 请求、成功/失败日志 | 单 Attempt；完成后释放，不回写 UI 或 StartupData |
| 当前出战阵容与持有卡 | 既有 `RunStateSingletonRawComponent` | 既有编成/奖励/融合规则 | Continue 快照、Battle 初始化 | 整局；本篇只读，成功/失败均不改卡牌状态 |
| 未确认融合槽 | 既有 `PreparationSessionSingletonRawComponent` | 既有融合拖放 | Continue 快照、Preparation Unload | 当前备战；成功离场回收，失败保持 |
| 下一关及其结算奖励定义 | `BattleProgressionCsvData` | 正式 `GameEngineDefault` CSV | Continue 业务入口、StartupData 构造 | 静态配置；按 BattleNumber 唯一 |
| 目标 Battle 输入 | `BattleStageStartupData` 防御性快照 | Continue 业务入口从 `BattleProgressionCsvData` 构造 | Battle Stage 工厂/初始化 | 单个目标 Battle Stage |

事务快照固定包含 `AttemptId`、`FromBattleNumber`、`TargetBattleNumber`、目标配置的 BatchId/canonical payload、3 槽、4 融合槽、持有数、Run/Fusion Revision、奖励账本数和 Stage 创建计数。它只用于保证点击语义与证据一致，不成为第二份可变运行状态。

### 2.2 新增数据列表

#### 2.2.1 新增Component类

| 类名 | 重要字段 | 归属哪种Entity |
| --- | --- | --- |
| `RunProgressionSingletonRawComponent` | `CurrentBattleNumber`、`BattleStageCreationCount`、`Revision`；只在 Battle StageGroup 事务成功后由 0→1 或 n→n+1 原子提交 | Run state 默认单例 Entity / `RunStateStage` |
| `PreparationContinueSingletonRawComponent` | `ListenableVariable<EPreparationContinueState> State`，枚举只有 `Idle`、`Waiting`；回收时先 `MakeInvalid()` 再复位 | Preparation session 默认单例 Entity / `PreparationStage` |

#### 2.2.2 新增CsvData类

| 类名 | 重要字段 |
| --- | --- |
| `BattleProgressionCsvData` | `BattleNumber`（DataApi 唯一 int key）、`SettlementRewardBatchId`、固定长度 5 的 `RewardCardNumbers`、`RewardAttacks`、`RewardMaxHealths`；`ReadLine()` 校验正关卡号、非空 BatchId、三个数组均恰好 5 项、卡号互异且 `1~98`、攻击非负、生命为正，再登记到 `GameEngineDefault` |

正式表路径为 `Assets/Resources/Config/BattleProgressionCsvData.csv`，表头为 `BattleNumber,SettlementRewardBatchId,RewardCardNumbers,RewardAttacks,RewardMaxHealths`，数组使用分号。当前唯一数据行为 `2,battle-002-reward-001,8;9;10;11;12,3;2;3;4;4,4;5;6;7;2`：分别对应 08 野猪 `3/4`、09 哥布林战士 `2/5`、10 哥布林战士 `3/6`、11 哥布林战士 `4/7`、12 哥布林投弹手 `4/2`，均使用明确永久值且引用真实 `BattleCardCsvData`。表的 Associated 为 `BattleCardCsvData`，并同步该表反向关联注释；不提供缺行兜底或随机生成。

新增普通值类型 `EPreparationContinueResult`（`Accepted`、`DuplicateIgnored`、`InvalidStage`、`InvalidRuntimeState`、`InvalidProgressionConfig`、`TargetLoadFailed`、`Committed`、`CommittedWithCleanupErrors`）、`PreparationContinueTransactionSnapshot` 和框架层 `GameStageTransitionResult`。结果类型只表达稳定结果和诊断字段，不保存权威玩法状态。

## 3. 游戏逻辑部分

### 3.1 涉及到的游戏逻辑概览

继续是离散 UI 事件，不新增每帧 System 或 StageListener。首个有效点击由 `HearthstoneGameEngine.TryEnterNextBattleStageGroup()` 完成全部业务边界检查和深拷贝：确认当前 Active Group 为 Preparation、Continue state 为 Idle、Run/Preparation/Progression 三个单例存在，读取 0～3 个槽但不要求填满，以 `TargetBattleNumber = CurrentBattleNumber + 1` 从 `DataApi` 取得唯一 `BattleProgressionCsvData`，完整验证五张配置引用后构造新 `PreparationRewardBatchStartupData` 与 `BattleStageStartupData`。Button 置 Waiting 后只提交一次事务；配置缺失/非法在 Stage 请求前返回 `InvalidProgressionConfig` 并恢复 Idle。当前 session Batch不参与目标输入。Waiting 时静态输入阻挡层只记 `DuplicateIgnored`，不会第二次调用生产入口；生产入口仍防御性拒绝非 Idle/重复 Attempt。

框架事务不以完整 `LoadStage()` 充当 prepare，固定阶段如下：

1. `Validate`：冻结目标 Stage 集合与 AttemptId，验证无 null/重复、所有 item 都声明可事务化策略，建立旧 Active 快照和目标阶段账本；任何业务副作用前失败。
2. `Prepare`：只准备隔离资源。Scene 异步加载到 staging 且根对象保持非激活；UiScene 在 inactive staging root 下实例化；DataGroup 解析到不发布给 `DataApi` 的 overlay；System 只创建且禁用、不加入 UpdateGroup；Listener 只构造不订阅；LoadItem/LateLoadItem 只执行无共享写入的 `Prepare`。Preparation/Battle 项的奖励、ECS 单例和 Entity 此时均不得创建。
3. `SuspendOld`：旧 Stage 仍保留全部状态和可见 UI，但其输入、System 与 Listener 暂停，防止在目标隐藏提交期间观察目标 ECS/Data；记录可逆 suspend 账本。失败时按账本 `ResumeOld`。
4. `CommitTargetHidden`：按原加载依赖顺序提交 LoadItem、Scene、UI、Data、System、Listener、LateLoad，但目标 Scene/UI 根保持隐藏、System 不开始 tick、Listener 的外部事件派发保持门控。每个 item 成功后登记补偿；单个 item 若在返回前抛错，必须自行清理自己的部分副作用。`InitializePreparationRuntime` 在应用奖励前捕获 RunState 全量受影响快照并登记补偿，后续阶段失败时精确恢复 CardInstances、3 槽、payload 账本和 Revision；`InitializeBattleRuntime` 只创建目标所有的 session/Entity并登记成对销毁，不写奖励或现有 RunState。
5. `PublishTarget`：所有隐藏提交成功后，一次发布 Data overlay、启用目标 Scene/UI 根、System 和 Listener 门控，并把目标设为唯一待提交集合；发布中任一步失败仍回滚目标、撤销已发布数据/可见性并 ResumeOld。
6. `UnloadOld`：目标已成功发布、旧 Stage 仍处于 suspend；按原卸载逆序 best-effort 执行旧 LateLoad/Listener/System/Data/UI/Scene/LoadItem，逐项继续并聚合异常。此阶段开始后不尝试恢复可能半销毁的旧 Stage；即使某项失败，目标仍为唯一 Active，旧 UI/input/System/Listener 永不恢复，残留资源进入隔离清理队列，结果为 `CommittedWithCleanupErrors`。
7. `Complete`：成功或带旧清理错误时只提交一次 `CurrentBattleNumber/BattleStageCreationCount`；目标提交前失败则回滚目标、ResumeOld、Progression 不变且 Continue state恢复 Idle。完成回调严格校验 AttemptId，过期回调不得写状态。

各类 Stage item 的部分执行与隔离契约如下：

| 类别 | Prepare 隔离 | Commit/Publish | 目标失败清理或补偿 |
| --- | --- | --- | --- |
| LoadItem | 只允许 `ITransactionalStageLoad.Prepare(context)` 做结构校验/预留；旧 `IStageLoad` 必须由适配器声明“无共享写入且 Load 自身强异常安全”，否则 Validate 拒绝严格事务 | Hidden commit 中调用 Load/Commit；成功返回后才入 completed 栈 | completed 项逆序 Unload/Rollback；当前抛错项负责清自己未完成副作用；奖励/RunState写入必须登记值级补偿 |
| Scene | 异步加载到 staging scene，根对象保持 inactive；不把 scene 设 active | Publish 才激活根并切 active scene（如需要） | 已加载 scene 异步卸载并等待完成；任何激活门控/原 active scene 精确恢复 |
| UiScene | 用现有 UiScene 配置在 inactive staging root 下创建，Controller 不 Open/Show | Publish 才执行 Ui 生命周期 Open/Show并接入正式 Group root | 只销毁本事务实例，恢复旧 Group 可见/输入状态；不得复用或销毁旧页面 |
| DataGroup | CSV/SO 解析到 `StageDataOverlay`，键冲突和引用在隔离字典验证；不调用全局 `DataApi.SetData` | Publish 原子写入 overlay并记录每个键原值/所有者计数 | 逆序恢复原值/所有者计数并卸载本事务 runtime SO；不卸载旧/引擎组仍持有的数据 |
| System | 类型实例创建后 disabled 且不加入 Update/Fixed group | Publish 按 RegisterSystemOrder 加入并 enable | 若已加入则移除、disable并销毁目标实例；旧组排序按快照恢复 |
| Listener | 只构造；不执行 `OnLoad`、不订阅 | Hidden commit 初始化内部依赖但事件出口门控，Publish 才解除门控 | 对 completed listener 调 `OnUnload` 并清订阅；当前 `OnLoad` 抛错者须自身强异常安全 |
| LateLoadItem | 与 LoadItem 相同，但只在前述目标项 hidden commit 完成后执行 | 成功后才允许 Publish | completed 项优先逆序 Rollback/Unload；用于 FUNC-06 的 Editor-only item 在此确定抛错，验证前序 Battle runtime/UI/Data 全部回滚 |

本项目当前 Battle/Preparation 没有业务 Scene 或独立 DataGroup，但框架测试仍逐类覆盖；正式配置继续由常驻 `GameEngineDefault` 提供。任何现有 Scene/UiScene/LoadItem 无法满足上述强异常安全或隔离协议时，Validate 明确拒绝严格事务，不静默退回旧“先卸载后加载”路线。

### 3.2 原有逻辑类改动

| 类名/文件 | 改动方向 |
| --- | --- |
| `BbxCommon.GameStage` | 把单体 `LoadStage` 内部拆成上述 Validate/Prepare/HiddenCommit/Publish/Rollback/Suspend/Resume/BestEffortUnload 阶段；为每类 item 记录 prepared/completed/published 数和补偿栈，只释放实际完成项。新增 `ITransactionalStageLoad`/事务 Context；旧 `IStageLoad` 经同一调度适配且不满足强异常安全时在 Validate 拒绝。正常 Unload 与失败 Rollback 复用同一项级释放原语，不维护平行生命周期 |
| `GameEngineBase<TEngine>` / `EngineStageWp` | 把现有 operation batch升级为唯一事务协调器：维护旧 Active 快照、目标 staging 集合、AttemptId、隔离 overlay、暂停/发布/清理状态；目标失败回滚并恢复旧集合，旧卸载失败采用目标已提交+旧永久隔离的明确策略。公开 `GameStageTransitionResult`（含失败阶段、原异常、回滚/旧清理异常）；原 `SetActiveGameStage` 委托同一实现。Loading UI、LoadingTimeData 与 `OnStageLoadingCompleted` 兼容保留 |
| `HearthstoneStageGroupTransitionCoordinator` | Request key 对 Battle 加入 `BattleNumber`；增加 `FailTransition`，失败时清正在加载项并恢复到旧 Active 请求，使相同 Target 可重试。Complete/Fail 都校验 group、key、AttemptId，忽略/拒绝过期回调 |
| `BattleProgressionCsvData` | 从固定数组字段构造防御性 `PreparationRewardBatchStartupData`；创建时再次从 `DataApi` 验证 5 个 `BattleCardCsvData/BattleCardTypeCsvData` 引用和永久值，避免只信 CSV 解析。缺 BattleNumber、重复 BatchId、卡已持有冲突或引用非法均在 Stage 请求前给出精确失败，不随机回退 |
| `HearthstoneGameEngine` | 新增唯一生产入口 `TryEnterNextBattleStageGroup()`；从目标 BattleNumber 的正式 CSV 捕获 Continue 快照、提交事务、输出 Request/Duplicate/Prepared/Published/RolledBack/Committed/CleanupError 日志。成功或旧清理错误时递增 Progression 一次，目标失败时恢复 Button；现有 Initial Battle 与自动 Battle→Preparation 继续走同一事务调度。`#if UNITY_EDITOR` 的 one-shot EditorOperation 只允许给下一目标 Stage追加一个事务 LoadItem，消费后立即清空，不进入 Player |
| `BattleStageStartupData` | 新增正整数 `BattleNumber`，构造/快照包含它；`CreateDefault()` 明确为第 1 关。Continue 只用 `BattleProgressionCsvData.CreateRewardBatchSnapshot()` 构造第 n+1 关，不把可变 Component、旧 Batch 或 Editor Asset 传入 Stage |
| `BattleStages.InitializeBattleRuntime` | 玩家 3 槽中 `0` 直接保留 `Entity.Null`，非零槽仍必须为已持有有效实例；创建成功日志包含 BattleNumber、槽位、CardNumber/永久攻血。0 卡 Battle 由现有 `BuildAliveMask/EvaluateResult` 正常判负，不增加特殊胜负分支 |
| `PreparationStages.InitializePreparationRuntime` | 改为事务 item并提供 RunState 奖励写入补偿；第 2 战终局进入的新 Preparation 使用 startup 中 `battle-002-reward-001` 首次应用，成功 Publish后丢弃补偿快照，正常退出不撤销已经提交的 5 张奖励 |
| `PreparationController` | Idle 下 Button `onClick` 只调用一次 Engine 生产入口；Waiting 时禁用 Button并激活静态 `ContinueWaitingInputBlocker`，其 `UiEventListener` 只记 `DuplicateIgnored`，不调用 Engine。Continue state 监听负责 Sprite/阻挡层；页签/滚动后记录常驻状态；不在 Controller 创建 Stage、拼装静态 Button 或复制阵容规则 |

框架定向测试必须逐项覆盖：Prepare 结束时目标 UI/Scene不可见、DataApi 无目标键、ECS/奖励无写入、System 不 tick、Listener 无订阅；LoadItem/Scene/UI/Data/System/Listener/LateLoad 在首项/中间项/末项失败时只清已完成项且旧 Stage Resume 后行为/输入/数据一致；Preparation reward 已写后再让后续 item 失败，RunState 卡实例/3 槽/账本/Revision 精确补偿；目标 Publish 部分失败撤销可见性与 overlay；当前抛错 item 的自清理失败与框架回滚失败均保留原异常；旧 Stage Unload 首项/中间项失败仍继续其余清理、目标保持唯一 Active且 Progression只提交一次；事务期间重复请求不创建第二 Stage；初始重复请求合并、最新冲突请求串行、完成回调恰好一次。项目测试覆盖正式第 2 关配置及缺行拒绝、0/1/3 槽、稀疏 Entity 映射、Waiting 阻挡层“一个 Request+多个 DuplicateIgnored”、成功单次 progression、Editor LateLoad失败不写 Run/Session、失败重试，以及第 2 战终局后新 Batch首次 Applied/再次 AlreadyApplied。

## 4. UI部分

### 4.1 涉及到的UI部分概览

只修改唯一 `PreparationView.prefab`。Continue Button 是页面根直属静态节点，右上锚定在卡池 ScrollRect 外；RewardPanel 移到左上与其对称，中央 Title/Tab 和两个 Operation Root 保持现有职责。Button 的主/辅助 TMP、Image、标准 Button、四态 Sprite及同 Rect 的 Waiting 输入阻挡层全部保存在 Prefab；阻挡层 Idle 时 inactive，Waiting 时位于禁用 Button 上方并通过 `UiEventListener` 只记录重复输入。Controller 不运行时创建外框、文字或阻挡对象。

### 4.2 原有Ui/Hud改动

| View类名 | 对应页面 | 新增或删除控件 |
| --- | --- | --- |
| `PreparationView` | 备战页面 | 新增 `ContinueButton`、目标 `Image`、主文字 TMP、辅助文字 TMP、`ContinueWaitingInputBlocker` 与其 `UiEventListener`；现有 RewardPanel 从右上调整到左上，Battle/Fusion Root、共享卡池和动态条目引用不变 |

| Controller类名 | 数据监听改动 |
| --- | --- |
| `PreparationController` | 新增 Open 生命周期的 `PreparationContinueSingletonRawComponent.State` ModelWrapper 监听；Idle 显示可用 Button并隐藏阻挡层，Waiting 显示 Waiting Sprite/禁用 Button/启用阻挡层。Button首击调用 Engine；阻挡层后续点击只记 `DuplicateIgnored`。已有 Run/Fusion Revision 与页签刷新保持；ScrollRect 值变化只输出可节流的结构化常驻状态日志，不写玩法数据 |

`PreparationViewUiBuilder.Build()` 增加一一对应的 Continue 静态层级、Waiting 阻挡层并给 Reward/Continue 明确镜像 Rect；标准 Button 使用 SpriteSwap：Normal=Idle、Highlighted=Highlighted、Pressed=Pressed、Disabled=Waiting。阻挡层没有可见图，只以透明 raycast Image + `UiEventListener` 捕获 Waiting 重复点击。所有 TMP 使用现有 Noto Sans SC FontAsset，主/辅助文字始终在同一框内。

### 4.3 UiScene配置与导出

#### 4.3.1 原有UiScene改动

| UI编辑场景路径 | 修改的Group或Prefab归属 | 导出Asset路径 | 需要重新导出的原因 | 受影响GameStage |
| --- | --- | --- | --- | --- |
| `Assets/Scenes/Ui/Preparation.unity` | `EPreparationUiGroup.Main` 下仍只有 Connected `PreparationView.prefab`；核对新 Continue 层级、右上位置和 Reward 左移 | `Assets/Resources/Ui/Preparation.asset` | 页面 Prefab与位置实质变化后从活动编辑场景重导，确保 Player 流程取得相同页面源 | 已有 `PreparationStage` |

#### 4.3.2 UiScene完整性检查

| 环节 | 完成标准 |
| --- | --- |
| Prefab/Builder | `PreparationViewUiBuilder` 可重复生成同一 Prefab；Continue Image/Button/TMP/StateSprite/WaitingBlocker/EventListener 引用完整，阻挡层只在 Waiting raycast，两个 Operation Root 初始化和默认 Battle 页行为不倒退 |
| 编辑场景 | Canvas/CanvasScaler、唯一 Exporter、`Hearthstone.EPreparationUiGroup` 和 Main Group 不变；页面实例为 Connected Prefab、场景无脏状态 |
| 导出/运行时 | 从活动 `Preparation.unity` 调用正式 `UiSceneExporter` 重导 `Preparation.asset`；精确 `Resources.Load("Ui/Preparation")` 和四个 Continue Sprite 均非空 |
| 共享资产屏障 | 关键词独占 Prefab 已落地后再统一执行 Preparation Builder/Exporter；Noto 字体、资源索引和导出 Asset只串行保存一次，不覆盖另一案引用 |
| 禁止项 | 不手写 Scene、Prefab、`.asset`、`.meta` 或资源索引；不直接改 `UiObjectDatas`，不以 Controller/MonoBehaviour 运行时拼 Button |

## 5. 美术部分

### 5.1 涉及到的美术表现概览

沿用 `UI-STYLE-001` 的红、蓝、暖金奇幻卡牌语言。页面背景、标题底框、奖励面板、双页签、战斗/融合操作区和卡池资产全部直接复用；现有 Fusion Button 是金色融合语义且只有禁用/可用/按下三态，Tab 是浅横向页签语义且只有两态，均不满足红色完整外框、四态和右上主行动按钮要求，因此新增专用无文字四态 PNG，文字继续由 TMP 叠加。策划原型只作构图与色彩参考，不作为运行时资产。

### 5.2 美术资产完整性检查

| 资产或资产组 | 用途 | 候选已有资产及路径 | 复用结论 | 判断依据 | 缺失或不满足需求的内容 | 处理方式 |
| --- | --- | --- | --- | --- | --- | --- |
| 页面/标题/卡池骨架 | Continue 所在整体构图 | `PreparationPageBackground.png`、`PreparationStageTitleFrame.png`、`PreparationCardPoolPanel.png` | 直接复用 | 风格、分区和 16:9 规格已符合备战页面；新增按钮不要求改底图 | 无 | Builder 保持原引用，只调整 Reward/Continue 静态 Rect |
| 红色主行动外框 | Continue Idle | `PreparationTabIdle/Selected.png`、`PreparationFusionButton*.png` | 无法复用 | Tab 比例/语义不匹配；Fusion Button 为金色且缺独立 Hover，均不具备图 1 的红色面板、双层暖金切角大框 | Idle 外框缺失 | 新增 Continue Idle 正式透明 PNG |
| Continue 四态组 | Normal/Hover/Pressed/Waiting | 现有无四态同尺寸候选 | 无法复用 | 任一现有组都缺状态或语义；简单运行时着色无法补出按压结构和等待品质 | Highlighted/Pressed/Waiting 缺失 | 新增四张同尺寸资产并由 Button SpriteSwap 使用 |
| 主/辅助文字 | “继续”“下一关” | `NotoSansSC-Dynamic SDF.asset` | 直接复用并补字形 | 与备战全部 TMP 字体一致，文字不应写入位图 | “继、续、下、一、关”等字形需确认 | 通过 TMP 正式 API补字并保存，不替换字体 |

### 5.3 新增美术资产

| 资产名或资产组 | 资产类型 | 用途 | 规格要求 | 预期路径 |
| --- | --- | --- | --- | --- |
| `PreparationContinueButtonIdle` | 透明 PNG Sprite | 常态完整外框 | `1024×420`、约 2.44:1；深红渐变面板、暖金双层描边、四角切角和向下阴影，中央无文字、透明外缘完整 | `Assets/Resources/Art/Preparation/UI/PreparationContinueButtonIdle.png` |
| `PreparationContinueButtonHighlighted` | 透明 PNG Sprite | Hover | 与 Idle 完全同画布/轮廓/Pivot；金边和内面适度提亮并增加高光，不放大轮廓 | 同目录同名 `.png` |
| `PreparationContinueButtonPressed` | 透明 PNG Sprite | 按下反馈 | 同尺寸；内面与高光下压、阴影收短，外轮廓位置不变，不含文字 | 同目录同名 `.png` |
| `PreparationContinueButtonWaiting` | 透明 PNG Sprite | 已接收点击、等待转场 | 同尺寸；降低红色饱和与金边亮度，仍保持完整正式外框和文字可读背景，视觉上明确不可重复点击 | 同目录同名 `.png` |

四张图片统一导入为 Single Sprite、Alpha Is Transparency、无 Mipmap、Clamp；Resource key 唯一，禁止在图内烘焙“继续/下一关”。

## 6. GameStage部分

本篇复用已有 `RunStateStage + PreparationStage` 与 `RunStateStage + BattleStage` 两组，不新增 GameStage、Unity Scene、业务 DataGroup、System、StageListener 或 UiScene。`BattleProgressionCsvData` 加入已有常驻 `GameEngineDefault`，保证 Continue 请求前已可由 `DataApi` 查询。改动集中在现有 Group 事务边界与三个早期 LoadItem；它们改为强异常安全的事务 item。

### 6.1 修改LoadItem和LateLoadItem项

| LoadItem项名 | 负责内容 | 所属GameStage |
| --- | --- | --- |
| `InitializeRunStateRuntime` | Prepare 只验证单例均不存在；Hidden Commit 创建 RunState 后创建 RunProgression，每次成功立即登记补偿，当前项内部失败先自清理；Rollback/Unload 逆序移除 Progression、RunState | 已有 `RunStateStage` |
| `InitializePreparationRuntime` | Prepare 验证正式 reward 配置引用并捕获待写范围；Hidden Commit 在应用奖励前深拷贝 RunState 卡数组、3 槽、payload 账本和 Revision，应用后登记值级补偿，再创建 Preparation/Continue session并逐项登记销毁。后续 UI/Listener/LateLoad失败时 Rollback 精确恢复奖励写入；正常 Unload 只移除 Continue/session，绝不回滚已提交奖励 | 已有 `PreparationStage` |
| `InitializeBattleRuntime` | Prepare 验证带 BattleNumber 的 StartupData和 0～3 个非空槽引用，不创建 Entity；Hidden Commit 创建 Battle session、稀疏玩家 Entity和完整敌方 Entity，每个成功创建立即入补偿栈。当前项失败先销毁自身部分结果，事务后续失败由 Rollback 对称销毁全部目标结果；不写 RunState/奖励 | 已有 `BattleStage` |

Group 入口完整集合保持 `RunStateStage + BattleStage` 或 `RunStateStage + PreparationStage`。Prepare/HiddenCommit 期间旧集合仍是公开 Active，目标只在 staging ledger 中；Publish 后目标成为唯一公开集合，旧集合保持 suspended 直至 best-effort Unload结束。目标失败不产生“Preparation 和半加载 Battle 同时 Active”的中间状态；旧卸载失败也不恢复半销毁旧 Stage。

## 7. 其他资产部分

### 7.1 涉及到的其他资产概览

不新增音频、视频、第三方包或字体文件；只扩充既有动态 TMP FontAsset 的 Continue 文本字形。

### 7.2 其他资产完整性检查

| 资产或资产组 | 资产类型 | 用途 | 候选已有资产及路径 | 复用结论 | 来源与授权 | 缺失或不满足需求的内容 | 处理方式 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 中文 UI 字体 | TMP Dynamic FontAsset | “继续”“下一关”和既有备战文本 | `Assets/Resources/Fonts/NotoSansSC-Dynamic SDF.asset` | 直接复用并扩充动态图集 | 项目既有字体资产与授权不变 | 确认“继、续、下、一、关”等字形；与关键词案新增词条字形需合并检查 | `PreparationUiBuilderUtility.RequiredChineseCharacters` 合并两案字符后，通过 TMP API 一次补入并保存 |

### 7.3 原有其他资产改动

| 资产路径 | 资产类型 | 当前用途 | 改动内容 |
| --- | --- | --- | --- |
| `Assets/Resources/Fonts/NotoSansSC-Dynamic SDF.asset` | TMP FontAsset | Preparation/Battle 中文与数字 | 在共享资产屏障中一次补齐 Continue 与关键词两案所需字形；全部修改 Prefab 继续引用同一资产，不手写 Font Asset |

## 8. 实现顺序建议

| 步骤/Todo | 实施内容 |
| --- | --- |
| 1 | 新增两个 singleton Component、Continue 枚举/快照/结果；新增 `BattleProgressionCsvData.cs/.csv` 与精确第 2 关行，登记 `GameEngineDefault`、双向 Associated 和资源 key，测试五张/唯一/字段范围/缺行/非法引用，不复用当前 Batch。 |
| 2 | 在 BbxCommon `GameStage`、Data/UI staging 与 `GameEngineBase` 实现唯一 Validate→Prepare→SuspendOld→HiddenCommit→Publish→UnloadOld→Complete 调度；逐类建立阶段账本、overlay、补偿、可见性门控和旧卸载失败隔离策略，保留原 API委托。 |
| 3 | 把 `InitializeRunStateRuntime`、`InitializePreparationRuntime`、`InitializeBattleRuntime` 改为事务 item；重点实现奖励写入前完整值快照/后续失败补偿、目标 ECS逐项销毁和正常 Unload不误回滚已提交奖励。 |
| 4 | 扩展 `BattleStageStartupData.BattleNumber`、Battle request key、Coordinator Complete/Fail/AttemptId；实现 `TryEnterNextBattleStageGroup()` 从正式 CSV 构造第 n+1 关、首击快照、单次 progression、失败恢复、Editor-only one-shot LoadItem入口和 `[PreparationContinue]` 分阶段日志。 |
| 5 | 修改 Battle 初始化支持 0～3 个稀疏玩家槽，保留非零槽持有校验和既有 BattleRules；新增 Continue 专用测试覆盖 0/1/3、逐槽映射、一个 Request+多个 DuplicateIgnored、正式第 2 关新 Batch、成功/失败/旧清理错误和重试，不编辑关键词案独占规则文件。 |
| 6 | 修改 `PreparationView` / `PreparationController`，接入 Continue state ModelWrapper、实际 Button首击、静态 Waiting 输入阻挡层、SpriteSwap、页签/滚动常驻日志；不改变已有编成/融合入口。 |
| 7 | 生成第 5.3 节 Continue 四态正式透明 PNG，核对无文字、画布/轮廓/Pivot 一致和 Resource key 唯一。 |
| 8 | 修改 `PreparationViewUiBuilder` 和 `PreparationUiBuilderUtility`，创建页面根 Continue/WaitingBlocker 静态层级、左移 Reward、合并所需字形；通过 Unity Editor执行 Build并检查 Prefab 引用、锚点、状态组和两页签 Root。 |
| 9 | 等待关键词案独占卡牌 Prefab/源码完成后，由本篇唯一整合方进入共享资产屏障：串行运行相关 Builder，打开 `Preparation.unity` 核对 Connected 页面并通过正式 Exporter 重导 `Preparation.asset`，一次保存字体/索引/预载等共享生成资产；不手写 Unity YAML/.meta。 |
| 10 | 收取两个执行方的事实清单，由本篇唯一文档整合方逐路径维护并行边界中列出的 9 份共享现状文档；逐条以代码、配置、实际资产和验证证据核对，只写已落地事实。 |
| 11 | 完成编译、逐类 BbxCommon 事务/补偿/旧卸载失败测试、Continue 定向 EditMode tests、Prefab/Scene/Exporter/Resources/Console 检查；主代理按 Trip A、B0、B1-C-settlement、B3-D-success、D-failure 从正式入口执行流程log验收。D-failure 通过 Editor-only one-shot LateLoadItem触发，不写 Run/Session；验收后清 hook并删除动态 C#/DLL/PDB/附属文件，复编译、成功重试并核对 Assets/Git/Temp 零注入遗留。正式 Review 分别记录 `ART-01`～`ART-03`、`FUNC-01`～`FUNC-07`、`RGR-01`～`RGR-05`、框架缺口/隔离契约/补偿与旧清理异常策略。 |

Todo 判定：步骤 1～11 与本表一一对应。发现目标关卡复用当前 Batch、缺配置时随机/静默兜底、把完整 LoadStage 当 prepare、Prepare 发布 Data/ECS/UI/System/Listener、副作用当前项无自清理、奖励写入无补偿、目标失败后旧 Stage 未 Resume、Publish 部分失败残留、旧 Unload 失败后恢复半销毁旧 Stage或形成双 Active、保留第二套 Stage 调度、失败后相同目标不可重试、Waiting 点击产生第二个 Request、关卡号提前/重复提交、0 槽被拒绝或自动补卡、Controller 创建 Stage/静态 Button、FUNC-06 写 Run/Session或遗留注入脚本、Button 位于 Operation Root/ScrollRect 内、四态缺失、两个代理并发保存共享 Unity 资产/编辑共享现状文档或 UiScene 无法从编辑场景重导时，回到对应步骤整改，不以兼容补丁、静态检查或单元测试替代正式验收。
