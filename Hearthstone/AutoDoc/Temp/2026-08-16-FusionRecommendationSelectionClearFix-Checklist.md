# 智能推荐选择时列表清理异常修复检查清单

- [x] 通过 — 用户问题：默认隐藏的推荐 `UiList` 现已进入页面 UI 生命周期，页面打开时初始化内部列表状态；选择组合触发同步清理时不再访问 null 的位移动画索引表。
- [x] 通过 — 根因核验：`TryApplyFusionRecommendation()` 同步写入 `FusionRevision`，监听器立即调用 `CloseFusionRecommendationPopup()`；异常发生在 `UiList.RemoveItem()` 第 200 行的 `m_InTranslationItemIndexes.Remove(index)`。重建前 Prefab 的 `BbxUiItems=6` 且推荐列表未注册，重建后为 7 且已注册。
- [x] 通过 — 修复范围：确认是 `UiViewBase.EditorPreInitialize()` 排除 inactive 子层导致的框架配置缺口；已将全部 UI Item 生命周期接口扫描改为 `includeInactive=true`，未给 `UiList.RemoveItem()` 添加掩盖问题的空判断。
- [x] 通过 — 状态正确性：玩法层推荐应用原子替换逻辑未改；既有 `ApplyingFusionRecommendationAtomicallyReplacesMaterialSlots` 与完整 `RunCardRulesTests` 通过，推荐弹窗仍由修订监听关闭并回收列表。
- [x] 通过 — 失败/无变化路径：`ApplyFusionRecommendation()` 的结果分支与统一关闭流程未改，失败刷新和 NoChange 关闭行为保持原样。
- [x] 通过 — UI 框架：推荐条目继续通过 `UiList.ItemWrapper.ClearItems()` 与 Controller 对象池回收，没有直接销毁或自行管理池。
- [x] 不适用 — UI Scene：只修正 Editor 预初始化扫描和页面 Prefab 的序列化生命周期清单，未改变 UiGroup、DefaultShow、整体 Transform 或导出路径。
- [x] 通过 — 代码质量：修复发生在生命周期注册根因，仅为 9 个既有 `GetComponentsInChildren` 调用补充 inactive 扫描参数；未新增状态字段或业务补丁。
- [x] 通过 — 框架边界审计：修复收敛在 BbxCommon View 的 Editor 配置源并由原 Builder 重建 Prefab；未绕过 `UiList`、Controller 生命周期或对象池。未修改 `UiList` 组件公开行为，因此 `bbxcommon-ui-item` 组件文档无需变化。
- [x] 通过 — 验证：Unity 编译通过；Prefab 实测推荐列表已注册且遮罩仍默认隐藏；针对性 EditMode 1/1、`RunCardRulesTests` 32/32 通过；清空后 Console 0 error/0 warning。未进入 Play Mode。
- [x] 不适用 — 玩家视角设计文档：已完整读取 `design-doc-format`；本次仅恢复既有“选择后填充并关闭”行为，没有新增或改变玩家可见规则，现有文档已准确。
- [x] 不适用 — 美术文档：已完整读取 `art-doc-writer`；未修改布局、Sprite、颜色、字体或其他 2D 图片美术现状。
- [x] 通过 — 程序文档：已完整读取 `program-doc-format` 与 UI 界面格式，在 `AutoDoc/Program/UI/preparation/preparation.md` 同步 inactive 推荐列表的预初始化注册与同步修订清理顺序。
- [x] 通过 — 无关改动审计：保留工作树中的用户和并行任务改动，仅修改框架预初始化、相关 Prefab 回归断言和备战程序文档。
- [x] 通过 — 结束审计：已重新读取清单并逐项核验根因、实现、Prefab、测试、Console 与文档证据。
- [x] 通过 — 清理与报告：结束审计后已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，并创建同名报告。
