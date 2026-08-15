# 卡牌等阶边框与传奇动态编号任务报告

## 结果

任务已完成。卡牌边框不再依据敌我阵营着色，统一读取卡牌的运行时等阶；铜、银、金、传奇分别对应基础卡、2 卡融合、3 卡融合、4 卡融合。备战悬停仍只在备战阶段将同一边框临时高亮为黄色，战斗阶段不开放悬停和拖拽。

卡阶数据链为：卡牌种类配置提供初始默认值，`RunCardInstanceData` 保存本局实例值，融合系统按实际材料数覆盖实例卡阶，生成战斗 Entity 时复制到 `BattleCardRawComponent`，共享卡牌 UI 最终只消费实例或 Entity 的 `Tier`。因此 UI 与敌我阵营、融合配方和类型编号均已解耦。

## 数据与规则

- 卡牌种类表新增 `Tier` 字段和 `Bronze / Silver / Gold / Legendary` 枚举。
- 2、3、4 张材料融合分别生成银、金、传奇运行时实例；四卡融合使用全部四种材料，不再丢弃点数较低的一张。
- 增加 65 个合法四卡配方，内部卡牌 ID 为 149～213；内部 ID 继续作为存档、拥有状态、融合结果、拖放和战斗逻辑的稳定主键。
- 总览对传奇条目在过滤前按确定性顺序分配从 149 开始的展示编号。“查看拥有”只过滤条目，不重新编号。
- 配置机械检查结果：卡牌 213、种类 120、融合配方 114，其中双卡 15、三卡 34、四卡 65；重复配方 0、等阶不匹配 0、食人魔数量违规 0。

## 表现与资源

- 边框颜色：铜 `#B87333`、银 `#C0CCD8`、金 `#E7A93B`、传奇 `#B25CFF`。
- 备战悬停色保持 `#FFD230`，锁定态继续使用灰色覆盖。
- 四阶复用同一套中性卡牌边框素材，仅动态改变颜色；未新增图片资源或平行卡牌 UI。
- 备战 Prefab 已通过既有 UiBuilder 重建：31 行、末项内部编号 213、内容高度 9820.8、槽位高度 316.8。

## 验证

- Unity Editor 定向测试：`BattleRulesTests`、`RunCardRulesTests`、`BattleKeywordRulesTests` 共 60 项，60 通过、0 失败、0 跳过。
- `Hearthstone.csproj`、`Hearthstone.Ui.Editor.csproj`、`Hearthstone.Tests.csproj` 均编译成功，0 error；各自保留 8 条既有 warning。
- C#、CSV 与本次文档的定向 `git diff --check` 通过；Unity 自动序列化的 Prefab 仍包含空标量行产生的行尾空格提示，未手工改写序列化格式；未修改任何 `.meta` 文件。
- 未进入 Play Mode。Unity Console 中的无效/组合关键词报错来自负向解析测试的预期输入，测试套件仍全部通过。
- 玩家视角设计文档、美术模块/UI/风格文档、程序模块/UI 文档均已按当前实现同步。

## 清单审计

`CardTierFrameAndLegendaryNumbering-Checklist.md` 的全部检查项均已逐项复核通过。框架边界保持为 CsvData/DataApi → 运行时实例 → Battle Entity → 共享卡牌 UI，继续复用 UiList、对象池和 UiBuilder。

## 清理与偏差

- 按规定仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 0；清理前后临时文件数均为 186，没有可删除项。
- 工作区中原有的无关改动保持不动。
- 本次没有 Play Mode 视觉截图；四卡传奇名称与立绘采用系统化名称和现有基础立绘复用，后续如提供专属美术资源，可仅替换配置而不改运行时卡阶链路。
