# 设计新 UI 页面

本章节描述**规划一层界面时**在数据与页面边界上的取舍；**单个 View / Controller 怎么写**见 [mvc-controller-view.md](new-class-examples/mvc-controller-view.md)，**Component / System 怎么声明**与玩法侧数据切分见 **`bbxcommon-ecs`**（子文档 `new-class-examples/raw-component.md`、`new-class-examples/singleton-raw-component.md`、`SKILL.md` 等）。

## 页面与 MVC

- **每个独立 UI 页面对应一对 `UiViewBase` + `UiControllerBase<TView>`**，在同一轮需求里一起设计（计时条、背包格、弹窗等各算一页或一条可复用条，按产品粒度拆）。
- 若界面需要**跟随场景中某个对象移动**（例如角色头顶血条、目标标记），应使用 **Hud**（`HudViewBase` + `HudControllerBase<TView>`），并通过 **`entity.BindHud<THudController>()`**（或 **`IHudController.Bind(entity)`**）挂到对应 **Entity** 上，而不是普通全屏/固定布局 UI。监听与 **`Bind` 的先后**见 [hud.md](new-class-examples/hud.md)。
- 界面要显示的数值、状态，**优先来自 ECS 里已有的 Component**；Controller 只负责监听与把数据反映到 `m_View`，不把「业务权威状态」只写在 MonoBehaviour 上。

## 数据放哪里：与玩法共用 Component

框架的惯例是 **「以数据为中心」**：动态状态进 **RawComponent / Singleton RawComponent**，**System** 与 **UI Controller** 都通过读写、监听同一份数据协作（示例思路：关卡计时放在 **`TimerSingletonRawComponent`**，**TimerSystem** 更新数值，**GameFailSystem** 判失败，**UiTimerController** 监听同一组件上的可监听字段刷新文本）。

设计新 UI 时先问：**这些数据 gameplay 是否也要用？**

- **会同时服务于游戏逻辑与界面**（例如 **血量、装备列表、回合行动条、关卡剩余时间**）：把字段放在**玩法侧已经在用或理应共用的 Component** 里，必要时为监听把字段做成 **`ListenableVariable<T>`** 或让组件实现 **`IListenable`**（见 [mvc-controller-view.md](new-class-examples/mvc-controller-view.md)）。避免在 UI 层复制一套。
- **仅界面关心、玩法逻辑完全不读、未来也不参与规则**（例如纯布局缓存、仅当前弹窗的临时筛选结果）：可以单独增加 **UI 专用 Component**（仍建议挂在合适 **Entity** 或单例上，并保持池化与回收约定），减少与核心玩法的耦合。

**默认倾向**：先找能否挂在现有实体或单例组件上；**只有确认 gameplay 端确实不需要这份数据**时，再新增 UI 专用 Component。类型边界与回收细节按 **`bbxcommon-ecs`** 子文档 **`new-class-examples/raw-component.md`** / **`new-class-examples/singleton-raw-component.md`** 执行。

## UiScene 与编辑器部署

页面是否加入现有 UiScene 或需要新增 UiScene，必须在页面设计时明确。UiScene 的 UiGroup、默认显隐、整体位置、缩放和 Pivot 以 UI 编辑场景为唯一配置源；具体创建、修改、编辑器场景和导出步骤必须继续执行 [UI 场景配置与导出](developer-docs/ui-scene-export.md)，不能只创建 Controller/View、只生成 Asset 或只在 Stage 中登记 Asset。

## 检查清单

- [ ] 该页是否已对应**唯一一对** View + Controller，且无重复造「第二份」核心数据。
- [ ] 静态层级是否已落在 View Prefab，动态重复条目是否使用条目 Prefab 与框架组件/项目对象池，而不是 Controller 运行时拼整页。
- [ ] 是否已明确所属 UiScene/UiGroup；涉及 UiScene 新增或调整时，是否包含 UI 编辑场景、Exporter、导出 Asset 与 GameStage 接入。
- [ ] 新字段是否**先**对齐玩法是否使用；**仅 UI 使用**时再落 UI 专用 Component。
- [ ] 需要监听的字段已按 [mvc-controller-view.md](new-class-examples/mvc-controller-view.md) 与 **`bbxcommon-ecs`**（`new-class-examples/raw-component.md` 等）约定落在 Component 上并做好生命周期清理。
