# Preparation Slot Runtime Scale Correction Checklist

- [x] 通过 `UiApi` 创建链确认列表项为默认矩形 Controller 外层与 `250 × 360` View 子层；旧算法误读外层尺寸。
- [x] 采用用户反馈后的中间尺寸 `150 × 216`，空槽与拖入卡片共同使用 `0.6 × 0.6` 缩放。
- [x] 出战中心距 `185`、可见间距 `35 px`；融合中心距 `190`、可见间距 `40 px`，均不相接或重叠。
- [x] `ApplyExactSlotScale()` 改读实际 View 矩形，并把最终缩放写入共同外层，使全部视觉和交互子节点同步。
- [x] 增加运行结构回归，实际创建外层与 Prefab View 并断言最终 `150 × 216` 轮廓；既有检查继续覆盖两列表间距。
- [x] 两项定向 EditMode 回归通过；`Hearthstone.Tests.csproj` 编译 `0` 错误；Unity Console `0` error。
- [x] 玩家视角描述无需变更；受影响的程序与两份美术现状文档已同步 `150 × 216`、`0.6` 缩放和实际 View 计算链。
- [x] 已逐项审计；待按规则运行一次清理脚本后创建 Report。
