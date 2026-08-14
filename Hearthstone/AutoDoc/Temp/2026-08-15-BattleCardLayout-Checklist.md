# 战斗场景竖向卡牌改造方案检查清单

## 任务分类与范围

- [x] **通过**：将本次任务按“要求出方案”处理，只调研现状并编写 Plan，不修改游戏代码、配置或 Unity 资产。证据：仅新增本清单、Plan 和结束 Report；Plan §1.11 明确后续实现边界。
- [x] **通过**：需求覆盖双方战斗卡牌：长方形竖向卡面、左下角血量、右下角攻击力、上半部分原画占位、下半部分技能说明。证据：Plan §1.1、§4.1 与 Prefab 层级逐项覆盖。
- [x] **通过**：敌方卡牌通过旋转实现与我方相反的朝向，并明确旋转后文字与数值的可读性策略。证据：用户确认全部内容同样倒置；Plan §1.4、§4.1、§4.2 固定为条目根节点 Z 轴 `180°` 且不做反向补偿。
- [x] **通过**：炉石卡面仅作为信息层级与构图参考，不直接复制受版权保护的图像资产。证据：Plan §1.10、§5.1 明确只参考布局且不复制素材。

## 现状调研

- [x] **通过**：仅从 `Assets/Scripts/` 定位战斗卡牌的数据、逻辑、View/Controller、UiScene 与 GameStage 归属。证据：确认现有 `BattleCardRawComponent`、`BattleSystem`、`BattleCardItemView/Controller`、`BattleUiScene`、`BattleStages` 与 `HearthstoneGameEngine`。
- [x] **通过**：按项目文档目录定位直接相关的程序、美术与玩家视角现状文档；不读取 `AutoDoc/DesignPlan/`。证据：`AutoDoc/Program/`、`AutoDoc/Art/`、`AutoDoc/Design/` 当前均无文件；未搜索或读取 `AutoDoc/DesignPlan/`。
- [x] **通过**：核对血量、攻击力和技能说明是否已有唯一权威数据源，避免为 UI 建立平行状态。证据：现有攻血来自 `BattleCardRawComponent`，项目无技能数据；Plan §2 将静态配置置于 CSV/DataApi、运行时状态置于 ECS。
- [x] **通过**：明确卡面实例由 `BattleCardItemController` 管理，Entity 只作为 ECS 玩法数据载体，不作为卡面 View 或 UI 生命周期对象。证据：现状通过 `UiList.AddItem<BattleCardItemController>()` 创建卡面；Plan §1.2、§4.2 保持该方式。
- [x] **通过**：核对项目中现有 UI/卡牌美术资产的候选路径、用途与复用条件；无法从文本现状确认的 Unity 资产明确标记为实施期 Editor 核验项。证据：项目无业务图片文件，现有候选为 `BattleCardItem.prefab` 中的基础 UI 控件；Plan §5.2 列出复用结论和 Editor 核验项。

## 方案设计

- [x] **通过**：先形成独立、可验收的功能点清单，并在交付时请用户确认是否完整。证据：用户已确认敌方全部倒置和空技能不显示，并追加 UiController 边界；Plan §1.1 固化最终功能点。
- [x] **通过**：按数据、逻辑、UI、美术、GameStage、其他资产与实现顺序检查适用范围，删除空章节并连续编号。证据：Plan 保留 1–7 连续章节；无变更的其他资产、Utils、新增 System/StageListener 等章节已省略。
- [x] **通过**：明确卡牌静态层级进入 View Prefab，Controller 只监听数据并刷新表现，不在运行时拼装整张静态卡面。证据：Plan §4.1 给出完整 Prefab 层级，§4.2 限定 Controller 职责。
- [x] **通过**：动态重复卡牌沿用项目现有条目/对象池生命周期，避免重复分配与 GC。证据：Plan §1.2 与 §4.2 保留 `UiList.AddItem<BattleCardItemController>()` 和回池复位。
- [x] **通过**：明确修改现有 UiScene 时须通过 UI 编辑场景与 `UiSceneExporter` 重新导出，不手写 Scene、Prefab 或 `UiSceneAsset`。证据：本方案不改变 UiScene 导出信息；Plan §4.1、§7 明确不修改场景、不手写 Asset，并在实现期通过 Unity Editor/MCP 编辑 Prefab。
- [x] **通过**：明确敌我朝向、布局锚点、尺寸比例、文字区域、原画占位区域以及最小可读性验收标准。证据：Plan §1.3–§1.7、§4.1 固定尺寸、旋转、上下分区、角标尺寸、字体范围和溢出规则。
- [x] **通过**：核对 CSV 的 DataGroup 与加载顺序，保证 `InitializeBattleRuntime` 创建卡牌前配置已由 `DataApi` 加载。证据：`GameEngineStage` 先加载 `GameEngineDefault`，再调用派生 `OnAwake()`；Plan §2.2.1 使用默认组，规避 BattleStage 的 LoadItem 早于 Stage Data 的顺序。
- [x] **通过**：美术资产完整性表覆盖卡框、原画遮罩/占位、属性底座、技能文本底板及字体/图标需求，并区分复用、变体与新增。证据：Plan §5.2 完整列出五类需求、候选、结论和处理方式。
- [x] **通过**：给出确定且单一路线的实现顺序、验证方式和逐条对应 Todo。证据：Plan §7.1 与 §7.2 均为八项且名称、顺序一一对应。

## 框架边界与结束审计

- [x] **通过**：审计方案未绕过 BbxCommon 的 ECS、UI、UiScene 导出、GameStage、ResourceApi 与对象池边界。证据：配置走 DataApi、战斗状态走 ECS、卡面走 UiController/UiList、Prefab 走 Unity Editor、Stage 沿用公开注册入口。
- [x] **不适用**：未发现框架能力缺口，无需按小改动/大改动分级，也未设计业务层兼容补丁。
- [x] **通过**：最终 Plan 保存到 `AutoDoc/Temp/Plan/`，并按 `plan-output-format` 复核标题、编号和表格。证据：`AutoDoc/Temp/Plan/2026-08-15-BattleCardLayout-Plan.md`；标题检查显示大章 1–7 连续。
- [x] **通过**：任务结束前逐项写入通过、未通过或不适用及证据。证据：本清单所有审计项均已标记并附证据。
- [x] **通过**：结束前只运行一次 `AutoDoc/CleanupTempDocs.bat`，随后创建同任务名 `*-Report.md` 记录结果与风险。证据：脚本退出码为 `0`；清理后 `AutoDoc/Temp/` 顶层 Markdown 数量为 `38`，未达到清理阈值，未删除文件。
