# 备战槽位与战斗分隔表现调整检查清单

## 用户要求与实现

- [x] 通过：出战列表中心距保持 205 px，可见槽位/卡牌宽度缩至 180.4 px，六槽相邻空隙为 24.6 px。
- [x] 通过：共享卡牌按 250×360 基准等比缩放为 180.4×259.776，总缩放 0.7216；绑定仍经 `BindPreparationBattleSlot` 统一复位和居中。
- [x] 通过：`PreparationView.prefab` 不再含 `BattleSlotHeader`，其标题文字与左右装饰线均已移除。
- [x] 通过：`BattleView.prefab` 不再含 `TurnText`、`ResultText`；控制器也不再绑定或刷新中央状态文字。
- [x] 通过：新增透明刀刻分隔线资产，并以 1260×160、alpha 0.58、关闭 Raycast 的静态层展示于战场中央。
- [x] 通过：复用 `ParchmentAgingOverlay`，战斗叠加 alpha 从 0.14 提高至 0.24；卡牌列表和结果 UI 层级保持在其上方。
- [x] 通过：两个 UiBuilder 与实际 Prefab 已同步重建；UiGroup、DefaultShow、场景位置/缩放/轴心和导出路径均未变化，因此 UiSceneAsset 导出不适用。

## imagegen 资产流程

- [x] 通过：已用 `view_image` 检查 `BattleBoardBackground.png` 与 `ParchmentAgingOverlay.png`，确认底图构图可保留、透明做旧层可复用。
- [x] 通过：已用内置 imagegen 生成并目检刀刻分隔线，背景透明、无文字、无水印，构图为单条超宽横向刻痕。
- [x] 通过：现有做旧纹理满足需求，仅提高战斗叠加强度至 24%，无需重新生成背景或纹理。
- [x] 通过：最终资产位于 `Assets/Resources/Art/BattleCards/UI/BattleCenterDividerCarving.png`，Builder 与 Prefab 引用一致；`.meta` 由 Unity 导入自动生成。
- [x] 通过：最终 Report 将记录内置工具模式、最终路径和完整提示词。

## 框架边界

- [x] 通过：Preparation/Battle 仍各自只有一个 View/Controller；静态分隔线和背景层仅由 Builder 构建。
- [x] 通过：通过 `PreparationViewUiBuilder.Build()`、`BattleViewUiBuilder.Build()` 重建 Prefab，未手写 Prefab YAML。
- [x] 通过：卡牌列表继续使用既有 `UiList`、预加载映射及对象池，生命周期路径未改变。
- [x] 通过：仅新增必要的 `BattleSlotVisualFillRatio` 与 Divider 资源路径常量，没有新增运行时抽象。
- [x] 通过：未手工修改 `.meta`，未回退或覆盖无关工作区改动。

## 验证与文档

- [x] 通过：Unity 结构核验得到中心距 205、可见尺寸 180.4×259.776、空隙 24.6、总缩放 0.7216。
- [x] 通过：Prefab 结构核验得到 Header=False、Turn=False、Result=False、Divider=True、OverlayAlpha=0.24、DividerAlpha=0.58、Raycast=False。
- [x] 通过：Unity EditMode 85/85 通过；`Hearthstone.csproj` 与 `Hearthstone.Editor.csproj` 构建均为 0 error（保留 8 条既有程序集冲突 warning）；刷新编译后 Unity Console 为 0 条 error。
- [x] 通过：活动场景为 `Assets/Scenes/Main.unity`、`isDirty=false`；遵循项目约定未进入 Play Mode。
- [x] 通过：已读取玩家视角设计文档格式 skill，并同步 `AutoDoc/Design/Specific/combat-system/combat-system.md`。
- [x] 通过：已读取美术文档 skill 与引用，并同步 UI 总览、战斗卡牌模块、备战卡池模块文档。
- [x] 通过：已读取程序文档 skill 与引用，并同步备战、战斗界面程序文档。
- [x] 通过：已重新打开本清单并逐项审计；目标文件 `git diff --check` 通过，仅有换行符提示。
- [x] 通过（流程确认）：此前未运行清理脚本；本清单审计完成后将仅运行一次 `AutoDoc/CleanupTempDocs.bat`，再创建对应 Report，执行结果写入 Report。
