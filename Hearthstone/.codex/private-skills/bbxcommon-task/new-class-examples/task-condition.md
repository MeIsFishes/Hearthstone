# TaskConditionBase 节点新建模板

## 使用场景

`TaskConditionBase` 子类用于判断一个条件是否成立。它本身也是 Task，但通常作为其它 Task 的条件挂点使用：

- EnterCondition：进入前判断，失败则目标 Task 不进入。
- Condition：运行期间每帧判断，失败则目标 Task 失败结束。
- ExitCondition：运行期间每帧判断，成功则目标 Task 成功结束。

## 最小模板

```csharp
using BbxCommon;

public class TaskConditionExample : TaskConditionBase
{
    public float LeftValue;
    public float RightValue;

    public enum EField
    {
        LeftValue,
        RightValue,
    }

    protected override void RegisterFields()
    {
        RegisterField(EField.LeftValue, LeftValue, (fieldInfo, context) =>
        {
            LeftValue = ReadFloat(fieldInfo, context);
        });
        RegisterField(EField.RightValue, RightValue, (fieldInfo, context) =>
        {
            RightValue = ReadFloat(fieldInfo, context);
        });
    }

    protected override EConditionState OnConditionUpdate(float deltaTime)
    {
        return LeftValue >= RightValue
            ? EConditionState.Succeeded
            : EConditionState.Failed;
    }

    protected override void OnConditionCollect()
    {
        LeftValue = 0f;
        RightValue = 0f;
    }
}
```

## 必填字段和函数

- `RegisterFields()`：注册条件需要读取的配置字段。
- `OnConditionUpdate(float deltaTime)`：返回条件成立或失败。
- `OnConditionCollect()`：回池时清理字段。

## 可选字段和函数

- `OnConditionEnter()`：条件开始判断前初始化缓存。
- `OnConditionExit()`：条件结束时清理临时状态。
- 枚举字段：如比较方式、筛选类型、触发类型，可用 `ReadEnum<T>()` 读取。

## 注意事项

- 条件节点不要长期保存业务权威状态，只负责判断。
- 条件可以读取 Context 或 Blackboard，因此可以做成高度复用的通用判断。
- 不建议让 `TaskConditionBase` 再嵌套复杂子 Task；条件应保持便宜、明确、可预测。
