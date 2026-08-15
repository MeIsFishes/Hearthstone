不通过

## 审查基线

- 策划案：`AutoDoc/DesignPlan/2026.08.15/preparation-stage-card-pool.md`
- 已审查实施 Plan：`AutoDoc/DesignPlan/Plan/preparation-stage-card-pool-plan.md`
- 策划案实现流程：`.codex/private-skills/project-state-preflight/design-plan-implementation.md`；本报告按其中“代码审查不代替验收”以及框架改动默认授权规则执行。
- Git：可用；仓库内，基线为本轮未另行指定引用时的 `HEAD`（`445f0cb9d3d8cc8b51143717d9776b40151b1701`）到当前工作区。已检查 `git status --short`、`git diff --name-status`、`git diff --cached --name-status`、工作区 diff，并读取相关未跟踪文件；暂存区无差异。
- 已跟踪修改：资源导出/预载配置 `LoadingTimeData.asset`、`PreLoadUiData.asset`、`ResourcesDictionary.json`；运行入口、Battle Component/规则/Stage/Editor Entry 共 9 个脚本；策划案及 Battle/Combat/UI 现状文档。与本篇直接相关的已跟踪代码文件均纳入审查。
- 未跟踪实现：RunState、奖励 StartupData、Preparation Stage/Listener、规则测试、Preparation 的 3 组 View/Controller/UiBuilder、UiScene、3 个 Prefab、导出 Asset、Editor Entry、Preparation 正式图片资源及对应文档；同时存在大量新增 `.meta`。本轮只按调用链与资源接入读取相关文件，没有把 `.meta` 存在本身当成手写证据。
- 明确排除：不进入 Unity、不执行验收、不检视验收截图；主代理给出的编译/Console 0、`RunCardRulesTests` 4/4 仅作为既有静态/测试背景。全 EditMode 19/20 中唯一失败的 CSV `Boar_001` 与旧 Boar 测试属于任务外遗留，不纳入本篇问题。

## 需求与代码实现覆盖表

| 需求或 Plan 项 | 代码/配置/资源接入落点 | 代码层覆盖状态 |
| --- | --- | --- |
| Battle 终局自动进入 Preparation | `BattleStages.cs:27` 注册 `BattleResultPreparationStageListener`；Listener 在 `BattleResultPreparationStageListener.cs:18-27` 监听首次终局并携带 batch 调正式入口 | 未完成：正常单次调用链已接通，但对框架排队切换的重入/并发调用不安全，见高严重度发现 1 |
| RunState 跨 Battle/Preparation 生命周期 | `HearthstoneGameEngine.cs:30,49,58` 始终提交 `RunStateStage`；`RunStateStages.cs:17-28` 只在该 Stage 真正卸载时创建/移除唯一状态；短期 Preparation session 在 `PreparationStages.cs:62-69` 对称创建/移除 | 代码层完成（仅表示静态链路） |
| 恰好 5 张、互异、强类型快照 | `BattleStageStartupData.cs:6-67` 的 grant/batch 类型、构造校验与逐项深拷贝 | 代码层完成 |
| 新批次原子应用、同 BatchId 幂等 | `RunCardRules.cs:24-61` 先完整检查持有冲突，再统一写入；已应用批次校验 payload 后不写入、不增 Revision；4 个规则测试覆盖主要分支 | 代码层完成 |
| 3 槽替换、换槽、单卡唯一、无效取消 | `RunCardRules.cs:91-117`；Card/Slot requester-responder 接入 `PreparationController.cs:44-47` 与 `PreparationSlotItemController.cs:72-99` | 未完成：规则提交本身闭环，但框架触摸目标未在拖拽结束后复位，重复拖向同一槽不再产生高亮，见高严重度发现 2 |
| 永久攻击/最大生命进入后续战斗 | `BattleStages.cs:120-133` 从 RunState 槽实例创建玩家卡；`BattleCardRawComponent.cs:51-68` 使用永久 Attack/MaxHealth 并以 MaxHealth 初始化本场 CurrentHealth；敌方仍走原随机分支 | 代码层完成 |
| 1~98 固定编号、7×14、3 槽常驻、纵向滚动 | `PreparationController.cs:59-77` 创建 98+3 条目；`PreparationViewUiBuilder.cs:77-83,100-167` 分离槽区和 ScrollRect，Content 为 7×14，纵向 Clamped | 未完成：结构存在，但 Builder 未按 Plan 从 viewport 可用宽度/7 推导 cellWidth，且滚动轨道宽度 350 越出容器，见中严重度发现 4 |
| UiBuilder→Prefab→UiScene→Exporter→PreLoad | 3 个 Builder 输出对应 Prefab；`PreparationUiSceneBuilder.cs:20-67` 创建 Connected Prefab 场景并调用公开 Exporter；`Preparation.asset` 含 `Ui/PreparationView`；`PreLoadUiData.asset:20-25` 有两类动态条目映射；`PreparationStages.cs:37-44` 通过 Resources + `SetUiScene` 消费 | 未完成：Pre-UiInit/PreLoad 环节通过反射调用框架私有方法，越过公开边界，见高严重度发现 3 |
| 不手写 Scene/Prefab/.asset YAML，不创建平行导出物 | 差异中 Scene/Prefab/Asset 内容与 Builder/Exporter 落点相互对应 | 无法确认：Git diff 无法证明生成来源；未发现另一份运行时导出物或直接手改 `UiObjectDatas` 的代码，但新增 `.meta`/YAML 的具体生成过程没有可审计证据 |

