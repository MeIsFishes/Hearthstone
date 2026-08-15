不通过

# 备战阶段卡牌融合代码审查（Round 1）

## 1. 审查基线

- 目标策划案：`AutoDoc/DesignPlan/2026.08.15/preparation-stage-card-fusion.md`。
- 已审查实施 Plan：`AutoDoc/DesignPlan/Plan/preparation-stage-card-fusion-plan.md`。
- 框架与流程依据：`.codex/private-skills/project-state-preflight/design-plan-implementation.md`、`bbxcommon-ecs`、`game-stage`（含 Load/Unload 与 StartupData 规范）、`bbxcommon-ui`（含页面生命周期、UiScene 编辑场景与导出）、`bbxcommon-ui-item`、`bbxcommon-resource`、`config-data-design`（含 DataApi/CsvData）。
- Git：可用。所有只读命令均使用 `git -c safe.directory=D:/Git/Hearthstone ...`；当前 `HEAD=cd5959a`，本篇追溯起点为 `7c97455`。检查了 `status --short`、工作区/暂存区 name-status、工作区 diff、相关未跟踪文件以及直接相关提交内容；暂存区无本篇改动。
- 基线污染说明：`7c97455..HEAD` 包含连续的任务外卡图替换提交。99 行与表头范围说明已被外部提交 `def6f2c` 一并带入 `BattleCardCsvData.csv`，因此它们在当前工作区 diff 中不可见，但当前 `HEAD` 内容确实存在；本审查将其作为“当前接入证据”而非本轮工作区差异归属证据。当前 CSV 的 39/40 行换行差异、其它 BattleCards 图片/`.meta`、外部 BattleCards 资源索引键均不计入融合实现。
- 明确排除：`Assets/Resources/BbxCommon/LoadingTimeData.asset`；任务外 `BattleCardCsvData.csv` 既有 1～98 行与 `Boar_001` 遗留；非 `FusionCard_099` 的 BattleCards `.meta`/图片；`ResourcesDictionary.json` 中任务外 BattleCards 键；`AutoDoc/Temp/BattleCardArtworkReplacement-*` 及其它 Temp；旧测试 `Expected Boar / actual Boar_001`。
- 本篇实际相关修改/新增范围：
  - 规则/ECS/Stage：`RunStateSingletonRawComponent.cs`、`RunCardRules.cs`、`BattleStageStartupData.cs`、`BattleStages.cs`、`PreparationStages.cs`。
  - 配置：`BattleCardCsvData.cs`、两张 BattleCard CSV、`PreparationStageEntry.asset`。
  - UI 运行时：`PreparationController.cs`、`PreparationCardItemController.cs`、`PreparationSlotItemController.cs`、新增 FusionSlot View/Controller 以及既有 View 字段扩展。
  - UI 配置源/产物：3 个 Builder、`PreparationUiBuilderUtility.cs`、`PreparationView.prefab`、`PreparationCardItem.prefab`、新增 `PreparationFusionSlotItem.prefab`、`Preparation.unity`、`PreLoadUiData.asset`、动态 TMP FontAsset、`ResourcesDictionary.json`；`Preparation.asset` 当前内容保持等价且无工作区 diff。
  - 美术资源：8 张 Preparation 融合 UI Sprite 与 `FusionCard_099.png`，以及 Unity 生成的对应 `.meta`。
  - 测试与现状文档：`BattleRulesTests.cs`、`RunCardRulesTests.cs` 及直接相关 Program/Design/Art 文档。

## 2. 需求与代码实现覆盖表

