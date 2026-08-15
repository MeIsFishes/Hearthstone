不通过

## 审查基线

- 策划案：`AutoDoc/DesignPlan/2026.08.15/preparation-stage-card-pool.md`
- 已审查实施 Plan：`AutoDoc/DesignPlan/Plan/preparation-stage-card-pool-plan.md`
- 前序代码审查：`AutoDoc/Temp/preparation-stage-card-pool-code-review-round-3.md`
- 本趟失败证据：`AutoDoc/Temp/preparation-stage-card-pool-acceptance-review-attempt-retry-1.md`，失败范围为卡池滚到底后动态条目越出 Viewport，遮挡标题、奖励与三个战斗槽。
- Git：可用；采用当前 `HEAD` 到工作区，检查了 `git status --short`、工作区/暂存区 name-status、本趟 Builder、生成 Prefab 和新增 EditMode 测试。暂存区无差异。
- 本趟预期文件：`Assets/Scripts/Hearthstone/Ui/Editor/PreparationViewUiBuilder.cs`、`Assets/Scripts/Hearthstone/Tests/Editor/RunCardRulesTests.cs`、`Assets/Resources/Ui/PreparationView.prefab`，以及重建的另外两个 Preparation Prefab、`Assets/Scenes/Ui/Preparation.unity`、`Assets/Resources/Ui/Preparation.asset`、`Assets/Resources/BbxCommon/Ui/PreLoadUiData.asset`。
- Git 范围限制：Preparation Builder、测试、Prefab、Scene 与导出 Asset 在当前 HEAD 下仍为未跟踪文件，无法仅凭 Git 独立还原本趟修改前内容；本轮依据主代理提供的趟次范围、retry-1 失败记录、round 3 基线和当前文件内容建立针对性范围。
- 排除：未进入 Unity、未执行测试、未检视验收截图、不判断 `ART-*`/`FUNC-*` 通过。主代理提供的隔离 Play、相关 9/9、全 24/25、Main 干净和 Console 0 只作为背景，不替代本代码审查。

## 需求与代码实现覆盖表

| 需求或修正项 | 代码/配置/资源接入落点 | 代码层覆盖状态 |
| --- | --- | --- |
| Viewport 建立可实际裁切的标准 UI Mask | `PreparationViewUiBuilder.cs:109-119` 在 Viewport 的 Image 上加入 `Mask`，设置 `showMaskGraphic=false`，并保留 `RectMask2D` | 代码层完成 |
| Content 与动态条目位于双 Mask 后代链 | `PreparationViewUiBuilder.cs:123-135` 保持 `Viewport/Content/UiList`；生产 `UiList.AddItem` 形成 `Content/Controller/ViewPrefab`，测试构造也复现该层级 | 代码层完成 |
| 生成 Prefab 保存双 Mask 配置 | `PreparationView.prefab:382-463` 的 Viewport 同时保存 Image、Mask 与 RectMask2D，Mask Graphic 不显示且 Image 可承接 ScrollRect 射线 | 代码层完成 |
| Stencil 材质链能裁掉 Viewport 外条目 | `RunCardRulesTests.cs:233-257` 检查每个活动 Graphic 的 Mask/RectMask 父链与 `_Stencil=1`、`_StencilComp=3` | 代码层完成 |
| RectMask2D 对动态条目真实发起 cull | `RunCardRulesTests.cs:225-263` | 未完成：断言前由测试主动调用 `graphic.Cull(clipRect, true)`，不能证明 `RectMask2D.PerformClipping()` 或 Canvas 更新实际设置了 cull |
| Builder/Exporter 重建链保持框架内 | 既有 `PreparationUiBuilderUtility.SavePrefab`、公开 `UiApi.EditorOperation` 与 `PreparationUiSceneBuilder`/`UiSceneExporter` | 代码层完成 |

## 发现

### 中严重度：动态裁切测试自行制造待断言的 cull 状态

