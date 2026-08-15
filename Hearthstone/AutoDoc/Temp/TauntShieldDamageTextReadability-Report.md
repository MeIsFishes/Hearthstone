# 嘲讽盾牌与伤害数字可读性任务报告

## 1. 任务结果

任务完成。嘲讽盾牌现在从卡面左右、上方和下方均能看到轮廓；伤害浮字改为红色并保留深色描边。所有静态配置均由 `BattleCardItemUiBuilder` 维护并通过 Unity Editor 重新生成 `BattleCardItem.prefab`。

## 2. 实现结果与证据

- 嘲讽盾牌：
  - `RectTransform.sizeDelta = (278, 384)`；
  - `anchoredPosition = (0, -4)`；
  - `preserveAspect = false`，完整使用 `278 px` 空卡槽宽度；
  - 仍是卡牌根节点的第一个子层，默认隐藏且不接收射线；
  - 根据素材 Alpha 包围盒与运行尺寸计算，实际金属轮廓约露出：左右各 `11.44 px`、上方 `4.29 px`、下方 `5.13 px`。
- 伤害数字：
  - TMP 颜色为 `#D22020FF`；
  - 深红描边距离为 `(2, -2)`；
  - 黄色爆炸底板、位置、浮动渐隐逻辑和 `0.8` 播放速度保持不变。
- Unity Prefab 实读结果：`Shield=(278,384) position=(0,-4) preserveAspect=False sibling=0; DamageColor=#D22020FF; Outline=(2,-2)`。

## 3. 修改文件

- `Assets/Scripts/Hearthstone/Ui/Editor/BattleCardItemUiBuilder.cs`
- `Assets/Scripts/Hearthstone/Tests/Editor/BattleRulesTests.cs`
- `Assets/Resources/Ui/BattleCardItem.prefab`（由 Builder 生成）
- `AutoDoc/Design/Specific/combat-system/combat-system.md`
- `AutoDoc/Art/Modules/battle-card/battle-card.md`
- `AutoDoc/Program/UI/battle/battle.md`

没有修改 View、Controller、图片资源或 `.meta` 文件。

## 4. 检查项结果

检查清单 15 项全部通过。用户两次盾牌可见性要求、红色伤害数字、Builder 配置源、Prefab 结构、测试、框架边界、抽象、修改范围与三类文档均已逐项复核并记录证据。

## 5. 验证结果

- Unity EditMode 定向测试：`2/2` 通过。
  - `BattleCardPrefabKeepsTauntShieldBehindCardWithVisibleOuterEdges`
  - `BattleCardPrefabConfiguresDamageStatAndKeywordFeedbackLayers`
- `dotnet build Hearthstone.Ui.Editor.csproj --no-restore`：退出码 `0`，`0` 错误，`8` 个既有程序集版本冲突警告。
- `dotnet build Hearthstone.Tests.csproj --no-restore`：退出码 `0`，`0` 错误，`8` 个同类既有警告。
- 最终 Unity 刷新后 Console：`0` 条错误。
- 非 Prefab 源码与文档执行 `git diff --check`：通过，仅有工作区换行提示。
- 按项目默认约定未进入 Play Mode 或实际开局目测。

## 6. 框架与抽象审计

- 静态尺寸、偏移、颜色和描边继续由一一对应的 `BattleCardItemUiBuilder` 维护。
- View 仍只持有引用，Controller 仍负责运行时触发、动画和对象池复用清理。
- 未新增平行 UI、资源加载、计时或对象池实现。
- 新增的盾牌位置常量与尺寸常量共同组成可复现配置，没有增加一次性 helper。

## 7. 文档处理

- 玩家视角战斗文档已同步盾牌四边可见和红色伤害数字。
- 战斗卡美术模块文档已同步运行尺寸、偏移、实际露出估算和颜色值。
- 战斗 UI 程序文档已同步 Builder 静态配置。
- 战斗系统数据与结算链路没有变化，因此 `AutoDoc/Program/Specific/combat-system/combat-system.md` 无需修改。

## 8. 执行偏差与风险

- 执行中备战模块的并发修改曾短暂导致 `FusionRecommendationText` 字段失配；未修改或回退该范围。并发修改完成后重新编译，Editor 与 Tests 工程均恢复为 `0` 错误。
- 为响应用户追加的“上下边界也露出”，盾牌透明矩形高度由 `360 px` 增至 `384 px`。素材透明留白把实际金属超出控制在约 `4~5 px`；横向仍严格保持 `278 px` 空卡槽宽度。
- 未进行实际战斗目测，最终视觉露出量仍可在试玩后继续微调。

## 9. 清理结果

任务结束前仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 `0`。运行后 `AutoDoc/Temp/` 共有 `198` 个 Markdown 文件，未超过 `500` 的清理阈值，因此没有删除文件。
