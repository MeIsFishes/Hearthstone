# 创建新的 StageGroup 入口

仅在现有入口无法进入目标 StageGroup 或无法表达必需输入时读取本文。用户已授权创建新的项目层 Editor StageGroup 入口适配文件与入口资产，不必再次询问；该授权不允许在项目层复制框架 Launcher、runner、窗口或入口资产协议。

1. 先读取 `.codex/private-skills/game-stage/SKILL.md`，再按其路由读取 `stage-entry-window.md`；涉及必需外部输入时同时读取 `stage-startup-data-convention.md`。
2. 在项目 Editor 程序集中创建继承框架 `GameStageEntryAsset` 的 `XxxStageEntryAsset.cs`；不要新建项目专用入口基类。
3. 只声明让目标 StageGroup 正常启动所需的序列化字段，在 `ValidateEntry` 中校验结构；通过 `CreateStageGroupBuildCallback()` 返回 `Func<bool>` 构建回调，在回调中等待 GameEngine 或其它 Editor-only 启动条件、构造新的强类型 StartupData 并调用正式具名 Group 入口。未满足条件时返回 `false`，完成时返回 `true`。
4. 不在 Editor 入口内创建 Stage 或直接调用 `SetActiveGameStage`。如果正式具名 Group 入口尚不存在，先按 `game-stage` 把它建立在项目 GameEngine 上。
5. 编译完成后可由临时 Editor 脚本调用 `GameStageWindow.CreateEntryAsset(typeof(XxxStageEntryAsset))` 创建资产；资产必须位于 `Assets/Resources/Editor/`。
6. 配置新资产字段后调用 `GameStageEntryLauncher.Start(entry)`。以后仅参数值变化时复用该资产，不再创建同类入口。
