# 查看拥有筛选消失问题任务报告

## 任务结果

问题已修复。勾选“查看拥有”后，卡池只显示当前持有卡并按原总览编号升序排列；取消勾选后恢复 `01~213` 全部条目。筛选切换继续复位到列表顶部，传奇动态展示编号不会因过滤变化。

## 原因与修复

筛选重建会回收旧条目并创建新条目。原实现让 `UiList.AddItem()` 在重建过程中沿用切换前的 Content 高度计算坐标，直到全部条目创建完才按新条目数量调整 Content 高度，但改高后没有重新布局：

- 全表切换到少量拥有卡时，拥有卡保留了高内容区坐标，随后内容缩短，卡片落到 ScrollRect 裁剪区外。
- 从拥有筛选切回全表时，大量卡片又沿用较短内容区创建，随后内容拉长，同样没有获得最终坐标。

修复在 `SetSizeWithCurrentAnchors()` 确定最终 Content 高度后调用现有 `UiList.RefreshLayout()`，让全部现存共享卡片按最终 7 列内容区域重新计算位置，再停止惯性并回到顶部。

## 框架边界

- 生产代码只新增一次现有公开 `UiList.RefreshLayout()` 调用。
- 保留 `ClearItems()`、`AddItem<BattleCardItemController>()` 和框架对象池生命周期。
- 没有修改 `RunStateSingletonRawComponent.HasCard()`、拥有数据、排序、传奇编号或卡牌绑定。
- 没有修改滚轮转发、空槽滚动、悬停、拖拽、Prefab、Builder、图片资源或 `.meta`。

## 验证结果

- `PreparationController.cs` Unity 标准脚本校验：0 warning、0 error。
- Unity 全量 EditMode：68 项全部通过，0 失败、0 跳过；任务 ID `2bf35ad018cf4a6d9661ed7f182714a7`。
- 扩充筛选回归测试，明确断言 Content 高度更新后必须再次执行卡池 `RefreshLayout()`。
- `Hearthstone.csproj`、`Hearthstone.Ui.Editor.csproj`、`Hearthstone.Tests.csproj` 均编译成功，0 error；各保留 8 条既有 Unity 依赖版本 warning。
- 定向 `git diff --check` 通过，`.meta` 差异为空。
- Console 中组合/未知初始关键词错误来自现有负向 CSV 测试的预期输入；全量测试仍全部通过。
- 按项目默认规则未进入 Play Mode。

## 文档处理

- 玩家视角设计文档已经准确描述筛选、反选、稳定编号、重排和回顶部，修复后实现重新符合文档，无需修改。
- 本次没有美术资源、布局规格或视觉规则变化，美术文档无需修改。
- 备战界面程序文档已补充“按新高度重新布局”及防止条目落入裁剪区外的运行顺序。

## 清单与清理

`OwnedCardFilterVisibilityFix-Checklist.md` 全部检查项通过并记录证据。按规定仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 0；清理前后临时文件数均为 190，没有可删除项。

## 剩余风险

未进入 Play Mode，因此没有录制实际点击筛选的画面；布局因果已从 `UiList` 的坐标计算顺序确认，并由全量 Editor 测试、编译和静态顺序断言覆盖。
