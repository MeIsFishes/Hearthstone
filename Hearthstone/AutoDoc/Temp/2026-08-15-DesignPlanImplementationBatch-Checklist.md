# 2026-08-15 策划案实施批次检查清单

## 任务范围与候选确认

- [x] 用户补充授权：本批次实施期间遇到的必要改动由主代理自行决策，无需逐项等待授权；仍须限制在四篇策划案范围内并遵守框架、审查、验收和任务外改动保护规则。
  - 证据：用户明确说明“期间遇到的改动你都可以自行决策，不用等我授权”。
- [x] 用户补充进度约束：从当前融合案起及后续策划案，各类独立审查每篇最多执行一次；审查发现由执行子代理修正后主代理直接核对，不再追加同类复审，验收修正也不再额外调用审查代理。
  - 证据：用户明确说明“接下来的案子请加快进度，各类审查都最多只做一次”。当前因 turn 中断而消失且未产出报告的审查代理不计为有效审查；正在运行的恢复审查是融合案唯一一次代码审查。
- [x] 用户确认子代理具备完整 MCP 访问权限；后续 Unity 操作无需为通道权限再次询问，可直接按项目限定的官方 MCP for Unity 或允许的 Editor 通道执行。
  - 证据：用户明确说明“子代理拥有完整的MCP访问权限，不用再问了”。该授权不改变正式 StageGroup、Builder/Exporter、禁止手写 Unity YAML/`.meta` 等框架边界。
- [x] 任务分类为“实现策划案”，严格执行 `design-plan-implementation.md` 的 a～h 门槛。
  - 证据：已完整读取 `project-state-preflight/SKILL.md` 与 `design-plan-implementation.md`，用户请求为“现在开始实施这些策划案”。
- [x] 明确纳入以下四篇正式策划案，并记录其初始状态均为 `In Design`；本轮用户“现在开始实施这些策划案”构成明确实施授权：
  - `AutoDoc/DesignPlan/2026.08.15/preparation-stage-card-pool.md`（P0）
  - `AutoDoc/DesignPlan/2026.08.15/preparation-stage-card-fusion.md`（P0）
  - `AutoDoc/DesignPlan/2026.08.15/preparation-stage-continue.md`（P1）
  - `AutoDoc/DesignPlan/2026.08.15/monster-combat-keywords.md`（P1）
- [x] 核对四篇文件存在、严格三行文件头合法、状态与优先级合法，且不误纳入 `Plan/`、`Review/` 或普通临时方案。
  - 证据：四篇均位于 `AutoDoc/DesignPlan/2026.08.15/`，前三行为合法 `title/state/priority`；两篇 P0、两篇 P1，初始状态均为 `In Design`；当前无 `DesignPlan/Plan` 或 `DesignPlan/Review` 目录。
- [x] 阅读四篇需求及判断依赖所需的当前代码、配置、资源和现状文档；调查阶段不提前修改策划案状态或实施内容。
  - 证据：已读取四篇正文、现有战斗 Program/Design/Art 文档、`Assets/Scripts/Hearthstone` 运行结构、两张卡牌 CSV、现有 Stage/UI/测试入口与相关资源清单；调查完成前未修改策划案状态。
- [x] 记录排序与并行组：`卡池编成 → 卡牌融合 →（继续下一关 ∥ 怪物战斗词条）`。
- [x] 记录排序依据：卡池编成提供备战 Stage、持有卡池与出战状态；融合依赖并扩展该基础；继续依赖出战状态并消费融合未确认选择；词条完整验收依赖融合产物。1、2 完成后，3、4 仅在实施 Plan 证明修改范围不交叠时并行。
  - 证据：当前 `HearthstoneGameEngine` 只进入 `BattleStage`，不存在备战 Stage/页面/持有卡池；融合正文声明共享卡池，继续正文声明依赖编成并消费未确认融合选择，词条正文含融合产物继承。
- [x] 记录任务开始前 Git 脏工作区及归属，不覆盖、回退或误提交用户既有改动。
  - 基线：`Assets/Resources/BbxCommon/LoadingTimeData.asset`、`Assets/Resources/ResourcesDictionary.json` 已修改；`Assets/Resources/Art/BattleCards/GoblinWarrior_004.png.meta`、`AutoDoc/Temp/2026-08-15-BattleCardFrameHierarchyAspectGit-Checklist.md` 未跟踪。它们在本批次实质实现前已存在，默认视为任务外遗留；本批次清单自身除外。
  - 并发更新：步骤 a 等待 Plan 期间又出现 `Assets/Resources/Ui/BattleCardItem.prefab`、`Assets/Scripts/Hearthstone/Tests/Editor/BattleRulesTests.cs`、`Assets/Scripts/Hearthstone/Ui/Editor/BattleCardItemUiBuilder.cs` 的修改，以及 `GoblinArcher_005.png.meta`、`GoblinArcher_006.png.meta`；均视为任务外并发改动，已通知执行子代理避让并适配当前接口。

