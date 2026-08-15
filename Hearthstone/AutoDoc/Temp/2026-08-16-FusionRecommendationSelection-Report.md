# 融合智能推荐选择与空素材池组合任务报告

## 1. 任务结果

任务已完成。智能推荐在融合素材池为空时会查询全部可用的精确 99 点组合；素材池非空时仍只返回包含全部当前素材的合法组合。弹窗以纵向滚动列表展示结果，每行横向显示 2～4 张共享卡面，当前素材显示“素材已选”标记，卡牌不可拖动，右侧“选择”按钮会把整条组合一次性写入融合素材槽。

为避免空素材池产生大量组合时一次创建数千行卡牌对象，结果数据完整保留，表现层只创建视口可见行数加一的对象池条目，并随滚动重绑定逻辑索引。

## 2. 检查项结果与证据

- 用户功能：通过。`FindFusionRecommendations()` 支持空素材池；`TryApplyFusionRecommendation()` 负责重新校验和原子回填；推荐行提供横排卡面、素材标记与选择按钮。
- UI 结构：通过。`PreparationView.prefab` 保存推荐 ScrollRect、手动列表和空结果文字；`FusionRecommendationItem.prefab` 保存推荐行、四槽卡片列表和选择按钮。
- 不可拖动：通过。共享卡片增加推荐只读绑定模式，显式关闭拖拽、交互器和悬停输入，并将滚轮转发给推荐列表。
- 大量组合：通过。完整结果数量决定 Content 高度；活跃推荐行数量只按视口高度计算，不随总组合数增加。
- 规则权威性：通过。UI 不直接写 `FusionSlotCardNumbers`；选择回填走规则层并只递增一次 `FusionRevision`。
- 对象池与生命周期：通过。推荐行及其内部卡片均通过 `UiList.ItemWrapper` 和预加载映射创建、隐藏、关闭与回收。
- 重复卡牌语义：通过。当前融合协议按互异卡号选择素材；同号多个副本不会生成重复的同卡号推荐，回填与融合继续使用该编号的首张副本。
- 框架边界：通过。没有直接访问 UI 内部 Manager、没有运行时拼装整页静态 UI、没有手写 UiScene 导出数据，也没有新增平行对象池。
- 无关改动：通过。任务开始时已有大量并发/用户改动，本任务未回退或清理这些文件。

## 3. 主要产物

- 规则与选择：`Assets/Scripts/Hearthstone/Ecs/System/RunCardRules.cs`
- 页面控制：`Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs`
- 推荐只读卡面：`Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs`
- 推荐行 MVC：`Assets/Scripts/Hearthstone/Ui/Controller/FusionRecommendationItemController.cs`、`Assets/Scripts/Hearthstone/Ui/View/FusionRecommendationItemView.cs`
- UI 配置源：`Assets/Scripts/Hearthstone/Ui/Editor/FusionRecommendationItemUiBuilder.cs`、`Assets/Scripts/Hearthstone/Ui/Editor/PreparationViewUiBuilder.cs`
- Prefab 与预加载：`Assets/Resources/Ui/FusionRecommendationItem.prefab`、`Assets/Resources/Ui/PreparationView.prefab`、`Assets/Resources/BbxCommon/Ui/PreLoadUiData.asset`
- 测试：`Assets/Scripts/Hearthstone/Tests/Editor/RunCardRulesTests.cs`

## 4. 验证结果

- Unity 编译：通过。
- 最终 Console：0 error。
- 聚焦测试：6/6 通过，覆盖空/非空推荐、原子回填、Prefab、字体与虚拟列表源码约束。
- `RunCardRulesTests`：32/32 通过。
- 全量 EditMode：完成 81 项，79 项通过，2 项失败。失败项为任务开始前已存在的 `AttackPresentationRejectsMismatchedAudioLists` 未声明预期错误日志，以及 `BattleCardHoverUsesUnifiedFramePaletteAndPreparationOnlyInteraction` 仍断言旧拖拽源码字符串；与本次推荐功能无关。
- Prefab 结构检查：页面推荐列表、空结果文字、推荐行卡片列表和选择按钮均非空；ScrollRect Content 指向推荐 UiList；弹窗默认隐藏。
- 预加载检查：`FusionRecommendationItemController → Ui/FusionRecommendationItem` 映射存在。
- Pre-UiInit 检查：推荐行记录 1 个 Bbx UI Item，页面记录 6 个 Bbx UI Item；实际层级分别含 1 个和 5 个 UiList。
- 文本与格式检查：相关代码、配置和正式文档执行 `git diff --check` 无错误。
- Play Mode：按项目默认约束未主动进入。

## 5. 执行偏差与未解决风险

- 第一次全量 EditMode 启动时 Editor 正处于 Play Mode 切换瞬间，测试任务未开始；只读复核确认 Editor 随后已退出 Play Mode，再次启动后完成 81 项测试。本任务没有主动进入或停止 Play Mode。
- 未执行游戏内鼠标拖拽、滚轮和按钮点击验收；当前证据来自规则测试、源码约束、Prefab/预加载结构检查和 Unity 编译。实际分辨率下的视觉密度仍建议在用户下次正常运行游戏时观察。
- 同编号多副本继续遵循现有“融合槽按卡号唯一”的协议，不支持同一编号的两个副本同时进入一条融合组合；本任务没有扩大该既有协议。

## 6. 文档处理

- 玩家视角：更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`。
- 美术：更新 `AutoDoc/Art/UI/ui-art-overview.md` 与 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md`。
- 程序：更新 `AutoDoc/Program/UI/preparation/preparation.md` 与 `AutoDoc/Program/Specific/preparation-card-pool/preparation-card-pool.md`。
- UI Item：未新增或修改自定义 `BbxUiItem`，只复用现有 `UiList`，因此不改 `AutoDoc/UIItem/`。

## 7. 清理结果

`AutoDoc/CleanupTempDocs.bat` 在结束审计后仅执行一次，退出码为 0。清理后创建本报告。
