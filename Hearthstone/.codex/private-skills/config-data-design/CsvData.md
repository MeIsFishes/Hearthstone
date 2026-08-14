# 创建 CsvData（CsvDataBase）

CSV 表用于**多行**配置；运行时由 **`ResourceManager.LoadCsv`** 读 **`TextAsset`**，解析后每行构造一个 **`CsvDataBase<T>`** 实例并调用 **`ReadLine()`**。基类与解析辅助方法：`Assets/Scripts/BbxCommon/CrossLibrary/Serializer/CsvDataBase.cs`。

## 类型声明

1. **定义类** `class YourCsvData : CsvDataBase<YourCsvData>`（`T` 与自身相同，且有无参 `new()`）。
2. **实现** `public override string[] GetTableNames()`：返回资源字典中的 **key**（通常与 CSV 文件名不含扩展名一致），例如 `SkillCsvData` 返回 `"SkillCsvData"`。框架按 key 查找文件；若 key 不存在，Stage 加载时会打 **`LoadCsv` 失败**类警告。
3. **可选** `public override string GetDataGroup()`：默认 **`"GameEngineDefault"`**；必须与 **`GameStage.AddDataGroup`** 及引擎初始化里 **`ResourceApi.DataGroupCsvPairs`** 的注册一致。
4. **可选** `public override EDataLoad GetDataLoadType()`：
   - **`Addition`**：可加载**多个**同名 key 的文件，依次合并（适合 Mod 追加行、多文件叠加）。
   - **`Override`**：只取**一个**文件内容（后者覆盖语义由资源列表顺序决定；常用于主表独占）。

**注册进数据组**：引擎 **`InitReflectionAndResource`**（`GameEngineStage.cs`）会反射所有非抽象 **`CsvDataBase` 子类**，无参构造一个实例，读 **`GetDataGroup()`**，加入 **`ResourceApi.DataGroupCsvPairs[group]`**。无需手写登记，但 **必须有无参公共构造函数**（或反射可访问的无参构造）。

## ReadLine 与列

- CSV **第一行**为列名，与代码里 **`ParseIntFromKey("Id")`** 等 **key** 一致。
- 以 **`//`** 开头的行视为注释，跳过。
- 每张 CSV 的表头后必须紧跟两行规范注释，之后才是数据行：
  1. 第一行是字段说明。使用英文半角逗号分隔，并与表头逐列一一对应；每项必须用英文具体说明对应字段的业务含义。整行以 `// ` 开头，字段说明内部不要再使用逗号。
  2. 第二行是跨表关联。只要本表字段会以 ID/CueId 等键指向另一张 CSV，或另一张 CSV 会以此类键指向本表，就写成 `// Associated: TableA, TableB`，表名使用不含扩展名的完整 CSV 文件名并按字母序排列；没有跨表键关联时写 `// Associated: None`。资源 key、Task key、枚举和普通数值不算 CSV 关联。
- 示例：

```csv
Id,WeaponId,DisplayName
// Unique row identifier,Weapon row identifier from WeaponCsvData,Display name shown to players
// Associated: WeaponCsvData
1,101,SCOUT
```

- 列数必须与表头一致；包含逗号或双引号的单元格按标准 CSV 写法用双引号包裹，单元格内部的双引号写成 `""`。运行时支持带引号的单行字段，不支持字段内部换行。
- 基类提供 **`GetStringFromKey`**、`ParseIntFromKey`、`ParseFloatFromKey`、`ParseEnumFromKey<T>`、**数组**（如 **`ParseIntArrayFromKey`**，默认 **`;`** 分隔）等。

### Unity 特殊值类型格式

`Color`、`Vector2`、`Vector3`、`Vector4` 各占一个 CSV 单元格，表头必须与公开字段或属性同名：

| 代码类型与成员 | CSV 表头 | 单元格格式 | 读取方式 |
| --- | --- | --- | --- |
| `Color UiColor` | `UiColor` | `#RRGGBB` 或 `#RRGGBBAA` | `UiColor = ParseColorFromKey(nameof(UiColor), Color.white);` |
| `Vector2 Offset` | `Offset` | `x;y`，例如 `1.5;-2` | `Offset = ParseVector2FromKey(nameof(Offset));` |
| `Vector3 Position` | `Position` | `x;y;z`，例如 `1;2;3` | `Position = ParseVector3FromKey(nameof(Position));` |
| `Vector4 Value` | `Value` | `x;y;z;w`，例如 `1;2;3;4` | `Value = ParseVector4FromKey(nameof(Value));` |
| `TaskBlackboardInjection Blackboard` | `Blackboard` | `Key,Type,Value;...`，例如 `"Duration,int,8;Speed,double,1.2;Name,string,Missle"` | `Blackboard = ParseTaskBlackboardInjectionFromKey(nameof(Blackboard));` |

