# 战斗场景竖向卡牌实现检查清单

## 任务与需求

- [x] **通过**：按“修改项目”实现 `AutoDoc/Temp/Plan/2026-08-15-BattleCardLayout-Plan.md`，未搜索或读取 `AutoDoc/DesignPlan/`。证据：源码、CSV、Prefab、测试与正式现状文档均已落地。
- [x] **通过**：卡面实例继续由 `BattleCardItemController` 与 `UiList` 创建、刷新和回收；Entity 只承载 ECS 玩法状态，不承担 UI 生命周期。证据：`BattleController.PopulateCards()` 仍调用 `UiList.AddItem<BattleCardItemController>()`，Pre-load 映射为 `Ui/BattleCardItem`。
- [x] **通过**：玩家卡牌为竖向长方形；敌方整张卡牌连同文字、数值、高亮和遮罩旋转 `180°`。证据：Prefab 根尺寸 `220 × 320`；Controller 按 `Side` 旋转 View 根节点并在回池时恢复。
- [x] **通过**：卡牌本地上半区为空原画区域，下半区为技能说明区域，本地左下显示血量、本地右下显示攻击力。证据：MCP 回读锚点为 Artwork `0.5–1.0`、Skill `0.0–0.5`、Health `(0,0)`、Attack `(1,0)`。
- [x] **通过**：无技能时隐藏技能文字；CSV 预留技能说明字段，并成为默认攻血配置的唯一静态来源。证据：默认 CSV 行为 `1,3,5,`；技能 TMP 默认 inactive，Controller 对空白字符串关闭对象；默认攻血常量已删除。

## Unity MCP 前置与资产安全

- [x] **通过**：完整工具目录发现 48 个 `mcp__unityMCP__*` 延迟工具，并实际调用成功。
- [x] **通过**：`codex mcp get unityMCP` 显示绝对 `uvx.exe`、`mcpforunityserver==10.0.0`、stdio、UTF-8 与 `SystemRoot` 环境均已配置。
- [x] **通过**：Unity 2022.3.62f3c1 进程正在运行；manifest 与 packages-lock 均固定 `com.coplaydev.unity-mcp#v10.0.0`；实例为 `Hearthstone@e97c0c17`。
- [x] **通过**：资产写入前 `manage_scene(get_active)` 返回未脏 `Assets/Scenes/Main.unity`，Console error 为 0；Editor 为 Idle、非 Play、无编译任务。
- [x] **通过**：Prefab 仅通过当前会话官方 `unityMCP.execute_code` 内的 Unity `PrefabUtility` 修改；没有文本读写 Prefab/Scene/Asset，也未启用其他桥接层。

## 数据与 ECS

- [x] **通过**：新增 `BattleCardCsvData`，继承 `CsvDataBase<BattleCardCsvData>`，使用 `Override`、默认 `GameEngineDefault`、同名表与 `DataApi.SetData(Id, this)`。
- [x] **通过**：新增 `Assets/Resources/Config/BattleCardCsvData.csv`，表头、等列英文说明、`Associated: None` 和默认空技能行均符合规范。
- [x] **通过**：通过 `Tools/Build Resources Dictionary` 构建，日志确认成功；key `BattleCardCsvData` 唯一映射到 `Config/BattleCardCsvData`，`Resources.Load<TextAsset>()` 非空。
- [x] **通过**：`BattleCardRawComponent.ConfigId` 随配置初始化并在回收时归零；Attack/MaxHealth/CurrentHealth 来源为配置；监听字段仍先 `MakeInvalid()` 再复位。EditMode 回收测试通过。
- [x] **通过**：`BattleRules` 仅保留 `DefaultCardConfigId = 1`；`InitializeBattleRuntime` 创建会话前从 `DataApi` 查询，缺失时抛出包含 ID 的异常。
- [x] **通过**：实际启动发现项目缺少框架要求的 `BbxCommon/ScriptableObjectAssets.asset`，导致 `GameStage` 在 CSV 自动加载前提前返回；现已通过 Unity Editor 创建并初始化空注册资产，资源字典已同步，初始化构建器也会为新项目生成该资产。

## UI 与 Prefab

