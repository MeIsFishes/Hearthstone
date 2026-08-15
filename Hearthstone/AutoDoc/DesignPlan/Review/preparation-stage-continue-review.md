## 1. 实现方式

以事务式 Stage 切换提交点击阵容快照，并在备战页提供常驻四态继续按钮。

## 2. 验收

### 2.1 验收方式

用户在 2026-08-15 明确免除进游戏验收，本篇改用代码逻辑、配置接入、Unity 编译、EditMode 测试和资产编排检查收口。主代理复核 `PreparationContinueTests` 作业 `6fec5c76f99b45f5a1f476bb8cf49117` 为 5/5，通过 0/1/3 槽、防御复制、默认 `Scenario=null`、canonical key、重复合并及失败可重试；`GameStageTransactionTests` 作业 `0bf4bf584aab4628aae7bdd21e93dc43` 为 6/6。最终全 EditMode 作业 `32dd621e7dfa4ba78a9bb9f546d52c37` 为 55/56，唯一失败是任务前 `Boar_001` 与旧断言 `Boar` 不一致。Unity 刷新后无编译错误。

### 2.2 美术资产验收

- `ART-01`：通过。正式 `PreparationContinueButtonIdle.png` 使用红金切角外框，`PreparationView.prefab` 将“继续”和“下一关”作为独立 TMP 层置于框内。
- `ART-02`：通过。Idle、Highlighted、Pressed、Waiting 四张正式 Sprite 均为 `1024×420`，通过标准 Button SpriteSwap 与 Waiting 图片切换覆盖四态，尺寸和锚点一致。
- `ART-03`：通过。Continue 根对象是出战区、融合区及卡池的兄弟节点，锚定右上角且位于滚动区外；两个页签只切换各自操作区，不切换 Continue 根。主代理通过公开 Builder 重建 `PreparationView.prefab`、`Preparation.unity` 和导出资产，确认按钮、等待阻挡层、Sprite 与字体引用非空，所需中文无缺字。

### 2.3 程序功能验收

- `FUNC-01`：通过。按钮常驻页面根，页签切换和卡池滚动只记录页面状态，不改变按钮生命周期。
- `FUNC-02`：通过。点击阵容使用固定三槽快照，空槽保留为空；0/1/3 槽防御复制测试全部通过，Battle 创建允许稀疏玩家 Entity。
- `FUNC-03`：通过。点击时冻结卡号、永久攻血与词条；`PreparationContinueTransactionSnapshot`、`BattleStageStartupData` 和 Battle hidden commit 全链消费该快照，request key 也包含逐槽值，不再从可变出战槽重读。
- `FUNC-04`：通过。Continue 只快照融合槽用于事务记录，离开 Preparation 时按既有 Session 生命周期清空选择，不调用融合提交或消耗素材。
- `FUNC-05`：通过。Idle/Waiting 状态、静态输入阻挡层和 StageGroup coordinator 共同保证一个生产 Request；重复请求被合并并记录 `DuplicateIgnored`。
- `FUNC-06`：通过。Stage 调度按 Validate、Prepare、SuspendOld、HiddenCommit、Publish、UnloadOld、Complete 执行；目标失败逆序回滚并恢复旧 Stage 和 Idle，补偿、部分 Prepare、旧卸载异常与重试测试通过。
- `FUNC-07`：通过。`BattleProgressionCsvData` 为第 2 关提供唯一强类型奖励批次；关卡号、Battle 创建数和奖励账本只在事务 committed 回调提交，失败路径不写入，重复点击不重复创建。

## 4. 详细审查意见

本篇唯一代码审查 `AutoDoc/Temp/preparation-stage-continue-code-review-round-1.md` 初始结论为不通过，核心意见是点击阵容虽然进入日志快照，却未进入 Battle startup，hidden commit 会重读可变 RunState；同时缺少 Continue 专属回归和现状文档同步。成立项均已修正：新增防御性 `BattlePlayerLineupStartupData`，由点击事务、Battle startup、request key 和玩家 Entity 创建共享；新增 5 项 Continue 回归；Program、Design、Art 与 UI 现状文档同步四态按钮和事务边界。用户要求每类审查最多一次，因此未追加同类复审，主代理以最终调用链、5/5 与 6/6 定向测试、55/56 全量回归和公开 Builder 资产检查完成核对。

框架能力缺口通过扩展唯一 `GameStage`/`GameEngineBase` 调度器解决，没有建立 Hearthstone 平行加载器。事务对目标副作用进行准备、隐藏提交、发布和逆序补偿，旧调用保持兼容；Scene/DataGroup 在没有正式 staging adapter 时由严格 Validate 拒绝。本篇没有运行时拼装 UI、直接写 ECS 验收状态、手写 Scene/Prefab/FontAsset/资源索引或日志替代业务快照的 trick。按用户最终限定的代码逻辑验收口径，本篇结论为通过。
