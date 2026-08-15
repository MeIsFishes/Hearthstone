## 修正趟次 1 重新验收结论

- 结论：字体主干已修复，但滚动裁切主干未通过；进入修正趟次 2。
- 正式入口：再次通过 `GameStageEntryLauncher.Start(InitialStageEntry.asset)` 启动，同一 Play 的真实 Battle 终局自动进入 Preparation。
- 字体复核证据：`AutoDoc/DesignPlan/media/2026.08.15/preparation-stage-card-pool/review/trip-a-preparation-overview-1.png`；“备战阶段”“本轮获得 5 张卡”“战斗槽位”“卡池 1-98”与中文卡名清晰可读，首次乱码问题闭环。
- 滚动操作：对运行时正式 `ScrollRect` 调用其公开 `OnScroll(PointerEventData)` 输入路径，滚至 `verticalNormalizedPosition=0`。
- 失败证据：`AutoDoc/DesignPlan/media/2026.08.15/preparation-stage-card-pool/review/trip-a-card-pool-bottom.png`。

## 主干失败

- `ART-03`：滚至末段时 Content 条目越过 Viewport 上边界，蓝色卡池内容覆盖整个画面，标题、奖励和 3 个战斗槽全部不可见；底框/滚动条不能维持策划要求的上下区构图。
- `FUNC-03`：截图虽能看到末段 `92~98`，但滚动前后上方战斗槽并未保持可见，因此核心通过条件失败。
- `ART-05`、`ART-07`：滚动状态破坏整体层级，不能证明所有正式资产在完整运行状态下正确编排。
- 因 A 组主干仍失败，本趟没有继续 B～D；未执行项不判为通过。

## 修正方向

- 核查 `ScrollRect`、Viewport、`RectMask2D`、Content 与动态 UiList 条目的实际 Canvas/Transform 层级及材质裁切，确认条目始终受 Viewport 裁剪而不是只在初始位置看似正确。
- 通过 UiBuilder/公开 UI 流程修正，重新生成 Prefab/Scene/导出 Asset；禁止以截图裁切或外层临时遮罩伪造结果。
- 增加能在滚动到底后验证条目 Rect 不越过 Viewport 可见矩形或实际 Mask 状态的最小回归；完成本趟唯一 round 4 代码复审后，主代理重新执行 A～D。
