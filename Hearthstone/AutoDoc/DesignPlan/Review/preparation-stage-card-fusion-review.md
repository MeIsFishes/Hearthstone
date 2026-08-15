## 1. 实现方式

在既有 RunState 与 Preparation 页面内实现原子融合、99 号卡继承、双页签 UI 和完整 Stage 生命周期。

## 2. 验收

### 2.1 验收方式

策划指定“流程log验收”。主代理在 Unity 中两次通过正式
`GameStageEntryLauncher.Start("Assets/Resources/Editor/PreparationStageEntry.asset")`
进入实际 Preparation Stage，并使用真实页签 Button、`UiDragable`、`UiInteractor` responder、融合 Button 完成三趟操作：

- Trip A：页签切换、素材放入/替换/移除、第五码，以及合计小于/等于/大于 99；覆盖 `FUNC-01`～`FUNC-05` 和 `FUNC-09` 的素材拒绝分支。
- Trip B：把出战中的 14 与 20、30、35 融合，验证原子消耗、`99/11/15`、出战槽清空、二次融合/99 素材拒绝，再把 99 编入生产 Battle；覆盖 `FUNC-06`～`FUNC-09` 和关键回归。
- Trip C：选中 14、20 后切页并未融合离开，验证选择只在本次 Preparation Session 保留、离开时清空且素材不消耗；覆盖 `FUNC-01`、`FUNC-10`。

Trip A/B 共用一次正式 Play；Trip C 停止后清空 Console 并重新从正式入口启动。所有日志均使用 `[PreparationFusion]`，包含 Stage、SessionId、BatchId、4 槽、合计、按钮状态、持有态、Revision 和出战槽，能够隔离并直接映射本篇操作。三趟结束均为 Console Error=0。

策划未要求游戏内截图，因此 `ART-` 由主代理在 Edit Mode 检查正式 Sprite、Prefab 字段、层级、状态引用、字体、CSV 和 Connected UiScene 编排；没有用流程日志替代玩家可见资产判断。

### 2.2 美术资产验收

| Case | 原编号 | 直接证据 | 结果 |
| --- | --- | --- | --- |
| ART-A | `ART-01` | `PreparationTabIdle.png`、`PreparationTabSelected.png` 均为正式 Sprite；`PreparationView.prefab` 同时编排出战/融合 Button 与 TMP，默认选中/未选底框分别引用 Selected/Idle，两个 operation root 在 Ui 初始化期均激活，显示态由 Controller 切换 | 通过 |
| ART-B | `ART-02` | `PreparationFusionSlotFrame.png` 编入 `PreparationFusionSlotItem.prefab`；运行时固定创建 4 槽。槽号徽章位于 `OccupiedState` 下，空态清空文字；有效目标复用正式 DropHighlight | 通过 |
| ART-C | `ART-03` | `PreparationFusionSumPanel.png` 与表达式/合计 TMP 使用固定布局；Prefab 中小于、等于、大于三色互不相同，运行实读等于 99 为绿色 `Bold, Underline`，代码由同一 `FusionEvaluationData` 驱动三态 | 通过 |
| ART-D | `ART-04` | Disabled/Enabled/Pressed 三张正式按钮 Sprite 尺寸体系一致；Button SpriteState 实读为 Enabled、Pressed、Disabled，并保留即时按下反馈 | 通过 |
| ART-E | `ART-05` | `PreparationMaterialSelected.png` 与 TMP“素材\n已选”组合，标记不占用编号/攻血区域；99 配置为 `CardTypeId=6, ArtworkKey=FusionCard_099`，专属 `FusionCard_099.png` 与既有卡框、编号、名称、攻血区域组合 | 通过 |
| ART-F | `ART-06` | 9/9 新 Sprite 均可加载，导入均为 Single Sprite、无 mip、Clamp；View 内 10 个 TMP 使用同一既有 `NotoSansSC-Dynamic SDF`；`Preparation.unity` 实读 `rootCount=1`、`dirty=false`、View 为 Connected Prefab | 通过 |

上述资产落点分别为 `Assets/Resources/Art/Preparation/UI/`、`Assets/Resources/Art/BattleCards/FusionCard_099.png`、`Assets/Resources/Ui/PreparationView.prefab`、`PreparationCardItem.prefab`、`PreparationFusionSlotItem.prefab` 和 `Assets/Scenes/Ui/Preparation.unity`。没有缺失、占位或临时无框资产。

### 2.3 程序功能验收

