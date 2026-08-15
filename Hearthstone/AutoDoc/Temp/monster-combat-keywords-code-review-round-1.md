不通过

# 怪物战斗词条与融合继承：代码审查（Round 1）

## 审查基线

- 目标策划案：`AutoDoc/DesignPlan/2026.08.15/monster-combat-keywords.md`
- 已审实施 Plan：`AutoDoc/DesignPlan/Plan/monster-combat-keywords-plan.md`
- 实现流程：`.codex/private-skills/project-state-preflight/design-plan-implementation.md`
- Git：可用；仓库基线为主代理指定的 `6a833b3`，当前是包含其他策划案与任务前遗留内容的混合脏工作区。
- 差异检查：已检查 `git status --short`、`git diff --name-status 6a833b3 --`、工作区 diff、暂存区 diff，并读取本案相关未跟踪文件；暂存区没有可供单独审查的本案差异。
- 本案实际相关范围：`BattleKeywordCsvData*`、`BattleCardTypeCsvData*`、`RunStateSingletonRawComponent.cs`、`RunCardRules.cs`、`BattleCardRawComponent.cs`、`BattleKeywordRules.cs`、`BattleRules.cs`、`BattleSystem.cs`、`BattleScenarioStartupData.cs`、四类动态卡牌 View/Controller/Builder/Prefab、`BattleKeywordRulesTests.cs`，以及 Continue 所有者写入的 `BattleStageStartupData.cs`、`BattleStages.cs`、`HearthstoneGameEngine.cs` 场景接入；共享资源检查覆盖 `PreLoadUiData.asset`、`ResourcesDictionary.json` 和 `NotoSansSC-Dynamic SDF.asset`。
- 未纳入：Continue 事务调度与按钮/页面根、融合案既有功能、任务前 `Boar_001` 断言、外部 BattleCards 图片与框图、`LoadingTimeData`、AGENTS/skills。混合脏工作区无法仅凭 Git 独立确认共享生成物中每一段变更的作者，但可以检查其当前最终内容。

## 需求与代码实现覆盖表

| 需求 / Plan 项 | 代码、配置或资源落点 | 代码层覆盖状态 |
| --- | --- | --- |
| `FUNC-01` 五类初始词条及同源显示/战斗集合 | `BattleCardTypeCsvData.InitialKeyword`、`RunCardInstanceData.Keywords`、`BattleCardRawComponent.Keywords`、四类 Controller | 完成，但配置校验和字体资源存在下述缺口 |
| `FUNC-02/03` 单/多嘲讽候选过滤与随机选择 | `BattleRules.FilterTargetCandidateMask`、`BattleSystem` 候选 mask 与 `TargetRandom` | 完成 |
| `FUNC-04/05` 远射半伤、向下取整、0 伤害、免主目标反伤 | `BattleRules.ScaleDamageFloor/ResolveKeywordDamage`、`BattleSystem` | 当前固定值逻辑完成，但未由 Plan 指定配置驱动 |
| `FUNC-06` 爆裂相邻槽、空/死亡位、延迟死亡 | `BattleRules.GetAdjacentLivingMask`、`BattleSystem` 延迟 `CommitAliveState` | 当前固定值逻辑完成，但距离/比例未由配置驱动 |
| `FUNC-07` 冲锋存活友军本场增益 | `BattleSystem.ApplyCharge`、`BattleCardRawComponent.ApplyBattleStatGain` | 当前固定 `+1/+1` 逻辑完成，但未由配置驱动 |
| `FUNC-08` 融合词条去重并集、None 无贡献、99 禁止素材 | `RunCardRules.TryFuse`、`FusionMaterialSnapshot/FusionTransactionSnapshot`、`BattleKeywordRules.UnionKeywords` | 首次融合代码完成；99 后续融合不可达边界与 Plan 披露一致；显示资源仍有缺口 |
| `FUNC-09/10` 多词条顺序、统一生命/死亡/胜负提交 | `BattleSystem` 冲锋→嘲讽→远射/爆裂→反伤→死亡/结果链 | 当前固定参数下完成 |
| `RGR-06` 保留 `int Attack` 并同步 `AttackValue` | `BattleCardRawComponent.SetAttack/SyncAttackValue`、`BattleCardItemController` | 完成 |
| 强类型场景 DTO、空槽/显式攻血、深拷贝、canonical key | `BattleScenarioStartupData.cs`、`BattleStageStartupData.cs`、`BattleStages.cs`、`HearthstoneGameEngine.cs` | 部分完成；Continue 默认入口错误地提交了非 null Scenario |
| 四类动态条目词条文本与预载/资源索引 | 四个 Prefab/Builder、`PreLoadUiData.asset`、`ResourcesDictionary.json` | 引用和索引完成；四词条布局与字体字形未完成 |
| 独立定向测试 | `BattleKeywordRulesTests.cs` | 已建立，但没有覆盖缺失的完整配置契约与默认 Continue `Scenario=null` 契约 |

