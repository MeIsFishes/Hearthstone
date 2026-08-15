# 备战阶段卡牌融合 Trip A 流程日志

- 启动隔离：2026-08-15，本趟开始前清空 Unity Console。
- 正式入口：`GameStageEntryLauncher.Start("Assets/Resources/Editor/PreparationStageEntry.asset")`
- 入口参数：`BatchId=fusion-acceptance-001`；奖励 `14:2/3,20:3/4,30:2/3,35:4/5,54:4/2`。
- 实际交互：真实页签 Button、卡池/融合槽 `UiDragable`、`UiInteractor` responder、禁用按钮 AttemptListener。

## 操作与结果

1. 正式进入备战：`StageInitialize ... RewardApplyResult=Applied AppliedBatchCount=1 ... Owned=8 RunRevision=2`。
2. 切到融合页，把 14、20 拖入槽 0、1：`FusionSlots=[14,20,0,0] Count=2 Sum=34 CanFuse=False`；点击禁用按钮得到 `FuseAttempt Result=SumMismatch`，状态不变。
3. 再放入 30、35：`FusionSlots=[14,20,30,35] Count=4 Sum=99 CanFuse=True`；页面合计文字实读为绿色 `Bold, Underline`。
4. 把重复 14 拖向槽 1：`Result=DuplicateMaterial`，仍为 `[14,20,30,35]`。
5. 用 54 替换 35：`FusionSlots=[14,20,30,54] Sum=118 CanFuse=False`；点击禁用按钮得到 `Result=SumMismatch`，持有数与出战槽不变。
6. 点击未拥有 98：`Result=UnownedCard`；恢复 35 后把第五码 54 拖到融合区非槽位置：`Result=InvalidSlot`，仍为 `[14,20,30,35]`。
7. 把槽 2 的 30 拖回卡池：`RemoveMaterial Result=Applied FusionSlots=[14,20,0,35] Sum=69`；再拖回槽 2 后恢复 `Sum=99 CanFuse=True`。
8. 切换“出战→融合”：两条 `SelectTab` 日志均保持 `BatchId=fusion-acceptance-001 AppliedBatchCount=1 FusionSlots=[14,20,30,35] RunRevision=2`，未离开 Stage、未重复发奖。

## 结论映射

- `FUNC-01`：通过；页签切换不重复发奖，素材选择保留。
- `FUNC-02`：通过；放入、替换、移除、重复和第五码分支均由 4 槽日志直接证明。
- `FUNC-03`：通过；34 时禁用且点击无写入。
- `FUNC-04`：通过；99 时启用且素材仍持有。
- `FUNC-05`：通过；118 时禁用且点击无写入。
- `FUNC-09`（重复、未拥有、第五码部分）：通过。
- Console Error：0。
