# 配置与反序列化

## 数据层级

运行时 Task 配置以三层结构表达：

- `TaskGroupInfo`：一个完整 Task 图，包含 Context 类型、根节点 id 和所有节点。
- `TaskValueInfo`：单个节点，包含节点类型、字段赋值、条件引用和 Timeline 子项。
- `TaskFieldInfo`：单个字段赋值，包含字段名、值来源和值字符串。

字段来源由 `ETaskFieldValueSource` 表达：

- `Value`：固定值，由导出字符串解析成目标类型。
- `Context`：从 `TaskContextBase` 注册字段读取。
- `Blackboard`：从 Context Blackboard 读取，适合运行时动态值。

## 加载路径

`TaskManager.CreateTask(key, context, run)` 先查缓存：

1. 如果 `m_Tasks` 中已有 key，直接使用缓存的 `TaskBridgeGroupInfo`。
2. 否则通过 `ResourceApi.LoadTextAsset(key)` 读取 JSON。
3. JSON 反序列化为 `TaskGroupInfo`。
4. `TaskGroupInfo` 转换为 `TaskBridgeGroupInfo` 并缓存。

业务也可以通过 `TaskApi.RegisterTask(key, TaskGroupInfo)` 直接注册代码构造出的模板。

## 桥接数据

桥接数据用于把编辑器导出的字符串结构转换为更便宜的运行时结构：

- `TaskBridgeGroupInfo`：保存 Context 类型、根节点连续下标、重排字典和节点列表。
- `TaskBridgeValueInfo`：保存节点 `Type`、typeId、普通字段、Blackboard 字段、条件引用和 Timeline 子项。
- `TaskBridgeFieldInfo`：保存字段 int 下标、值来源和可复用的 `TaskBridgeConstValue`。

节点 id 会被重排为 0 到 n-1 的连续下标，使连接子节点、条件节点和 Timeline 子项时可以直接访问 List。

## 创建节点

CreateTask 的节点创建流程：

1. 校验 Task 模板绑定的 Context 类型是否等于传入 Context 类型。
2. 调用 `context.Init(taskGroupInfo)` 注册 Context 字段。
3. 遍历 `TaskBridgeValueInfo`，按节点类型从对象池分配 `TaskBase` 子类实例。
4. 写入 `TaskValueInfo` 和 `TaskContext`。
5. 对普通字段调用 `ReadFieldInfo()`，并把字段标记为已初始化。
6. 初始化 `TaskConnectPoint` 引用。
7. 绑定 Timeline 子项和条件引用。
8. 取得根节点，必要时加入运行队列。

## 字段读值缓存

普通字段在 CreateTask 时读取。第一次读取时会把字符串解析结果写入 `TaskBridgeConstValue`，之后复用同一桥接字段时可以少做解析。

Context 来源字段第一次会把 Context 字段名转换成 int 下标，后续通过 `GetConstValue(index)` 读取。

Blackboard 来源字段不会只读一次，而是在 Task 每次 `Enter()` 时重新读取。这是因为 Blackboard 值可能由前置节点动态写入。

## CSV 初始黑板注入

需要从配置表为 TaskContext 注入一组初始 Blackboard 值时，统一使用 `TaskBlackboardInjection` 特殊类型，不在业务代码中逐项调用 setter。一个对象完整保存在一个 CSV 单元格中，内层格式为：

```text
Duration,int,8;Speed,double,1.2;Name,string,Missle
```

因为内层含逗号，CSV 文件中的完整字段必须使用双引号包裹：

```csv
"Duration,int,8;Speed,double,1.2;Name,string,Missle"
```

- 每项是 `Key,Type,Value`，多项以 `;` 分隔，Key 不得为空或重复。
- 类型只允许 `bool`、`int`、`long`、`float`、`double`、`string`。bool/整数进入 long 黑板，浮点数进入 double 黑板，string 进入 object 黑板。
- Key 或 string 值中的 `\`、`,`、`;` 分别使用 `\\`、`\,`、`\;` 转义。
- CSV 数据类公开同名 `TaskBlackboardInjection` 属性，并在 `ReadLine()` 中调用 `ParseTaskBlackboardInjectionFromKey`；元数据导出为 `TaskBlackboardInjection` Kind。
- 创建 Context 后调用 `context.ApplyBlackBoardInjection(data)` 一次写入。运行时计算出的值仍可随后通过普通 setter 覆盖。

## 字段类型支持

TaskBase 提供常用读取函数：

- 基础数值：`ReadBool`、`ReadShort`、`ReadInt`、`ReadLong`、`ReadFloat`、`ReadDouble` 等。
- 枚举：`ReadEnum<T>()`。
- 字符串：`ReadString()`。
- 引用或自定义对象：`ReadValue<T>()`。
- 列表：`ReadList<T>()`，常量支持 bool、char、sbyte、byte、short、ushort、int、uint、long、ulong、float、double、decimal、string 和枚举。
- 字典：`ReadDictionary<TKey, TValue>()`。
- 子 Task 连接：`ReadConnectPoint()`。

列表常量使用 `TaskExportCrossVariable.ListElementSplit` 分隔。当前列表常量主要支持基础类型、字符串和枚举；自定义类型列表不适合作为常量解析。

Dictionary 常量不使用列表分隔符，而是在 `TaskFieldInfo.Value` 字符串中嵌入 CrossLibrary `JsonApi.SerializeToString(dictionary)` 生成的完整字典 JSON。JSON 保留 `Default.TypeInfo`、`N, Key` 和 `N, Value` 旧协议结构；运行时由 `ReadDictionary<TKey, TValue>()` 调用 `JsonApi.DeserializeFromString<Dictionary<TKey, TValue>>()` 还原并缓存。Dictionary 的键和值只应使用 JsonApi 能稳定转换的基础类型、字符串或枚举，不应嵌套自定义类对象。编辑器侧必须检查泛型类型信息、连续完整的键值条目，以及按声明类型转换后的键唯一性；仅结构完整的早期 `%||%` 键值交替数据允许迁移。旧 JsonApi 把字符串字面量 `"null"` 解释为空引用，而 `Dictionary` 不允许空键，因此 string 键和值均不得使用该字面量。

## 修改风险

- 修改 Task 字段名会影响旧 Task 配置读取。
- 修改 Context 字段名会影响从 Context 来源取值的配置。
- 改动桥接结构、节点 id 重排或 `TaskBridgeFieldInfo.Inited` 行为，会影响运行时性能和旧配置兼容。
- 改动 Blackboard 读入时机，会影响依赖动态参数的节点。
- 修改 `TaskBlackboardInjection` 的类型名、分隔符或转义规则会同时影响 Unity CSV、BbxEditor 校验和既有数据。
