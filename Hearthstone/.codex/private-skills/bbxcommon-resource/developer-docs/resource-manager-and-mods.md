# 资源管理与 Mod 底层

## 模块边界

- 对外入口：`Assets/Scripts/BbxCommon/Api/ResourceApi.cs`
- 底层实现：`Assets/Scripts/BbxCommon/ResourceManager/ResourceManager.cs`
- Mod 配置：`Assets/Scripts/BbxCommon/ResourceManager/ModSettings.cs`
- 初始化入口：`GameEngineStage.InitReflectionAndResource()` 调用 `ResourceManager.Init()`。

业务层只使用 `ResourceApi`。只有修改索引策略、来源读取方式或新增资源类型时才改 `ResourceManager`。

## 索引规则

`ResourceManager` 以**文件名去掉扩展名**作为 key，值是按来源优先级排列的 `List<FileInfo>`。路径与扩展名不参与 key，因此不同目录、不同扩展名的同名文件会进入同一槽位。

`FileInfo` 包含：

- `Path`：Resources 相对路径或磁盘绝对路径。
- `FileSource`：`Resources` 或 `Directory`。

同一来源内的同名文件没有稳定顺序保证；不要用目录或枚举顺序表达业务优先级。

## Mod 扫描与优先级

启动时遍历工作目录下的 `Mods/*/ModSettings.json`，反序列化为：

```json
{
  "Default.FullType": "BbxCommon.ModSettings",
  "Name": "MyMod",
  "Version": 1,
  "Enabled": true,
  "Priority": 1
}
```

- `Name`：Mod 名称；官方内容使用 `Native`。
- `Version`：整型版本信息。
- `Priority`：数值越小，资源顺序越靠前。
- `Enabled`：字段会被读取，但当前 `ResourceManager` **没有按该字段过滤 Mod**；在底层补齐过滤前，不要把它描述为已生效的开关。

若没有合法的 `Native` Mod，框架会创建一个内存中的默认 Native 条目。遍历到 Native 时，先加入 `Assets/Resources/` 的资源，再加入 `Mods/Native/`，所以同名情况下 Resources 位于 Native 目录资源之前。

`ResourceManager.Init()` 按单次启动初始化设计，内部集合不会在再次初始化前统一清空；不要在业务层重复调用。

## 不同来源的加载

- `TextAsset`：目录来源使用 `StreamReader`；Resources 来源使用 Unity Resources 加载。
- `Sprite`：目录来源读取字节并创建 `Texture2D` / `Sprite`；Resources 来源使用 Unity Resources，并按 key 缓存。
- CSV：`Addition` 读取全部同名文件并依次合并；`Override` 只读取最高优先级文件。业务侧通过 DataGroup 与 `config-data-design` 接入。

当前 `CollectResourcesFiles()` 只在 `UNITY_EDITOR` 下扫描 `Assets/Resources/`。修改打包资源策略时必须同时验证 Player 构建环境，不能只验证 Editor。

## 新增资源类型

1. 在 `ResourceManager` 为该类型分别处理 `Resources` 与磁盘目录来源。
2. 明确读取一个最高优先级资源，还是读取全部同名资源。
3. 如有缓存，明确缓存释放与重复加载行为。
4. 在 `ResourceApi` 暴露简洁的业务入口。
5. 验证资源缺失、同名覆盖、Native/普通 Mod、Editor 与 Player 构建。
