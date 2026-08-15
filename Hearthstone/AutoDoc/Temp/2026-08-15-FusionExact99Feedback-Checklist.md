# 融合页签精确 99 与数值反馈检查清单

## 用户要求

- [x] **通过**：融合页签右侧显示两个文本：上方为当前卡牌编号总和，下方为距离 99 的剩余值。证据：`PreparationController.RefreshAll()` 分别写入“当前值”和 `99 - 当前值`；现有 `PreparationView.prefab` 已有两项 TMP 序列化引用。
- [x] **通过**：当前值正好为 99 时，上方当前值文字产生闪光效果。证据：`UpdateFusionTargetGlow()` 在目标绿与白色间循环插值，并以 `1.0~1.05` 倍轻微缩放当前值文字。
- [x] **通过**：仅当前值正好为 99 时，右侧融合按钮亮起并可用。证据：按钮 `interactable` 读取 `evaluation.CanFuse`，而 `EvaluateFusion()` 在配方评估前强制 `cardNumberSum == 99`；结果已拥有等其他既有阻断条件仍会保持禁用。
- [x] **通过**：融合成立条件必须是卡牌编号总和等于 99，超过 99 不得融合。证据：`RunCardRules.FusionTargetCardNumberSum = 99`；小于与大于目标均返回 `CardNumberSumNotExact`，`TryFuse()` 复用同一评估结果。

## 实现与验证

- [x] **通过**：复用现有 `PreparationView`、`PreparationController`、`PreparationSessionSingletonRawComponent.FusionRevision`、`RunCardRules.EvaluateFusion()`、Button SpriteSwap 和两项 TMP 引用；未新增平行页面或运行时静态层级。
- [x] **通过**：显示读取 `FusionEvaluationData.CardNumberSum`，按钮读取 `CanFuse`，执行入口 `TryFuse()` 再次调用同一 `EvaluateFusion()`，三处没有分叉判定。
- [x] **通过**：新增测试覆盖 `34`、`99`、`100` 三种编号和；空选择由评估返回 `0`，界面确定显示“当前值：0 / 剩余值：99”；超过目标显示负剩余值。
- [x] **通过**：切出融合页、页面隐藏、关闭或数值离开 99 时调用 `SetFusionTargetGlow(false)` 恢复单位缩放；页面重新显示时 `RefreshAll()` 从权威状态恢复。
- [x] **通过**：`dotnet build Hearthstone.Tests.csproj --no-restore --nologo -v:minimal` 与 `dotnet build Hearthstone.Ui.Editor.csproj --no-restore --nologo -v:minimal` 均为 0 错误；仅报告项目既存的程序集版本冲突警告。Unity Builder/EditMode 测试因许可令牌不可用且 Unity MCP 无活动实例未执行。
- [x] **通过**：`git diff --name-only` 仅列出本任务 3 个代码/测试文件和 5 份相关正式文档；启动时已有及执行期间继续出现的未跟踪 `BattleAttackFusion_*.png/.meta` 均未纳入差异、未编辑或删除。
- [x] **通过**：仅新增精确目标常量、闪光状态和两个职责明确的生命周期方法；删除了已失去用途的算式/结果文本构造函数与 `StringBuilder` 引用，没有保留一次性抽象。

## 框架边界审计

- [x] **通过**：View 仍只保存引用；Controller 通过 `OnUiUpdate`、`OnUiShow`、`OnUiHide`、`OnUiClose` 管理表现；权威状态仍在 ECS RawComponent/规则层；按钮、Prefab、UiScene 和导出 Asset 均沿用现有框架。
- [x] **不适用**：现有 BbxCommon UI 生命周期、TMP、Button SpriteSwap 与规则层已能满足需求，没有框架能力缺口或框架改动。

## 文档同步

- [x] **通过**：已完整读取 `design-doc-format`，并更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`，记录精确 99、当前/剩余值、闪光与按钮反馈。
- [x] **通过**：已完整读取 `art-doc-writer`、模块与 UI 美术格式，更新 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md` 与 `AutoDoc/Art/UI/ui-art-overview.md`；明确复用现有面板和按钮 Sprite、无新增位图。
- [x] **通过**：已完整读取 `program-doc-format`、UI 与玩法模块格式，更新备战 UI 与卡池规则程序文档，记录权威精确判定、UI 刷新和生命周期。

## 结束审计

- [x] **通过**：已于结束阶段重新读取本清单，并以代码、测试、编译、差异和文档证据逐项复核。
- [x] **通过**：已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 `0`。
- [x] **通过**：清理后已创建同任务名 `2026-08-15-FusionExact99Feedback-Report.md`，记录结果、证据、偏差、风险、文档处理与清理结果。
