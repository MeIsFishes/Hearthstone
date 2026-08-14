# TaskOnceBase 节点新建模板

## 使用场景

`TaskOnceBase` 子类适合一次性执行的行为。它进入后只调用一次 `OnExecute()`，然后立即返回成功或失败。

常见用途：

- 结算一次伤害、治疗或资源变化。
- 写入 Blackboard。
- 派发一次请求或事件。
- 创建一次对象或触发一次表现。

## 最小模板

```csharp
using BbxCommon;

public class TaskOnceExample : TaskOnceBase
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

    protected override EOnceState OnExecute()
    {
        return EOnceState.Succeeded;
    }

    protected override void OnTaskCollect()
    {
        IntValue = 0;
    }
}
```

## 必填字段和函数

- `public enum EField`：列出需要配置赋值的字段。
- `RegisterFields()`：注册所有可配置字段。
- `OnExecute()`：一次性逻辑入口，返回 `Succeeded` 或 `Failed`。
- `OnTaskCollect()`：回池时清理字段。

## 可选字段和函数

- `[TaskComment("...")]`：给节点或字段写编辑器说明。
- 私有辅助方法：用于拆分结算逻辑，例如计算数值、写入记录、创建请求。

## 注意事项

- `TaskOnceBase` 已经密封 `OnEnter()`、`OnUpdate()`、`OnExit()`，业务逻辑只写在 `OnExecute()`。
- `TaskOnceBase` 不会返回 `Running`，不适合等待动画、等待时间或监听持续状态。
- EnterCondition 失败会阻止执行；普通 Condition 首帧失败也会让节点失败，通常不要给一次性节点挂复杂持续条件。
- 如果 `OnExecute()` 分配了对象池对象，必须明确由谁负责回收。
