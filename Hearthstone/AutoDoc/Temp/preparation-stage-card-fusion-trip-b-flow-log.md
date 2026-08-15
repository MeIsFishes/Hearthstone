# 备战阶段卡牌融合 Trip B 流程日志

- 启动隔离：与 Trip A 共用同一次正式 Preparation Play，证据均使用同一 `SessionId=-1340849188` 与 `BatchId=fusion-acceptance-001`。
- 实际交互：页签 Button、卡池到出战槽/融合槽拖放、融合 Button，以及生产 `HearthstoneGameEngine.EnterBattleStageGroup(BattleStageStartupData.CreateDefault())`。

## 操作与结果

1. 切到出战页，把 14 放入原第三槽，再切回融合页：`BattleSlots=[4,1,14]`，融合素材仍为 `[14,20,30,35]`。
2. 点击亮起的融合按钮，单条事务日志记录：
   - `Materials=[...14,Attack=2,MaxHealth=3,AffectedBattleSlot=2;20,3/4;30,2/3;35,4/5]`
   - `BattleSlotsBefore=[4,1,14] ResultCard=99 ResultAttack=11 ResultMaxHealth=15`
   - `PostFusionSlots=[0,0,0,0] PostBattleSlots=[4,1,0]`
   - `PostMaterialOwned=[14:False,20:False,30:False,35:False] ResultOwned=True PostOwned=5`
3. 用剩余的 1、4、40、54 组成 99：`FusionSlots=[1,4,40,54] Sum=99 CanFuse=False`，禁用点击得到 `FuseAttempt Result=ResultAlreadyOwned`，持有数仍为 5。
4. 移除 54 后把 99 拖入空融合槽：`SetMaterial Result=ResultCardCannotBeMaterial`，槽保持 `[1,4,40,0]`、Run Revision 不变。
5. 清空未确认素材，切回出战，把 99 拖入空槽：`BattleSlots=[4,1,99]`；调用生产 Battle Group 后记录：`BattlePlayerEntity Slot=2 CardNumber=99 Attack=11 MaxHealth=15`。
6. Preparation 卸载记录 `SessionExists=False FusionSlots=[] RunRevisionBefore=5 RunRevisionAfter=5`；本场战斗完成后按既有流程回到下一次 Preparation 并应用新的奖励 Batch。

## 结论映射

- `FUNC-06`：通过；消耗、生成 99、清槽在同一事务中完成。
- `FUNC-07`：通过；永久攻血 `2/3+3/4+2/3+4/5=11/15`。
- `FUNC-08`：通过；素材 14 所在出战槽同步清空，其余槽保持。
- `FUNC-09`：通过；持有 99 时二次合法组合被拒，99 不能作为素材，数量始终为 1。
- `RGR-02`：通过；99 以 `CardNumber=99 Attack=11 MaxHealth=15` 进入真实 Battle 玩家 Entity。
- `RGR-04`：通过；Stage 切换后 Session 对称回收，Run 状态与 99 保留。
- Console Error：0。
