# 任务元数据与 Inspector 设计

## 模块说明

本模块把 Unity 导出的 Task、TaskContext 和 Enum JSON 转成平台无关的 `TaskCatalog`，再由 WPF Inspector 根据声明类型生成字段编辑器。编辑器不直接加载 Unity 游戏程序集，也不通过反射实例化游戏任务类。

字段值来源包括 Value、Context 和 Blackboard。ConnectPoint 由行为树端口负责，不在 Inspector 中作为普通常量编辑。常量支持基础标量、枚举、List 和 Dictionary；集合元素只支持基础类型或枚举，不支持类对象和复杂类型嵌套。

## UI 交互

- 节点类型选择器按调用场景过滤候选集，并按显示名、类型名、标签和说明搜索；显示名沿用现有去前缀规则。
- Context 同时接受短名称和完整名称；未来保存优先使用元数据提供的完整名称，显示时继续移除命名空间和 `TaskContext` 前缀。
- Value 根据类型显示文本框、布尔/枚举选项或结构化 List/Dictionary 编辑器。
- List 和 Dictionary 支持添加、删除及上下排序；Dictionary 同时编辑键和值。
- Context 来源从当前绑定 Context 的兼容字段中选择；Blackboard 来源编辑键名。
- Timeline Action 被选中时，Inspector 最上方显示 StartTime 和 Duration，时间字段与轨道拖拽结果双向同步；Condition 字段显示在同一 Inspector 中时仍保留所属 Action 的时间卡片。

## 数据来源

- `ExportedTaskInfo` 目录中的 `TaskExportInfo`、`TaskContextExportInfo` 和 `TaskEnumExportInfo` JSON。
- 当前文档的 `BindingContextType`。
- 当前选中 `TaskInstance` 的字段值、来源、类型和注释。
- CrossLibrary 中的标签、类型名及集合协议常量。

## 设计约束

- 未知任务、Context、枚举或不兼容字段必须产生诊断，不静默回退为任意文本。
- 不支持集合嵌套和集合中的类对象；验证失败时禁止运行时导出。
- Dictionary 键在转换到声明类型后必须唯一。
- 旧 JsonApi 将 string 字面量 `"null"` 解释为空引用，因此 Dictionary 的 string 键和值禁止该字面量。
