# 备战阶段卡牌融合实施 Plan

## 1. 需求明确

### 1.1 需求对齐

**验收方式（策划案第 6.1 节，优先记录）**：只使用“流程log验收”。主代理通过 `GameStageEntryLauncher.Start(Assets/Resources/Editor/PreparationStageEntry.asset)` 进入正式 `RunStateStage + PreparationStage`，从本次启动时间隔离日志后，使用实际页签 Button、卡牌 PointerDown/Drag/PointerUp、4 个融合槽 responder、卡池取消 responder 和融合 Button 完成玩家操作；离开备战和验证 99 进战斗时均通过生产 `HearthstoneGameEngine.EnterBattleStageGroup(...)` 提交完整 StageGroup，不另建临时 Stage 或 Play Mode 调度。稳定日志前缀使用 `[PreparationFusion]`：Preparation 操作记录 `BatchId`、当前页签、4 槽快照、素材数/编号和、按钮可用性、Run Revision、持有态和 3 个出战槽；Battle 初始化记录每个玩家 Entity 的槽位、CardNumber、Attack 与 MaxHealth。预计 3 趟：A（页签、放入/替换/移除/第 5 张与小于/等于/大于 99）、B（合法融合、攻血、消耗、出战槽同步、剩余合法 99 组合仍拒绝、99 编入出战并进入实际 Battle）、C（未融合切页保留后离开 Stage 清空）。证据分别整理到 `AutoDoc/Temp/preparation-stage-card-fusion-trip-a-flow-log.md`～`trip-c-flow-log.md`。

**美术资产验收覆盖（只做实际资产与编排检视，不以流程日志代替）**：

| 编号 | 主干 | 资产落点与编排位置 | 检视方式与通过证据 |
| --- | --- | --- | --- |
| `ART-01` | 是 | `PreparationTabIdle.png`、`PreparationTabSelected.png`；编入 `PreparationView.prefab/TabArea` 的“出战/融合”Button，文字继续使用 TMP | 在 Prefab 与 Builder 产物中核对两个页签左上并列、底框完整；选中态除颜色外还有底部金色指示结构，未选态仍清晰可点击 |
| `ART-02` | 是 | `PreparationFusionSlotFrame.png`、复用 `PreparationDropHighlight.png` 与完整卡面资产；编入 4 个 `PreparationFusionSlotItem.prefab` | 核对 4 个 2:3 槽等尺寸、同基线/间距；空态、占用态和有效悬停状态引用齐全，卡号、名称、攻血无遮挡 |
| `ART-03` | 是 | `PreparationFusionSumPanel.png` 与 TMP 合计文本；编入 `PreparationView.prefab/FusionOperationRoot` | 核对当前表达式、当前和、目标 `99` 同时可读；小于/等于/大于三态由固定 Rect 与文字/强调色组合，不发生布局跳动 |
| `ART-04` | 是 | `PreparationFusionButtonDisabled.png`、`PreparationFusionButtonEnabled.png`、`PreparationFusionButtonPressed.png`；由标准 Unity `Button` SpriteSwap 使用 | 核对三态尺寸一致、全部为正式 Sprite；等于 99 的可用态、其它条件的禁用态和按下反馈在整体配色中明确区分 |
| `ART-05` | 是 | `PreparationMaterialSelected.png`；编入 `PreparationCardItem.prefab`；`FusionCard_099.png` 与既有卡框、灰色编号六边形、攻血徽章组合成 99 号完整卡面 | 核对素材标记不遮挡编号和攻血；99 号池位使用专属原画，名称、编号 `99`、攻击、生命区域完整 |
| `ART-06` | 是 | 本表新增 Sprite、修改后的 2 个 Prefab、新增融合槽 Prefab、Connected `PreparationView` 场景实例与重新导出的 `Preparation.asset` | 逐项核对页签、4 槽、合计、按钮三态、素材标记和 99 卡没有缺失、占位、临时色块、裸文字底框或错误引用；缺任一项即不通过 |

**程序功能验收覆盖（实际运行行为，与美术 case 分开）**：

