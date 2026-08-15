# 备战槽共享卡面与外框层级调整任务报告

## 任务结果

任务已完成。备战卡池、三个出战槽和四个融合槽的活跃条目现在都由 `UiList` 创建同一个 `BattleCardItemController`，并加载同一个 `Assets/Resources/Ui/BattleCardItem.prefab`。共享 Controller 通过卡池、出战槽、融合槽三种备战绑定模式区分卡号来源、空槽状态、拖拽来源、投放目标和无效来源过滤，不再由独立槽位 Controller 绘制卡面。

卡框、攻击徽章和生命徽章的共享层级也已调整。红金/蓝金卡框及攻击者、目标高亮的底边统一上移 `24 px`，左右与上边仍贴合 `250 × 360` 卡面；攻击和生命徽章继续位于框层之后绘制的前景层，因此底部从卡框外完整露出。该表现同时应用于战斗、卡池、出战槽和融合槽。

## 实现与检查结果

- 出战槽复用：通过。`BattleSlotList` 创建 `BattleCardItemController` 并绑定为出战槽模式。
- 融合槽复用：通过。`FusionSlotList` 创建 `BattleCardItemController` 并绑定为融合槽模式。
- 交互保留：通过。共享 Prefab 保存卡池空态、出战空槽、融合空槽、素材角标、投放高亮、`UiDragable` 和 `UiInteractor`；共享 Controller 保留原有放置、替换、素材移动和拖回卡池行为。
- 比例一致：通过。卡池、出战槽、融合槽分别为 `200 × 288`、`220 × 316.8`、`190 × 273.6`，均为 `25:36`。
- 对象池重入：通过。换绑时解除五个战斗监听，并清理绑定模式、卡号、槽位、缩放、交互数据、空态和卡面显示。
- Builder/Prefab：通过。通过已连接 Unity Editor 执行 `BattleCardItemUiBuilder.Build()` 与 `PreparationViewUiBuilder.Build()`，没有手写生成 Prefab YAML。
- 预加载：通过。旧的三个备战条目映射通过公开 Editor API 清除，当前共享卡片映射唯一指向 `Ui/BattleCardItem`。
- 框架边界：通过。活跃页面未绕过 View/Controller、`UiList`、Pre-load、UiBuilder 或资源 API，没有平行卡片视觉路径。
- `.meta`：通过。最终没有 `.meta` 文件改动。

## 废弃 UI 处理

用户允许删除以前的废弃 UI，但项目规则禁止创建、编辑或删除 `.meta`。实际尝试只删除文件本体时，Unity 会立刻删除对应 `.meta` 并产生 12 条孤立元数据错误，因此该尝试已完整撤销，资产与 `.meta` 恢复到一致状态。

最终采用不破坏项目资产数据库的退役方式：旧 Controller 清为无运行逻辑的序列化兼容壳，旧 Builder 只转调共享 `BattleCardItemUiBuilder`，三条旧 Pre-load 映射全部删除，活跃 `PreparationController` 不再引用旧 View、Controller 或 Prefab。旧物理文件仅为既有 GUID/序列化兼容保留，不再构成运行时或 Builder 平行实现。

## 验证结果

- Unity 脚本编译完成，最终 Console 为 0 Error。
- Unity 结构校验通过：共享备战引用完整；三层卡框底边均为 `24 px`；攻击和生命徽章 sibling index 均高于框层；三个列表比例正确；旧 Pre-load 映射不存在。
- 直接相关 Editor 测试：7/7 通过。
- `RunCardRulesTests` 与 `BattleKeywordRulesTests`：合计 28/28 通过。
- `BattleRulesTests`：18/19 通过；唯一失败为既有资源数据断言期望 `Boar`、实际为 `Boar_001`，与本次 UI 修改无关。
- 按项目默认规则未进入 Play Mode。未执行的部分是游戏内人工视觉验收和完整拖拽手感验收。

## 文档处理

- 玩家视角：更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`。
- 美术：更新 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md`。
- 程序：更新 `AutoDoc/Program/UI/preparation/preparation.md` 与 `AutoDoc/Program/UI/battle/battle.md`。
- 本次未新增图片资产，通用 UI 风格分组和现有资产列表无需新增条目。

## 执行偏差与风险

- 废弃 UI 未物理删除，原因是项目 `.meta` 禁止改删约束；其活跃引用、业务逻辑和 Pre-load 已清除。若后续明确允许连同 `.meta` 一起通过 Unity AssetDatabase 删除，可再做物理清理。
- 未进入游戏进行人工视觉验收。建议后续重点观察空槽投放高亮、出战槽替换、融合槽移动/拖回卡池，以及不同缩放下徽章露出是否符合最终美术感受。
- 整类 `BattleRulesTests` 的 `Boar`/`Boar_001` 资源断言仍需独立任务处理。
- 工作区同时存在奖励发牌、Loading 和其他临时文档修改，本任务没有回退或覆盖这些并行改动。

## 清理结果

结束审计后实际运行一次 `AutoDoc/CleanupTempDocs.bat`，返回 `CleanupExitCode=0`。清理后创建本报告，未再次运行清理脚本。
