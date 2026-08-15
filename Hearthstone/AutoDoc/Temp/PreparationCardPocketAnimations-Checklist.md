# 备战卡牌收纳动效检查清单

## 1. 用户要求

- [通过] 融合结果揭晓停留后，玩家执行既有确认操作时不立即消失，结果卡快速向屏幕 `x=0` 的底端移动并缩小。
  - 证据：`StartFusionRevealPocket()` 与 `UpdateFusionRevealPocket()` 从当前结果卡变换进入 `0.36 s` 收纳段。
- [通过] 融合结果到达屏幕底端时缩放精确保留为 `0.3`，随后才关闭揭晓遮罩并复位。
  - 证据：`CardPocketFinalScale = 0.3f`；`ApplyPocketTransform()` 完成后才调用 `ResetFusionReveal()`。
- [通过] 每回合进入备战阶段获得新卡时，新增卡牌依次排在屏幕上等待玩家确认。
  - 证据：奖励状态机读取 `WasNewlyApplied` 与 `RewardCards`，按 `0.14 s` 间隔从底端发入中央横排。
- [通过] 新卡展示期间使用灰色全屏底板压暗背景并阻挡备战页其他操作。
  - 证据：`RewardRevealOverlay` 为 `78%` Alpha 灰色全屏 Image，CanvasGroup 始终阻挡射线。
- [通过] 玩家确认新卡后，卡牌按顺序逐张滑向屏幕 `x=0` 的底端，并在完成后关闭展示层。
  - 证据：`RewardRevealPocketStagger = 0.11f`，每张卡以 `0.34 s` 收纳，全部完成后清空列表并关闭遮罩。
- [通过] 为融合结果收纳和每张新卡收纳配置合适的移动/入袋音效。
  - 证据：融合结果与奖励卡收纳播放 `handleSmallLeather`；奖励卡滑入播放 `card-place-1`。
- [通过] 摸牌展示层上方显示内容精确为“获得卡牌”的生成式艺术字图片，风格与现有中世纪红金卡牌界面一致。
  - 证据：生成并导入 `PreparationRewardTitle.png`；图像人工检查为“获得卡牌”四字、金红中世纪轻油画风。
- [通过] 卡牌库“查看拥有”默认勾选，页面首次打开与重新打开时默认只显示已拥有卡牌；玩家仍可取消勾选查看完整总览。
  - 证据：Builder 的 `toggle.isOn = true`，Controller 打开态同步 `m_ShowOwnedOnly = true` 与 `SetIsOnWithoutNotify(true)`；原切换回调保留。

## 2. 现状与交互边界

- [通过] 核对融合揭晓现有等待点击、Tooltip、关闭与音频生命周期，新增收纳段不破坏结果卡悬停词条。
  - 证据：等待确认时仍启用共享卡面悬停；只有卡外 Button 触发收纳，收纳开始后才关闭悬停。
- [通过] 核对每回合备战奖励批次数据与页面打开时机，只展示本轮实际新摸到的卡，不把既有牌库全量重复展示。
  - 证据：仅绑定 `PreparationSessionSingletonRawComponent.RewardCards`；批次 ID 包含本局序号与轮次，确认后记录防重。
- [通过] 明确确认输入沿用无提示文字的全屏卡外点击，不新增“按任意键继续”或确认按钮文本。
  - 证据：两个遮罩使用无文本全屏 Button；卡面射线阻止事件冒泡，艺术字关闭射线。
- [通过] 页面隐藏、关闭、Stage 切换、重复打开或状态变化时可以安全终止并复位展示、收纳和音效。
  - 证据：`OnUiHide()`、`OnUiClose()` 调用两类 Reset，并通过 `AudioApi.StopGroup()` 停止卡牌动画音效。

## 3. UI、对象池与代码质量