| 编号 | 主干 | 功能落点 | 流程操作、日志与通过条件 |
| --- | --- | --- | --- |
| `FUNC-01` | 是 | `PreparationController` 页签状态与同一 Preparation session | Trip A：记录初始“出战”、奖励 BatchId/Revision，点击“融合→出战→融合”；日志须显示 StageGroup/BatchId 未变、奖励应用次数与 Run Revision 未增加，切页前后融合槽一致 |
| `FUNC-02` | 是 | `RunCardRules.TrySetFusionMaterial/TryRemoveFusionMaterial`、4 个 `PreparationFusionSlotItemController` | Trip A：放入、替换、槽拖回池取消，再在满 4 槽时把第 5 张拖到融合区非槽位置；逐步日志列出固定 4 槽和持有态，替换/移除不消耗，第 5 张拒绝且原状态不变 |
| `FUNC-03` | 是 | `RunCardRules.EvaluateFusion` 与按钮刷新 | Trip A：组成 `14+20=34`；日志素材和与显示值均为 34、Button `interactable=false`，点击后槽、持有和出战状态不变 |
| `FUNC-04` | 是 | 同一派生计算与 Button 状态 | Trip A：组成 `14+20+30+35=99`；日志显示精确和 99、Button 从禁用变为可用且四张素材尚未消耗 |
| `FUNC-05` | 是 | 同一派生计算与无效点击保护 | Trip A：用已拥有的 54 替换 35，得到 `14+20+30+54=118`；日志显示大于 99、Button 禁用，点击后卡池、融合槽和出战槽不变 |
| `FUNC-06` | 是 | `RunCardRules.TryFuse` 的预校验与单次提交 | Trip B：恢复 `14+20+30+35=99` 后点击融合；同一事务日志同时列出消耗集合、99 结果、融合槽全空、唯一持有 99 与 Button 禁用，不允许部分完成 |
| `FUNC-07` | 是 | 从 `RunCardInstanceData` 永久字段求和 | `PreparationStageEntry.asset` 的验收批次用 14=`2/3`、20=`3/4`、30=`2/3`、35=`4/5`；Trip B 日志列出融合前四份永久值和结果 `Attack=11、MaxHealth=15`，不读取 Battle 临时生命/增益 |
| `FUNC-08` | 是 | 融合事务对 `CardInstances` 与 `BattleSlotCardNumbers` 的同步写入 | Trip B 先在“出战”把 14 放入一个槽，再将其用于融合；日志显示 14/20/30/35 的固定池位为空、含 14 的出战槽清空，未作为素材的其它持有卡和槽不变 |
| `FUNC-09` | 是 | `EFusionOperationResult` 拒绝原因与无写入分支 | Trip A 记录重复素材、未拥有池位和满槽第 5 张；Trip B 首次融合后用剩余 `1+4+40+54=99` 填满 4 槽，日志显示和恰为 99 但因 `ResultAlreadyOwned` Button 禁用，实际点击仍拒绝且四张未消耗。随后移除 54、尝试把 99 放入空融合槽，记录 `ResultCardCannotBeMaterial` 且前后状态相同；99 持有数始终为 1 |
| `FUNC-10` | 是 | `PreparationSessionSingletonRawComponent` 生命周期与 `InitializePreparationRuntime.Unload` | Trip C：选 14/20，切页往返确认仍为 14/20，再通过生产 Battle Group 离开；Unload 日志记录选择清空，进入后续 Stage 后 Run state 仍持有 14/20 且 Revision/出战槽未因取消选择变化 |

**关键旧功能回归**：

| 编号 | 主干 | 回归点与证据 |
| --- | --- | --- |
| `RGR-01` | 是 | 奖励 Batch 仍恰好 5 张、仅允许 `1~98`、按 BatchId + 不可变 payload 指纹原子幂等；同 ID 同 payload 即使卡已融合消耗也返回 `AlreadyApplied` 且不恢复素材，同 ID 不同 payload 在任何写入前拒绝 |
| `RGR-02` | 是 | “出战”页原 3 槽卡池→槽、槽→槽、占用替换和槽外释放行为不变。Trip B 融合后切“出战”，把 99 拖入因素材消耗而清空的槽，再进入生产 Battle Group；本次 Battle 日志必须显示玩家 Entity `CardNumber=99、Attack=11、MaxHealth=15`，证明未使用 type 6 的 `0/1` 随机后备值 |
| `RGR-03` | 是 | 共享池仍为 7 列、固定编号不补位；扩展为 15 行 `01~99`，滚到末行能看到唯一 99 位置，上方操作区不随池滚动且双 Mask 裁切链保持有效 |
| `RGR-04` | 是 | `RunStateStage` 在 Battle/Preparation 互切中不卸载；Preparation session、UI 与融合选择在离开时对称回收，无重复页面、残留监听或 Console Error |
| `RGR-05` | 是 | `BattleRulesTests` 对 1～98 的连续编号、类型 1～5 分布 `20/20/20/19/19`、Ogre 仅位于 35～98 且平均攻血偏置的断言原样保留；另断言 99 存在、`CardTypeId=6`、`ArtworkKey=FusionCard_099`、type 6 合法，但所有既有敌方/普通随机编号仍 `<=98` 且类型 `<=5`。不放宽任何 1～98 断言 |

**开发期定向测试（不替代正式流程log与美术编排验收）**：

