# 备战槽位入槽卡片尺寸适配检查清单

- [通过] 复核问题根因：原实现把专用空态子节点四边内缩 `50 px`，而占用卡面仍使用更大的条目根缩放；空态与占用态因而不在同一尺寸基准上。
- [通过] 出战页签的空槽与入槽卡片使用同一套紧凑根节点尺寸。证据：`PreparationSlotVisualFillRatio = 0.48f` 同时用于出战绑定；实测高度均为 `127.872 px`。
- [通过] 融合页签的空槽与入槽卡片使用同一套紧凑根节点尺寸。证据：同一常量用于融合绑定；实测高度均为 `131.328 px`。
- [通过] 检查脚本确认尺寸与间距。证据：出战占用/空槽为 `88.8 × 127.872` / `85.248 × 127.872`，间距 `96.2` / `99.752 px`；融合为 `91.2 × 131.328` / `87.552 × 131.328`，间距 `98.8` / `102.448 px`，均大于零。
- [通过] 保持现有 `PreparationPoolEmptySlot.png`、`UiList.ConstantSlot`、共享卡条目、拖拽组件、对象池与页面生命周期；仅统一条目根缩放和空态静态布局。
- [通过] 编辑器测试已更新，覆盖统一 `0.48` 缩放、空槽完整拉伸、占用/空态高度一致、宽度差上限、相邻间距和专用空态切换条件；目标测试 `1/1` 通过。
- [通过] 已通过 `BattleCardItemUiBuilder.Build()` 重新生成 `Assets/Resources/Ui/BattleCardItem.prefab`；Unity 检查确认两种专用空态共用 Sprite、场景未脏、Editor 未进入 Play Mode。
- [未通过] 相关三个程序集均 `0` 错误，目标测试通过，Unity Console 清理后 `0` 错误；完整 EditMode 实际执行 `111` 项，其中 `107` 通过、`4` 项非 UI 规则测试失败。失败涉及战斗场景六槽约束、攻击序列、稀疏继续阵容和卡牌替换规则，与本次槽位缩放断言无直接调用关系；按范围未改动这些玩法逻辑。未进入 Play Mode，符合项目默认约束。
- [通过] 框架边界审计：修改仍通过现有 `PreparationController`、`BattleCardItemController`、`UiList`、BbxCommon 对象池和一一对应 `BattleCardItemUiBuilder` 落地；未新增平行 UI、手写 Prefab YAML 或运行时静态拼装。
- [通过] 一次性抽象审计：把两页旧的独立比例常量合并为一个复用常量，并删除仅服务旧内缩方案的常量与参数；未新增函数。
- [通过] 玩家视角设计文档：已完整读取 `design-doc-format`，并同步 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md` 的空槽与入槽卡片一致紧凑表现。
- [通过] 美术文档：已完整读取 `art-doc-writer` 与 `art-module-format`，同步 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md` 和共享卡牌模块文档的实际尺寸与间距。
- [通过] 程序文档：已完整读取 `program-doc-format` 与 `ui-screen-doc-format`，同步 `AutoDoc/Program/UI/preparation/preparation.md` 的统一根缩放、尺寸、间距和 Builder 配置。
- [通过] 已检查任务相关 diff 与直接依赖；没有通过本任务创建、编辑或删除 `.meta`，未回退或覆盖工作区其他功能改动。相关文件 `git diff --check` 无新增空白错误。
- [待收尾] 已完成逐项审计；下一步只运行一次 `AutoDoc/CleanupTempDocs.bat`，随后创建同名 Report 并记录清理结果。
