# Battle UiSceneAsset 缺失诊断报告

## 任务结果

已定位错误根因，但未执行 Unity 资产修复。`Resources/Ui/Battle` 的路径契约正确；真正缺失的是 Battle UI 的完整可导出资产链。当前会话没有项目允许的 Unity Editor 工具，且项目规则与实际 MCP 配置互相冲突，因此按框架边界停止，没有用手写 YAML、私有通道或运行时降级绕过。

## 根因与证据

- `BattleStages.TryAddBattleUi` 调用 `Resources.Load<UiSceneAsset>("Ui/Battle")`；根据 `UiSceneExporter` 的 `{ExportPath}/{活动场景名}.asset` 规则，名为 `Battle.unity`、导出目录为 `Assets/Resources/Ui` 时该 key 正确。
- 已存在 `BattleView`、`BattleController`、`BattleCardItemView`、`BattleCardItemController`、`BattleUiScene` 和 `EBattleUiGroup.Main`。
- `Assets/Resources/Ui/` 只有 CanvasProto、Placeholder Asset 和 PlaceholderView；没有 Battle View Prefab、BattleCardItem Prefab 或 Battle Asset。
- `Assets/Scenes/Ui/` 只有 Placeholder 编辑场景，没有 Battle UI 编辑场景。
- `Assets/Resources/BbxCommon/Ui/PreLoadUiData.asset` 不存在；即使单独补出 Battle Asset，卡牌 `UiList` 创建 `BattleCardItemController` 时仍会因预加载映射缺失继续失败。
- `ResourcesDictionary.json` 也没有任何 Battle UI 资源条目。

## 正确修复范围

需要在 Unity Editor 中通过正式流程完成：

1. 创建 `BattleCardItemView` Prefab并配置图片、攻血文本与攻击者/目标/死亡状态引用。
2. 创建 `BattleView` Prefab，配置敌我两个横向 `UiList`、回合文本和胜负文本，并引用卡牌条目预加载能力。
3. 创建 `Assets/Scenes/Ui/Battle.unity`，配置 Canvas、CanvasScaler、`UiSceneExporter` 和 `EBattleUiGroup.Main`，保持 BattleView 的 Prefab 连接。
4. 由该编辑场景导出 `Assets/Resources/Ui/Battle.asset`，而不是直接构造或编辑 `UiObjectDatas`。
5. 为 BattleController 与 BattleCardItemController 生成正式 `PreLoadUiData` 映射，并重建资源字典。
6. 从 Main 入口验证 Stage 加载、三张敌我卡牌、5 血 3 攻、轮流攻击、死亡显示和胜负结果。

## MCP 阻塞

- 当前 Codex 会话的工具清单没有任何 Codely 或 Unity Editor 工具。
- 项目 `AGENTS.md` 当前指定官方 Codely Bridge 1.0.75，但项目根目录 `.com-unity-codely.json` 不存在。
- `Packages/manifest.json`、`Packages/packages-lock.json` 与正在运行的 Unity 命令实际采用 CoplayDev MCP for Unity v10.0.0；Unity 进程使用官方 `MCPForUnity.Editor.McpCiBoot.StartStdioForCi` 入口启动。
- 因配置源冲突，不能把当前 Coplay 后端自行视作项目允许的 Codely 工具，也不能通过标准 SDK 写调用绕过当前会话未暴露工具。

## 检查项结果

- 路径与调用链定位：通过。
- 资产完整性核对：通过，确认完整资产链缺失。
- UiScene、GameStage 与资源框架边界：通过，未建立绕行实现。
- Unity Editor 资产生成：未通过，缺少项目允许的客户端适配工具。
- 源码、正式文档与 `.meta` 修改：不适用；本次均未修改。

## 清理与风险

- `AutoDoc/CleanupTempDocs.bat` 仅运行一次，退出码为 0。
- 未关闭、重启或修改用户正在运行的 Unity Editor。
- 未解决风险：在 MCP 选择与主代理规则统一、并让新 Codex 会话实际暴露 Unity 工具前，Battle UI 资产无法安全生成，当前运行时错误会持续出现。
