# 卡牌融合公式与备战栏改造报告

## 结果

- 融合结果卡使用 100～148，共 49 张独立卡牌类型；99 独立作为不可交互的锁位。
- 配方只包含基础类型的无序二重组合和三重组合：15 条二卡配方、34 条三卡配方。三巨魔 `5;5;5` 被排除，未保留三巨魔或四巨魔槽位。
- 四张素材被选择时，以卡牌编号最高的三张确定配方；四张仍全部参与攻击、生命、词条结算和消费。
- 融合卡类型表的攻击、生命与初始词条为空，运行时实例完全来自材料结算。
- 99 号使用新的带锁铁门卡图；100 号以后卡位在扩展至 22 行的备战卡池中可达。

## 需求与实现证据

- `BattleCardCsvData.csv` 新增 `FusionRecipeTypeIds`，以分号表示排序后的 `List<int>` 基础类型 ID；100～148 各有唯一配方。
- `BattleCardCsvData` 在 CSV 读取阶段校验配方并登记到 `DataApi` 的负整数键；运行时通过规范化组合键 O(1) 查询，不区分输入排列。
- `RunCardRules` 维护最高三个卡号以处理四卡配方，融合成功后按结果卡号创建实例，并消费全部已选素材。
- `BattleCardItemController` 为 99 建立专用锁定显示和禁用交互路径；`PreparationController` 将卡池内容高度同步到 22 行并显示实际配方结果。
- 用户给出的七个命名公式均已保留；其余配方已补齐并赋予独立名称。

## 检查清单状态

检查清单中的需求、数据、代码质量、兼容性、UI/资源、验证、框架边界、变更范围与三类文档项均为通过。原始“补全融合组合”范围由用户后续明确收窄为只保留二卡、三卡结果；最终实现以该补充要求为准。

## 验证

- `dotnet build Hearthstone.Tests.csproj --no-restore -v:q -clp:ErrorsOnly`：成功，0 错误；存在 8 个项目原有的程序集版本冲突警告。
- CSV 校验：`FusionRows=49`、`PairRows=15`、`TripleRows=34`、`DuplicateRecipes=0`、`UnsortedRecipes=0`、`TripleOgre=0`、`MissingTypes=0`、`RuntimeStatRowsNonBlank=0`。
- 定向 `git diff --check`：本次代码、CSV、测试和正式文档未发现空白错误。
- Unity Editor 已处于打开状态并完成自动刷新，最新日志尾部未发现 C# 编译失败或编译错误。
- 按项目默认未进入 Play Mode；为避免与已打开的 Editor 冲突，没有另起批处理 Unity Test Runner。因此玩家实际拖拽、滚动和融合演出仍需后续人工游戏内验收。

## 文档同步

- 玩家文档：`AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`、`AutoDoc/Design/Specific/combat-system/combat-system.md`。
- 美术文档：`AutoDoc/Art/Modules/battle-card/battle-card.md`、`AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md`、`AutoDoc/Art/UI/ui-art-overview.md`。
- 程序文档：`AutoDoc/Program/Specific/preparation-card-pool/preparation-card-pool.md`、`AutoDoc/Program/UI/preparation/preparation.md`、`AutoDoc/Program/Specific/combat-system/combat-system.md`。

## 美术资源

- 使用内置 `imagegen` 编辑原 `FusionCard_099.png`，保留既有路径、资源 key 和 `.meta`。
- 生成提示概要：暗黑奇幻卡牌插画；中央为大型锻铁挂锁与交叉锁链，配红色封印和克制的熔炉橙光；无角色、文字、UI 或水印。
- 100～148 当前复用既有五类基础卡牌图作为占位表现，没有生成 49 张独立融合插画；这不影响配方、卡牌类型和运行时结算，但仍是后续美术完善项。

## 偏差与风险

- “点数”按项目现有卡牌编号解释；如果策划定义的点数并非 `CardNumber`，需要提供独立字段后再切换比较依据。
- 工作区在任务开始前已有大量未提交修改，且执行期间出现了并行的融合揭示 UI 与攻击表现修改。本次仅做增量适配，没有回退这些内容，也未创建、修改或删除任何 `.meta`。

## 清理

`AutoDoc/CleanupTempDocs.bat` 在结束审计后只执行一次，退出码为 0。
