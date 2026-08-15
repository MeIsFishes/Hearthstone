# 备战卡牌收纳动效任务报告

## 1. 任务结果

任务已完成。融合结果卡在卡外点击确认后，以 `0.36 s` 快速缩至 `0.3` 并移动到遮罩局部坐标 `x=0` 的屏幕底端，完全离开后才关闭揭晓层。每轮新发放卡牌会在 `78%` 灰色全屏遮罩上从底端依次滑入中央横排，等待卡外点击确认后再从左到右依次收纳到底端；卡面停留期间保留标准悬停词条。

奖励层上方新增透明图片艺术字“获得卡牌”，并将卡牌库“查看拥有”改为 Prefab 与 Controller 打开态均默认勾选。奖励发牌、奖励收纳和融合结果收纳均接入受管音效与生命周期停止逻辑。

## 2. 主要产物

- `Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs`
  - 新增融合结果收纳段、奖励展示三态时间轴、共享底端目标与缩放 helper、音效分组，以及奖励批次防重复确认。
  - `OwnedOnlyToggle` 打开态默认同步为已勾选。
- `Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs`
  - 新增本轮奖励实例的共享卡面绑定入口，继续复用现有词条 Tooltip。
- `Assets/Scripts/Hearthstone/Ui/View/PreparationView.cs`
  - 新增奖励遮罩、CanvasGroup、确认 Button 和 UiList 的必要序列化引用。
- `Assets/Scripts/Hearthstone/Ui/Editor/PreparationViewUiBuilder.cs`
  - 固化 `RewardRevealOverlay`、`RewardCardList`、`RewardTitle` 与默认勾选状态。
- `Assets/Resources/Ui/PreparationView.prefab`
  - 已由 Unity Editor 执行 `Hearthstone.PreparationViewUiBuilder.Build()` 重建。
- `Assets/Resources/Art/Preparation/UI/PreparationRewardTitle.png`
  - 新增 `2079 × 756`、32-bit ARGB、透明背景的“获得卡牌”艺术字。
- `Assets/Scripts/Hearthstone/Tests/Editor/RunCardRulesTests.cs`
  - 更新 Prefab/资源测试与默认筛选测试，新增奖励发牌/收纳/音效测试，并扩展融合揭晓测试。

## 3. 检查项结果与证据

### 3.1 用户体验

- 通过：融合结果确认后先收纳、后关闭；终点缩放精确为 `0.3`。
- 通过：新奖励只读取 `WasNewlyApplied` 与 `RewardCards`，不会把既有牌库重复展示；同批次确认后不会重播。
- 通过：奖励卡按 `0.14 s` 间隔发入中央横排，确认后按 `0.11 s` 间隔逐张收纳。
- 通过：奖励与融合遮罩均采用卡面外点击，卡面自身继续提供词条 Tooltip；未新增确认提示文字。
- 通过：`RewardTitle` 显示“获得卡牌”，位于 `(0, 270)`，UI 尺寸为 `620 × 225`，关闭射线命中。
- 通过：卡牌库默认勾选“查看拥有”，取消勾选后原完整总览逻辑仍保留。

### 3.2 UI 与框架边界

- 通过：静态 UI 全部由一一对应的 `PreparationViewUiBuilder` 生成，没有在 Controller 运行时拼装页面层级。
- 通过：奖励卡、融合素材和结果卡继续使用 `BattleCardItemController → Ui/BattleCardItem` 预加载映射与 `UiList` 回收。
- 通过：View 只保存运行时必须控制的四个奖励层引用；静态艺术字不增加 View 字段。
- 通过：底端目标由遮罩与卡片 RectTransform 的局部矩形计算，兼容 CanvasScaler。
- 通过：未改 UiGroup、DefaultShow、UI Scene、`Preparation.asset` 或导出信息，未手写 Prefab/Scene YAML。
- 不适用：未发现需要修改 BbxCommon 公开契约或生命周期的框架能力缺口。

### 3.3 音频

- 通过：奖励滑入使用唯一资源键 `card-place-1`，音频约 `0.689 s`。
- 通过：奖励与融合结果收纳使用唯一资源键 `handleSmallLeather`，音频约 `0.338 s`。
- 通过：三类声音共用 `UiPreparationCardAnimation` GroupKey，分别使用稳定 ConcurrencyKey；逐张音效 `MaxConcurrent = 3` 并有音量衰减，融合收纳 `MaxConcurrent = 1`。
- 通过：页面隐藏、关闭、复位或动画重新开始时调用 `AudioApi.StopGroup()`，未直接创建 AudioSource。

### 3.4 艺术字生成

