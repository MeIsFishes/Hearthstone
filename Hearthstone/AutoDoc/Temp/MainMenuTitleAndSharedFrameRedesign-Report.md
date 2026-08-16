# 主菜单标题与共享红框重设计报告

## 1. 任务结果

已完成主菜单标题图片化与上移、开始按钮无框化、低饱和湿润羊皮纸悬停反馈，以及备战界面旧亮红金交互框的共用组件迁移。

- 主菜单标题由 TMP 改为独立 `MainMenuTitle.png` Image，位置由原中心 Y=`190` 上移至 Y=`285`。
- “开始游戏”常态只显示深棕文字；悬停与按下分别以 `42%`、`58%` Alpha 显示灰褐湿润羊皮纸纹理。
- 新增 `MedievalParchmentControl.png`，以低饱和旧羊皮纸、深胡桃木和暗古铜细边取代亮红漆、宝石和塑料高光。
- 备战阶段标题、出战/融合页签、继续、融合、智能推荐和推荐项“选择”均迁移到同一共用控件，并用 ColorTint 表现状态。
- 旧 `PreparationContinueButton*`、`PreparationFusionButton*`、`PreparationTab*V2` 和 `PreparationStageTitleFrame` 文件保留为历史资源，但三个当前 Prefab 不再引用。

## 2. 图片生成与接入记录

### 2.1 标题

- 模式：先基于用户参考图生成，随后以生成结果为引用进行低饱和重绘；因图像模型把透明棋盘绘入 RGB，最终执行确定性的亮背景 Alpha 提取与进一步去饱和处理。
- 核心提示要求：文字只能为“99升变”；旧黄铜灰、烟褐、暗木；中世纪雕刻与轻油画纹理；无边框、无附加文字、无红色、无亮金与塑料高光。
- 生成源：`C:\Users\黄昕玮\.codex\generated_images\01a00675-21c4-7d51-9610-8d7decc4c630\exec-7ba7bf34-3621-49df-b5dc-72966c672293.png`
- 项目路径：`Assets/Resources/Art/MainMenu/UI/MainMenuTitle.png`
- 审计：`1672 × 941`，平均非透明像素饱和度约 `0.194`，四角 Alpha 均为 `0`。

### 2.2 共用交互框

- 模式：新图生成，随后保留 Alpha 做确定性去饱和处理。
- 核心提示要求：横向中世纪 UI 控件；旧羊皮纸内面、深胡桃木雕边、暗古铜细边；无文字、红漆、宝石、亮金、霓虹和卡通塑料感。
- 生成源：`C:\Users\黄昕玮\.codex\generated_images\01a00675-21c4-7d51-9610-8d7decc4c630\exec-54257515-d14f-4ebb-a87e-6cecad669f51.png`
- 项目路径：`Assets/Resources/Art/Common/UI/MedievalParchmentControl.png`
- 审计：`2048 × 768`，平均非透明像素饱和度约 `0.232`，四角 Alpha 均为 `0`。

### 2.3 开始按钮悬停水渍

- 模式：新图生成，随后保留 Alpha 做确定性去饱和处理。
- 核心提示要求：低饱和灰褐湿痕、羊皮纸纤维扩散、透明软边；无边框、硬矩形、文字、图标、红色或亮金。
- 生成源：`C:\Users\黄昕玮\.codex\generated_images\01a00675-21c4-7d51-9610-8d7decc4c630\exec-cfbfb567-09b1-4437-b583-24179923dffa.png`
- 项目路径：`Assets/Resources/Art/MainMenu/UI/MainMenuStartHoverWetParchment.png`
- 审计：`2048 × 768`，平均非透明像素饱和度约 `0.044`，四角 Alpha 均为 `0`。

## 3. 检查项状态与证据

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 标题文字、风格与透明背景 | 通过 | 生成图目视复核；像素 Alpha 与饱和度审计；Prefab 引用 `MainMenuTitle` |
| 标题图片化并上移 | 通过 | `MainMenuView.GameTitle` 为 Image；EditMode 断言位置 `(0, 285)` |
| 旧亮红框使用位置检索 | 通过 | 检索代码、Prefab、Scene 与 Sprite GUID；确定当前交互使用集中在主菜单、备战页和推荐项 |
| 共用控件重设计 | 通过 | `MedievalParchmentControl.png` 与 `AddMedievalParchmentButton()` |
| 旧亮红交互框引用清零 | 通过 | `GeneratedUiPrefabsDoNotReferenceLegacyGlossyRedControls` 覆盖三个 Prefab；资源文本检索无当前引用 |
| 开始按钮常态无框与湿润悬停 | 通过 | ColorTint 常态/禁用 Alpha=`0`，悬停=`0.42`，按下=`0.58` |
| Builder 与框架边界 | 通过 | 由 `MainMenuViewUiBuilder`、`PreparationViewUiBuilder`、`FusionRecommendationItemUiBuilder` 重建；复用 BbxCommon View/Controller、UiList 与既有资源加载边界 |
| 图片导入设置 | 通过 | 三张新图均为 Single Sprite、Alpha Is Transparency、Mipmap 关闭、Clamp |
| 测试覆盖 | 通过 | `MainMenuTests` 和 `RunCardRulesTests` 新增标题、按钮状态、共用控件与旧引用清零断言 |
| Unity 验证 | 通过 | `Hearthstone.Tests` EditMode `95/95`；最终 Console `0` 错误 |
| 文档同步 | 通过 | 更新开始界面、主菜单美术、UI 美术总览、备战美术、主菜单程序和备战程序文档 |
| 无关改动保护 | 通过 | 未回滚工作树中既有的卡槽尺寸、拖拽、战斗界面等用户改动；只在重叠文件追加本任务所需变更 |

## 4. 验证记录

- Unity 强制刷新与编译：成功，编译错误 `0`。
- Builder：成功重建 `MainMenuView.prefab`、`PreparationView.prefab`、`FusionRecommendationItem.prefab`。
- Scene/UiSceneAsset：连接 Prefab 的场景结构、UiGroup 和导出映射未改变，不需重建或重导出。
- EditMode：作业 `0adf144793434a388d052be55d1ef95d`，总计 `95`，通过 `95`，失败 `0`，跳过 `0`。
- Console：测试中的负例 CSV 校验按预期写入异常日志；测试完成后清空，并确认最终错误条目 `0`。
- Play Mode：按项目默认要求未进入。

## 5. 偏差与风险

- 图像模型两次未为中文标题输出真实 Alpha，而是把透明棋盘绘入 RGB；已通过确定性亮背景去除生成 RGBA，并验证内容包围盒未扩展到画布边缘。
- 未进行 Play Mode 或实际游戏内截图验证；当前证据来自资源目视检查、Prefab 序列化检查、像素审计和 EditMode 测试。不同分辨率下的最终主观观感仍建议由用户在下次运行游戏时确认。

## 6. 清理结果

- 已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`。
- 退出码：`0`。
- 运行后 `AutoDoc/Temp/` Markdown 数量：`235`，低于清理阈值 `500`，未删除文件。
- 对应 Checklist 保留，并已逐项标记为通过。
