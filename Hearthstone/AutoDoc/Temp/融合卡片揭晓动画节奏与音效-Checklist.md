# 融合卡片揭晓动画节奏与音效检查清单

## 用户要求

- [x] **通过**：揭晓卡片先变大、再缩小，最终回到适合展示结果的尺寸。证据：缩放由 `0.72` 倍升至 `1.28` 倍，并在真实结果显示前平滑回落至 `1` 倍。
- [x] **通过**：整体揭晓动画播放速度降低到当前速度的 `80%`。证据：时间轴统一使用 `deltaTime × FusionRevealPlaybackSpeed` 推进，常量为 `0.8f`，实际总时长由约 `3.39 s` 延长至约 `4.24 s`。
- [x] **通过**：为揭晓动画过程配置音效。证据：成功动画开始时播放 `card-shuffle`，音频时长约 `3.063 s`。
- [x] **通过**：为结果正式揭晓的瞬间配置独立音效。证据：结果卡面首次显示的单帧播放 `highUp`，音频时长约 `0.549 s`，布尔状态防止重复播放。
- [x] **通过**：优先复用项目现有合适音频；仅在没有可用资源时制作新音频。证据：复用现有 Kenney Casino Audio 与 Digital Audio 音效库，两个键均唯一且资源索引可解析，因此未制作新音频。

## 实现与验证

- [x] **通过**：已核对现有揭晓时间轴、UI 生命周期、音频播放公开接口和可用音频资源。证据：完整读取 `bbxcommon-audio`、`playback.md`、`selection.md` 以及 `AudioApi`、`AudioPlayOptions`、`AudioHandle`、`ResourceApi` 当前实现。
- [x] **通过**：缩放曲线包含明显的放大峰值与随后回落阶段，且不影响正反面切换与闪光时序。证据：只替换卡根缩放计算，既有 `90°/270°/360°` 面切换和旋转结束后的闪光节点保持不变。
- [x] **通过**：所有时间段按统一倍率放慢，不以零散常量造成时序错位。证据：所有阶段继续共享同一逻辑时间 `m_FusionRevealElapsed`，仅推进速度统一乘 `0.8f`。
- [x] **通过**：动画音效只在成功揭晓开始时播放一次，揭晓瞬间音效只在跨过揭晓节点时播放一次。证据：开始声位于 `StartFusionReveal()`；揭晓声受 `m_FusionRevealMomentAudioPlayed` 门控。
- [x] **通过**：页面隐藏、关闭或动画复位后不会残留触发状态，也不会因大帧间隔重复播放。证据：`ResetFusionReveal()` 停止两个 `AudioHandle`、清空句柄并复位揭晓声标记；同帧跨过节点也只会触发一次。
- [x] **通过**：新增字段、常量和函数确有必要，不产生每帧重复分配。证据：两个句柄和一个布尔值分别承担音频生命周期与单次触发；播放选项只在两个声音触发时创建，逐帧路径只使用值类型运算。
- [x] **通过**：执行 Unity 编译、相关结构检查与完整 EditMode 测试；按项目默认不主动进入 Play Mode。证据：Unity 脚本域刷新后 Ready；EditMode `66/66` 通过、`0` 失败、`0` 跳过，结果为 `Passed`。
- [x] **通过**：未误改无关文件，未创建、编辑或删除任何 `.meta` 文件。证据：本轮仅修改揭晓 Controller、其 Editor 测试、三类正式文档及任务清单/报告；音频资源和许可证只读复用。

## 框架边界审计

- [x] **通过**：音频通过项目既有公开 API 与资源系统播放，不直接访问底层管理器或另建平行播放器。证据：使用 `AudioApi.Play/Stop`、无目录 basename 键、`AudioPlayOptions.Default`、稳定 Group/Concurrency Key 与 `MaxConcurrent = 1`。
- [x] **通过**：UI 继续使用 View/Controller、静态 Prefab/UiBuilder、既有生命周期与对象池；不在运行时拼装页面静态层级。证据：本轮仅修改 `PreparationController` 的逐帧表现和音频触发，View、Prefab、Builder、UiScene 与预加载映射均无需变化。
- [x] **不适用**：未发现框架能力缺口；现有 UI 更新生命周期与音频 API 已完整覆盖需求。

## 文档同步

- [x] **通过**：完整读取 `design-doc-format` 并更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`，记录先放大后缩小、舒缓节奏和两段听觉反馈。
- [x] **通过**：完整读取 `art-doc-writer` 与模块格式，更新 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md` 的缩放峰值与回落规范；音频不属于 2D 资产列表，且本轮未新增位图。
- [x] **通过**：完整读取 `program-doc-format` 与 UI 页面格式，更新 `AutoDoc/Program/UI/preparation/preparation.md`，记录 `0.8` 时间倍率、缩放曲线、音频键、播放参数、限流和停止路径。

## 结束审计

- [x] **通过**：已重新读取本清单，并逐项对照代码、资源、许可证、测试结果与正式文档复核。
- [x] **通过**：仅运行一次 `AutoDoc/CleanupTempDocs.bat`；退出码 `0`，运行前后均为 `143` 篇 Markdown，未触发删除。
- [x] **通过**：清理后已创建 `融合卡片揭晓动画节奏与音效-Report.md`。
