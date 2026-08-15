# 备战槽共享卡面与外框层级调整检查清单

## 用户要求与实现

- [通过] 出战槽中的卡片直接使用与战斗页面相同的完整卡片 UI，不保留独立卡面表现。
  - 证据：`PreparationController` 的 `BattleSlotList` 只创建 `BattleCardItemController`，并调用 `BindPreparationBattleSlot()`。
- [通过] 融合槽中的卡片直接使用与战斗页面相同的完整卡片 UI，不保留独立卡面表现。
  - 证据：`PreparationController` 的 `FusionSlotList` 只创建 `BattleCardItemController`，并调用 `BindPreparationFusionSlot()`。
- [通过] 出战槽与融合槽继续保留各自的槽位交互、拖放、选中和空槽状态。
  - 证据：共享 View 包含两种空槽、投放高亮、拖拽与 Interactor；共享 Controller 按绑定模式保留出战放置、融合素材移动/替换和无效来源过滤。
- [通过] 卡面外框下边缘向上调整，只遮住卡图底部，不遮挡攻击和血量徽章。
  - 证据：三层框的 `offsetMin=(0,24)`、`offsetMax=(0,0)`，Unity 结构校验和对应 Editor 测试通过。
- [通过] 攻击与血量徽章位于外框前景层并完整露出，战斗、卡池、出战槽与融合槽复用后表现一致。
  - 证据：Builder 将两枚徽章放在框层之后，结构校验确认徽章 sibling index 大于三层框；四种上下文共用同一 Prefab。
- [通过] 保持已统一的 `25:36` 卡面比例，避免槽内卡片非等比缩放。
  - 证据：卡池、出战、融合列表槽分别为 `200×288`、`220×316.8`、`190×273.6`，结构校验与比例测试通过。
- [通过] Prefab 与其一一对应的 UiBuilder 同步修改，并通过允许的 Unity Editor 通道生成或核验，不手写 Prefab YAML。
  - 证据：通过已连接 Unity Editor 执行 `BattleCardItemUiBuilder.Build()` 和 `PreparationViewUiBuilder.Build()`，Prefab 保存成功且 Console 最终 0 Error。
- [通过] 动态卡片仍通过 `UiList`、预加载映射和既有 Controller 生命周期创建与回收，不另建运行时 UI 或平行对象池。
  - 证据：三个列表统一使用 `UiList.AddItem<BattleCardItemController>()`；唯一映射为 `BattleCardItemController → Ui/BattleCardItem`。
- [通过] 退役废弃的备战卡池、出战槽、融合槽独立实现并清理其预加载映射；按项目规则不改删 `.meta` 文件。
  - 证据：三条旧 Pre-load 映射均由 Editor API 删除；旧 Controller 无运行逻辑、旧 Builder 仅转调共享 Builder。物理文件因禁止删除 `.meta` 而作为序列化兼容壳保留，`git status -- '*.meta'` 无改动。
- [通过] 检查对象池重入、绑定解绑、拖放目标和槽位索引等直接依赖，避免共享 Controller 后状态串槽。
  - 证据：`ResetBinding()` 清除战斗监听、备战模式、卡号、槽位、缩放、交互数据与显示状态；每次刷新重建对应来源和目标槽数据。
- [通过] 涉及代码时检查新增函数和字段的复用价值，删除不必要的一次性抽象。
  - 证据：卡面内容刷新集中到 `ShowPreparationCard()`，三种备战上下文共享状态机；预加载删除能力作为通用 Editor API 提供，未增加一次性页面 helper。

## 验证与边界

- [通过] 完成 Unity 编译、Prefab 结构检查和适用 Editor 测试；记录未进入 Play Mode 的验证边界。
  - 证据：Unity Console 最终 0 Error；结构校验通过；直接相关 7/7、`RunCardRulesTests` 与 `BattleKeywordRulesTests` 合计 28/28 通过。未进入 Play Mode，按项目默认规则保留人工视觉验收边界。
- [通过] 检查改动范围，保留用户和并行任务已有修改，不创建、修改或删除 `.meta` 文件。
  - 证据：未处理同时存在的奖励发牌、Loading 与其他临时文档修改；最终 `.meta` 状态无改动。
- [通过] 框架边界审计：未绕过 View/Controller、UiList、预加载、UiBuilder、资源 API 或编辑器配置源；最终活跃路径中没有平行卡片视觉实现。
  - 证据：活跃路径唯一使用共享 View/Controller/Prefab；静态配置来自两个一一对应 Builder，映射清理由公开 Editor API 完成，旧实现不在 Pre-load 或页面创建路径中。

## 文档同步

- [通过] 完整读取玩家视角设计文档格式，核对并同步本次玩家可见变化。
  - 证据：已读取 `design-doc-format`，更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md` 的三处共享卡面和徽章露出表现。
- [通过] 完整读取美术文档格式，核对并同步卡面层级、槽位复用和尺寸现状。
  - 证据：已读取 `art-doc-writer` 与模块格式，更新 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md`；未新增图片资产，UI 通用风格分组不变。
- [通过] 完整读取程序文档格式及适用专项格式，核对并同步 Controller、Prefab、绑定与布局现状。
  - 证据：已读取 `program-doc-format` 与 UI 界面格式，更新备战/战斗 UI 文档的唯一共享映射、绑定模式、尺寸和框层布局。

## 收尾

- [通过] 结束前重新打开本清单，逐项写入状态和证据。
- [通过] 只运行一次 `AutoDoc/CleanupTempDocs.bat` 并记录结果。
  - 证据：脚本实际执行一次，`CleanupExitCode=0`。
- [通过] 清理后创建 `AutoDoc/Temp/PreparationSlotSharedCardFrame-Report.md`。
