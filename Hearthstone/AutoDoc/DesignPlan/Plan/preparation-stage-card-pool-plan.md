# 备战阶段卡池编成实施 Plan

## 1. 需求明确

### 1.1 需求对齐

**验收方式（策划案第 6.1 节，优先记录）**：只使用“游戏内截图”。主干从 `InitialStageEntryAsset` 启动同一次正式 Play，进入 `RunStateStage + BattleStage`，等待实际战斗首次产生终局结果，由 `BattleResultPreparationStageListener` 自动切到 `RunStateStage + PreparationStage` 后截图；不得退出 Play、手调 Stage 或用直达入口拼接 `FUNC-01`。`PreparationStageEntryAsset` 只供其它 case 的隔离验收。截图统一归档到 `AutoDoc/DesignPlan/media/2026.08.15/preparation-stage-card-pool/review/`。

**美术资产验收覆盖（与功能验收分开判定）**：

| 编号 | 资产落点与编排位置 | 游戏内截图检视映射 |
| --- | --- | --- |
| `ART-01` | `PreparationStageTitleFrame.png`；编入 `PreparationView.prefab/TitleArea`，TMP 标题居中叠放 | 截图组 A 全屏：标题底框完整、红金结构与原型一致 |
| `ART-02` | `PreparationSectionLine.png`、`PreparationBattleSlotFrame.png`；编入槽位标题两侧及 3 个 `PreparationSlotItem.prefab` | 截图组 A：横线对称连续、恰好 3 槽且尺寸/基线/间距一致 |
| `ART-03` | `PreparationCardPoolPanel.png` 与滚动条轨道/滑块/箭头资产组；编入 `PoolArea/ScrollRect` | 截图组 A 首段与末段：完整包框、滚动条清晰且不覆盖卡位 |
| `ART-04` | 复用卡框、编号六边形、攻击/生命徽章及卡牌原画；`PreparationPoolEmptySlot.png`；编入 `PreparationCardItem.prefab` 两态 | 截图组 A 首屏：7×2 等宽等高、编号连续、缺失不补位、完整卡面 `2:3` |
| `ART-05` | `PreparationPageBackground.png`、`PreparationRewardPanel.png` 及页面资产组合 | 截图组 A 全屏与原型并排：区域比例、颜色、装饰语言和常驻控件一致 |
| `ART-06` | `PreparationDropHighlight.png`；编入槽位有效目标层；拖起层来自 Card/Slot Prefab | 截图组 B 悬停：被拖卡和唯一有效目标均清楚且无遮挡 |
| `ART-07` | 本表正式 Sprite、3 个 View Prefab、`Preparation.unity` 的 Connected Prefab 编排及导出 Asset | 截图组 A～D：无缺图、默认控件外观、临时色块、裸文字或占位框 |

**程序功能验收覆盖（实际运行行为，与美术 case 分开）**：

| 编号 | 功能落点 | 正式入口下的实际操作与截图映射 |
| --- | --- | --- |
| `FUNC-01` | `BattleStageStartupData` 携带已分配 batch；结果 Listener 首次终局自动切换；Run state 按 BatchId 幂等应用 | 截图组 A：同一 Play 从 Initial/Battle entry 启动，截取战斗终局与自动出现的备战页，备战页显示“本轮获得 5 张卡”。首批 grant 采用 `02/03/05/06/07`，与首次旧阵容 `01/04/40` 区分；随后在同一 Play 通过生产 `EnterPreparationStageGroup` 再提交同一 batch，前后截图逐位比对 `01~07` 持有态、3 槽和奖励反馈完全相同，以可见状态证明未重复发放 |
| `FUNC-02` | Run state 的 1～98 持有实例与页面 98 个固定条目绑定 | 截图组 A 首屏：持有卡完整、缺失为空且后项不前移；卡面攻血等于 grant 永久值 |
| `FUNC-03` | `ScrollRect` + 7 列、14 行固定 Content，槽位区在 Viewport 外 | 截图组 A：首段滚到含 `98` 的末段，前后 3 槽内容与位置不变 |
| `FUNC-04` | `RunCardRules.TryPlaceCard` 与 Card→Slot requester/responder | 截图组 B：持有卡悬停空槽 0 后释放，只在槽 0 新增且无重复 |
| `FUNC-05` | 同一放置入口的直接替换分支 | 截图组 C：新卡替换槽内原卡，无叠放；原卡池位置仍为完整卡面 |
| `FUNC-06` | Slot→Slot requester/responder，提交前先识别原槽 | 截图组 D：已上阵卡换到空/占用槽；原槽清空，目标原卡恢复未上阵 |
| `FUNC-07` | 无有效 Slot responder 时不提交 Run state | 截图组 D：释放到无效区前后 3 槽完全一致，无复制、丢失或替换 |
| `FUNC-08` | 固定 3 长度阵容与单卡唯一占槽约束 | 截图组 D：满槽继续拖卡及同卡换槽后仍恰好 3 槽、无叠放/重复编号 |

