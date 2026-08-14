# TaskDurationBase 节点新建模板

## 使用场景

`TaskDurationBase` 子类适合持续一段时间、并可按固定间隔执行逻辑的节点。它自带两个字段：

- `Duration`：持续时间。小于 0 表示不会因时间主动结束。
- `Interval`：间隔回调。等于 0 表示每帧调用 `OnInterval()`，小于 0 表示不调用间隔逻辑。

## 最小模板

```csharp
using BbxCommon;

public class TaskDurationExample : TaskDurationBase
{
    public int IntValue;

    public enum EField
    {
        IntValue,
    }

    protected override void RegisterTaskFields()
    {
        RegisterField(EField.IntValue, IntValue, (fieldInfo, context) =>
        {
            IntValue = ReadInt(fieldInfo, context);
        });
    }

    protected override void OnTaskEnter()
    {
    }

    protected override EDurationState OnInterval()
    {
        return EDurationState.Running;
    }

    protected override void OnTaskExit()
    {
    }

    protected override void OnTaskCollect()
    {
        base.OnTaskCollect();
        IntValue = 0;
    }
}
```

## 必填字段和函数

- `RegisterTaskFields()`：注册业务字段。`Duration` 和 `Interval` 已由基类注册，不要重复注册。
- `OnInterval()`：间隔逻辑入口，返回 `Running` 或 `Failed`。
- `OnTaskCollect()`：如果重写，必须先调用 `base.OnTaskCollect()`，让基类重置 `Duration` 和 `Interval`。

## 可选字段和函数

- `OnTaskEnter()`：持续节点进入时初始化。
- `OnTaskExit()`：持续节点退出时清理或恢复。
- 业务字段：用于描述周期效果的目标、数值、倍率、黑板 key 等。

## 注意事项

- `OnInterval()` 不接收 `deltaTime`，需要间隔值时读取 `Interval`。
- 如果某一帧跨度超过多个 `Interval`，基类会补偿调用多次 `OnInterval()`，但不会超过 `Duration` 限制。
- `Duration > 0` 且时间到达后，节点会成功结束；`Duration < 0` 时需要外部条件或自身逻辑让节点结束。
- 当前脚本模板中若出现 `OnCollect()`，以当前代码为准，应改为 `OnTaskCollect()`。
