# 创建 Controller 与 View

本章节覆盖：**成对新增** `UiViewBase` 与 `UiControllerBase<TView>`、`GetControllerType` 绑定、通过 `m_View` 驱动表现、**UI 生命周期**调用次数，以及与 Model 数据的监听挂载。页面级**设计取舍**见 [page-design.md](../page-design.md)；**Hud** 见 [hud.md](hud.md)。

**每个页面对应唯一一对 View 与 Controller，在同一轮开发中一起添加**：一个 `UiViewBase` 子类、一个 `UiControllerBase<TView>` 子类，View 的 `GetControllerType()` 返回该 Controller 的 `typeof(...)`。

## 生命周期与缓存（`OnUi*` 调用次数）

- **`OnUiInit`**：该 Controller 实例在**整个存活期内调用一次**（首次创建并执行 `Init` 时）。`Close` 后实例**回到对象池**，再次打开时**不会**再走 `Init`。
- **`OnUiOpen`**：每次从池中**再次打开**时调用一次，与 `Close` 配对。适合绑定「仅本次打开」需要的资源，并在 `OnUiClose` 里释放。
- **`OnUiShow` / `OnUiHide`**：每次界面**显示 / 隐藏**各调用一次，与 `Show` / `Hide` 配对。适合与可见性强相关的刷新。
- **做法**：把一次性初始化、可跨多次打开复用的缓存放在 **`OnUiInit`**；仅在每次打开或每次显示时才需要刷新的逻辑放在 **`OnUiOpen`** 或 **`OnUiShow`** / **`OnUiHide`**。

## View Prefab 与 View 类

页面的静态结构必须先制作成 View Prefab：背景、面板、固定文字、按钮、页签、滚动容器和布局组件都属于 Prefab 内容。Controller 不得用 `new GameObject`、`AddComponent` 等方式在运行时重建整页静态层级；确实按数据变化的重复条目应使用独立条目 Prefab/View/Controller，并交给 `UiList` 等既有组件或项目对象池管理。

继承 `UiViewBase`，只用 **public 字段**承载要在运行时读写的 Unity 组件引用；实现 **`GetControllerType()`**，返回与本 View 配对的 Controller 类型。View 中不要添加 `Awake`/`Start`/`Update`、初始化函数、监听注册、资源加载、布局、刷新、动画或供 Controller 转调的表现 helper。

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using BbxCommon.Ui;

namespace YourNamespace
{
    public class YourFeatureView : UiViewBase
    {
        public Button ConfirmButton;
        public Text TitleText;

        public override Type GetControllerType()
        {
            return typeof(YourFeatureController);
        }
    }
}
```

## Controller 类

继承 **`UiControllerBase<YourFeatureView>`**（泛型参数即上文的 View 类型）。在代码里用 **`m_View`** 访问 View 的字段与 `m_View.transform`。框架在运行时根据 Prefab 实例化流程**创建 Controller 节点并添加对应 Controller 组件**，Prefab 上只挂 View。

```csharp
using BbxCommon.Ui;

namespace YourNamespace
{
    public class YourFeatureController : UiControllerBase<YourFeatureView>
    {
        protected override void OnUiInit()
        {
            m_View.TitleText.text = "READY";
            m_View.ConfirmButton.onClick.AddListener(OnConfirm);
        }

        protected override void OnUiUpdate()
        {
            // 运行时刷新、布局与动画都在 Controller 中直接写入 m_View 的组件引用。
        }