**旧功能回归**：`RGR-01` 同次主干 Play 中确认 3v3 自动战斗、结算文字、敌方按既有配置随机生成仍工作；`RGR-02` 首次 Battle 的既有玩家默认 `{4,1,40}` 被一次性写入 Run state 后，卡面与旧逻辑一致，后续 Battle 改从 Run state 3 槽永久实例构造；`RGR-03` 同一 batch 重入 Preparation 不增加持有数、不递增 Revision，换新 BatchId 但包含已持有编号时整批拒绝且状态全不变；`RGR-04` Battle/Preparation Group 切换时 `RunStateStage` 始终保留，短生命周期 Battle/Preparation 单例与 UiScene 对称卸载，无重复页面或 Console Error；`RGR-05` `BattleCardItem.prefab`、其 Builder/View/Controller 与既有 `BattleRulesTests.cs` 不由本篇修改。

1. 新增持久 `RunStateStage`；Battle Group 固定为 `RunStateStage + BattleStage`，Preparation Group 固定为 `RunStateStage + PreparationStage`，每次只调用一次 `SetActiveGameStage` 声明完整集合。
2. Run state 是整局唯一可变权威状态，保存 `1~98` 持有卡实例的永久攻击/最大生命、3 槽阵容与已应用 BatchId；PreparationStage 只保存本次奖励展示 Component、UiScene 和页面生命周期，卸载不得销毁整局状态。
3. 上游必须为每轮提供唯一 BatchId 与恰好 5 个互异 grant；每个 grant 已确定 CardNumber、永久 Attack、永久 MaxHealth。随机编号与数值分配算法不在本篇实现。
4. 新 batch 的 5 个编号必须在应用前全部未持有，否则整批拒绝且不写 BatchId、不改数组、不增 Revision；相同 BatchId 重入视为已应用并完全无写入。这是保证玩家“本轮新增 5 张”的唯一入口契约，不做抽换、重掷或部分补发。
5. Battle 结果首次终结时由 StageListener 使用该 Battle 启动时携带的 batch 调正式 Preparation Group；Preparation 直达 Editor entry 只用于隔离验收，不承担生产切换。
6. 卡池与槽位卡面直接显示 Run state 永久实例数值；玩家后续 Battle 从 3 槽实例创建卡牌并以永久最大生命作为本场初始生命，敌方继续使用现有配置随机生成。当前策划不新增备战后返回战斗的按钮或调用方。
7. 不变玩法契约集中到唯一 `RunCardRules`：槽位 `3`、编号 `1~98`、每行 `7`、卡面 `2:3`、每批 `5`、单卡唯一占位；纯像素尺寸、间距和参考分辨率只在各 UiBuilder，不新增 CSV/SO 或平行常量。
8. 页面、卡池条目、战斗槽条目各使用独立 View/Controller/Prefab/一一对应 UiBuilder；不复用或修改并发范围内的 BattleCardItem 资产与代码。

## 2. 数据部分

### 2.1 涉及到的数据概览