## 发现

### 高严重度 1：StageGroup 入口把“已请求”当成“已激活”，无法安全处理排队期间的第二次调用

- 位置：`Assets/Scripts/Hearthstone/Bootstrap/HearthstoneGameEngine.cs:42-49,52-58`；相关框架语义见 `Assets/Scripts/BbxCommon/GameFramework/GameEngineBase.cs:427-441,473-507`。
- 证据：业务入口在调用 `SetActiveGameStage` 前就更新 `m_CurrentStageGroup`/`m_CurrentBattleRewardBatchId`，但框架只把 load/unload 追加到 `m_OperateStages`，且 `SetActiveGameStage` 只检查当前 `m_EnabledStages`，不检查待处理操作。若切换尚未消费时再次调用正式入口，第二次调用仍按旧 enabled 集合再排一次 unload/load；Preparation 入口甚至没有同组/同 batch 防重入。框架随后会动态遍历同一操作表并最终整体 Clear，可能重复卸载 Battle、同时加载两个 Preparation，造成重复短生命周期单例/UiScene 或半加载状态。Battle Listener 内的 `PreparationTransitionRequested` 只能挡住同一 Listener 的重复终局通知，挡不住公开入口、Editor entry 或其它调用方在加载窗口内重入。
- 影响：违反 Plan 第 1、5 项及 `RGR-04` 的完整 StageGroup、短 Stage 对称卸载、无重复页面要求；同一 batch 幂等规则不能修复两个 Preparation Stage 同时排队的生命周期问题。
- 必须修正：基于框架公开生命周期建立单一 StageGroup 切换协调状态，明确区分 requested/loading/active；在前一请求落定前合并相同请求并拒绝或串行化冲突请求，且只提交一次完整集合。至少增加覆盖“同帧/加载期间重复 EnterPreparation”“初始 Entry 与 OnAwake 请求交叠”的代码级回归。

### 高严重度 2：重复拖向同一槽时，框架保留旧 `m_Touching`，目标高亮链路不闭环

- 位置：业务订阅在 `Assets/Scripts/Hearthstone/Ui/Controller/PreparationSlotItemController.cs:72-80,101-104`；根因在直接相关框架 `Assets/Scripts/BbxCommon/Ui/Misc/UiInteractor.cs:139-182`。
- 证据：`UiInteractor.OnDrag` 只有碰到“不同”的 interactor 才触发 `InteractorTouch`；`OnDragEnd` 完成 interact 后没有对 `m_Touching` 调 `InteractorTouchEnd`，也没有置空。业务 `OnBackFromTop` 只刷新视觉。第一次拖拽结束后高亮会被业务刷新关闭，但下一次直接拖向同一槽时 `m_Touching == uiInteractor`，不会再次触发 `OnInteractorTouch`，因此没有有效目标高亮。离开所有 interactor 时框架同样没有清理旧触摸目标。
- 影响：重复调整是策划案核心流程；违反 4.3/4.4 的反复拖拽和 4.5 的即时反馈，也违反 Plan 4.1 中 EndDrag 清全部高亮/下次交互重新建立目标的链路。
- 必须修正：在现有 `UiInteractor` 生命周期内补齐拖拽结束及“当前帧未命中 responder”时的 TouchEnd/置空，并回归其它 UiInteractor 调用方；业务侧不得用反射或私有字段补丁规避。该能力缺口为小型框架缺口，局部、向后兼容；策划案实现流程已默认授权，无需另行取得用户许可。

### 高严重度 3：Preparation Builder 反射调用框架私有 Editor 方法，越过公开 API

