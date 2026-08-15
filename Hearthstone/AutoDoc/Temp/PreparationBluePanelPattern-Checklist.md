# 备战蓝色底板浅色花纹检查清单

## 用户要求与视觉范围

- [通过] `CardPoolPanel/BluePanelPattern` 已增加两排浅蓝空心菱形和小菱点；线纹 Alpha `7.5%`、小点 Alpha `5%`。
- [通过] 新纹样只位于 `PreparationView` 的深蓝卡池面板，未修改共享 `BattleCardItem` 或其 `CardBasePattern`。
- [通过] 1920×1080 空卡池预览确认纹样稀疏、低对比并位于滚动卡牌之后，不覆盖页签、按钮、文字或卡槽前景。
- [通过] `CardPoolPanel` 仍为 `1780×630`、`ScrollRect` 仍为 `1650×540`；出战、融合与卡池布局未改。

## UI 与框架边界

- [通过] 已核对 `PreparationView.prefab`、`PreparationView`、`PreparationController`、`PreparationViewUiBuilder`、`Preparation.unity`、`Preparation.asset`、`EPreparationUiGroup.Main` 与 `PreparationStage` 链路。
- [通过] 花纹由一一对应的 `PreparationViewUiBuilder.Build()` 创建，并通过 Unity Editor 执行 Builder 重建 Prefab；Controller 无运行时花纹代码。
- [通过] 花纹由 36 个静态 `Image` 图形组成，全部 `raycastTarget=false`；专项测试验证其位于 `ScrollRect` 之前。
- [通过] `Preparation.unity` 与 `Preparation.asset` 无差异；未改 Group、DefaultShow、场景位置、缩放、Pivot 或导出字段，因此不需要重导出。
- [通过] 框架边界审计通过：沿用既有 View/Controller、UiBuilder、Resources 与 UiScene 配置源；未添加平行页面、运行时静态拼装或手写 YAML。
- [通过] 保留共享工作区既有修改；`git diff --name-only` 未发现 `.meta` 变更。

## 验证与文档

- [通过] Unity Builder 执行成功；Prefab 结构与测试确认 `1600×500` 花纹层、6 个菱形、12 个小点、36 个无射线 Image 及正确层级。
- [通过] 已生成并查看 `AutoDoc/Temp/PreparationBluePanelPattern-Preview.png`；1920×1080 下纹样清淡、稀疏且不遮挡内容。
- [通过] Builder 与测试脚本校验均为 0 error / 0 warning；3 项定向测试及 25 项 RunCard/Preparation 回归测试全部通过；按项目默认边界未进入 Play Mode。
- [通过] 玩家视角设计文档：已完整读取 `design-doc-format`，核对并更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md` 的蓝色底板可见表现。
- [通过] 美术文档：已完整读取 `art-doc-writer` 与模块格式，核对并更新 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md` 的数量、颜色与透明度规格；既有 `UI-STYLE-001` 足以覆盖，无需修改 UI 总览。
- [通过] 程序文档：已完整读取 `program-doc-format` 与 UI 界面格式，核对并更新 `AutoDoc/Program/UI/preparation/preparation.md` 的静态层级、绘制组成与射线边界。

## 收尾

- [通过] 结束前已重新打开清单，并逐项写入实现、Prefab、预览、测试、框架和文档证据。
- [待执行] 只运行一次 `AutoDoc/CleanupTempDocs.bat` 并记录退出结果。
- [待执行] 清理后创建 `AutoDoc/Temp/PreparationBluePanelPattern-Report.md`。
