# GameStage 里可以添加哪些项、如何添加

以下顺序为 **`GameStage`** 在一次 **Load** 中的**先后**；**卸载**为对应**逆序**。

---

## 1. 加载管线总览

| 顺序 | 内容 | 配置方式 / API |
|------|------|----------------|
| ① | **LoadItem**（早） | **`AddLoadItem(IStageLoad)`** 或 **`AddLoadItem<T>()`** |
| ② | **场景** | **`AddScene(params string[] scenes)`**（Additive） |
| ③ | **UiScene** | **`SetUiScene(UiSceneBase, UiSceneAsset)`**（每个 Stage 仅一次） |
| ④ | **DataGroup** | **`AddDataGroup(string group)`** |
| ⑤ | **Tick（ECS System）** | **`AddUpdateSystem<T>()`**、**`AddFixedUpdateSystem<T>()`** |
| ⑥ | **StageListener** | **`AddStageListener<T>()`**，**`T : StageListenerBase`** |
| ⑦ | **LateLoadItem**（晚） | **`AddLateLoadItem(IStageLoad)`** 或 **`AddLateLoadItem<T>()`** |

业务 Stage 一般只使用上表中的 API。

> 当前实现中，DataGroup 之后会直接挂载 System、调用 StageListener，最后才执行 LateLoadItem；尚不存在独立的“Data 已就绪、System / Listener 尚未启用”公开注册阶段。规范中涉及的启动数据初始化必须遵守下文的当前兼容限制，不能把待补的底层阶段当作现有 API。

---

## 2. 各类项说明与接口

### 2.1 `IStageLoad`（LoadItem / LateLoadItem）

```csharp
public interface IStageLoad
{
    void Load(GameStage stage);
    void Unload(GameStage stage);
}
```

- **注册**：**`AddLoadItem`**（早）或 **`AddLateLoadItem`**（晚）；或 **`AddLoadItem<T>()`** / **`AddLateLoadItem<T>()`**（`new T()`）。
- **用途**：**`Load`** 里初始化（Entity、GameObject、单例 Component 等），**`Unload`** 里成对释放。

### 2.2 场景

- **`AddScene("SceneName")`**：Additive 加载；卸载时卸载对应场景。

### 2.3 UiScene

- **`SetUiScene(uiScene, uiSceneAsset)`**：按资产创建 UI；卸载时销毁。需先通过 GameEngine **`CreateUiScene` / `GetOrCreateUiScene`** 得到 **`UiSceneBase`**。
- `uiSceneAsset` 必须来自仓库内对应 UI 编辑场景的 `UiSceneExporter`；注册前核对编辑场景、UiGroup、View Prefab 实例、导出路径和 Asset 内容。不得直接构造或手写 `UiSceneAsset.UiObjectDatas`。
- 只有 `UiSceneBase` 类型、只有 Asset 或只有 `SetUiScene` 调用都不是完整接入；完整步骤见 `bbxcommon-ui` 的 [UI 场景配置与导出](../bbxcommon-ui/developer-docs/ui-scene-export.md)。

### 2.4 DataGroup

- **`AddDataGroup("GroupName")`**：按组加载 **`BbxScriptableObject`** 与 **`ResourceApi.DataGroupCsvPairs`** 中注册的 CSV。详情见 **`config-data-design`** skill。

### 2.5 ECS System

- **`AddUpdateSystem<T>()`**：每帧 **`UpdateSystemGroup`**。
- **`AddFixedUpdateSystem<T>()`**：进入 GameEngine 托管的 FixedUpdate 有序组，该组运行在 DOTS **`FixedStepSimulationSystemGroup`** 内。
- 打了 **`[DisableAutoCreation]`** 的 System 须在此注册。最终顺序由 GameEngine 的 **`RegisterSystemOrder(typeof(...), ...)`** 类型列表决定，不使用 `UpdateBefore` / `UpdateAfter`。
- Update 与 FixedUpdate 分属不同更新频率，只在各自组内复用同一份类型顺序。未登记 System 保持原相对顺序并追加到已登记项末尾。

### 2.6 StageListener

