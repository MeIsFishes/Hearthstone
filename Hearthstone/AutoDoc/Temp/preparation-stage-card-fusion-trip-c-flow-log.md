# 备战阶段卡牌融合 Trip C 流程日志

- 启动隔离：停止前一 Play、等待返回 Edit Mode、清空 Unity Console 后重新通过正式 Preparation entry 启动。
- 入口参数：`BatchId=fusion-acceptance-001`，`RewardApplyResult=Applied AppliedBatchCount=1`。

## 操作与结果

1. 切到融合页，把 14、20 拖入槽 0、1：`FusionSlots=[14,20,0,0] Count=2 Sum=34 CanFuse=False RunRevision=2 FusionRevision=2 Owned=8`。
2. 切换“出战→融合”：两条 `SelectTab` 日志都保持 `FusionSlots=[14,20,0,0]`、`AppliedBatchCount=1`、`RunRevision=2`。
3. 未点击融合，通过生产 `EnterBattleStageGroup(CreateDefault())` 离开备战：
   - `StageUnloadBegin ... FusionSlots=[14,20,0,0] MaterialRunState=[14:Owned=True,Attack=2,MaxHealth=3;20:Owned=True,Attack=3,MaxHealth=4] RunRevision=2`
   - `StageUnloadComplete ... SessionExists=False FusionSlots=[] MaterialRunBefore=[14:Owned=True,...;20:Owned=True,...] MaterialRunAfter=[14:Owned=True,...;20:Owned=True,...] RunRevisionBefore=2 RunRevisionAfter=2 Owned=8`
4. 后续真实 Battle 玩家阵容仍为 `[4,1,40]`，战斗结束返回下一次 Preparation，证明未确认素材未污染 Run 状态。

## 结论映射

- `FUNC-01`：通过；切页不重复发奖且选择保留。
- `FUNC-10`：通过；离开 Stage 后 Session 选择清空，14/20 仍持有，Run Revision 不变。
- `RGR-04`：通过；Preparation Session/UI 随 Stage 对称卸载，下一次 Preparation 正常创建。
- Console Error：0。
