# 继续按钮与融合点数布局修复检查清单

- [x] 通过 — 用户要求：Continue Button 的 Pressed/Selected 改为复用透明边缘的 Idle Sprite，Navigation 设为 None；Highlighted 与 Waiting 继续使用各自透明边缘 Sprite，Prefab 实测引用正确。
- [x] 通过 — 用户要求：当前点数拆为 `FusionCurrentPointLabel` 与 `FusionCurrentPointValue`；Prefab 实测标签为 `MidlineLeft`、数值为 `MidlineRight`。
- [x] 通过 — 用户要求：剩余点数拆为 `FusionRemainingPointLabel` 与 `FusionRemainingPointValue`；Prefab 实测标签为 `MidlineLeft`、数值为 `MidlineRight`。
- [x] 通过 — 用户要求：两块白色木制点数底框从 `216 × 72` 加宽至 `280 × 72`，标签区与数值区均位于底框内。
- [x] 通过 — 运行时刷新：Controller 只写入两个数值 Text；精确 99 闪光与超过 99 红色仅作用于当前点数数字，剩余点数继续显示 `99 - 当前值`。
- [x] 通过 — UI 框架：沿用 Preparation View/Controller、`PreparationView.prefab` 与一一对应的 `PreparationViewUiBuilder`；Unity 重新执行 Builder 生成静态层级和序列化引用。
- [x] 不适用 — UI Scene：仅修改页面 Prefab 内部控件，未改变 UiGroup、DefaultShow、整体 Position/Scale/Pivot 或导出路径，无需重导 UiSceneAsset。
- [x] 通过 — 交互回归：Continue Button 状态由既有 Button/Controller 驱动；`RunCardRulesTests` 32/32 通过，覆盖融合按钮、智能推荐、点数状态与继续按钮 Prefab 状态。
- [x] 通过 — 代码质量：四个字段分别承担标签/数字引用；全局检索确认 C# 与 Prefab 中没有 `FusionExpressionText`、`FusionResultText` 旧引用。
- [x] 通过 — 框架边界审计：未新增平行 UI 实现、运行时静态层级或绕过生命周期的数据写入。
- [x] 通过 — 验证：Unity 编译成功；Prefab 结构检查确认 `610 × 250` 面板、`280 × 72` 点数底框、对齐与 SpriteState；针对性测试 2/2、`RunCardRulesTests` 32/32 通过；清空后 Console 为 0 error/0 warning。未进入 Play Mode。
- [x] 通过 — 玩家视角设计文档：已完整读取 `design-doc-format`，同步点数信息层级、加宽底框与 Continue Button 透明点击反馈。
- [x] 通过 — 美术文档：已完整读取 `art-doc-writer`、UI 总览与模块格式，同步实际使用的按钮状态和点数底框视觉布局。
- [x] 通过 — 程序文档：已完整读取 `program-doc-format` 与 UI 界面格式，同步四项 View 引用、数值刷新、Builder 层级和 Continue Button SpriteState。
- [x] 通过 — 无关改动审计：工作树内存在大量并行任务改动；本任务未回滚或整理它们，修改仅落在备战 UI 相关代码、测试、Prefab 与对应文档。
- [x] 通过 — 结束审计：已重新读取本清单并逐项核验实现与验证证据。
- [x] 通过 — 清理与报告：结束审计后已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，并创建对应报告。
