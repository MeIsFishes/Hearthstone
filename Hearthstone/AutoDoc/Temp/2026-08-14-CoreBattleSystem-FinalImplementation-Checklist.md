# 核心战斗系统最终实施检查清单

## 输入与范围

- [x] **通过**：以 `AutoDoc/Temp/Plan/2026-08-14-CoreBattleSystem-Plan.md` 为唯一普通方案输入。
- [x] **通过**：`BattleRules` 固定双方各 3 张、5 血 3 攻；会话初始化固定我方先手。
- [x] **通过**：独立攻击游标使用原始槽位环回，`FindNextLivingSlot` 按存活位掩码跳过死亡牌。
- [x] **通过**：System 只从目标方存活位掩码等概率选取序号，并调用同时伤害规则。
- [x] **通过**：`EvaluateResult` 先检查敌方空场，因此双方同时空场判我方胜利；枚举无 Draw。
- [x] **通过**：源码仅实现纯自动战斗与最小占位 UI；未加入技能、英雄、酒馆、音频或正式美术。

## 源码 Todo

- [x] **通过**：已新增 `BattleCardRawComponent`、`BattleSessionSingletonRawComponent`、`EBattleSide`、`EBattleResult`，监听变量回收前均调用 `MakeInvalid()`。
- [x] **通过**：已新增确定性 `BattleRules`，覆盖游标、活体选择、同时伤害、胜负与终局门控。
- [x] **通过**：已新增 `[DisableAutoCreation] BattleSystem`，倒计时到期每帧最多结算一次，终局后直接返回。
- [x] **通过**：已新增 `BattleRulesTests` 共 8 个测试；实际规则文件另经临时 .NET Harness 执行，输出 `CoreBattleHarness=PASS`，生成物随后清除。
- [x] **通过**：已新增 `BattleView` / `BattleController` 与 `BattleCardItemView` / `BattleCardItemController`，数据来自 ECS `ListenableVariable`。
- [x] **通过**：已新增 `BattleUiScene` / `EBattleUiGroup`。
- [x] **通过**：已新增 `BattleStages` 与 `InitializeBattleRuntime`，用公开 `EcsApi` 成对创建/销毁会话和 6 个卡牌 Entity。
- [x] **通过**：`HearthstoneGameEngine` 已注册 BattleSystem 顺序并默认进入 `EnterBattleStageGroup()`。
- [x] **通过**：Placeholder 源码和资产未删除，只从 GameEngine 启动链路停用；未创建、编辑或删除 `.meta`。

## Unity MCP 与资产 Todo

- [x] **通过**：已记录工具、`codex mcp list/get`、Unity 进程、manifest/lock、uv/uvx、官方 CI 启动参数与专用 Editor 日志证据；包与 Server 均为 10.0.0。
- [ ] **未通过**：标准 MCP SDK 只读探针已完成 initialize、46 项 tools/list、活动 `Main` 场景与 0 条 Console error；但本次 Codex 会话工具表仍未暴露 `unityMCP`，必须重载会话后才能执行写操作。
- [ ] **未通过**：未创建 `BattleView.prefab` 与 `BattleCardItemView.prefab`；恢复 skill 禁止用临时 SDK 写操作绕过会话工具边界。
- [ ] **未通过**：未执行 `BattleCardItemView` 的框架预加载导出与公开映射校验。
- [ ] **未通过**：未创建 `Assets/Scenes/Ui/Battle.unity`，也未配置 `UiSceneExporter` 与 Prefab 连接。
- [ ] **未通过**：未由 Editor 导出 `Assets/Resources/Ui/Battle.asset`，未刷新资源字典。
- [ ] **未通过**：`BattleStages` 已写好正式加载路径，但由于导出 Asset 尚不存在，完整 UiScene 注册链路尚未闭环。

## 框架边界与质量

- [x] **通过**：新增业务代码只使用 `EcsApi`/Entity 扩展、`ModelWrapper`/`UiList`、`IStageLoad` 与 GameStage 公开入口；扫描无底层 Manager 访问。
- [x] **通过**：两个 View 仅有 public 组件引用与 `GetControllerType()`；静态页面没有由 Controller 运行时拼装。
- [x] **通过**：动态条目监听在 `InitListeners()` 创建、`Bind(Entity)` 重绑、`OnUiClose()` 显式解绑，避免池化实例保留旧 Entity 监听。
- [x] **通过**：未新增一次性 Utils、平行 Model、配置抽象或手写 Unity 导出数据。
- [x] **通过**：规则函数均被 System/UI/Test 实际消费；未发现无用字段、无用抽象或新增死代码。
- [x] **通过**：未修改计划外既有业务文件；仅修改 GameEngine 与运行时 asmdef，并新增计划内源码/测试文件；无 Battle `.meta` 生成或人工处理。

## 验证与文档

- [x] **通过（降级验证）**：12 个脚本经 MCP `validate_script(level=standard)` 全部为 0 warning/0 error，核心规则 Harness 执行通过；Unity Test Runner 必须待会话重载与 Asset 刷新后补跑。
- [ ] **未通过**：当前 Editor 未自动刷新新文件，`Hearthstone.dll` 时间戳仍为改动前；因此不能把 Console 0 error 当成本次完整 Unity 编译证据。
- [x] **通过**：未进入 Play Mode，只执行只读场景/Console/脚本诊断。
- [ ] **未通过**：正式现状文档尚未更新；功能未形成可加载 UI 资产前不写成已完成现状。
- [x] **通过**：已重新读取并逐项审计；可修正的源码问题均已处理，剩余项均由当前会话未暴露 Unity 写工具阻塞。
- [x] **通过**：`AutoDoc/CleanupTempDocs.bat` 已在复核后唯一执行一次，退出码 0；根级临时 Markdown 数量 24 未达到清理阈值。
- [x] **通过**：清理后已创建 `2026-08-14-CoreBattleSystem-FinalImplementation-Report.md`，记录完成项、验证证据、偏差、阻塞与继续条件。
