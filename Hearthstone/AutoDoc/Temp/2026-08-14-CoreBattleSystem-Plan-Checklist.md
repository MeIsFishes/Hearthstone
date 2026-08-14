# 核心战斗系统 Plan 检查清单

## 需求确认

- [x] **通过**：卡牌固定为 5 点生命、3 点攻击；见已确认需求稿 1.1.3 与最终 Plan 1.1.1。
- [x] **通过**：双方各生成 3 张牌，我方先手，敌我严格交替行动；见最终 Plan 1.1.1—1.1.2。
- [x] **通过**：双方分别按原始槽位从左到右循环选择攻击者，死亡牌立即退出并跳过；见最终 Plan 1.1.2—1.1.3。
- [x] **通过**：攻击目标从对方当前存活牌中等概率随机选择；见最终 Plan 1.1.4 与 3.1。
- [x] **通过**：攻击与反击使用结算前面板同时结算；见已确认需求稿 1.1.8 与最终 Plan 3.1。
- [x] **通过**：任一方无存活牌时立即结束；用户“判胜”已收敛为敌方空场优先，所以双方同次结算空场判我方胜利；见最终 Plan 1.1.5 和 3.1。
- [x] **通过**：第一阶段采用纯自动战斗与最小占位 UI，不加入复杂攻击动画；见最终 Plan 1.1.7—1.1.8。

## 方案结构与数据

- [x] **通过**：按 `plan-output-format` 的需求、数据、逻辑、UI、美术、GameStage、实现顺序连续编号，空章已省略；标题扫描结果为 1—7 连续。
- [x] **通过**：运行时数据唯一权威来源已限定为卡牌 RawComponent 与会话 Singleton，System/Controller/View 不建立平行状态；见最终 Plan 2.1。
- [x] **通过**：`BattleCardRawComponent` 与 `BattleSessionSingletonRawComponent` 的字段、Entity 归属和生命周期已列入 2.2.1。
- [x] **通过**：固定原型规则不引入 CsvData、ScriptableObject 或 DataGroup；见最终 Plan 2.1，相关空节已省略。
- [x] **通过**：非零 `RandomSeed`、`Unity.Mathematics.Random TargetRandom`、双攻击游标、0.75 秒行动节奏和无平局胜负枚举均有唯一归属；见最终 Plan 2.2.1 与 3.1。

## 逻辑、UI 与 Stage

- [x] **通过**：同时伤害、死亡跳过、活体随机、游标环回、胜负优先级和终局停止规则均已确定；见最终 Plan 3.1。
- [x] **通过**：规划 `[DisableAutoCreation] BattleSystem : EcsMixSystemBase`，并在 GameEngine 中固定于 Input 与 Task 之间；见最终 Plan 3.2.1 与 6.3。
- [x] **通过**：页面/条目 View 与 Controller、`UiList`、UI 编辑场景、Exporter、预加载映射、导出 Asset 和 Stage 注册链路完整；见最终 Plan 4.2—4.3。
- [x] **通过**：静态页面由 Prefab 承载，动态条目走 `UiList`；明确禁止手写 Scene/Prefab/UiSceneAsset YAML；见最终 Plan 4.1 与 4.3.2。
- [x] **通过**：`InitializeBattleRuntime`、`BattleSystem`、`BattleUiScene` 均逐项归属新增 `BattleStage`；见最终 Plan 6.1—6.3。

## 资产与现状

- [x] **通过**：现状依据来自 `Assets/Scripts/Hearthstone/`、BbxCommon 公开实现和真实 Assets；未读取 `AutoDoc/DesignPlan/`。扫描确认 `AutoDoc/Program/`、`AutoDoc/Art/`、`AutoDoc/Design/` 当前无可用现状文档。
- [x] **通过**：扫描确认项目无游戏用 2D 图片；`CanvasProto.prefab` 可复用，占位视觉固定使用 UGUI 内置白色 Sprite 着色与 TMP 文本；见最终 Plan 5.1—5.2。
- [x] **通过**：本轮不需要音频、额外字体、视频或第三方资源，按跳过规则省略“其他资产”章。

## 实施与框架边界

- [x] **通过**：最终 Plan 7 共 16 条 Todo，与 Component、规则/System、测试、UI 代码/Prefab、动态条目预加载、UiScene 创建/配置/导出、LoadItem、GameStage、启动切换和验证顺序一一对应。
- [x] **通过**：方案只使用 `EcsApi`、`ModelWrapper`/`UiList`/`UiApi`、`IStageLoad`、`GameStage` 和官方 UI 导出入口；没有访问底层 Manager 或手写导出产物。
- [x] **不适用**：现有框架能力可覆盖需求，无需新增框架 API 或设计业务层绕行。
- [x] **通过**：关键字扫描未发现“可选/可能/待定”，随机、胜负、UI、Stage 与占位链路均只有一条确定路线。
- [x] **通过**：Placeholder 仅从启动组合停用，不做无关破坏性清理；方案明确不创建、编辑或删除 `.meta` 文件。

## 结束审计

- [x] **通过**：修正后的需求稿与完整最终 Plan 已写入 `AutoDoc/Temp/Plan/`。
- [x] **通过**：已重新读取并执行自动字符串、结构、Todo 数量与框架关键入口检查，全部返回 True。
- [x] **通过**：`AutoDoc/CleanupTempDocs.bat` 已在复核后唯一执行一次，退出码 0；根级临时 Markdown 数量 22 未达到清理阈值，未删除文件。
- [x] **通过**：清理后已创建 `2026-08-14-CoreBattleSystem-Plan-Report.md`，记录结果、证据、偏差、风险和清理结果。
