# 备战界面布局与页签美术修改检查清单

- [通过] 用户要求 1：卡池面板由 `1640 × 600` 扩为 `1780 × 660`，滚动区由 `1510 × 500` 扩为 `1650 × 570`，Viewport 保留 `Mask + RectMask2D`；Prefab 结构测试通过。
- [通过] 用户要求 2：Builder 不再创建 `RewardPanel/RewardText`，View 与 Controller 的 `RewardText` 引用和刷新逻辑已删除；Unity Prefab 查询确认节点不存在。
- [通过] 用户要求 3：`BattleTab`、`FusionTab` 改为左上锚点，接入新建的红金选中态和深木金边未选态 V2 Sprite；离线渲染已确认视觉区分。
- [通过] 用户要求 4：Continue Button 只保留“继续”主文字，删除“下一关”辅助节点与 View 引用，主文字在按钮内水平、垂直居中。
- [通过] 用户要求 5：Builder 不再创建 `PoolTitle`，Prefab 查询确认不存在卡池容量或持有数量计数节点。
- [通过] 用户要求 6：从项目 `NotoSansSC-VF.ttf` 固化 `wght=650` 的 SemiBold 字体并创建独立动态 TMP FontAsset；Preparation 页面和共享卡面共 0 个错误字体引用，所需中文字符检查通过。
- [通过] 使用 `imagegen` 内置工具生成项目用位图页签资源；以用户截图为风格参考，最终透明图保存为 `PreparationTabSelectedV2.png` 与 `PreparationTabIdleV2.png`，两图均为 `2172 × 724`、角点 Alpha 0，旧资源未覆盖。
- [通过] 已读取并遵循 `bbxcommon-ui`、`bbxcommon-resource` 与 `imagegen` skill；页面仍由 View/Controller、UiList、ResourceApi 和 Resources 字典管理。
- [通过] 已检查现有 Builder、View、Controller、Prefab、字体、资源字典及相关测试，并基于当前脏工作树增量修改；未回退既有共享卡面改动。
- [通过] 通过 Unity 编辑器正式通道调用 `PreparationViewUiBuilder.Build()` 与 `BattleCardItemUiBuilder.Build()`，并刷新 Resources 字典；没有手写 Prefab YAML。
- [通过] 删除失效的 `CreateReward`、`RewardText`、`ContinueAuxiliaryText`；新增 `ConfigureFont` 服务共享卡面多个文字节点，不保留一次性字段或孤立运行时引用。
- [通过] Unity 编译无错误；`Hearthstone.Ui.Editor.csproj` 与 `Hearthstone.Tests.csproj` 构建成功；`RunCardRulesTests` 19/19 通过；资源查询、Prefab 结构和离线视觉渲染通过。未进入 Play Mode。
- [未通过（既有非本任务问题）] 全量 EditMode 56 项中 55 项通过，`BattleRulesTests.RuntimeResourcesContainBattleCardCsvAndStageDataRegistry` 因测试期待 `Boar` 而当前资源为 `Boar_001` 失败；与本次 UI、字体和页签修改无直接依赖，未擅自改动。
- [通过] 玩家视角设计文档：已读取 `design-doc-format`，并同步 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md` 的页签、底框、隐藏文字、按钮与字体现状。
- [通过] 美术文档：已读取 `art-doc-writer` 与模块格式，更新 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md` 的两态页签规格、参考图、旧资产状态和字体视觉。
- [通过] 程序文档：已读取 `program-doc-format` 与 UI 界面格式，更新 `AutoDoc/Program/UI/preparation/preparation.md` 的 Builder、View、ResourceApi、字体和尺寸现状。
- [通过] 框架边界审计：Prefab 来自一一对应 Builder；运行时页签 Sprite 走 `ResourceApi`；新增资源已进入 Resources 字典；未添加临时菜单、运行时静态拼装、裸管理器访问或手写导出资产。
- [通过] 已完成逐项复核；下一动作只执行一次 `AutoDoc/CleanupTempDocs.bat`，随后创建同名报告并记录退出结果。