| 需求 / Plan 项 | 代码、配置或资源落点 | 代码层覆盖状态 |
| --- | --- | --- |
| BatchId → 不可变 canonical payload 账本；融合消耗后同 payload 仍幂等、异 payload 拒绝 | `RunStateSingletonRawComponent.AppliedRewardBatchPayloadFingerprints`；`RunCardRules.CreateRewardBatchPayloadFingerprint/ApplyRewardBatch` | 完成。指纹由 5 项 grant 按 CardNumber 排序后用 invariant 十进制编码，不读取可变 `CardInstances`，回收清账本。 |
| 普通奖励/素材仅 1～98，总持有池 1～99 | `LastOrdinaryCardNumber`/`FusionResultCardNumber`/`LastCardNumber`；`RewardCardGrantStartupData` 与 `RunCardInstanceData` 分别采用不同上限 | 完成。99 不能由奖励直接发放。 |
| 2～4 素材、和为 99、99 唯一；素材设置/移动/替换/移除及非法分支 | `TrySetFusionMaterial`、`TryRemoveFusionMaterial`、`EvaluateFusion` | 完成。选择只写 `PreparationSessionSingletonRawComponent`；无效分支代码上无数组写入和 Dirty。 |
| 预校验、结果预构造、永久攻血求和、一次提交、清出战引用与选择 | `RunCardRules.TryFuse` | 完成。先用 `long` 求和并构造结果，再清素材/出战引用、写 99、清选择，最后发送两个 Revision；监听回调读取时底层数组已全部提交。 |
| 融合不改历史奖励账本；Stage 离开清未确认选择 | Run state / Preparation session 分离；`OnSingletonCollect`；`InitializePreparationRuntime.Unload` | 完成。Session 回收先使 `FusionRevision` 失效，再清 4 槽；Run state 不受未确认选择影响。 |
| 99/type6 配置，且普通/敌方入口不随机产出 99 | `BattleCardCsvData` 99 行、`BattleCardTypeCsvData` type6 行、`RewardCardGrantStartupData` 上限、既有 `BattleRules` 固定 1～98 编号与新增测试约束 | 完成（99 CSV 行归属存在基线污染，但当前接入内容存在）。 |
| 99 进入 Battle 时使用 Run instance 永久值 | `BattleStages.CreatePlayerCards` → `BattleCardRawComponent.InitializePlayer` | 完成。玩家卡攻血来自 `RunCardInstanceData`；没有为 99 建立随机后备或第二条战斗卡链。 |
| 双页签、同一 Session、4 融合槽、99 固定池、拖入/替换/移动/拖回池/非法释放 | `PreparationController`、三个条目 Controller、`UiList`、`UiInteractor`、`PreparationView` | 基本完成，但融合槽空态会残留编号，且“素材已选”显式文本与合计三态强调未接入，见发现 2、3。 |
| 现有 UiScene/Prefab/Builder/PreLoad/Exporter 链 | 三个一一对应 Builder；`UiApi.EditorOperation.PreInitializeView/ExportPreloadedView`；`Preparation.unity` Connected Prefab；`Preparation.asset`；PreLoad 映射 | 完成静态接入。未发现私有反射、内部 Manager 或手写平行 UiScene；实际 Editor 重导过程仍需主代理以执行证据确认。 |
| 9 张正式 Sprite、唯一资源 key、字体字形 | 8 张融合 UI PNG、`FusionCard_099.png`、Unity import `.meta`、`ResourcesDictionary.json`、NotoSansSC Dynamic FontAsset | 部分完成。9 个资源 key 唯一且 Sprite 导入配置存在；但“素材已选”TMP 组合及“已/选”字形未落地，见发现 3。 |
| Production 流程日志覆盖 FUNC-01～10 与 99 Battle | `[PreparationFusion]` 日志位于 `PreparationController`、`PreparationStages`、`BattleStages` | 未完成。日志缺少成功事务的素材/永久攻血快照、页签链的 Stage/发奖计数，以及 Session 回收后的可判定状态，见发现 1。 |
| 验收入口 C# 默认值与 Unity 资产同步为指定融合批次 | `PreparationStageEntryAsset.cs`、`PreparationStageEntry.asset` | 未完成。C# 默认值仍为旧批次，Asset 的 BatchId 也与 Plan 指定值不一致，见发现 4。 |
| 开发期测试保留 1～98 断言并覆盖新增规则/资产 | `BattleRulesTests.cs`、`RunCardRulesTests.cs` | 部分完成。核心幂等、2/3/4 张、原子融合、溢出、持有 99、资源接入均有测试；选择类无效分支的“不 Dirty/不变”断言不完整，见发现 5。 |