## 单篇 a～h 门槛：备战阶段卡池编成

- [x] a：开始制定 Plan 前由主代理将状态从 `In Design` 改为 `In Progress`，复核严格三行文件头，并记录时间、原状态和恢复点。
  - 证据：2026-08-15 05:25:13 +08:00 完成切换；原状态 `In Design`，当前严格三行文件头为标题、`state: In Progress`、`priority: P0`；恢复点为步骤 a“尚未创建实施 Plan”。
- [x] a：由执行子代理按 `game-module-design`、`plan-output-format` 及必要项目 skill 创建 `AutoDoc/DesignPlan/Plan/preparation-stage-card-pool-plan.md`；分别覆盖验收方式、`ART-01`～`ART-07`、`FUNC-01`～`FUNC-08` 和旧功能回归。
  - 证据：替补执行子代理 `/root/card_pool_plan_writer` 创建 185 行 Plan；先记录“游戏内截图”，分别映射 ART-01～07 与 FUNC-01～08，并列出旧功能回归、框架边界与实现 Todo。原执行代理因调查耗时异常被中断且未写文件。
- [x] b：由 `design_plan_plan_reviewer` 独立只读审查 Plan；主代理评估意见，执行子代理修正成立项，硬阻塞清零后才实施。
  - 首次唯一审查证据：`/root/card_pool_plan_review` 结论“不通过”；成立的硬阻塞为跨 Stage 持有状态被卸载、FUNC-01 用两个 Editor 入口替代真实自动转换、奖励批次无正式调用方/幂等契约、完整卡面永久攻血无唯一来源。已交执行子代理修订原 Plan，不追加第二次同类 reviewer。
  - 修正核对：执行子代理在原路径修订为 230 行 Plan；主代理逐项复核已加入持久 `RunStateStage`、完整 `{run,battle}`/`{run,preparation}` Group、已分配 grant+BatchId 深拷贝、结果 Listener 自动切换、原子幂等、永久攻血、Battle 初始化改动、明确 UI 交互配置与同次 Play 截图闭环；原审查硬阻塞清零，准予进入步骤 c。
- [x] c：由同一执行子代理按通过的 Plan 实现，记录静态检查与编译证据，不建立平行框架或大型测试模块。
  - 证据：执行子代理完成 RunState/Preparation Stage、奖励批次与幂等、战斗终局自动切换、永久卡牌实例、三套 UI/Builder/PreLoad/编辑场景/Exporter/UiSceneAsset/Editor entry、11 张正式 Sprite 及现状文档；Unity 编译完成、Console error=0，`RunCardRulesTests` 4/4 通过，相关资产链结构检查通过。
  - 开发期遗留：全套 Hearthstone EditMode 19/20；唯一失败为任务前 `BattleCardCsvData.csv` 卡1 `ArtworkKey=Boar_001` 与既有 `BattleRulesTests` 断言 `Boar` 不一致，本篇未修改二者，交 code reviewer 与最终回归审计确认归属。
- [x] d：由 `design_plan_code_reviewer` 完成首次全面代码审查；不通过时仅按规则修正并最多追加一次初始阶段复审。
  - 首轮证据：`AutoDoc/Temp/preparation-stage-card-pool-code-review-round-1.md` 结论“不通过”；成立项为 StageGroup 排队重入协调缺失、`UiInteractor` 拖拽触摸目标未复位、Preparation Builder 私有反射越过公开导出边界、卡池列宽与滚动轨道布局偏离 Plan。已交原执行子代理统一修正，尚未进入正式验收。
  - [x] 修正 StageGroup requested/loading/active 协调，并覆盖同帧重复入口及 OnAwake/Editor entry 交叠。
  - [x] 在 BbxCommon `UiInteractor` 生命周期内补齐 TouchEnd/清空，回归重复拖向同槽与既有调用方。
  - [x] 删除业务 Builder 私有反射，以框架公开、强类型 Editor 导出入口完成 PreUiInit/PreLoad。
  - [x] 按 viewport 可用宽度推导 7 列并收敛滚动轨道矩形，重跑 Builder/Exporter 后复审。
  - 最终证据：`AutoDoc/Temp/preparation-stage-card-pool-code-review-round-2.md` 结论“通过”；相关测试 7/7、全 EditMode 22/23，唯一失败仍为任务外 `Boar_001`；Main 干净、Console error=0、三 Prefab/UiScene/Exporter/PreLoad 已重建。
