# 嘲讽盾牌与伤害数字可读性检查清单

- [x] 用户要求：增加嘲讽盾牌从卡面后方露出的面积，同时保持其位于卡面下层；横向仍不超过 `278 px` 空卡槽宽度。（通过：关闭 `preserveAspect`，左右实际金属轮廓约露 `11.44 px`）
- [x] 用户追加：嘲讽盾牌的上、下边界也各露出少量轮廓；利用素材透明留白控制实际金属边缘仅超出卡面约 `4~5 px`。（通过：`278 × 384`、位置 `(0,-4)`，估算上 `4.29 px`、下 `5.13 px`）
- [x] 用户要求：将伤害浮字改为清晰的红色，并保留必要描边以保证黄色爆炸底板上的对比度。（通过：文字 `#D22020FF`，深红 `2 px` 描边）
- [x] 定位 `BattleCardItemUiBuilder` 中嘲讽盾牌运行尺寸、伤害文字颜色与描边的唯一配置源。（通过：`TauntShieldRenderSize`、`TauntShieldRenderPosition`、`ConfigureFeedbackLayers`）
- [x] 只通过与 `BattleCardItem.prefab` 一一对应的 Builder 修改静态布局和样式；不手写 Prefab YAML，不在 Controller/View 中增加静态配置逻辑。（通过：源码仅改 Builder 与测试，Prefab 由 Builder 保存）
- [x] 重新执行 Builder，核对 View 序列化引用、盾牌层级、默认显隐、尺寸边界、伤害文字颜色与图层顺序。（通过：live Builder 执行成功；盾牌 sibling `0`、默认隐藏、红色与描边均由 Prefab 实读确认）
- [x] 更新定向 EditMode 测试，使其覆盖新的盾牌露出尺寸与红色伤害文字。（通过：更新两个 `BattleRulesTests` 断言）
- [x] 执行 Unity 定向 EditMode 测试、相关程序集编译并检查 Console 新错误。（通过：Unity `2/2`；Editor 与 Tests 工程退出码均 `0`、`0` 错误；最终刷新 Console `0` 新错误）
- [x] 框架边界审计：保持 View 仅持有引用、Controller 负责运行时状态、Builder 维护静态 Prefab；不绕过 BbxCommon UI 生命周期和对象池。（通过：未修改 View/Controller/资源或生命周期）
- [x] 抽象审计：本次仅调整现有配置常量与测试断言，不新增无复用价值的函数或字段。（通过：仅增加可复现盾牌位置常量，无一次性 helper）
- [x] 修改范围审计：保留工作区既有攻击节奏、卡牌动效、嘲讽和备战相关改动，不回退无关文件。（通过：未修改或回退并发中的备战文件）
- [x] 玩家视角设计文档：按战斗系统格式核对盾牌露出程度与红色伤害数字的当前描述，必要时更新 `AutoDoc/Design/Specific/combat-system/combat-system.md`。（通过：已同步左右与上下露出、红色伤害数字）
- [x] 美术文档：按模块美术格式核对盾牌运行尺寸与伤害数字配色，必要时更新 `AutoDoc/Art/Modules/battle-card/battle-card.md`。（通过：已同步 `278 × 384`、偏移、可见边界与 `#D22020`）
- [x] 程序文档：按战斗系统与 UI 界面格式核对 Builder 尺寸和伤害浮字颜色，必要时更新 `AutoDoc/Program/Specific/combat-system/combat-system.md`、`AutoDoc/Program/UI/battle/battle.md`。（通过：UI 文档已同步静态配置；战斗系统数据与结算未变化，专项程序文档无需修改）
- [x] 结束前重新读取清单，逐项写入证据；仅运行一次 `AutoDoc/CleanupTempDocs.bat`，随后创建同名报告。（通过：本次复核已完成；待执行清理并写报告）