## 3. 发现（按严重程度）

### 高：唯一验收方式所依赖的生产日志不足以判定成功事务与关键生命周期

- 位置：`Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs:178`、`:235`；`Assets/Scripts/Hearthstone/GameStage/PreparationStages.cs:62`、`:67`；`Assets/Scripts/Hearthstone/GameStage/BattleStages.cs:131`。
- 证据：
  - `OnFuseClicked` 在 `TryFuse` 返回后才调用通用 `LogFusion`。此时 4 槽已清空；日志只剩 `CardNumber=99`、空槽、聚合后的 Count/Sum/Owned/Revision，没有本次消耗编号集合，也没有各素材融合前的永久 Attack/MaxHealth。
  - 随后的 `FusionResult` 只记录结果卡 `99/Attack/MaxHealth`，仍不能从同一事务日志核对 `14=2/3、20=3/4、30=2/3、35=4/5` 的求和输入。
  - `SelectTab` 日志有 BatchId、Revision 与槽快照，但没有 Plan 指定的 StageGroup 身份、奖励应用结果/次数（现有 `WasNewlyApplied` 也未被日志消费）。
  - `StageUnload` 只在移除 Session 之前记录非空选择，移除后没有日志证明 Session 已不存在/4 槽已回收；后续 Battle 日志只记录玩家 Entity，不能证明未融合素材仍由 Run state 持有。
- 影响：策划案唯一指定“流程log验收”。现有日志不能独立满足 `FUNC-01`、`FUNC-06`、`FUNC-07`、`FUNC-10` 的 Plan 判定条件；尤其无法以流程证据证明成功事务消费了哪几张卡、结果永久值来自哪些输入，以及离 Stage 后选择确已清除。功能代码本身的原子写入成立，但实现范围中的验收日志链未闭环，不能进入正式验收。
- 违反：策划案 6.1/6.3 的 `FUNC-01`、`FUNC-06`、`FUNC-07`、`FUNC-10`；Plan 第 1.1 节日志契约、程序功能验收表与第 3 节日志职责。
- 必须修正：在提交前捕获只读、不可变的融合事务快照（素材槽、每张永久攻血、受影响出战槽），让成功结果日志一次关联输入、消费集合、99 结果与提交后状态；在 Preparation 初始化/页签日志中加入可判定的奖励应用结果/计数与 Stage/Session 身份；在 Session 移除后或后续正式 Stage 初始化中增加可判定的回收后状态。日志只读规则结果，不把验收状态写成第二套权威数据。

### 中：融合槽变空后仍显示旧编号，空态与占用态未完整切换

- 位置：`Assets/Scripts/Hearthstone/Ui/Editor/PreparationFusionSlotItemUiBuilder.cs:27`、`:47`；`Assets/Scripts/Hearthstone/Ui/Controller/PreparationFusionSlotItemController.cs:34`。
- 证据：Builder 把 `CardNumberBadge` 建在条目根节点，而不是 `OccupiedState` 下。`Refresh` 只切换 `EmptyState/OccupiedState`，空槽时在第 44 行直接返回，既不隐藏 Badge 也不清空 `CardNumberText`。因此初始空槽显示 `00`，素材移除/替换/成功融合后更会继续显示上一张素材编号。
- 影响：玩家可见的 4 槽并未真实呈现“槽已清空”，会让融合完成、移除、替换后的状态与权威 Session 不一致；也破坏 ART-02/FUNC-02/FUNC-06 所依赖的槽状态映射。
- 违反：策划案 4.2、4.4；Plan 的 FusionSlot 空/占用态、刷新职责与 ART-02/FUNC-06 接入要求。
- 必须修正：把编号徽章纳入 `OccupiedState`，或将其作为独立序列化状态在 `Refresh` 中随 `occupied` 显隐并清理文本；由 Builder 重新生成 Prefab、执行 Pre-UiInit/Pre-load 导出，并回归移除、替换、融合成功后的空槽刷新。

