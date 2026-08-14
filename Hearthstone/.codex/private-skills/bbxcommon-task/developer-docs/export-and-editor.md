# 导出与编辑器数据

## 导出入口

Unity 编辑器菜单：

```text
BbxCommon/ExportAllTasks
```

对应代码是 `ExportTaskInfo.ExportAllTasks()`。

导出路径当前为：

```text
../ExportedTaskInfo/
```

该路径相对 Unity 编辑器工程工作目录解析。

## 导出内容

导出流程遍历反射可见的类型：

- 非抽象 `TaskBase` 子类导出为 `TaskExportInfo`。
- 非抽象 `TaskContextBase` 子类导出为 `TaskContextExportInfo`。
- Task 字段中引用到的枚举类型导出为 `TaskEnumExportInfo`。

导出信息供外部 Task 编辑器读取，用来知道有哪些节点、字段、字段类型、标签和说明。

## TaskExportInfo

`TaskExportInfo` 主要包含：

- `TaskTypeName`
- `TaskFullTypeName`
- `Comment`
- `Tags`
- `FieldInfos`

字段说明来自：

- Task 类上的 `[TaskComment("...")]`。
- `EField` 枚举项上的 `[TaskComment("...")]`。
- 字段本身的 `[TaskComment("...")]`。

## 标签

如果 Task 类没有用 `[TaskTag]` 覆盖标签，框架按基类自动添加：

- `TaskDurationBase`：`Action`、`Duration`
- `TaskOnceBase`：`Action`、`Once`
- `TaskConditionBase`：`Condition`
- 普通 `TaskBase`：`Action`、`Normal`

驱动节点可用：

```csharp
[TaskTag(TaskTagAttribute.ESetTag.Override, TaskExportCrossVariable.TaskTagDrive)]
```

Timeline 使用 `TaskTagTimeline`。

## TaskContextExportInfo

`TaskContextExportInfo` 主要包含：

- `TaskContextTypeName`
- `FieldInfos`

Context 字段类型来自 `RegisterFields()` 中的 `RegisterInt`、`RegisterObject` 等注册调用。

## 编辑器数据与运行时数据

Task 编辑器通常保存两类数据：

- 编辑时数据：节点位置、连线表现、编辑器状态等，供下次继续编辑。
- 运行时数据：`TaskGroupInfo`、`TaskValueInfo`、`TaskFieldInfo`，供 Unity 运行时构建 Task。

运行时不应依赖编辑器专用数据。业务侧只应关心导出的 Task key、绑定 Context 类型、节点字段和 Blackboard 参数。

## 新增节点后的检查

新增或修改 Task 节点后，应检查：

- 节点类不是 abstract，且可被反射创建。
- 字段都在 `EField` 和 `RegisterFields()` 中注册。
- 字段类型能被 `TaskApi.GenerateTaskTypeInfo()` 识别。
- 需要给设计者看的字段补充了 `TaskComment`。
- 导出后编辑器能看到节点、字段、枚举值和标签。

新增或修改 Context 后，应检查：

- `GetFieldEnumType()` 返回具体 `EField`。
- `RegisterFields()` 注册所有需要配置引用的字段。
- 抽象基类没有被当作可直接使用的运行 Context。
- 已有字段名没有被随意重命名。
