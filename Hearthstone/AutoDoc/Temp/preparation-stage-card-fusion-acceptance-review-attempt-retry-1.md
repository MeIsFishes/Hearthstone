# 备战阶段卡牌融合验收修正趟次 1

## 修正范围

首次正式入口验收发现 `FusionOperationRoot` 在 Ui 预初始化前处于 inactive，导致融合区 Wrapper 与动态列表没有进入框架初始化链。执行代理删除 Builder 的默认 inactive 设置，让出战/融合两个 operation root 在 `PreUiInit/UiInit` 均激活，仍由 `PreparationController.OnUiOpen -> SelectTab(Battle)` 在初始化完成后切换默认显示；随后通过公开 Builder、UiSceneBuilder 与 Exporter 重建正式资产。按用户“各类审查最多一次”的明确约束，本趟由主代理核对差异和框架证据，不追加第二次代码审查。

## 重新验收

- 正式入口初始化：`CardPoolList=99`、`FusionSlotList=4`、`BattleSlotList=3`，无 NRE，Console Error=0。
- Trip A：页签、素材放入/替换/移除、第五码、重复/未拥有、小于/等于/大于 99 全部通过。
- Trip B：融合事务生成唯一 `99/11/15`，消耗素材并清对应出战槽；二次融合与 99 作素材被拒；99 进入生产 Battle Entity。
- Trip C：未确认选择切页保留，离开 Preparation 时 Session 清空而素材与 Run Revision 不变。
- 资产编排：9/9 Sprite 可加载且导入规格正确；三态页签/按钮、4 槽、合计三色、素材标记、99 原画与卡面均已连接；Preparation Scene 为 Connected、非 dirty。
- 最终 Editor：活动场景 `Assets/Scenes/Main.unity`、`isDirty=false`、`rootCount=1`、Console Error=0。

结论：修正趟次 1 的主干重新验收通过，不再进入更多修正或审查趟次。
