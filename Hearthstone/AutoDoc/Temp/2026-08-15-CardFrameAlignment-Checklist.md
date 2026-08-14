# 卡牌边框与卡面对齐检查清单

## 任务目标与范围

- [x] 通过：卡牌基础边框、攻击者高亮边框和目标高亮边框均与 `BattleCardItem` 根节点完全拉伸对齐，三者 `sizeDelta` 均为 `(0, 0)`。
- [x] 通过：敌方运行时使用窄边 `CardFrame-v3`，我方使用同轮廓窄边 `CardFrameBlue-v2`；Controller 在绑定阵营框时同步把同一 Sprite 赋给攻击者和目标高亮层，战斗状态切换不再改用旧粗框。
- [x] 通过：未修改战斗交互、ECS 数据、结算、列表槽位或 UiScene 导出信息；仅修改卡框静态配置源、状态表现绑定和相应测试/文档。
- [x] 通过：Prefab 由 Unity Editor API Builder 经当前会话 `unityMCP execute_code` 保存，未手写 Scene、Prefab 或 `.asset` YAML；未手工创建、编辑或删除 `.meta` 文件。

## 现状定位与实现

- [x] 通过：定位到 `BattleCardItem.prefab`、`BattleCardItemView/Controller`、`CardFrame-v3.png`、`CardFrameBlue-v2.png` 和历史粗框 `CardFrame-v2.png`。
- [x] 通过：确认错位由框层外扩导致：基础框原 `sizeDelta=(14,16)`，攻击/目标高亮原 `sizeDelta=(26,28)`；后两者还引用历史粗框 `CardFrame-v2`。
- [x] 通过：新增一一对应的 `BattleCardItemUiBuilder.Build()`，统一三层框锚点、位置、尺寸、Sprite 类型和 Raycast 配置，并通过正式 MCP 通道更新 Prefab。
- [x] 通过：`BattleCardItemController` 继续通过既有 `ResourceApi` 读取阵营窄框，同时让状态层复用当前阵营 Sprite；未新增一次性运行时布局逻辑。
- [x] 通过：静态扫描确认 `BattleCardItem.prefab` 已无 `CardFrame-v2` GUID/名称引用；本次修改范围限定在卡牌 Prefab、对应 Builder/asmdef、Controller、测试和直接相关文档。

## Skill 与框架边界

- [x] 通过：已遵循 `project-state-preflight` 的修改项目、检查清单、文档同步、结束审计和报告流程。
- [x] 通过：已遵循 `bbxcommon-ui`；静态布局落在 Prefab 与一一对应 UiBuilder，动态阵营选择仍由 Controller 负责，未绕过 View/Controller、UiList 或资源 API。
- [x] 不适用：未新增或修改 `BbxUiItem` 自定义组件，仅调整 Unity `Image`/`RectTransform` 与业务 Controller 的现有引用，因此 `bbxcommon-ui-item` 的 `AutoDoc/UIItem/` 文档无需修改。
- [x] 通过：框架边界审计未发现运行时拼装静态 UI、直接访问底层 UI Manager、手写导出资产、平行资源加载或 UiScene 配置源绕行。
- [x] 通过：曾误启动隔离 batchmode Unity；发现 §2.7 要求后立即终止且未采用该副本产物，随后通过 `recover-unity-mcp` 在完整工具目录中找到正式延迟工具，并用当前会话 `unityMCP execute_code` 执行 Builder。隔离副本与日志已清除。

## 验证

- [x] 通过：Unity 强制刷新并完成编译，修正本次新增的 asmdef 引用与测试 `Random` 别名后，最终 Console 为 0 error。
- [x] 通过：MCP Prefab 探针结果为 `prefab=True, red=True, blue=True, baseAligned=True, attackerAligned=True, targetAligned=True, stateFramesShareNarrowDefault=True`。
- [x] 通过：`Hearthstone.Tests` EditMode 共 15 项，15 通过、0 失败、0 跳过；新增测试覆盖三层窄框 Sprite 一致性与 RectTransform 对齐。
- [x] 通过：最终 `manage_scene(action="get_active")` 返回 `Assets/Scenes/Main.unity`、`isDirty=false`；最终 `read_console(types=["error"])` 返回 0 条。
- [x] 不适用：按项目默认授权未进入 Play Mode；未执行游戏内动画期间的目视回归，保留为交付风险。

## 文档同步门槛

- [x] 通过：完整读取 `design-doc-format`、战斗系统子格式、`art-doc-writer`、UI/模块美术格式、`program-doc-format` 与 UI 界面格式。
- [x] 通过：玩家视角设计文档已更新 `AutoDoc/Design/Specific/combat-system/combat-system.md`，记录攻击/目标状态沿用阵营窄边轮廓且不会切换粗框。
- [x] 通过：美术文档已更新 `AutoDoc/Art/UI/ui-art-overview.md` 与 `AutoDoc/Art/Modules/battle-card/battle-card.md`，明确状态层复用窄框、同尺寸覆盖以及历史粗框不再被 Prefab 引用。
- [x] 通过：程序文档已更新 `AutoDoc/Program/UI/battle/battle.md`，记录三层 RectTransform、运行时阵营 Sprite 同步和 `BattleCardItemUiBuilder` 配置源。
- [x] 不适用：项目级美术风格未变化；未生成或修改任何图片资产，因此 `art-style-overview.md` 无需同步。

## 结束审计

- [x] 通过：已重新打开并逐项核对本清单，证据来自当前文件、Unity MCP 结构探针、Editor 状态、Console 和测试结果。
- [x] 通过：将在本清单复核完成后仅运行一次 `AutoDoc/CleanupTempDocs.bat`，实际退出结果写入最终报告。
- [x] 通过：清理后创建 `AutoDoc/Temp/2026-08-15-CardFrameAlignment-Report.md`，记录结果、证据、验证、偏差、风险、文档处理和清理结果。
