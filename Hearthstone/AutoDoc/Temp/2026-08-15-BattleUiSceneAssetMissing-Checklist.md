# Battle UiSceneAsset 缺失修复检查清单

- [x] **通过**：`BattleStages.TryAddBattleUi` 以 `Resources.Load<UiSceneAsset>("Ui/Battle")` 加载，缺失时输出用户所见错误；资源 key 与 `UiSceneExporter` 以场景名 `Battle` 导出的路径契约一致。
- [x] **通过**：C# View/Controller、`BattleUiScene` 与 `EBattleUiGroup.Main` 已存在；`Assets/Resources/Ui` 和 `Assets/Scenes/Ui` 中不存在 Battle View Prefab、卡牌条目 Prefab、Battle UI 编辑场景或 Battle Asset，`PreLoadUiData` 资产也不存在。判定为完整资产流程未执行，而非单纯路径错误。
- [x] **通过**：未运行时拼装 UI，未手写或直接修改 `UiSceneAsset.UiObjectDatas`；确认正确修复必须由 Prefab、UI 编辑场景和 `UiSceneExporter` 产出。
- [x] **通过**：现有 Battle Stage 使用 `GetOrCreateUiScene<BattleUiScene>()` 与 `SetUiScene`，符合 GameStage 消费导出 Asset 的契约；未修改该注册流程。
- [x] **不适用**：此处框架正式范式就是按 Resources 相对路径加载 `UiSceneAsset`，不是 `ResourceApi` 文件名索引；现有 `Ui/Battle` key 正确，无需改成文件名加载。
- [ ] **未通过**：当前会话没有任何 Codely/Unity 工具。项目 `AGENTS.md` 指定 Codely 1.0.75，但 `.com-unity-codely.json` 不存在；`manifest.json`、`packages-lock.json` 与 Unity 启动命令实际采用 Coplay MCP for Unity v10.0.0，存在配置源冲突。
- [ ] **未通过**：修复需要 Unity Editor 创建两个 Prefab、Battle UI 编辑场景、导出 `Battle.asset`、生成 UI 预加载映射并重建资源字典。当前缺少项目允许的 Unity 工具，无法在本会话安全完成；未重启或破坏正在运行的 Unity Editor。
- [x] **不适用**：未修改业务源码，因此没有新增函数、字段、一次性抽象或 Harness；已核对直接调用方和预加载依赖。
- [x] **通过**：框架边界审计确认不能以手写 YAML、私有 Socket、标准 SDK 写调用、自动触发的临时 Builder 或无 UI 的运行时降级掩盖缺失资产；本次没有建立平行实现。
- [x] **不适用**：本次未改变游戏现状，只完成故障诊断；当前工程也没有 `AutoDoc/Program/`、`AutoDoc/Design/` 或 `AutoDoc/Art/` 目录可同步。
- [x] **通过**：除本任务检查清单和报告外未修改项目代码、配置、Unity 资产或任何 `.meta` 文件。
- [x] **通过**：仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0；清理后已创建同任务名报告并记录阻塞证据。
