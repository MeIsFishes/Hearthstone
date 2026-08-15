# 《99升变》主菜单与封面检查清单

## 1. 用户要求

- [通过] 游戏默认启动后先进入主菜单，不再直接进入第 1 轮备战。证据：`OnStageLoadingCompleted()` 首次请求 `MainMenu` StageGroup。
- [通过] 主菜单明确显示游戏名《99升变》。证据：`MainMenuView.prefab` 中 `GameTitle.text == "99升变"`。
- [通过] 主菜单包含可用的“开始游戏”按钮，点击后进入现有新一局流程与第 1 轮备战。证据：`MainMenuController -> StartNewRun() -> RestartRun() -> BeginPreparationForBattle(1)`。
- [通过] 设计并生成适合《99升变》的主菜单封面图。最终版为做旧羊皮纸、两侧相向中世纪板绘哥布林与中央留白。

## 2. 启动流程与 GameStage

- [通过] `OnAwake()` 不再创建 RunState；只有开始新一局、重开或隔离 Stage 入口需要时才创建，主菜单不污染本局数据。
- [通过] 主菜单使用 `EHearthstoneStageGroup.MainMenu` 与 `EnterMainMenuStageGroup()`；Controller 只调用 GameEngine 公开入口。
- [通过] `MainMenuStage` 单独激活并加载 `Ui/MainMenu`；进入 `RunStateStage + PreparationStage` 时由 StageWrapper 卸载主菜单。

## 3. BbxCommon UI

- [通过] 主菜单使用唯一 `MainMenuView/MainMenuController`，静态结构保存在 `Resources/Ui/MainMenuView.prefab`。
- [通过] 具备 `EMainMenuUiGroup`、`MainMenuUiScene`、`Scenes/Ui/MainMenu.unity`、`UiSceneExporter`、`Resources/Ui/MainMenu.asset` 和 Stage 注册；Builder 已重建成功。
- [通过] Prefab 由 `MainMenuViewUiBuilder`、场景/导出资产由 `MainMenuUiSceneBuilder` 生成，未手写 Unity YAML。
- [通过] View 只持有 TMP/Button 引用；Controller 只有一个防重复点击标记与生命周期处理。
- [通过] 按钮 Navigation 为 None，SpriteSwap 的常态/悬停/按下/禁用 Sprite 均存在，首击后禁用。
- [通过] CanvasScaler 参考分辨率为 `1920 × 1080`；Cover 四边拉伸、不保留原比例且 `raycastTarget=false`。

## 4. 封面图与资源

- [通过] 已完整读取 `imagegen` skill 及 prompting/sample-prompts，使用内置 `image_gen` 生成位图。
- [通过] 最终封面不含文字、数字、Logo 或水印；“99升变”由独立 TMP 层显示。
- [通过] `MainMenuCover.png` 为 `1672 × 941` RGB PNG，中央留白；Unity 导入为 Sprite Single、Mipmap 关闭、Clamp、Alpha Is Transparency 关闭。SHA-256: `2CE1685DF96998BA143098DF6486DFEE857B3038042ABAE202635B85D9C60359`。
- [通过] 报告将记录内置生成模式、工作区/原始保存路径、最终 prompt 和人工检查结论。

## 5. 框架边界与代码质量

- [通过] 沿用项目现有 GameStage/UiScene/MVC/UiBuilder/Resources 链路，未建平行体系。
- [通过] Prefab、Scene、UiSceneAsset 和 `.meta` 均由 Unity Builder/导入流程生成，未手写。
- [通过] 新增职责限于主菜单 Stage 创建、新一局入口、UI 引用与按钮防重；无运行时名称查找。
- [通过] 未重置或覆盖工作区既有修改；修改仅落在启动、主菜单 UI/资产/测试及直接相关现状文档。

## 6. 验证

- [通过] Unity 编译通过；最终清空 Console 后查询到 0 error。
- [通过] `Hearthstone.Tests` EditMode 完整回归 `90/90` 通过；新增 5 项覆盖启动流程、Controller、Prefab、封面导入和 UiScene。
- [通过] 测试打开 `MainMenu.unity` 核对 `UiSceneExporter`、唯一 Main Group、已连接 Prefab 实例及 `MainMenu.asset` 字段。
- [通过] 未进入 Play Mode，符合项目默认；仍保留“未亲自以玩家视角点击并观察实机切换”的人工验收风险。

## 7. 文档同步

- [通过] 已读取设计文档与开始界面专项格式，新增 `AutoDoc/Design/Specific/start-screen/start-screen.md`。
- [通过] 已读取美术文档与模块格式，新增主菜单美术模块并更新 UI 美术总览。
- [通过] 已读取程序 UI/GameStage 格式，新增主菜单 UI 与 GameStage 文档，并修正备战、战斗和 Loading 中的旧启动描述。

## 8. 结束审计与报告

- [通过] 已重新打开本清单，核对代码、图像、Importer、Prefab、Scene、UiSceneAsset、文档、编译与 `90/90` 测试证据，并修正了 Editor 测试程序集边界和 Alpha 导入设置。
- [待执行] 结束时只运行一次 `AutoDoc/CleanupTempDocs.bat` 并记录退出码。
- [待执行] 清理后创建 `AutoDoc/Temp/MainMenuAndCover-Report.md`。
