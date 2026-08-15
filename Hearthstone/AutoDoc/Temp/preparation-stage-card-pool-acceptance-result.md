## 最终正式验收结果

- 结论：通过。首次验收暴露中文字体缺字；修正趟次 1 暴露滚动裁切缺口；修正趟次 2 完成双 Mask 裁切并修正 round 4 的自证式测试问题。最终主干 `ART-01`～`ART-07`、`FUNC-01`～`FUNC-08` 与 `RGR-01`～`RGR-05` 均取得可判定证据。
- 正式进入：通过 `GameStageEntryLauncher.Start(Assets/Resources/Editor/InitialStageEntry.asset)` 启动；没有手动打开内部 Scene。所有拖放均向实际 `UiEventListener` 发送 PointerDown/Drag/PointerUp，滚动向实际 `ScrollRect.OnScroll` 发送 PointerEventData。
- 最终 Unity 状态：退出 Play 后活动场景 `Assets/Scenes/Main.unity`，唯一加载、`isDirty=false`、`rootCount=1`；Console error=0。

## Trip A：真实战斗、自动备战、固定卡池与幂等

- 战斗证据：`trip-a-battle-before-terminal.png` 显示实际 3v3 Battle 与“战斗进行中”；运行态随后为 `PlayerVictory|actions:4`，并自动进入 batch `initial-battle-reward-001` 的 Preparation。
- 备战常态：`trip-a-idempotent-reentry.png` 显示中文标题/奖励/分区、3 个槽、01～14 首屏、7×2、完整卡面与空槽、正式底框/滚动条。
- 滚动末段：实际 `ScrollRect.OnScroll` 后 `verticalNormalizedPosition=0`；`trip-a-card-pool-bottom-final.png` 显示 `92～98`，标题、奖励和 3 槽保持完整可见。
- 同 batch 幂等：同一 Play 中生产 `EnterPreparationStageGroup` 重提默认 batch，前后均为 `owned:8|rev:2|slots:4,1,40`；截图 `trip-a-idempotent-before-repeat.png` 与 `trip-a-idempotent-after-repeat.png` 可见状态一致。
- 覆盖：`ART-01`～`ART-05`、`ART-07`、`FUNC-01`～`FUNC-03`、`RGR-01`～`RGR-04`，全部通过。

## Trip B：拖入空槽

- 前置通过真实 Slot→Slot 拖放把 `4,1,40` 调整为 `0,4,40`，得到空槽。
- `trip-b-empty-slot-hover.png`：卡 02 被拖到左侧空槽，目标高亮开启（运行态 `highlight=True`），卡与目标可辨。
- PointerUp 后运行态 `slots:2,4,40|highlight=False`；`trip-b-empty-slot-after-drop.png` 只在左槽新增卡 02，同卡未占第二槽。
- 覆盖：`ART-06`、`FUNC-04`，通过。

## Trip C：替换占用槽

- `trip-c-replace-before.png`：左槽为卡 02；真实拖放卡 03 到该槽后，运行态从 `2,4,40` 变为 `3,4,40`。
- `trip-c-replace-after.png`：左槽变为卡 03、没有叠卡；原卡 02 在卡池固定编号位置仍显示完整卡面。
- 覆盖：`FUNC-05`，通过。

## Trip D：换槽、无效拖放与唯一性

- `trip-d-move-before.png` / `trip-d-move-after.png`：真实 Slot→Slot 拖放把 `4,1,40` 变为 `0,4,40`，原槽清空、目标遵循替换规则，同卡唯一，覆盖 `FUNC-06`。
- `trip-d-invalid-before.png` / `trip-d-invalid-after.png`：卡 05 释放到槽外，运行态前后均为 `3,4,40`，无丢失、复制或误替换，覆盖 `FUNC-07`。
- 满 3 槽时把卡 05 拖入占用槽并再次把同卡拖向另一槽，最终固定数组仍长 3、状态 `0,5,40` 且无重复编号；`trip-d-full-slots-after-repeat.png` 显示恰好 3 个槽框、无叠放，覆盖 `FUNC-08`。
- `BattleCardItem.prefab`、其 Builder/View/Controller 与 `BattleRulesTests.cs` 无本篇差异；最终 Play Console error=0，覆盖 `RGR-04`～`RGR-05`。

## 美术分类结论

- `ART-01`～`ART-07`：全部通过。标题、分隔线、3 槽、完整卡面/空槽/编号、羊皮纸与深蓝池构图、滚动条、悬停高亮均使用正式资产；最终截图无乱码、默认控件、裸文字、缺图或临时占位。

## 程序分类结论

- `FUNC-01`～`FUNC-08`：全部通过。自动切换、5 张单次奖励、固定 01～98、滚至 98、空槽放置、替换、换槽、无效取消、3 槽与单卡唯一均由本次实际运行和操作证据闭环。
- 已知全 EditMode 24/25 的唯一失败为任务前 `BattleCardCsvData.csv` 卡 1 `ArtworkKey=Boar_001` 与既有测试断言 `Boar` 不一致；本篇没有修改该 CSV 或旧测试，不影响上述本篇实际验收结论，但保留为任务外风险。
