# 战斗界面程序文档

## 1. 核心数据来源

### 1.1 Component

| Component | 战斗界面用途 |
| --- | --- |
| `BattleSessionSingletonRawComponent` | 提供双方卡牌 Entity、当前行动方、战斗结果、当前攻击者与当前目标 |
| `BattleCardRawComponent` | 提供卡牌编号、种类 ID、阵营、攻击力、当前生命和存活状态 |

### 1.2 Csv和ScriptableObject配置项

`BattleCardItemController` 根据 `BattleCardRawComponent.CardNumber` 通过 `DataApi` 读取 `BattleCardCsvData` 的种类关联与原画资源键，再按 `CardTypeId` 读取 `BattleCardTypeCsvData.DisplayName`，并通过 `ResourceApi.LoadSprite(ArtworkKey)` 加载对应怪物原画。左上角编号直接使用运行时 Component 中的 `CardNumber`。

当前界面未读取 ScriptableObject 配置。

## 2. UI界面

### 2.1 关联界面Controller列表

| Controller | View Prefab | 职责 |
| --- | --- | --- |
| `BattleController` | `Assets/Resources/Ui/BattleView.prefab` | 创建双方卡牌列表，显示无阵营文字标记的战斗状态和胜负结果 |
| `BattleCardItemController` | `Assets/Resources/Ui/BattleCardItem.prefab` | 绑定单张卡牌数据，刷新阵营卡框、名称、怪物原画、编号灰底框、攻血数值、高亮和死亡遮罩，并确保回池与换绑后的根节点保持正向、原画引用清空 |

`BattleCardItemController` 通过 Pre-load 映射 `Hearthstone.BattleCardItemController → Ui/BattleCardItem` 由 `UiList.AddItem<BattleCardItemController>()` 创建和回收。Entity 仅作为玩法数据句柄，不作为 UI View 或 Controller。

`BattleView.prefab` 的根尺寸为 `1920 × 1080`，`BoardBackground` 拉伸覆盖完整界面并使用 `BattleBoardBackground.png`。敌方与玩家列表分别位于 `y = 224` 与 `y = -224`，列表尺寸均为 `900 × 360`，通过 `UiList.AreaFit` 和 `278 × 360` 槽位水平排列三张卡牌。Prefab 不再包含 `TitleText`、`EnemyLabel` 或 `PlayerLabel`；中央 `TurnText` 只显示“战斗进行中”，结果文本只显示“胜利”或“失败”。

卡面尺寸为 `250 × 360`。`ArtworkViewport` 使用 `RectMask2D` 覆盖卡面约 `89%` 宽、`82.5%` 高的主体区域，实际为 `222.5 × 297`，`ArtworkArea` 在 Controller 绑定时保持 `2:3` 原画比例。`SkillArea` 扩大为卡面 `72%` 宽、`21%` 高的下部说明区，实际由 `160 × 45.72` 增至 `180 × 75.6`；子级说明文字区域为 `160 × 63.6`。

`CardFrameOverlay` 默认直连红金 `CardFrame-v3.png`，绑定时由 `BattleCardItemController` 根据阵营通过 `ResourceApi.LoadSprite` 选择敌方红金框或我方蓝金 `CardFrameBlue-v2.png`。`CardFrameOverlay`、`AttackerHighlight` 与 `TargetHighlight` 均在卡面根节点内拉伸并使用 `sizeDelta = (-40, -32)`，实际尺寸为 `210 × 328`；Controller 绑定阵营框时同步把当前阵营的窄框 Sprite 赋给两个高亮层，因此攻击者/目标状态保持相同的主体框范围，不会显示旧 `CardFrame-v2.png` 粗框。

左上角 `58 × 38` 的 `CardNumberBadge` 与其 TMP 子文本已经固化在 Prefab 静态层级并由 View 持有序列化引用，不再由 Controller 运行时创建。左下 `HealthBadge` 使用 `60 × 60` 的 `HealthDropBadge.png`，锚点为左下、中心位置 `(30, 30)`；右下 `AttackBadge` 使用 `60 × 60` 的无剑 `AttackBadgeFrame.png`，锚点为右下、中心位置 `(-30, 30)`。两个徽章分别比主体窄框向左右露出 `20`、向下露出 `16`；数值使用 `30` 号白色粗体 TMP，并通过深色 `Outline` 增强对比度。敌我双方 View 根节点保持单位旋转，不再使用方形阵营底色；池化换绑或关闭时恢复单位旋转、清空名称、隐藏编号并移除原画 Sprite。

该静态布局与三张相关 Sprite 的 Single/Alpha/Mipmap/WrapMode 导入约束由一一对应的 `BattleCardItemUiBuilder.Build()` 维护。

`BattleView.prefab` 与 `BattleCardItem.prefab` 中的 7 个 TMP 文本统一引用 `Assets/Resources/Fonts/NotoSansSC-Dynamic SDF.asset`。该字体资产使用 Dynamic population 与 Multi Atlas，预置当前战斗中文字符，并允许技能说明在运行时补充其他简体中文字形；源字体为同目录的 `NotoSansSC-VF.ttf`。

### 2.2 每个Controller监听的Component变量

| Controller | 监听来源 | 响应 |
| --- | --- | --- |
| `BattleController` | `BattleSessionSingletonRawComponent.CurrentSide` | 保持中央状态为“战斗进行中”；战斗结束后清空状态文字 |
| `BattleController` | `BattleSessionSingletonRawComponent.Result` | 刷新空状态、“胜利”或“失败”，并在结算后清空行动状态 |
| `BattleCardItemController` | `BattleCardRawComponent.CurrentHealth` | 刷新生命数字 |
| `BattleCardItemController` | `BattleCardRawComponent.IsAlive` | 控制死亡遮罩 |
| `BattleCardItemController` | `BattleSessionSingletonRawComponent.CurrentAttacker` | 控制攻击者高亮 |
| `BattleCardItemController` | `BattleSessionSingletonRawComponent.CurrentTarget` | 控制目标高亮 |

### 2.3 不同Controller之间的跳转关系

当前没有页面跳转。`BattleUiScene` 创建 `BattleController` 后，后者在两个 `UiList` 中创建卡牌条目 Controller；Stage 卸载时整页与条目按 UI 框架生命周期关闭并回池。

## 3. 所属GameStage

战斗界面属于 `BattleStage`，使用 `BattleUiScene`、`EBattleUiGroup.Main` 和 `Assets/Resources/Ui/Battle.asset`。导出资产中的 View Prefab 路径为 `Ui/BattleView`，默认显示。当前视觉调整只修改 View Prefab 内部静态结构与图片引用，没有改变 UI 编辑场景、UiGroup、DefaultShow、场景级 Position/Scale/Pivot 或导出路径，因此导出 Asset 保持不变。