| 测试文件/case | 必须保留或新增的判定 |
| --- | --- |
| `RunCardRulesTests.ApplyRewardBatch_IsAtomicAndIdempotent` | 继续覆盖新 ID 应用、同 ID 同 payload 无 Revision、同 ID 不同 payload 拒绝、新 ID 与已持有编号冲突整批拒绝 |
| `RunCardRulesTests.ApplyRewardBatch_RemainsIdempotentAfterGrantedCardsWereFused` | 应用 `14/20/30/35/54` → 融合消耗 14/20/30/35 → 再提交相同 ID/相同 payload；必须返回 `AlreadyApplied`、不恢复四张素材、不覆盖 99、不递增 Run Revision |
| `RunCardRulesTests.ApplyRewardBatch_RejectsDifferentPayloadForRecordedIdAfterFusion` | 上述融合后以同 ID 改任一 grant 攻血或编号；必须在写入前拒绝，指纹账本、99、剩余 54、3 槽与 Revision 全不变 |
| `RunCardRulesTests` 融合组 | 覆盖 2～4 张、和小于/等于/大于 99、永久攻血、原子消耗/生成/清出战槽、重复/99/未拥有/第 5 张/已持有及无效分支不发 Dirty |
| `BattleRulesTests.NumberedCardTableContainsContinuousBalancedAssignmentsWithBiasedOgres` | 循环与统计仍严格只覆盖 1～98 并保留原分布/Ogre 偏置断言；把原 `99 is null` 改为精确断言 99/type6/`FusionCard_099`，另断言 type 6 配置存在且 `ResourceApi.LoadSprite("FusionCard_099")` 非空 |
| `BattleRulesTests.InitialLineupContainsAllFiveCardTypes` | 保留现有我方 `{4,1,40}`、敌方 `{5,2,9}` 和类型 1～5 断言；补所有既有普通/敌方入口编号 `<=98`、类型 `<=5`，明确 type 6 不进入敌方或普通随机路径 |

确定边界：本篇不增加继续按钮、下一轮奖励、99 独立技能、商店或存档；不新建 Stage、System、平行卡池或第二个备战页面。`PreparationStageEntry.asset` 仅作为正式隔离入口，将其验收批次改为 `14、20、30、35、54`，不进入生产奖励逻辑。当前 `BattleCardCsvData.csv` 的既有工作区改动和任务外 BattleCards `.meta` 保留原样；实现只在现有 CSV 末尾增加 99 行及必要范围说明，不重写 1～98 行。策划指定的 14/20/30/35 永久值有部分低于当前类型“随机生成”区间，因此 `InitializePreparationRuntime` 不再把奖励永久值误当作随机生成值校验；`RewardCardGrantStartupData` 继续保证攻击非负、生命为正，卡/类型配置引用仍必须存在，敌方和首次随机生成仍严格使用类型区间。`ValidateGrantReferences` 当前并不检查原画资源，Plan 不声称“保留原画校验”；ArtworkKey 非空继续由 CSV 解析校验，实际 `FusionCard_099` Sprite 由 `BattleRulesTests` 的 `ResourceApi.LoadSprite`、UiScene/Resources 完整性检查和 `ART-05/ART-06` Prefab 编排共同证明。

## 2. 数据部分

### 2.1 涉及到的数据概览

| 数据 | 唯一权威来源 | 产生者 | 消费者 | 生命周期 |
| --- | --- | --- | --- | --- |
| 1～99 持有实例、永久攻血、3 个出战槽 | `RunStateSingletonRawComponent` | 奖励应用、融合事务、编成规则 | Battle 初始化、Preparation UI | 整局 `RunStateStage` |
| 已应用奖励 payload 账本 | `RunStateSingletonRawComponent.AppliedRewardBatchPayloadFingerprints` | `RunCardRules.ApplyRewardBatch` 首次成功应用 | 后续同 BatchId 幂等/冲突校验 | 整局 `RunStateStage`；不随卡牌融合消耗变化 |
| 4 个融合素材槽 | `PreparationSessionSingletonRawComponent` | 融合拖放规则 | 融合规则、Preparation UI、流程日志 | 当前 `PreparationStage`；切页保留，离 Stage 回收 |
| 当前页签 | `PreparationController` 私有 UI 状态 | 两个页签 Button | 同一 Controller 的显隐/刷新 | 页面 Open；首次固定为“出战”，不作为玩法 Model |
| 素材数、编号和、Button 状态 | `RunCardRules.EvaluateFusion` 派生值 | Run state + Preparation session | Preparation UI、融合提交与日志 | 不持久化，每次刷新重新计算 |
| 99 展示定义 | `BattleCardCsvData` 第 99 行 + `BattleCardTypeCsvData` 新类型行 | GameEngineDefault CSV DataGroup | Preparation/Battle 卡面 Controller | DataGroup 生命周期 |

不复制卡牌所有权到 UI 或 Session；融合槽只保存所选编号，读取永久值时始终回到 Run state。当前页签不参与玩法判定，因此保留为 Controller 局部展示状态。

