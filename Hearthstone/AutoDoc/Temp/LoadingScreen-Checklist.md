# Loading 界面任务检查清单

- [通过] 用户需求：已增加一张全屏静态 Loading 图；Prefab 中不存在进度条、百分比、文字或旋转图标。
- [通过] 底层复用：`HearthstoneGameEngine.OnAwake()` 只调用既有 `SetLoadingUi<LoadingController>()`；显隐继续由 `GameEngineBase.StartLoading()` 管理，未复制加载调度。
- [通过] 美术调查：核对了 `battle-scene-concept-v2.png`、`BattleBoardBackground.png` 与 `PreparationPageBackground.png`，项目横向背景均为 `1672 × 941`，据此采用木质、暖金、深蓝和约 `16:9` 视觉语言。
- [通过] 图片生成：通过 `imagegen` 内置模式生成 `Assets/Resources/Art/Loading/UI/HearthstoneLoadingBackground.png`；实际尺寸 `1672 × 941`，目视确认无文字、标志、进度元素或水印。
- [通过] UI 与资源接入：新增唯一一对 `LoadingView` / `LoadingController`、一一对应 `LoadingViewUiBuilder`、`LoadingView.prefab`、Pre-load 映射与资源索引项；背景 Image 开启 Raycast 并由底层 Loading 生命周期显隐。
- [通过] 分辨率适配：Unity PreviewScene 验证 `1920×1080 → 1920.0×1080.6`、`2560×1080 → 2560.0×1440.8`、`1440×1080 → 1919.0×1080.0`，三种画布均 `cover=True`。
- [通过] 静态验证：Unity 强制刷新与编译成功；Prefab、Sprite 导入、Pre-load、`Resources.Load("Ui/LoadingView")` 均通过；Console 0 条错误，`Main.unity` 未变脏。按项目默认约束未进入 Play Mode。
- [通过] 抽象审计：Controller 只保留显示时全屏拉伸职责，View 只做类型映射；未新增一次性公共 API、状态字段或进度空实现。
- [通过] 框架边界审计：静态层级由 Builder 生成进 Prefab，使用公开 `UiApi.EditorOperation` 导出 Pre-load，通过 `SetLoadingUi` 接入 `UiGameEngineScene.Loading`；未手写 Prefab、`.asset` 或资源索引产物。
- [通过] 文件范围审计：任务修改限定在 Loading 代码/资源、引擎注册、框架生成的 Pre-load/ResourcesDictionary 和直接相关文档；工作区中其他备战/卡牌任务改动保持原状。`.meta` 仅由正在运行的 Unity Editor 自动导入生成，未手工创建、编辑或删除。
- [通过] 玩家视角设计文档：已完整读取 `design-doc-format`，新增 `AutoDoc/Design/loading-screen/loading-screen.md`，只记录当前可见的静态遮罩体验。
- [通过] 美术文档：已完整读取 `art-doc-writer` 及适用格式，更新风格总览与 UI 总览，并新增 `AutoDoc/Art/Modules/loading-screen/loading-screen.md`。
- [通过] 程序文档：已完整读取 `program-doc-format` 与 UI 界面格式，新增 `AutoDoc/Program/UI/loading-screen/loading-screen.md`，记录当前 Controller、Prefab、生命周期与 GameStage 边界。
- [通过] MCP 锁诊断：Restart Manager 确认 `Temp/UnityLockfile` 由可见 Unity 主编辑器 PID 46056 持有；MCP CI 进程未持锁，因此未卸载 MCP。现有 MCP 端到端读取活动场景与 Console 成功，并用于当前 Editor 内执行 Builder。
- [通过] 结束复核：已逐项核对实际文件、Unity 返回值、Console、分辨率测试和 Git 差异。
- [通过] 临时文档清理：已且仅已执行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 `0`；清理阈值未触发，其他任务检查清单未受影响。随后创建本任务报告。
