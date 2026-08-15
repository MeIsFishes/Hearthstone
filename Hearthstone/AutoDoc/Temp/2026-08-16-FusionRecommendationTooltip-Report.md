# 智能推荐按钮悬浮提示报告

## 1. 任务结果

备战页面融合面板的“智能推荐”按钮已挂接悬浮说明框。鼠标进入按钮时在按钮右侧显示“智能寻找牌库中可以融合的组合”，移出、点击打开推荐弹窗、切换到出战页、页面隐藏或关闭时收起。

## 2. 实现范围

- `PreparationViewUiBuilder` 在智能推荐按钮下静态创建 `Tooltip` 子节点：尺寸 `460 × 94`，相对按钮位置 `(354, 0)`，复用 `BattleBoardBackground.png` 暖棕木纹底板与深棕描边。
- `PreparationView` 保存按钮上的 `UiEventListener` 和 Tooltip 引用。
- `PreparationController` 通过 Pointer Enter/Exit 控制显隐，并在点击、切页、隐藏与关闭生命周期中清理提示。
- `PreparationUiBuilderUtility` 的中文字体字形验证集合已包含完整提示文案。
- `RunCardRulesTests` 增加节点归属、文案、对齐、位置、尺寸、默认显隐和 Raycast 断言。

## 3. 检查项与证据

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 悬浮框挂接 | 通过 | Prefab 实测 Tooltip 父节点为 `FusionRecommendationButton`，Listener 与按钮位于同一 GameObject |
| 提示文案 | 通过 | TMP 文本精确为“智能寻找牌库中可以融合的组合” |
| 视觉复用 | 通过 | 复用现有悬浮框采用的 `BattleBoardBackground`、暖棕着色、深棕 Outline 和 TMP 文字组合 |
| 射线与点击 | 通过 | 背景和文本 `raycastTarget=false`；点击先隐藏提示再执行原推荐查询 |
| 生命周期 | 通过 | Pointer Exit、点击、切到出战页、页面 Hide/Close 均调用统一隐藏入口 |
| 框架边界 | 通过 | 静态 UI 由 Builder 写入 Prefab，View 保存引用，Controller 管理事件；未运行时拼装或新增平行组件 |
| UI Scene | 不适用 | 页面内部节点变化未影响 UiScene 导出信息 |
| 文档同步 | 通过 | 已同步玩家设计、美术 UI 总览、美术模块和备战 UI 程序文档 |

## 4. 验证结果

- Unity 编译：通过。
- `PreparationViewUiBuilder.Build()`：执行成功并重新生成 Prefab。
- Prefab 实测：`460 × 94`、位置 `(354, 0)`、默认隐藏、文案/对齐/背景/描边/射线均正确。
- 针对性 EditMode：1/1 通过。
- `Hearthstone.Tests.RunCardRulesTests`：32/32 通过。
- Unity Console：清空并重新编译后 0 error、0 warning。
- 相关代码和文档 `git diff --check`：通过。
- 按项目默认约束未进入 Play Mode。

## 5. 偏差与风险

- 未进行 Play Mode 鼠标手感目视验收；已通过 Prefab 结构、事件源码与 EditMode 断言验证。最终游戏内验收可重点观察不同分辨率下按钮右侧说明框的屏幕边距。
- 工作树包含其他任务的未提交修改，本任务未回滚或整理这些内容。

## 6. 清理结果

结束审计后已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0。检查清单与本报告均已保留。
