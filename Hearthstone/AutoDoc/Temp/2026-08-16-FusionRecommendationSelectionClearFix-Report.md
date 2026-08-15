# 智能推荐选择时列表清理异常修复报告

## 1. 任务结果

已修复点击智能推荐组合“选择”时，`FusionRevision` 同步通知关闭推荐弹窗并清理列表所触发的 `NullReferenceException`。

修复没有在 `UiList.RemoveItem()` 增加空判断，而是补齐默认隐藏 UI Item 的预初始化和运行时生命周期注册，使推荐列表在第一次添加/清理条目前已经完成 `IUiInit/IUiOpen`。

## 2. 根因

调用顺序为：

1. 推荐行“选择”调用 `ApplyFusionRecommendation()`。
2. `TryApplyFusionRecommendation()` 写入 `FusionRevision`，同步派发监听器。
3. `OnFusionRevision()` 立即调用 `CloseFusionRecommendationPopup()`。
4. `FusionRecommendationList.ClearItems()` 进入 `UiList.RemoveItem()`。
5. `m_InTranslationItemIndexes` 为 null，在第 200 行调用 `Remove(index)` 时抛出异常。

该列表位于默认 inactive 的 `FusionRecommendationOverlay` 下。`UiViewBase.EditorPreInitialize()` 原先只扫描激活层级，因此它没有被序列化进 `BbxUiItems`，运行时也没有执行 `UiList.OnUiOpen()`。修复前实测 `BbxUiItems=6`、推荐列表未注册；修复后为 7、推荐列表已注册，同时遮罩仍默认隐藏。

## 3. 实现范围

- `UiViewBase.EditorPreInitialize()`：全部 UI Item 接口扫描改为包含 inactive 子层。
- `PreparationView.prefab`：通过原 `PreparationViewUiBuilder.Build()` 重建，序列化生命周期清单现包含 `FusionRecommendationList`。
- `RunCardRulesTests`：增加默认隐藏推荐列表必须存在于 `BbxUiItems` 的回归断言。
- 备战 UI 程序文档：同步 inactive 列表注册与融合修订同步清理顺序。

## 4. 检查项与证据

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 异常根因 | 通过 | 堆栈行号对应 `m_InTranslationItemIndexes.Remove(index)`；Prefab 检查确认列表此前未注册 |
| 生命周期修复 | 通过 | 9 个 `GetComponentsInChildren<IUi*>` 扫描均使用 `includeInactive=true` |
| Prefab 配置 | 通过 | 推荐列表已进入 `BbxUiItems`，推荐遮罩仍为 inactive |
| 推荐应用规则 | 通过 | 玩法层与 Controller 结果分支未改，原子替换测试通过 |
| 对象池边界 | 通过 | 推荐行仍由 `UiList.ClearItems()` 和 Controller 池统一回收 |
| UI Scene | 不适用 | 未改变 UiScene 导出字段 |
| 设计/美术文档 | 不适用 | 玩家可见规则和 2D 图片表现未变化 |
| 程序文档 | 通过 | 已更新备战 UI 程序文档 |

## 5. 验证结果

- Unity 编译：通过。
- Builder 重建：通过。
- Prefab 实测：`BbxUiItems=7`、`RecommendationListRegistered=True`、`RecommendationOverlayActive=False`。
- 针对性 EditMode：1/1 通过。
- `Hearthstone.Tests.RunCardRulesTests`：32/32 通过。
- Unity Console：清空并重新编译后 0 error、0 warning。
- C#、测试、程序文档范围 `git diff --check`：通过；Unity 自动序列化 Prefab 仍包含项目既有的空值尾随空格格式。
- 按项目默认约束未进入 Play Mode。

## 6. 偏差与风险

- 未在 Play Mode 中复现点击链路；使用原异常堆栈、序列化生命周期检查和 EditMode 回归验证修复。
- `includeInactive=true` 是向后兼容的框架修复：后续重新执行其他 View 的 Pre-UiInit 时，其默认隐藏 UI Item 也会正确进入生命周期。这是预期行为，但本任务只重建了直接受影响的 `PreparationView.prefab`。
- 工作树包含其他任务的未提交修改，本任务未回滚或整理。

## 7. 清理结果

结束审计后已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0；检查清单和本报告已保留。
