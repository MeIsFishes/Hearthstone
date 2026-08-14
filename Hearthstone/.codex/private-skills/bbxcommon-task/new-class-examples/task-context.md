# TaskContextBase 新建模板

## 使用场景

`TaskContextBase` 是一次 Task 运行的输入容器。它适合保存本次运行天然存在的数据，例如发起对象、作用对象、事件 Id、资源 key、触发源、上一段流程结果等。

Context 字段适合稳定输入。Blackboard 适合配置化参数、临时计算结果和节点之间传递的数据。

## 最小模板

```csharp
using System;
using BbxCommon;

public class TaskContextExample : TaskContextBase
{
    public int OwnerId;
    public int SubjectId;
    public string EventKey;

    public enum EField
    {
        OwnerId,
        SubjectId,
        EventKey,
    }

    public override Type GetFieldEnumType()
    {
        return typeof(EField);
    }

    protected override void RegisterFields()
    {
        RegisterInt(EField.OwnerId, OwnerId);
        RegisterInt(EField.SubjectId, SubjectId);
        RegisterString(EField.EventKey, EventKey);
    }

    protected override void OnContextCollect()
    {
        OwnerId = 0;
        SubjectId = 0;
        EventKey = null;
    }
}
```

## 必填字段和函数

- `public enum EField`：列出可被 Task 配置引用的 Context 字段。
- `GetFieldEnumType()`：返回当前具体 Context 的 `EField` 类型。
- `RegisterFields()`：用 `RegisterInt`、`RegisterFloat`、`RegisterString`、`RegisterObject` 等注册字段。
- `OnContextCollect()`：Context 回池时清理字段、集合和对象池对象。

## 可选字段和函数

- Blackboard API：可用 `SetBlackBoardDoubleValue`、`SetBlackBoardLongValue`、`SetBlackBoardObjectValue` 或泛型 `SetBlackBoardValue<T>` 写入运行时参数。
- 抽象 Context 基类：用于共享运行时字段和统一清理逻辑。
- 辅助方法：例如 `GetOwnerId()`，让不同 Context 对外暴露统一访问方式。

## 继承规则

如果多个 Context 继承同一个抽象基类，字段注册遵循以下规则：

- 需要导出给 Task 配置引用的字段，统一写在具体子类的 `EField` 中，并在子类 `RegisterFields()` 中注册。
- 纯运行时字段可以放在抽象基类里，不加入 `EField`，也不注册。
- 基类负责清理自己的运行时字段，并调用子类清理钩子；子类清理自己的字段。

## 注意事项

- 已用于 Task 配置的 Context 字段不要随意重命名。
- 新增字段尽量追加到 `EField` 末尾，避免破坏已有缓存和旧配置假设。
- Blackboard 不需要写入 `EField`，它通过字符串 key 访问。
- Context 本身来自对象池，集合、事件和对象池对象必须在回收时处理干净。
