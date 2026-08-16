# 敌人阵容配置与回合规则修改报告

## 任务结果

本任务已完成。项目新增按关卡配置的敌方阵容 CSV；同一关卡的多行阵容会等概率随机选择整行。基础敌卡按卡型基础攻防范围随机，融合敌卡先随机生成符合目标配方的不同基础卡实例，再复用玩家融合的攻防求和、词条并集、等阶和展示卡逻辑生成最终实例。玩家抽卡调整为首回合 3 张、后续每回合 5 张，最终槽位依次为 2、3、4、5、6。

## 检查项状态与证据

- [通过] 两列敌阵配置：`EnemyLineupCsvData.csv` 表头严格为 `BattleNumber,CardNumbers`，卡号用分号分隔。
- [通过] 多阵容随机：匿名数据保留重复关卡行，`GetRandomRequired` 使用蓄水池抽样，随机结果始终是一套完整阵容。
- [通过] 基础卡生成：`EnemyCardFactory` 复用 `BattleCardTypeCsvData.RollAttack/RollHealth`。
- [通过] 融合卡生成：按 `FusionRecipeTypeIds` 随机选择不同基础材料，并复用 `RunCardRules.TryCreateFusionResultInstance`；玩家融合也改用同一函数。
- [通过] 五关数据：静态交叉校验确认共 15 行、每关 3 行，每行素材数模式分别为 `1,1`、`1,1,1`、`1,2,2,1`、`1,3,2,2,2`、`1,3,3,3,2,1`。
- [通过] 抽卡与槽位：`BattleProgressionCsvData.csv` 为 `1,2,3`、`2,1,5`、`3,1,5`、`4,1,5`、`5,1,5`，累计槽位为 `2,3,4,5,6`。
- [通过] 动态战斗阵容：敌方实体数组按所选阵容长度创建；战斗目标、攻击游标、相邻伤害及 UI 继续使用实际数组长度。
- [通过] 数据边界：关卡号、阵容长度、卡号范围和锁定分隔卡均有校验；缺少关卡或卡牌配置时抛出明确异常。
- [通过] 回归覆盖：新增重复关卡整行随机、融合敌卡属性/词条、运行时 15 行配置与素材数模式测试，并更新首回合进度断言。
- [通过] 文档同步：更新战斗系统与备战卡池的玩家视角设计文档、程序文档；无新美术资产，仅同步相关美术文档中的二至六张布局现状。
- [通过（受限偏差）] 资源流程：运行时仍通过项目资源 API 与 DataApi。为避免主动触发 Unity 导入并改动 `.meta`，未运行编辑器导出器；资源字典按现有格式补入唯一映射，JSON、键唯一性和路径均已校验。
- [通过] 无关改动：保留脏工作树中的既有修改，未创建、编辑或删除 `.meta`。收尾时观察到后台 Unity 在 `2026-08-16 10:18:35` 生成了未跟踪的 `EnemyLineupCsvData.csv.meta`，本任务未触碰该文件。

## 验证结果

- 静态数据交叉校验：通过，`rows=15`、`battles=5`、`variantsPerBattle=3`。
- 进度序列校验：通过，最终槽位 `2,3,4,5,6`，抽卡 `3,5,5,5,5`。
- `ResourcesDictionary.json`：可解析，`EnemyLineupCsvData -> Config/EnemyLineupCsvData` 映射恰好一条。
- `git diff --check`：通过，无空白错误。
- `dotnet build Hearthstone.csproj --no-restore`：通过，0 错误、8 条项目既有程序集版本警告。
- `dotnet build Hearthstone.Tests.csproj --no-restore`：通过，0 错误、8 条相同警告。
- `dotnet build Hearthstone.sln --no-restore`：未通过，现有解决方案包含两个同名 `Hearthstone` 项目并触发 `MSB5004`；改用两个直接项目编译完成验证。
- `dotnet test Hearthstone.Tests.csproj --no-build --no-restore`：退出码 0，但 Unity 测试程序集未由该命令发现或执行，因此不计为已运行测试。
- Unity EditMode/PlayMode 与游戏内验证：未运行，遵循项目默认不进入游戏验证及不主动改动 `.meta` 的要求。

## 偏差与未解决风险

- 新增测试已经编译，但尚未在 Unity Test Runner 中实际执行；运行期资源导入和完整五关实战仍需后续在允许 Unity 导入的环境中验证。
- 后台生成的未跟踪 CSV `.meta` 保持原状，需由用户按当前工作树策略决定是否纳入版本控制。

## 临时文档清理

结束审计后仅执行一次 `AutoDoc/CleanupTempDocs.bat`。退出码为 0；清理前后 Markdown 文件数均为 252，未达到 500 个文件的清理阈值，因此没有删除文件。