- [通过] 静态遮罩、卡牌容器和序列化引用由 `PreparationViewUiBuilder` 固化到 `PreparationView.prefab`，不在 Controller 运行时拼装静态层级。
  - 证据：Builder 已通过 Unity Editor 入口重建 Prefab；运行时代码仅切换状态与创建池化条目。
- [通过] 艺术字作为透明 PNG 导入现有 Preparation UI 资源目录，由 Builder 静态引用；不以 TMP 或运行时生成方式替代。
  - 证据：`Assets/Resources/Art/Preparation/UI/PreparationRewardTitle.png` 由 `RewardTitle/Image` 直接引用。
- [通过] 新卡与融合结果继续复用 `BattleCardItemController → Ui/BattleCardItem` 预加载映射和 `UiList` 对象池，不直接管理对象池或重复创建完整卡面。
  - 证据：奖励、素材和结果均通过 `UiList.ItemWrapper.AddItem<BattleCardItemController>()` 与 `ClearItems()` 管理。
- [通过] View 只保存必要引用；Controller 负责确认输入、状态机、位置/缩放时间轴与音效句柄。
  - 证据：View 只新增奖励遮罩、CanvasGroup、Button、UiList 四个引用；静态艺术字未新增 View 字段。
- [通过] 默认筛选状态同时由 Prefab Toggle 与 Controller 打开态同步，避免视觉勾选与实际列表过滤不一致。
  - 证据：Prefab 检查 `OwnedOnlyToggle.isOn=True`，Controller 打开态同步相同布尔值。
- [通过] 动画终点使用遮罩坐标系中的屏幕底端与 `x=0`，兼容 CanvasScaler；不依赖世界坐标硬编码。
  - 证据：`GetPocketTarget()` 使用遮罩和卡片 RectTransform 的局部矩形计算端点。
- [通过] 新增状态、字段和 helper 具有明确复用或职责价值，无一次性抽象、重复时间轴或不可达分支。
  - 证据：融合与奖励共用 `GetPocketTarget()`、`ApplyPocketTransform()` 和音频 helper；奖励使用单一枚举状态机。

## 4. 音频规范

- [通过] 完整读取 `bbxcommon-audio` 的 playback 与 selection 规范并核对实际 `AudioApi` 接口。
  - 证据：播放统一使用 `AudioApi.Play`、`AudioPlayOptions` 与 `AudioApi.StopGroup`，未直接创建 AudioSource。
- [通过] 从现有音频库选择并检查最终候选的时长、格式、basename 唯一性与听感角色。
  - 证据：`card-place-1.ogg` 唯一且约 `0.689 s`；`handleSmallLeather.ogg` 唯一且约 `0.338 s`，分别对应落位与皮革收纳。
- [通过] 通过 `AudioApi.Play` 使用稳定 GroupKey、ConcurrencyKey、音量、优先级与并发上限；页面生命周期结束时停止有效句柄或分组。
  - 证据：共用 `UiPreparationCardAnimation` 分组，按 Deal/Pocket/FusionPocket 分并发键，Reset 调用 StopGroup。
- [通过] 高频逐张收纳音效不会无上限叠加或遮盖融合揭晓提示音。
  - 证据：奖励逐张音效 `MaxConcurrent = 3` 并设置音量衰减；融合收纳 `MaxConcurrent = 1`。

## 5. 框架边界审计

- [通过] 未绕过 View-Controller、UiBuilder、UiList、AudioApi、Resources、Prefab 配置源或 Unity 导入流程。
  - 证据：静态层级走 Builder，动态条目走 UiList，声音走 AudioApi，图片走 Resources 与 TextureImporter。
- [通过] 未直接编辑 `UiSceneAsset`、未手写 Prefab/Scene YAML、未在运行时查找名称维持页面引用、未建立平行 UI/音频体系。
  - 证据：Prefab 由 Unity 执行 Builder 生成；代码变更未触及 UiSceneAsset、Scene 或底层管理器。
