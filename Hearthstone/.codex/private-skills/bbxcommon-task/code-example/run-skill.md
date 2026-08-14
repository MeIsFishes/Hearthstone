# 启动 Task 代码示例

## 使用场景

如果业务工程中没有已有参考，或需要新建一条启动 Task 的流程，参考本示例。

示例展示最常见的顺序：

1. 创建 Context。
2. 写入本次运行的稳定输入。
3. 写入 Blackboard 参数。
4. 按 key 创建 Task。
5. 绑定完成回调。
6. 启动 Task。

## 示例代码

```csharp
using BbxCommon;

public static class ExampleTaskStarter
{
    public static void RunExampleTask(
        int ownerId,
        int subjectId,
        TaskBlackboardInjection initialBlackboard,
        object runtimeTarget)
    {
        var context = TaskApi.CreateContext<TaskContextExample>();
        context.OwnerId = ownerId;
        context.SubjectId = subjectId;
        context.EventKey = "OpenDoor";

        context.ApplyBlackBoardInjection(initialBlackboard);
        context.SetBlackBoardObjectValue("RuntimeTarget", runtimeTarget);

        var task = TaskApi.CreateTask("OpenDoorTask", context);
        if (task == null)
        {
            context.CollectToPool();
            return;
        }

        task.OnFinished = () =>
        {
            context.CollectToPool();
        };

        task.Run();
    }
}
```

## 说明

- `"OpenDoorTask"` 是 Task 配置 key。资源系统会用这个 key 查找不带扩展名的同名文本资源，例如 `OpenDoorTask.json`。
- `TaskContextExample` 只是示例 Context，实际业务应替换成自己的 `TaskContextBase` 子类。
- Context 字段适合放本次运行的稳定输入。
- Blackboard 适合放可配置参数、临时值或节点之间传递的数据。
- CSV 提供的一组初始 Blackboard 参数使用单单元格 `TaskBlackboardInjection`，创建 Context 后调用一次 `ApplyBlackBoardInjection`；运行时计算出的目标等动态值仍使用对应 setter 写入或覆盖。完整格式见 [配置与反序列化](../developer-docs/config-and-deserialization.md#csv-初始黑板注入)。
- 如果不需要设置 `OnFinished`，也可以直接调用 `TaskApi.RunTask(key, context)`；但此时仍要明确 Context 由谁回收。