| 数据 | 类型与唯一来源 | 生命周期与消费者 |
| --- | --- | --- |
| 本轮奖励 grant | `PreparationRewardBatchStartupData` 的深拷贝只读快照 | 上游/Editor 构造 → `BattleStageStartupData` → Battle session → 结果 Listener → Preparation 初始化；不回写 |
| 整局卡牌实例/阵容/已应用批次 | `RunStateSingletonRawComponent` | `RunStateStage` 全程存活；规则、Battle 初始化与 Preparation UI 共用唯一可变状态 |
| 本次奖励展示 | `PreparationSessionSingletonRawComponent` | PreparationStage 存活；只复制本 batch 的 5 个 grant 与“本次是否新应用”表现信息，不承担所有权 |
| 卡牌类型/原画 key | 既有 `BattleCardCsvData`、`BattleCardTypeCsvData` | `DataApi` 静态配置与 `ResourceApi` Sprite；不复制进配置资产 |

### 2.2 新增数据列表

| 类名 | 类别 | 重要字段与校验 | 预期路径 |
| --- | --- | --- | --- |
| `RewardCardGrantStartupData` | 强类型值对象 | `CardNumber`、`Attack`、`MaxHealth`；构造即检查编号范围、Attack≥0、MaxHealth>0 | `Assets/Scripts/Hearthstone/GameStage/BattleStageStartupData.cs` |
| `PreparationRewardBatchStartupData` | 强类型批次快照 | 非空唯一 `BatchId`、恰好 5 个 grant；构造逐项深拷贝并检查编号互异；`CreateSnapshot()` 再深拷贝 | 同上 |
| `BattleStageStartupData` | Battle 启动输入 | 必须携带一个 batch 快照；构造深拷贝、`ValidateStructure()`、`CreateSnapshot()` | 同上 |
| `RunCardInstanceData` | ECS 内持久实例值 | `CardNumber`、永久 `Attack`、永久 `MaxHealth`；值类型，不保存本场 CurrentHealth | `Assets/Scripts/Hearthstone/Ecs/RawComponent/Singleton/RunStateSingletonRawComponent.cs` |

#### 2.2.1 新增 Component 类

| 类名 | 重要字段 | 归属 Entity |
| --- | --- | --- |
| `RunStateSingletonRawComponent` | 预分配 `RunCardInstanceData[99] CardInstances`、`int[3] BattleSlotCardNumbers`、`HashSet<string> AppliedRewardBatchIds`、`ListenableVariable<int> Revision`；回收先 Invalid 再清空 | RunStateStage 的默认单例 Entity，全局唯一 |
| `PreparationSessionSingletonRawComponent` | 本次 `BatchId`、深拷贝 `RunCardInstanceData[5] RewardCards`、`bool WasNewlyApplied` | PreparationStage 短生命周期单例 Entity |

### 2.3 原有数据类新增字段

#### 2.3.1 原有Component类新增字段

| 类名 | 新增/调整字段 | 目的 |
| --- | --- | --- |
| `BattleSessionSingletonRawComponent` | `PreparationRewardBatchStartupData PendingPreparationRewardBatch`、`bool PreparationTransitionRequested` | Listener 在首次终局取得本场固定 batch；标记先置 true 再切 Stage，防止重复请求；回收清引用 |

## 3. 游戏逻辑部分

### 3.1 涉及到的游戏逻辑概览

| 类/入口 | 职责 | 框架边界 |
| --- | --- | --- |
| `RunCardRules` | 唯一契约常量；两阶段批次校验；BatchId 幂等、全批原子应用；空槽、替换、换槽、无效取消与唯一占槽 | 纯业务规则，无第二份状态；先完整验证再一次提交，成功才 Revision+1 |
| `InitializeRunStateRuntime` | 创建 Run state 单例；Unload 仅在 RunStateStage 真正退出时移除 | `RunStateStage` 早批 LoadItem |
| `InitializePreparationRuntime` | 读取 batch 快照；经 DataApi 校验每个 CardNumber 与永久数值落在类型配置范围；原子应用或识别同 BatchId；创建展示单例 | `PreparationStage` 早批 LoadItem；Unload 只移除展示单例，不动 Run state |
| UI 拖放回调 | 将强类型 requester/target 描述交给 `RunCardRules`，成功后由 Revision 刷新 | Controller 只负责交互与表现，不保存阵容副本 |

