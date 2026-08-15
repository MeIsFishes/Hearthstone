# 四卡融合表现与智能推荐调整报告

## 任务结果

已完成。四卡融合继续使用完整四类型公式生成独立四卡结果，并累计四张素材的攻击、最大生命和词条，运行时等阶保持传奇；卡面原画、名称、战斗帧动画、音效和受击时点改为使用实际素材中点数最高三张所对应的三卡融合版本。智能推荐窗口只展示 2～4 张素材卡，不展示融合结果卡。

## 实现摘要

- `FusionEvaluationData` 增加独立的表现来源卡号。四卡评估保留素材卡号与类型配对，按点数排序后丢弃最低点数素材，用其余三张类型查询三卡表现来源；完整四类型仍用于查询实际结果。
- `RunCardInstanceData` 持久保存 `PresentationCardNumber`，普通卡、双卡和三卡默认等于自身卡号，四卡融合提交时写入动态三卡来源。
- `BattleCardRawComponent` 同时保存实际卡号/类型和表现卡号/类型。备战卡面、融合揭晓、战斗卡面与战斗攻击表现读取表现身份；编号、攻血、词条、等阶和持有身份读取实际四卡实例。
- 推荐条目继续只复用四个素材卡位；View、Builder 和 Prefab 均无结果卡节点，结果卡号只保留在推荐数据中供选择时重新校验。
- 新增 Editor 测试覆盖相同四卡公式因实际最高三张不同而选择不同三卡表现，以及四卡结果、攻血与传奇等阶不变；补充推荐 Prefab 无结果卡节点断言。

## 检查项结果与证据

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 最高点数三张决定四卡表现 | 通过 | CSV 核验：`2,4,8,85 → 结果177/表现127`；`4,7,8,80 → 结果177/表现143`；`14,20,30,35 → 结果184/表现131`。同一四卡结果 177 可按实际素材选择不同三卡表现。 |
| 四卡效果与面板保留 | 通过 | `TryFuse()` 仍累计全部素材攻血与词条，实际结果号仍由四类型公式确定，四素材写入 Legendary 等阶；测试断言四卡攻血和结果身份。 |
| 配置兼容 | 通过 | 未新增无法表达实际点数差异的静态四卡表现映射；现有四卡配置只承担结果公式身份，现有三卡配置承担动态表现。 |
| 智能推荐不输出结果卡 | 通过 | 推荐 Controller 只绑定素材卡号；View/Builder/Prefab 无结果卡字段或节点，并有 Editor 断言。 |
| 普通融合回归 | 通过 | 非四卡路径直接令表现卡号等于实际结果卡号；现有构造调用默认表现等于自身，保持向后兼容。 |
| 框架边界 | 通过 | 继续使用 `DataApi`、`ResourceApi`、Run state、ECS Component 与 BbxCommon UI 对象池；无平行配置、手写资源索引或绕过生命周期。 |
| 玩家视角设计文档 | 通过 | 已更新备战卡池与战斗系统设计文档，记录动态三卡表现和推荐只显示素材。 |
| 美术文档 | 通过 | 已更新战斗卡牌与备战卡池模块文档；本次未新增或改动位图，UI 美术总览不受影响。 |
| 程序文档 | 通过 | 已更新备战玩法、战斗玩法、备战 UI 与战斗 UI 文档，记录实际身份/表现身份分离和消费链路。 |

## 验证结果

- `dotnet build Hearthstone.Tests.csproj --no-restore`：执行两次，均为 0 错误；末次为 8 个既有程序集版本冲突警告。
- 定向 `git diff --check`：通过，无本任务文件空白错误。
- 静态检索：战斗与共享卡面表现消费者均改读 `PresentationCardNumber` / `PresentationCardTypeId`；推荐条目代码和 Prefab 均无结果卡输出。
- Unity Editor Test Runner：未执行。项目当前由 Unity Editor 进程 PID 23324 占用，且项目说明默认不进入游戏验证；新增 Editor 测试已由测试工程编译覆盖。

## 文档处理

- 玩家视角：更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`、`AutoDoc/Design/Specific/combat-system/combat-system.md`。
- 美术：更新 `AutoDoc/Art/Modules/battle-card/battle-card.md`、`AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md`。
- 程序：更新 `AutoDoc/Program/Specific/preparation-card-pool/preparation-card-pool.md`、`AutoDoc/Program/Specific/combat-system/combat-system.md`、`AutoDoc/Program/UI/preparation/preparation.md`、`AutoDoc/Program/UI/battle/battle.md`。

## 偏差与未解决风险

- 未直接修改四卡 CSV 的静态名称、原画复用项或攻击表现列；实际融合实例已不把这些四卡行作为权威表现来源。这样避免用静态配置错误表达依赖实际素材点数的动态规则。
- 未运行 Unity Test Runner 或进入游戏验证。编译、静态数据核验和新增用例源码均通过；实际卡面与攻击动画的视觉串联仍可在当前已打开的 Unity Editor 中后续手动观察。
- 工作区在任务开始前已有大量用户未提交修改和未跟踪资产；本任务未回退或整理这些内容，只对直接相关文件做局部补丁。

## 清理结果

仅执行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 0。执行后 `AutoDoc/Temp/` 有 214 份 Markdown，未达到脚本的 500 份清理阈值，因此没有删除文件。本报告在清理完成后创建。