- [x] **通过**：`BattleCardItemView` 新增 `ArtworkArea` 与 `SkillDescriptionText`，删除 `SlotText`；类内仍只有 public 引用和 `GetControllerType()`。
- [x] **通过**：`BattleCardItemController` 保留四个 ModelWrapper 监听，新增 DataApi 查询、空技能隐藏、整卡旋转与 Unbind 复位；静态子节点全部在 Prefab。
- [x] **通过**：`BattleController` 的 UiList 创建路径保持不变，没有把 Controller 存入 ECS 或新增平行 GameObject 管理路径。
- [x] **通过**：`BattleCardItem.prefab` 已由 Unity Editor 改为 `220 × 320`，包含 12 个对象且 View 八项关键引用均非空；旧 Slot/Label 对象已移除。
- [x] **通过**：`BattleView.prefab` 两个列表均为 `930 × 320`，Y 为 `±205`，AreaFit slot 为 `244 × 320`；中央状态区保留。
- [x] **通过**：未修改 `Battle.unity`、`Battle.asset`、UiGroup 或导出参数；`Ui/Battle` 仍含一个条目且 PrefabPath 为 `Ui/BattleView`。
- [x] **不适用**：未新增或修改通用 `BbxUiItem`，无需执行组件文档流程。

## 验证

- [x] **通过**：在 `BattleRulesTests` 增加 CSV/DataApi 空技能测试与配置初始化/回收复位测试，并为测试程序集补齐直接引用。
- [x] **通过**：新增运行时资源链路测试，确认 Stage 数据注册资产与 CSV 资源均存在且默认配置可解析；最终 EditMode `Hearthstone.Tests` 共 11 项，11 passed、0 failed、0 skipped。
- [x] **通过**：Unity 编译完成并处于 Idle；以 `error CS` 过滤 Console 返回 0 条。
- [x] **通过**：MCP 确认两个 Prefab 层级与引用、Pre-load=`Ui/BattleCardItem`、CSV 可加载、Battle UiSceneAsset 非空且含 1 项。
- [x] **通过**：针对用户报告的启动异常进行一次短暂 Play 验证：运行时自动取得配置 `1:3:5`、创建双方各 3 张卡牌 Entity，并存在 6 个 `BattleCardItemController`；随后退出 Play。结束验收活动场景仍为未脏 `Main.unity`，Editor 非 Play，Console error 为 0。

## 文档同步

- [x] **通过**：完整读取 `design-doc-format`、`art-doc-writer`、`program-doc-format` 及命中的战斗、UI、模块格式引用；按实际实现判断文档范围。
- [x] **通过**：玩家视角设计文档原不存在；新增 `AutoDoc/Design/Specific/combat-system/combat-system.md`，记录当前自动战斗、攻血、竖卡布局和倒置表现。
- [x] **通过**：美术文档原不存在；新增风格总览、UI 美术总览和 battle-card 模块文档，记录当前纯色 Prefab 表现、色值、无独立图片资产事实与参考来源。
- [x] **通过**：程序文档原不存在；新增战斗系统特殊程序文档与 battle UI 文档，记录当前 CSV、Component、System、LoadItem、UiController、Pre-load 与 Stage 关系。

## 框架边界与结束审计

- [x] **通过**：配置通过 DataApi、状态通过 ECS、界面通过 UiController/UiList、资源字典通过项目菜单、Prefab 通过 Unity Editor API、Stage 通过公开 Add/Set 入口。
- [x] **通过**：新增代码没有一次性 Utils、重复 UI 状态、裸 delegate、底层 Manager 访问或运行时静态层级构建；未手写 Unity 序列化资产。
- [x] **通过**：修改范围限于 Plan 直接文件、测试程序集直接依赖、Pre-load/Resources Dictionary 导出资产与正式现状文档。未通过文件工具创建、编辑或删除 `.meta`；Unity 导入新源码/CSV 时自动生成配套 `.meta`，未对其进行人工操作。
- [x] **通过**：本清单所有项目已逐项复核；发现的测试程序集引用、Pre-load 与 Stage 数据注册资产缺口均已修复后复核通过。
- [x] **通过**：结束审计期间仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0；用户随后报告运行时缺口时未重复清理，并在最终修复与复核完成后创建对应 `*-Report.md`。
