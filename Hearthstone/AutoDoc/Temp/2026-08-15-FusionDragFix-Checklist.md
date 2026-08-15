# 融合与出战卡片拖拽修复检查清单

## 1. 用户验收项

- [通过] 融合槽拖回卡池时调用 `TryRemoveFusionMaterial`，刷新后卡池按持有数据重新显示卡片；返回拖拽还会刷新三个列表并恢复布局位置。
- [通过] 融合页中已选素材的卡池条目隐藏卡面并启用 `PreparationEmptyState`，保留原编号与浅灰色占位。
- [通过] `PreparationFusionSlotEmptyState` 改用 `PreparationPoolEmptySlot.png`，Prefab 中该 Sprite GUID 共出现两次，分别对应卡池和融合空槽。
- [通过] 出战卡仍按 `runState.HasCard` 显示在卡池，不进入空槽分支；出战槽调整只改变战斗槽编号。
- [通过] 卡池卡命中 `BattleSlotCardNumbers` 时显示右上角 `PreparationDeployedState`；Prefab 已序列化棕色 `#8B5226` 的“已出战”。

## 2. 实现与回归检查

- [通过] 已核对 `BattleCardItem.prefab`、View、Controller、Builder、`PreparationController`、`RunCardRules` 与会话/运行状态刷新链路。
- [通过] View 仅新增序列化引用，表现与交互逻辑位于 Controller，Prefab 通过项目 Builder 和 `UiApi.EditorOperation.PreInitializeView` 生成。
- [通过] 已审计卡池、战斗槽、融合槽的拖入/拖出/取消返回/刷新分支；融合素材槽是唯一状态源，空占位和恢复均由刷新投影。
- [通过] 新字段只表达独立的已出战状态；空槽 Builder 函数新增 `inset` 参数以复用卡池/战斗/融合三类配置，没有新增一次性业务抽象。
- [通过] 仅修改本任务直接相关代码、Prefab 和现状文档；保留工作区原有改动，未创建、编辑或删除任何项目 `.meta` 文件。
- [通过] 同版本 Unity 批处理编译及 EditMode 测试通过：68/68；Builder 重复执行退出码 0，View 缓存为 3 个有效引用、0 个空引用。按项目默认未进入游戏，风险写入报告。

## 3. 框架边界审计

- [通过] 沿用 `UiControllerBase`、`UiViewBase`、`UiDragable`、`UiInteractor`、列表 ItemWrapper 与现有对象池/生命周期；没有更改公开契约。
- [通过] 业务层未直接管理框架池；Prefab 由项目内 `BattleCardItemUiBuilder.Build` 在同版本 Unity 中生成并回写。
- [通过] 发现重复 Builder 预初始化会残留空 View 缓存引用，已在 `UiViewBase.EditorPreInit` 增加局部兼容的空引用清理；重复构建验证为 3 个有效引用、0 个空引用。

## 4. 文档同步审计

- [通过] 已完整读取 `design-doc-format`、`art-doc-writer`、`program-doc-format` 及适用的美术模块/UI 页面格式参考。
- [通过] 已更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`，记录融合占位、拖回恢复和已出战标识的玩家可见现状。
- [通过] 已更新 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md`，记录浅灰空槽复用、棕色文字规范及旧素材当前未引用状态。
- [通过] 已更新 `AutoDoc/Program/UI/preparation/preparation.md`，记录 View/Controller/Prefab/Builder 与融合恢复刷新路径。
- [不适用] 未新增或修改 `BbxUiItem` 自定义组件及其公开用法；`UiViewBase` 仅增加 Editor 预初始化空缓存清理，因此无需修改 `AutoDoc/UIItem/`。

## 5. 结束审计

- [通过] 结束阶段已重新读取本清单，并按实际代码、Prefab、测试和文档证据逐项复核。
- [通过] 已修复重复 Builder 产生空 View 缓存引用的缺口，并完成重复构建与测试复核；外部临时 Unity 项目副本已删除。
- [通过] 已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 0；清理后临时区共有 165 个 Markdown，未达到 500 个的裁剪阈值。
- [通过] 清理后已创建同任务名 `2026-08-15-FusionDragFix-Report.md`，记录结果、证据、验证、偏差、风险、文档处理与清理结果。
