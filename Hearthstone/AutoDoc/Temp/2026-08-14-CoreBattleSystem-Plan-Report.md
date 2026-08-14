# 核心战斗系统 Plan 任务报告

## 任务结果

已完成核心自动战斗系统的需求修正稿与正式实现 Plan。方案固定为双方各 3 张 5 血 3 攻卡牌、我方先手、双方独立槽位游标、死亡立即跳过、存活目标等概率随机、同时伤害、结算后立即判胜以及纯自动最小 UI。

## 检查项结果与证据

- **需求：通过。** 已确认需求稿和最终 Plan 均使用“5 血 3 攻”；双方同时空场按敌方空场优先分支判我方胜利，不定义平局。
- **数据：通过。** 卡牌状态归属 `BattleCardRawComponent`，整场状态归属 `BattleSessionSingletonRawComponent`；固定原型规则不建立 CsvData、ScriptableObject 或 DataGroup。
- **逻辑：通过。** `BattleRules` 与 `[DisableAutoCreation] BattleSystem` 的职责、行动顺序、确定性随机、终局停止及测试边界均已明确。
- **UI：通过。** 页面与动态条目使用成对 View/Controller、`UiList`、ModelWrapper、条目预加载导出、UI 编辑场景和 `UiSceneExporter` 完整链路；无运行时拼整页或手写 Unity YAML。
- **GameStage：通过。** LoadItem、System、UiScene 与 GameEngine 入口均归属 `BattleStage`，原 Placeholder 仅从启动组合停用。
- **资产：通过。** 实际扫描无游戏用 2D 图片；复用 `CanvasProto.prefab`，首版用 UGUI 平色块与 TMP 文本，无音频或第三方资产新增。
- **框架边界：通过。** 只使用 BbxCommon 公开 ECS/UI/GameStage 入口和 Editor 导出流程；不需要框架改动，不处理 `.meta`。
- **结构与 Todo：通过。** 最终 Plan 保留 1—7 连续章节，共 16 条一一对应的实施 Todo；无“可选/可能/待定”并行路线。

## 验证结果

自动文档检查结果：`PlanExists=True`、`RequirementCorrect=True`、`CoreRulesPresent=True`、`FrameworkPresent=True`、`NoUnityYamlBypass=True`、`TodoCount16=True`、`NoParallelWording=True`、`NoMetaMutation=True`。现状文档扫描结果为空，游戏用图片资产扫描结果为空，与 Plan 的现状陈述一致。

本任务只输出方案，没有修改或编译游戏代码，也没有打开 Play Mode；相关编译、Editor 资产链路和玩法验收已作为实施 Todo 写入最终 Plan。

## 执行偏差

初始需求确认稿误写为“5 攻 3 血”并设置平局，已根据用户确认修正为“5 血 3 攻”且无平局。第一轮 Plan 曾包含删除 Placeholder 源码/资产，框架审计后收敛为仅停用启动链路，以避免扩大范围和触碰 `.meta`。

## 未解决风险

用户第 3 条只写“判胜”，未进一步指定双方同时空场时胜者。方案已提前明示并固定解释为“我方胜利”；若用户实际意图是“最后出手方胜利”或其他规则，应在实施前修改同归于尽的判定优先级。

## 文档处理与清理结果

- 已更新：`AutoDoc/Temp/Plan/2026-08-14-CoreBattleSystem-RequirementDraft.md`。
- 已新增：`AutoDoc/Temp/Plan/2026-08-14-CoreBattleSystem-Plan.md`。
- 已复核：`AutoDoc/Temp/2026-08-14-CoreBattleSystem-Plan-Checklist.md`。
- `AutoDoc/CleanupTempDocs.bat` 仅执行一次，退出码为 `0`；根级临时 Markdown 数量执行前后均为 22，未达到清理阈值，因此未删除文件。