- Vector 分量固定使用半角分号 `;` 分隔，不加括号；分量数量必须与类型维度完全一致，数字使用小数点 `.`。
- 不要使用逗号分隔 Vector 分量；Vector 的内部协议固定为分号。
- 每个读取函数都可传对应类型的 `defaultValue`。单元格为空、分量数量错误或任一分量非法时，整个值返回 `defaultValue`；非法非空值会记录 CSV 错误。
- Color 必须带开头的 `#`；非法或空颜色返回传入的默认值。
- 元数据导出器按同名公开字段或属性直接导出 `Color`、`Vector2`、`Vector3`、`Vector4`、`TaskBlackboardInjection` Kind；字段不是公开成员或表头名称不匹配时，会退化为无绑定的 `String`。

### Task Blackboard 注入单元格

- 内层固定为 `Key,Type,Value`，多项用半角分号 `;` 分隔；同一单元格不允许重复 Key。
- 支持的类型名为小写 `bool`、`int`、`long`、`float`、`double`、`string`。整数和 bool 写入 TaskContext 的 long 黑板，浮点数写入 double 黑板，字符串写入 object 黑板。
- 因内层格式含逗号，保存到 CSV 文件时整个单元格必须使用标准 CSV 双引号包裹，例如 `"Duration,int,8;Speed,double,1.2;Name,string,Missle"`；读取后的实际值不包含外层双引号。
- Key 或 string 值中的反斜杠、逗号、分号分别写成 `\\`、`\,`、`\;`。数值使用不受区域设置影响的小数点 `.`；不接受 NaN 和 Infinity。
- `TaskBlackboardInjection.Serialize()` 负责生成内层规范字符串，`TaskContextBase.ApplyBlackBoardInjection(...)` 一次写入全部初始值。

在 **`ReadLine()`** 末尾通常：

- **`DataApi.SetData(intKey, this)`** 或 **`DataApi.SetData(stringKey, this)`**，供运行时按 id 查询；
- 如需 O(1) 静态查询，可同步写入 **`public static Dictionary<int, YourCsvData>`**（项目示例：`SkillCsvData.DataById`）。

参考实现：`Assets/Scripts/Chaos/Config/Csv/SkillCsvData.cs`。

## 加载时机

**`GameStage.OnLoadStageData`** 在加载 ScriptableObject 同阶段，对每个 data group 遍历 **`ResourceApi.DataGroupCsvPairs[group]`** 中的 **模板实例**，对 **`GetTableNames()`** 返回的每个 name 调用 **`ResourceManager.LoadCsv(name, csvObj)`**。因此：**Stage 必须 `AddDataGroup` 包含该表的 `GetDataGroup()`**。

## 特殊模式：不按表名自动加载

若 **`GetTableNames()` 返回空数组**，则不会参与上述自动按 key 加载；例如 **`LocKeyCsvData`** 由 **`LocApi`** 在切换语言时按名称再调 **`LoadCsv`**（见 `LocCsvData.cs`）。仅在确有动态表名需求时使用。

## 检查清单

- [ ] 继承 **`CsvDataBase<T>`**，并实现 **`ReadLine()`**、**`GetTableNames()`**。
- [ ] 资源 **`ResourcesDictionary`**（或项目实际使用的 key 体系）中存在 **`GetTableNames()`** 中的 key。
- [ ] **`GetDataGroup()`** 与加载该表的 Stage 的 **`AddDataGroup`** 一致。
- [ ] **`EDataLoad`** 与是否需要多文件合并/覆盖一致。
- [ ] 行数据已 **`DataApi.SetData(...)`**（若逻辑需要按 id 查询）。
- [ ] 表头后的第一行注释包含与表头等量的英文半角逗号分隔说明，第二行使用 `// Associated: ...` 或 `// Associated: None`。
- [ ] Associated 表名来自真实的跨表键引用，双向列出关联并排除资源 key、Task key、枚举和普通数值。
- [ ] `Color` / Vector / Task Blackboard 特殊字段（若有）各占一个单元格，表头与公开字段或属性同名，值遵循对应格式。

返回选型与对照见 [SKILL.md](SKILL.md)。
