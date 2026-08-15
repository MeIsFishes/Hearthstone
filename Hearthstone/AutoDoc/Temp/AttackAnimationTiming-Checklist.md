# 攻击动画时序调整检查清单

- [通过] 用户要求：攻击结束后的等待时间在当前基础上延长 `0.5` 秒。
  - 证据：新增 `AttackEndWaitDuration = ActionInterval + 0.5f`，攻击完成路径使用该值；原初始 `ActionInterval = 0.75f` 保持不变，因此只把攻击后等待改为 `1.25s`。
- [通过] 用户要求：为攻击动画播放增加统一速度系数，并将当前整体速度设为 `0.8` 倍速。
  - 证据：新增 `AttackPresentationPlaybackSpeed = 0.8f`；攻击表现时钟按 `TimeApi.DeltaTime * AttackPresentationPlaybackSpeed` 推进。
- [通过] 统一系数必须同步影响攻击主体动画、音效延迟、闪红延迟及同一攻击反馈链中的其他相关时序，避免事件错位。
  - 证据：`BattleSystem` 的音效与受击/伤害判断，以及 `BattleCardItemController` 的前拱、八帧图集和闪红均消费同一 `AttackPresentationElapsed`；系数施加在该共享时钟源头，没有分别缩放各调用方。
- [通过] 定位攻击动画时间轴、结束等待、音效、闪红与调用方，确认时间单位和现有配置来源。
  - 证据：时间轴和结束等待位于 `BattleSystem`；常量与完整表现时长位于 `BattleRules`；音效/受击延迟来自 `BattleCardTypeCsvData`；UI 表现位于 `BattleCardItemController`。
- [通过] 修改应复用项目现有框架、公开 API 和生命周期，不建立平行动画系统或兼容补丁。
  - 证据：已完整读取 `bbxcommon-ecs`；修改保留既有 `EcsMixSystemBase` 更新入口、会话 RawComponent 和 UI 消费链，没有新增组件、System、监听或管理器。
- [通过] 检查新增字段、函数或抽象确有复用价值，删除不必要的一次性抽象。
  - 证据：只新增两个集中表达全局时序策略的 `BattleRules` 常量；没有新增一次性函数、组件或包装层。
- [通过] 补充或更新与时序换算直接相关的测试，并执行适当的静态检查、编译或自动化测试。
  - 证据：`BattleRulesTests.AttackPresentationUsesSharedPlaybackSpeedAndExtendedEndWait` 固定 `0.8` 倍速和 `+0.5s` 关系；`Hearthstone.csproj` 与 `Hearthstone.Tests.csproj` 顺序编译均为 0 错误、8 条既有程序集版本冲突警告；定向 `git diff --check` 通过。项目已有 Unity Editor 正在运行，按默认规则未进入游戏或另启批处理测试。
- [通过] 检查修改范围、直接依赖和回归风险，不误改或回退工作区其他既有改动，不创建、编辑或删除任何 `.meta` 文件。
  - 证据：本任务代码差异仅覆盖 `BattleRules.cs`、`BattleSystem.cs`、`BattleRulesTests.cs`；没有修改含既有并行改动的 `BattleCardItemController.cs`，没有触碰任何 `.meta`。
- [通过] 框架边界审计：确认实现仍通过现有攻击表现/Task/动画框架驱动，不绕过内部管理器、编辑器配置源或导出流程。
  - 证据：继续由 `BattleSystem` 推进权威会话时间轴，音效继续走 `AudioApi`，UI 继续读取会话时钟与 CSV 配置；无平行时间轴、手写资源或内部管理器访问。
- [通过] 玩家视角设计文档：读取基础及适用专项 skill，核对现有相关文档、玩家可见节奏变化、处理结论、目标路径与证据。
  - 证据：已完整读取 `design-doc-format` 与战斗系统专项格式；已更新 `AutoDoc/Design/Specific/combat-system/combat-system.md`，记录开战 `0.75s`、攻击后 `1.25s`、整体 `0.8` 倍速及反馈同步。
- [不适用] 美术文档：读取基础及适用专项 skill，核对现有相关文档、视觉反馈节奏变化、处理结论、目标路径与证据。
  - 证据：已完整读取 `art-doc-writer` 并核对 `AutoDoc/Art/Modules/battle-card/battle-card.md`；本次没有改变图集帧数、资产规格、构图、颜色、布局或资源清单，播放节奏属于运行逻辑，不改美术规格文档。
- [通过] 程序文档：读取基础及适用专项 skill，核对现有相关文档、攻击时序实现变化、处理结论、目标路径与证据。
  - 证据：已完整读取 `program-doc-format`、战斗系统专项格式和 UI 界面格式；已更新 `AutoDoc/Program/Specific/combat-system/combat-system.md` 与 `AutoDoc/Program/UI/battle/battle.md`，记录共享时钟、速度系数和攻击后等待。
- [通过] 结束前重新读取本清单，逐项标记通过、未通过或不适用并记录证据。
  - 证据：所有实现、验证、范围、框架和文档项已写入直接证据；清理与报告在本次复核后按规定执行。

复核完成后只运行一次 `AutoDoc/CleanupTempDocs.bat`，随后创建同名报告。
