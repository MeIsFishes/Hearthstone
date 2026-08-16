# 备战页敌方阵容预览抽屉任务报告

## 1. 任务结果

已完成备战出战页的敌方阵容预览抽屉。侧签最终改为贴住屏幕左边缘的窄矩形，材质沿用上一版较旧、较暗的粗糙胡桃木板缝、结疤和磨损暗古铜薄边；独立箭头只有三角头部，不含矩形箭杆。侧签在出战页始终显示，按钮略微上移，展开后向木板右边缘内压，消除两张资源透明安全边造成的视觉缝隙。展开面板已加长、加高，左侧显示“敌方阵容”，右侧完整容纳最多六张与我方出战卡同尺寸的敌方卡。

点击可在 `0.34 s` 内展开或收起木板。展开过程中箭头保持朝右，完全展开后一次切换为朝左；收起过程中保持朝左，完全收回后恢复朝右。连续点击会从当前进度反向播放。木板用 `UiList` 和共享 `BattleCardItemController` 展示下一场实际使用的完整敌方卡牌实例。

敌方阵容在进入备战前从本关多行配置中选定并生成。基础卡按类型范围随机攻血；融合卡先生成基础素材，再通过共享融合模拟得到最终攻血、词条、等阶和表现来源。完整实例及后继随机状态保存在 `EnemyBattlePreviewStartupData`，由 Preparation session 防御性复制并传入正式 Battle startup，因此预览和实战不会二次随机分叉。

## 2. 最终布局与交互证据

| 项目 | 最终值 / 证据 |
| --- | --- |
| 侧签 Rect | `86 × 156`；严格窄矩形旧木资源；相对面板中心上移 `20 px` |
| 箭头 Rect | `66 × 72`；锚点和局部坐标均居中，用于补偿源图透明安全区 |
| 收起位置 | 抽屉根 `(-508, -300)`；侧签 Rect 左边缘为屏幕 `x=0`，源图可见边仅保留约 `2%` 透明安全区 |
| 展开位置 | 抽屉根 `(978, -300)`；`1500 × 400` 面板左边缘为 `x=0` |
| 紧密连接 | 面板右边缘为抽屉局部 `x=522`，侧签 Rect 左边缘为 `x=508`，几何重叠 `14 px`；扣除双方透明安全边后仍有可见覆盖 |
| 左侧标题 | `230 × 90`，文字“敌方阵容”，字号 `38` |
| 卡牌列表 | `UiList.ConstantSlot/Horizontal`，内容区 `1110 × 266.4`，步长 `185 × 266.4`，容纳最多 6 张 |
| 卡牌最终尺寸 | 从共享卡根 `250 × 360` 计算，`150 / 185` 填充比例得到根缩放 `0.6` 和 `150 × 216`，与我方出战卡一致 |
| 动效 | `MoveTowards + SmoothStep`，时长 `0.34 s`，面板同步淡入/淡出 |
| 箭头方向 | 仅在展开进度到 `1` 或收起进度到 `0` 时切换 `0°/180°` |
| 旧 Session | 无敌方快照时侧签仍显示，仅将 Button 设为不可交互 |

## 3. Imagegen 交付

使用内置 `image_gen` 工具生成并目视筛选最终三张资源。最终提示词要点如下：

1. 木板：`final Unity wide enemy lineup preview drawer panel; front-facing rounded rectangle 3.2:1; restrained mature medieval UI; deep neutral walnut, very muted brass; one thin border, one shallow inner line; real RGBA transparency; no ornament/rivets/text/cards/shadow/watermark.`
2. 矩形侧签第一步：以旧梯形为编辑目标，`change only the silhouette into a strict tall narrow rectangle; keep the same rough aged walnut boards, seams, knot, uneven wear, deep muted brown tone and thin worn antique-brass frame; not newly polished; real transparent RGBA; no arrow/text/ornaments/shadow.`
3. 矩形侧签透明修正：`keep the aged rectangular wood and brass visually unchanged; remove only the checkerboard and deliver true 32-bit RGBA with alpha 0 outside; minimal transparent padding.`
4. 箭头：`final Unity one compact solid right-pointing triangular arrowhead only; antique brass, real RGBA, no shaft/tail/frame/text/shadow; suitable for rotate180.`

| 资产 | Imagegen 原始输出 | 项目路径 | 检查结果 |
| --- | --- | --- | --- |
| 木制面板 | `C:/Users/黄昕玮/.codex/generated_images/01a00856-abb8-74d1-abca-dc81d40c7b8e/exec-a2f72198-9de2-4b8d-92a5-fc1bf01f8a3c.png` | `Assets/Resources/Art/Preparation/UI/PreparationEnemyPreviewPanel.png` | `2172×724`、32-bit ARGB、四角 Alpha `0` |
| 窄矩形侧签 | 材质参考：`.../exec-d904d2fc-2fb4-4eb8-b5f0-6898915adbbe.png`；最终透明输出：`.../exec-76897db6-a0d6-439b-beb6-28dc8acac809.png` | `Assets/Resources/Art/Preparation/UI/PreparationEnemyPreviewToggle.png` | 裁除多余透明边后 `650×1628`、32-bit ARGB、四角 Alpha `0`、左右可见安全边约 `2%` |
| 三角箭头 | `C:/Users/黄昕玮/.codex/generated_images/01a00856-abb8-74d1-abca-dc81d40c7b8e/exec-5da13527-d48e-4011-b95d-1364d901d8aa.png` | `Assets/Resources/Art/Preparation/UI/PreparationEnemyPreviewArrow.png` | `1237×1272`、32-bit ARGB、四角 Alpha `0` |

