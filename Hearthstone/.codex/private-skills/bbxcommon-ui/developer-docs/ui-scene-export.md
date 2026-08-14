# UI 场景配置与导出

## 适用场景

当一个 GameStage 需要统一创建一组 UI，并希望在 Unity 场景中可视化配置它们的 UI Group、默认显隐、位置、缩放和 Pivot 时，使用 `UiSceneExporter`。

UI 编辑场景是配置源，导出的 `UiSceneAsset` 是运行时产物。不要绕过编辑场景直接编写或构造 `UiSceneAsset.UiObjectDatas`，否则场景中的可视化配置与运行时数据会失去唯一来源。

这个流程只负责把已经存在的 View Prefab 组合成一个 UI 场景，不会把普通场景 GameObject 自动转换成 View Prefab。

## 完成定义

一个 UiScene 需求只有同时具备下列产物并能重新导出时才算完成：

1. UiGroup 枚举与 `UiSceneBase<TGroup>` 派生类。
2. 保存于项目 UI 场景目录的独立 Unity 编辑场景。
3. 场景内配置完整的 `UiSceneExporter`、Canvas/CanvasScaler、UiGroups 和仍保持 Prefab 连接的 View 实例。
4. 由该编辑场景实际导出的 `UiSceneAsset`。
5. GameEngine 对导出 Asset 的引用或项目既有加载入口，以及 GameStage 的 `SetUiScene` 注册。
6. 从默认 Main 入口完成加载、显示、卸载和目标分辨率验证。

只有 UiScene 类、只有导出 Asset、只有 Stage 注册，或找不到对应 UI 编辑场景时，均判定为未完成。不得手写 Asset 来补齐缺失的编辑场景；应回到编辑场景完成配置并重新导出。

## 前置条件

1. 每个待配置 UI 都已经保存为 Prefab，并位于 `Assets/Resources/` 下。
2. Prefab 根节点挂有 `UiViewBase` 派生组件，`GetControllerType()` 返回对应 Controller，`DefaultShow` 已按需求设置。
3. 业务侧已经定义 UI Group 枚举，以及对应的 `UiSceneBase<TGroupKey>` 派生类；运行时 `OnSceneInit()` 创建的 Group 必须与编辑场景中的 Group 一致。
4. Game Engine 已提供与编辑场景相同参考分辨率和缩放规则的 `CanvasProto`。

## 创建 UI 编辑场景

1. 在项目的 UI 场景目录创建独立场景。场景名会直接成为导出 Asset 文件名，例如 `KeyboardWeapon.unity` 会导出 `KeyboardWeapon.asset`。
2. 创建或实例化一个带 `Canvas` 与 `CanvasScaler` 的根对象，并挂载 `UiSceneExporter`。
3. 设置 `ExportPath` 为目标 Assets 目录，例如 `Assets/Resources/Ui`。
4. 设置 `FullUiGroupType` 为 Group 枚举的完整类型名，例如 `MyGame.EMainUiGroup`。
5. 执行 `GenerateUiGroups()`，让导出器按枚举值创建并记录 Group。不要手动改变 `UiGroups` 与枚举值的对应顺序。
6. 为 Group 建立与运行时 Canvas 一致的坐标系。推荐在 Canvas 下使用居中的固定参考分辨率 RectTransform，例如参考分辨率为 `1920×1080` 时把 Group 的 `sizeDelta` 设为 `1920×1080`。
7. 把 View Prefab 实例放到对应 Group 下，通过场景调整其位置、缩放和 Pivot。必须保留 Prefab 连接；不要把未保存的普通 GameObject 当成导出对象。
8. 保存场景。

`UiSceneExporter` 当前只导出 Prefab Resources 路径、Group、默认显隐、`localPosition`、`localScale` 和 Pivot，不导出 Anchor、SizeDelta 或完整 RectTransform。编辑场景的 Group 原点、参考分辨率和缩放规则必须与运行时 Group 一致，否则导出位置会产生偏移。不要依赖可停靠 Game View 窗口的临时像素尺寸作为 Group 尺寸。

## 导出

活动场景中应当只有一个 `UiSceneExporter`。保存场景后使用以下任一入口：

- 选中 `UiSceneExporter`，执行 Inspector 中的 `ExportUiScene`。
- 使用菜单 `BbxCommon/UI/Export Active UI Scene`。

菜单入口会检查活动场景中是否恰好存在一个导出器，然后调用该实例的 `ExportUiScene()`。导出结果路径为：

```text
{ExportPath}/{活动场景名}.asset
```

导出器会从每个 Group 的子节点查找 `UiViewBase`，记录最近 Prefab 根的路径，并移除 `Assets/Resources/` 前缀与 `.prefab` 后缀。导出完成后应检查 `UiSceneAsset.UiObjectDatas` 的数量、PrefabPath、UiGroup、DefaultShow 和位置。

## 运行时接入

运行时从 Resources 加载导出 Asset，并把业务 `UiSceneBase` 与 Asset 交给 GameStage：

```csharp
var uiScene = gameEngine.GetOrCreateUiScene<MyUiScene>();
var uiSceneAsset = Resources.Load<UiSceneAsset>("Ui/MyUiScene");
stage.SetUiScene(uiScene, uiSceneAsset);
```

Stage 加载 UI 时，`UiSceneBase.CreateUiByAsset()` 根据记录的 PrefabPath 创建 Controller，把界面放入对应 Group，恢复位置、缩放和 Pivot，再按 `DefaultShow` 决定是否显示。Stage 卸载时关闭该 Asset 创建的 Controller。

## 修改与自动化规则

- 修改 View 内部层级、图片、文字或引用：编辑 Prefab。
- 修改 UI 属于哪个 Group、默认显隐、整体位置、缩放或 Pivot：编辑 UI 场景并重新导出。
- 修改 Group 枚举：同时更新业务 `UiSceneBase`、重新执行 `GenerateUiGroups()`、检查场景内 Prefab 归属并重新导出。
- 自动化工具可以打开并保存 UI 场景，再调用 `UiSceneExportMenu.ExportActiveUiScene()` 或场景内导出器的 `ExportUiScene()`；自动化结束后应恢复原场景。
- 自动化和 Builder 不应直接修改 `UiSceneAsset.UiObjectDatas`，也不应每次重建时覆盖作为配置源的 UI 场景。

## 回归检查

1. UI 编辑场景可以独立打开，Canvas、Group 与 View Prefab 实例引用完整。
2. 导出 Asset 的文件名与运行时 Resources 路径一致。
3. 每个导出项的 PrefabPath 能通过 `Resources.Load<GameObject>()` 加载。
4. 编辑场景与运行时使用相同参考分辨率和 Group 坐标系。
5. 从默认 Main 场景进入 Play Mode，确认 UI 位于预期 Group、默认显隐正确、位置与不同横屏分辨率表现正确。
6. 修改场景位置后重新导出，确认 Asset 与运行时表现同步变化。
7. 仓库中存在与导出 Asset 对应的 UI 编辑场景，且能由该场景再次生成等价导出项；没有手写或直接修改 `UiObjectDatas`。
