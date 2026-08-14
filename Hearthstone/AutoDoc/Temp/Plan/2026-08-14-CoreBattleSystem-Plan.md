# 核心自动战斗系统实现 Plan

## 1. 需求明确

### 1.1 需求对齐

1. 进入战斗模式后，我方和敌方各创建 3 张牌；每张牌固定为 5 点最大生命、3 点攻击。
2. 我方先行动，随后敌我严格交替；双方各自按原始槽位 1→2→3→1 循环选取攻击者。
3. 死亡牌立即退出攻击序列和索敌候选，槽位不重排；攻击游标从原位置继续向右跳过死亡牌。
4. 每次攻击从对方存活牌中等概率随机索敌。攻击者与目标同时受到对方攻击力造成的伤害，然后统一处理死亡。
5. 每次结算后立即判胜负并停止后续行动：敌方空场优先判我方胜利，否则我方空场判敌方胜利；双方同时空场也判我方胜利，不设平局。
6. 随机索敌使用战斗会话持有的非零种子与连续随机状态，使相同种子和相同战斗状态可复现。
7. 首版为纯自动战斗。最小 UI 显示敌我各三张牌、攻击/当前生命、当前行动方、攻击者与目标高亮、最终胜负；攻击间隔固定为 0.75 秒，不制作复杂动画。
8. 不包含技能、关键词、英雄与战后伤害、酒馆阶段、奖励、联网、存档、正式美术和音频。

## 2. 数据部分

### 2.1 涉及到的数据概览

- 卡牌实体是单张牌运行时状态的唯一权威来源；每张牌保存阵营、固定槽位、攻击、最大生命、当前生命和存活状态。
- 战斗会话 Singleton 是整场战斗状态的唯一权威来源；保存双方实体引用、各自行进游标、当前行动方、当前攻击者/目标、胜负、行动计时与随机状态。
- System 只读取和修改上述 Component；Controller 只监听 Component 并刷新 View，不复制生命、轮次或胜负状态。
- 本原型的 3 张牌、5 血、3 攻和 0.75 秒行动间隔是已确认的固定规则，集中放入领域类 `BattleRules` 的常量，不新增 CsvData、ScriptableObject 或 DataGroup。

### 2.2 新增数据列表

运行时数据全部在 BattleStage 加载时创建、卸载时回收；不跨战斗保存。

#### 2.2.1 新增 Component 类

| 类名 | 重要字段 | 归属 Entity |
|---|---|---|
| `BattleCardRawComponent` | `EBattleSide Side`、`int SlotIndex`、`int Attack`、`int MaxHealth`、`ListenableVariable<int> CurrentHealth`、`ListenableVariable<bool> IsAlive`；回收时使监听变量失效并恢复默认值 | 每张卡牌实体，共 6 个 |
| `BattleSessionSingletonRawComponent` | `Entity[] PlayerCards`、`Entity[] EnemyCards`、`int PlayerAttackCursor`、`int EnemyAttackCursor`、`ListenableVariable<EBattleSide> CurrentSide`、`ListenableVariable<EBattleResult> Result`、`ListenableVariable<Entity> CurrentAttacker`、`ListenableVariable<Entity> CurrentTarget`、`uint RandomSeed`、`Unity.Mathematics.Random TargetRandom`、`float ActionCountdown`、`int ActionIndex`；回收时使监听变量失效、清空 Entity 引用和会话状态 | ECS Singleton Entity，全场唯一 |

`EBattleSide` 固定为 `Player`、`Enemy`；`EBattleResult` 固定为 `InProgress`、`PlayerVictory`、`EnemyVictory`，不定义 `Draw`。

## 3. 游戏逻辑部分

### 3.1 涉及到的游戏逻辑概览

- `BattleRules` 承担无 Unity 场景依赖的领域规则：从游标向右查找存活攻击者并推进游标、先统计存活目标数再用会话内 `TargetRandom.NextInt` 选取对应序号、按结算前攻击力同时扣血、更新死亡、按确定优先级判胜负；索敌过程不创建临时集合。
- `BattleSystem` 每帧只推进会话倒计时；倒计时到零时最多执行一次攻击，避免卡顿帧内连续补算多次而看不到中间状态。
- 一次行动的固定顺序为：选攻击者 → 选存活目标 → 写入高亮 Entity → 快照双方攻击力 → 同时扣血并钳制到 0 → 更新存活状态 → 判胜负 → 若仍在战斗则切换阵营并重置 0.75 秒倒计时。
- 判胜规则固定先检查敌方存活数；敌方为 0 即 `PlayerVictory`，因此双方同时空场也落入我方胜利；仅敌方仍存活且我方为 0 时才是 `EnemyVictory`。
- 战斗结果不再为 `InProgress` 后，System 不再推进计时、索敌或结算。

