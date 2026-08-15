---
name: bbxcommon-ui
description: 设计、创建或修改 BbxCommon 页面、Hud 与 UiScene 时使用。
---

# BbxCommon UI

## 1. 模块概览

BbxCommon UI 使用 View 与 Controller 组织界面：View 只保存 Unity UI 组件引用并声明配对的 Controller 类型；Controller 负责全部初始化、交互、生命周期、数据监听与运行时表现更新。普通页面使用 `UiViewBase` / `UiControllerBase<TView>`，需要跟随场景 Entity 的界面使用 `HudViewBase` / `HudControllerBase<TView>`。

UI 不保存玩法权威状态。需要同时服务玩法与界面的运行时数据应放在 ECS RawComponent 中，由 Controller 监听并通过 View 引用写入组件；只负责通用表现或交互的组件可使用 `BbxUiItem`。View 不编写初始化、事件注册、资源加载、布局、刷新、动画、计时或其他运行时逻辑，也不提供由 Controller 转调的表现 helper。页面设计边界见 [page-design.md](page-design.md)，UI 组件规则见 `bbxcommon-ui-item`。

Controller 不直接向 ECS 数据字段挂裸 delegate。需要响应 Model 时，在 `InitListeners()` 中通过 `ModelWrapper.CreateVariableDirtyListener`、`CreateVariableInvalidListener` 或 `CreateListener` 创建监听，使用 `RebindTarget` 绑定当前 `ListenableVariable<T>` / `IListenable`，并选择最短且正确的 `EControllerLifeCycle`。数据生产与回收规则见 `bbxcommon-ecs`。

更多说明：

- [创建 Controller 与 View](new-class-examples/mvc-controller-view.md)
- [创建 Hud](new-class-examples/hud.md)
- [UI 运行时生命周期](developer-docs/ui-runtime-lifecycle.md)
- [UI 场景配置与导出](developer-docs/ui-scene-export.md)：当一组 UI 需要按 GameStage 统一配置分组、默认显隐和位置时使用。

## 2. 需求路由与强制步骤

处理 UI 需求时先确定属于下面哪一种；一个需求命中多项时，按命中项合并执行，不得只完成代码注册或只创建运行时 Asset。

### 2.1 新增普通页面

1. 确定页面所属 GameStage、现有或新增 UiScene、UiGroup、默认显隐和页面跳转关系。
2. 创建完整的 View Prefab：静态背景、面板、按钮、文字、容器和布局全部保存在 Prefab；Prefab 位于 `Assets/Resources/`，根节点挂 `UiViewBase` 派生类。可以在 Unity Editor 中正常编辑，也可以按 §2.7 使用与该 Prefab 一一对应的 `UiBuilder` 创建或更新。
3. 成对创建 View 与 Controller。View 只保存序列化组件引用和 Controller 类型映射；Controller 通过 `m_View` 处理事件、数据监听与表现刷新。
4. 动态重复条目使用独立条目 Prefab/View/Controller，并优先使用 `UiList` 等已有 BbxUiItem 或项目现有对象池；不得在刷新循环中反复 `new GameObject` / `AddComponent`。使用或改动通用 UI 组件时读取 `bbxcommon-ui-item`。
5. 若页面加入现有 UiScene，打开对应 UI 编辑场景，把 View Prefab 实例放入正确 Group，并重新导出；若需要新增 UiScene，继续执行 §2.4 的全部步骤。
6. 在所属 GameStage 通过导出的 `UiSceneAsset` 注册页面，并从默认 Main 流程验证创建、显示、隐藏、关闭、重入与不同目标分辨率。

### 2.2 修改普通页面

1. 先定位 View Prefab、View、Controller、UI 编辑场景、导出 Asset、所属 UiGroup 和 GameStage；任何一环缺失都先按框架补齐，不能在 Controller 中另建平行页面。
2. 修改页面内部控件、布局和引用时编辑 View Prefab，并同步 View 字段与 Controller；既有 Prefab 由 `UiBuilder` 维护时同步修改其一一对应的 Builder，并由 Builder 重新生成或更新。修改 Group、默认显隐、整体位置、缩放或 Pivot 时编辑 UI 场景。
3. 只要 UI 场景的导出信息发生变化，就通过 `UiSceneExporter` 重新导出并检查 Asset；不得直接编辑 `UiSceneAsset.UiObjectDatas`。
4. 按受影响生命周期和分辨率回归；若发现既有框架外静态构建、直接管理池或手写导出资产，必须一并迁回框架流程后才能判定完成。

### 2.3 新增或修改 Hud