- **`AddStageListener<T>()`**：在 **System 已挂到 World** 之后、**LateLoadItem 之前** 执行 **`OnLoad`**（初始化监听）。卸载时 **`OnUnload`**。

### 2.7 其它

- **`SetStageData` / `GetStageData`**：Stage 上挂生命周期级自定义数据。当前 API 使用字符串键和 `object` 值；业务代码必须把键集中定义为常量，并在工厂与初始化项之间只传递一个强类型 `XxxStageStartupData` 快照，禁止散布多个魔法字符串或用 `Dictionary<string, object>` 表达业务字段。
- **`PreLoadStage` / `PostLoadStage` / `PreUnloadStage` / `PostUnloadStage`**：整段加载/卸载前后的委托。

`StageData` 适合作为 Stage 创建与加载之间的中转，不是运行时黑板：

- 只存入口选择、外部初始值和加载所需的不可变快照。
- 数据组加载完成后，由一个专用 `IStageLoad` 把 ID 解析为正式配置，再创建 ECS Component。
- System 与 UI 只消费 ECS Component 和 `DataApi` 中的正式配置，不逐帧访问 `StageData`。
- Stage 实例可能卸载后再次加载；保留的启动快照必须可重复使用。可变对象、池对象或需要逐次释放的资源不得无所有权约定地放入 `StageData`。

详细约束见 [stage-startup-data-convention.md](./stage-startup-data-convention.md)。

---

## 3. `LoadItem` 与 `LateLoadItem` 的差异

| | **LoadItem** (`AddLoadItem`) | **LateLoadItem** (`AddLateLoadItem`) |
|---|------------------------------|--------------------------------------|
| **执行时机** | **最先**一批：在场景、UI、Data、System、Listener **之前** | **最后**一批：在场景、UI、Data、System、Listener **之后** |
| **典型用途** | 不依赖后续阶段；或同批内**按添加顺序**靠前的步骤 | 依赖本 Stage 已加载的 **Data**、**场景**、**UiScene**、**System**、**StageListener** 之后再执行的逻辑 |
| **卸载** | 整段卸载流程中**较晚**执行（与加载顺序相反） | **较早**执行（先于 Listener / System / Data / UI / 场景 / 早批 LoadItem） |
| **实现** | 同一 **`IStageLoad`**；**`Load` / `Unload` 成对** | 相同 |

**要点**：

- 多个 **`AddLoadItem`** 按**添加顺序**依次 **`Load`**，每项之间会 **`await UniTask.NextFrame()`** 分摊帧负载；整批仍早于场景/UI/Data/System。
- 若逻辑**必须**在 **Data、场景、UiScene、System 或 StageListener 就绪之后**运行，用 **`AddLateLoadItem`**，不要仅靠「同一批 LoadItem 里排在后面」。

### 启动数据初始化的当前兼容写法

需要读取 DataGroup 并创建运行时 Component 的初始化项，当前只能临时注册为第一个 `LateLoadItem`。这能保证它读取到正式配置，但 StageListener 已先于它执行，而且 LateLoadItem 之间会跨帧，不能用当前管线笼统保证所有 System 在完整初始化结束前都不会更新。因此：

- 当前 Stage 的 Listener 不得依赖这些尚未创建的 Component；System 必须能在初始化标记就绪前安全保持休眠。只要任一消费者无法满足该条件，就必须先补齐底层数据就绪阶段，不能靠注册顺序碰运气。
- 该初始化项必须是第一个 LateLoadItem；其它依赖其结果的 LateLoadItem 排在它之后，并自行处理跨帧加载期间的可见状态。
- 初始化项的 `Unload` 必须移除自己创建的单例 Component、Entity、对象池和绑定，并允许同一 Stage 实例再次加载。
- 后续底层同步应在 DataGroup 之后、System 与 StageListener 之前增加明确的数据就绪初始化阶段；在该 API 真正落地前，本 Skill 不把任何拟议名称写成可调用接口。

---

## 4. 卸载顺序（与加载相反）

自 Stage 卸载起，大致为：**LateLoadItem → StageListener → 移除 System → DataGroup → UiScene → 场景 → LoadItem（早批）**。
