# 图鉴融合模拟与打开动画任务报告

## 任务结果

已完成图鉴融合卡模拟与点击打开动画：

- 新增公共 `BattleCardSimulationFactory`，统一普通卡随机实例和融合卡素材模拟；敌方生成与图鉴融合预览复用同一入口。
- 图鉴融合卡按配方选择互不重复的普通素材、滚动素材攻防，再调用既有真实融合规则生成攻击、生命、词条、等阶与表现来源。
- 图鉴使用按卡号派生的固定非零随机种子，因此同一融合卡的预览属性稳定。
- 点击已解锁卡后，预览从被点击卡位的世界坐标、`0.8` 倍缩放开始，在 `0.28s` 内三次缓出至屏幕中央并放大到 `2.0` 倍。
- 打开阶段禁止点击蒙板收纳；打开完成后继续使用原有 `0.36s`、缩至 `0.3` 并移出屏幕底部的收入口袋动画和皮革音效。

## 检查项结果与证据

1. 融合权威规则：通过。公共模拟入口最终调用 `RunCardRules.TryCreateFusionResultInstance()`，没有复制攻防、词条或等阶结算规则。
2. 公共模拟复用：通过。`EnemyCardFactory.Create()` 委托 `BattleCardSimulationFactory.Create()`；图鉴委托 `CreateDeterministic()`。
3. 图鉴融合属性：通过。模拟素材卡号互不重复，并按素材类型配置滚动攻防；卡面绑定模拟结果的攻击、最大生命、词条和等阶。
4. 范围保持：通过。普通卡显示逻辑不变；四卡融合结果仍由现有目录过滤排除。
5. 打开动画：通过。卡项回传 `RectTransform`，预览从点击位置移动并放大到中心。
6. 交互互锁：通过。`m_Opening` 期间蒙板按钮不可交互，完成后才允许触发收纳。
7. 框架边界审计：通过。继续使用既有 View、Prefab、Controller、`UiList` 和对象池；没有运行时搭建静态整页 UI，也没有平行实现融合规则。
8. 抽象审计：通过。公共工厂同时服务敌方卡生成与图鉴预览，具备实际复用价值；没有新增一次性包装层。

## 验证结果

- Unity 脚本刷新与编译：成功。
- `CardCollectionTests`：8/8 通过。
- `BattleRulesTests.EnemyFusionCardRollsBaseMaterialsThenUsesSharedFusionComposition`：1/1 通过。
- 完整 EditMode：104/108 通过，没有本任务新增失败。
- 完整回归保留 4 个任务前已存在、与本需求无关的失败：
  - `BattleKeywordRulesTests.ScenarioDataSupportsEmptyAndExplicitSlotsWithDefensiveCopies`
  - `BattleRulesTests.LivingAttackerSequenceWrapsFromLeftToRight`
  - `PreparationContinueTests.ContinueLineup_DefensivelyCapturesSparseSlots(3)`
  - `RunCardRulesTests.TryPlaceCard_ReplacesAndMovesWithoutDuplicates`
- 最终 Unity Console：0 条错误。
- 按项目默认要求未进入 Play Mode。

## 文档处理

- 玩家视角设计文档：更新 `AutoDoc/Design/Specific/meta-progression/meta-progression.md`，记录融合卡稳定模拟和点击位置到中央的玩家可见流程。
- 程序文档：更新 `AutoDoc/Program/UI/card-collection/card-collection.md` 与 `AutoDoc/Program/Specific/combat-system/combat-system.md`，记录公共模拟工厂、固定种子与打开动画状态。
- 美术文档：不适用。本任务没有修改图片资产、视觉风格、生成规格或静态 UI 结构。

## 执行偏差与未解决风险

- 未进行 Play Mode 人工视觉验收；动画行为由 Controller 实现、源码约束测试、编译与 EditMode 回归确认。
- 工作区存在本任务开始前的其他未提交修改；本任务未回退或覆盖这些修改。
- 完整 EditMode 的 4 个既有失败未在本任务中扩大范围修复。

## 清理结果

`AutoDoc/CleanupTempDocs.bat` 在本任务结束阶段仅执行一次，退出码为 0；随后创建本报告。