### 中：素材选中状态和编号合计三态的静态组合未按 Plan 接入

- 位置：`Assets/Scripts/Hearthstone/Ui/Editor/PreparationCardItemUiBuilder.cs:70`；`Assets/Scripts/Hearthstone/Ui/Editor/PreparationUiBuilderUtility.cs:18`；`Assets/Scripts/Hearthstone/Ui/Editor/PreparationViewUiBuilder.cs:156`；`Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs:158`。
- 证据：
  - `MaterialSelected` 仅创建并引用一张无文字 Sprite，没有叠加 Plan 明确要求由 TMP 承载的“素材已选”文本；`RequiredChineseCharacters` 也不含“已”“选”，当前 FontAsset 中这两个 Unicode 字形均不存在。
  - 合计面板只创建一组固定颜色的 Expression/Result TMP；Controller 只改字符串和 Button `interactable`，没有为小于/等于/大于 99 接入 Plan 所述的文字/强调色三态。
- 影响：资源文件虽然齐备，`ART-03/ART-05` 所需的引用组合与状态覆盖在代码/Prefab 层不完整；这不是美术品质判断，而是静态控件与状态驱动缺失。
- 违反：Plan ART-03、ART-05、第 4.1/4.3、5.2/5.3、7.3 节。
- 必须修正：在 `MaterialSelected` 内加入不遮挡编号/攻血的 TMP“素材已选”，把“已/选”加入字体校验并通过 TMP API补字；为合计文本提供明确的 `<99`、`=99`、`>99` 状态映射（可由 Controller 根据同一 `FusionEvaluationData` 刷新颜色/强调引用），再由 Builder 重建相关 Prefab。

### 中：验收入口的 C# 默认值、Asset 与 Plan 未同步

- 位置：`Assets/Scripts/Hearthstone/Editor/GameStage/PreparationStageEntryAsset.cs:26`；`Assets/Resources/Editor/PreparationStageEntry.asset:18`。
- 证据：C# 默认值仍为 `preparation-isolated-001` 与 `2/3/5/6/7`；Unity Asset 虽已改为 `14/20/30/35/54` 及正确攻血，但 BatchId 为 `preparation-fusion-acceptance-001`，不是 Plan 指定的 `fusion-acceptance-001`。
- 影响：新建/重置 Entry 时会回到旧批次，C# 配置源与已保存 Asset 不一致；按 Plan 固定 BatchId 汇总日志时也会发生基线偏差。
- 违反：Plan 第 6.1、6.3、8 步骤 9 对“C# 默认值与 Unity 资产同步”的明确要求。
- 必须修正：用同一常量语义更新 C# 默认 BatchId 与五项 grant，并通过 Unity Editor 正常序列化使现有 Asset 与之完全一致；不得手写 `.asset` YAML。

### 低：开发期测试没有完整锁定选择类无效分支的“不写入、不发 Dirty”契约

- 位置：`Assets/Scripts/Hearthstone/Tests/Editor/RunCardRulesTests.cs:130`、`:233`。
- 证据：现有测试调用了重复、99、未拥有、非法目标槽，但没有在这些调用前后逐项断言 4 槽快照与 `FusionRevision` 不变；也没有覆盖同槽 `NoChange`、空槽移除、非法 sourceFusionSlot、格式损坏 Session 的 Evaluate 拒绝。当前实现静态阅读上未写 Dirty，但 Plan 明确把这些边界列为开发期回归表。
- 影响：后续修改容易在非法分支引入半写入或多发 Dirty，而测试无法报警。
- 违反：Plan 第 2 节开发期测试表、第 8 步骤 3。
- 必须修正：用紧凑的参数化/辅助断言补齐每个选择类无效分支的槽数组、Run Revision、FusionRevision、持有态不变；不要建立新的测试框架。

## 4. 框架边界审计

