# 融合页签精确 99 与数值反馈任务报告

## 任务结果

已完成。融合页签右侧原有两项 TMP 文本现在分别显示“当前值”和“剩余值”；剩余值按 `99 - 当前值` 计算，超过目标时显示负数。当前值正好为 `99` 且位于融合页时，上方文字循环提亮并轻微缩放；按钮只在规则层确认可融合时切换为可用亮态。

融合的权威规则已增加严格编号和门槛：只有 `CardNumberSum == 99` 才继续评估配方；低于或高于 `99` 都返回 `CardNumberSumNotExact`。按钮显示与 `TryFuse()` 均复用 `FusionEvaluationData`，不存在 UI 可用但实际执行采用另一套阈值的情况。

## 检查项结果与证据

| 检查范围 | 状态 | 证据 |
| --- | --- | --- |
| 当前值与剩余值 | 通过 | `PreparationController.RefreshAll()` 写入当前编号和与 `99 - 当前值`；复用现有两项 TMP 引用 |
| 精确 99 闪光 | 通过 | `UpdateFusionTargetGlow()` 在目标绿和白色间插值，并使用最大 `1.05` 倍缩放 |
| 按钮亮态 | 通过 | `FusionButton.interactable = evaluation.CanFuse`，Button 继续使用既有 SpriteSwap 三态资源 |
| 严格等于 99 | 通过 | `RunCardRules.FusionTargetCardNumberSum` 为 `99`；不相等时统一返回 `CardNumberSumNotExact` |
| 生命周期 | 通过 | 页签切换、Hide、Close 与数值变化都会停止闪光并恢复单位缩放；Show 时重新刷新 |
| 边界覆盖 | 通过 | 新增低于 `34`、精确 `99`、超过 `100` 的评估与提交测试；空选择显示由 `0` 和目标常量直接派生 |
| 框架边界 | 通过 | 玩法权威状态留在 ECS/规则层，View 只保存引用，Controller 使用 BbxCommon UI 生命周期，无平行页面、池或导出资产 |
| 无关文件隔离 | 通过 | 最终 `git diff --name-only` 仅包含本任务代码、测试和正式文档；未跟踪的 `BattleAttackFusion_*.png/.meta` 未编辑、删除或纳入任务差异 |

## 验证结果

- `dotnet build Hearthstone.Tests.csproj --no-restore --nologo -v:minimal`：通过，0 错误。
- `dotnet build Hearthstone.Ui.Editor.csproj --no-restore --nologo -v:minimal`：通过，0 错误。
- `git diff --check`：通过，无空白错误。
- 编译输出仍有项目既存的 `System.Net.Http`、`System.Security.Cryptography.Algorithms` 版本冲突警告，以及底层既存警告；本任务未新增编译错误。
- 未进入游戏验证，符合项目默认“不主动进入游戏”的约束。

## 执行偏差

尝试通过 Unity BatchMode 运行页面 Builder 时，许可客户端返回“Access token is unavailable”；随后检查 Unity MCP，当前无活动实例。因此未运行 Builder 或 Unity EditMode Test Runner。页面所需两项 TMP 和 Button 已存在于当前 Prefab，最终实现只改变 Controller 的运行时文案、状态和动画，无需修改 Prefab、UiScene 或导出 Asset。

Builder 尝试曾造成 Prefab 与动态字体图集的意外重序列化；结束审计已把这两项恢复到任务开始时的内容，最终差异中不包含这些生成副作用。

## 未解决风险

未在实际 Game View 中观察文字闪光节奏、负剩余值排版和按钮 Sprite 切换。代码与程序集编译已通过，且现有文本矩形尺寸不变；仍建议后续在 Unity 许可可用时做一次融合页实际画面检查。

## 文档处理

- 玩家视角：更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`。
- 美术：更新 `AutoDoc/Art/UI/ui-art-overview.md` 与 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md`，确认无新增位图资产。
- 程序：更新 `AutoDoc/Program/UI/preparation/preparation.md` 与 `AutoDoc/Program/Specific/preparation-card-pool/preparation-card-pool.md`。

## 清理结果

任务结束时已且仅已执行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 `0`；随后创建本报告。清理未触碰用户并行产生的 `Assets/Resources/Art/BattleCards/Effects/BattleAttackFusion_*.png/.meta` 文件。
