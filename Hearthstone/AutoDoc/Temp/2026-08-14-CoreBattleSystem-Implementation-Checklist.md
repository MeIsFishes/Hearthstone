# 核心战斗系统实施检查清单

## 用户需求与范围

- [ ] 将规则更正为每张牌 5 点生命、3 点攻击。
- [ ] 进入战斗后双方各生成 3 张牌，我方先攻，敌我每次各行动一张牌并严格交替。
- [ ] 每方攻击者按原始槽位从左到右循环，死亡牌立即跳过。
- [ ] 攻击目标从对方当前存活牌中等概率随机选择。
- [ ] 攻击采用同时伤害；攻击完成后立即处理死亡并判定胜、负或平局。
- [ ] 完成可运行的最小战斗表现：两排卡牌、攻击/生命、攻击者/目标反馈、结果显示。
- [ ] 不加入酒馆、技能、关键词、英雄生命、玩家操作、正式美术、复杂动画或音频。

## 数据与 ECS

- [ ] 新增每张战斗卡牌的 `BattleCardRawComponent`，保存阵营、槽位、攻击、最大生命、当前生命、存活状态及 UI 可监听字段。
- [ ] 新增唯一 `BattleSessionSingletonRawComponent`，保存战斗阶段、当前行动阵营、双方攻击游标、随机状态、行动间隔和结果。
- [ ] 运行时状态仅保存在 ECS Component；System 与 UI 不保存平行权威状态。
- [ ] Component 回收时正确失效监听、清空引用与重置池化数据。
- [ ] 预设数值与节奏若无需外部策划配置，不额外建立一次性配置抽象；保持最小实现。

## 战斗逻辑

- [ ] 新增纯规则执行入口，确定下一攻击者、随机存活目标、同时伤害、死亡和胜负结算。
- [ ] 新增 `[DisableAutoCreation]` 的 `BattleSystem`，仅在会话初始化完成且战斗进行中按间隔推进一次攻击。
- [ ] 同一种子和初始状态产生相同目标序列；随机状态保存在会话 Component。
- [ ] 处理死亡攻击者跳过、槽位环回、单方空场、双方空场和战斗结束后停止更新。
- [ ] 没有复用价值的一次性逻辑留在所属类内，不创建无必要 Utils。

## Stage 与生命周期

- [ ] 新增 `BattleStages.CreateBattleStage`，不再使用 Placeholder 业务 Stage 承载战斗。
- [ ] 使用成对的 Stage LoadItem 创建/销毁战斗会话和 6 个卡牌 Entity。
- [ ] 处理当前 Stage 管线初始化时序，确保 System 在完整状态就绪前休眠。
- [ ] 在 `HearthstoneGameEngine` 中注册 `BattleSystem` 顺序并提供 `EnterBattleStageGroup` 入口。
- [ ] 战斗 Stage 的 System、UI、场景和初始化项全部显式归属，不绕过 `StageWrapper.SetActiveGameStage`。

## UI 与 Unity 资产

- [ ] 新增 `BattleView` / `BattleController` 与 `BattleCardItemView` / `BattleCardItemController`，View 只持序列化引用，Controller 监听 ECS。
- [ ] 双方卡牌列表使用现有 `UiList` 创建与回收条目，不在刷新循环中手动 `new GameObject` 或 `AddComponent`。
- [ ] 新增 `BattleUiScene` 与 `BattleUiGroup`，UiScene 只定义 Group。
- [ ] 创建完整 BattleView 和 BattleCardItem Prefab，静态层级与组件引用保存在 Prefab。
- [ ] 创建 UI 编辑场景，配置 `UiSceneExporter`、Prefab 连接和 Group，并导出可追溯的 `UiSceneAsset`。
- [ ] BattleStage 仅注册由 UI 编辑场景导出的 Asset，不手写 Scene/Prefab/`.asset` YAML。
- [x] 通过：将不可接入当前 Codex 的 Codely Bridge 完整移除并替换为 CoplayDev MCP for Unity v10.0.0；已同步更新 `AGENTS.md`，安装 `uv 0.12.4` 并加入用户 PATH，注册 `unityMCP`，标准 MCP 探针列出 46 个工具且成功读取活动场景 `Main`、Console 错误为 0。
- [ ] 不新增自定义 `BbxUiItem`；直接复用已有 `UiList`，因此无需修改 UIItem 文档。

## 测试与验证

- [ ] 新增规则自动测试，覆盖固定 5 血/3 攻、顺序轮转、死亡跳过、随机目标合法、确定性和最终结果。
- [ ] 检查新增 C# 的程序集引用、命名空间、框架 API 用法与编译风险。
- [ ] 若 Unity 工具可用，执行资产导出、编译/Console 检查和最小战斗运行验证；默认不额外进入游戏验收。
- [ ] 更新需求确认稿中 5/3 的错误解释及相关验收描述。
- [ ] 按项目现状同步直接相关程序与玩家视角文档；只记录已完成内容，不写未实现预期。

## 框架边界审计

- [ ] 业务代码只通过 `EcsApi`、`UiApi` 和 GameStage 公开入口，不直接访问底层 Manager/存储。
- [ ] 不复制 ECS/UI 生命周期、不手写导出资产、不用运行时拼装整页静态 UI。
- [ ] 若出现框架能力缺口，收敛为最小改动并按影响分级；大改动先向用户报告并取得许可。
- [ ] 结束前检查没有遗留 Placeholder 平行启动逻辑、重复状态源或无所有权对象。

## 结束审计

- [ ] 重新读取本清单，逐项标记通过、未通过或不适用并写证据。
- [ ] 只运行一次 `AutoDoc/CleanupTempDocs.bat` 并记录退出码。
- [ ] 清理后创建 `2026-08-14-CoreBattleSystem-Implementation-Report.md`，记录产物、验证、偏差、风险和清理结果。