### 3.2 新增StageListener类

| 类名 | 职责 |
| --- | --- |
| `BattleResultPreparationStageListener` | `InitListener()` 取得 Battle session 并监听 `Result` Dirty；仅处理首次非 `InProgress`，先设置 `PreparationTransitionRequested=true`，再调用 `HearthstoneGameEngine.EnterPreparationStageGroup(session.PendingPreparationRewardBatch.CreateSnapshot())`；Unload 由 StageListener 自动解监听 |

### 3.3 原有逻辑类改动

| 类名/文件 | 改动方向 |
| --- | --- |
| `BattleStages` / `InitializeBattleRuntime` / `BattleCardRawComponent` | 工厂改为强制接收 `BattleStageStartupData`、保存快照、注册结果 Listener；初始化先取得 Run state。Run state 为空时仅为兼容首次旧流程，用现有 `{4,1,40}` 和既有配置随机入口一次性建立永久实例与 3 槽；新增从 `RunCardInstanceData` 初始化玩家卡的入口，之后玩家卡严格从 Run state 3 槽实例创建，敌方继续既有配置随机生成；Unload 只销毁本场 Entity/session |
| `HearthstoneGameEngine` | OnAwake 创建一次 RunStateStage，并用与 Initial entry 默认字段相同的明确 batch 启动 Battle Group；`EnterBattleStageGroup(startupData)` 与 `EnterPreparationStageGroup(batch)` 每次创建对应短 Stage，分别提交完整 `{run,battle}` / `{run,preparation}` 集合；相同 batch 的 Editor 初始入口调用可识别当前 Battle 输入而不重复重启 |
| `InitialStageEntryAsset` | 序列化 BatchId 与 5 个已分配 grant，默认明确为 `initial-battle-reward-001`：`(2,5,3)`、`(3,4,4)`、`(5,3,5)`、`(6,5,4)`、`(7,6,2)`（顺序为编号/攻/血）；Validate 后构造 `BattleStageStartupData`，通过正式 Battle Group 入口启动/确认当前同输入 Group |
| `BattleRules` | `CardsPerSide`、编号边界等与 `RunCardRules` 唯一常量源对齐；保留敌方编号、行动和伤害规则，不改 BattleSystem 终局判定 |

不新增每帧 System：BattleSystem 继续只产生 Result，切 Stage 属于存活期事件订阅，由 StageListener 承担。

## 4. UI部分

### 4.1 涉及到的UI部分概览

| 项目 | 确定方案 |
| --- | --- |
| 静态页面 | `PreparationView.prefab` 保存背景、标题、奖励反馈、槽位区、池底框、ScrollRect/Viewport/Content/Scrollbar；Controller 不运行时拼整页 |
| 卡池 UiList | `Content` 顶部锚定，`UiList.ArragementType=ConstantSlot`、`ConstantSlotDirection=Horizontal`；Builder 由 Viewport 可用宽度除以 7 得 cellWidth，按 `2:3` 得 cellHeight，Content 宽=`7*cellWidth`、高=`14*cellHeight`，一次池化 98 个条目，形成严格行优先 `01~98` |
| 滚动裁切 | `ScrollRect.horizontal=false`、`vertical=true`、`movementType=Clamped`；Viewport 带 Image+RectMask2D/Mask，Content pivot=(0.5,1)；纵向 Scrollbar 绑定 track/handle，宽度从可用区扣除且不覆盖第 7 列；槽位区位于 ScrollRect 外 |
| 槽位 UiList | 独立 `BattleSlotList` 使用 `ConstantSlot/Horizontal`，3 个固定 `PreparationSlotItemController`；像素尺寸/间距只在 Builder，不进入规则配置 |
| 拖放 requester/responder | Card 持有态或 Slot 占用态的 `UiInteractor.ExtraInfo` 保存 source CardNumber/可选 SourceSlot；被拖对象是 requester；3 个 Slot 的 interactor 是唯一 responder，ExtraInfo 保存 TargetSlot；只有 responder Controller 处理 `OnInteract` 并核对双方类型，避免 requester/responder 双回调重复提交 |
| 拖动复位与排序 | `UiDragable.TurnBackWhenDragEnd=true`；BeginDrag 由框架 `SetUiTop`，EndDrag 先完成/取消提交并 `SetTopUiBack`；`OnBackFromTop` 后调用所属 UiList `RefreshLayout`/页面刷新恢复原父级、sibling 与 anchoredPosition，清全部高亮 |
| 空槽与滚动冲突 | 未持有池位和空战斗槽禁用 source `UiDragable`，但战斗槽 responder 保持启用；非卡面区域与空池位不截获 Drag，交给 ScrollRect 纵向滚动；卡面开始 Drag 后不触发列表滚动 |
| Builder/资源 | `PreparationViewUiBuilder.Build()`、`PreparationCardItemUiBuilder.Build()`、`PreparationSlotItemUiBuilder.Build()` 一一对应；Card Controller 用 `DataApi` + `ResourceApi`，不复用 BattleCardItem Prefab/Controller |

