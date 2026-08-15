# 融合智能推荐选择与空素材池组合检查清单

- [通过] 用户要求：智能推荐的每个合法组合横向排列卡牌，右侧提供“选择”按钮。证据：新增 `FusionRecommendationItem.prefab`，包含四槽横向 `CardList` 与右侧 `SelectButton`；Prefab 结构测试通过。
- [通过] 用户要求：点击“选择”后，将对应组合的卡牌自动加入融合素材池，并保持总点数恰好为 99。证据：`FusionRecommendationItemController` 回调页面，`RunCardRules.TryApplyFusionRecommendation()` 重新评估后原子替换四槽；`ApplyingFusionRecommendationAtomicallyReplacesMaterialSlots` 通过。
- [通过] 用户要求：智能推荐面板内的卡牌不可拖动。证据：推荐绑定模式显式关闭 `UiDragable`、其事件监听器、`UiInteractor` 与悬停输入；相关源码断言通过。
- [通过] 用户要求：推荐组合列表可上下滚动，能够承载较多组合。证据：纵向 `ScrollRect` 继续作为入口，完整结果决定 Content 高度；Controller 只维持可见行数加一的虚拟行并按滚动偏移重绑定，卡片滚轮会转发到推荐 ScrollRect。
- [通过] 用户要求：当前融合素材池为空时，展示全部可用的 99 点组合；非空时仅展示包含当前素材的合法组合。证据：规则入口移除空选择门禁；空选择与已有选择的两组推荐测试均通过。
- [通过] UI 框架：沿用现有 Preparation View/Controller、Prefab 与一一对应 UiBuilder；静态布局写入 Prefab，不在运行时拼装整页或绕过序列化引用。证据：页面内部由 `PreparationViewUiBuilder` 更新；推荐行由一一对应的 `FusionRecommendationItemUiBuilder` 生成并导出预加载映射。
- [通过] 动态条目与生命周期：复用项目既有 UiList/对象池和 Controller 生命周期；检查是否需要适用 `bbxcommon-ui-item`，如命中则先完整读取。证据：已完整读取 `bbxcommon-ui-item`；未新增或修改自定义 `BbxUiItem`，仅使用现有 `UiList`。Unity 检查确认页面 5 个 UiList、推荐行 1 个 UiList 均已 Pre-UiInit，动态 Controller 由预加载池创建和回收。
- [通过] 重复卡牌：核对推荐组合与自动选择是否能正确识别同编号的不同持有副本，避免组合展示或回填数量错误。证据：当前融合槽权威协议仍按卡号且禁止同号重复素材；推荐按互异卡号生成，不因同号副本重复输出相同组合，选择时沿用该卡号首张副本，与现有融合语义一致。
- [通过] 交互回归：推荐弹窗打开、关闭、空结果提示、滚动、选择回填、融合按钮状态及原有拖拽流程不受破坏。证据：`RunCardRulesTests` 32/32 通过；Prefab 保持弹窗默认隐藏、中央空结果、关闭按钮和纵向滚动；融合按钮继续只读取 `evaluation.CanFuse`。
- [通过] 代码质量：检查新增字段/函数的复用价值，删除不必要的一次性抽象。证据：规则层仅新增可独立复用和测试的原子推荐应用入口；可见行索引与行距只服务虚拟列表；旧富文本 Builder 与缓存已删除。
- [通过] 框架边界审计：检查是否绕过 BbxCommon UI、公开 API、生命周期、Prefab Builder、对象池或权威玩法状态；发现平行实现必须整改。证据：View 只保存引用，Controller 管交互；玩法写入集中在 `RunCardRules`；静态层级来自 Builder；动态行与卡牌都走 `UiList`/`UiApi` 预加载池，无直接访问内部 Manager 或运行时静态页面拼装。
- [通过] 验证：完成编译、相关 EditMode 测试、Prefab 结构/引用检查与 Console 错误检查；默认不进入 Play Mode。证据：Unity 编译成功、最终 Console 0 error；相关测试 32/32，通过的聚焦测试 6/6；全量 EditMode 完成 81 项，其中 79 项通过、2 项为任务前已存在且与本改动无关的失败。未进入 Play Mode。
- [通过] 玩家视角设计文档：完整读取基础格式 skill，核对现有融合/牌库文档并同步实际玩家规则与交互。证据：已读取 `design-doc-format`，更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md` 的智能推荐规则、展示、滚动和选择行为。
- [通过] 美术文档：完整读取基础格式 skill，核对推荐组合行、按钮与滚动区域的当前视觉结构并同步或说明不适用。证据：已读取 `art-doc-writer`、UI 总览与模块格式，更新 UI 总览和备战卡池模块文档，记录横排共享卡面、素材标记、暖棕行底与红金选择按钮。
- [通过] 程序文档：完整读取基础格式 skill，核对推荐算法、选择回填、列表对象池和重复卡牌语义并同步。证据：已读取 `program-doc-format` 与 UI 界面格式，更新备战 UI 和备战卡池程序文档，记录空池枚举、原子回填、虚拟行和预加载生命周期。
- [通过] 无关改动审计：检查工作树，保留用户及其他既有改动，仅报告本任务实际修改。证据：任务开始时工作树已有大量脚本、资源、文档和攻击动画资产改动；本任务未回退或清理这些内容，审计只覆盖推荐功能相关文件。
- [通过] 结束审计：重新读取本清单，逐项写入通过/未通过/不适用及证据，修复可修正缺口。证据：2026-08-16 逐项复核完成；代码与文档范围执行 `git diff --check` 无错误。
- [通过] 清理与报告：结束审计后仅运行一次 `AutoDoc/CleanupTempDocs.bat`，再创建同名 `*-Report.md` 记录结果、验证、偏差、风险、文档与清理结果。证据：清理脚本于 2026-08-16 唯一一次执行并以退出码 0 完成；对应报告已创建。