奖励账本使用 `Dictionary<string, string>`：键为 BatchId，值为按 CardNumber 升序排列 5 个 grant 后，用 invariant 十进制和固定分隔符编码出的 canonical payload 字符串（包含每项 CardNumber/Attack/MaxHealth）。字符串不可变、跨进程稳定，不使用 `string.GetHashCode()` 或当前 `CardInstances` 作为历史依据；grant 顺序变化但内容相同时得到相同指纹。

### 2.2 新增数据列表

| 类型 | 重要字段/值 | 归属与用途 |
| --- | --- | --- |
| `EPreparationTab` | `Battle`、`Fusion` | UI 枚举，放在 `PreparationController`；决定两个上方操作 Root 的显隐 |
| `EPreparationDragSource` | `CardPool`、`BattleSlot`、`FusionSlot` | 扩展 `PreparationInteractorData`，使同一拖放链能区分池卡、出战槽和融合槽来源 |
| `EFusionOperationResult` | `Applied`、`NoChange`、`InvalidSlot`、`UnownedCard`、`ResultCardCannotBeMaterial`、`DuplicateMaterial`、`MaterialCountInvalid`、`SumMismatch`、`ResultAlreadyOwned`、`StatOverflow` | 规则层稳定结果；Controller 只把结果映射为表现与结构化日志，不复制规则判断 |
| `FusionEvaluationData` | `MaterialCount`、`CardNumberSum`、`CanFuse`、`BlockingResult` | 只读派生值；保证显示合计、Button 状态和提交前条件使用同一计算 |

固定契约集中在 `RunCardRules`：普通/素材编号 `1~98`、融合结果 `99`、总编号范围 `1~99`、素材数 `2~4`、目标和 `99`、结果持有上限 1、7 列/15 行、4 个融合槽。

### 2.3 原有数据类新增/删除字段

#### 2.3.1 原有Component类新增/删除字段

| Component | 字段改动 | Entity/生命周期 | 回收要求 |
| --- | --- | --- | --- |
| `RunStateSingletonRawComponent` | `CardInstances` 从索引 `0~98` 扩为 `0~99`；`HasCard/GetOwnedCardCount` 覆盖 `1~99`；3 个出战槽结构不变；以 `AppliedRewardBatchPayloadFingerprints` 替换只存 ID 的 `AppliedRewardBatchIds`，保存 BatchId→不可变 canonical payload 指纹 | Run state 单例 Entity / 整局 | `OnSingletonCollect` 清卡数组、3 槽、整个 payload 指纹账本与 Revision；融合只删素材实例，绝不改历史账本 |
| `PreparationSessionSingletonRawComponent` | 新增固定长度 4 的 `FusionSlotCardNumbers` 与 `ListenableVariable<int> FusionRevision` | Preparation session 单例 Entity / 当前备战 Stage | 回收先 `FusionRevision.MakeInvalid()`，再清 4 槽、奖励快照和字段，保证离场未确认选择不消耗 |

`RunCardInstanceData` 的有效编号验证扩为 `1~99`；`RewardCardGrantStartupData` 单独改用 `1~98` 素材/普通卡范围，避免奖励批次直接发放融合结果。

#### 2.3.2 原有CsvData类新增/删除字段

| CsvData | 配置内容与校验改动 |
| --- | --- |
| `BattleCardCsvData` | 支持编号 `1~99`；`BattleCardCsvData.csv` 只追加 `99,6,FusionCard_099` 并把范围注释改为 1～99，1～98 既有行不改写 |
| `BattleCardTypeCsvData` | `BattleCardTypeCsvData.csv` 追加 `6,融合造物,1,1,0,0`；该攻血范围只作为格式合法的非随机后备值，生产路径永不随机生成 99，玩家 99 战斗攻血来自 `RunCardInstanceData` |

## 3. 游戏逻辑部分

### 3.1 涉及到的游戏逻辑概览

融合是备战期离散交互，不需要每帧 System 或 StageListener。全部规则继续放在 `RunCardRules`：选择操作只写 Preparation session；融合提交先读取并校验 2～4 个唯一、已拥有的 1～98 素材，使用 `long` 求和检查编号与攻血溢出，确认玩家未持有 99 后才开始写入。写阶段依次清素材实例、清引用素材的出战槽、写入 99 永久实例、清 4 个融合槽；所有数组写完后再分别递增 Run Revision 与 FusionRevision，使监听者不会观察到“已消耗未产出”或“已产出未消耗”的中间状态。无效分支不写数组、不发 Dirty。

### 3.2 原有逻辑类改动