### 4.2 新增Ui/Hud

| View类名 | 对应页面/条目 | 主要控件列表 |
| --- | --- | --- |
| `PreparationView` | 备战页面 | 背景、标题/奖励 TMP、3 槽 UiList、卡池 ScrollRect/Viewport/Content/98条目 UiList、Scrollbar |
| `PreparationCardItemView` | 固定编号卡位 | 空槽、卡面、原画、名称、永久攻/血、编号牌、UiDragable、UiInteractor |
| `PreparationSlotItemView` | 3 个战斗槽 | 空/占用卡面、永久攻/血、目标高亮、UiDragable、UiInteractor |

| Controller类名 | 数据监听来源 | 监听与响应行为 |
| --- | --- | --- |
| `PreparationController` | Run state `Revision` + Preparation session | Open 生命周期监听；Open 一次创建 98+3 条目并绑定唯一状态；Dirty 全量重绑轻量表现；Close 解绑 |
| `PreparationCardItemController` | Run state 对应 `RunCardInstanceData` | 显示持有/空态及永久值；只有持有态启用 requester；不保存所有权 |
| `PreparationSlotItemController` | Run state 槽索引/实例 | 显示空/占用/高亮；始终可作 responder、占用时可作 requester；只向页面提交强类型 drop |

### 4.3 UiScene配置与导出

#### 4.3.1 新增UiScene

| UI编辑场景路径 | UiScene类与UiGroup | Group列表 | 纳入的View Prefab | `FullUiGroupType` | 导出Asset路径 | 所属GameStage |
| --- | --- | --- | --- | --- | --- | --- |
| `Assets/Scenes/Ui/Preparation.unity` | `PreparationUiScene` / `EPreparationUiGroup` | `Main` | `Assets/Resources/Ui/PreparationView.prefab`（Connected、DefaultShow=true） | `Hearthstone.EPreparationUiGroup` | `Assets/Resources/Ui/Preparation.asset` | PreparationStage |

动态 `PreparationCardItem.prefab`、`PreparationSlotItem.prefab` 不作为场景 View 导出；分别执行 `Pre-UiInit` 与 `Export as Pre-load`，由 Editor 流程更新 `Assets/Resources/BbxCommon/Ui/PreLoadUiData.asset` 的 Controller→Resources Prefab 映射。

#### 4.3.2 UiScene完整性检查

| 环节 | 完成标准 |
| --- | --- |
| Prefab/Builder | 3 个 Resources Prefab 根 View、引用完整；3 个 Builder 可重复生成等价层级且严格一一对应 |
| 编辑场景 | Canvas/CanvasScaler 与 `UiCanvasProto` 一致；恰好一个 Exporter；Main 下页面保持 Connected Prefab |
| 导出/运行时 | 从活动场景实际导出 `Preparation.asset`，核对全部字段；精确 `Resources.Load("Ui/Preparation")` 和两类 PreLoad 映射均非空；Stage 才可 `SetUiScene` |
| 禁止项 | 不手写 Scene、Prefab、`.asset` YAML，不直接改 `UiObjectDatas`，不创建/编辑/删除 `.meta`；只经 Unity Editor、UiBuilder、UiSceneExporter 和公开 Stage API |