1. 创建或修改 Hud Prefab，并使用唯一一对 `HudViewBase` + `HudControllerBase<TView>`；静态表现保存在 Prefab，View 只保留引用。Hud Prefab 也可以按 §2.7 使用一一对应的 `UiBuilder` 创建或更新。
2. 通过 `entity.BindHud<THudController>()` 或既有 Hud 公开入口绑定 Entity，不直接管理 Hud Controller 池。
3. 在 `InitListeners()` 创建监听，在 `OnHudBind` 对当前 Entity 的 Component 执行 `RebindTarget`，并验证换绑、解绑、Entity 销毁与回池。
4. 若修改通用 Hud/UI 组件，继续执行 `bbxcommon-ui-item` 的组件与文档流程；若 Hud 同时由 UiScene 统一创建，再执行对应 UiScene 流程。

### 2.4 新增 UiScene

新增 UiScene 必须完成 Unity 资产落地，不能在提交 `UiSceneBase` 和 Stage 注册代码后结束。必须使用 Unity Editor 创建并保存独立 UI 编辑场景，把 View Prefab 实例放入 `UiSceneExporter` 生成的 Group，随后从该场景实际导出 `UiSceneAsset`；最后用 Stage 中的精确 Resources 路径验证 `Resources.Load<UiSceneAsset>()` 非空。缺少任一步时，即使代码可以编译，也应判定任务未完成。

#### 2.4.1 准备代码与 View Prefab

1. 定义 UiGroup 枚举；枚举值将被序列化为导出 Asset 的 `UiGroup`，不要在已有数据落地后随意改值。
2. 创建 `UiSceneBase<TGroup>` 派生类，在 `OnSceneInit()` 中对每个枚举成员调用 `UiGroupWrapper.CreateUiGroupRoot`。运行时创建的 Group 必须与编辑场景一致。
3. 先完成所有 View Prefab：放在 `Assets/Resources/` 下，根节点挂对应 `UiViewBase`，设置 `DefaultShow`，保存完整静态层级和序列化引用。可以在 Unity Editor 中正常编辑，也可以按 §2.7 使用与 Prefab 一一对应的 `UiBuilder`；不得把场景中的临时普通 GameObject 当成 View 配置源。
4. 对 View 内的 `BbxUiItem` 执行 `Pre-UiInit`。若页面包含通过 `UiApi.OpenUiController<T>()`、`UiList.AddItem<T>()` 等入口动态创建、但不会作为场景 View 导出的条目，还要在条目 View Prefab 上执行 `Export as Pre-load`，并检查 `PreLoadUiData` 的 Controller 到 Resources Prefab 映射。

#### 2.4.2 创建 UI 编辑场景

1. 保护当前 Editor 状态：记录原活动场景，确认没有未保存 Scene/Prefab；自动化完成后恢复原场景，并按实际 Editor 操作通道完成状态保护与验收。
2. 在项目约定的 UI 场景目录新建独立场景。场景名决定默认导出文件名，例如 `Battle.unity` 将导出 `Battle.asset`。
3. 创建 UI 根对象，配置与运行时 `CanvasProto` 相同的 `Canvas`、`CanvasScaler`、参考分辨率、缩放模式和 `GraphicRaycaster`，并挂 `UiSceneExporter`。UI 编辑场景按项目 UI 配置源约定创建，不因通用场景模板要求额外加入无关玩法对象。
4. 将 `ExportPath` 设为运行时资源目录，例如 `Assets/Resources/Ui`；将 `FullUiGroupType` 设为 UiGroup 枚举完整类型名，例如 `MyGame.EMainUiGroup`。
5. 执行 `GenerateUiGroups()`，检查每个枚举值都有且只有一个 Group。为 Group 设置与运行时一致的坐标系和参考尺寸，不要手工打乱 `UiGroups` 与枚举值的对应顺序。
6. 通过 `PrefabUtility.InstantiatePrefab` 或 Editor 的正常 Prefab 实例化流程，把已保存的 View Prefab 放进正确 Group；保持 Prefab 连接，只在编辑场景配置整体 Position、Scale、Pivot、Group 和默认显隐来源。
7. 保存 UI 编辑场景。可以使用 Unity Editor 正常操作、项目约定的 Editor API/Editor 脚本或当前项目允许的自动化通道创建、保存 Scene 与 Prefab；不得用文件工具手写 Scene、Prefab 或 `.asset` YAML。

#### 2.4.3 导出与运行时接入