### 3.2 新增 System、StageListener

#### 3.2.1 新增 System 类

| 类名 | 职责 |
|---|---|
| `[DisableAutoCreation] BattleSystem : EcsMixSystemBase` | 驱动 0.75 秒行动节奏，调用 `BattleRules` 完成双方交替攻击、随机索敌、同时伤害、死亡跳过和胜负结束；通过公开 ECS Component 接口更新唯一运行时状态 |

本模块不新增 StageListener：进入/离开时的一次性建场与清理由 `IStageLoad` 负责，持续模拟由 `BattleSystem` 负责。

## 4. UI 部分

### 4.1 涉及到的 UI 部分概览

- 新增一个默认显示的 `BattleView` 页面，上方是敌方横向三槽，下方是我方横向三槽，中间显示行动方和最终结果。
- 两排重复卡牌通过现有 `UiList` 创建 `BattleCardItemController`，卡牌条目使用独立 Prefab/View/Controller；Controller 不在运行时拼装整页静态层级。
- 卡牌以 UGUI `Image` 平色块和 TextMeshPro 文本实现：攻击者显示金色描边/遮罩，目标显示红色描边/遮罩，死亡牌灰化并保留在原槽位。
- 页面无按钮、拖拽和输入逻辑；所有显示由 ECS `ListenableVariable` 变化驱动。

### 4.2 新增 Ui/Hud

| View 类名 | 对应页面 | 主要控件列表 |
|---|---|---|
| `BattleView` | 核心战斗主页面 | `UiList EnemyCardList`、`UiList PlayerCardList`、`TMP_Text TurnText`、`TMP_Text ResultText` |
| `BattleCardItemView` | 动态卡牌条目 | `Image CardBackground`、`Image AttackerHighlight`、`Image TargetHighlight`、`Image DeadOverlay`、`TMP_Text SlotText`、`TMP_Text AttackText`、`TMP_Text HealthText` |

| Controller 类名 | 数据监听来源 | 监听响应行为 |
|---|---|---|
| `BattleController` | `BattleSessionSingletonRawComponent.CurrentSide`、`Result` | 页面显示时为敌我各创建并绑定 3 个条目；刷新“我方行动/敌方行动”和“战斗中/我方胜利/敌方胜利”；页面关闭时由 `UiList.ClearItems()` 回收条目 |
| `BattleCardItemController` | 绑定 Entity 的 `BattleCardRawComponent.CurrentHealth`、`IsAlive`，以及 `BattleSessionSingletonRawComponent.CurrentAttacker`、`CurrentTarget` | 刷新攻击与生命文本、原始槽位标签、死亡灰化，并根据当前 Entity 是否等于攻击者/目标切换金色或红色高亮；解绑时解除 ModelWrapper 监听 |

### 4.3 UiScene 配置与导出

#### 4.3.1 新增 UiScene

| UI 编辑场景路径 | UiScene 类与 UiGroup 枚举 | Group 列表 | 纳入的 View Prefab | `UiSceneExporter.FullUiGroupType` | 导出 Asset 路径 | 所属 GameStage |
|---|---|---|---|---|---|---|
| `Assets/Scenes/Ui/Battle.unity` | `BattleUiScene`、`EBattleUiGroup` | `Main` | `Assets/Resources/Ui/BattleView.prefab`，默认显示 | `Hearthstone.EBattleUiGroup` | `Assets/Resources/Ui/Battle.asset` | `BattleStage` |

动态条目 Prefab 为 `Assets/Resources/Ui/BattleCardItemView.prefab`，不作为整页放入 UiScene Group；必须在 Unity Inspector 对其 `BattleCardItemView` 执行 **Export as Pre-load**，使 `UiList.AddItem<BattleCardItemController>()` 能通过 `UiApi` 获取实例。

#### 4.3.2 UiScene 完整性检查

1. `BattleView.prefab` 必须存在于 `Assets/Resources/Ui/`，静态层级和序列化引用完整，并在 `Battle.unity` 中保持 Prefab 连接。
2. `Battle.unity` 必须包含 `UiSceneExporter`，`FullUiGroupType` 为 `Hearthstone.EBattleUiGroup`，`Main` Group 下只有页面 Prefab 实例。
3. 必须由 `UiSceneExporter.ExportUiScene()` 生成并校验 `Assets/Resources/Ui/Battle.asset`；不得手写 `.unity`、`.prefab` 或 `.asset` YAML。
4. `BattleCardItemView.prefab` 必须通过 Inspector 的预加载导出入口登记，并用 `UiApi.CapturePreloadedUiPrefabPathsForValidation` 或等价公开校验入口确认路径有效。
5. `HearthstoneGameEngine` 必须能 `GetOrCreateUiScene<BattleUiScene>()`，`BattleStage` 必须引用由编辑场景导出的 `Battle.asset`。
6. Unity 资产的创建、删除、导出与资源字典刷新均通过当前项目配置的 MCP for Unity/Unity Editor 完成；若工具链不可用则停止资产步骤并按项目恢复流程处理，不以文本方式绕过。

