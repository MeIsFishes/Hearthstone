# 逐轮配置与战斗结算流程改造报告

## 结果

任务已完成。游戏现在从第 1 轮备战开始；玩家首轮随机获得 3 张普通卡并解锁 3 个出战槽。每轮的新增槽位数与摸牌数由 `Assets/Resources/Config/BattleProgressionCsvData.csv` 分别配置，玩家出战槽与战斗数组按当前累计解锁数在 `3~6` 之间变化。单方单位耗尽后延迟 `0.5 s` 才提交战斗结果；玩家胜利播放左入、停留、右出的横幅，非最终轮演出完成后进入下一轮备战，失败与最终轮胜利显示带“重新开始”按钮的结果弹窗。

## 实现摘要

- 配置：`BattleProgressionCsvData` 提供连续轮次、每轮新增槽位、每轮摸牌数、累计槽位及最终轮判断。当前示例共 5 轮，首轮为 `3/3`，累计槽位上限为 6。
- 整局状态：`RunStateSingletonRawComponent` 使用六槽存储并保存 `UnlockedBattleSlotCount`；`PreparationSessionSingletonRawComponent` 保存本轮编号与动态奖励快照。
- Stage 流程：数据加载完成后先进入第 1 轮 PreparationStage；Continue 进入同轮 BattleStage；非最终胜利横幅完成后进入下一轮备战；失败与最终胜利留场等待重开。
- 战斗：`BattleSessionSingletonRawComponent.PlayerCards` 为动态长度，敌方保持当前三个默认槽位；`BattleSystem` 使用挂起结果与 `0.5 s` 倒计时阻止继续攻击或重复结算。
- UI：`PreparationController` 只创建当前已解锁出战槽；`BattleController` 驱动胜利横幅和两类结果弹窗；`RestartRun()` 清空整局状态并回到首轮备战。
- Prefab：`BattleViewUiBuilder` 一一对应维护 `BattleView.prefab` 的战场、动态卡牌列表、横幅、结果弹窗和重开按钮；Preparation Prefab 的出战列表按六槽上限扩宽。

## 美术生成记录

生成源目录：`C:\Users\黄昕玮\.codex\generated_images\01a0066d-3fad-7c41-b0e8-63df50f11604`

### 胜利横幅

提示词：

> Create a single transparent-background 2D game UI asset: a wide victory banner frame for a fantasy card battler. Match an established Hearthstone-like interface with polished antique gold trim, deep royal blue enamel and a restrained warm glow, symmetrical pointed ribbon ends that imply fast lateral motion, elegant high-fantasy craftsmanship, crisp readable silhouette at 900x240. Leave the central area clean and empty for separately rendered Chinese UI text. No words, no letters, no logos, no watermark, no drop shadow beyond the object, isolated PNG with true alpha transparency.

- 生成文件：`exec-7052c9a0-a6ef-4c86-b1ff-23067e465272.png`
- 项目资产：`Assets/Resources/Art/BattleCards/Result/BattleVictoryBanner.png`
- 尺寸：`1983 × 793`
- 接入：`BattleView/VictoryBanner`，中文“战斗胜利”由 TMP 叠加。

### 失败弹窗

提示词：

> Create a single transparent-background 2D game UI asset: a defeat/restart modal panel frame for a fantasy card battler. Match an established parchment, dark crimson enamel, aged iron and antique gold interface. Broad rectangular panel with subtly broken shield motifs and subdued red embers, clear empty central area for separately rendered Chinese title/body/button UI text. Front-facing, symmetrical, premium polished game UI, crisp silhouette at roughly 760x520. No words, no letters, no logos, no watermark, isolated PNG with true alpha transparency.

- 生成文件：`exec-42a735f1-619d-4608-af83-df6cd12d58ae.png`
- 项目资产：`Assets/Resources/Art/BattleCards/Result/BattleDefeatPanel.png`
- 尺寸：`1517 × 1037`
- 接入：失败时的 `BattleView/ResultPopup`，标题、说明和按钮由 TMP 叠加。

### 整局胜利弹窗

提示词：

> Create a single transparent-background 2D game UI asset: a whole-run victory/restart modal panel frame for a fantasy card battler. Match an established parchment, luminous royal blue enamel, radiant antique gold and small celebratory gem accents. Broad rectangular panel with upward laurel-like flourishes, clear empty central area for separately rendered Chinese title/body/button UI text. Front-facing, symmetrical, premium polished game UI, crisp silhouette at roughly 760x520. No words, no letters, no logos, no watermark, isolated PNG with true alpha transparency.

- 生成文件：`exec-4e28d35f-5dca-465f-bacf-278a79f526cb.png`
- 项目资产：`Assets/Resources/Art/BattleCards/Result/RunVictoryPanel.png`
- 尺寸：`1519 × 1036`
- 接入：最终轮横幅演出结束后的 `BattleView/ResultPopup`，标题、说明和按钮由 TMP 叠加。

三张图片均已视觉检查。`BattleViewUiBuilder` 将三张资源统一导入为 Single Sprite、开启 Alpha Transparency、关闭 Mipmap 并使用 Clamp；精确中文未烘入图片。

## 检查清单结果与证据

### 需求覆盖

