# 战斗场景竖向卡牌改造 Plan

## 1. 需求明确

### 1.1 需求对齐

1. 继续使用现有 `BattleView` 战斗页面和双方各三张卡牌的布局，不改变自动战斗、攻击目标、伤害结算和胜负规则。
2. 卡面展示实例必须使用现有 `BattleCardItemController`，由 `UiList.AddItem<BattleCardItemController>()` 创建和回收；Entity 只承载 ECS 玩法状态，不作为 UI GameObject、View 或 UI 生命周期对象。
3. 每张卡牌调整为 `220 × 320` 的竖向长方形，玩家卡牌局部 Z 轴旋转为 `0°`。
4. 敌方卡牌由 `BattleCardItemController` 将整张卡牌根节点局部 Z 轴旋转为 `180°`；原画区域、技能区域、技能文字、血量、攻击力、高亮和死亡遮罩全部随根节点一起倒置，不做文字反向补偿。
5. 卡牌本地坐标中的上半部分为原画预留区，下半部分为技能说明区；本次不提供原画，原画控件保留空引用/空 Sprite 状态。
6. 卡牌本地坐标左下角显示当前血量，右下角显示攻击力。敌方卡牌旋转后，两项属性随整张卡牌转到相反朝向。
7. 无技能时不显示任何占位文字；技能说明区域仍保留，`SkillDescription` 为空时关闭对应 TMP 文本对象。
8. 新增卡牌 CSV 配置，预留技能说明并把默认攻击力、最大生命从常量迁入同一张卡牌配置表；当前只配置一条默认卡牌数据，六张战斗卡牌继续共用该配置。
9. 保留现有攻击者高亮、目标高亮、死亡遮罩和敌我配色；删除卡面上的“我方/敌方 + 槽位编号”文字，避免占用技能说明区域。
10. 视觉信息层级参考[炉石传说官方卡牌库](https://hearthstone.blizzard.com/en-us/cards)的主视觉、说明区和底部属性布局，但不复制其卡框、图标、字体或原画素材。
11. 本次只输出方案；后续实现必须通过项目指定的 `unityMCP` 修改 Prefab。默认不进入 Play Mode。

## 2. 数据部分

### 2.1 涉及到的数据概览

| 数据 | 类型 | 权威来源 | 产生方 | 消费方 | 生命周期 |
| --- | --- | --- | --- | --- | --- |
| 卡牌配置 ID | 运行时标识 | `BattleCardRawComponent.ConfigId` | `InitializeBattleRuntime` | `BattleCardItemController` | 随卡牌 Entity 创建与回收 |
| 攻击力 | 静态初值、运行时只读属性 | `BattleCardCsvData.Attack` 初始化到 `BattleCardRawComponent.Attack` | CSV / 初始化 LoadItem | `BattleSystem`、`BattleCardItemController` | 配置全局；运行时随卡牌 Entity |
| 最大生命 | 静态初值 | `BattleCardCsvData.MaxHealth` 初始化到 `BattleCardRawComponent.MaxHealth` | CSV / 初始化 LoadItem | `BattleCardRawComponent` | 配置全局；运行时随卡牌 Entity |
| 当前血量 | 运行时状态 | 现有 `BattleCardRawComponent.CurrentHealth` | 初始化 LoadItem、`BattleSystem` | `BattleSystem`、`BattleCardItemController` | 随卡牌 Entity |
| 技能说明 | 静态展示配置 | `BattleCardCsvData.SkillDescription` | CSV | `BattleCardItemController` | `GameEngineDefault` 数据组 |
| 卡牌朝向 | UI 派生表现 | 由 `BattleCardRawComponent.Side` 派生，不新增状态 | `BattleCardItemController` | `BattleCardItemView` 根节点 | 随 UiController 绑定周期 |

技能说明不复制到 ECS Component；UiController 通过 `ConfigId` 从 `DataApi` 获取静态配置。攻击力、最大生命和当前血量继续由玩法 Component 提供，UiController 不保存第二份权威数值。

### 2.2 新增数据列表

#### 2.2.1 新增 CsvData 类

| 类名与路径 | 重要字段 | 加载与登记 |
| --- | --- | --- |
| `BattleCardCsvData`；`Assets/Scripts/Hearthstone/Config/Csv/BattleCardCsvData.cs` | `int Id`、`int Attack`、`int MaxHealth`、`string SkillDescription` | 继承 `CsvDataBase<BattleCardCsvData>`；表名 `BattleCardCsvData`；使用 `EDataLoad.Override`；沿用 `GameEngineDefault` 数据组；`ReadLine()` 后执行 `DataApi.SetData(Id, this)` |

新增 `Assets/Resources/Config/BattleCardCsvData.csv`，表头为 `Id,Attack,MaxHealth,SkillDescription`。首条默认数据使用 `1,3,5,`，即技能说明为空；表头后按项目规范补充等列英文说明行与 `// Associated: None`。文件名作为资源 key，完成导入后通过项目的 Resources Dictionary 构建流程登记 `BattleCardCsvData`。

选择 `GameEngineDefault` 数据组是为了保证配置在 `BattleStage.InitializeBattleRuntime` 执行前已经加载；不在 `BattleStage` 新增同名 DataGroup，避免当前 Stage 的“LoadItem 先于 Stage Data”顺序导致初始化时查不到配置。

### 2.3 原有数据类新增/删除字段

#### 2.3.1 原有 Component 类新增/删除字段

| 类名 | 变更 | 初始化与回收 |
| --- | --- | --- |
| `BattleCardRawComponent` | 新增 `int ConfigId`；`Initialize` 增加 `BattleCardCsvData` 参数，并用配置写入 `ConfigId`、`Attack`、`MaxHealth` 和初始 `CurrentHealth` | `OnComponentCollect()` 将 `ConfigId` 重置为 `0`，继续先 Invalid 监听字段再清空数值，避免对象池复用泄漏 |

## 3. 游戏逻辑部分

### 3.1 涉及到的游戏逻辑概览

战斗计算方式不变。`BattleRules.DefaultAttack` 与 `BattleRules.DefaultHealth` 删除，新增唯一的默认卡牌配置 ID 常量 `DefaultCardConfigId = 1`。初始化阶段从 `DataApi` 读取该配置；缺失配置时抛出包含配置 ID 的明确异常，不使用隐藏回退常量，以保证 CSV 是唯一静态来源。

不新增 System、StageListener、Task 或 Utils；`BattleSystem`、攻击顺序、伤害结算和胜负测试均保持不变。

## 4. UI部分

### 4.1 涉及到的UI部分概览

本次修改现有普通页面及其动态条目，不新增 Hud、UiScene、UiGroup 或通用 `BbxUiItem`。静态层级全部保存在 Prefab 中；Controller 只负责绑定、监听、配置查询、旋转和显隐刷新，不在运行时创建卡面子节点。

`BattleCardItem.prefab` 的确定层级如下：

```text
BattleCardItem（220 × 320，BattleCardItemView）
├─ CardBackground（全尺寸）
├─ ArtworkArea（上半区，空 Sprite）
├─ SkillArea（下半区）
│  └─ SkillDescriptionText（四周 16 px，底部为属性角标预留 52 px）
├─ HealthBadge（本地左下角，48 × 48）
│  └─ HealthText
├─ AttackBadge（本地右下角，48 × 48）
│  └─ AttackText
├─ AttackerHighlight（全尺寸）
├─ TargetHighlight（全尺寸）
└─ DeadOverlay（全尺寸）
```

`ArtworkArea` 与 `SkillArea` 各占卡高的 50%。技能文本使用现有 TMP 字体、居中对齐、字号上限 `24`、下限 `16`、最多四行并使用省略溢出；空字符串时隐藏文本对象。所有纯展示 `Image` 与 TMP 控件关闭 Raycast Target。

`BattleView.prefab` 保持敌方列表在上、我方列表在下；两组 `UiList` 的卡牌尺寸统一为 `220 × 320`，横向间距为 `24`，整体按 `1920 × 1080` 参考分辨率居中，保留中间回合/结果信息区域。旋转作用于每个敌方卡牌条目根节点，不旋转整个 Enemy List，避免改变列表布局方向。

现有 `Assets/Scenes/Ui/Battle.unity`、`EBattleUiGroup.Main`、`Assets/Resources/Ui/Battle.asset` 和 `BattleStage` 的 UiScene 注册路径均不变。因为 View Prefab 路径、Group、DefaultShow、场景位置、缩放和 Pivot 不变，本次不修改 UI 编辑场景，也不重新导出 `Battle.asset`；实现验收时仍需核对 Prefab 连接和现有导出 Asset 可加载。

### 4.2 原有Ui/Hud改动

| View 类名 | 对应页面/条目 | 新增或删除控件 |
| --- | --- | --- |
| `BattleCardItemView` | `BattleView` 的动态卡牌条目 | 新增 `ArtworkArea` Image 与 `SkillDescriptionText` TMP 引用；保留背景、攻击/目标高亮、死亡遮罩、攻击与血量文本；删除 `SlotText` 引用；Prefab 根节点改为 `220 × 320` 并落地上述静态层级 |
| `BattleView` | `BattleView` | 不新增序列化字段；修改 Enemy/Player 两个 `UiList` 的 RectTransform、条目尺寸和间距，使两排竖卡在参考分辨率内完整显示 |

| Controller 类名 | 数据监听改动 |
| --- | --- |
| `BattleCardItemController` | 继续监听 `CurrentHealth`、`IsAlive`、`CurrentAttacker`、`CurrentTarget`；绑定时按 `ConfigId` 通过 `DataApi` 查询 `BattleCardCsvData` 并刷新技能说明；按 `Side` 将条目根节点旋转为 `0°/180°`；移除槽位文字刷新；`Unbind()` 或重新绑定时恢复根节点旋转和技能文本显隐，避免池化实例保留上一张卡牌的状态 |
| `BattleController` | 数据监听不变；继续通过两个 `UiList` 创建、复用和绑定 `BattleCardItemController`，不创建卡牌 GameObject 或把 Controller 存入 ECS |

## 5. 美术部分

### 5.1 涉及到的美术表现概览

项目当前没有战斗卡框、原画、属性角标或技能底板图片文件。本次使用 Unity UI 基础 `Image` 的矩形、描边/纯色与现有敌我配色完成结构占位，不新增或复制炉石素材；上半区明确保持无原画状态。后续若加入正式美术，只替换 View Prefab 的 Sprite 引用，不改变 Controller、CSV 或 ECS 数据边界。

### 5.2 美术资产完整性检查

| 资产或资产组 | 用途 | 候选已有资产及路径 | 复用结论 | 判断依据 | 缺失或不满足内容 | 处理方式 |
| --- | --- | --- | --- | --- | --- | --- |
| 卡牌背景与外框 | 建立竖向卡面轮廓 | `Assets/Resources/Ui/BattleCardItem.prefab` 中现有 `CardBackground` | 复用控件，不复用图片 | 项目内除框架/第三方文件外没有业务图片素材 | 无正式卡框 Sprite | 使用基础 Image 纯色与描边完成本次结构，不新增图片文件 |
| 原画区域 | 卡牌上半部分 | 无 | 不需要资产 | 用户明确要求原画留空 | 正式原画缺失但本次不构成阻塞 | 保留空 Sprite 的 `ArtworkArea` 引用 |
| 技能说明底板 | 承载下半部分文字 | 无独立图片 | 不需要资产 | 只需清晰分隔信息区域 | 无纹理底板 | 使用半透明基础 Image；空技能只隐藏文字，不隐藏区域 |
| 血量/攻击力角标 | 左下血量、右下攻击 | 无独立图片 | 不需要资产 | 数值可由基础几何底座承载 | 无正式图标 | 使用 48 × 48 基础 Image 与现有 TMP 字体 |
| 攻击者、目标与死亡状态 | 保留现有战斗反馈 | `Assets/Resources/Ui/BattleCardItem.prefab` 中现有高亮/遮罩控件 | 保留并适配新尺寸 | View 与 Controller 已存在对应引用和刷新逻辑 | 实际拉伸效果需在 Editor 核验 | 调整 RectTransform 为全卡拉伸，不新增素材 |

## 6. GameStage部分

### 6.1 修改LoadItem和LateLoadItem项

| LoadItem 项名 | 负责内容 | 所属GameStage |
| --- | --- | --- |
| `BattleStages.InitializeBattleRuntime` | 在创建双方卡牌前通过 `DataApi.GetData<BattleCardCsvData>(BattleRules.DefaultCardConfigId)` 取得默认配置，并把同一配置传给六个卡牌 Component；配置缺失时中止初始化并给出明确错误 | 已有 `BattleStage` |

`BattleStage` 的 System、UiScene 和激活组合不变，不新增 Stage 注册项。

## 7. 实现顺序建议

### 7.1 实现顺序

1. **调整卡牌 Component 数据契约**：修改 `BattleCardRawComponent` 的配置 ID、初始化参数与回收重置，并把默认攻血常量迁出 `BattleRules`。
2. **新增卡牌 CSV 配置**：新增 `BattleCardCsvData` 类和 `BattleCardCsvData.csv`，使用 `GameEngineDefault`、`Override` 与 `DataApi` 的 int key 登记；通过 Unity Editor 重建 Resources Dictionary 并确认资源 key 唯一。
3. **修改卡牌 View 与 Controller**：调整 `BattleCardItemView` 引用；在 `BattleCardItemController` 中实现技能配置查询、空文本隐藏、敌我根节点旋转、旧槽位文案删除和回池状态复位。
4. **修改战斗卡牌 Prefab**：在 Unity Editor 中修改 `BattleCardItem.prefab` 为 `220 × 320`，建立原画区、技能区、左下血量、右下攻击及全尺寸状态层；保持根节点与 `BattleCardItemView` 的 Prefab/Controller 映射。
5. **修改战斗页面 Prefab**：在 Unity Editor 中调整 `BattleView.prefab` 两个 `UiList` 的卡牌尺寸、间距和上下布局；保持动态条目预加载映射，不修改 `Battle.unity` 或手写 `Battle.asset`。
6. **接入 BattleStage 初始化**：修改 `InitializeBattleRuntime` 从 `DataApi` 读取默认卡牌配置并初始化 Entity；不改变 `BattleSystem` 注册与计算。
7. **补充自动化验证**：增加 CSV 解析/按 ID 查询、空技能字符串、配置驱动攻血初始化与 Component 回收复位的 EditMode 测试，并回归现有 `BattleRulesTests`。
8. **执行 Editor 验收**：使用项目指定的 `unityMCP` 检查两个 Prefab、预加载映射、`Resources.Load<UiSceneAsset>("Ui/Battle")` 和资源字典；等待编译后执行活动场景与 Console error 只读检查。默认不进入 Play Mode，并把实际画面验收保留为用户另行授权项。

### 7.2 Todo

- [ ] **调整卡牌 Component 数据契约**
- [ ] **新增卡牌 CSV 配置**
- [ ] **修改卡牌 View 与 Controller**
- [ ] **修改战斗卡牌 Prefab**
- [ ] **修改战斗页面 Prefab**
- [ ] **接入 BattleStage 初始化**
- [ ] **补充自动化验证**
- [ ] **执行 Editor 验收**
