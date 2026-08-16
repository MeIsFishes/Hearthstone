# 备战槽位与融合点数底板调整报告

## 任务结果

本次调整已完成。备战页“出战”和“融合”页签中的拖拽槽位都缩小显示，并直接复用共享卡 Prefab 已有的浅灰木纹空槽；两种列表均保留经过检查脚本验证的正间距。融合页“当前点数”和“剩余点数”恢复使用现有横向数值底板，底板实际覆盖左侧说明文本和右侧数字文本。

## 实现结果与证据

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 出战槽缩小 | 通过 | 中心距 `205 px`，可见宽度 `168.1 px`，相邻间距约 `36.9 px` |
| 融合槽缩小 | 通过 | 中心距 `190 px`，可见宽度 `155.8 px`，相邻间距 `34.2 px` |
| 复用空槽 UI | 通过 | `PreparationBattleSlotEmptyState` 与 `PreparationFusionSlotEmptyState` 均引用 `PreparationPoolEmptySlot.png`，并由测试断言为同一 Sprite |
| 间距机制 | 通过 | `UiList.ConstantSlot` 以 `ConstantSlotSize.x` 作为相邻中心距；可见槽位统一乘 `0.82`，所以两种列表的可见宽度都小于中心距 |
| 当前点数底板 | 通过 | 底板 `280 × 72`；标签边界 `-128~30 px`、数字边界 `46~124 px`，均位于底板 `-140~140 px` 内 |
| 剩余点数底板 | 通过 | 与当前点数底板使用相同尺寸、Sprite、文本布局与覆盖规则 |
| 底板资源与拉伸 | 通过 | 两块底板均引用 `PreparationFusionSumPanel.png`，`Image.preserveAspect=false`，图像覆盖完整横向矩形 |
| Builder 与 Prefab | 通过 | 通过 Unity 正式通道执行 `BattleCardItemUiBuilder.Build()`、`PreparationViewUiBuilder.Build()`，未手写 Prefab YAML |
| UiScene 导出 | 不适用 | 未改变 UiGroup、DefaultShow、场景级 Position/Scale/Pivot 或导出路径，`Preparation.asset` 无需重导 |
| 框架边界 | 通过 | 继续使用既有 View/Controller、共享卡 Controller、`UiList`、对象池、预加载映射与一一对应 Builder，无平行 UI 实现 |

## 主要文件

- `Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs`
- `Assets/Scripts/Hearthstone/Ui/Editor/BattleCardItemUiBuilder.cs`
- `Assets/Scripts/Hearthstone/Ui/Editor/PreparationViewUiBuilder.cs`
- `Assets/Scripts/Hearthstone/Tests/Editor/BattleRulesTests.cs`
- `Assets/Scripts/Hearthstone/Tests/Editor/RunCardRulesTests.cs`
- `Assets/Resources/Ui/BattleCardItem.prefab`
- `Assets/Resources/Ui/PreparationView.prefab`

本次没有新建图片资产，也没有创建、编辑或删除 `.meta` 文件。

## 验证结果

- 直接相关 Unity EditMode：`2/2` 通过。
- 完整 Unity EditMode：`90/90` 通过，0 failed，0 skipped；任务 ID `a131843fab924b70a8cd63b67812039d`。
- `dotnet build Hearthstone.csproj --no-restore`：0 error；保留 8 条既有程序集版本冲突 warning。
- `dotnet build Hearthstone.Editor.csproj --no-restore`：0 error；保留 8 条既有程序集版本冲突 warning。
- Unity Prefab 实测：两种空槽 Sprite 均为 `PreparationPoolEmptySlot`；点数底板 Sprite 均为 `PreparationFusionSumPanel`；两个底板均关闭 `preserveAspect`。
- Unity Console：完整测试产生的预期异常日志清理后为 0 error。
- Editor 状态：活动场景 `Assets/Scenes/Main.unity`，`isDirty=false`，`Application.isPlaying=false`。
- 目标代码与文档定向 `git diff --check` 通过；Builder 生成的 Unity YAML 保留 Unity 序列化的标准空值尾随空格。

首次并行执行两份 `dotnet build` 时，两进程争用同一个 `obj/Debug/Hearthstone.dll`，其中一次出现文件锁。改为顺序执行后两份构建均通过，因此该过程没有留下构建阻塞或代码风险。

## 文档处理

- 玩家视角：已更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`。
- 美术：已更新 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md`；UI 风格分组未变化，`ui-art-overview.md` 无需改动。
- 程序：已更新 `AutoDoc/Program/UI/preparation/preparation.md`。
- `AutoDoc/UIItem/` 不适用：未新增或修改 BbxCommon UI 组件能力，仅使用既有 `UiList` 布局规则。

## 偏差与风险

按项目默认约定未进入 Play Mode；验证覆盖 Builder 重建、Prefab 结构和几何边界、完整 EditMode、程序集构建与 Unity Console。当前没有已知未解决问题。

## 清理结果

结束审计后仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 `0`；随后创建本报告，未再次运行清理脚本。