1. 通过：`OnStageLoadingCompleted()` 调用 `BeginPreparationForBattle(1)`；首行校验固定解锁 3 槽、摸 3 张。
2. 通过：轮次 CSV 提供 `BattleNumber/UnlockSlotCount/DrawCardCount` 三列。
3. 通过：Run state、规则层、备战 UiList、Continue 快照和玩家 BattleSession 均读取当前累计解锁数。
4. 通过：`ResultSettlementDelay = 0.5f`；挂起期间 `BattleSystem` 提前返回。
5. 通过：胜利横幅时序为 `0.24/0.68/0.24 s`，坐标为 `-1450 → 0 → 1450`。
6. 通过：失败加载 `BattleDefeatPanel`，显示“战斗失败”“本局冒险已经结束”“重新开始”。
7. 通过：无下一轮配置时 `IsFinalBattle=true`，横幅后加载 `RunVictoryPanel`。
8. 通过：`RestartRun()` 替换 RunStateStage、重置协调状态并重新进入第 1 轮。
9. 通过：三张新增结果美术已进入 Resources 并完成 Prefab/Controller 接入。

### 配置与框架边界

1. 通过：新配置继承 `CsvDataBase<T>`，使用 Override、规范表名、两行 CSV 注释和 DataApi 查询。
2. 通过：CSV 仅保存静态轮次规则，可变状态保存在 Run/Preparation/Battle Component 与启动快照。
3. 通过：StageGroup 切换仍由 GameEngine/StageListener 负责，Controller 未复制业务流程状态机。
4. 通过：View 只保存序列化引用，Controller 监听/驱动表现，Builder 维护 Prefab 静态层级。
5. 不适用：未新增或修改自定义 `BbxUiItem`，无需新增 `AutoDoc/UIItem` 文档。
6. 通过：动态卡牌和槽位继续使用现有 `UiList` 条目池与生命周期。
7. 通过：未手工操作 `.meta`，Unity 刷新时自动生成新资产元数据；未回退无关工作区改动。

### 美术与资产

1. 通过：三张图片由 imagegen 按现有红蓝金奇幻 UI 风格生成，提示词明确要求无文字和真实透明背景。
2. 通过：生成结果已视觉检查并复制到稳定 Resources 路径。
3. 通过：所有精确中文由 TMP 负责。
4. 通过：提示词、源文件、最终路径、尺寸和接入位置已在本报告记录。

### 验证与文档

1. 通过：静态调用链覆盖首轮、普通下一轮、最终轮胜利、失败和重开五条关键路径。
2. 通过：动态阵容覆盖 `3~6`，奖励批次不再强制 5 张；轮次配置单元测试验证累计槽位与摸牌字段。
3. 通过：挂起结果分支位于攻击表现和 `ExecuteAction()` 之前，期间不会继续攻击；重复 Begin 受挂起标记保护。
4. 通过：StageListener 只在 `OutcomePresentationCompleted` 后推进；最终轮弹窗在 UI 横幅完整时序后显示。
5. 通过：两个受影响程序集串行构建均 0 错误；Unity EditMode 83/83 通过。
6. 通过：已同步 Design、Program、Program/UI、Art/UI、战斗卡牌与备战卡池美术现状文档。
7. 通过：未发现需要修改 BbxCommon 通用框架的能力缺口。
8. 通过：Unity 运行资源检查结果为 `CSV=True|Banner=True|Defeat=True|RunVictory=True|Refs=True`；活动场景为未修改的 `Assets/Scenes/Main.unity`；测试预期错误日志清理后 Console 为 0 错误。
9. 通过：`AutoDoc/CleanupTempDocs.bat` 已且仅已运行一次，退出码为 0、无输出；脚本按自身策略保留了既有 Temp 内容与本任务 Checklist，随后创建本 Report。

## 验证记录

- `dotnet build Hearthstone.csproj --no-restore`：通过，0 错误；存在项目既有程序集版本冲突警告。
- `dotnet build Hearthstone.Editor.csproj --no-restore`：通过，0 错误；存在同类既有警告。
- Unity EditMode job `97489829e2c443f8bc9c36673ad1a6a2`：83 通过、0 失败、0 跳过，耗时约 `3.65 s`。
- Unity Resources/Prefab：轮次 CSV、三张结果 Sprite、BattleView 全部必需引用均加载成功。
- `git diff --check`（脚本、CSV、现状文档范围）：无空白错误；Unity 自动序列化的 Prefab 仍带项目既有 YAML 行尾空格格式。
- Play Mode：未执行，遵循项目默认“不主动进入游戏验证”的规则。

## 偏差与风险

- 敌方仍保持当前三个默认槽位；本次需求只调整玩家每轮出战上限，因此没有扩展敌方阵容。
- 玩家槽位最大值当前为 6，轮次 CSV 的累计解锁数会校验不得超过该 UI/运行时边界。
- 未做 Play Mode 端到端视觉验收；横幅、弹窗、重开和轮间切换已通过静态链路、资源加载、Prefab 引用、编译和 EditMode 测试验证，仍建议后续人工运行一次完整五轮流程确认屏幕观感与节奏。
- 初次并行执行两个 dotnet build 时共享 `obj/Debug/Hearthstone.dll` 发生文件锁；改为串行后两者均成功，未修改或清理用户中间产物。
- 整局胜利图片初次检查发现尚未导入为 Sprite；已将其纳入 `BattleViewUiBuilder` 的统一导入流程并复验为可加载。