- 位置：`Assets/Scripts/Hearthstone/Tests/Editor/RunCardRulesTests.cs:229-263`，尤其是 `252-253` 与 `258-262`。
- 证据：测试先调用 `viewportRectMask.PerformClipping()`，但随后对每个 Graphic 又直接调用公开的 `graphic.Cull(clipRect, true)`；紧接着才断言 `graphic.canvasRenderer.cull == true`。由于测试已经明确传入不相交的裁切矩形并主动执行 Cull，该断言即使 RectMask2D 没有发现、登记或裁切动态条目也会通过。
- 影响：retry-1 的实际缺陷正是“已有 RectMask2D 与后代层级，但运行时条目仍越界显示”。当前测试能证明新增 Stencil Mask 和材质参数存在，却不能独立回归 RectMask2D 对真实动态条目的通知链；把该结果描述为“Image cull 已由动态真实层级测试覆盖”会掩盖同类回归。
- 违反：本趟明确要求测试真实覆盖动态条目而非 trick；Plan 的动态条目/滚动裁切闭环；代码审查不得用手动调用结果代替实际调用链证据。
- 必须修正：在完成 Canvas 更新和 `viewportRectMask.PerformClipping()` 后、任何测试侧 `graphic.Cull` 调用之前直接断言 `canvasRenderer.cull`；若 EditMode 环境确实无法驱动 RectMask 注册链，则删除“RectMask 实际 cull”结论，把测试明确限定为层级与 Stencil 材质测试，并另用能够驱动生产 Canvas/UiList 生命周期的最小测试覆盖 cull。不得由测试主动设置待验证状态。

## 双 Mask 设计审计

- `Mask(showMaskGraphic=false) + RectMask2D` 位于同一个矩形 Viewport 上，几何范围一致。Mask 提供可靠的 Stencil 裁切，RectMask2D 提供矩形 cull；两者没有建立平行业务状态，也没有改变 Content、UiList 或 ScrollRect 数据流。
- Viewport Image 使用标准白色、无 Sprite 的矩形 Graphic，`showMaskGraphic=false` 只保留 Stencil 写入，不形成可见白块；Image 继续作为 ScrollRect 的射线入口。未发现标题、奖励或槽位进入该父链，它们不会被误裁。
- 拖拽时既有 `UiApi.SetUiTop` 会让 requester 暂时离开 Viewport Mask 父链，符合拖起层可见要求；归位后恢复原父链。新增 Mask 没有改写交互回调。
- 性能上会同时承担 Stencil 材质修饰和 RectMask2D cull，但当前都是单一 Viewport、共享标准 UI/TMP 材质且页面不新增每帧业务分配。98 个常驻条目本来就是 Plan 的既定 UiList 方案；本趟未发现足以构成阻塞的材质实例泄漏或额外对象池旁路。
- 因此双 Mask 实现本身代码设计合理；本轮不通过仅由回归测试证据存在自证式调用造成。

## 框架边界审计

- 实现通过框架边界检查。Builder 使用 Unity UI 的公开 Image、Mask、RectMask2D、ScrollRect 与项目既有 UiBuilder/UiSceneExporter；未访问内部 Manager、未反射私有 API、未手写 Prefab/Scene/Asset YAML。
- 动态条目仍由生产 `UiList.AddItem`/`UiApi.OpenUiController` 创建并池化；本趟没有为裁切建立业务 MonoBehaviour、并行 Canvas 或截图专用遮罩。
- 未识别新的框架能力缺口；不需要本趟框架迭代。

## 特定需求 trick 汇报

- 发现一处测试 trick：`RunCardRulesTests.cs:253` 主动调用 `graphic.Cull(...)` 后再于 `260-262` 断言 cull，属于由测试写入待验证状态。该调用不在生产代码中，但会产生虚假的回归覆盖信号，必须按上述方向移除或重构。
- 生产 Builder/Prefab 未发现特定截图 trick；新增 Mask 是标准 UI 裁切组件，不是外层临时遮挡或业务旁路。

## 超出范围与无法确认的风险

- 本轮未执行 Unity、测试或玩家可见截图检视，因此不判断滚到 `98` 后的实际画面、交互命中或策划案验收结果。
- Preparation 相关文件相对 HEAD 为未跟踪文件，Git 无法提供本趟前后 diff；当前 Builder 与 Prefab 序列化字段一致，但历史生成操作不能由版本差异独立证明。
- 全套测试唯一 `Boar_001` 失败是已知任务外基线问题，不属于本趟裁切差异。
- 其它工作区 Stage、字体、资源索引与 `.meta` 差异不属于本趟复审范围。

本结论仅为代码审查结论，不代表策划案验收通过；本报告未修改评审意见文件以外的任何文件，不代替主代理验收、编写正式 Review 或实现修正。