| 类名/文件 | 改动方向 |
| --- | --- |
| `RunCardRules.ApplyRewardBatch` | 先生成 canonical payload 指纹并查 Run state 账本：同 ID/同指纹直接 `AlreadyApplied`，不查看或恢复当前 CardInstances；同 ID/不同指纹立即拒绝。新 ID 才校验全部普通卡未持有，成功写完 5 卡后登记指纹并递增一次 Revision；冲突失败不登记。另拆分普通素材上限 98 与总卡牌上限 99 |
| `RunCardRules` 融合入口 | 新增融合槽设置/移动/替换、拖回池移除、统一评估和原子融合。Pool 来源重复卡拒绝；FusionSlot 来源可移动；目标占用时替换原素材；99、未拥有、无效/第 5 槽与已经持有 99 均返回稳定拒绝结果。融合消耗不得删除或改写奖励 payload 账本 |
| `RewardCardGrantStartupData` | 构造时显式使用普通卡上限 98；保持每批 5 张、唯一编号和深拷贝；BatchId 幂等语义改由 Run state 的不可变 payload 指纹账本承担 |
| `PreparationStages.InitializePreparationRuntime` | 保留卡牌与卡牌类型配置引用校验，移除“奖励永久值必须落在类型随机区间”的错误耦合；不虚构原画校验。Unload 在移除 session 前输出一次融合槽与 Run state 快照，再由 Component 回收清空选择 |
| `BattleStages.InitializeBattleRuntime` | 玩家 Entity 继续由 Run state 实例初始化；创建后输出稳定 `[PreparationFusion] BattlePlayerEntity` 日志，逐槽记录 `CardNumber/Attack/MaxHealth`，使 Trip B 能直接证明 99 以 `99/11/15` 进入实际 Battle 且未走 type 6 随机后备值；不改变敌方随机或战斗规则 |
结构化日志由 `PreparationController` 在实际页签点击、拖放响应、禁用 Button 点击拦截和融合点击后输出；规则层只返回结果与快照，不依赖 UI 或日志。`BattleCardRawComponent.InitializePlayer` 与 `BattleCardItemController` 无需修改：99 继续走正式 CSV/type 取得类型名与原画，攻血仍来自 Run instance，不增加 99 特判或第二套战斗卡表现。

## 4. UI部分

### 4.1 涉及到的UI部分概览

修改唯一的 `PreparationView.prefab`，不创建第二个页面。标题和共享池常驻；左上 `TabArea` 常驻；上方 `BattleOperationRoot` 保留既有 3 槽，`FusionOperationRoot` 保存 4 槽、合计面板与融合 Button，二者由 Controller 互斥显隐。共享池从 98 个扩为 99 个动态条目，仍用 `UiList.ConstantSlot/Horizontal`、7 列、2:3 卡面和双 Mask Viewport；Content 高度从 14 行扩为 15 行，最后一行只有固定编号 99，缺失编号不前移。

卡池条目继续是唯一共享入口。融合页拖卡到融合槽时提交融合选择；融合槽占用卡可拖到另一融合槽或池面板取消。`FusionOperationRoot` 背景 responder 捕获满 4 槽后的“第 5 张”非槽释放并记录拒绝；`CardPoolPanel` responder 只接受 FusionSlot 来源执行取消，不改变出战页原槽外释放规则。空池位保留不可拖动；其空态交互阻挡记录 `UnownedCard`，不产生移动。

### 4.2 新增Ui/Hud

| View类名 | 对应页面/条目 | 主要控件列表 |
| --- | --- | --- |
| `PreparationFusionSlotItemView` | 4 个融合素材槽条目 | 空态、占用卡面、原画、名称、编号徽章/文字、永久攻血、DropHighlight、UiDragable、UiInteractor |

| Controller类名 | 数据监听来源 | 监听与响应行为 |
| --- | --- | --- |
| `PreparationFusionSlotItemController` | Run state + Preparation session 固定槽索引 | 显示空/占用与永久值；槽始终作 responder，占用时作 FusionSlot requester；调用页面的强类型 Set/Remove 入口，不保存所有权 |

新增 `PreparationFusionSlotItemUiBuilder.Build()` 与 `Assets/Resources/Ui/PreparationFusionSlotItem.prefab` 一一对应；Prefab 执行 `Pre-UiInit` 和 `Export as Pre-load`，不作为 UiScene 顶层 View 导出。

### 4.3 原有Ui/Hud改动

| View类名 | 对应页面 | 新增或删除控件 |
| --- | --- | --- |
| `PreparationView` | 备战页面 | 新增两个页签 Button/目标 Image、`BattleOperationRoot`、`FusionOperationRoot`、4 槽 UiList、合计/目标 TMP、融合 Button 与禁用点击 `UiEventListener`、融合区与卡池 responder；保留奖励、共享池 ScrollRect 与 3 槽列表 |
| `PreparationCardItemView` | 共享固定卡位 | 新增素材已选标记和空态拒绝交互引用；现有完整卡面结构复用到 99 |

