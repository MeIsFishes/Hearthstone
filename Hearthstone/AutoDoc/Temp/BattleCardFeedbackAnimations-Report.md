# 卡牌反馈动效任务报告

## 1. 结果

任务完成。战斗卡牌现已支持受伤伤害浮字、攻击/生命上升双文本过渡、冲锋号角与远射弓箭浮动反馈；所有新增反馈统一使用 `BattleRules.AttackPresentationPlaybackSpeed = 0.8` 推进。用户否决的第一版弓箭未保留，最终资源为重新生成的简化符号版。

## 2. 实现证据

- `BattleCardItemUiBuilder` 创建并维护 `DamagePopup`、`ChargeFeedbackIcon`、`LongShotFeedbackIcon`、`AttackValueOutgoingText` 与 `HealthValueOutgoingText`，通过 Unity 编辑器执行 Builder 后保存到 `Assets/Resources/Ui/BattleCardItem.prefab`。
- `BattleCardItemView` 只保存新增 UI 引用。
- `BattleCardItemController`：
  - 比较 `CurrentHealth` 监听前后值，只在生命下降时显示 `-实际差值`；
  - 攻击或生命上升时共用双文本滑动渐隐/渐显逻辑；
  - 只在当前卡牌为攻击者且攻击表现序列首次出现时，根据 `Charge`、`LongShot` 分别触发左右图标；
  - 每帧用 `deltaTime × BattleRules.AttackPresentationPlaybackSpeed` 更新新增反馈；
  - 换绑、隐藏和回池时重置文字、位置、Alpha、计时器和显隐。
- 三张运行素材：
  - `Assets/Resources/Art/BattleCards/UI/DamageNumberBurst.png`：`1448 × 1086`，四角 Alpha 均为 `0`，SHA-256 前缀 `A02BE573F5B3520F`；
  - `Assets/Resources/Art/BattleCards/UI/ChargeHornIcon.png`：`1254 × 1254`，四角 Alpha 均为 `0`，SHA-256 前缀 `0A55ABE47E2C0B00`；
  - `Assets/Resources/Art/BattleCards/UI/LongShotBowIcon.png`：`1254 × 1254`，四角 Alpha 均为 `0`，SHA-256 前缀 `8E91E064753166EF`。

## 3. 检查清单状态

`BattleCardFeedbackAnimations-Checklist.md` 的 24 项检查全部通过：四项玩家反馈、三张透明素材、权威触发源、Builder/View/Controller 边界、对象池清理、共享 `0.8` 速度、自动化测试、文档同步和结束清理均有对应产物或验证证据。

## 4. 验证结果

- Unity EditMode 定向结构测试：`2/2` 通过。
  - `BattleCardPrefabConfiguresDamageStatAndKeywordFeedbackLayers`
  - `BattleCardPrefabKeepsTauntShieldBehindCardAndInsideSlotBounds`
- Unity EditMode 关键词回归：`BattleKeywordRulesTests` 共 `9/9` 通过。
- `dotnet build Hearthstone.csproj --no-restore`：退出码 `0`，`0` 错误，`8` 个既有程序集版本冲突警告。
- `dotnet build Hearthstone.Ui.Editor.csproj --no-restore`：退出码 `0`，`0` 错误，`8` 个同类既有警告。
- `dotnet build Hearthstone.Tests.csproj --no-restore`：退出码 `0`，`0` 错误，`8` 个同类既有警告。
- 非 Prefab 源码与文档执行 `git diff --check`：通过。Prefab 中的行尾空格由 Unity YAML 序列化器生成，未手写修剪。
- 按项目默认约定未进入 Play Mode 或实际开局验证。

## 5. 文档同步

- 玩家视角：`AutoDoc/Design/Specific/combat-system/combat-system.md`
- 程序战斗链路：`AutoDoc/Program/Specific/combat-system/combat-system.md`
- 程序战斗 UI：`AutoDoc/Program/UI/battle/battle.md`
- 战斗卡美术模块：`AutoDoc/Art/Modules/battle-card/battle-card.md`

## 6. 最终素材提示词

### 6.1 伤害数字爆炸底板