- [通过] 未改变 UiGroup、DefaultShow、页面整体 Transform 或导出信息；若发生变化则按 UiSceneExporter 流程同步。
  - 证据：只修改页面 Prefab 内部子层和运行时逻辑，`Preparation.asset` 与 UI Scene 均未变更。
- [不适用] 若发现框架能力缺口，按最小范围处理并记录证据。
  - 证据：现有 View/Controller、UiBuilder、UiList 与 AudioApi 已完整覆盖需求，无框架缺口。

## 6. 验证

- [通过] Unity 编译通过并在清空日志后保持 Console 0 error。
  - 证据：最终 `refresh_unity` ready；清空后 `read_console(types=error)` 返回 0 条。
- [通过] 新增/更新 EditMode 测试覆盖融合结果收纳、终点 `0.3`、新卡批次展示、逐张次序、确认阻挡、音效键与生命周期复位。
  - 证据：定向任务 `29540067722440cd99733d05e88d3ced` 的三项相关测试全部通过。
- [通过] Prefab 结构、序列化引用、UiList 生命周期与 Resources 音频键验证通过。
  - 证据：共享 Prefab/资源测试通过；Unity 结构检查确认奖励层、UiList、层级顺序与默认筛选。
- [通过] 艺术字图片文字准确、透明背景、导入设置、Prefab 引用、层级位置与射线关闭状态验证通过。
  - 证据：源图 `2079 × 756`、32-bit ARGB、透明角；导入为 Single Sprite、Alpha Is Transparency、无 Mipmap、Clamp；Prefab 位于 `(0,270)`、`620 × 225`、raycast=false。
- [通过] 卡牌库默认筛选的 Prefab 与 Controller 行为测试通过。
  - 证据：定向任务 `00a1d9bfddaf46df82f2b30326ddbdcb` 通过；Prefab 结构检查 `OwnedOnlyToggle.isOn=True`。
- [不适用] 按项目默认不主动进入 Play Mode；记录未执行游戏内人工观感验收的风险。
  - 证据：未进入 Play Mode；仍需玩家在游戏内最终确认动画速度、艺术字视觉比例与音效混合听感。

## 7. 文档同步门槛

- [通过] 完整读取玩家视角设计文档格式 skill，核对并同步备战摸牌展示与融合收纳的当前玩家体验。
  - 证据：已更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`，包含奖励标题、收纳和默认筛选。
- [通过] 完整读取美术文档格式及模块格式，核对灰色遮罩、卡牌排布、运动终点与音效是否影响美术文档并按需同步。
  - 证据：已更新模块美术文档的模块风格、UI 分组、参考图和已有资产清单。
- [通过] 完整读取并遵循 `imagegen` skill；记录内置生成模式、最终 prompt、工作区保存路径与人工图像检查结果。
  - 证据：使用内置 image_gen 生成；最终素材保存到 `Assets/Resources/Art/Preparation/UI/PreparationRewardTitle.png`，prompt 与检查结果写入任务报告。
- [通过] 完整读取程序文档格式及 UI 界面格式，更新新增 Prefab 容器、View 引用、Controller 状态机、奖励批次来源和音频接入。
  - 证据：已更新 `AutoDoc/Program/UI/preparation/preparation.md` 的三段固定结构内容。

## 8. 结束审计与报告

- [通过] 逐项以实际代码、Prefab、资源、Unity 编译与测试证据复核，并修正可修正缺口。
  - 证据：首次新增图片测试发现 Unity 导入尺寸受 2048 上限调整，已改为合理下限断言并重跑通过。
- [通过] 结束时只运行一次 `AutoDoc/CleanupTempDocs.bat` 并记录退出结果。
  - 证据：已执行一次，退出码为 `0`；未重复运行。
- [通过] 清理后创建 `AutoDoc/Temp/PreparationCardPocketAnimations-Report.md`。
  - 证据：清理结束后已创建同任务名报告，包含结果、验证、偏差、风险、文档与清理记录。