- [x] e：正式验收前记录当前并行列表、a～d 状态、可合并性、逐类验收列表、主干/非主干和统一验收趟次映射。
  - 当前并行列表：只有“备战阶段卡池编成”一篇，所属单篇串行组；a～d 全部完成且无代码硬阻塞，具备验收条件。融合尚未启动，继续/词条并行组须等待前两篇 Completed。
  - 合并判断：无其它策划案可合并；本篇 A～D 共享 `InitialStageEntryAsset` 同一次正式 Play 与 Preparation 页面状态，允许在同一 Play 内连续取证，但美术/功能 case 分开结论。
  - 美术主干：`ART-01`～`ART-07` 全部为主干；分别覆盖标题、3 槽与分隔线、卡池框/滚动条、7×2 首屏卡位、整体视觉、拖起/高亮、正式资产齐备性，证据为本次游戏内截图及其实际资源引用。
  - 功能主干：`FUNC-01`～`FUNC-08` 全部为主干；覆盖战斗终局自动进入/单次5张、01～98固定位置、滚至98、拖入空槽、替换、换槽、无效拖放、满槽与单卡唯一。`RGR-01`～`RGR-05` 均按关键回归处理；已知任务外 `Boar_001` 失败单独归属，不冒充本篇回归通过。
  - 统一验收预计 4 趟，均使用“游戏内截图”：A 从正式 Initial entry 进入同一 Play，等待真实 Battle 终局自动切 Preparation，截战斗终局/备战首屏/滚至98/同 batch 重提；B 拖卡悬停并释放到空槽；C 替换占用槽并保留原卡卡池位置；D 换槽、无效区释放及满槽重复换槽。证据目录统一为 `AutoDoc/DesignPlan/media/2026.08.15/preparation-stage-card-pool/review/`，文件名按 trip/case 使用英文小写 kebab-case。
- [x] f：主代理按策划指定“游戏内截图”通过正式 StageGroup 入口实际操作并验收；截图归档到本篇 `media/.../review/`，分别判定 `ART-` 与 `FUNC-`，失败时最多执行两趟修正闭环。
  - 证据：主代理使用正式 `InitialStageEntryAsset` 与 `GameStageEntryLauncher` 在实际 Play 中完成 4 趟操作；`ART-01`～`ART-07`、`FUNC-01`～`FUNC-08`、`RGR-01`～`RGR-05` 均有逐项映射并通过。首次验收发现中文字体方块，修正趟次 1 接入既有动态 NotoSansSC 并经 round-3 复审通过；重验发现滚动池条目越界，修正趟次 2 接入标准 `Mask`+`RectMask2D`，round-4 指出的自证式测试已在同趟删除主动 `Cull` 后由主代理核对，实际运行滚至 98 时首项图形 10/10 被裁切。正式截图共 14 张，位于 `AutoDoc/DesignPlan/media/2026.08.15/preparation-stage-card-pool/review/`。
- [x] f：主代理创建并复读 `AutoDoc/DesignPlan/Review/preparation-stage-card-pool-review.md`，符合固定结构且证据自足。
  - 证据：Review 从 `## 1. 实现方式` 开始，包含 2.1/2.2/2.3 与第 4 章；逐项映射稳定编号、实际操作及正式截图，最终结论为“通过”，引用的全部媒体路径均存在。
- [x] g：根据正式 Review 设置 `Completed` 或 `Warning`，写入合法 `plan:`、`review:` 路径，同步直接相关现状文档。
  - 证据：正文已写入 `state: Completed`、合法 `plan:`/`review:` 五行文件头；备战程序/UI/设计/美术现状及受影响战斗文档已同步，均只描述现有实现。
- [x] h：只暂存本篇明确归属文件并提交；在唯一 upstream 存在时普通推送，记录 commit、分支、upstream、push 和提交后状态；关闭本篇全部子代理。
  - Git 证据：Git 根 `D:/Git/Hearthstone`，分支 `main`，唯一 upstream `origin/main`；显式暂存 116 个本篇文件，混合的 `LoadingTimeData.asset` 与 `ResourcesDictionary.json` 仅在索引中纳入本篇新增项，未纳入任务外计时/卡图键；提交 `7c97455 feat: implement preparation card pool stage`，普通 push 成功（`9437d6f..7c97455 main -> main`）。
  - 提交后状态：仅保留任务外 `LoadingTimeData.asset`、`BattleCardCsvData.csv`、`ResourcesDictionary.json`、28 个 BattleCards `.png.meta` 与本批次 `AutoDoc/Temp/` 工作材料；本篇正式产物无未提交残留。
  - 子代理关闭证据：`list_agents` 显示 `/root/card_pool_plan_writer`、`/root/card_pool_code_review` 均为 completed，`/root/card_pool_executor` 为 interrupted；本篇无 running 子代理，不复用于下一篇。