| Controller类名 | 数据监听改动 |
| --- | --- |
| `PreparationController` | 同时监听 Run `Revision` 与 Session `FusionRevision`；Open 默认 Battle 页、创建 99+3+4 条目；切页只切 Root/页签 Sprite，不重建 Stage/Session。统一刷新合计表达式、Button `interactable`、素材标记及全部列表，并输出稳定流程日志 |
| `PreparationCardItemController` | 刷新时读取当前页签与 session 选择；融合页标记已选素材，99/重复/未拥有尝试交由规则或空态阻挡记录，出战页仍允许已拥有 99 拖入战斗槽 |
| `PreparationSlotItemController` | `PreparationInteractorData` 增加来源类型；现有 BattleSlot responder 只处理池卡/出战槽来源，保持原编成语义 |

`PreparationViewUiBuilder.Build()` 只构建完整静态层级与序列化引用；`PreparationCardItemUiBuilder.Build()` 增加素材标记/空态交互；现有 `PreparationSlotItemUiBuilder` 不改视觉层级。Controller 不在运行时创建整页静态对象，不直接访问 `UiControllerManager`。

### 4.4 UiScene配置与导出

#### 4.4.1 原有UiScene改动

| UI编辑场景路径 | 修改的Group/Prefab归属 | 导出Asset路径 | 重新导出原因 | 受影响GameStage |
| --- | --- | --- | --- | --- |
| `Assets/Scenes/Ui/Preparation.unity` | `EPreparationUiGroup.Main` 下仍只有 Connected `PreparationView.prefab`；核对其更新后的页签/双 Root/共享池层级 | `Assets/Resources/Ui/Preparation.asset` | 页面 Prefab 实质改动后重新从活动编辑场景导出并证明配置源可复现；导出条目路径、Group、DefaultShow、位置/缩放/Pivot 应保持一致 | 已有 `PreparationStage` |

#### 4.4.2 UiScene完整性检查

| 环节 | 完成标准 |
| --- | --- |
| Prefab/Builder | `PreparationView`、`PreparationCardItem` 修改后引用完整；新增 FusionSlot Prefab/Builder 一一对应；4 槽条目 PreLoad 映射可通过 `UiApi` 创建 |
| 编辑场景 | Canvas/CanvasScaler、唯一 Exporter、`Hearthstone.EPreparationUiGroup` 和 Main Group 不变；页面实例保持 Connected Prefab，场景无脏改动 |
| 导出/运行时 | 从活动 `Preparation.unity` 调用正式 `UiSceneExporter` 重导 `Preparation.asset`；精确 `Resources.Load("Ui/Preparation")`、三个动态条目 PreLoad 与所有新增 Sprite 非空 |
| 资源索引 | 新 Sprite、99 原画和 FusionSlot Prefab 只通过 Unity 导入、公开资源/预载导出流程进入索引；不手写 `ResourcesDictionary.json`、`PreLoadUiData.asset` 或导出 Asset |
| 禁止项 | 不手写 Scene、Prefab、`.asset` YAML 或 `.meta`，不直接改 `UiObjectDatas`，不以 Controller 运行时拼静态页签/面板 |

## 5. 美术部分

### 5.1 涉及到的美术表现概览

继续使用 `UI-STYLE-001`。页面背景、标题、池面板、滚动条、普通空池位、完整卡框、灰色编号徽章、攻击/生命徽章及 DropHighlight 直接复用；页签、融合语义槽、合计、融合按钮、素材标记和 99 专属原画缺失，必须补正式透明 PNG。策划原型 `AutoDoc/DesignPlan/media/2026.08.15/preparation-stage-card-fusion/preparation-stage-fusion.png` 仅作为构图参考，不能作为运行时 UI 资产。

### 5.2 美术资产完整性检查

