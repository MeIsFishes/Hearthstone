# TaskBase 节点新建模板

## 使用场景

`TaskBase` 子类适合写普通托管行为：进入时初始化状态，每帧更新，结束时清理或恢复。典型例子包括播放一段动画、等待某个外部状态、移动对象、打开/关闭表现等。

如果逻辑只需要立即执行一次，改用 `TaskOnceBase`。如果逻辑需要固定间隔回调，改用 `TaskDurationBase`。如果逻辑只判断真假，改用 `TaskConditionBase`。

## 最小模板

```csharp
using BbxCommon;

public class TaskNodeExample : TaskBase
{
    public int IntValue;

    public enum EField
    {
        IntValue,
    }

    protected override void RegisterFields()
    {
        RegisterField(EField.IntValue, IntValue, (fieldInfo, context) =>
        {
            IntValue = ReadInt(fieldInfo, context);
        });
    }

    protected override void OnEnter()
    {
    }

    protected override ETaskRunState OnUpdate(float deltaTime)
    {
        return ETaskRunState.Succeeded;
    }

    protected override void OnExit()
    {
    }

    protected override void OnTaskCollect()
    {
        IntValue = 0;
    }
}
```

## 必填字段和函数

- `public enum EField`：列出需要被 Task 配置赋值的字段。
- `RegisterFields()`：逐项调用 `RegisterField`，绑定字段名、导出类型和读值回调。
- `OnUpdate(float deltaTime)`：返回 `Running`、`Succeeded` 或 `Failed`，决定节点是否继续运行。
- `OnTaskCollect()`：节点回池时清理字段、集合和缓存引用。

## 可选字段和函数

- `OnEnter()`：需要在节点进入时缓存状态、启动表现或读取外部对象时重写。
- `OnExit()`：需要在节点结束时恢复对象状态或取消临时效果时重写。
- `OnSucceeded()` / `OnFailed()`：需要按成功或失败分别处理时重写。
- `[TaskComment("...")]`：给节点或字段补充导出给编辑器的说明。
- `[TaskTag(...)]`：需要覆盖或追加编辑器标签时使用。

## 注意事项

- `EField` 项名要与字段语义保持稳定，已经用于 Task 配置的字段不要随意重命名。
- `RegisterField` 的第三个参数必须把读取结果写回当前实例字段。
- 如果字段是 `List<T>`，用 `ReadList(fieldInfo, context, ListField)`，并在回池时 `Clear()`。
- 如果字段是引用类型或自定义对象，通常用 `ReadValue<T>()` 从 Context 或 Blackboard 读取。
- 不要在构造函数中写依赖运行时数据的逻辑；对象来自对象池，运行时初始化应放在 `OnEnter()`。