## 单篇 a～h 门槛：备战阶段卡牌融合

- [x] 仅在卡池编成进入 `Completed` 后启动本篇；若前置为 `Warning` 或未完成则不得启动。
  - 证据：前置正文为 `state: Completed`，Plan/Review 路径有效；提交 `7c97455` 已推送到 `origin/main` 后才启动本篇。
- [x] a：开始制定 Plan 前由主代理将状态从 `In Design` 改为 `In Progress`，复核文件头并记录状态切换。
  - 证据：2026-08-15 08:58:55 +08:00 完成切换；原状态 `In Design`，当前严格三行文件头为标题、`state: In Progress`、`priority: P0`；恢复点为步骤 a“尚未创建实施 Plan”。
- [x] a：由新执行子代理创建 `AutoDoc/DesignPlan/Plan/preparation-stage-card-fusion-plan.md`；分别覆盖策划指定验收方式、全部稳定 `ART-`、`FUNC-` 和回归点。
  - 证据：新执行子代理 `/root/fusion_executor` 创建 233 行 Plan，本轮未提前实施；开头记录“流程log验收”，单独映射 `ART-01`～`ART-06`、`FUNC-01`～`FUNC-10` 与 `RGR-01`～`RGR-05`，并收敛唯一 RunState、PreparationSession 瞬态 4 槽、原子融合、固定 99 池位、页签 UI、正式资源与 Builder/Exporter 流程。
- [x] b：由 Plan reviewer 独立审查一次，执行子代理修正成立项，主代理核对无硬阻塞。
  - 唯一审查证据：`/root/fusion_plan_review` 结论“不通过”；成立项为奖励 Batch 历史与可变持有态耦合导致融合后幂等失效、FUNC-09 验收奖励无法在持有 99 后再组成合法 99、99/type6 会使既有配置测试确定失败、99 入下一场 Battle 缺少实际回归链、现状原画校验表述不准确。已交原执行子代理修订同一 Plan，不追加第二次 Plan reviewer。
  - 修正核对：原 Plan 已修订为 249 行；主代理逐项复核不可变 canonical payload 账本与两项幂等测试、验收第五码 54 与 `1+4+40+54=99` 二次合法组合、BattleRulesTests 对 1～98 保持和 99/type6 精确新增断言、融合后 99 进入生产 Battle 的 `99/11/15` 日志链、原画校验真实责任均已落到明确步骤。原审查硬阻塞清零，准予进入步骤 c。
- [x] c：由执行子代理实现已审查 Plan，并记录静态检查、编译和范围证据。
  - 证据：`/root/fusion_executor` 完成不可变奖励账本、原子融合规则、99/type6、双页签与 4 融合槽、固定 99 池位、结构化流程日志、9 张 imagegen 正式位图、Prefab/UiScene/PreLoad/Exporter、入口与现状文档；未进入正式 Play 验收。
  - 开发验证：Unity 编译与最终 Console error=0；`RunCardRulesTests` 17/17，99/type6 与普通/敌方边界 2/2；全 EditMode 32/33，唯一失败仍为任务外 `BattleRulesTests.RuntimeResourcesContainBattleCardCsvAndStageDataRegistry` 的 `Boar` / 外部 `Boar_001` 旧断言。Main `dirty=False`；Preparation Scene `dirty=False canvas=1 exporter=1 connectedViews=1`；FusionSlot PreLoad 与 9 张 Sprite 均可加载。
  - 并发基线：实现期间外部美术任务连续提交卡图并把 `main/origin/main` 推进到 `6a833b3`；其中 CSV 随卡图提交已包含 99 行。执行代理保留全部外部 1～98/卡图/资源索引内容，未回退或提交；代码审查以当前 HEAD `6a833b3` 的工作区差异为直接基线，并保留本篇起点 `7c97455` 作为追溯基线。
