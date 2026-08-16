# 备战槽位与融合点数底板调整检查清单

- [x] 通过：已定位 `PreparationView.prefab`、`PreparationViewUiBuilder`、`BattleCardItem.prefab`、`BattleCardItemUiBuilder`、`PreparationController`、`Preparation.unity`、`Preparation.asset`、`EPreparationUiGroup.Main` 与 `PreparationStage`。
- [x] 通过：出战空槽和融合空槽均复用共享卡 Prefab 中已有的 `PreparationPoolEmptySlot.png`，未新增槽位资源或平行 View。
- [x] 通过：出战槽可见填充比例统一为 `0.82`，Prefab/运行参数实测中心距 `205 px`、可见宽度 `168.1 px`。
- [x] 通过：融合槽可见填充比例统一为 `0.82`，Prefab/运行参数实测中心距 `190 px`、可见宽度 `155.8 px`。
- [x] 通过：检查 `UiList.ConstantSlot` 计算确认相邻中心距等于 `ConstantSlotSize.x`；Editor 测试显式断言出战间距约 `36.9 px`、融合间距 `34.2 px`，均大于 0。
- [x] 通过：Prefab 实测两个底板均为 `280 × 72`；标签横向边界 `-128~30 px`，数字边界 `46~124 px`，均位于底板 `-140~140 px` 内。
- [x] 通过：点数底板改用现有横向 `PreparationFusionSumPanel.png`，并设置 `preserveAspect=false`，图像实际覆盖完整 `280 px` 宽度，不再被竖图比例压窄。
- [x] 通过：通过 Unity 正式通道执行 `BattleCardItemUiBuilder.Build()` 与 `PreparationViewUiBuilder.Build()`，随后保存、刷新并重新加载 Prefab 核验；未手写 Prefab YAML。
- [x] 不适用：本次只改页面内部控件、Builder 与 Prefab，未改变 UiGroup、DefaultShow、场景级 Position/Scale/Pivot 或导出路径；`Preparation.asset` 无需重新导出，也未直接编辑。
- [x] 通过：直接相关 EditMode 2/2、完整 EditMode 90/90；两个程序集顺序构建均 0 error；Unity Console 清理测试预期日志后 0 error。按项目默认约定未进入 Play Mode。
- [x] 通过：继续使用既有 View/Controller、共享 `BattleCardItemController`、`UiList`、对象池、UiBuilder 与预加载映射；无运行时静态拼装、手写导出数据或框架绕行。
- [x] 通过：目标 diff 与结构检查未发现无关业务改动；共享 `PreparationSlotVisualFillRatio` 同时服务两种槽位，未增加一次性函数或字段。生成的 Unity YAML 保留 Unity 标准空值尾随空格，代码与文档的定向 `git diff --check` 通过。
- [x] 通过：已完整读取 `design-doc-format`，并同步 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md` 的共享空槽、缩小槽位、正间距与点数底板覆盖现状。
- [x] 通过：已完整读取 `art-doc-writer` 及模块格式，更新 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md` 的尺寸、间距、资源引用与资产状态；UI 总览风格分组未变化，不需改动。
- [x] 通过：已完整读取 `program-doc-format` 及 UI 界面格式，更新 `AutoDoc/Program/UI/preparation/preparation.md` 的布局计算、共享空槽和底板边界现状。
- [x] 通过：本清单已逐项复核并记录证据；下一步只运行一次 `AutoDoc/CleanupTempDocs.bat`，随后创建对应报告。