## 5. 美术部分

### 5.1 涉及到的美术表现概览

首版只需要功能性占位表现：深色战场背景、敌我两排卡牌色块、白色数值文本、攻击者金色高亮、目标红色高亮和死亡灰化。项目当前没有游戏用 2D 图片资产，也没有可复用的现状美术文档；因此本轮不新增位图，不把第三方插件 Logo 当作游戏资产。

### 5.2 美术资产完整性检查

| 资产或资产组 | 用途 | 候选已有资产及路径 | 复用结论 | 判断依据 | 缺失或不满足需求的内容 | 处理方式 |
|---|---|---|---|---|---|---|
| 全屏 UI 容器 | 承载战斗页面 | `Assets/Resources/Ui/CanvasProto.prefab` | 直接复用 | 已配置 Screen Space Overlay、1920×1080 参考分辨率与 GraphicRaycaster | 无 | 保持原资源不改图像内容 |
| 战场背景与卡牌底板 | 区分背景、阵营和卡牌区域 | 无游戏用图片资产 | 不需要图片资产 | 首版只要求最小占位 UI，UGUI `Image` 纯色即可满足层级辨识 | 正式背景、卡框与角色图 | 本轮不新增；直接在 Prefab 上配置内置白色 Sprite 的颜色 |
| 攻击者、目标与死亡状态 | 反馈当前结算对象和死亡 | 无 | 不需要图片资产 | 颜色遮罩/描边即可表达金色、红色和灰化状态 | 正式特效与动画 | 本轮不新增；使用 `Image` 显隐和颜色切换 |

## 6. GameStage 部分

### 6.1 新增 GameStage

| GameStage 名 | 包含项 |
|---|---|
| `BattleStage` | `InitializeBattleRuntime` LoadItem、`BattleSystem` UpdateSystem、由 `Battle.unity` 导出的 `Battle.asset`/`BattleUiScene`；不新增 Scene、DataGroup、StageListener 或 LateLoadItem |

### 6.2 新增 LoadItem 和 LateLoadItem 项

| LoadItem 项名 | 负责内容 | 所属 GameStage |
|---|---|---|
| `InitializeBattleRuntime` | Load 时通过 `EcsApi.AddSingletonRawComponent` 创建会话，通过 `EcsApi.CreateEntity("BattleCard")` 创建 6 个卡牌 Entity 并附加/初始化 `BattleCardRawComponent`，按槽位写入双方数组，生成非零随机种子，设置我方先手和 0.75 秒首次倒计时；Unload 时逐个 `EcsApi.DestroyEntity` 并移除 Singleton | 新增 `BattleStage` |

不新增 LateLoadItem：战斗没有配置 DataGroup，UI 与 System 在 LoadItem 完成后进入 Stage 生命周期即可读取完整会话。

### 6.3 新增注册项

| 项名 | 负责内容 | 所属 GameStage |
|---|---|---|
| `BattleSystem` | 通过 `AddUpdateSystem<BattleSystem>()` 驱动自动战斗 | `BattleStage` |
| `BattleUiScene` + `Battle.asset` | 通过 `GetOrCreateUiScene<BattleUiScene>()` 与 `SetUiScene` 加载 4.3 中由编辑场景导出的 UI | `BattleStage` |
| `HearthstoneGameEngine` System 顺序 | 将 `BattleSystem` 注册在 `InputSystem` 之后、`TaskSystem` 之前；移除 `PlaceholderStateSystem` | 全局 GameEngine 注册表 |
| `EnterBattleStageGroup()` | 缓存 `BattleStage` 并以 `StageWrapper.SetActiveGameStage(m_BattleStage)` 作为唯一模式切换边界；`OnAwake()` 默认进入该 Group | `HearthstoneGameEngine` |

现有 `Main.unity`、`Bootstrap.prefab`、`CanvasProto.prefab` 和 `InitialStageEntry.asset` 继续复用；入口 Asset 仍只等待 GameEngine 就绪，因为 GameEngine 启动时已经进入 BattleStage Group。

### 6.4 删除 GameStage 项