| 资产或资产组 | 用途 | 候选已有资产及路径 | 复用结论 | 判断依据 | 缺失或不满足 | 处理方式 |
| --- | --- | --- | --- | --- | --- | --- |
| 页面/标题/池/滚动 | 共享页面骨架 | `Assets/Resources/Art/Preparation/UI/PreparationPageBackground.png`、`PreparationStageTitleFrame.png`、`PreparationCardPoolPanel.png`、`PreparationScroll*.png` | 直接复用 | 风格、尺寸与现有备战页面一致，页签不改变下方池语义 | 无 | Builder 保持原引用，池标题改为 TMP“卡池 1-99” |
| 普通/99 完整卡面框架 | 卡面、编号、攻血 | `CardFrame-v3.png`、`CardNumberBadgeHex.png`、`AttackBadgeFrame.png`、`HealthDropBadge.png` | 直接复用 | 99 仍属于同一战斗卡体系，灰色六边形正符合验收 | 缺 99 专属原画 | 新增 `FusionCard_099.png`，其它层继续组合复用 |
| 页签状态 | 左上双页签 | 无 | 无法复用 | 现有资源没有可辨识选中/未选且带结构差异的页签底框 | Idle/Selected 两态缺失 | 新增两张通用 tab 底框，文字用 TMP |
| 4 个融合槽 | 空/占用/悬停 | `PreparationBattleSlotFrame.png`、`PreparationDropHighlight.png` | 部分复用 | DropHighlight 规格为 2:3 可直接复用；BattleSlotFrame 已承载出战语义，不混用 | 融合空槽缺失 | 新增 FusionSlotFrame；占用态复用卡面，悬停复用高亮 |
| 合计面板 | 表达式与目标 | `PreparationRewardPanel.png` | 需要新增变体 | RewardPanel 比例可拉伸但语义和内部留白针对奖励短句，不适合四项表达式 | 合计专用底框缺失 | 新增 FusionSumPanel，保持同一红金/羊皮纸语言 |
| 融合按钮三态 | 禁用/可用/按下 | 无 | 无法复用 | 现有 UI 没有正式按钮状态组 | 三态全部缺失 | 新增等尺寸三张 Button Sprite |
| 素材已选标记 | 固定池卡状态 | 无 | 无法复用 | 既有 DropHighlight 表达目标悬停，不能替代持久选择语义 | 标记缺失 | 新增不遮编号/攻血的小型透明标记 |

### 5.3 新增美术资产

| 资产名或资产组 | 资产类型 | 用途 | 规格要求 | 预期路径 |
| --- | --- | --- | --- | --- |
| `PreparationTabIdle`、`PreparationTabSelected` | 透明 PNG Sprite | 双页签底框状态 | 同尺寸约 2.4:1；Idle 棕金、Selected 红金并带独立底部金线/凸起结构；无文字 | `Assets/Resources/Art/Preparation/UI/` 同名 `.png` |
| `PreparationFusionSlotFrame` | 透明 PNG Sprite | 4 个融合空槽 | `1024×1536`、2:3，红金融合纹样，中央留空，不含文字/数字 | 同目录 |
| `PreparationFusionSumPanel` | 透明 PNG Sprite | 编号合计底框 | 宽横向、可作为 Sliced Image，中央留足表达式与目标区域，无文字 | 同目录 |
| `PreparationFusionButtonDisabled/Enabled/Pressed` | 透明 PNG Sprite 三态组 | 标准 Button SpriteSwap | 三张同尺寸约 3:1；禁用低饱和、可用金黄、按下有内压/高光位移，不含文字 | 同目录 |
| `PreparationMaterialSelected` | 透明 PNG Sprite | 卡池“素材已选”标记 | 小型角标/窄条，红金高对比，不遮左上编号及底部攻血；文字由 TMP 叠加 | 同目录 |
| `FusionCard_099` | PNG Sprite 原画 | 99 号融合结果卡 | 与现有 BattleCards 原画兼容的竖向主体构图、无卡框/数字/文字，风格与怪物原画一致 | `Assets/Resources/Art/BattleCards/FusionCard_099.png` |

## 6. GameStage部分

本篇继续使用已有 `RunStateStage + PreparationStage`，不新增 Stage、Scene、DataGroup、System、Listener 或注册项。融合选择绑定 Preparation session，Run state 只保存已经确认的结果。

### 6.1 修改LoadItem和LateLoadItem项

| LoadItem项名 | 负责内容 | 所属GameStage |
| --- | --- | --- |
| `InitializePreparationRuntime` | Load 继续原子/幂等应用 5 张普通奖励并创建 session；融合数组默认全空。引用校验不再套用随机类型攻血范围。Unload 先输出未确认融合选择快照，再移除 session，由回收清 4 槽且不写 Run state | 已有 `PreparationStage` |

`PreparationStageEntryAsset` 的 C# 默认值与 Unity 资产通过 Editor 同步为验收批次 `fusion-acceptance-001`：14=`2/3`、20=`3/4`、30=`2/3`、35=`4/5`、54=`4/2`；它仍只构造强类型 `PreparationRewardBatchStartupData` 并调用正式 Group 入口，不成为运行时可变配置。

## 7. 其他资产部分

### 7.1 涉及到的其他资产概览

不新增音频、视频、第三方包或字体文件。既有动态 TMP FontAsset 需要补入页签、融合、合计、素材标记和新卡名所需汉字。

### 7.2 其他资产完整性检查

| 资产或资产组 | 资产类型 | 用途 | 候选已有资产及路径 | 复用结论 | 来源与授权 | 缺失或不满足 | 处理方式 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 中文 UI 字体 | TMP Dynamic FontAsset | 页签、融合槽标题、编号合计、按钮、素材标记、融合造物名称 | `Assets/Resources/Fonts/NotoSansSC-Dynamic SDF.asset` | 直接复用并扩充动态图集 | 项目既有字体资产与授权不变 | 需确认“出战融合编号合计素材已选造物”等字形 | 由 Builder 的字体校验通过 TMP API 补字并保存，不替换字体、不手写 Asset |