- 通过：遵循 `imagegen` skill，使用内置 `image_gen` 模式生成，没有使用 CLI/API fallback。
- 通过：人工图像检查确认文字为准确且仅出现一次的“获得卡牌”；金色浮雕、红色内阴影、暗棕描边与少量红宝石符合当前红金中世纪卡牌 UI。
- 通过：源图为 `2079 × 756`、`Format32bppArgb`，左上角 Alpha 为 0；Unity 导入为 Single Sprite、Alpha Is Transparency、无 Mipmap、Clamp。
- 最终生成 prompt：

```text
Use case: stylized-concept
Asset type: transparent game UI title lettering sprite for a medieval fantasy card game
Primary request: Create a single polished Chinese art-title graphic containing exactly the four Chinese characters “获得卡牌”. The words mean “Cards Acquired” and will sit centered above a row of reward cards.
Subject: only the four characters “获得卡牌”, rendered once, left-to-right, fully legible and correctly written.
Style/medium: hand-painted fantasy game UI lettering, medieval card-game aesthetic, lightly oil-painted, ornate but restrained; beveled antique gold letter faces with warm ivory highlights, deep burgundy inner shadows, a subtle dark brown outer stroke, and tiny red-gem accents only if they do not hurt readability.
Composition/framing: wide horizontal title, four characters evenly spaced on one line, perfectly centered, balanced and nearly symmetrical, generous transparent padding, no plaque or solid backing panel.
Lighting/mood: warm heroic glow, premium reward reveal, crisp at UI scale.
Color palette: antique gold, warm ivory, deep red, dark brown; matches bright red-and-gold medieval fantasy card UI.
Text (verbatim): “获得卡牌”
Constraints: genuinely transparent background with clean alpha; exact text only once; no extra characters, Latin letters, numbers, icons, card drawings, frame, banner, panel, watermark, or background scene; keep edges clean and usable as a Unity UI sprite; prioritize correct Chinese glyph shapes and readability.
```

## 4. 验证结果

- Unity 编译：通过。
- Unity Console：最终清空后 Error 0 条。
- Prefab 结构检查：`RewardTitle=True`、Sprite=`PreparationRewardTitle`、尺寸 `(620,225)`、位置 `(0,270)`、PreserveAspect=True、Raycast=False；`OwnedOnlyToggle.isOn=True`；奖励层 sibling 10、融合层 sibling 11。
- EditMode 定向任务 `29540067722440cd99733d05e88d3ced`：3 项通过，覆盖共享 Prefab/资源、奖励发牌收纳音效、融合揭晓收纳。
- EditMode 定向任务 `00a1d9bfddaf46df82f2b30326ddbdcb`：1 项通过，覆盖默认筛选与卡池重建逻辑。
- C# 静态校验：`PreparationController`、`PreparationView`、`PreparationViewUiBuilder` 0 error/0 warning；`BattleCardItemController` 0 error，保留一条既有的 Update 字符串拼接通用告警；测试脚本已由 Unity 编译并运行通过。
- `git diff --check`：代码与三类文档无空白错误；Unity 生成的 Prefab YAML 对空字符串字段保留序列化尾随空格，未手工修改生成产物。
- Play Mode：按项目默认未进入。

## 5. 执行偏差与修正

- 艺术字导入后的 Unity 纹理宽度受默认最大尺寸影响，从源图 `2079` 调整为 `2048`。首次测试使用源图精确尺寸断言而失败；已改为质量下限断言并重新运行通过。Prefab 显示保持宽高比，视觉使用尺寸不受影响。
- Unity MCP 在刷新期间发生过断线重试，工具自动恢复后完成 Builder、结构检查、测试与最终 Console 验证。

## 6. 文档处理

- 玩家视角：更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`，记录奖励艺术字、摸牌/收纳、音效、融合结果收纳和默认只看拥有。
- 美术：更新 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md` 的模块风格、UI 分组、参考图片与已有资产列表。
- 程序：更新 `AutoDoc/Program/UI/preparation/preparation.md` 的 Controller、Prefab 层级、状态机、奖励批次、音频与默认筛选说明。

## 7. 未解决风险

- 未执行 Play Mode 人工观感验收。动画速度、艺术字相对卡牌的最终视觉比例，以及音效在整套游戏混音中的主观听感，仍建议由玩家在实际运行中确认。
- 当前无已知编译错误、测试失败、资源缺失或框架边界问题。

## 8. 清理结果

- 结束审计后仅运行一次 `AutoDoc/CleanupTempDocs.bat`。
- 退出码：`0`。
- 清理完成后创建本报告。
