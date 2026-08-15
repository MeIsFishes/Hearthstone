# 融合与出战卡片拖拽修复报告

## 任务结果

任务已完成。融合素材卡在卡池原位显示浅灰色编号空槽，从融合槽拖回后会恢复卡面；融合槽复用同一浅灰空槽；出战卡继续保留在卡池，并在右上角以棕色文字显示“已出战”。

## 用户验收项

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 融合槽拖回后卡片不消失 | 通过 | 融合槽拖至卡池调用 `TryRemoveFusionMaterial`；刷新以 `runState.HasCard` 重新投影卡面，拖拽返回同时刷新三个列表与布局。 |
| 融合素材原位留下空槽 | 通过 | 融合页命中 `FusionSlotCardNumbers` 后隐藏卡面、保留显示编号并启用 `PreparationEmptyState`。 |
| 融合槽使用浅灰空槽 | 通过 | Builder 将 `PreparationFusionSlotEmptyState` 指向 `PreparationPoolEmptySlot.png`；Prefab 中对应 GUID 出现两次。 |
| 出战卡不消失且不留空槽 | 通过 | 卡池占用状态仍由 `HasCard` 决定，出战状态只读取 `BattleSlotCardNumbers` 并叠加标记。 |
| 右上角显示棕色“已出战” | 通过 | Prefab 已序列化 `PreparationDeployedState`、文本“已出战”和颜色 `#8B5226`。 |

## 实现与框架审计

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 定位 View、Prefab、Controller、拖拽和状态源 | 通过 | 已核对 `BattleCardItem`、`PreparationController`、`RunCardRules` 与会话/运行状态刷新链路。 |
| 遵循 View/Controller 与生命周期边界 | 通过 | View 只新增引用；交互和表现位于 Controller；Prefab 由 Builder 经 `PreInitializeView` 生成。 |
| 拖入、拖出、取消、重复与刷新路径 | 通过 | 状态以融合槽/战斗槽数组为唯一来源，刷新负责空槽、卡面和标识投影。 |
| 抽象与依赖范围 | 通过 | 仅为共享空槽配置增加 `inset` 参数；未增加无复用价值的业务抽象。 |
| 无关改动与 `.meta` | 通过 | 保留任务开始前的脏工作区和无关改动；本任务未创建、编辑或删除项目 `.meta`。 |
| 框架池、公开 API 与导出流程 | 通过 | 沿用 `UiControllerBase`、`UiViewBase`、`UiDragable`、`UiInteractor` 和 ItemWrapper，没有直接管理框架池或改变公开契约。 |
| 框架缺口处理 | 通过 | `UiViewBase.EditorPreInit` 局部清除销毁组件留下的空缓存；重复 Builder 后为 3 个有效引用、0 个空引用。 |

## 验证结果

- 同版本 Unity `2022.3.62f3c1` 批处理编译及 EditMode 测试：总计 68，通过 68，失败 0，跳过 0。
- `BattleCardItemUiBuilder.Build` 重复执行成功，退出码 0；重复构建的 View 缓存稳定为 3 个有效引用、0 个空引用。
- Prefab 静态核对：存在“已出战”文本与棕色序列化颜色；卡池浅灰空槽 Sprite GUID 引用数为 2。
- 代码与三份现状文档执行 `git diff --check` 无错误；Prefab 的空字段尾随空格是 Unity 标准 YAML 序列化格式，未手工改写。
- 按项目默认未进入 Play Mode。剩余风险是最终视觉位置、字号和实际拖拽手感尚未在游戏画面中人工确认。

主项目的 Unity 工具连接返回 `Transport closed`，没有关闭或重启用户正在运行的编辑器。为保持 Prefab 由官方 Builder 生成，改用系统临时目录中的同版本 Unity 项目副本执行构建和测试；副本随后已删除，主项目未受该清理影响。

## 文档同步

| 文档项 | 状态 | 位置/结论 |
| --- | --- | --- |
| 格式规范 | 通过 | 已完整读取玩家设计、美术、程序基础格式及适用参考。 |
| 玩家视角设计文档 | 通过 | `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md` 已同步空槽、恢复和出战标识。 |
| 美术文档 | 通过 | `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md` 已同步素材复用、颜色和旧素材未引用状态。 |
| 程序文档 | 通过 | `AutoDoc/Program/UI/preparation/preparation.md` 已同步 View/Controller/Prefab/Builder 和刷新链路。 |
| UIItem 文档 | 不适用 | 未新增或修改 `BbxUiItem` 自定义组件及其公开用法。 |

## 清理结果

- 结束阶段已重新读取并逐项审计检查清单，可修正缺口均已修正。
- 外部临时 Unity 项目副本已删除。
- `AutoDoc/CleanupTempDocs.bat` 已且仅已运行一次，退出码 0；清理后共有 165 个 Markdown，低于 500 个的裁剪阈值，因此没有触发历史文档删除。
- 本报告在清理命令之后创建。

完整逐项证据同时保留在 `AutoDoc/Temp/2026-08-15-FusionDragFix-Checklist.md`。
