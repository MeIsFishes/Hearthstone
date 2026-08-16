# 备战槽位入槽卡片尺寸适配报告

## 任务结果

已修正出战与融合槽位“空槽很小、卡片放入后恢复大尺寸”的问题。两页不再单独缩小空态图片，而是让空槽与占用卡片共同使用 `PreparationSlotVisualFillRatio = 0.48f` 的紧凑条目根缩放；卡片入槽后与对应空槽高度一致，并保留明确的相邻间距。

## 检查项结果与证据

- 通过：出战槽占用卡面实测为 `88.8 × 127.872 px`，空槽为 `85.248 × 127.872 px`；高度一致，相邻可见间距分别为 `96.2 px` 与 `99.752 px`。
- 通过：融合槽占用卡面实测为 `91.2 × 131.328 px`，空槽为 `87.552 × 131.328 px`；高度一致，相邻可见间距分别为 `98.8 px` 与 `102.448 px`。
- 通过：两页专用空态继续共用 `PreparationPoolEmptySlot.png`，空槽在 `250 × 360` 共享卡根内完整拉伸并保持原图比例，实际源绘制区为 `240 × 360 px`。
- 通过：`BattleCardItemUiBuilder.Build()` 已执行并保存 `Assets/Resources/Ui/BattleCardItem.prefab`；检查时活动场景未脏，Unity 未进入 Play Mode。
- 通过：编辑器测试覆盖统一缩放、尺寸匹配、宽度比例差、间距与专用空态条件，目标测试 `1/1` 通过。
- 通过：框架边界保持在现有 `PreparationController`、共享 `BattleCardItemController`、`UiList`、对象池和一一对应 UiBuilder 内；没有新增平行 UI、手写 Prefab YAML 或运行时静态拼装。
- 通过：旧的两页独立比例常量合并为单一复用常量；删除仅用于空态内缩的一次性常量和函数参数。
- 通过：本任务没有创建、编辑或删除 `.meta`；相关文件 `git diff --check` 未发现新增空白错误。
- 未通过：完整 EditMode 回归 `111` 项中 `107` 项通过、`4` 项失败。失败为战斗场景六槽约束、存活攻击者序列、稀疏继续阵容和卡牌替换规则测试，不经过本次 UI 尺寸断言；未越权修改这些玩法逻辑。

## 验证结果

- `Hearthstone.Tests.BattleRulesTests.PreparationPoolAndSlotsUseSharedBattleCardAndMatchItsAspectRatio`：`1/1` 通过。
- `dotnet build Hearthstone.csproj --no-restore`：通过，`0` 错误。
- `dotnet build Hearthstone.Ui.Editor.csproj --no-restore`：通过，`0` 错误。
- `dotnet build Hearthstone.Tests.csproj --no-restore`：通过，`0` 错误。
- Unity Console 清理后读取：`0` 条错误。
- 几何检查：两页空槽与占用卡面高度相等，全部相邻间距大于零，两页专用空态 Sprite 相同。
- 未进入 Play Mode；按项目默认要求使用 Editor 结构、脚本、测试、编译与 Console 验证。

## 文档处理

- 玩家视角设计：更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`，记录卡片入槽后保持空槽对应紧凑尺寸。
- 美术：更新 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md` 与 `AutoDoc/Art/Modules/battle-card/battle-card.md`，统一实际尺寸、填充比例和间距规格。
- 程序：更新 `AutoDoc/Program/UI/preparation/preparation.md`，记录统一根缩放、空槽布局、尺寸计算与 Builder 配置源。

## 执行偏差与未解决风险

- 完整 EditMode 回归未全绿，保留上述 4 项非 UI 玩法规则失败作为当前工作区风险；本次目标测试、编译和 Console 均通过。
- 未执行游戏内拖放目视验证。当前结果由实际 Prefab 几何、运行时绑定代码和 EditMode 断言共同确认；最终体感仍可在后续正常游戏验收中观察。

## 清理结果

已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 `0`。清理后创建本报告。