1. 让目标 UI 编辑场景成为活动场景，并确保其中恰好有一个 `UiSceneExporter`。
2. 通过 `BbxCommon/UI/Export Active UI Scene` 或 Inspector 的 `ExportUiScene` 实际生成 `UiSceneAsset`。不得仅创建同名空 Asset，也不得直接写 `UiSceneAsset.UiObjectDatas`。
3. 检查导出项数量及每项 `PrefabPath`、`UiGroup`、`DefaultShow`、`Position`、`Scale`、`Pivot`；每个 `PrefabPath` 必须能通过 `Resources.Load<GameObject>()` 加载。
4. 在所属 GameStage 使用项目既有方式加载导出 Asset，并通过 `GetOrCreateUiScene<T>()` 与 `SetUiScene` 注册。Stage 中的 Resources 路径、编辑场景名和导出 Asset 文件名必须完全一致。
5. 在 Editor 内用 Stage 使用的精确路径验证 `Resources.Load<UiSceneAsset>()` 非空；重新打开 UI 编辑场景，确认 Exporter 配置完整、View 实例仍为 Connected Prefab、场景未脏且可以再次导出等价数据。

#### 2.4.4 验收与收尾

1. 操作结束后保存必要资产、刷新并等待编译完成，恢复原活动场景；通过实际使用的 Editor 操作通道核对活动场景与 Console 错误并记录证据。
2. 按项目的游戏验收授权验证 Stage 加载/卸载、页面默认显隐、Group 排序、关闭与重入以及目标分辨率。项目默认不允许主动进入 Play Mode 时，不得擅自进入；改做 Editor 结构、Resources 加载和 Console 验收，并明确记录未执行的游戏内验证风险。
3. 在不改变目标配置的前提下重新导出并比对关键字段，或用等价的结构检查证明配置源能够稳定复现导出结果。最终交付必须同时保留编辑场景与导出 Asset。

只有 UiScene 类、只有 `UiSceneAsset`、只有 Stage 注册，或缺少可重新导出的 UI 编辑场景，均判定为流程未完成。完整导出细节见 [UI 场景配置与导出](developer-docs/ui-scene-export.md)。

### 2.5 修改 UiScene

1. 打开现有 UI 编辑场景，核对其 `UiSceneExporter`、Group 枚举、Prefab 实例和当前导出 Asset；禁止把导出 Asset 反向当作唯一配置源。
2. Group 枚举变化时同步 `UiSceneBase` 并重新 `GenerateUiGroups()`；页面归属、默认显隐、整体位置、缩放或 Pivot 变化时只在编辑场景调整。
3. 保存并重新导出，检查导出项与场景一致，再验证所有使用该 Asset 的 GameStage。

### 2.6 框架边界判定

以下情况属于框架外实现，不能作为最终交付：

- 在 Controller、Stage 初始化项或普通 MonoBehaviour 中运行时拼装整页静态 UI 层级。
- 绕过 View 序列化引用，用查找名称、场景根对象或临时 Transform 维持页面结构。
- 业务代码直接访问 `UiControllerManager`、自行管理 Controller/Hud 池，或复制 `UiApi` 生命周期。
- 手写或直接修改 `UiSceneAsset.UiObjectDatas`，却没有对应 UI 编辑场景和 `UiSceneExporter` 配置源。
- 用兼容补丁掩盖生命周期、导出或框架能力缺口，而保留平行实现。

发现上述情况时，按 `project-state-preflight` 的框架边界规则持续整改；现有框架确实缺能力时，小型局部改动可直接补到 BbxCommon 并回归，大型契约或生命周期改动先上报用户。

### 2.7 使用 UiBuilder 创建或更新 UI Prefab

`UiBuilder` 是可选的 Unity Editor 配置源，用于以可复现的 Editor API 代码创建或更新 UI Prefab；它不改变 View/Controller、Resources、UiScene 和导出流程。选择此方式时遵循以下规则：

1. Builder 固定放在 `Assets/Scripts/<项目名>/Ui/Editor/`，其中 `<项目名>` 使用当前业务项目代码目录名；例如本项目为 `Assets/Scripts/Hearthstone/Ui/Editor/`。`Editor` 目录确保 Builder 不进入运行时程序集。
2. 每个 Prefab 必须对应一个独立 Builder，类名使用 `<Prefab名>UiBuilder`。可以复用纯 Editor 辅助函数，但不得用一个多用途 Builder 通过参数或分支维护多个 Prefab；Prefab 与最终构建入口必须保持一一对应。
3. Builder 提供可由动态 C# 直接调用的公开静态入口，统一命名为 `public static void Build()`；入口应可重复执行，明确创建或更新目标 Prefab，保存序列化引用，并清理临时对象。产物仍放在该 UI 类型规定的 `Assets/Resources/` 路径。
4. Builder 及其入口不得添加 `[MenuItem]`，也不得使用 `InitializeOnLoad`、资源导入回调等方式自动执行，不在 Unity 项目菜单栏注册任何临时或永久菜单项。
5. 执行 Builder 时使用当前环境正式提供且项目允许的 Unity Editor 操作通道，直接调用 Builder 的完整类型名和 `Build()`，例如当前项目沿用 `Hearthstone` 命名空间时调用 `Hearthstone.ExampleUiBuilder.Build();`；目录层级不强制增加同名命名空间。项目不要求使用 MCP，也不优先使用 MCP；不得仅因 MCP 不可用而中断 Builder 流程或添加临时菜单入口。
6. 当前环境没有可执行 Builder 的允许通道时，应如实记录未完成项；不得用手写 Prefab YAML、自动菜单项、初始化回调或其他平行配置源替代 Builder 执行。
7. Builder 只负责 Editor 期的静态层级、组件、资源引用、布局和序列化字段，不得把运行时初始化、事件、数据监听、动画或玩法逻辑塞进 Builder 或 View。动态重复条目、对象池、UiScene Group 和导出资产仍分别遵循本 skill 的既有流程。
8. 构建后重新加载目标 Prefab，核对根 View 类型、完整层级、组件和序列化引用；若 Prefab 属于 UiScene，再继续实例化到 UI 编辑场景并通过 `UiSceneExporter` 导出。UiBuilder 不能替代 UI 编辑场景或 `UiSceneAsset` 导出配置源。