- [x] d：由 code reviewer 完成全面审查；按用户新增约束，本篇只执行这一次，成立项修正后由主代理直接核对。
  - 唯一审查证据：`AutoDoc/Temp/preparation-stage-card-fusion-code-review-round-1.md` 结论“不通过”；规则层 canonical 账本、原子融合、99 Battle、框架边界与无 trick 均认可。成立项为生产流程日志缺成功事务输入/页签发奖与 Stage 身份/Session 回收后证据，FusionSlot 空态残留编号，素材已选 TMP/字形与合计三态未接，Entry C# 默认与 Asset BatchId 不一致，无效选择分支不 Dirty 测试不足。后续不追加审查。
  - 修正核对：执行代理与并行测试代理一次性修完全部成立项；主代理依据源码/Builder产物与官方 MCP 证据核对事务前后日志快照、StageInitialize/UnloadComplete、Badge归属与空态清文本、素材 TMP/字形、合计三态、Entry C#/Asset一致、非法分支不 Dirty 测试。公开 Builder/Exporter 重建成功，`RunCardRulesTests` 19/19，全 EditMode 34/35（唯一任务外 Boar/Boar_001），Preparation Connected、Main不脏、Console error=0；不追加第二次审查。
- [x] e：正式验收前完成并记录四项验收规划门槛及分类映射。
  - 当前并行列表：只有“备战阶段卡牌融合”一篇；a～d 已完成且审查成立项清零，具备验收条件。继续/词条尚未启动，须等待本篇 Completed。
  - 合并判断：无其它策划案可合并。本篇使用同一 `PreparationStageEntry.asset` 正式入口；Trip A/B 共享一个 Preparation Play 的操作链，Trip C 重新进入以验证未确认选择离场回收，证据逐趟隔离。
  - 美术主干：`ART-01`～`ART-06` 全部为主干，按非截图资产编排检视；核对9张正式Sprite、页签双态、4槽空/占用/悬停、合计三态、按钮三态、素材标记TMP与99完整卡面、Prefab/PreLoad/Connected Scene/Exporter引用。
  - 功能主干：`FUNC-01`～`FUNC-10` 全部为主干；`RGR-01`～`RGR-05` 为关键回归。Trip A覆盖页签、放入/替换/移除/第5张与小于/等于/大于99；Trip B覆盖合法融合、11/15、消耗/清出战槽、持有99后合法组合拒绝、99素材拒绝、99编入下一Battle；Trip C覆盖切页保留与离Preparation后选择清空/素材不消耗。
  - 统一验收预计3趟，均使用“流程log验收”：主代理通过 `GameStageEntryLauncher.Start(PreparationStageEntry.asset)` 正式进入，使用实际 Button 与 PointerDown/Drag/PointerUp UI事件；99进战斗与离场均调用生产 `EnterBattleStageGroup`。日志分别保存为 `AutoDoc/Temp/preparation-stage-card-fusion-trip-a-flow-log.md`～`trip-c-flow-log.md`，每条映射原稳定编号；不创建长期测试框架。
- [x] f：主代理按策划指定“流程log验收”通过正式 StageGroup 入口实际操作；另按资产编排检视 `ART-`，失败时最多执行两趟修正闭环。
  - 证据：主代理两次通过正式 `PreparationStageEntry.asset` 启动实际 Play，使用真实 Button、UiDragable/UiInteractor responder 完成 Trip A/B/C。首次入口发现 inactive 融合根未进入 Ui 初始化链，修正趟次 1 通过标准 Builder 重建且不加初始化旁路；按用户每类审查最多一次的约束由主代理直接核对差异后重验。Trip A 覆盖页签、放入/替换/移除/第五码与 34/99/118；Trip B 原子融合得到 99/11/15、清出战槽、拒绝二次融合/99素材并把99送入真实Battle；Trip C 证明切页保留、离Stage清选择不消耗。9/9 Sprite、Prefab三态、字体、99配置和 Connected Scene 编排均由主代理实读通过；Console error=0。
- [x] f：主代理创建并复读 `AutoDoc/DesignPlan/Review/preparation-stage-card-fusion-review.md`。
  - 证据：Review 从第1章开始且实现概括不超过100字符，包含2.1/2.2/2.3和第4章；逐项映射 `ART-01`～`ART-06`、`FUNC-01`～`FUNC-10`、RGR、唯一Plan/代码审查及首次验收修正，最终结论“通过”。
- [x] g：根据正式 Review 设置 `Completed` 或 `Warning` 并同步直接相关现状文档。
  - 证据：正文已设置 `state: Completed` 并写入现有 Plan 与 Review 的合法路径；执行代理已同步备战程序/UI/玩家设计/美术现状文档，仅描述当前已落地实现。