| Case | 原编号 | 操作与关键结果 | 结果 |
| --- | --- | --- | --- |
| FUNC-A | `FUNC-01` | `SelectTab` 的 Battle→Fusion→Battle→Fusion 日志始终为同一 Stage/Session/Batch，`AppliedBatchCount=1`；`FusionSlots=[14,20,30,35]` 或 Trip C 的 `[14,20,0,0]` 切页前后不变 | 通过 |
| FUNC-B | `FUNC-02` | 依次放入 14/20/30/35、以 54 替换、把 30 拖回卡池并恢复；重复 14 得到 `DuplicateMaterial`，第五码拖到非槽区域得到 `InvalidSlot`，4 槽未超限且无卡被消耗 | 通过 |
| FUNC-C | `FUNC-03` | `FusionSlots=[14,20,0,0] Count=2 Sum=34 CanFuse=False`；点击禁用按钮为 `FuseAttempt Result=SumMismatch`，Revision、持有态和出战槽不变 | 通过 |
| FUNC-D | `FUNC-04` | `FusionSlots=[14,20,30,35] Count=4 Sum=99 CanFuse=True`；素材仍持有，按钮由禁用变为可用 | 通过 |
| FUNC-E | `FUNC-05` | 54 替换 35 后 `Sum=118 CanFuse=False`；禁用点击为 `SumMismatch`，槽、卡池与出战槽均未写入 | 通过 |
| FUNC-F | `FUNC-06` | 单条 `Action=Fuse Result=Applied` 事务日志同时记录四张素材消耗、`ResultCard=99`、`PostFusionSlots=[0,0,0,0]`、`ResultOwned=True`、`PostOwned=5`，无部分完成 | 通过 |
| FUNC-G | `FUNC-07` | 事务快照记录 14=`2/3`、20=`3/4`、30=`2/3`、35=`4/5`，结果为 `Attack=11 MaxHealth=15`；取值来自 Run 永久实例 | 通过 |
| FUNC-H | `FUNC-08` | 融合前 `BattleSlotsBefore=[4,1,14]`，14 的 `AffectedBattleSlot=2`；融合后 `PostBattleSlots=[4,1,0]`，其余槽保持，四个素材持有态均为 False | 通过 |
| FUNC-I | `FUNC-09` | 未拥有 98、重复 14、第五码分别返回明确拒绝；首次融合后 `1+4+40+54=99` 因 `ResultAlreadyOwned` 禁用且不消耗；99 拖入素材槽返回 `ResultCardCannotBeMaterial`，99 持有数始终为 1 | 通过 |
| FUNC-J | `FUNC-10` | Trip C 切页前后保持 `[14,20,0,0]`；`StageUnloadComplete` 记录 `SessionExists=False FusionSlots=[]`，14/20 的前后 Owned/Attack/MaxHealth 相同，`RunRevisionBefore=2 RunRevisionAfter=2` | 通过 |

关键旧功能回归结果：

- `RGR-01`：奖励使用不可变 canonical payload 账本；同 ID 同 payload、融合后重入和同 ID 异 payload 均有定向原子/幂等测试。
- `RGR-02`：主代理把融合后的 99 拖入空出战槽并调用生产 Battle Group，实际日志为 `BattlePlayerEntity Slot=2 CardNumber=99 Attack=11 MaxHealth=15`。
- `RGR-03`：运行时共享池 `ItemWrapper.Count=99`、固定 7 列 15 行；99 为末位，既有双 Mask 裁切链与滚动区保持。
- `RGR-04`：三次 Preparation 卸载均记录 Session 回收；RunState 跨 Battle/Preparation 保留，下一次 Preparation 正常创建且 Console Error=0。
- `RGR-05`：99/type6 与普通/敌方 1～98 边界定向测试 2/2；`RunCardRulesTests` 19/19。全 EditMode 34/35，唯一失败是任务外并发把 card 1 的 ArtworkKey 改为 `Boar_001`，而既有测试仍断言 `Boar`；该失败不涉及本篇 99/type6、融合或随机边界，未被本篇回退或掩盖。

## 4. 详细审查意见

结论：通过。

Plan 唯一一次独立审查最初指出奖励历史与可变持有态耦合、二次合法 99 组合不足、99/type6 回归边界、99 未实际进入 Battle，以及原画校验职责表述不准。成立项均在原 Plan 内修正：使用 BatchId→canonical payload 不可变账本；验收批次第五张统一为 54；保留 1～98 分布与随机边界并精确新增 99/type6；把 99 编入真实 Battle；原画由资源与 Prefab 编排验证。主代理逐项核对后确认无实施硬阻塞，未重复调用 Plan reviewer。

代码唯一一次独立审查认可原子融合、账本、99 Battle 和框架边界，但指出事务/Stage 生命周期日志不足、融合槽空态编号残留、素材标记/字形与合计三态未接、Entry 默认值不一致、非法分支不 Dirty 覆盖不足。执行代理一次性补齐不可变事务快照与 StageInitialize/UnloadComplete 日志、空态清理、TMP“素材已选”、三类合计视觉、C#/Asset 同一验收批次和完整不变性测试；主代理以源码、Builder 产物、19/19 定向测试和 Unity 资产实读核对全部成立项。按用户“各类审查最多一次”的约束，没有追加第二次代码审查。

首次正式入口验收暴露 `FusionOperationRoot` 在 Ui 初始化前 inactive，导致框架没有初始化其 Wrapper/动态列表。修正没有引入手动初始化或平行页面，而是让两个 operation root 在标准 `PreUiInit/UiInit` 生命周期保持 active，再由 `OnUiOpen -> SelectTab(Battle)` 控制初始化后的默认显示，并通过公开 Builder、UiSceneBuilder、Exporter 重建正式资产。修正趟次 1 中三趟主代理正式验收全部通过。

实现继续使用唯一 `RunStateSingletonRawComponent` 和 Preparation Session；融合先完整预校验，再一次提交素材消耗、出战槽清理与 99 生成。UI 只消费规则派生值，不复制玩法判定；Stage、ECS、UiList、PreLoad、Prefab、UiScene 与资源导出均走既有公开框架。未发现兼容补丁、手写 Unity YAML、内部管理器旁路、平行状态系统或针对特定验收输入伪造结果的 trick。

框架影响仅为既有运行契约的向后兼容扩展：RunState 增加不可变奖励账本和 99 容量，Preparation Session 增加 4 个瞬态选择，Battle 初始化允许玩家出战 99，但普通与敌方随机仍严格限于 1～98/type1～5。最终活动场景为 `Assets/Scenes/Main.unity`、`isDirty=false`、`rootCount=1`，Unity Console Error=0。
