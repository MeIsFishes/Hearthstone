# 备战与融合拖拽交互回归修复报告

## 任务结果

备战卡池、出战槽和融合槽的卡牌拖拽输入已恢复。已持有卡在备战阶段可以重新接收 PointerDown、Drag 与 PointerUp；悬停黄色高亮继续有效。战斗阶段仍关闭悬停和拖拽，没有扩大交互范围。

## 根因

之前为了实现备战黄色悬停，`BattleCardItem` 增加了独立透明子对象 `HoverInput`。该对象带有自己的 `UiEventListener`，并位于卡片根节点的拖拽监听器之上。Unity Pointer 事件会先在命中的子对象找到对应 Handler，因此 PointerDown/Drag 被 `HoverInput` 截获，根节点 `UiDragable` 无法开始拖动。

新加的蓝色底板花纹不是根因：`BluePanelPattern` 的 36 个 `Image` 全部 `raycastTarget=false`，结构检查和回归测试均确认花纹射线目标数量为 0。

## 修复

- 删除 `BattleCardItem.prefab` 中多余的透明 `HoverInput` 子对象。
- `CardHoverInput` 直接引用卡片根节点的 `CardBackground`。
- `CardHoverListener` 与 `PreparationDragable.EventListener` 引用根节点上的同一个 `UiEventListener`。
- 同一根输入面现在同时处理 Pointer Enter/Exit 与 PointerDown/Drag/PointerUp，不再存在上层 Handler 抢占拖拽事件。
- `ApplyPreparationState` 的阶段开关保持不变：备战已持有卡开启监听器和拖拽，空槽只保留投放响应，战斗绑定关闭悬停与拖拽。
- 通过 Unity Editor 执行 `BattleCardItemUiBuilder.Build()` 重建共享 `Assets/Resources/Ui/BattleCardItem.prefab`；卡池、出战槽、融合槽和战斗继续复用同一 Prefab。

## 验证

- Prefab 结构输出：`hoverInputGO=BattleCardItem`、`dragListenerGO=BattleCardItem`、`sameImage=True`、`sameListener=True`、`obsoleteHover=False`、`patternRaycastTargets=0`。
- 新增 `BattleCardPreparationHoverAndDragShareOneUnblockedInputSurface`，验证共享根 Graphic、共享监听器和旧子层删除。
- 强化卡池蓝底纹样测试，验证所有装饰 Image 均不接收射线。
- 5 项定向输入/卡池测试全部通过。
- `RunCardRulesTests`、`BattleKeywordRulesTests`、`PreparationContinueTests` 合计 34/34 通过，0 failed，0 skipped。
- Builder 与测试脚本校验均为 0 error；最终 Unity Console 为 0 error / 0 warning。
- `Preparation.unity` 与 `Preparation.asset` 没有变更，无需重新导出 UiSceneAsset。
- 未创建、编辑或删除 `.meta`；共享工作区既有修改未被回退。

## 框架边界与 UI 组件

修复沿用共享 View/Controller、`UiDragable`、`UiInteractor`、`UiEventListener`、UiBuilder、Resources Prefab 和对象池。没有新增平行拖拽系统、裸 EventSystem 业务逻辑或运行时静态 UI 拼装。

本次只调整业务 Prefab 的组件引用，没有修改 BbxCommon UI 组件源码；共享工作区中 `PreLoadUiData.cs` 的既有差异并非本任务产生，因此无需更新 `AutoDoc/UIItem`。

## 文档处理

- 玩家视角设计文档已正确描述备战拖拽规则；本次恢复既有玩家体验，无需改写。
- 视觉没有变化，美术文档无需更新。
- 已更新 `AutoDoc/Program/UI/preparation/preparation.md` 与 `AutoDoc/Program/UI/battle/battle.md`，记录悬停和拖拽共用根输入面及同一监听器的当前实现。

## 执行偏差

首次定向测试中，旧测试仍断言悬停与拖拽必须使用不同监听器，因此按新的正确输入结构失败。该过时断言已同步修正，随后 5 项定向测试和 34 项回归测试全部通过。

## 清理结果

任务结束前仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 `0`。本报告在清理完成后创建；脚本按自身保留规则保留本任务检查清单。

## 未解决风险

按项目默认边界未进入 Play Mode。当前通过 Prefab 引用、事件 Handler 归属、射线目标、阶段开关和 Editor 测试验证输入链路；建议用户在现有运行进度中直接拖一张已持有卡到出战槽和融合槽，确认具体手感。