- ECS：Run 权威持有状态与 Preparation Session 临时选择分离正确；`ListenableVariable` 的 Dirty/Invalid 生命周期、单例回收与 Stage 卸载对称，未把选择复制到 UI。
- GameStage：继续复用 `RunStateStage + PreparationStage/BattleStage` 与生产 Group 入口，没有新增平行 Stage/System/Listener。StartupData 仍为强类型防御性快照；Editor Entry 仍只构造 StartupData 并调用正式 Group。
- UI：使用既有 View/Controller、`UiList`、`UiInteractor`、ModelWrapper 生命周期；动态 99+3+4 条目走框架预载与池化，没有 Controller 运行时拼整页静态层级，没有直接访问 `UiControllerManager`。
- Builder/Exporter：三个 Prefab 各有一一对应 `public static void Build()`；通过 `UiApi.EditorOperation.PreInitializeView/ExportPreloadedView` 公共 API 接入，没有私有反射、MenuItem 或 InitializeOnLoad；场景仍保存 Connected `PreparationView`，`Preparation.asset` 条目与原导出信息一致。
- Data/Resource：静态定义走 CSV/DataApi，Sprite 走 ResourceApi 与唯一文件名 key；未发现业务代码直接访问 ResourceManager 或自造配置容器。
- 结论：未发现框架外实现、兼容补丁、平行系统或需要新增框架能力的缺口。当前阻塞均可在既有公开契约内修正；无需框架迭代授权。
- 过程证据边界：仅凭 Git 内容不能独立证明 Prefab/Scene/PreLoad/ResourcesDictionary/FontAsset 确由 Unity Editor/Exporter 生成，但现有 Builder、场景连接和产物结构相互一致。主代理应保留实际 Editor 调用与等价重导证据；这不构成本轮新的框架缺口。

## 5. 特定需求 trick 汇报

未发现。

- 99 进入 Battle 沿用 `BattleCardRawComponent.InitializePlayer` 与既有卡面 Controller，没有 `if (cardNumber == 99)` 的战斗特判或第二套战斗数据。
- type6 的非随机攻血占位、融合验收入口批次和 `[PreparationFusion]` 结构化日志均来自已审查 Plan 的明确需求，不认定为未声明 trick；但日志必须按发现 1 完成真实生产链闭环。
- canonical payload 未使用 `GetHashCode()`、当前卡实例或融合后状态自证，未发现为单一测试样例硬编码融合结果。

## 6. 超范围与无法确认风险

- 未进入 Unity、Play Mode 或任何游戏流程，未运行验收脚本、未查看验收截图、未对 `ART-`/`FUNC-` 给出实际通过结论；这是本代理职责边界。
- 未执行编译或测试。测试代码只作为覆盖证据静态审查；运行结果应由执行代理/主代理另行记录。
- `BattleCardCsvData.csv` 的 99 行已被任务外提交 `def6f2c` 带入 `HEAD`，将来提交融合实现时应明确记录这一归属，避免误以为融合提交自身包含全部配置变更。
- `ResourcesDictionary.json` 同时包含外部 BattleCards 索引键与本篇融合键，提交时必须继续做任务内/任务外拆分，不能吞入外部未授权范围。
- `Preparation.asset` 当前无 diff，只能确认内容与场景导出关键字段一致；是否在本轮实际完成等价重导，需要主代理的 Editor 过程证据。

## 7. 结论

规则层的 canonical 奖励账本、融合预校验/原子提交、永久攻血、99 Battle 接入和 Stage 生命周期总体设计合理，且严格位于既有 ECS、GameStage、UI、Data/Resource 框架内。但唯一流程 log 验收所需的生产日志链仍缺关键事务输入与回收后证据，融合槽空态存在可见旧编号残留，素材标记/合计状态组合与验收入口默认值也没有按 Plan 完整接入，因此本篇唯一一次代码审查不通过。后续由主代理把成立项交执行代理修正，并自行完成差异核对与针对性验证，不再发起追加代码审查。

本结论仅为代码审查结论，不代表策划案验收通过。
