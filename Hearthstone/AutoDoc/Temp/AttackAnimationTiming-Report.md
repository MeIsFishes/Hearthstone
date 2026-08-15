# 攻击动画时序调整报告

## 任务结果

已把攻击完整结束后的行动等待从 `0.75s` 延长到 `1.25s`，开战前的初始等待仍为 `0.75s`。攻击表现新增统一的 `0.8` 倍播放速度：权威表现时钟按每帧真实时间的 `0.8` 倍推进，因此攻击前拱、八帧特效、音效延迟、闪红延迟、伤害时点和表现结束判定同步减速，真实播放时长统一变为原来的 `1.25` 倍。

## 检查项结果与证据

- 通过：攻击后等待延长 `0.5s`。`BattleRules.AttackEndWaitDuration` 定义为 `ActionInterval + 0.5f`，且只在攻击完成路径写入下一次行动倒计时。
- 通过：整体 `0.8` 倍速。`BattleRules.AttackPresentationPlaybackSpeed` 为 `0.8f`，`BattleSystem` 在共享攻击时间轴源头乘用该系数。
- 通过：反馈同步。音效和伤害触发在 `BattleSystem` 中比较共享 `AttackPresentationElapsed`；前拱、图集和闪红在 `BattleCardItemController` 中消费同一时钟，没有建立分离时间轴。
- 通过：框架边界。继续使用既有 ECS System、会话 RawComponent、`AudioApi`、CSV 配置与 UI Controller，没有新增平行 System、管理器、组件或资源流程。
- 通过：抽象审计。仅新增两个有明确全局语义的战斗时序常量，没有新增一次性函数或包装层。
- 通过：范围保护。代码修改限定在 `BattleRules.cs`、`BattleSystem.cs` 和 `BattleRulesTests.cs`；没有覆盖 `BattleCardItemController.cs` 中既有工作区改动，没有创建、编辑或删除 `.meta`。
- 通过：玩家设计与程序文档同步。战斗设计、战斗系统程序和战斗 UI 程序文档均已反映当前时序。
- 不适用：美术文档。本次未改变攻击图集帧数、资产规格、构图、颜色、布局或资源清单。

## 验证结果

- `dotnet build Hearthstone.csproj --no-restore`：通过，0 错误；8 条既有程序集版本冲突警告。
- `dotnet build Hearthstone.Tests.csproj --no-restore`：通过，0 错误；8 条既有程序集版本冲突警告。
- `BattleRulesTests` 新增 `AttackPresentationUsesSharedPlaybackSpeedAndExtendedEndWait`，固定 `0.8` 倍速与 `+0.5s` 关系；测试程序集已编译。
- 定向 `git diff --check`：通过。
- 静态调用链核对：开战初始化仍使用 `ActionInterval = 0.75s`；攻击完成使用 `AttackEndWaitDuration = 1.25s`；共享表现时钟唯一推进点乘用 `0.8` 系数。

## 文档处理

- 已更新：`AutoDoc/Design/Specific/combat-system/combat-system.md`。
- 已更新：`AutoDoc/Program/Specific/combat-system/combat-system.md`。
- 已更新：`AutoDoc/Program/UI/battle/battle.md`。
- 已核对且不修改：`AutoDoc/Art/Modules/battle-card/battle-card.md`。

## 执行偏差与未解决风险

没有实现偏差。项目已有 Unity Editor 正在运行，按项目默认规则未进入游戏，也没有另启会与现有 Editor 冲突的批处理测试；因此本轮验证覆盖编译、测试程序集和静态时序链，未包含实际画面与听感验收。主观节奏仍可在下次正常战斗中观察。

## 清理结果

结束审计后仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 `0`；随后创建本报告。清理脚本保留了本任务检查清单，未处理工作区其他任务文件。