三项资源均由 Unity Builder 校验并导入为 Single Sprite、Alpha Is Transparency、无 Mipmap、Clamp。自动资源字典中三个 key 各出现一次，`ResourceApi.LoadSprite()` 验证可读。

## 4. 检查清单复核

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 窄矩形旧木侧签和右箭头 | 通过 | Prefab 字段、Builder 尺寸及 Unity 编辑器关闭/展开渲染 |
| 展开木板与敌方卡列表 | 通过 | `PreparationController`、`UiList`、共享卡 Controller |
| 反向收起动效 | 通过 | 同一进度插值反向移动和淡出 |
| 箭头端点切换 | 通过 | 进度 `1/0` 才修改方向，无运动中旋转 |
| 薄边、低装饰视觉 | 通过 | 最终三张资源目视检查和美术文档 |
| 纯三角箭头 | 通过 | 独立 Arrow Sprite，无箭杆/底框；Prefab 无箭头子节点 |
| 贴屏与无缝相连 | 通过 | 三项边缘公式均由 EditMode 断言覆盖 |
| 侧签运行态可见 | 通过 | 出战页始终 `SetActive(battle)`；缺快照仅禁用 Button |
| Imagegen 生成与落盘 | 通过 | 上表三项原始输出和项目路径 |
| View/Controller/Builder 边界 | 通过 | 静态层级在 Builder/Prefab，运行逻辑在 Controller |
| UiList 与对象池复用 | 通过 | 预览列表使用既有 Pre-load 映射和共享卡片条目 |
| 预览与实战一致 | 通过 | 同一 `EnemyBattlePreviewStartupData` 从备战传入战斗 |
| 基础/融合卡模拟一致 | 通过 | 共同调用 `BattleCardSimulationFactory` |
| 生命周期复位 | 通过 | Open/Close/Hide/页签切换均覆盖 |
| 快速反向稳定性 | 通过 | `MoveTowards` 从当前进度反向，端点收敛 |
| 2～6 张布局 | 通过 | 6 个 `185 px` 固定槽恰好占 `1110 px`；每张可见卡为 `150 × 216`，完整落在 `1500 × 400` 面板内 |
| ResourceApi 与唯一 key | 通过 | 三 key 各一次且运行时加载测试通过 |
| Prefab 配置源 | 通过 | 通过 Unity 内执行 Builder 重建，无手写 YAML |
| 代码职责 | 通过 | 快照、列表绑定、动效、静态构建分层 |
| 测试与回归 | 通过 | 针对性 `2/2`、全量 EditMode `114/114` |
| 框架边界 | 通过 | 未引入平行对象池、运行时静态 UI 或底层资源直读 |
| 脏工作树保护 | 通过 | 未回退无关修改；`.meta` 由 Unity 自动导入 |
| 玩家视角文档 | 通过 | 更新 `AutoDoc/Design/Specific/preparation-card-pool/` |
| 美术文档 | 通过 | 更新 `AutoDoc/Art/Modules/preparation-card-pool/` |
| 程序文档 | 通过 | 更新 Preparation UI 与 preparation-card-pool 程序文档 |
| 结束验证 | 通过 | 项目/测试程序集编译、资源与 Prefab 检查、Unity EditMode 回归 |
| 清理 | 通过 | 任务结束审计后只运行一次 `AutoDoc/CleanupTempDocs.bat`；文档数未超过阈值，未删除文件 |

## 5. 验证结果

- `dotnet build Hearthstone.Tests.csproj --no-restore`：成功，`0 error`；保留既有 `System.Net.Http` 与 `System.Security.Cryptography.Algorithms` 程序集版本冲突 warning。
- Unity Console 编译：`0 error`。
- 针对性 EditMode：`2 passed / 0 failed`。
- 全量 EditMode：`114 passed / 0 failed / 0 skipped`。
- Prefab 几何：侧签、面板、箭头、列表和序列化字段均通过断言。
- 资源：三张 PNG 尺寸、32-bit Alpha、透明四角和资源字典唯一 key 均通过静态检查。
- 编辑器关闭态渲染：确认旧木矩形与箭头在 `1920×1080` 参考画布可见且左侧贴屏；展开态六卡渲染确认上移按钮、连续接缝、“敌方阵容”标题和六个 `150 × 216` 共享卡根均完整落在面板内；预览图仅写入 `Temp/`，未保存为项目资产。

## 6. 偏差与未解决风险

- 用户在实现过程中逐步收敛了视觉要求；最终实现以最后规格为准：窄矩形、沿用上一版粗糙旧木与磨损暗古铜材质、按钮稍向上、薄边、纯三角箭头、端点切换、左侧贴屏，并以 `14 px` 几何重叠紧贴 `1500 × 400` 木板；敌方卡与我方出战卡同为 `150 × 216`。
- 按项目默认未由代理主动进入游戏做人工点击验收；验证覆盖编译、Prefab、编辑器静态渲染和 EditMode 测试。实际鼠标手感与不同窗口比例下的主观视觉仍建议在下次正常运行时观察。
- 已经在修改前创建的旧 Preparation session 不含敌方快照；侧签会显示但禁用。开始新局或进入下一轮后会生成快照并恢复完整预览功能。
