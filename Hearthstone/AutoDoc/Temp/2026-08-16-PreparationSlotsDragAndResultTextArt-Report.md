# 备战槽位、拖拽回牌库与结算 UI 美术任务报告

## 1. 任务结果

本任务已完成。备战出战槽位进一步缩小并形成明确间隔；出战卡拖离原槽且未成功落入其他槽时会退回牌库；“已出战”、单场胜利、失败、整局胜利与“返回主菜单”按钮均已换成生成图片。结算按钮不再重新开始整局，而是进入现有主菜单 StageGroup。

## 2. 实现与接入

- 出战列表：`1110 × 320`，固定步距 `185 × 266.4`，占用卡面填充比例 `0.76`，实际卡面约 `140.6 × 202.464`，横向可见间隙约 `44.4 px`。
- 拖离规则：`RunCardRules.TryRemoveCardFromBattleSlot()` 校验来源槽和预期卡号后只清空阵容槽并递增 `Revision`；成功换槽后的回调不会误删目标槽卡牌。
- “已出战”：`BattleCardItem.prefab` 的 `PreparationDeployedState` 改为 `Image`，无 TMP；显示尺寸 `120 × 44`。
- 胜利横幅：`BattleView.prefab` 使用完整图片，不再创建标题 Label；继续按左入、停留、右出的既有时序播放。
- 失败/整局胜利：面板运行时只交换完整 Sprite，不再叠加标题或正文 TMP。
- 返回主菜单：结果面板共用图片按钮，点击后先禁用自身，再调用 `HearthstoneGameEngine.EnterMainMenuStageGroup()`。
- 资源加载：业务 Controller 通过 `ResourceApi.LoadSprite()` 切换结果面板，不直接使用 `Resources.Load()`。

## 3. 最终图片资产与提示词摘要

全部使用内置 imagegen 的全新图片生成模式，输出为透明 RGBA PNG；生成源保留在 `C:\Users\黄昕玮\.codex\generated_images\01a0066d-3fad-7c41-b0e8-63df50f11604\`。

| 资产 | 项目路径 | 源文件 | 最终提示词摘要 |
| --- | --- | --- | --- |
| 已出战状态图 | `Assets/Resources/Art/Preparation/UI/PreparationDeployedText.png` | `exec-92177dc2-23ec-4d4e-95e5-7bf0673a41ad.png` | 黄色“已出战”、粗糙旧木板、轻油画、真实透明背景、无额外字符与装饰 |
| 单场胜利横幅 | `Assets/Resources/Art/BattleCards/Result/BattleVictoryBannerAged.png` | `exec-dffd1233-9607-46a8-b7bd-022cca745634.png` | 蓝金旧布、旧木与暖羊皮纸、整合“胜利”、克制经典奇幻卡牌轮廓、避免塑料与卡通感 |
| 失败完整面板 | `Assets/Resources/Art/BattleCards/Result/BattleDefeatPanelAged.png` | `exec-9ebf1418-35a1-4bc8-a117-263e49d9ac64.png` | 暗红旧布、黑铁、污损羊皮纸，整合“失败”“本局冒险已经结束”，下部留按钮区 |
| 整局胜利完整面板 | `Assets/Resources/Art/BattleCards/Result/RunVictoryPanelAged.png` | `exec-8e61e768-d372-41b8-b9f4-f321059fbdaa.png` | 蓝金旧布、旧木与羊皮纸，整合“大获全胜”“恭喜完成全部轮次”，下部留按钮区 |
| 返回主菜单按钮 | `Assets/Resources/Art/BattleCards/Result/ReturnToMainMenuButtonAged.png` | `exec-0400aed7-030f-4866-ab73-6236d4f04481.png` | 深胡桃木、暗红旧皮革、黑铁与低饱和古金，整合“返回主菜单”，克制奇幻卡牌风格 |

五张源图均检查为 `Format32bppArgb`，四角 Alpha 为 `0,0,0,0`。Unity 导入后均为 Single Sprite、`alphaIsTransparency=true`、MipMap 关闭；横向 2172 像素源图按项目默认最大尺寸导入为 2048 像素宽。淘汰的 `BattleVictoryText.png`、`BattleDefeatText.png`、`RunGrandVictoryText.png` 和 `RestartButton.png` 草稿已通过 Unity AssetDatabase 删除。

## 4. 文档同步

- 美术：更新 `art-style-overview.md`、`ui-art-overview.md`、战斗卡牌、备战卡池、主菜单和 Loading 模块中的 UI 风格分组名称及资源现状。
- 玩家设计：更新战斗结算返回主菜单、完整结算面板表现、已出战状态图与出战槽拖离规则。
- 程序：更新 Battle/Preparation UI、战斗系统与备战卡池规则文档，记录 `ResourceApi`、Prefab 静态结构、拖离规则和 StageGroup 跳转。

## 5. 检查清单状态与证据

检查清单全部项目通过或不适用。主要证据：

- Unity Prefab 结构：步距 `185`、卡面宽 `140.6`、间隙 `44.4`；五张目标 Sprite 引用正确；旧标题、正文与按钮 Label 不存在。
- 规则与源码测试：覆盖出战放置、替换、换槽、预期卡号保护、退回牌库及返回主菜单调用。
- 框架边界：继续使用单一 View/Controller、`UiList`、预加载映射与对象池；静态节点由 Builder 生成；UiScene 导出属性未变化，因此未重新导出。

## 6. 验证结果

- Unity EditMode：`95/95` 通过，0 failed，0 skipped。
- `dotnet build Hearthstone.csproj --no-restore`：0 error，8 个既有 Unity 依赖版本 MSB3277 warning。
- `dotnet build Hearthstone.Editor.csproj --no-restore`：0 error，8 个既有 Unity 依赖版本 MSB3277 warning。
- Unity 刷新编译：通过。
- Unity Console：测试预期异常日志清理后 0 error。
- 活动场景：`Assets/Scenes/Main.unity`。
- 场景状态：`Dirty=False`，`Playing=False`。
- `git diff --check`：目标代码与正式文档无空白错误，仅有工作区 LF/CRLF 提示。

## 7. 偏差、风险与清理结果

- 首次并行执行两个 `dotnet build` 时因共享 `obj` 输出发生文件锁；改为顺序执行后两个构建均通过，此问题不属于项目代码错误。
- 按项目约定未进入 Play Mode，因此未执行游戏内鼠标拖拽手感、横幅实际屏幕占比和按钮点击的人工视觉验收；编辑器结构、规则测试与源码跳转检查均通过。
- `AutoDoc/CleanupTempDocs.bat` 已且仅已运行一次，正常退出；本任务 Checklist 保留并完成终审，Report 在清理后创建。
