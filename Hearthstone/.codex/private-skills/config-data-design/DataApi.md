# 使用 DataApi

逻辑里读写的数据有两类：**Component**（运行时状态）和 **Data**（静态配置）。**`DataApi`** 负责 **Data** 的存取。

## 无键：按类型一个槽

每个类型对应**一个**全局实例；多次 **`SetData`** 会**覆盖**同类型先前存入的引用。

```csharp
DataApi.SetData(monsterData);
monsterData = DataApi.GetData<MonsterData>();
```

## 有键：int 或 string

同一类型可同时用多种方式存多份，例如按 id、按名字，再额外保留一份无键全局实例。

```csharp
DataApi.SetData(monsterData.Id, monsterData);
DataApi.SetData(monsterData.Name, monsterData);

monsterData = DataApi.GetData<MonsterData>(id);
monsterData = DataApi.GetData<MonsterData>(name);
```

## 释放

使用 **`DataApi`** 的 **`ReleaseData`** 系列（与 Get/Set 对应：无键、int 键、string 键），按需要释放引用。

## 遍历与匿名数据

- **`GetEnumerator<T>()`**：迭代返回该 **`T`** 下的全部实例（无键、int 键、string 键），**去重**；即使同一对象以多种形式登记，也只出现一次。
- **`SetAnonymousData<T>(data)`**：仅能通过 **`GetEnumerator<T>()`** 访问，不能用其它 Get 取出；只清匿名项用 **`ReleaseAllAnonymousData<T>()`**；清空该类型**全部** Data（无键、各键、匿名）用 **`ReleaseAllData<T>()`**。

## int 键的存储策略

默认 int 键背后是 **`Dictionary<int, T>`**。可调用 **`DataApi.SetKeyDistribution<T>(EDataDistribution)`** 改为 **`Continuous`**，则底层为 **`List<T>`**（从 0 起按索引铺到最大键，中间空槽为默认值）。