- [ ] h：按明确路径提交并按 upstream 条件推送，记录 Git 证据，关闭本篇全部子代理。
  - 当前阻塞：2026-08-15 约 11:25 +08:00，显式路径暂存时 Git 无法在 `D:/Git/Hearthstone/.git/` 创建 `index.lock`（`Permission denied`）。当前沙箱仅允许写项目子目录 `D:/Git/Hearthstone/Hearthstone`，对上级 `.git` 为只读；未使用破坏性替代方案，也未误暂存任务外改动。其余实现继续推进，批次收尾时再复核该权限。

## 并行组 a～h 门槛：继续下一关与怪物战斗词条

- [x] 仅在卡池编成与卡牌融合均进入 `Completed` 后启动并行组。
  - 偏差与依据：2026-08-15 11:20:19 +08:00，融合主干规则、UI 与正式入口已经通过代码修正，主代理正式验收 Trip A/B 核心链已实际跑通；融合状态收尾尚在进行时，用户明确要求“并行开启后面两个案子的plan阶段”，因此只提前启动两篇的 a～b Plan 工作，不提前进入 c 实施，也不把融合视为已 Completed。
- [x] 启动前复核两篇实施范围是否完全独立：继续篇只负责备战出口、提交阵容、取消未确认选择和关卡推进；词条篇负责词条数据、战斗结算、卡面展示与融合结果词条并集。若 Plan 显示同文件或公共契约交叠，先串行收敛公共边界或取消并行。
  - 证据：两份 Plan 明确源码所有权；公共 `BattleScenarioStartupData` 由关键词方定义、Continue 方单向接入，Preparation 生成资产、字体、PreLoad、资源索引与现状文档由 Continue 方在串行屏障统一导出。
- [x] a：主代理分别先把两篇从 `In Design` 改为 `In Progress`，复核各自严格三行文件头并记录状态切换。
  - 证据：2026-08-15 11:20:19 +08:00，两篇均由 `In Design` 切换为 `In Progress`，标题与 `priority: P1` 保持不变；恢复点均为步骤 a“尚未创建实施 Plan”。
- [x] a：分别启动独立执行子代理创建：
  - `AutoDoc/DesignPlan/Plan/preparation-stage-continue-plan.md`，覆盖指定流程log、`ART-01`～`ART-03`、`FUNC-01`～`FUNC-07` 与回归；
  - `AutoDoc/DesignPlan/Plan/monster-combat-keywords-plan.md`，覆盖指定流程log、`ART-01`、`FUNC-01`～`FUNC-10` 与回归。
  - [x] 继续案 Plan：`/root/continue_plan` 已创建 211 行 `preparation-stage-continue-plan.md`，分离映射 ART-01～03、FUNC-01～07、RGR-01～05；收敛唯一 Stage 调度器事务式加载/失败逆序回滚，并明确并行业务源码所有权与 Unity 资产串行屏障。
  - [x] 关键词案 Plan：`/root/keywords_plan` 已创建 196 行 `monster-combat-keywords-plan.md`，分离映射 ART-01、FUNC-01～10、RGR-01～05；收敛词条配置/Run/Battle/融合并集/动态卡面，并明确 99 禁作素材条件与共享资产串行屏障。
- [x] b：分别使用 Plan reviewer 独立审查；逐篇传递、修正和核对成立意见，不合并审查结论。
  - 继续案唯一审查：结论“不通过”；成立项为下一战错误复用已应用旧奖励 Batch、完整 LoadStage 被误当隔离 prepare 而未覆盖真实副作用/部分失败/旧卸载失败、FUNC-06 直接篡改 Component 的验收方式违规、共享现状文档未入串行屏障、禁用 Button 与 DuplicateIgnored 日志不闭环。已交原 Plan 执行代理修订，不追加 reviewer。
  - 关键词案唯一审查：结论“不通过”；成立项为正式 Battle Startup 无法表达空槽/可控敌我编成、K1～K4 缺精确可复现输入、直接把 Attack 改成 Listenable 会破坏调用面、共享 Unity 生成物执行者矛盾。FUNC-08 的“99 后续再融合”条件不可达处理被确认正确。已交原 Plan 执行代理修订，不追加 reviewer。
  - 主代理修正核对：继续 Plan 已改用正式 `BattleProgressionCsvData` 新 Batch、完整 Validate→Prepare→SuspendOld→HiddenCommit→Publish→UnloadOld 事务/补偿契约、Editor-only one-shot LateLoad 失败注入、Waiting 输入阻挡层和9份文档串行清单；关键词 Plan 已补强类型 Scenario DTO、K1～K4 精确 Entry/配方/槽位/seed、兼容 `int Attack + AttackValue`、Stage 单向接入和唯一共享屏障。原两次唯一审查的全部硬阻塞均清零，准予分别进入步骤 c，不追加 Plan reviewer。
