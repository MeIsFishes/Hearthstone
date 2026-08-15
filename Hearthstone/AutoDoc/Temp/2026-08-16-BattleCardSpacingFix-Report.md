# 战斗卡牌间距与备战槽缩放修复报告

## 1. 任务结果

本次完成两项关联 UI 修复：

1. 战斗场景的敌我卡牌按固定中心距横向排开，并随 3～6 张实际卡牌数量整体居中。
2. 备战卡牌进入尺寸较小的出战槽或融合槽时，根据卡面与槽位实际尺寸等比缩小并居中；对象池换绑时先复位缩放，避免比例残留。

## 2. 根因与实现

### 2.1 战斗排列

旧战斗列表为 `1500 × 330`，使用 `UiList.ConstantSlot/Horizontal` 与 `230 × 331.2` 槽位。`ConstantSlot` 计算到的纵向容量为 0，布局循环没有处理条目，导致卡牌全部停留在初始位置重叠。

修复后敌我列表均为 `1680 × 360`，使用 `UiList.AreaFit/Horizontal` 和 `278 × 360` 槽位。`250 px` 宽卡面之间保留约 `28 px` 可见间隙；`BattleController.PopulateCards()` 在动态创建完成后显式调用 `RefreshLayout()`。

### 2.2 备战槽缩放

标准卡面为 `250 × 360`，出战槽为 `205 × 295.2`。旧出战槽固定缩放为 `0.88`，会生成 `220 × 316.8` 卡面并溢出槽位。

修复后，`PreparationController` 把实际 `ConstantSlotSize` 传给共享卡片 Controller；后者取“槽宽 ÷ 卡宽”和“槽高 ÷ 卡高”的较小值，并限制最大为 1。当前出战槽缩放为 `0.82`，融合槽缩放为 `0.76`。`ResetBinding()` 保持单位缩放复位，因此卡池、槽位、战斗、推荐与揭晓上下文之间不会互相残留比例。

## 3. 修改范围

- `Assets/Scripts/Hearthstone/Ui/Editor/BattleViewUiBuilder.cs`
- `Assets/Scripts/Hearthstone/Ui/Controller/BattleController.cs`
- `Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs`
- `Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs`
- `Assets/Scripts/Hearthstone/Tests/Editor/BattleRulesTests.cs`
- `Assets/Resources/Ui/BattleView.prefab`
- `AutoDoc/Design/Specific/combat-system/combat-system.md`
- `AutoDoc/Art/UI/ui-art-overview.md`
- `AutoDoc/Art/Modules/battle-card/battle-card.md`
- `AutoDoc/Program/UI/battle/battle.md`
- `AutoDoc/Program/UI/preparation/preparation.md`

没有修改 UiScene Group、DefaultShow、场景级 Position/Scale/Pivot 或导出路径，因此没有重复导出 UiSceneAsset。没有新增或修改 BbxCommon UI 组件，因此 `AutoDoc/UIItem/` 不适用。没有手工创建、编辑或删除 `.meta`。

## 4. 检查项与证据

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 战斗卡固定间距 | 通过 | 两排均使用 `AreaFit/Horizontal` 与 `278 × 360` 槽位 |
| 3～6 张整体居中 | 通过 | X 坐标分别为 `[-278,0,278]`、`[-417,-139,139,417]`、`[-556,-278,0,278,556]`、`[-695,-417,-139,139,417,695]` |
| 动态布局刷新 | 通过 | `PopulateCards()` 末尾调用 `list.RefreshLayout()` |
| 出战槽等比缩放 | 通过 | `250 × 360 × 0.82 = 205 × 295.2` |
| 融合槽等比缩放 | 通过 | `250 × 360 × 0.76 = 190 × 273.6` |
| 对象池换绑复位 | 通过 | `ResetBinding()` 恢复 `Vector3.one`，随后按绑定上下文设置比例 |
| Builder 与 Prefab 同步 | 通过 | 已运行 `Hearthstone.BattleViewUiBuilder.Build()` 并核验 Prefab |
| 框架边界 | 通过 | 使用既有 `UiList`、公开布局刷新、预加载映射和对象池生命周期 |
| 玩家视角设计文档 | 通过 | 已同步固定间距、整体居中与槽内等比缩小表现 |
| 美术文档 | 通过 | 已同步战斗排布、卡面间隙与备战槽内构图 |
| 程序文档 | 通过 | 已同步列表参数、刷新时机、尺寸计算与换绑复位 |

## 5. 验证结果

- `dotnet build Hearthstone.csproj --no-restore`：通过，0 错误；保留既有程序集版本冲突警告。
- `dotnet build Hearthstone.Editor.csproj --no-restore`：通过，0 错误；保留既有程序集版本冲突警告。
- Unity EditMode：`84/84` 通过，0 失败、0 跳过。
- Unity Console：清理测试预期日志后 0 Error。
- Unity 资产尺寸核验：卡面 `250 × 360`，出战槽 `205 × 295.2`、比例 `0.82`，融合槽 `190 × 273.6`、比例 `0.76`。
- 活动场景：`Assets/Scenes/Main.unity`，`isDirty=false`。
- `git diff --check`：目标代码与文档无空白错误，仅有仓库行尾转换提示。

按项目默认约定未进入 Play Mode；本次使用 EditMode、Unity Editor 结构/尺寸核验和程序集构建完成验证。

## 6. 偏差与风险

用户在战斗卡间距修复进行中追加了备战槽缩放要求，已并入同一检查清单、测试与文档审计。当前没有未解决功能风险。

## 7. 清理结果

结束审计后仅运行一次 `AutoDoc/CleanupTempDocs.bat`，命令成功退出；随后创建本报告。
