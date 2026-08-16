# 战斗场景装饰可见性调整报告

## 任务结果

- 左上角匕首维持水平镜像，刀尖仍朝左下；位置由初始 `(80, -20)` 最终调整为 `(220, -280)`，尺寸保持 `420 × 630`，主体几乎完整露出，仅手柄最顶端轻微越过画布上缘。
- 战斗主面板新增专用底图 `Assets/Resources/Art/BattleCards/UI/BattleBoardBackgroundAged.png`，直接绘入可见的不规则淡褐水渍、磨痕、短划痕与边缘积垢；原共享底图 `BattleBoardBackground.png` 已保持原貌，备战推荐与卡牌词条窗不受影响。
- 战斗面板继续叠加 `ParchmentAgingOverlay.png`，Alpha 从 `24%` 提高到 `42%`，并保持在卡牌与交互层下方且不接收射线。
- `BattleViewUiBuilder` 已改为引用战斗专用做旧底图并保存最终布局，Unity 已重新生成 `Assets/Resources/Ui/BattleView.prefab`。
- 设计、美术总览、UI 美术、战斗卡美术模块与战斗 UI 程序文档均已同步当前实现。

## 检查项状态与证据

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 匕首位于左上角且刀尖朝左下 | 通过 | 左上锚点与 `localScale.x = -1` 保持不变，测试通过 |
| 匕首几乎完整露出且仍有轻微裁切 | 通过 | Prefab 序列化位置 `(220, -280)`，尺寸 `420 × 630` |
| 主面板做旧清晰可见 | 通过 | 新底图本身带可见旧化纹理，并叠加 `42%` 透明做旧层 |
| 不影响其他复用界面 | 通过 | 战斗引用 `BattleBoardBackgroundAged.png`，原 `BattleBoardBackground.png` 的 Git 状态保持干净 |
| 装饰不遮断卡牌交互 | 通过 | 角落装饰与做旧层不接收射线，且均在卡牌列表前创建 |
| Builder 与 Prefab 一致 | 通过 | Unity 执行 `BattleViewUiBuilder.Build()` 成功，Prefab 值与 Builder 一致 |
| 自动化测试 | 通过 | 两个针对性 EditMode 测试共 `2/2` 通过 |
| 文档同步 | 通过 | 五份相关设计、美术与程序文档已同步 |
| 无关改动保护 | 通过 | 未回退工作区其他改动，未手工编辑 `.meta`；新 Meta 由 Unity 导入生成 |
| 最终审计 | 通过 | 新图片为 `1672 × 941`，Prefab、源码、测试断言、旧值残留和文档均已复核 |
| 临时文档清理 | 通过 | 终审后仅执行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 `0` |

## 验证结果

- `Hearthstone.Tests.BattleRulesTests.BattleViewUsesCornerDecorationsBehindCards`：通过。
- `Hearthstone.Tests.BattleRulesTests.PreparationAndBattleShareParchmentAgingOverlayWithStrongerBattleWeathering`：通过；同时验证战斗背景 Sprite 为 `BattleBoardBackgroundAged`。
- Unity 测试任务 `b3f77228569747f7ba1158f6ca903f84`：`2 passed / 0 failed / 0 skipped`。
- 任务相关 C# 与 Markdown 文件通过 `git diff --check`；Unity 生成的 Prefab 保留序列化空字段尾随空格。

## 图像生成记录

- 模式：内置 `image_gen` 编辑模式（`precise-object-edit`）。
- 编辑目标：`BattleBoardBackground.png`；最终作为独立战斗资产保存到 `Assets/Resources/Art/BattleCards/UI/BattleBoardBackgroundAged.png`。
- 最终提示：仅增强大面积内侧羊皮纸表面的可见旧化，加入不规则棕色水渍、褪色斑、磨痕、短划痕、折痕和边缘积垢；在 `1920 × 1080` 显示时仍可辨认，同时保持中央卡牌区清晰；严格保留木框、金边、蓝宝石、构图、比例、色彩和光照，不加入文字、卡牌、人物、武器、羽毛笔、印章、新装饰、破洞或大面积撕裂。

## 偏差与未解决风险

- 为避免污染备战推荐弹窗和卡牌词条窗，没有直接覆盖共享底图，而是新增战斗专用做旧版本；玩家可见目标不变，影响范围更准确。
- 未主动进入游戏进行运行时视觉验收。终审阶段检测到 Unity Editor 已处于运行态，并持续出现任务范围外的 `UiTransformSetter.OnUiUpdatePos()` 空引用；本任务未改动该系统，也未停止用户或其他流程启动的运行状态。

## 清理结果

- `AutoDoc/CleanupTempDocs.bat` 已在终审后执行一次，退出码 `0`。
- 恢复共享底图过程中使用的临时压缩包和解压目录均已移除。