## 发现

### 阻塞级 1：词条数值配置没有进入生产规则链，当前实现形成“CSV 展示配置 + 战斗硬编码”双轨

- 位置：
  - `Assets/Scripts/Hearthstone/Config/Csv/BattleKeywordCsvData.cs:8-30`
  - `Assets/Resources/Config/BattleKeywordCsvData.csv:1-7`
  - `Assets/Scripts/Hearthstone/Ecs/System/BattleRules.cs:142-159`
  - `Assets/Scripts/Hearthstone/Ecs/System/BattleSystem.cs:133-149`
- 证据：`BattleKeywordCsvData` 只有 `Keyword/DisplayName/DisplayOrder`，缺少 Plan 明确要求的 `DamageNumerator/Denominator`、`AffectRange`、`AttackGain/MaxHealthGain/CurrentHealthGain`、`PreventsCounterDamage`。远射/爆裂比例在 `ResolveKeywordDamage` 中直接写死为 `1/2`，反伤门控直接绑定 `isLongShot`，冲锋在 `ApplyCharge` 中直接写死 `1,1`，爆裂距离固定为相邻一槽。
- 影响：当前示例数值恰好正确，但配置项不可调且 CSV 并非战斗权威源；配置变更不会影响生产行为，违反同一规则源和 Plan 的配置接入要求，属于只满足本次固定需求的实现方式。
- 违反：Plan 2.2.1、3.1、3.2、步骤 1/3/5，以及策划案第 5 节全部战斗词条配置项。
- 必须修正：扩展 CSV 数据结构和正式 CSV 四行，把比例、范围、增益和反伤门控统一从 `DataApi` 读取；`BattleKeywordRules` 负责完整四行/唯一顺序/合法分母与非负增益校验，`BattleRules/BattleSystem` 只消费已校验配置，不保留同值硬编码后备路径。

### 阻塞级 2：正常 Continue 入口提交了显式 Scenario，违反默认 `Scenario=null` 兼容契约并在 Engine 内复制战斗初始化

- 位置：
  - `Assets/Scripts/Hearthstone/Bootstrap/HearthstoneGameEngine.cs:173-190`
  - `Assets/Scripts/Hearthstone/Bootstrap/HearthstoneGameEngine.cs:224`
  - `Assets/Scripts/Hearthstone/Bootstrap/HearthstoneGameEngine.cs:346-371`
- 证据：`TryContinue` 调用 `CreateContinueBattleScenario(runState)`，预先在 Engine 中读取敌人卡牌/类型配置并滚攻血，随后以非 null Scenario 构造下一战。Plan 明确要求默认 Continue 永远提交 null Scenario，显式 DTO 仅作为可控场景输入；敌方默认随机初始化应继续只在 `InitializeBattleRuntime` 内完成。
- 影响：正常下一关脱离旧默认路径，Engine 与 BattleStage 各维护一份敌方初始化规则；后续默认战斗规则变化可能只改一处而产生偏差，且 `RGR-07` 无法成立。
- 违反：Plan 6.1、步骤 8、`RGR-07` 和“Continue 方不复制词条/战斗初始化解析”的所有权边界。
- 必须修正：正常 Continue 用 `new BattleStageStartupData(targetBattleNumber, rewardBatch)`（或等价 null Scenario）进入；删除 Engine 中仅为默认继续预生成场景的重复初始化。保留 `EnterBattleStageGroup` 对调用方显式 Scenario 的公共接入以及 Stage 内唯一初始化链。

### 重要级 3：类型初始词条校验不完整，并以兼容回退静默接受缺列配置

- 位置：`Assets/Scripts/Hearthstone/Config/Csv/BattleCardTypeCsvData.cs:49-68`
- 证据：缺少 `InitialKeyword` 列时捕获 `KeyNotFoundException` 并默认为 `None`；当前校验只排除未知位，没有拒绝普通类型拥有多个已知词条。`BattleKeywordCsvData` 也只做逐行检查，未验证正式集合恰好四项且 `DisplayOrder` 唯一。
- 影响：格式过期的正式表会静默把全部怪物降为无词条；`Taunt|Charge` 之类普通类型配置会被接受，破坏“一类普通怪物一个初始词条”和稳定显示顺序。
- 违反：Plan 2.2.1、2.3.2、步骤 1 和配置完整性要求；兼容旧自定义表的补丁不能替代正式 schema 迁移。
- 必须修正：正式读取必须要求 `InitialKeyword` 列；普通类型 1～5 仅允许 None 或单一已知位，类型 6 为 None；增加完整四词条与显示顺序唯一校验，并补定向失败用例。

### 重要级 4：共享字体缺少 5 个实际词条字形

- 位置：
  - `Assets/Resources/Fonts/NotoSansSC-Dynamic SDF.asset:2509`（`m_CharacterTable`）
  - `Assets/Resources/Ui/BattleCardItem.prefab:1432`
  - `Assets/Resources/Ui/PreparationCardItem.prefab:964`
  - `Assets/Resources/Ui/PreparationSlotItem.prefab:510`
  - `Assets/Resources/Ui/PreparationFusionSlotItem.prefab:888`
