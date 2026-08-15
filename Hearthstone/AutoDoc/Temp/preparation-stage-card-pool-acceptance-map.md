## 正式验收映射（首次）

- 入口：`Assets/Resources/Editor/InitialStageEntry.asset`，通过 `GameStageEntryLauncher.Start` 启动正式 `{RunStateStage, BattleStage}`，等待真实终局 Listener 自动进入 `{RunStateStage, PreparationStage}`。
- 方式：游戏内截图；证据目录 `AutoDoc/DesignPlan/media/2026.08.15/preparation-stage-card-pool/review/`。
- Trip A / 常态主干：`trip-a-battle-terminal.png`、`trip-a-preparation-overview.png`、`trip-a-card-pool-bottom.png`、`trip-a-idempotent-reentry.png`；覆盖 `ART-01`～`ART-05`、`ART-07`、`FUNC-01`～`FUNC-03`、`RGR-01`～`RGR-04`。预期为真实战斗终局后自动进入备战，只新增一次 5 张，首屏固定编号/空位、3 槽与滚动条完整，滚至末段含 98，同 batch 重提可见状态不变。
- Trip B / 空槽拖入：`trip-b-empty-slot-hover.png`、`trip-b-empty-slot-after-drop.png`；覆盖 `ART-06`、`FUNC-04`。预期悬停目标唯一高亮，释放后卡只进入目标空槽。
- Trip C / 占用槽替换：`trip-c-replace-before.png`、`trip-c-replace-after.png`；覆盖 `FUNC-05`。预期目标槽由新卡替换，原卡仍在固定卡池编号位置且无叠卡。
- Trip D / 换槽与无效拖放：`trip-d-move-before.png`、`trip-d-move-after.png`、`trip-d-invalid-before.png`、`trip-d-invalid-after.png`、`trip-d-full-slots-after-repeat.png`；覆盖 `FUNC-06`～`FUNC-08`、`RGR-04`～`RGR-05`。预期换槽后原槽清空/目标遵循替换，无效释放前后 3 槽不变，满槽重复换槽仍最多 3 张且无同号重复。
- 分类：上述 `ART-*` 与 `FUNC-*` 均为主干并分别判定；截图可交叉复用但不以功能结论代替美术结论。
