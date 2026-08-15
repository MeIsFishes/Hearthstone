# 备战与融合拖拽交互回归修复检查清单

## 用户问题与复现链路

- [通过] 玩家设计文档、`PreparationController` 与 `BattleCardItemController` 均确认：备战已持有卡应可拖入出战槽或融合槽，融合素材可在槽间移动并拖回卡池。
- [通过] 已核对卡池、出战槽、融合槽的 `UiDragable`、`UiInteractor`、根 `CardBackground` Graphic、事件监听器和备战/战斗绑定启停逻辑。
- [通过] `BluePanelPattern` 的 36 个 `Image` 全部 `raycastTarget=false`，专项结构输出与测试均为 0 个花纹射线目标，不是本次根因。
- [通过] 根因是独立 `HoverInput` 子层的 `UiEventListener` 优先截获 PointerDown/Drag；已删除该层，将悬停与拖拽合并到卡片根输入面，没有关闭装饰或添加绕行。

## UI 组件与框架边界

- [通过] 已完整核对 `BattleCardItemController`、`PreparationController`、`UiDragable`、`UiInteractor` 与 `UiEventListener`；合并后的根监听器同时服务 Pointer Enter/Exit 和 PointerDown/Drag/PointerUp。
- [不适用] 未修改任何 BbxCommon UI 组件；`Assets/Scripts/BbxCommon/Ui/Mvc/PreLoadUiData.cs` 的既有差异属于共享工作区，本任务未触碰，因此无需更新 `AutoDoc/UIItem`。
- [通过] `ApplyPreparationState` 仍只为备战已持有卡开启根监听器和拖拽；空槽仅保留投放响应；战斗绑定继续关闭悬停与拖拽。
- [通过] 修复由共享 `BattleCardItem` View/Controller、既有 BbxUiItem、UiBuilder 和对象池链路承载；没有平行拖拽系统或裸 EventSystem 业务逻辑。
- [通过] 保留共享工作区已有修改；`git diff --name-only` 未发现 `.meta` 变更。

## 验证与文档

- [通过] 新增 `BattleCardPreparationHoverAndDragShareOneUnblockedInputSurface`，并强化蓝底纹样测试；验证根 Graphic、共享监听器、旧层删除、花纹 0 射线目标及卡池遮罩链。
- [通过] 3 个相关脚本校验 0 error；5 项定向测试通过；卡牌、关键词与备战流程回归 34/34 通过；最终 Console 0 error / 0 warning；按项目默认边界未进入 Play Mode。
- [通过] 玩家视角设计文档：已完整读取 `design-doc-format` 并核对 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`；文档已正确描述可拖拽行为，本次是恢复既有体验，无需改动。
- [不适用] 美术文档：已完整读取 `art-doc-writer` 并核对 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md`；删除的是透明输入对象，画面不变，无需同步美术规格。
- [通过] 程序文档：已完整读取 `program-doc-format` 与 UI 界面格式，并更新 `AutoDoc/Program/UI/preparation/preparation.md`、`AutoDoc/Program/UI/battle/battle.md`，记录共享根输入面和同一监听器的现状。

## 收尾

- [通过] 结束前已逐项写入根因、Prefab、测试、框架、文档和验证证据。
- [待执行] 只运行一次 `AutoDoc/CleanupTempDocs.bat` 并记录退出结果。
- [待执行] 清理后创建 `AutoDoc/Temp/PreparationDragInteractionRegression-Report.md`。
