# 战斗卡牌间距与备战槽缩放修复检查清单

## 问题与实现

- [x] 通过——定位战斗卡未排开的根因：`1500 × 330` 列表使用 `ConstantSlot` 的 `230 × 331.2` 槽位，纵向容量计算为 0，布局循环没有处理任何条目。
- [x] 通过——敌我战斗列表改为 `1680 × 360`、`AreaFit/Horizontal`、`278 × 360` 槽位，卡面间保留约 `28 px` 可见间隙。
- [x] 通过——`PopulateCards()` 在动态条目全部创建和绑定后调用 `UiList.RefreshLayout()`。
- [x] 通过——`BattleViewUiBuilder` 已生成并同步 `Assets/Resources/Ui/BattleView.prefab`。
- [x] 不适用——本次未改变 UiScene Group、DefaultShow、场景级 Position/Scale/Pivot 或导出路径，不重复导出 UiSceneAsset。
- [x] 通过——确认标准卡面为 `250 × 360`，备战出战槽为 `205 × 295.2`；旧 `0.88` 缩放会得到 `220 × 316.8` 并溢出槽位。
- [x] 通过——出战槽与融合槽绑定改为读取实际 `ConstantSlotSize`，按宽高容纳比例的较小值等比缩小并限制不放大；当前结果分别为 `0.82` 与 `0.76`。
- [x] 通过——对象池条目每次换绑先由 `ResetBinding()` 恢复单位缩放，再应用当前卡池、槽位、战斗、推荐或揭晓上下文比例。

## 框架边界

- [x] 通过——保持唯一 `BattleView`/`BattleController` 页面，没有在 Controller 中运行时拼装静态卡位。
- [x] 通过——战斗排列使用 `UiList.AreaFit` 与公开 `RefreshLayout()`；备战槽仍由 `UiList` 创建和回收共享卡片条目。
- [x] 通过——仅新增私有槽位容纳缩放函数，用于宽高比例计算与异常尺寸回退，没有新增无复用价值的字段或业务抽象。
- [x] 通过——未手工创建、编辑或删除 `.meta`，未覆盖或回退无关工作区改动；状态中已有 `.meta` 均为既存改动。
- [x] 不适用——没有新增或修改 BbxCommon UI 组件，只调整业务 Controller 对既有组件的使用，因此不更新 `AutoDoc/UIItem/`。

## 验证与文档

- [x] 通过——Unity 结构校验：敌我列表在 3～6 张卡时坐标分别为 `[-278,0,278]`、`[-417,-139,139,417]`、`[-556,-278,0,278,556]`、`[-695,-417,-139,139,417,695]`。
- [x] 通过——Unity 尺寸校验：标准卡 `250 × 360`；出战槽缩放 `0.82` 后为 `205 × 295.2`；融合槽缩放 `0.76` 后为 `190 × 273.6`。
- [x] 通过——`Hearthstone.csproj` 与 `Hearthstone.Editor.csproj` 均构建成功，0 错误；仅保留既有程序集版本冲突警告。
- [x] 通过——Unity EditMode 测试 `84/84` 通过，Unity Console 清理后错误数为 0。
- [x] 通过——活动场景仍为 `Assets/Scenes/Main.unity`，`isDirty=false`；按项目约定未进入 Play Mode。
- [x] 通过——已按 `design-doc-format` 同步玩家视角战斗文档的固定间距与槽内等比缩小表现。
- [x] 通过——已按 `art-doc-writer` 同步 UI 美术总览与战斗卡牌模块文档的排列和槽内构图。
- [x] 通过——已按 `program-doc-format` 同步战斗与备战 UI 程序文档的布局参数、刷新与换绑缩放行为。
- [x] 通过——目标代码与文档运行 `git diff --check` 无空白错误；仅提示仓库行尾转换策略。
- [x] 通过——结束审计完成；下一步仅运行一次 `AutoDoc/CleanupTempDocs.bat`，随后创建 Report。
