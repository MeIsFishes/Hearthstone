# 智能推荐按钮悬浮提示检查清单

- [x] 通过 — 用户要求：`FusionRecommendationTooltip` 已作为“智能推荐”按钮的静态子节点写入并序列化到备战 Prefab。
- [x] 通过 — 用户要求：Prefab 实测悬浮框文字精确为“智能寻找牌库中可以融合的组合”。
- [x] 通过 — 复用现有模式：沿用战斗卡关键词悬浮框的 `BattleBoardBackground` 暖棕底板、深棕 Outline 与 TMP 文本组合，并使用既有 `UiEventListener` Pointer Enter/Exit；未新增自定义提示组件或平行事件系统。
- [x] 通过 — UI 框架：静态节点和引用由 `PreparationViewUiBuilder` 生成；View 仅保存 Listener/Tooltip 引用，Controller 负责显隐与生命周期清理。
- [x] 不适用 — UI Scene：只修改页面 Prefab 内部节点，未改变 UiGroup、DefaultShow、整体 Position/Scale/Pivot 或导出路径，无需重导 UiSceneAsset。
- [x] 通过 — 交互回归：悬浮框背景和文本均关闭 Raycast；点击时先隐藏提示再沿用原推荐查询与弹窗流程，按钮交互状态未改变。
- [x] 通过 — 代码质量：只新增必要的两项 View 引用、两个事件回调和一个统一隐藏方法；没有一次性包装层或重复提示逻辑。
- [x] 通过 — 框架边界审计：未绕过 View/Controller、`UiEventListener`、Prefab Builder 或 UI 生命周期；未运行时拼装静态 UI。
- [x] 通过 — 验证：Unity 编译通过；Builder 重建成功；Prefab 实测 Listener 与按钮同对象、Tooltip 为按钮子节点、`460 × 94`、位置 `(354, 0)`、默认隐藏、文案和射线设置正确；针对性 EditMode 1/1 与 `RunCardRulesTests` 32/32 通过；Console 0 error/0 warning。未进入 Play Mode。
- [x] 通过 — 玩家视角设计文档：已完整读取 `design-doc-format`，同步智能推荐按钮的悬停、收起时机和显示文案。
- [x] 通过 — 美术文档：已完整读取 `art-doc-writer`、UI 总览与模块格式，同步暖棕木纹悬浮说明框和复用资产。
- [x] 通过 — 程序文档：已完整读取 `program-doc-format` 与 UI 界面格式，同步 Builder 层级、序列化引用、事件与生命周期行为。
- [x] 通过 — 无关改动审计：保留工作树中既有与并行修改，未回滚或整理本任务外文件。
- [x] 通过 — 结束审计：已重新读取清单并逐项核对代码、Prefab、测试、Console 与文档证据。
- [x] 通过 — 清理与报告：结束审计后已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，并创建同名报告。
