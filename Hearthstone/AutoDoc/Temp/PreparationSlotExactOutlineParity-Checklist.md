# 备战空槽与卡片轮廓完全一致检查清单

- [通过] 出战空槽与入槽卡片共用 `250 × 360` 源轮廓和 `0.33744 × 0.3515` 根缩放，最终宽高同为 `84.36 × 126.54 px`。
- [通过] 融合空槽与入槽卡片共用 `250 × 360` 源轮廓和 `0.37392 × 0.3895` 根缩放，最终宽高同为 `93.48 × 140.22 px`。
- [通过] 以用户确认“两版之前合适”的空槽尺寸为唯一基准，恢复出战 `84.36 × 126.54` 与融合 `93.48 × 140.22`，仅让占用卡片匹配，不再估算新尺寸。
- [通过] Unity 几何检查分别得到 `equal=True`；出战与融合的空槽/卡片宽高逐项相等，相邻间距分别为 `100.64 px` 与 `96.52 px`，均大于零。
- [通过] Editor 测试已删除允许约 `4 px` 宽度差的旧断言，改为分别断言 X/Y 缩放、空槽/卡片宽高相等和间距相等；目标测试通过。
- [通过] 已通过 `BattleCardItemUiBuilder.Build()` 重建并保存 Prefab；未手写 Prefab YAML，本任务未创建、编辑或删除 `.meta`。
- [通过] 继续复用 `PreparationPoolEmptySlot.png`、`UiList.ConstantSlot`、共享拖拽交互、BbxCommon 对象池和页面生命周期；只调整专用空槽静态轮廓与槽位绑定缩放。
- [未通过] 目标测试通过；三个相关程序集均 `0` 错误；Unity Console 清理后 `0` 错误。完整 EditMode `113` 项中 `109` 通过、`4` 项非 UI 玩法规则测试失败，仍是六槽场景、攻击者序列、稀疏继续阵容和卡牌替换规则；按范围未改这些逻辑。未进入 Play Mode。
- [通过] 框架边界审计：实现位于现有 `PreparationController`、共享 `BattleCardItemController` 和一一对应 UiBuilder 内；没有平行 UI、手写导出资产或绕过 BbxCommon 生命周期。
- [通过] 一次性抽象审计：新增 `ApplyExactSlotScale()` 被出战与融合两种槽位共同使用；保留原 `ApplyContainedSlotScale()` 服务敌方预览等需要等比适配的上下文，没有旧尺寸兼容分支。
- [通过] 玩家视角设计文档：已完整读取 `design-doc-format`，并同步备战卡池文档为“空槽与入槽卡片轮廓宽高和缩放完全一致”。
- [通过] 美术文档：已完整读取 `art-doc-writer` 与 `art-module-format`，同步备战卡池与共享卡牌模块的恢复尺寸、X/Y 缩放和间距。
- [通过] 程序文档：已完整读取 `program-doc-format` 与 `ui-screen-doc-format`，同步 Builder 的专用空槽轮廓和 `ApplyExactSlotScale()` 绑定逻辑。
- [待收尾] 已复核任务相关 diff、直接依赖和文档旧值，相关文件 `git diff --check` 无新增错误；下一步只运行一次清理脚本并创建同名报告。