- [x] c：各自执行子代理按所有权边界实现，不回退他人改动；静态检查和编译证据逐篇归属。
  - Continue：事务式唯一 Stage 调度、正式第2关配置、Progression/Continue状态、点击阵容快照、0～3槽 Battle、右上四态按钮及共享资产/文档已落盘；事务测试6/6。
  - Keywords：配置驱动四词条、Run融合去重、Battle结算、Scenario DTO、四类动态卡面及 Builder 已落盘；关键词测试最终9/9。
  - 共享屏障：主代理接管并通过公开入口重跑六个 UI Builder 与 `PreparationUiSceneBuilder`，字体“继续下一关嘲讽远射爆裂冲锋”无缺字，Main Scene不脏、Console无Error。
- [x] d：分别由 code reviewer 审查实际改动；按篇保存唯一 round 文件并遵守审查次数上限。
  - Continue 唯一审查：`AutoDoc/Temp/preparation-stage-continue-code-review-round-1.md` 初始“不通过”；阵容快照死数据、专属测试及现状文档缺口均已修正。主代理核对正式 startup/request key/Battle消费链与5/5测试，不追加审查。
  - Keywords 唯一审查：`AutoDoc/Temp/monster-combat-keywords-code-review-round-1.md` 初始“不通过”；CSV双轨、默认Scenario、校验、字体和四词条布局均已修正。主代理核对最终代码、Builder与9/9测试，不追加审查。
- [x] e：同步完成两篇 a～d 后，主代理记录并行列表与进度、可合并验收判断、逐篇分类验收清单和统一趟次映射。
  - 用户最终明确“验收只要保证代码逻辑正确，进游戏验收免了”，因此取消原计划的 StageGroup Play 趟次；两篇分别以唯一代码审查、定向逻辑测试、全 EditMode、编译与静态资产编排收口，不混用结论。
- [x] f：按用户最新明确要求，不进入游戏；主代理以代码逻辑、配置接入、自动化测试与资产编排检查完成两篇验收。
  - Continue：5/5专属测试、6/6事务测试；`ART-01～03`、`FUNC-01～07` 逐项写入正式 Review。
  - Keywords：9/9专属测试；`ART-01`、`FUNC-01～10` 逐项写入正式 Review。
  - 最终全 EditMode作业 `32dd621e7dfa4ba78a9bb9f546d52c37` 为55/56，唯一失败是任务前 `Boar`/`Boar_001` 旧断言；Main=`Assets/Scenes/Main.unity`、dirty=false、Console Error=0。
- [x] f：主代理分别创建、复读：
  - `AutoDoc/DesignPlan/Review/preparation-stage-continue-review.md`
  - `AutoDoc/DesignPlan/Review/monster-combat-keywords-review.md`
  - 证据：两份 Review 均从第1章开始，包含2.1/2.2/2.3和第4章，记录用户免进游戏口径、逐项稳定编号、唯一代码审查与修正、定向/全量测试及框架边界，结论均为通过。
- [x] g：逐篇根据各自 Review 设置 `Completed` 或 `Warning`，写入正确 Plan/Review 路径并同步现状文档；一篇结果不得代替另一篇。
  - 证据：两篇正文均为合法五行 `Completed` 文件头并指向各自 Plan/Review；Program/Design/Art/UI 现状文档已同步实际继续按钮、事务、词条和融合继承。
- [ ] h：逐篇只提交各自明确归属文件，记录提交/推送证据，关闭各篇执行和审查子代理。
  - 子代理：Continue/Keywords执行与两名唯一代码 reviewer 均为 completed，无活动遗留。
  - Git阻塞：显式 `git add` 仍无法创建 `D:/Git/Hearthstone/.git/index.lock`（Permission denied）；当前沙箱对父仓库 `.git` 只读，无法提交或推送，未使用替代仓库或破坏性绕行。

## 正式验收与证据规则

- [x] 流程log或游戏内截图验收前完整读取并遵循 `test-game-content`，只通过正式 StageGroup 与 `GameStageEntryLauncher` 进入，不手动打开内部 Scene 或绕过框架。
  - 结果：规则已读取；用户随后明确免进游戏验收，因此本并行组未进入 Play，按代码逻辑验收执行。
- [x] 每趟验收先记录趟次、策划案、case、原 `ART-`/`FUNC-` 编号、预期和证据位置，并隔离旧日志。
  - 前两篇按原验收方式完成；后两篇经用户明确改为代码逻辑验收，不再产生 Play 趟次，映射直接写入各自正式 Review。
