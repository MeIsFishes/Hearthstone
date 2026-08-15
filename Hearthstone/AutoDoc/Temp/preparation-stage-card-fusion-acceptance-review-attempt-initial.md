# 备战阶段卡牌融合首次正式验收失败记录

- 时间：2026-08-15
- 正式入口：`Assets/Resources/Editor/PreparationStageEntry.asset`，由 `GameStageEntryLauncher.Start` 启动；因 Unity 编辑器不在前台，本次 Play 临时设置 `Application.runInBackground=true` 后 Player Loop 正常运行。
- 已到达状态：生产 `{RunStateStage, PreparationStage}` 已建立，日志记录 `BatchId=fusion-acceptance-001`、奖励 `14/20/30/35/54`、`Owned=8`、`RewardApplyResult=Applied`。
- 主干失败：Preparation UI 创建 Controller 时在 `PreparationController.OnUiInit:42` 空引用；随后 `UiList.UpdateTranslation:510` 持续空引用，动态卡池与融合槽均未建立，Trip A 无法开始。
- 直接证据：Unity Console 首个异常栈为 `PreparationController.OnUiInit -> FusionAreaInteractor.Wrapper.OnInteract`；运行态字段 `FusionAreaInteractor` 本身非空，但其 `Wrapper` 未初始化。Prefab 中 FusionOperationRoot 默认 inactive，框架初始化遍历未覆盖其内部 `UiInteractor/UiList`，而 Controller 在 `OnUiInit` 立即访问。
- 影响：`ART-01`～`ART-06`、`FUNC-01`～`FUNC-10` 与关键回归均不能在本趟判定通过；本次正式验收结论为主干失败。
- 修正方向：Builder/Prefab 保持 FusionOperationRoot 在 Ui Init 阶段 active，待 `OnUiOpen` 默认选择“出战”页签时再由生产 Controller 隐藏；重建 View/UiScene/Exporter，验证所有融合 Root 内组件已 PreUiInit/UiInit，Main 不脏、Console error=0。按用户“审查最多一次”约束，本趟修正不追加代码审查，由主代理核对后重新验收。
