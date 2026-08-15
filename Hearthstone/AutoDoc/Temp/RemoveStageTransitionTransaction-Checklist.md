# 移除 Stage 切换事务检查清单

- [x] **通过**：已移除用户取消的“目标 Stage 加载失败必须回滚并恢复旧 Stage”要求及其事务框架实现。证据：`Assets/Scripts/` 扫描无 `ITransactionalStageLoad`、`GameStageTransition*`、`RegisterCompensation`、`FailTransition` 等引用。
- [x] **通过**：`GameEngineBase` 已恢复既有 operation batch 切换。证据：与提交 `a1c7d66^` 对比，忽略两处空白后框架文件无语义差异；`SetActiveGameStage` 恢复 `void`，完成回调恢复 `OnStageLoadingCompleted`。
- [x] **通过**：`GameStage` 已恢复普通 `IStageLoad`、Scene、UiScene、DataGroup、System、Listener 生命周期。证据：Validate/Prepare/Suspend/HiddenCommit/Publish/Rollback 类型及方法扫描为零。
- [x] **通过**：`HearthstoneGameEngine` 已去除框架 AttemptId/Result 和失败回退分支；仍保留 Coordinator 加载去重、`PreparationContinueTransactionSnapshot` 阵容快照、成功加载后的关卡提交。
- [x] **通过**：`InitializeBattleRuntime`、`InitializePreparationRuntime`、`InitializeRunStateRuntime` 均恢复实现 `IStageLoad`，Load/Unload 业务行为保留。
- [x] **通过**：原事务测试改为简单生命周期契约测试，Continue 测试改为验证加载期间去重及完成；`git diff --name-only -- '*.meta'` 返回空，未修改、创建或删除 `.meta`。
- [x] **通过**：扫描直接调用方无已删除类型引用；删除 `m_LoadingFrameworkAttemptId`、失败结果枚举值、RunState 补偿快照等一次性事务抽象。
- [x] **通过**：`BbxCommon.csproj`、`Hearthstone.csproj`、`Hearthstone.Tests.csproj` 分别构建为 0 error；仅有仓库既存程序集版本冲突警告。解决方案级构建因 `.sln` 内两个同名 `Hearthstone` 项目而无法启动，属既存工程配置问题。未进入游戏；Unity Editor 当前由进程 22092 打开，未另启批处理测试。
- [x] **通过**：已读取 `design-doc-format`；更新玩家文档，移除内部“StageGroup 事务”措辞但保留玩家可见的首击等待、阵容快照和重复点击阻挡。正式继续策划案同步删除失败回退场景、配置与验收项，并在历史 Plan/Review 标记事务内容已失效。
- [x] **不适用**：已读取 `art-doc-writer` 并核对 Preparation 模块与 UI 美术文档；本次不改变按钮四态、布局、图片、Prefab 或视觉表现，无需修改美术文档。
- [x] **通过**：已读取 `program-doc-format`；`AutoDoc/Program/Specific/preparation-card-pool/preparation-card-pool.md` 已改为普通 StageGroup 加载完成回调和反向卸载/正向加载顺序，删除 hidden commit、失败恢复和事务表述。
- [x] **通过**：框架边界审计确认 Battle/Preparation 入口仍只调用一次 `StageWrapper.SetActiveGameStage(m_RunStateStage, targetStage)`；未建立平行加载器，UiScene、ECS、Data 与资源入口保持既有框架路径。
- [x] **通过**：结束审计已完成；`git diff --check` 通过，待执行且只执行一次 `AutoDoc/CleanupTempDocs.bat`，随后创建报告。
