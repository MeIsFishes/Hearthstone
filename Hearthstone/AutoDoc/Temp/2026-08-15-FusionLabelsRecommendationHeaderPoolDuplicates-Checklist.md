# 融合数值样式、推荐页精简与牌库重复卡排列检查清单

- [x] 通过——两个 `216 × 72` 数值底板均引用不透明主体的白色木纹 `PreparationPoolEmptySlot`，常态文字和 `FusionUnderTargetColor` 均为黑色。
- [x] 通过——Builder 默认值与 Controller 刷新值分别为“当前点数  0 / 当前点数  数值”和“剩余点数  99 / 剩余点数  数值”，均使用双空格且不含冒号。
- [x] 通过——智能推荐面板不再创建 `Title`、`Hint` 节点，Unity Prefab 检查结果两者均为 `False`；结果区扩大为 `1060 × 560` 并上移到 `(0,-20)`。
- [x] 通过——Run state 保存同编号附加实例，固定奖励配置与批次允许重复卡号；牌库以卡号外层循环、副本序号内层循环展开，因此同号连续且后续编号顺延。
- [x] 通过——零副本编号仍创建一个空态，99 仍按卡号顺序位于 98/100 之间；查看拥有跳过零副本并保留全部副本；出战/融合继续使用原卡号型槽位规则，UiList 负责创建、回收、布局与滚动 Content 高度。
- [x] 通过——已核对 `PreparationView`、`PreparationController`、`BattleCardItemController`、`PreparationView.prefab`、`PreparationViewUiBuilder`、`Preparation.unity`、`Preparation.asset`、`EPreparationUiGroup.Main` 与 `PreparationStage` 链路；场景级导出字段未变。
- [x] 通过——直接复用现有 `PreparationPoolEmptySlot.png`，未新增、编辑或复制图片资产。
- [x] 通过——测试覆盖白色木纹 Sprite、黑色文字、无冒号文案、推荐页无标题提示、卡池副本内层排序、重复奖励保存、固定配置重复编号及融合消耗单个副本。
- [x] 通过——静态控件归 Builder、动态副本列表归 Controller/UiList、权威副本数据归 RawComponent/RunCardRules；Prefab 由 Unity Editor Builder 生成，未手写 YAML；UiSceneAsset 未改。
- [x] 通过——新增的副本计数、读取、追加和移除接口同时服务奖励、融合与 UI，具有明确复用职责；未自行管理 UiList 池，既有监听和交互生命周期保持不变。
- [x] 通过——已按 `design-doc-format` 更新玩家视角备战卡池文档，记录数值样式、无标题推荐页和同号副本连续排列。
- [x] 通过——已按 `art-doc-writer` 及 UI/模块格式更新 UI 总览和备战美术模块文档，记录白色木纹底板、黑字、旧底框停用及推荐页精简。
- [x] 通过——已按 `program-doc-format` 更新备战 UI 文档，并同步备战卡池程序文档中的副本存储、奖励应用、固定配置与融合消耗规则。
- [x] 通过——Unity 编译完成；Prefab 实例检查确认文案、黑色、Sprite、无标题提示和结果区尺寸；`RunCardRulesTests` 31/31、重复配置/融合副本/Prefab 三项重点测试 3/3、随机奖励相关测试 2/2 通过；最终 Console 错误为 0；代码、配置与文档 `git diff --check` 通过。完整 EditMode 套件另有两个与本任务无关的既存失败，已记录在报告风险中。
- [x] 通过——结束审计后已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0；随后创建同名 Report。