## 3. API接口

业务代码通过 `UiApi` 和 `UiControllerBase` 使用 UI，不直接访问 `UiControllerManager`、`UiModelManager` 等底层管理类。

最简页面 UI 由一对 View 与 Controller 组成：

- `UiViewBase` 派生类只保存界面组件引用，并通过 `GetControllerType()` 声明与它配对的 Controller 类型。
- `UiControllerBase<TView>` 派生类通过 `m_View` 直接读写组件，在 `OnUi*` 生命周期函数中处理初始化、交互、资源、布局、数据监听与运行时更新。
- `UiApi.OpenUiController<T>()` 从已预加载的 View 创建或复用 Controller，并把页面挂到指定父节点。
- 返回的 Controller 负责页面的显示、隐藏和关闭；关闭后的实例由框架管理，业务代码不直接操作对象池。

这个模型主要使用以下接口：

- `UiApi.OpenUiController<T>(Transform parent, bool show = true) where T : UiControllerBase`
- `UiControllerBase.Show()`
- `UiControllerBase.Hide()`
- `UiControllerBase.Close()`

调用 `OpenUiController<T>()` 前，对应 Controller 的 View 必须已经预加载，并且 View 的 `GetControllerType()` 必须返回该 Controller 类型。

## 4. 业务类

业务侧通常按页面成对新建以下类型：

- `UiViewBase` 派生类：只保存普通页面的 Unity UI 组件引用，并返回配对的 Controller 类型。
- `UiControllerBase<TView>` 派生类：处理普通页面的全部初始化、交互、生命周期、数据监听与运行时表现。
- `HudViewBase` 派生类：只保存需要跟随场景对象的 HUD 组件引用。
- `HudControllerBase<TView>` 派生类：处理 HUD 的全部初始化与运行时逻辑，并通过 `Bind(Entity)` / `Unbind()` 管理跟随目标。

普通页面模板与监听写法见 [mvc-controller-view.md](new-class-examples/mvc-controller-view.md)，HUD 差异见 [hud.md](new-class-examples/hud.md)。自定义 UI 组件按 `bbxcommon-ui-item` 的规则创建和维护。

## 5. 主要类的生命周期

### 5.1 UiViewBase

- 创建：作为组件挂在 UI Prefab 的 View 根对象上，由 `UiApi` 实例化。
- 初始化：Controller 首次创建时建立双向关联并初始化 View 上的 `BbxUiItem`。
- 使用：只保存组件引用和 Controller 类型映射；不得包含初始化或运行时逻辑，也不得用 View helper 间接承载这些逻辑。
- 销毁：随所属 Controller 的 GameObject 一起销毁。

### 5.2 UiControllerBase

- 创建：首次打开页面时由框架创建；从池中复用时不会重复创建。
- 初始化：`OnUiInit()` 每个实例只执行一次，适合建立可跨多次打开复用的内容。
- 打开与显示：`OnUiOpen()` 对应每次打开，`OnUiShow()` / `OnUiHide()` 对应每次显隐。
- 关闭：`Close()` 触发 `OnUiClose()` 并把实例交还框架管理；框架会按阶段解除通过 `ModelWrapper` 创建的监听，业务侧在这里释放其它仅供本次打开使用的资源。
- 销毁：真正销毁时触发 `OnUiDestroy()`，用于清理实例级资源。

### 5.3 HudControllerBase

- 创建与 UI 生命周期：与普通 Controller 相同。
- 绑定：通过 `Bind(Entity)` 关联跟随目标，在 `OnHudBind(Entity)` 中取得目标数据并重绑监听。
- 换绑与解绑：目标变化时重新绑定；`Unbind()` 或关闭时执行 HUD 的解绑清理。

完整生命周期与监听阶段见 [UI 运行时生命周期](developer-docs/ui-runtime-lifecycle.md)。
