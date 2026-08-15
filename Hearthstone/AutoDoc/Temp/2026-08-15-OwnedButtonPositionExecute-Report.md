# “查看拥有”按钮位置调整执行报告

## 任务结果

本次已定位到新的实际阻塞，但未落地按钮位置修改。Unity MCP 当前连接正常，Editor 状态可用；失败原因不再是上一对话的 `Transport closed`，而是项目在刷新后出现 7 个既有编译错误，导致 Unity 无法装载更新后的 `PreparationViewUiBuilder`。

目标坐标仍为 `(-770, 285)`：卡池面板宽 `1780`，左边缘为 `-890`；按钮宽 `240`，中心 X 为 `-770` 时左边缘同为 `-890`。当前 Builder 与 `PreparationView.prefab` 均保持 `(-720, 285)`，Prefab 尺寸仍为 `240 × 42`。

## 阻塞证据

强制刷新脚本后，Unity Console 返回 7 个 `CS1061`：`BattleRulesTests.cs` 仍引用 `BattleCardTypeCsvData.AttackAudioKey`、`AttackAudioDelay`、`HitDelay`，但另一组未完成改动已把数据类字段改为 `AttackAudioKeys`、`AttackAudioDelays`、`HitDelays` 数组。该问题属于正在进行的战斗攻击表现改动，与“查看拥有”按钮无关。

由于编译失败，调用 Builder 只会命中旧程序集并重建旧坐标。为避免配置源与 Prefab 不一致，本次曾准备的 Builder 单行坐标修改已经撤回；未修改 Controller/View，未手写 Prefab 或 `UiSceneAsset` YAML，也未创建、编辑或删除 `.meta`。

## 验证与检查结论

| 范围 | 状态 | 证据 |
| --- | --- | --- |
| Unity MCP/Editor 连接 | 通过 | `Hearthstone@e97c0c17` 正常运行，Editor 状态 `ready_for_tools=true`。 |
| 项目编译 | 未通过 | Unity Console 返回 7 个无关 `BattleRulesTests.cs` 编译错误。 |
| Builder 执行 | 未通过 | 更新后的程序集无法装载，未调用旧 Builder。 |
| Builder/Prefab 一致性 | 通过 | 两者最终均保持旧坐标 `(-720, 285)`。 |
| 按钮左移并贴齐 | 未通过 | 目标 `(-770, 285)` 已确认，但未落地。 |
| 框架边界 | 通过 | 未加入运行时布局补丁，未绕过 BbxCommon UI/UiBuilder 流程。 |
| 文档同步 | 不适用 | 玩家界面、Prefab 和当前实现均未改变，三类正式现状文档不更新。 |

## 后续条件与风险

需要先由负责战斗攻击表现改动的对话同步 `BattleCardTypeCsvData`、CSV 与 `BattleRulesTests`，让 Unity 完成无错误编译。之后再把 Builder 坐标改为 `(-770, 285)`、通过 Unity MCP 执行 `Hearthstone.PreparationViewUiBuilder.Build()`，并核对 Prefab/Editor 测试。未经修复直接执行旧程序集会静默生成旧坐标，这是当前主要风险。

## 清理

已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 0；本报告在清理后创建。