        private void OnConfirm()
        {
            m_View.TitleText.text = "CONFIRMED";
            // 读写 ECS Component（Model），并通过 m_View 引用更新组件。
        }
    }
}
```

上例中的标题初始化、按钮监听和运行时更新都位于 Controller；View 只有字段和 `GetControllerType()`。即使某段表现逻辑只服务这个页面，也不能移动到 View 方法中。

## 运行时结构（用 `m_View` 驱动表现）

框架用带 Controller 的根节点承载生命周期与 `Update`，并把 **View 根物体作为子节点**挂在其下。`Hide` 时常关闭 View 所在 GameObject。表现层位移、显隐、组件属性应通过 **`m_View`** 修改，保证与 UI 生命周期一致。

**Hud（`HudViewBase` / `HudControllerBase<TView>`）** 必须挂到要跟进的 **ECS `Entity`** 上：通常调用扩展方法 **`entity.BindHud<THudController>(show: true)`**（内部会 **`OpenHudController`** 并对该 **`IHudController`** 执行 **`Bind(entity)`**，并把控制器登记到实体的 **`HudRawComponent`**）；若已持有 Hud 实例，也可直接 **`Bind(entity)`**。跟随场景坐标、监听随 Entity 变化而重绑等约定见 [hud.md](hud.md)。

## Model：ECS Component 与监听挂载

**`EcsRawComponent`（及单例 RawComponent 等）即 Model**：与玩法共用同一套组件数据，不另建独立 Model 类型。

- **按字段监听变化**：在组件里把需要通知 UI 的字段声明为 **`ListenableVariable<T>`**，在 Controller 里用 **`ModelWrapper.CreateVariableDirtyListener<T>`**（或 **`CreateVariableListener` / `CreateVariableInvalidListener`**）针对 **Dirty / Invalid** 等事件注册回调。
- **需要比「字段变了」更细的信息时**：让该 **`EcsRawComponent` 实现 `IListenable`**，用整型事件 id（常用 `enum`）区分事件，业务在适当时机 **`Dispatch`**；在 Controller 里用 **`ModelWrapper.CreateListener(...)`** 订阅对应事件。

监听类型按场景选择：

- **刷新数值或表现**：`CreateVariableDirtyListener<T>`，回调直接取得最新值。
- **监听目标随 Entity 销毁或 Component 回池而失效**：再创建 `CreateVariableInvalidListener`，用于清空表现、解除业务引用或等待下一次绑定。
- **响应伤害、选择变化等语义事件**：`CreateListener`，事件 key 优先使用业务枚举。

`ListenableVariable<T>` 的数据生产者必须通过 `SetValue` 修改普通值；只修改集合等引用对象内部内容时要手动 `SetDirty()`；所属 Component 回收时必须 `MakeInvalid()`。这些属于 ECS 数据生命周期，完整约束见 `bbxcommon-ecs`。

**挂载方式**：在 **`InitListeners()`** 里创建上述监听（该函数在 **`OnUiInit` 之前**由框架调用）。传入 **`EControllerLifeCycle.Init` / `Open` / `Show`** 之一，表示监听从该阶段开始生效，并在对应反向生命周期自动解除。若创建监听时还没有目标引用，先 **`ModelWrapper.Create*`** 再对返回的 **`ListenableItemListener`** 调用 **`RebindTarget(IListenable)`**，把 **`ListenableVariable<T>`** 或实现了 **`IListenable`** 的组件实例绑上去；目标在运行期切换时也可再次 **`RebindTarget`**。**Hud** 上「何时 `Create*`、何时 `RebindTarget`」与 **`Bind(entity)` 的先后**见 [hud.md](hud.md)。

```csharp
private ListenableItemListener m_ValueDirtyListener;
private ListenableItemListener m_ValueInvalidListener;

protected override void InitListeners()
{
    m_ValueDirtyListener = ModelWrapper.CreateVariableDirtyListener<float>(
        EControllerLifeCycle.Show,
        (value) => OnValueChanged(value));
    m_ValueInvalidListener = ModelWrapper.CreateVariableInvalidListener(
        EControllerLifeCycle.Show,
        OnValueInvalid);

    m_ValueDirtyListener.RebindTarget(someComponent.SomeListenableVariable);
    m_ValueInvalidListener.RebindTarget(someComponent.SomeListenableVariable);
}
```

把返回的 `ListenableItemListener` 保存为 Controller 字段；目标 Entity 或 Component 改变时，对同一个 listener 再次调用 `RebindTarget`。`RebindTarget` 会先尝试从旧目标解除，再绑定新目标。不要在每次刷新时重复创建 listener。

## 检查清单

- [ ] 成对新增 View 与 Controller，`GetControllerType()` 指向该 Controller。
- [ ] 静态 UI 层级保存在 View Prefab，动态重复条目使用条目 Prefab 与框架组件/项目对象池，没有在 Controller 中重建整页静态结构。
- [ ] View 只包含组件引用与 Controller 类型映射，没有初始化、监听、加载、布局、刷新、动画或其他运行时方法。
- [ ] Controller 写为 `UiControllerBase<对应View>`，业务用 `m_View` 访问界面。
- [ ] 按调用次数放置逻辑：`OnUiInit` 一次；`OnUiOpen` 每次打开；`OnUiShow` / `OnUiHide` 每次显隐；仅本次打开的资源在 `OnUiClose` 释放。
- [ ] 在 **`InitListeners`** 中用 **`ModelWrapper`** 挂载 **`ListenableVariable`** 或 **`IListenable`** 的监听，按需 **`RebindTarget`**。
