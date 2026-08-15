# 移除 Stage 切换事务报告

## 任务结果

已按用户要求取消“目标 Stage 加载失败必须回滚并恢复旧 Stage”的要求，并移除为该要求新增的跨 Stage 事务框架。`GameEngineBase` 与 `GameStage` 恢复事务改动前的普通切换语义：反向卸载不再需要的业务 Stage，再正向加载尚未加载的目标 Stage，最后调用 `OnStageLoadingCompleted`。

Continue 的正常功能保持不变：点击瞬间阵容快照、等待态防重复、canonical request key、StageGroup 请求串行、正常加载后的关卡号提交和下一关奖励输入仍保留。

## 检查项结果与证据

- **通过——事务框架移除**：`Assets/Scripts/` 中不存在 `ITransactionalStageLoad`、`GameStageTransitionContext/Result`、Validate/Prepare/Suspend/HiddenCommit/Publish/Rollback、补偿栈或框架 AttemptId 引用。
- **通过——框架恢复**：`GameEngineBase.cs` 和 `GameStage.cs` 与 `a1c7d66^` 的事务前版本进行忽略空白对比，无语义差异；仅清除了两处既存行尾空格。
- **通过——业务适配**：Battle、Preparation、RunState 三个初始化项均恢复为 `IStageLoad`；`HearthstoneGameEngine` 使用 `OnStageLoadingCompleted`，不再处理事务结果或失败回退。
- **通过——正常 Continue 保留**：`PreparationContinueTransactionSnapshot` 仍防御性保存点击阵容；Coordinator 仍合并加载中的同 key 请求；加载完成后仍只提交一次关卡进度。
- **通过——测试调整**：事务专属测试改为简单 Stage 生命周期契约检查；Continue Coordinator 测试改为验证请求去重直到正常完成。
- **通过——无 `.meta` 操作**：`git diff --name-only -- '*.meta'` 无输出。
- **通过——框架边界**：Battle/Preparation Group 入口仍各调用一次 `StageWrapper.SetActiveGameStage(m_RunStateStage, targetStage)`；没有 Hearthstone 私有加载器或 ECS/UI/Data/资源绕行。

## 验证结果

- `git diff --check`：通过。
- 事务代码引用扫描：0 项。
- `dotnet build BbxCommon.csproj --no-restore -p:UseSharedCompilation=false`：通过，0 error。
- `dotnet build Hearthstone.csproj --no-restore -p:UseSharedCompilation=false`：通过，0 error。
- `dotnet build Hearthstone.Tests.csproj --no-restore -p:UseSharedCompilation=false`：通过，0 error。
- 三个构建仍报告仓库既存的程序集版本冲突警告；没有本次新增警告或错误。
- `dotnet build Hearthstone.sln --no-restore` 无法启动，因为现有 `.sln` 含两个同名 `Hearthstone` 项目；已改用三个直接相关项目逐项构建。
- `dotnet test Hearthstone.Tests.csproj --no-build --no-restore` 退出码为 0 但未发现可执行的 VSTest 测试；本次只把测试程序集编译作为证据。Unity Editor 当前已由进程 22092 打开，未另启批处理 EditMode 测试；按项目默认规则未进入游戏。

## 文档处理

- 玩家视角：更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`，删除内部“StageGroup 事务”措辞；玩家可见的阵容快照、等待态和重复输入阻挡保持不变。
- 策划案：`preparation-stage-continue.md` 已删除失败回退场景、配置和验收项；历史 Plan/Review 顶部标记事务内容已由后续用户决定取消，不再代表当前实现。
- 程序文档：更新 `AutoDoc/Program/Specific/preparation-card-pool/preparation-card-pool.md`，改为普通 LoadItem 与 StageGroup 加载完成语义。
- 美术文档：已核对 Preparation 模块和 UI 总览；按钮四态、布局、图片和 Prefab 均未变化，不适用修改。

## 偏差与未解决风险

- 未执行 Unity EditMode/PlayMode 运行验证；代码和测试程序集已编译通过。
- 恢复普通切换后，目标 Stage 加载异常不再承诺回滚旧 Stage，这是本次用户明确要求取消的行为，不记为实现缺口。
- 工作区原有未跟踪文件 `Library.rar` 未触碰。

## 清理结果

任务结束前只运行了一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0。检查清单按项目审计要求保留，本报告在清理后创建。