## 5. 美术部分

### 5.1 涉及到的美术表现概览

沿用 `UI-STYLE-001`；完整卡面复用现有卡框、编号/属性徽章和 `Assets/Resources/Art/BattleCards/` 原画，但由独立 Preparation Prefab 重新编排并显示 Run state 永久攻血。备战背景、标题、槽位、池框、滚动条、奖励反馈与高亮使用专属正式 Sprite。

### 5.2 美术资产完整性检查

| 资产组 | 候选已有资产 | 复用结论/依据 | 缺失与处理 |
| --- | --- | --- | --- |
| 卡面与编号/属性 | `CardFrame-v3.png`、`CardNumberBadgeHex.png`、`AttackBadgeFrame.png`、`HealthDropBadge.png`、`BattleCards/*.png` | 直接复用 Sprite；规格/语义满足，不改 Battle Prefab | 无 |
| 页面大框与分区 | `BattleBoardBackground.png` | 不直接复用；不含深蓝池区和备战层级 | 新增专属背景、标题、分隔线、池面板、奖励面板 |
| 槽位/滚动/反馈 | 无 | 无法复用 | 新增空槽、Scrollbar 状态组、Drop Highlight |

### 5.3 新增美术资产

| 资产名或资产组 | 类型/规格 | 预期路径 |
| --- | --- | --- |
| `PreparationPageBackground` | `16:9` 全屏 PNG，羊皮纸上区+深蓝池区+木金外框，不含文字/卡牌 | `Assets/Resources/Art/Preparation/UI/PreparationPageBackground.png` |
| `PreparationStageTitleFrame`、`PreparationRewardPanel`、`PreparationSectionLine` | 透明 PNG，红金底框与可延展装饰线 | `Assets/Resources/Art/Preparation/UI/` 同名文件 |
| `PreparationBattleSlotFrame`、`PreparationPoolEmptySlot` | `2:3` 透明 PNG，完整金边且两种空态语义可区分 | 同目录同名文件 |
| `PreparationCardPoolPanel` | 深蓝九宫格透明 PNG，完整包围 7 列且给滚动条留位 | `Assets/Resources/Art/Preparation/UI/PreparationCardPoolPanel.png` |
| `PreparationScrollTrack/Thumb/Arrow` | 透明 PNG 状态组，正常/悬停/按下可辨识 | 同目录同名前缀文件 |
| `PreparationDropHighlight` | `2:3` 红金透明高亮，不遮挡卡面 | `Assets/Resources/Art/Preparation/UI/PreparationDropHighlight.png` |

## 6. GameStage部分

### 6.1 新增GameStage

| GameStage名 | 包含项与组合 |
| --- | --- |
| `RunStateStage` | `InitializeRunStateRuntime`；OnAwake 创建一次并保存在 GameEngine，Battle/Preparation 两个 Group 都包含，普通互切不卸载 |
| `PreparationStage` | 当前 batch 快照、`InitializePreparationRuntime`、由 `Preparation.unity` 导出的 UiSceneAsset；Group=`RunStateStage + PreparationStage`，不含新 System/DataGroup/gameplay Scene |

Battle Group 调整为 `RunStateStage + BattleStage`。`EnterBattleStageGroup(BattleStageStartupData)` 与 `EnterPreparationStageGroup(PreparationRewardBatchStartupData)` 都先防御性快照、创建新的短 Stage，再一次提交完整集合；不得在 SetActive 后补造输入。备战后返回 Battle 的触发器不在本篇。

### 6.2 新增LoadItem项

| LoadItem项名 | 负责内容 | 所属GameStage |
| --- | --- | --- |
| `InitializeRunStateRuntime` | 创建/最终移除整局唯一 Run state 单例 | 新增 RunStateStage |
| `InitializePreparationRuntime` | 原子应用/幂等识别 batch，创建本次展示单例；Unload 只移除展示单例 | 新增 PreparationStage |

