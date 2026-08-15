# 《99升变》主菜单与封面实施报告

## 1. 结果概要

本次已将游戏默认启动流程改为先进入《99升变》主菜单。主菜单使用独立 `MainMenuStage + MainMenuUiScene + MainMenuView/MainMenuController`，当前只提供“开始游戏”按钮。本局 `RunStateStage` 不在引擎 `OnAwake()` 时创建；玩家首次点击按钮后，Controller 立即禁用按钮并调用 `StartNewRun()`，由 GameEngine 创建新一局并进入第 1 轮备战。

主菜单封面按用户的三轮视觉反馈重新收敛：最终为做旧羊皮纸主底板，左侧持剑盾哥布林朝右、右侧弓箭哥布林朝左，两者以低对比、半透明的后中世纪木板蛋彩画质感位于边缘。中央保持大面积空白，游戏名和按钮均由 UI 独立叠加。

## 2. 主要产物

### 2.1 运行时与编辑器代码

- 启动与 StageGroup：`Assets/Scripts/Hearthstone/Bootstrap/HearthstoneGameEngine.cs`
- 主菜单 Stage：`Assets/Scripts/Hearthstone/GameStage/MainMenuStages.cs`
- UiScene：`Assets/Scripts/Hearthstone/Ui/Scene/MainMenuUiScene.cs`
- View/Controller：`Assets/Scripts/Hearthstone/Ui/View/MainMenuView.cs`、`Assets/Scripts/Hearthstone/Ui/Controller/MainMenuController.cs`
- Builder：`Assets/Scripts/Hearthstone/Ui/Editor/MainMenuViewUiBuilder.cs`、`Assets/Scripts/Hearthstone/Ui/Editor/MainMenuUiSceneBuilder.cs`
- EditMode 验证：`Assets/Scripts/Hearthstone/Tests/Editor/MainMenuTests.cs`

### 2.2 Unity 资产

- 封面：`Assets/Resources/Art/MainMenu/UI/MainMenuCover.png`
- View Prefab：`Assets/Resources/Ui/MainMenuView.prefab`
- UI 编辑场景：`Assets/Scenes/Ui/MainMenu.unity`
- 导出 UiSceneAsset：`Assets/Resources/Ui/MainMenu.asset`

Prefab、Scene、UiSceneAsset 和 `.meta` 由 Unity 正常导入或 Builder/UiSceneExporter 生成，未手写 Unity YAML。

### 2.3 文档

- 开始界面设计：`AutoDoc/Design/Specific/start-screen/start-screen.md`
- 主菜单美术：`AutoDoc/Art/Modules/main-menu/main-menu.md`
- 主菜单 UI：`AutoDoc/Program/UI/main-menu/main-menu.md`
- 主菜单 GameStage：`AutoDoc/Program/GameStage/main-menu/main-menu.md`
- 直接相关现状更新：UI 美术总览、备战设计/程序、战斗程序与 Loading 设计/程序文档。

## 3. 封面生成记录

- 生成模式：内置 `image_gen` 图像编辑模式，使用上一版羊皮纸底图与项目内 `GoblinWarrior.png` / `GoblinArcher.png` 作为参考。
- 工作区路径：`Assets/Resources/Art/MainMenu/UI/MainMenuCover.png`
- 生成器原始保存路径：`C:\Users\黄昕玮\.codex\generated_images\01a00675-21c4-7d51-9610-8d7decc4c630\exec-3ea2dac7-2064-4cf1-a474-687ac88f4f6f.png`
- 尺寸/格式：`1672 × 941 px`，RGB 24-bit PNG，约 `16:9`。
- Unity 导入：Sprite Single，Mipmap 关闭，Wrap Mode Clamp，Alpha Is Transparency 关闭。
- SHA-256：`2CE1685DF96998BA143098DF6486DFEE857B3038042ABAE202635B85D9C60359`

最终 prompt：

```text
Edit the first referenced parchment menu background while preserving its overall aged-parchment layout, central empty space, 16:9 framing, restrained color, worn edges, and corner ornament placement.

Redraw ONLY the two goblin side murals. Use the second and third referenced images only for recognizable goblin equipment and anatomy: sword-and-round-shield warrior on the left, bow-and-quiver archer on the right.

Facing directions and pose: the LEFT goblin warrior must be in a clear three-quarter/profile pose facing RIGHT toward the center. The RIGHT goblin archer must be in a clear three-quarter/profile pose facing LEFT toward the center. They should face one another across the blank center, not face the viewer. Keep them partially cropped at the outer edges and confined to the outermost 18–20% of the canvas.

Historical visual treatment: make both murals look like late-medieval painted wooden panel art / egg tempera transferred onto old parchment, not modern fantasy concept art. Use flattened perspective, firm dark contour underdrawing, simplified angular folds, restrained modeling, matte mineral pigments, tiny aged gilded accents, subtle craquelure, abrasion, and pigment loss. Muted olive, raw umber, iron red, smoky sepia, tarnished gold. Avoid glossy 3D lighting, cinematic realism, bright saturated green, photorealistic skin, or cartoon exaggeration.

Opacity and hierarchy: the murals remain faded and semi-transparent, around 25% visual opacity, quieter than the parchment texture. They are decorative edge illustrations, never focal characters. Keep the central 60% almost completely blank and clean for a separate Chinese title and Start Game button.

Strict constraints: no text, no Chinese characters, no letters, no numerals, no logo, no watermark, no UI button, no cards, no scenery, no central emblem, no extra creatures. Do not add new ornament into the center. Do not change the parchment into a scene.
```

人工图像检查结论：中央完整留白；左右哥布林方向正确、相向但不直视镜头；板绘轮廓、做旧颜料和羊皮纸纹理可辨；无文字、数字、Logo 或水印；不与中央标题和开始按钮抢夺层级。

## 4. 验证结果

| 验证项 | 结果 | 证据 |
| --- | --- | --- |
| Unity 编译 | 通过 | 最终完成脚本刷新与域重载 |
| Unity Console | 通过 | 最终清空后查询 `0 error` |
| `Hearthstone.Tests` EditMode | 通过 | `90 passed / 0 failed / 0 skipped`，耗时约 `2.71 s` |
| 启动入口 | 通过 | `OnAwake()` 无 RunState 创建；初次 Stage 完成后请求 MainMenu |
| Prefab 结构 | 通过 | 标题、按钮文字、四态 Sprite、Navigation None、Cover 射线和铺满设置通过测试 |
| UiScene 导出 | 通过 | `MainMenu.asset` 唯一默认 View；场景 Exporter 为 Main Group，View 保持 Connected Prefab |
| 封面导入 | 通过 | 尺寸、比例、Sprite Single、Mipmap、Clamp 和 Alpha 设置通过测试 |
| `git diff --check` | 通过 | 本任务代码与文档无空白错误 |

验证过程未进入 Play Mode，符合项目默认。剩余风险是未以玩家视角在 Game View 中实际点击按钮并人工观察不同屏幕比例下的最终缩放与阶段切换；结构、资源引用、导出数据与跳转代码已通过 EditMode 覆盖。

## 5. 检查清单结论

用户需求、启动/GameStage、BbxCommon UI、封面资源、框架边界、验证和文档同步项均通过。唯一未执行项为 Play Mode 人工验收，该项按项目默认明确不主动执行，已作为剩余风险记录。

## 6. 清理结果

`AutoDoc/CleanupTempDocs.bat` 已在结束审计后且在本报告创建前执行一次，退出码为 `0`。本报告在清理后创建。
