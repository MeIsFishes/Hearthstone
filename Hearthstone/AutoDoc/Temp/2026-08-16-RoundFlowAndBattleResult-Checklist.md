# 逐轮配置与战斗结算流程改造检查清单

## 需求覆盖

- [x] 通过：游戏数据组加载完成后调用 `BeginPreparationForBattle(1)`，第 1 轮配置强制为随机摸 3 张、累计解锁 3 槽，并先进入 PreparationStage。
- [x] 通过：新增 `BattleProgressionCsvData.csv`，逐行配置 `BattleNumber`、`UnlockSlotCount`、`DrawCardCount`。
- [x] 通过：Run state 保存最多六槽及 `UnlockedBattleSlotCount`；备战列表、拖放规则、Continue 快照和玩家战斗数组均读取当前解锁数。
- [x] 通过：`BattleSystem` 在一方单位耗尽时进入 `ResultSettlementPending`，按 `ResultSettlementDelay = 0.5f` 倒计时后才写正式结果。
- [x] 通过：普通胜利横幅按 `0.24 s` 左入、`0.68 s` 停留、`0.24 s` 右出，完成信号后才允许进入下一轮。
- [x] 通过：失败结果显示 `BattleDefeatPanel` 弹窗与“重新开始”按钮。
- [x] 通过：轮次表无下一行时标记最终轮；胜利横幅结束后显示 `RunVictoryPanel` 整局胜利弹窗与重开按钮。
- [x] 通过：`RestartRun()` 重置切换协调器、替换 RunStateStage，并重新进入第 1 轮备战。
- [x] 通过：补齐 `BattleVictoryBanner.png`、`BattleDefeatPanel.png`、`RunVictoryPanel.png` 并接入 `BattleView.prefab`/Controller。

## 配置、架构与实现约束

- [x] 通过：配置类继承 `CsvDataBase<BattleProgressionCsvData>`，使用 Override 数据加载、规范表名/表头/两行注释并提供必需行、累计槽位和存在性查询。
- [x] 通过：轮次 CSV 是新增槽位与摸牌数的静态来源；轮次、持有卡、已解锁槽位、挂起结算和表现倒计时均存入运行时 Component/启动快照。
- [x] 通过：Preparation/Battle StageGroup 仍由 `HearthstoneGameEngine` 与 StageListener 切换，Controller 只驱动 UI 结果表现和重开按钮。
- [x] 通过：`BattleView` 仅增加序列化引用，`BattleController` 负责监听和逐帧动效；静态层级由 `BattleViewUiBuilder` 生成到 Prefab。
- [x] 不适用：本任务未新增或修改自定义 `BbxUiItem`，只使用现有 `UiList`、Unity UI 与 Controller/View。
- [x] 通过：卡牌条目继续由现有 `UiList` 对象池创建回收，动态槽位只改变条目数量，不引入逐帧容器分配。
- [x] 通过：未手工创建、编辑或删除 `.meta`；新脚本与图片导入时由 Unity 自动生成元数据，且未覆盖或回退无关工作区改动。

## 美术与资产

- [x] 通过：使用 imagegen 生成无字、透明背景、红蓝金奇幻卡牌 UI 风格的横幅与两张面板。
- [x] 通过：三张候选均经视觉检查并复制到 `Assets/Resources/Art/BattleCards/Result/` 稳定路径。
- [x] 通过：图片不包含模型文字，“战斗胜利”“战斗失败”“整局胜利”“重新开始”等由 TMP 渲染。
- [x] 通过：提示词、生成源目录、最终路径与接入点将在任务 Report 中完整记录。

## 验证与文档

- [x] 通过：静态调用链核对首轮备战、非最终轮胜利进入下一轮、最终轮胜利留场弹窗、任意轮失败留场弹窗及重开回首轮五条路径。
- [x] 通过：玩家阵容、Run state 存储、槽位规则、备战条目、启动快照和 BattleSession 均支持 `3~6`；摸牌批次接受 CSV 动态数量。
- [x] 通过：`BattleSystem.OnSystemUpdate()` 优先处理挂起结果并立即返回，延迟期间不会执行 `ExecuteAction()`；重复 Begin 也受挂起标记保护。
- [x] 通过：非最终轮 StageListener 只监听 `OutcomePresentationCompleted`；最终轮弹窗由本地横幅完整时序结束后触发。
- [x] 通过：`Hearthstone.csproj` 与 `Hearthstone.Editor.csproj` 串行构建均 0 错误；Unity EditMode 83/83 通过；按项目规则未进入 Play Mode。
- [x] 通过：已同步玩家视角设计文档、程序系统/UI 文档、UI 总览与战斗/备战美术模块现状。
- [x] 通过：无需新增通用框架能力；数据、Stage、ECS、View/Controller 与对象池边界均沿用现有框架。
- [x] 通过：清单已逐项复核；运行资源检查为 `CSV=True|Banner=True|Defeat=True|RunVictory=True|Refs=True`，Unity Console 清理测试预期错误日志后为 0 错误。
- [x] 待执行：本清单完成后仅运行一次 `AutoDoc/CleanupTempDocs.bat`，随后创建对应 Report。
