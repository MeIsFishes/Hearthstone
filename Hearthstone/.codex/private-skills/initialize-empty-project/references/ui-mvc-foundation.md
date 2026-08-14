# MVC/UI 基础体系

## 目录

1. 适用条件
2. BbxCommon MVC 映射
3. 最小代码结构
4. Model 与监听
5. UiScene 与 Stage
6. 编辑器资产步骤
7. 生命周期与验收

## 1. 适用条件

用户没有说明 UI 需求时，默认创建 `PlaceholderUiScene`、`PlaceholderView` 和 `PlaceholderController` 代码，用于展示 MVC 的连接方式；不自动创建 Canvas、Prefab 或 UiSceneAsset。只有用户明确说明项目无运行时 UI（例如无头模拟或纯工具）时才跳过这些占位代码。

实现前读取 [bbxcommon-ui](../../bbxcommon-ui/SKILL.md) 的页面设计与 Controller/View 文档。若需要 Hud，再读取其 Hud 文档。

## 2. BbxCommon MVC 映射

- Model：默认是 `EcsRawComponent`、`EcsSingletonRawComponent` 或其可监听字段；
- View：`UiViewBase`/`HudViewBase`，只保存 Unity 组件引用和表现；
- Controller：`UiControllerBase<TView>`/`HudControllerBase<TView>`，监听 Model 并驱动 `m_View`；
- 页面容器：`UiSceneBase<TGroupKey>`；
- 生命周期容器：GameStage 的 `SetUiScene`。

虽然框架代码仍可能包含 `UiModelBase` API，但当前惯例不推荐新建 UiModel。只有项目现有设计明确依赖它时才沿用。

## 3. 最小代码结构

首个真实页面已确定时，直接创建真实 View/Controller；尚未确定时使用基础占位模板中的唯一一对 Placeholder View/Controller。

View：

```csharp
using System;
using BbxCommon.Ui;
using UnityEngine.UI;

namespace ProjectName
{
    public sealed class AppStatusView : UiViewBase
    {
        public Text StatusText;

        public override Type GetControllerType()
        {
            return typeof(AppStatusController);
        }
    }
}
```

Controller：

```csharp
using BbxCommon;
using BbxCommon.Ui;

namespace ProjectName
{
    public sealed class AppStatusController : UiControllerBase<AppStatusView>
    {
        private ListenableItemListener m_ReadyListener;

        protected override void InitListeners()
        {
            m_ReadyListener = ModelWrapper.CreateVariableDirtyListener<bool>(
                EControllerLifeCycle.Show,
                OnReadyChanged);
        }

        protected override void OnUiShow()
        {
            var model = EcsApi.GetSingletonRawComponent<AppSessionSingletonRawComponent>();
            m_ReadyListener.RebindTarget(model.Ready);
            OnReadyChanged(model.Ready.Value);
        }

        private void OnReadyChanged(bool ready)
        {
            m_View.StatusText.text = ready ? "Ready" : "Loading";
        }
    }
}
```

示例中的具体 listener 类型、可见性和回调签名必须以当前框架源码/`bbxcommon-ui` skill 为准。重点是：监听在 `InitListeners()` 创建，目标可在数据就绪后 `RebindTarget`，表现通过 `m_View` 更新。

## 4. Model 与监听

选择 Model 时：

- 玩法和 UI 都使用的数据：放已有/应有的玩法 Component；
- 仅表现临时缓存：可以放 Controller 私有字段；
- 仅 UI 但需要跨 Controller 共享和生命周期管理的数据：可以使用 UI 专用 ECS Component；
- 静态文本/配置：走 DataApi；
- 不复制一份核心状态到 Controller 或 UiModel。

监听生命周期：

- `Init`：Controller 实例整个池化存活期；
- `Open`：每次打开到关闭；
- `Show`：每次显示到隐藏。

把监听绑定到最短且正确的生命周期。目标 Entity/Component 变化时重新绑定；Stage 卸载前确保 Controller/监听不会继续引用已回收 Model。

## 5. UiScene 与 Stage

UiScene 最小结构：

```csharp
using BbxCommon.Ui;

namespace ProjectName
{
    public enum MainUiGroup
    {
        Main,
        Overlay
    }

    public sealed class MainUiScene : UiSceneBase<MainUiGroup>
    {
        protected override void OnSceneInit()
        {
            UiGroupWrapper.CreateUiGroupRoot(MainUiGroup.Main);
            UiGroupWrapper.CreateUiGroupRoot(MainUiGroup.Overlay);
        }
    }
}
```

只创建真实需要的组。Stage 中：

1. 从 GameEngine `GetOrCreateUiScene<T>()`；
2. 加载/取得对应 `UiSceneAsset`；
3. 对该 Stage 调用一次 `SetUiScene`；
4. Stage 卸载时由框架反向销毁/卸载。

一个 Stage 只能设置一个 UiScene；页面跨模式常驻时应放合适的常驻 Stage/UiScene，而不是重复创建。

## 6. 编辑器资产步骤

文本代码完成后，可操作目标 Unity 时通过 Editor API 或现有工具完成；否则向用户交付精确清单：

1. 创建 Canvas 原型 Prefab，确认 CanvasScaler、GraphicRaycaster 等组件；
2. 在启动 Scene 的 GameEngine 上绑定 `UiCanvasProto`；
3. 为页面创建 Prefab，根节点只挂 View，绑定所有 public 组件字段；
4. 不在 Prefab 上手动挂 Controller；运行时由框架创建 Controller 根节点；
5. 在 UiScene 导出场景/工具中把页面放入正确 UI Group；
6. 导出/创建 `UiSceneAsset` 到项目约定 Resources 路径；
7. 运行框架要求的 PreLoad UI/ResourcesDictionary 构建；
8. 在 Stage 中使用与导出资产一致的 Resources key；
9. PlayMode 检查打开、隐藏、关闭、重开和 Stage 卸载。

未完成这些步骤时，状态应写为“代码骨架完成，编辑器接入待办”。启动 Scene、Canvas 原型和 Build Settings 的结构及安全规则见 [主场景搭建](main-scene-setup.md)。

## 7. 生命周期与验收

职责：

- `OnUiInit`：实例存活期一次，绑定长期可复用的 View 事件；
- `OnUiOpen/Close`：每次打开/关闭的资源；
- `OnUiShow/Hide`：每次显隐刷新；
- `InitListeners`：创建 ModelWrapper 监听；
- Hud `Bind(entity)`：绑定目标 Entity，跟随与重绑按 Hud 专项规则。

验收至少覆盖：

- View 的 `GetControllerType()` 指向唯一配对 Controller；
- Controller 泛型 View 类型正确；
- Prefab 字段全部绑定；
- Model 来自 ECS/Data，不在 Controller 复制权威状态；
- 页面重开不会重复添加按钮回调或监听；
- Model 更换/Entity 销毁时监听不悬挂；
- Stage 卸载后 UiScene、Controller 和 Hud 被正确清理。