### 7.3 原有其他资产改动

| 资产路径 | 资产类型 | 当前用途 | 改动内容 |
| --- | --- | --- | --- |
| `Assets/Resources/Fonts/NotoSansSC-Dynamic SDF.asset` | TMP FontAsset | 备战全部中文/数字 | 在 `PreparationUiBuilderUtility.RequiredChineseCharacters` 增加融合页面文本并由 Unity/TMP 正式 API补入字形；全部修改/新增 Prefab 的 TMP 引用同一资产 |

## 8. 实现顺序建议

| 步骤/Todo | 实施内容 |
| --- | --- |
| 1 | 扩展 `RunCardRules` 编号常量、`RunCardInstanceData` 与 Run/Preparation 两个单例数据结构；把已应用 BatchId HashSet 升级为 BatchId→不可变 canonical payload 指纹账本，新增融合结果/评估值类型并在回收时清完整账本。 |
| 2 | 更新 `RewardCardGrantStartupData` 普通卡边界；在两张现有 CSV 只追加 99/类型 6 配置并扩展 `BattleCardCsvData` 校验，保留所有 1～98 既有行；按开发期测试表更新 `BattleRulesTests`，不放宽原分布/Ogre/入口断言。 |
| 3 | 实现 `ApplyRewardBatch` 指纹幂等与融合槽设置/移动/替换/移除、统一评估、预校验后原子融合；补齐“应用→融合消耗→同 payload 重入”“同 ID 不同 payload”及 2～4 张、和 99、永久攻血、消耗/生成/出战槽、重复/99/未拥有/第 5 张/已持有/无 Dirty 测试。 |
| 4 | 修改现有 Preparation View/Controller/CardItem/SlotItem，并新增 FusionSlot View/Controller 与四态拖放来源，完成双页签、共享 99 池、合计/Button、取消 responder 和结构化流程日志；不运行时拼静态层级。 |
| 5 | 创建第 5.3 节正式 Sprite 与 99 原画；只以策划原型作参考，输出透明、无文字的正式运行时资产并核对资源 key 唯一。 |
| 6 | 扩充既有 NotoSansSC TMP FontAsset 所需字形，保持所有 Preparation TMP 引用一致。 |
| 7 | 修改 `PreparationViewUiBuilder`、`PreparationCardItemUiBuilder`，新增一一对应 `PreparationFusionSlotItemUiBuilder`；通过 Unity Editor 执行 Build、Pre-UiInit 和 FusionSlot Pre-load 导出，核对 99+3+4 动态条目、序列化引用及可重复生成。 |
| 8 | 打开 `Assets/Scenes/Ui/Preparation.unity` 核对 connected 页面、Canvas/Group/Exporter，保存后从活动场景正式重导 `Preparation.asset`；验证 Resources、PreLoad、Sprite、CSV 和等价重导出，不手写 Unity YAML/.meta/索引。 |
| 9 | 修改 `InitializePreparationRuntime` 的卡/类型引用校验与 Unload 日志，并为 `InitializeBattleRuntime` 增加逐玩家 Entity 的结构化状态日志；通过 Unity Editor 把 `PreparationStageEntry.asset` 正式验收批次更新为 `14/20/30/35/54`。保持 StageGroup 与加载注册不变，原画存在性按 Resources/ART 编排链验证。 |
| 10 | 同步 `AutoDoc/Program/Specific/preparation-card-pool/preparation-card-pool.md`、`AutoDoc/Program/UI/preparation/preparation.md`、`AutoDoc/Art/UI/ui-art-overview.md`、`AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md` 与直接相关 Design 现状文档，只写已实现现状。 |
| 11 | 完成编译、针对性 EditMode tests、Prefab/Scene/Exporter/Resources/Console 静态检查；再由主代理按 Trip A～C 从正式入口执行流程log验收，分别维护 `ART-01`～`ART-06`、`FUNC-01`～`FUNC-10` 与 `RGR-01`～`RGR-05` 结论。 |

Todo 判定：步骤 1～11 与本表一一对应。发现幂等仍读取可变 CardInstances、同 ID 不同 payload 未拒绝、融合改写历史指纹账本、99 被奖励直接发放、选择写入 Run state、融合写入中途可观察、永久值读取 Battle 临时状态、卡池不是固定 99 位置、页签重建 Stage/重复发牌、Controller 拼静态 UI、FusionSlot 未 Pre-load、UiScene 不可从编辑场景重导、99 无法以 `11/15` 进入实际 Battle、放宽 1～98 既有断言或改写其配置时，回到对应步骤整改，不以兼容补丁、静态检查或测试自证替代正式验收。
