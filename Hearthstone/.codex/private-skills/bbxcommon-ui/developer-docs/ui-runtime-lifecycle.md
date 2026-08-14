# UI 运行时：自动打开、显示与生命周期保障

本章节说明框架**如何把页面级 UI 拉起来并显示**，以及 **`Init` / `Open` / `Show` 等在底层如何串起来、如何与池化、监听卸载对齐**。业务侧写法仍以 [mvc-controller-view.md](../new-class-examples/mvc-controller-view.md)、[hud.md](../new-class-examples/hud.md) 为准。

## 1. 谁触发「自动打开」：GameStage → UiScene → UiApi

带 **`SetUiScene(UiSceneBase, UiSceneAsset)`** 的 **`GameStage`** 在 **`LoadStage()`** 里，在 **`IStageLoad`、Additive 场景之后**会执行 **`OnLoadStageUiScene()`**，其中调用 **`m_UiScene.CreateUiByAsset(m_UiSceneAsset)`**（`UiSceneAsset` 在 **`SetUiScene`** 时被 **`Instantiate`** 一份，避免改坏资源）。

**`UiSceneBase<TGroup>.CreateUiByAsset`** 遍历导出数据 **`UiObjectDatas`**：首次为每条记录 **`Resources.Load`** Prefab，缓存 **`UiView`**、**`ControllerTypeId`**；然后对每个条目调用 **`UiApi.OpenUiController(uiView, typeId, 对应 UiGroup 的 Canvas.transform, show: false)`**，把 **`RectTransform`** 赋成导出时的 **Position / Scale / Pivot**；若该条 **`DefaultShow`** 为 true，再 **`controller.Show()`**（注释中强调：先设 Transform 再 **Show**，保证 **`OnUiShow`** 时布局已就绪）。

因此：**「自动打开」**指 Stage 加载阶段**批量**为导出列表创建/取出 Controller 并挂到分组 Canvas 下；**是否立刻可见**由 **`OpenUiController` 的 `show` 参数**与 **`DefaultShow`** 决定。

## 2. OpenUiController：池化复用 vs 新建

**`UiApi.OpenUiController(sourceView, typeId, parent, show)`**：

1. **`UiControllerManager.GetPooledUiController(typeId)`** 若取到实例：只再 **`Open()`**（不再 **`Init`**），挂到 **`parent`**，按 **`show`** 调用 **`Show()`** 或 **`Hide()`**。
2. 若池为空：**`Instantiate(sourceView.gameObject)`**，再 **`CreateUiControllerWithGameObject`**：新建名为 **`…Controller`** 的根节点、挂上 **`GetControllerType()`** 对应的 **`UiControllerBase`**、**`SetView`**，然后 **`Init()`**、**`Open()`**；最后同样 **`SetParent(parent)`** 与 **`Show`/`Hide`**。

**`OpenUiController<T>(parent, show)`**（Pre-load 路径）通过 **`PreLoadUiData`** 解析 Prefab，逻辑同上。

**保障**：新建路径上 **`Init` 必定先于首次 `Open`**；从池取出时**只走 `Open`**，与基类注释一致：**`Init` 每实例一次**，**`Close` 后进池**，下次只有 **`Open`**。

## 3. Init / Open / Show：底层顺序与「谁保证」

**`UiControllerBase<TView>.Init()`**（仅 **`m_Inited == false`** 时执行）：

- 分配三份 **`ListenableItemListener`** 列表（Init / Open / Show 桶）。
- **`InitListeners()`**（业务可重写，在 **`OnUiInit` 之前**）。
- **`m_View.InitBbxUiItem()`**，再按 View 登记的 **`IUiInit`** 子项依次 **`OnUiInit(this)`**。
- **`OnUiInit()`**，再对 **Init 桶**里已创建的 listener **`AddListener()`**（若 **`RebindTarget`** 已绑定目标，此处开始订阅 Model）。
- 置 **`m_Inited = true`**。

**`Open()`**（仅 **`m_Opened == false`**）：View 上 **`IUiOpen`** → **`OnUiOpen()`** → **Open 桶** **`AddListener()`** → **`m_Opened = true`** → **`UiControllerManager.OnUiOpen`**（登记到当前 **UiCollection** 的「活跃」列表，供 **`GetUiController<T>()`** 等）。

**`Show()`**（仅 **`m_Shown == false`**）：**`IUiShow`** → **`m_View.gameObject.SetActive(true)`** → **`OnUiShow()`** → **Show 桶** **`AddListener()`** → **`m_Shown = true`**。

**`Hide()`**：先对 **Show 桶** **`TryRemoveListener()`**，再 **`SetActive(false)`**、**`OnUiHide`**、**`IUiHide`**。

**`Close()`**：对 **Show 桶** 再卸监听 → **`Hide()`** → **`OnUiClose`**、**`IUiClose`** → **`UiControllerManager.CollectUiController`**（从活跃列表移除、放入池列表、**`UiGameEngineScene.PoolUiController`** 把节点挪到 Pooled 组、**`SetActive(false)`**）→ **`m_Opened = false`**。

**`OnDestroy`**：**Init 桶**卸监听 → 若仍视为 Shown/Opened 则补 **`OnUiHide`/`OnUiClose`** → **`OnUiDestroy`**、**`IUiDestroy`** → 三份 listener 列表 **`ReleaseInfo`** 并回池。

**保障**：阶段顺序由 **`sealed`** 的 **`Init`/`Open`/`Show`/`Hide`/`Close`/`OnDestroy`** 固定；**`Close`/`Destroy`** 会按注释约定**级联**（例如先 **`Hide`**）。**`Update`** 挂在 **Controller** 的 GameObject 上，**View 子节点 `SetActive(false)`** 不会停掉父节点上的 **`OnUiUpdate`**。

## 4. Model 监听与生命周期对齐

**`ModelWrapper.Create*`** 创建的 **`ListenableItemListener`** 按 **`EControllerLifeCycle`** 存入 **Init / Open / Show** 三个桶；在对应阶段末尾 **`AddListener()`**，在 **`Hide`/`Close`/`OnDestroy`** 等路径上 **`TryRemoveListener()`** 或 **`ReleaseInfo()`**，避免 Close 后仍订阅 ECS 数据。

**`RebindTarget`**：先 **`TryRemoveListener`**，换 **`ListenTarget`**，再 **`AddListener()`**；若重绑时当前已处于 **Show/Open**，由 **`AddListenerIfConditionMeets`** 等逻辑在业务 **`Rebind`** 后按需挂上（与 **`m_Inited`/`m_Opened`/`m_Shown`** 对齐）。

## 5. Stage 卸载与 UI

**`UnloadStage()`** 在 **Data、Tick、Listener** 之后调用 **`OnUnloadStageUiScene()`**，即 **`DestroyUiByAsset`**：对每条导出数据 **`CreatedController.Close()`**，走完整 **Close → 进池** 链（若之后销毁 GameObject 再走 **`OnDestroy`**）。

**Hud** 同样走 **`UiApi.OpenHudController<T>()` → 内部 `OpenUiController` 挂到 Hud 根**；**`entity.BindHud<T>()`** 在 **`Bind(entity)`** 前后与 **`Show`** 的组合见 [hud.md](../new-class-examples/hud.md)，**`Init`/`Open` 与池**仍符合上文。

**小结**：页面 UI 的**自动创建与默认显示**由 **Stage 加载 + `CreateUiByAsset` + `OpenUiController` + 可选 `Show`** 串联；**生命周期与监听卸载**由 **`UiControllerBase` 的 sealed 流程 + `UiControllerManager` 池 + `ListenableItemListener` 分桶**共同保证。