- 证据：四个 Prefab 的 `KeywordText` 已引用该字体；对字体字符表按 Unicode 检查，`嘲/射/冲` 存在，但 `讽(U+8BBD)`、`远(U+8FDC)`、`爆(U+7206)`、`裂(U+88C2)`、`锋(U+950B)` 缺失。
- 影响：五个字可能在运行时显示缺字方框，`0/1/4` 词条资源接入不完整；不能以 Dynamic 模式假定提交后的目标环境必定即时补字来代替 Plan 要求的共享补字。
- 违反：Plan 5.2、7.2、步骤 10 及 `ART-01/FUNC-01/FUNC-08` 的共同资源前提。
- 必须修正：由唯一共享整合通道通过既有 TMP/Editor API 将八个词条字符完整合并并保存，随后只读核对字符表和四 Prefab 字体引用；不得手写 FontAsset YAML。

### 重要级 5：四词条文本仍是单行拼接，未实现 Plan 指定的稳定两行/两列编排

- 位置：
  - `Assets/Scripts/Hearthstone/Ecs/System/BattleKeywordRules.cs:53-70`
  - `Assets/Scripts/Hearthstone/Ui/Editor/BattleCardItemUiBuilder.cs:78-94`
  - `Assets/Scripts/Hearthstone/Ui/Editor/PreparationCardItemUiBuilder.cs:53-58`
  - `Assets/Scripts/Hearthstone/Ui/Editor/PreparationSlotItemUiBuilder.cs:43-48`
  - `Assets/Scripts/Hearthstone/Ui/Editor/PreparationFusionSlotItemUiBuilder.cs:42-47`
- 证据：格式化器始终以 `" · "` 连接为一行，四个 Builder 只靠 Auto Size（最低 8/9）压缩文本，没有稳定换行/两列规则。
- 影响：顺序正确但四词条状态只能缩字挤在单行，与已审 Plan 的可读布局不一致，且测试只断言 Auto Size/最小字号，不能发现该偏差。
- 违反：Plan 4.1、`ART-01` 覆盖与步骤 6/7。
- 必须修正：让统一格式化/表现规则对四词条输出稳定两行排列（仍保持嘲讽→远射→爆裂→冲锋），四个 Builder 使用足以容纳该布局的静态区域；不要在各 Controller 分别拼版。

## 框架边界审计

- 已通过部分：战斗仍由唯一 `BattleSystem` 驱动；没有新增平行 System/StageListener/Task 图；融合使用既有 `RunCardRules` 原子事务；UI 仍通过既有 View/Controller/Builder/PreLoad 体系，未运行时拼静态卡面；Scenario 通过 Stage startup snapshot 和公开 Component 初始化入口接入；未发现手写 Scene/Prefab/`.asset` 导出内容的代码路径。
- 未通过部分：`HearthstoneGameEngine.CreateContinueBattleScenario` 把默认敌人配置读取和攻血生成复制到 Engine，越过 BattleStage 的唯一初始化职责；词条 CSV 与硬编码战斗规则形成双轨配置源。两项必须收敛回现有 Stage/DataApi 框架后才能通过代码审查。
- 框架能力缺口：未发现需要另建框架能力的缺口；现有 DataApi、BattleStage startup、TMP/Builder 流程足以完成修正。策划案实现流程已默认授权，无需另行取得用户许可。

## 特定需求 trick 汇报

- 已发现：`BattleRules.ResolveKeywordDamage` 和 `BattleSystem.ApplyCharge` 用本次需求的固定 `1/2`、`+1/+1`、远射免反伤条件绕过已规划 CSV 配置源；位置见阻塞级 1。
- 已发现：正常 Continue 先在 Engine 构造显式 Scenario，使表面上复用可控场景入口，但实质复制默认 Battle 初始化并规避 `Scenario=null` 兼容路径；位置见阻塞级 2。
- 未发现通过卡号 99/type 6 反推融合词条、重复叠层、直接写验收 Entity/Component、绕过 UiList 或新增 Task 图的 trick。

## 超范围与无法确认风险

- 当前工作区混有其他策划案和外部美术改动；本审查没有把外部 BattleCards PNG、框图、`LoadingTimeData`、AGENTS/skills 或 Continue 页面根差异归入本案。
- 本轮按用户最新要求不进入游戏、不执行策划案验收；这里的覆盖状态只表示静态调用链与资源接入证据，不能证明玩家可见效果或运行时验收通过。
- K1～K4 正式 Entry 属于已取消的进游戏验收支撑，本轮未将其缺失作为代码逻辑阻塞；显式 Scenario 的公共生产接入仍按代码检查。
- 任务前 `Boar_001` 旧断言不属于本案。

本结论仅为代码审查结论，不代表策划案验收通过。
