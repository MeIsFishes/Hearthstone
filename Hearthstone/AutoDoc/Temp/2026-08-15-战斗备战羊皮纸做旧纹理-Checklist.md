# 战斗与备战羊皮纸做旧纹理检查清单

- [通过] 已确认两处原先使用不同的整页背景 Sprite：备战为 `PreparationPageBackground.png`，战斗为 `BattleBoardBackground.png`；两张图包含不同结构边框，不能直接互换。已将羊皮纸表面做旧拆为同一个共享 `ParchmentAgingOverlay.png`，两个 Prefab 均引用该 Sprite。
- [通过] 使用内置 imagegen 生成透明做旧层；首版因棋盘格被烘入而淘汰，第二版经 Alpha 采样确认 `min=0`、`max=224`、无全不透明采样点。原有背景、木框和金边均未修改。
- [通过] 最终 Sprite 无文字、无水印、无规则重复纹样；1920×1080 备战与战斗离线渲染确认只出现低对比淡斑、水渍边和短划痕，不遮挡界面信息。
- [通过] 最终位图保存到 `Assets/Resources/Art/Preparation/UI/ParchmentAgingOverlay.png`，Unity 自动生成/维护 `.meta`，并设置为 Single Sprite、Alpha Is Transparency、无 Mipmap、Bilinear、CompressedHQ；未手写 `.meta`。
- [通过] `PreparationViewUiBuilder` 作为备战 Prefab 一一对应配置源创建共享层并正式重建；没有 Builder 的 `BattleView.prefab` 通过 Unity `PrefabUtility` 正式编辑与保存；未手写 Prefab YAML。
- [通过] Unity 编译与控制台检查通过；相关 EditMode 测试 2/2 通过；两份 dotnet 工程顺序构建 0 错误；资源、Prefab 引用、Alpha、层级与尺寸检查通过；两界面离线渲染通过；未进入 Play Mode。
- [通过] 玩家视角设计文档：已读取基础格式和战斗系统专项格式；同步战斗与备战羊皮纸当前低对比共享做旧表现。
- [通过] 美术文档：已读取基础、UI 总览和模块格式；同步 UI 通用资产分组、战斗卡牌模块与备战卡池模块的共享 Sprite、Alpha 和使用范围。
- [通过] 程序文档：已读取基础与 UI 界面格式；同步战斗、备战 Prefab 的共享 Sprite、层级、Rect 与 Alpha。
- [通过] 仅在既有 Builder 中增加静态层级，没有新增运行时函数、字段或一次性抽象；战斗 Prefab 也只增加一个静态 Image。
- [通过] 框架边界审计：继续使用唯一 Resources Sprite、`ResourceApi`/现有 Builder、静态 View Prefab 与 Unity 导入/Prefab 保存流程；没有运行时纹理加工、平行资源加载或手写导出产物。
- [通过] 已完成逐项复核；下一步只运行一次 `AutoDoc/CleanupTempDocs.bat`，随后创建同名报告。