```text
Use case: stylized-concept
Asset type: isolated 2D fantasy card-game UI damage-number backing
Primary request: create one bright yellow comic-style explosion burst plate that will sit behind a dynamic damage number near a card's health badge
Scene/backdrop: true transparent PNG background, including all space outside the burst
Subject: a compact asymmetrical starburst / impact burst with 12-16 bold pointed rays, a generous clean center area for a large number overlay, no number baked into the art
Style/medium: polished mobile fantasy card-game UI, crisp hand-painted edges, readable at about 88 x 66 pixels, compatible with warm gold and parchment UI
Composition/framing: one centered isolated horizontal burst, entire silhouette visible, about 82% of canvas, safe transparent margin
Color palette: saturated lemon yellow center, warm golden-orange outer edge, a very thin dark amber rim for contrast, small restrained white highlight near center
Materials/textures: luminous magical impact paper/energy shape, clean rather than smoky
Constraints: TRUE ALPHA transparency; no text, no digits, no minus sign, no emblem, no button frame, no shadow outside silhouette, no scene, no checkerboard, no watermark
```

### 6.2 冲锋号角

```text
Use case: stylized-concept
Asset type: isolated 2D fantasy card-game keyword feedback icon
Primary request: create one instantly readable battle charge icon: a compact curved war horn / cavalry signal horn, no character holding it
Scene/backdrop: true transparent PNG background
Subject: side-view brass war horn angled gently up toward the right, flared bell, short wrapped dark-brown leather grip, one small red cloth ribbon tied near the grip to imply charge momentum
Style/medium: polished mobile fantasy card-game UI icon, bright hand-painted metal, bold clean silhouette, readable at about 64 x 64 pixels, compatible with red-gold card badges and silver fantasy frames
Composition/framing: one centered isolated horn, full silhouette visible, about 78% of square canvas, generous safe transparent margin; no circular button or backing plate
Color palette: warm brass gold, amber shadow, small deep-red ribbon, restrained white-gold edge highlight
Materials/textures: polished brass and leather, simplified details, no tiny engraving
Constraints: TRUE ALPHA transparency; no text, no letters, no sound-wave symbols, no shield, no character, no scene, no checkerboard, no watermark
```

### 6.3 远射弓箭（最终重做版）

```text
Use case: stylized-concept
Asset type: tiny isolated 2D fantasy card-game keyword status icon, designed for final display at 64 x 64 pixels
Primary request: create a SIMPLE, SYMBOL-LIKE long-shot icon showing a compact short bow and one small arrow; prioritize immediate readability over weapon detail
Scene/backdrop: true transparent PNG background
Subject: a stout crescent-shaped short bow viewed from the side, oriented mostly upright with a slight right tilt; one short arrow crosses the bow horizontally toward the upper right. The arrowhead must be small and simple, no larger than the arrow fletching. Thick bow limbs, thick taut string, clear negative spaces.
Style/medium: clean stylized mobile card-game UI icon, bold graphic silhouette with lightly hand-painted fantasy polish, not a full weapon illustration, not realistic concept art
Composition/framing: compact near-square silhouette centered on canvas, about 70% of canvas, wide transparent safety margin, readable when reduced to 64 px; bow and arrow should form one cohesive icon
Color palette: warm golden-brown bow with muted gold caps, pale silver arrow, one restrained cool-blue fletching accent; high contrast edges
Materials/textures: minimal texture, large simple color blocks, restrained highlights, no tiny wrapping or engraving
Constraints: TRUE ALPHA transparency; exactly one bow and one arrow; small arrowhead; no oversized spearhead, no quiver, no target, no circular backing plate, no shield, no character, no text, no scene, no checkerboard, no watermark
```

生成模式：内置 `imagegen`。三种不同素材分别生成；弓箭根据用户反馈从头重新生成并覆盖未采用版本。

## 7. 偏差与风险

- 未执行完整 `BattleRulesTests` 组。本工作区已有两项与本任务无关的已知失败：旧音效列表异常日志断言和已移除拖拽返回代码断言；本次使用新增定向测试、嘲讽回归和完整关键词测试隔离验证。
- 未进行实际战斗 Play Mode 目测，因此最终观感仍可在用户试玩后继续微调位置、尺寸和持续时间；结构、资源、触发逻辑和编译已验证。

## 8. 清理结果

任务结束前仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 `0`。运行时 `AutoDoc/Temp/` 共有 `195` 个 Markdown 文件，未超过 `500` 的清理阈值，因此没有删除文件。
