# 创建 BbxScriptableObject 配置

`BbxScriptableObject` 继承 **`UnityEngine.ScriptableObject`**，在 **`Load()`** 时调用 **`OnLoad()`**（Stage 卸载时对已跟踪资源调用 **`Unload()` → `OnUnload()`**）。源码：`Assets/Scripts/BbxCommon/Misc/ScriptableObject/BbxScriptableObject.cs`。

## 最小类型骨架

1. **继承** `BbxCommon.BbxScriptableObject`。
2. **实现** `protected override void OnLoad()`；绝大多数配置在开头执行 **`DataApi.SetData(this)`**，以便 **`DataApi.GetData<T>()`** 按类型取单例引用。
3. 若需在 Stage 卸载时释放句柄、清缓存，**重写** `protected override void OnUnload()`（基类默认为空）。
4. 使用 **`[CreateAssetMenu(...)]`** 在 Project 菜单创建资产；编辑器内置模板可参考 `Assets/Scripts/BbxCommon/Editor/ScriptTemplate/ScriptableObjectTemplate.txt`。

## LoadingType 与分组

- **`AutoLoading`**：通过编辑器 **`BbxScriptableObject.ExportAssetPath`** 登记时，默认归入 **`GameEngineDefault`** 组（见 `BbxScriptableObject.cs` 中 `ExportAssetPath`）。
- **`GroupedByName`**：指定 **`GroupName`**，资产路径写入同一 **`ScriptableObjectAssets`** 下的该组；只有 **GameStage** 在加载时 **`AddDataGroup`** 包含了该组，对应 SO 才会被 **`Resources.Load` + `Instantiate` + `Load()`**。

`ScriptableObjectAssets` 资源路径常量：**`BbxVar.ExportScriptableObjectPathInResource`**（`BbxCommon/ScriptableObjectAssets`）。保存资产时 **`FileModificationCallback`** 可对 `BbxScriptableObject` 自动触发 **`ExportAssetPath`**，保持登记最新。

## 运行时如何被加载

**`GameStage.OnLoadStageData`**（`GameStage.cs`）会：

1. 读取 **`ScriptableObjectAssets`** 中当前 Stage 请求的每个 **data group** 下的资产路径；
2. 对每个路径 **`Resources.Load`**，若为 **`BbxScriptableObject`**，则 **`Object.Instantiate`** 后调用 **`Load()`**（即 **`OnLoad()`**）；
3. 将**原始**资产引用加入集合，**卸载 Stage** 时对这些资产调用 **`Unload()`**。

因此：**你的引擎/玩法 Stage 必须在合适的时机 `AddDataGroup("...")`**，且资产已登记到 **`ScriptableObjectAssets`** 的对应组，否则 **`OnLoad` 不会执行**，`DataApi` 里也没有这份数据。

## DataApi 用法提示

- 无键 **`SetData(this)`** 与 **`GetData<T>()`** 搭配时，**同一类型只有一个全局槽**；重复 `SetData` 覆盖前者。
- 若一种 SO **需要多实例**且要用键区分，可改用 **`DataApi.SetData(key, instance)`**（与设计一致即可）。

## 检查清单

- [ ] `OnLoad` 中已 **`DataApi.SetData(...)`**（或按设计使用键）。
- [ ] **`LoadingType` / `GroupName`** 与目标 Stage 的 **`AddDataGroup`** 一致。
- [ ] 资产已进入 **`ScriptableObjectAssets`**（保存/导出路径正确）。

返回选型与对照见 [SKILL.md](SKILL.md)。
