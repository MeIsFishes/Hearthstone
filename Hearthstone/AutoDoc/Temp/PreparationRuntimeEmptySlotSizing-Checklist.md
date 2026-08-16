# 备战运行时空槽实际尺寸修正检查清单

- [通过] 依据用户截图核对运行时空态链路。证据：`BattleCardItemController.ApplyPreparationState()` 在 `BattleSlot/FusionSlot` 绑定且 `occupied == false` 时分别打开专用空态；并非误用了卡池通用空态。上次四边仅内缩 `12 px`，宽度只减少约 `10%`，缩小幅度不足是画面仍显得很大的原因。
- [通过] 将出战与融合页真正渲染的空槽明显缩小。证据：继续复用 `PreparationPoolEmptySlot.png`，四边内缩改为 `50 px`；源矩形 `150 × 260`，保持宽高比后的绘制区 `150 × 225`，未新增 UI 或 Sprite。
- [通过] 确保相邻空槽存在清楚间距。证据：出战中心距 `185 px`，可见空槽 `84.36 × 126.54`、相邻间距 `100.64 px`；融合中心距 `190 px`，可见空槽 `93.48 × 140.22`、相邻间距 `96.52 px`。六个出战槽仍位于 `1110 px` 列表宽度内，四个融合槽仍位于 `800 px` 列表范围内。
- [通过] 保持完整条目根与交互范围不变。证据：只调整两个空态 Image 子节点；条目根、`CardBackground` 射线面、`UiDragable`、`UiInteractor`、投放高亮、占用卡面、`UiList` 步长和对象池代码均未改。
- [通过] 扩展检查脚本。证据：测试同时验证 `sizeDelta = (-100, -100)`、`Image.preserveAspect`、绘制区 `150 × 225`、运行时缩放后的间距、空槽宽度不超过占用卡面 `60%`，并确认专用空态的运行时显隐条件。
- [通过] 修改一一对应的 `BattleCardItemUiBuilder` 并重建 Prefab。证据：通过 Unity Editor 正式通道执行 `Hearthstone.BattleCardItemUiBuilder.Build()`，保存 `BattleCardItem.prefab`；未手写 Prefab/Scene/Asset YAML，未创建或修改 `.meta`。
- [不适用] UiScene 导出信息未变化。证据：未修改页面 Group、DefaultShow、整体 Transform、View 导出字段或场景连接，`Preparation.asset` 无工作区差异，故未重导。
- [通过] 完成验证。证据：定向 EditMode `1/1`、完整 EditMode `101/101` 通过；`Hearthstone.csproj`、`Hearthstone.Ui.Editor.csproj`、`Hearthstone.Tests.csproj` 均 `0` 错误；Unity 几何实测与预期一致，Console 清理后 `0` error；活动场景 `Main`、未 dirty、未进入 Play Mode。
- [通过] 框架边界审计。证据：继续使用既有 View/Controller、共享 Prefab、`UiList`、对象池、预加载映射与 UiBuilder；没有运行时临时缩放或平行 UI。
- [通过] 检查误改、依赖和抽象。证据：本次功能改动仅为既有 Builder 常量、生成 Prefab 与对应 Editor 测试；没有新增一次性函数或字段，相关文件 `git diff --check` 通过，工作区其他改动均保留。
- [通过] 玩家视角设计文档。证据：已完整读取 `design-doc-format`，同步空槽视觉约为占用卡面六成宽且不缩小拖放范围的当前体验。
- [通过] 美术文档。证据：已完整读取 `art-doc-writer` 与模块格式，同步 `50 px` 安全区、源/绘制/运行时尺寸及间距。
- [通过] 程序文档。证据：已完整读取 `program-doc-format` 与 UI 界面格式，同步专用空态显隐链路、Builder 参数和运行时几何。
- [通过] 结束前逐项复核完成；清理脚本与报告将在本审计保存后执行。