| 项名 | 负责内容 | 所属 GameStage |
|---|---|---|
| 原启动 `BaseStage` | 从 `HearthstoneGameEngine.OnAwake()` 的活动 StageGroup 和 System 顺序中移出，不再创建占位 Singleton、运行 `PlaceholderStateSystem` 或加载 Placeholder UI；占位源码与 Unity 资产保持未激活，不在本核心战斗范围内做破坏性清理 | 原 `BaseStage` |

## 7. 实现顺序建议

以下实现顺序和 Todo 一一对应，只有前一项的公开契约稳定后才进入依赖它的下一项：

1. [ ] **实现 Battle Component 与枚举**：新增 `BattleCardRawComponent`、`BattleSessionSingletonRawComponent`、`EBattleSide` 和无平局的 `EBattleResult`，完成 `ListenableVariable` 失效与回收复位。
2. [ ] **实现确定性领域规则**：新增 `BattleRules`，固定 3 卡、5 血、3 攻、0.75 秒间隔，实现游标环回、死亡跳过、存活目标无分配随机、同时伤害和敌方空场优先判胜。
3. [ ] **实现自动战斗 System**：新增 `[DisableAutoCreation] BattleSystem`，每个到期帧最多结算一次并在终局后停止。
4. [ ] **补齐规则级自动测试**：覆盖首手与交替顺序、5/3 首击各剩 2 血、游标环回、死亡跳过、随机目标只含存活牌、同种子复现、双方同时空场判我方胜利和终局停止。
5. [ ] **实现 Battle View/Controller**：新增页面与卡牌条目 View/Controller，使用 `ModelWrapper` 监听 ECS 状态，使用 `UiList` 创建/回收 6 个动态条目，不保存平行战斗数据。
6. [ ] **创建并绑定 UI Prefab**：通过 MCP for Unity/Unity Editor 创建 `BattleView.prefab` 与 `BattleCardItemView.prefab`，绑定全部序列化引用，完成 1920×1080 敌上我下布局和纯色状态表现。
7. [ ] **导出动态条目预加载映射**：在 `BattleCardItemView` Inspector 执行 **Export as Pre-load**，保存并校验预加载路径，使 `UiList.AddItem` 可用。
8. [ ] **创建 Battle UI 编辑场景**：通过 MCP for Unity/Unity Editor 创建 `Assets/Scenes/Ui/Battle.unity`，添加 `UiSceneExporter`、生成 `Main` Group，并以 Prefab 实例方式放入 `BattleView`。
9. [ ] **配置 UiScene Exporter 与 Prefab 归属**：设置 `FullUiGroupType=Hearthstone.EBattleUiGroup`、导出目录、默认显示和 Prefab 连接，逐项检查引用完整性。
10. [ ] **执行 UiScene 导出并校验 Asset**：运行 `ExportUiScene()` 生成 `Assets/Resources/Ui/Battle.asset`，刷新资源字典，校验 PrefabPath、Group、默认显示和变换数据来自编辑场景。
11. [ ] **实现 BattleStage LoadItem**：新增 `InitializeBattleRuntime`，用公开 `EcsApi` 创建/销毁会话与 6 个卡牌 Entity，保证卸载无残留。
12. [ ] **注册 BattleStage 项**：新增 `BattleStages.CreateBattleStage`，逐项加入 LoadItem、`BattleSystem` 和步骤 10 导出的 `BattleUiScene` Asset，不添加无需求的 Scene/DataGroup/LateLoadItem。
13. [ ] **切换 GameEngine 启动链路**：注册 `BattleSystem` 稳定顺序，实现 `EnterBattleStageGroup()`，让 `OnAwake()` 默认通过 `SetActiveGameStage` 进入战斗。
14. [ ] **停用占位启动链路**：确认 Battle 链路可加载后，使 `OnAwake()` 不再注册 `PlaceholderStateSystem` 或进入原 `BaseStage`；保留未激活脚手架文件，不创建、编辑或删除任何 `.meta` 文件。
15. [ ] **执行编译与框架边界验证**：检查 Console 无编译/预加载/UI 导出错误，确认业务层只走 `EcsApi`、`UiApi`、GameStage 和官方导出流程，确认运行时不会加载 Placeholder Stage/UI。
16. [ ] **执行 Play Mode 最小验收**：默认不主动进入游戏；获得用户要求或在实施任务明确需要时，通过 GameStage 入口验证双方各 3 张 5 血 3 攻、我方先手、交替顺序、死亡跳过、随机活体索敌、同时伤害、两种胜负和终局停止。

Plan 完成标准：代码测试和 Editor 资产链路均通过后，首次真实战斗模式在启动链路上完全取代 Placeholder；任何 Unity MCP/Editor 连接失败都按项目恢复 skill 先恢复并做端到端只读验证，不能改用手写 Unity YAML。