- 位置：`Assets/Scripts/Hearthstone/Ui/Editor/PreparationUiBuilderUtility.cs:104-126,135-141`；被访问的私有方法在 `Assets/Scripts/BbxCommon/Ui/Mvc/UiViewBase.cs:28-30,112-115`。
- 证据：Utility 用 `BindingFlags.Instance | BindingFlags.NonPublic` 查找字符串方法名 `PreUiInit` 和 `ExportAsPreLoadUi`，再 `MethodInfo.Invoke`。这不是框架公开 Builder/导出契约，重命名或签名变化不会得到编译期保护；它把本需求专属 Builder 绑定到框架内部实现。
- 影响：即使当前 Prefab 与 `PreLoadUiData.asset` 已存在，也不能据此判定框架接入合格；违反 Plan 4.3 的 Editor 流程/公开 Stage 与导出边界，以及禁止直接访问框架内部能力的审查规则。
- 必须修正：删除业务层私有反射。若继续由人工/标准 Editor 按钮执行，则 Builder 只生成 Prefab并按既有流程导出；若项目需要可重复自动 Builder，则在 BbxCommon Editor 层提供受支持、强类型、可回归的公开 PreUiInit/PreLoad 导出入口，并让所有调用方统一使用。该能力缺口为小型框架缺口；策划案实现流程已默认授权，无需另行取得用户许可。

### 中严重度 4：卡池 Builder 与已审 Plan 的宽度推导不一致，滚动轨道矩形越出容器

- 位置：`Assets/Scripts/Hearthstone/Ui/Editor/PreparationViewUiBuilder.cs:100-137`。
- 证据：Plan 明确要求由 viewport 可用宽度除以 7 得 cellWidth；代码把 viewport 设为 1400，却固定 `cellWidth=160`，Content 宽仅 1120，留下 280 的未解释空白。Scrollbar 根宽 46，但 `TrackVisual` 宽设为 350；按当前锚点与位置，其右边界超过 1640 宽 CardPoolPanel 的右边界。该值不是来自规则或容器宽度计算。
- 影响：代码层无法证明“完整底框内明确滚动条、七列等宽区域按可用宽度排布”的 Plan 实现，且 Builder 重建会稳定复现该偏差。此项只判断布局代码与 Plan 一致性，不给出玩家可见 ART 验收结论。
- 必须修正：从 viewport 扣除滚动条/间距后的可用宽度统一推导 7 列 cellWidth 与 Content 宽；将 TrackVisual 限制在 Scrollbar/Panel 设计矩形内，去除无解释的超宽常量，并重新经标准 Builder/Exporter 流程生成资产。

## 框架边界审计

- 不通过。
- 符合项：运行时 Stage 使用 `CreateStage`、`AddLoadItem`、`AddStageListener`、`SetUiScene`、`SetActiveGameStage` 等公开 API；RunState 没有平行副本；卡池条目通过既有 `UiList`/预载映射创建；资源通过 `ResourceApi`/`DataApi`/Resources 导出资产消费；未发现直接访问 ECS 内部 manager 或手写第二套运行时资源表。
- 越界项：`PreparationUiBuilderUtility` 以私有反射调用 `UiViewBase` 的内部 Editor 按钮实现，必须收敛到框架公开导出流程。
- 小型框架能力缺口：一是可重复 Builder 所需的公开 PreUiInit/PreLoad Editor API（若决定自动化）；二是 `UiInteractor` 的拖拽触摸目标结束复位。二者均局部、可向后兼容，应在框架内修正并回归既有调用方。策划案实现流程已默认授权，无需另行取得用户许可。
- 未识别需要改变公开数据格式、核心生命周期或多模块迁移的大型框架缺口。

## 特定需求 trick 汇报

- 发现：`Assets/Scripts/Hearthstone/Ui/Editor/PreparationUiBuilderUtility.cs:135-141` 的“按私有方法名反射并 Invoke”是为本篇 Builder 绕过框架 Editor 入口可见性而设计的特定需求 trick；判断依据是字符串绑定、非公开反射、无编译期契约且仅服务 Preparation 资产生成链。
- 未发现为了 5 张奖励、98 卡位或 3 槽规则另建平行运行时状态；这些不变量集中在 `RunCardRules`，设计方向合理。

## 超出范围与无法确认的风险

- 未进入 Unity、未运行 Play/验收脚本、未采集或检视截图，因此不判断 `ART-*`/`FUNC-*` 实际通过，也不确认玩家可见滚动、拖放、画面品质或 Stage 实际切换结果。
- Git 只能确认当前内容与基线差异，不能独立证明新 Scene/Prefab/`.asset`/`.meta` 是由 Unity/Exporter 生成还是手写；代码存在对应 Builder/Exporter，但私有反射使该生成链本身不合格。
- `AutoDoc/DesignPlan/Plan/` 及若干实现目录整体未跟踪，状态清单不能提供任务前版本；本报告按当前文件内容审查。
- 主代理提供的编译、Console 与测试结果不覆盖上述排队 Stage 重入、UiInteractor 重复拖向同一槽、公开 Editor 导出边界问题。

本结论仅为代码审查结论，不代表策划案验收通过；本报告未修改评审意见文件以外的任何文件，不代替主代理验收、编写正式 Review 或实现修正。
