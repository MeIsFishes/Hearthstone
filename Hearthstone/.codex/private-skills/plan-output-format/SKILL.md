---
name: plan-output-format
description: 规定游戏模块 Plan 的结构、编号、输出路径和复核；编写 Plan 时使用。
---

# Plan 模式输出格式

本文档**约束 Plan 模式下的输出结构**：设计或评审游戏模块时，按下列**顺序**组织内容。如果是游戏模块之外的方案设计，无需参考本文档结构。

**如果修改的是底层实现，则不需要按照该文档输出**。

## 输出位置

- 普通方案的 plan 必须写成 Markdown 文档，保存到项目根目录下的 `AutoDoc/Temp/Plan/` 目录。
- 实现正式策划案时，以 `project-state-preflight` 的策划案实现流程为准，将逐篇实施 Plan 保存到 `AutoDoc/DesignPlan/Plan/<document-name>-plan.md`，不得再写入 `AutoDoc/Temp/Plan/`。
- 对话框中只需要告知用户 plan 文档路径，并可简要说明 plan 已按本文档结构完成；不要只在对话框中输出完整 plan 而不落盘。

## 编号规则

- **按顺序**输出下面各大块。
- **若某条、某小节在本方案中无变动或无新增**，则**整段略过**，不写空节。
- **标题序号**：略去节后，**余下标题从 1 起连续编号**（不占空洞）。例如：若没有「新增 CsvData」，则下一条「新增 ScriptableObject」在输出中记为 **1.2.1**（而非固定保留 1.2.2 的空位）。大节同理：若整章无内容可写，则该大节不出现，后面大节前移编号。

以下给出**完整纲目**；实际输出时按上述规则裁剪编号。
1. 需求明确
1.1 需求对齐：将需求拆解成若干点，用序号依次列出来。
2. 数据部分
2.1 涉及到的数据概览
2.2 新增数据列表：具体哪些数据作为配置项，哪些数据是运行时数据；
2.2.1 新增Component类：表格形式，包含类名、重要字段、归属哪种Entity
2.2.2 新增CsvData类：表格形式，包含类名、重要字段
2.2.3 新增ScriptableObject：表格形式，包含类名、重要字段
2.3 原有数据类新增/删除字段
2.3.1 原有Component类新增/删除字段：表格形式
2.3.2 原有CsvData类新增/删除字段：表格形式
2.3.3 原有ScriptableObject类新增/删除字段：表格形式
3. 游戏逻辑部分
3.1 涉及到的游戏逻辑概览
3.2 新增System、StageListener
3.2.1 新增System类：表格形式，包含类名、职责
3.2.2 新增StageListener类：表格形式，包含类名、职责
3.3 原有逻辑类改动
3.3.1 原有System逻辑改动：表格形式，包含类名、改动方向
3.3.2 原有StageListener逻辑改动：表格形式，包含类名、改动方向
4. UI部分
4.1 涉及到的UI部分概览
4.2 新增Ui/Hud：表格形式，2张表格。表格1：View类名、对应页面、主要控件列表；表格2：Controller类名、数据监听来源（Component或配置项的对应类名）和监听响应行为
4.3 原有Ui/Hud改动：表格形式，2张表格。表格1：View类名、对应页面、新增或删除控件；表格2：Controller类名、数据监听改动
4.4 UiScene配置与导出
4.4.1 新增UiScene：表格形式，包含UI编辑场景路径、UiScene类与UiGroup枚举、Group列表、纳入的View Prefab、`UiSceneExporter.FullUiGroupType`、导出Asset路径、所属GameStage
4.4.2 原有UiScene改动：表格形式，包含UI编辑场景路径、修改的Group或Prefab归属、导出Asset路径、需要重新导出的原因、受影响GameStage
4.4.3 UiScene完整性检查：逐项确认View Prefab、UI编辑场景、`UiSceneExporter`、Prefab连接、导出Asset、GameEngine引用和GameStage注册；缺少编辑场景或无法重新导出的Asset必须列为待整改，禁止把手写Asset列为完成项
5. 美术部分
5.1 涉及到的美术表现概览
5.2 美术资产完整性检查：表格形式，包含资产或资产组、用途、候选已有资产及路径、复用结论、判断依据、缺失或不满足需求的内容、处理方式
5.3 新增美术资产：表格形式，包含资产名或资产组、资产类型、用途、规格要求、预期路径
5.4 原有美术资产改动：表格形式，包含资产路径、当前用途、改动内容
6. GameStage部分
6.1 新增GameStage：表格形式，包含GameStage名、包含哪些项
6.2 新增LoadItem和LateLoadItem项：表格形式，包含LoadItem项名、负责内容、所属GameStage（标注是新增或已有GameStage）
6.3 新增注册项：System、UiScene等除LoadItem之外的注册项，表格形式，包含项名、负责内容、所属GameStage；UiScene注册必须引用4.4中由UI编辑场景导出的Asset，不重复或替代其配置与导出步骤
6.4 修改LoadItem和LateLoadItem项：表格形式，包含LoadItem项名、负责内容、所属GameStage
6.5 删除GameStage项：表格形式，包含项名、负责内容、所属GameStage
7. 其他资产部分
7.1 涉及到的其他资产概览：记录不属于美术表现章节的音频、字体、视频、第三方资源包等资产
7.2 其他资产完整性检查：表格形式，包含资产或资产组、资产类型、用途、候选已有资产及路径、复用结论、来源与授权、缺失或不满足需求的内容、处理方式
7.3 新增其他资产：表格形式，包含资产名或资产组、资产类型、用途、规格要求、来源与授权、预期路径
7.4 原有其他资产改动：表格形式，包含资产路径、资产类型、当前用途、改动内容
8. Utils新增函数：新增Utils类（如有），以及Utils静态类需新增的函数列表
9. 实现顺序建议：你的实现建议补充和todo list

## 主代理复核

1. 删除空章节并重新连续编号。
2. 将“可选、建议、可能、待定”和并列方案收敛为一个确定方案；现有信息不足以确定时询问用户。
3. 原有内容的修改写入“原有内容改动”，新增内容写入“新增内容改动”。
4. 实现顺序按 Component、配置数据、Utils、System、UI View/Controller与Prefab、美术资产、其他资产、UiScene编辑场景与导出、GameStage注册排列，每个实际存在的步骤单独成条。新增或修改UiScene时，“创建/修改UI编辑场景”“配置Exporter与Prefab归属”“执行导出并校验Asset”“接入GameStage”必须是可审计的独立步骤，不能合并成一句“注册UiScene”。
5. Todo 与实现顺序逐条对应，名称和顺序保持一致。
6. UI方案必须按 `bbxcommon-ui` 的需求路由复核。发现Controller运行时拼装整页静态UI、手写UiSceneAsset或其它框架外路线时，持续修订到框架内；框架能力不足时按 `project-state-preflight` 的小改动/大改动分级处理。
