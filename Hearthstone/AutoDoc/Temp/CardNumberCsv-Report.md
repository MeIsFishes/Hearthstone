# 卡牌编号与双 CSV 配置任务报告

## 1. 任务结果

已完成 98 张编号卡牌、五种卡牌种类范围配置、运行时属性生成、左上角编号 UI 与灰色六边形编号底框资源接入。

- 卡牌编号为连续唯一的 `1~98`。
- 五种卡牌数量为 `20/20/20/19/19`。
- 食人魔从 `35~98` 候选范围抽取 19 个离散编号，实际平均编号 `70.42`，不独占最大号段。
- 食人魔生命范围 `6~8`，攻击范围 `5~7`；区间中值分别为其他四种卡平均值的 `1.556` 与 `1.5` 倍。
- 编号显示在卡面左上角，使用独立深灰金属六边形 Sprite 与白色粗体 TMP 数字。

## 2. 主要产物

### 2.1 配置与代码

- `Assets/Resources/Config/BattleCardTypeCsvData.csv`：五种卡的名称、生命范围、攻击范围。
- `Assets/Resources/Config/BattleCardCsvData.csv`：98 个编号、种类 ID 与立绘资源键。
- `Assets/Scripts/Hearthstone/Config/Csv/BattleCardTypeCsvData.cs`：种类表解析、范围校验与整数属性生成。
- `Assets/Scripts/Hearthstone/Config/Csv/BattleCardCsvData.cs`：编号表解析与编号/种类/立绘字段校验。
- `BattleCardRawComponent`、`BattleStages`、`BattleRules`：编号、种类与范围随机生成链路。
- `BattleCardItemController`：编号显示、六边形 Sprite 加载与 UI 条目复用。
- `BattleRulesTests`：双表、98 编号分布、食人魔偏大/约 1.5 倍属性、运行时状态与资源加载验证。

### 2.2 美术资源

- `Assets/Resources/Art/BattleCards/UI/CardNumberBadgeHex.png`
- 尺寸：`384 × 256`，`3:2`，`Format32bppArgb`。
- Alpha：四角均为 `0`，背景透明。
- 视觉：横向六边形、深灰石墨填充、浅灰金属双层描边、中心无文字并保留两位数字空间。
- 生成方式：内置 imagegen；生成后仅做高质量等比缩放并保存到项目资源目录。
- 最终提示词摘要：为 Unity 卡牌左上角制作透明背景的横向六边形编号底框，深灰填充、浅灰金属描边、中心留白，无文字、数字、外投影、辉光、水印、卡面或附加图标。

## 3. 检查项结果与证据

全部检查项通过。详细逐项证据见 `AutoDoc/Temp/CardNumberCsv-Checklist.md`。

- 数据框架：两表继承 `CsvDataBase<T>`，经默认数据组加载并用 `DataApi` 按整数键登记。
- 资源框架：所有 Sprite 通过 `ResourceApi.LoadSprite` 读取；`ResourcesDictionary.json` 由 Unity 的 `Tools/Build Resources Dictionary` 菜单生成。
- UI 生命周期：编号对象在条目首次绑定时创建并缓存，解绑隐藏、换绑更新，没有每次刷新重复分配。
- 资源引用：五张怪物立绘和 `CardNumberBadgeHex` 均能从资源字典加载。
- CSV 规范：两表均有逐列英文说明和双向 `Associated` 注释。
- 文档：玩家视角战斗文档、战斗卡牌美术模块、UI 美术总览、战斗程序文档与战斗 UI 程序文档均已同步当前实现。
- 框架边界：未手写 Prefab/Scene/Asset YAML，未建立平行 CSV/资源加载器，未绕过 DataApi、ResourceApi、ECS 或 UI Controller 生命周期。

## 4. 验证结果

- Unity MCP：`manage_scene(get_active)` 成功，活动场景为 `Assets/Scenes/Main.unity`；`read_console(types=[error])` 可用。
- Unity 编译：刷新并编译完成，无 `CS` 编译错误。
- 本次相关 EditMode 测试：`5/5` 通过。
- 六边形资源加载测试：`1/1` 通过。
- 静态 CSV 校验：98 行编号、5 行种类；编号无缺失、无重复；类型计数 `20/20/20/19/19`；立绘缺失数 0。
- 食人魔编号：`40,44,45,49,59,60,66,67,68,72,74,76,78,79,86,91,92,95,97`，均在 `35~98` 内；该区间同时包含 45 个非食人魔编号。
- 资源静态校验：六边形 PNG 为 `384 × 256` ARGB，四角 Alpha `0,0,0,0`；资源字典包含种类表与六边形资源键。
- 按用户要求未进入 Play Mode、未做游戏内视觉验证。

## 5. 偏差与修正

- 初版思路曾让食人魔占用连续最大号段；按用户反馈改为从高号候选范围离散抽取，随后又将候选范围调整为 `35~98`。
- 初次 PowerShell `Import-Csv` 校验把 CSV 规范注释行当作数据；已改为先过滤 `//` 行再统计，最终证据使用修正后的结果。
- 六边形后续要求以资源制作为主；由于原编号底框此前是运行时代码生成的纯色 Image，为使新资源实际生效，仅在既有 Controller 中增加资源键加载与比例设置，没有改玩法逻辑、Prefab 或场景。

## 6. 未解决风险

- 未进行 Play Mode 视觉验收，因此六边形在实际卡面上的最终观感、数字字号与遮挡关系尚未在运行画面确认；这是遵循用户“不用进游戏测”的要求。
- 全量 EditMode 测试为 `13/14`：唯一失败是既有 `BattleFontContainsCurrentChineseInterfaceCharacters` 字体字形覆盖测试，与本次数字编号和六边形资源无直接关联。本次相关测试全部通过。
- Console 保留一条 Test Runner 写出 `TestResults.xml` 路径的无堆栈 Exception 级日志；未发现 `CS` 编译错误。

## 7. 文档处理与清理

- 已更新：
  - `AutoDoc/Design/Specific/combat-system/combat-system.md`
  - `AutoDoc/Art/Modules/battle-card/battle-card.md`
  - `AutoDoc/Art/UI/ui-art-overview.md`
  - `AutoDoc/Program/Specific/combat-system/combat-system.md`
  - `AutoDoc/Program/UI/battle/battle.md`
- 已且仅已执行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 0。
- 清理前后 `AutoDoc/Temp/` Markdown 数量为 `50 → 50`，未达到删除阈值，没有文件被清理。