### 6.3 新增注册项

| 项名 | 负责内容 | 所属GameStage |
| --- | --- | --- |
| `BattleResultPreparationStageListener` | 监听首次终局并自动进入正式 Preparation Group | 已有 BattleStage |
| `PreparationUiScene` / `EPreparationUiGroup.Main` | 消费 4.3 从编辑场景实际导出的 `Preparation.asset` | 新增 PreparationStage |
| `PreparationStageEntryAsset` | 隔离验收时编辑 batch 并直达正式 Preparation Group；不参与生产 Battle→Preparation | Editor，脚本位于 `Assets/Scripts/Hearthstone/Editor/GameStage/`，资产由窗口创建到 `Assets/Resources/Editor/` |

### 6.4 修改LoadItem项

| LoadItem项名 | 改动内容 | 所属GameStage |
| --- | --- | --- |
| `InitializeBattleRuntime` | 接收 Battle startup 快照；确保首次旧默认阵容持久化；玩家从 Run state 永久实例建 Entity、敌方维持旧随机配置；session 保存待发 batch；Unload 不清 Run state | 已有 BattleStage |

## 7. 实现顺序建议

| 步骤/Todo | 实施内容 |
| --- | --- |
| 1 | 新增 `RunCardRules`、三种 StartupData/值对象与深拷贝/结构校验，集中全部不变契约。 |
| 2 | 新增 Run state/Preparation session Component，落实固定容器、Applied BatchId、Revision、Invalid/回收。 |
| 3 | 实现批次两阶段原子应用与编成规则；新增独立 `RunCardRulesTests.cs` 覆盖同 BatchId 无 Revision、重叠新 batch 全拒绝、恰好新增5张、拖放规则；不改既有 Battle 测试。 |
| 4 | 修改 Battle startup/session/card 初始化/BattleStages，完成首次旧阵容持久化、玩家永久值、敌方旧随机与待发 batch。 |
| 5 | 新增结果 StageListener，修改 GameEngine 为持久 RunStateStage 与两个完整 Group；扩展 Initial entry/明确运行时默认 batch，打通同 Play 自动切换。 |
| 6 | 新增三对 View/Controller，按 4.1 落实 98+3 UiList、ScrollRect 裁切/尺寸、requester-responder、复位/排序/空槽禁用。 |
| 7 | 创建第 5.3 节正式 Sprite；更新 `AutoDoc/Art/UI/ui-art-overview.md` 与新增 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md`。 |
| 8 | 新增并在 Unity Editor 分别执行三个一一对应 UiBuilder，生成/更新 Prefab；执行 Pre-UiInit 与动态条目 Pre-load 导出。 |
| 9 | 新增 UiGroup/UiScene；用 Unity Editor 创建 `Assets/Scenes/Ui/Preparation.unity`，配置 Canvas、Exporter、Main 与 Connected 页面 Prefab。 |
| 10 | 从活动编辑场景实际导出 `Assets/Resources/Ui/Preparation.asset`，核对字段、Resources、PreLoad 与可重复导出；禁止手写 YAML/.meta。 |
| 11 | 新增 Preparation 隔离 Editor entry；同步 `AutoDoc/Program/Specific/preparation-card-pool/preparation-card-pool.md`、`AutoDoc/Program/UI/preparation/preparation.md`、`AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md` 与 Battle 相关现状文档。 |
| 12 | 完成编译、独立 Editor tests、Prefab/Scene/Resources/Console 检查；再从正式入口执行截图组 A～D 与 `RGR-01`～`RGR-05`，分别维护 ART、FUNC、回归映射。 |

Todo 判定：步骤 1～12 名称、顺序与本表一致；发现 Run state 被短 Stage 清除、batch 部分写入/重复 Revision、玩家卡重新随机、UiList 非 7×14、拖放双提交、UiScene 不可重导出或触及既有 BattleCardItem 并发范围时，回到对应步骤整改，不以兼容补丁或静态检查判定完成。