- [x] 游戏内截图来自本次实际运行、状态完整清晰，归档到正式 `DesignPlan/media/.../review/` 英文 kebab-case 路径。
  - 本并行组不适用；用户免除进游戏验收。前两篇既有正式截图/流程证据保持不变。
- [x] 流程日志能够直接映射操作、关键 Log、预期、实际和稳定编号；静态检查、截图或代码审查不冒充功能行为证据。
  - 后两篇因用户变更验收口径为代码逻辑，不声称执行过流程日志；正式 Review 明确记录自动化测试与静态资产证据。
- [x] 未要求截图的美术项检查实际资产路径、内容、引用、组合、层级、布局和状态覆盖，不能只以文件存在判定通过。
- [x] 每趟验收结束删除一次性启动/验收脚本及其测试附属文件，不删除正式功能脚本。
  - 后两篇未创建一次性 Play 启动脚本；`PreparationContinueTests`、`BattleKeywordRulesTests` 和事务测试均为正式最小回归，不属于验收临时脚本。
- [x] 任一主干失败时先写 attempt 证据，再按篇最多执行两趟“执行子代理修改→一次 code reviewer 复审→主代理重新验收”；不得自动增加第三趟。
  - 用户后续限定每类审查最多一次；后两篇唯一审查发现均由执行代理一次性修正并由主代理核对，没有追加审查。
- [x] 正式 Review 从 `## 1. 实现方式` 开始，含 `2.1`、`2.2`、`2.3`、必要时第 3 章及详细第 4 章，并逐项映射直接证据。

## 框架边界审计

- [x] 所有 GameStage、ECS、UI、Task、配置与资源操作使用现有框架公开 API、生命周期、编辑器配置源和导出流程。
- [x] 不手写 Scene、Prefab 或 `.asset` YAML，不创建、编辑或删除 `.meta`，不自行调用私有 Editor Bridge 或桌面 UI 自动化。
- [x] 不在业务层重复实现框架已有能力，不保留兼容补丁、平行状态系统、手写导出产物或对内部管理器的绕过访问。
- [x] 确认框架缺口时只实施本批次所需的最小改动，纳入 Plan、审查、验证和最终 Review，并检查向后兼容与受影响回归。
  - 证据：唯一 Stage 调度器新增事务生命周期；默认 Battle仍Scenario=null，显式Scenario与Continue快照是互斥强类型输入；词条数值只取唯一CSV权威。
- [x] 对象适合复用且生命周期明确时使用项目现有对象池，避免无必要重复分配与 GC。

## Git、文档与批次收尾

- [x] 每篇提交前核对 Git 根、分支、upstream、暂存区与文件归属；禁止 `git add .`、`git add -A`、强推、改历史或自动 pull/rebase/merge。
  - Git根=`D:/Git/Hearthstone`、分支=`main`、upstream=`origin/main`；只使用显式路径尝试暂存，但父仓库 `.git/index.lock` 写入被沙箱拒绝。
- [x] 正式 Review 完成后只同步实际实现可证实的 `AutoDoc/Program/`、`AutoDoc/Design/`、`AutoDoc/Art/` 现状，不把未通过或未来预期写成当前事实。
- [x] 汇总每篇 Plan/Review、审查 round、框架改动、验收方式与趟次、终态、commit/push 和子代理关闭证据。
- [x] 确认没有活动子代理；重新读取本清单并逐项标记通过、未通过或不适用，补充直接证据并先修正可修正缺口。
  - `list_agents` 显示 Continue/Keywords执行与代码审查代理均 completed；融合/卡池代理此前亦已 completed/interrupted关闭。
- [x] 整个任务只运行一次 `AutoDoc/CleanupTempDocs.bat`，记录退出结果。
  - 偏差：关键词执行代理在其局部收尾时提前运行了本批次唯一一次清理（exit 0）；主代理发现后立即禁止其它代理及自身再次运行，未发生第二次清理。
- [x] 清理后创建 `AutoDoc/Temp/2026-08-15-DesignPlanImplementationBatch-Report.md`，记录任务结果、逐项状态与证据、验证、偏差、风险、文档与清理结果。
- [ ] 报告后最后检查 `git status --short`；只有无输出才能报告整个批次完成。任务外遗留改动保持原样并明确作为完成阻塞上报。
  - 未通过：最终 `git status --short` 共 314 行，包含后三篇任务内未提交产物及大量任务外/并发改动；由于 `.git/index.lock` 权限阻塞，无法完成显式暂存、提交、推送或获得干净工作区。
