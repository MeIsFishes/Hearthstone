# 创建 Hud（跟随场景的 UI）

Hud 与普通页面 UI 的差别在于**基类**：使用 **`HudViewBase`** 与 **`HudControllerBase<TView>`**（`TView : HudViewBase`），而不是 `UiViewBase` / `UiControllerBase<TView>`。`HudControllerBase` 实现 **`IHudController`**，提供 **`Bind(Entity)` / `Unbind()`**，生命周期以 **`OnHudInit`**、**`OnHudBind`**、**`OnHudUnbind`** 等与 **`OnUi*`** 的密封转发为准。

其余约定（`GetControllerType`、`m_View`、**`ListenableVariable`** / **`IListenable`**、**`ModelWrapper`**）与 [mvc-controller-view.md](mvc-controller-view.md) 相同。

## 挂载到 Entity（`Bind`）

Hud 必须关联到要跟进的 **`Entity`** 才会按实体位置更新并参与 **`HudRawComponent`** 管理。常用写法：

- **`entity.BindHud<THudController>(show: true)`**（`UiApi` 对 **`Entity`** 的扩展）：内部 **`OpenHudController<T>()`** → **`hudController.Bind(entity)`** → 按需 **`Show()`**，并把控制器登记到该 Entity 的 **`HudRawComponent`**。
- 若已拿到 **`IHudController`** 实例，也可直接 **`Bind(entity)`**；解绑、查询见项目内 **`UnbindHud` / `GetHud`** 等扩展。

**`HudViewBase`** 在 Inspector 的 **HUD** 分组下有序列化字段（例如 **`AutoUpdatePos`**、**`HudOffset`** 等）。**这些参数由用户在编辑器里调整**；业务代码**不要随意改写**，除非需求明确要求代码驱动。

## 监听：何时 `Create*`、何时 `RebindTarget`

与普通 **`UiControllerBase`** 一样使用 **`ModelWrapper`** 与 **`ListenableItemListener`**。**`InitListeners()`** 仍在 **`OnUiInit` 之前**执行；此时 **`Bind(entity)` 往往尚未调用**（典型流程是先 **`OpenHudController`** → **Init / InitListeners** → 再 **`Bind`**）。

- **监听目标在 `InitListeners` 时已经确定**（例如 **`EcsSingletonRawComponent`**、全局唯一数据源）：在 **`InitListeners()`** 里直接 **`ModelWrapper.CreateVariableDirtyListener`** / **`CreateListener`** 等，并可立即 **`RebindTarget`** 到该目标；**`EControllerLifeCycle`** 的选取与普通 UI 相同。
- **监听目标挂在「当前绑定的 Entity」上、或会随换绑实体而变化**：在 **`InitListeners()`** 里只 **`Create*`**，**先不调用** **`RebindTarget`**。在 **`OnHudBind(Entity entity)`** 里用 **`entity.GetRawComponent<T>()`** 等取到 **`ListenableVariable`** / **`IListenable`** 后，再 **`RebindTarget`**。同一 Hud 实例**改绑另一个 Entity** 时会再次 **`Bind`** 并进入 **`OnHudBind`**，应在此处**重新 `RebindTarget`** 到新组件实例。**`Unbind`** / **`Close`** 会走 **`OnHudUnbind`** 与 Controller 反向生命周期，与 **`ModelWrapper`** 监听一并按阶段解除。
