# GameStage 启动数据中转规范

## 1. 何时需要

当一个 Stage 必须由外部决定关卡、角色、队伍、敌人编成或其它初始选择，且缺少这些值就不能正确运行时，为它定义一个强类型 `XxxStageStartupData`。

如果 Stage 的全部内容都能从自身固定配置和已加载框架服务中推导，不创建空的 StartupData，也不为了形式统一增加泛型基类。

## 2. 职责与单向数据流

```text
正式游戏入口默认值 / Editor Group 入口配置资产
    → Group 入口
    → XxxStageStartupData 独立快照
    → DataGroup / DataApi 中的正式配置
    → ECS Component 可变运行状态
    → System / UI
```

各层职责固定如下：

| 层 | 保存内容 | 不应承担 |
|---|---|---|
| 入口配置资产 | 供 Editor 编辑的入口选择和序列化数据 | 作为运行时状态或被游戏逻辑修改 |
| `XxxStageStartupData` | 本次创建 Stage 必需、无法由 Stage 自行推导的输入 | 复制完整配置表、记录持续变化的战斗状态 |
| `DataApi` 正式配置 | CSV、ScriptableObject 等静态权威配置 | 保存单局可变状态 |
| ECS Component | 血量、位置、冷却、波次进度等运行状态 | 反向同步到 StartupData 或入口资产 |

禁止形成 `Component → StartupData → Component` 的循环，也不保留两份可变运行状态。初始化完成后，ECS Component 是运行状态的唯一来源。

## 3. 构造与消费时机

StartupData 的生命周期必须绑定一次明确的 Group 切换请求：

1. 正式流程或 Editor Group 入口先取得玩家选择、关卡选择等原始输入；
2. 在调用目标 Stage 工厂之前构造 `XxxStageStartupData`；
3. Stage 工厂立即完成结构校验、防御性快照和 `SetStageData`；
4. Group 入口最后调用一次 `SetActiveGameStage(...)`；
5. DataGroup 就绪后，专用 `IStageLoad` 从 StageData 读取快照一次，解析正式配置并建立 ECS 运行状态；
6. 初始化完成后，普通 System 与 UI 只消费 ECS Component 和 `DataApi`，不再读取 StartupData。

StartupData 不是 Scene 加载参数，也不由 GameStage 框架自动构造。禁止在 Additive Scene 加载、System 首帧、StageListener 或 Controller 中临时补造。卸载后重新激活同一 Stage 实例时，初始化项可再次消费该实例保留的同一快照；如果本次进入需要不同输入，必须创建新的 StartupData 和新的 Stage 实例。

Group 入口可以从原始业务选择构造 StartupData，也可以接收由 Editor 入口已经构造好的强类型 StartupData，但必须保证 Stage 创建发生在 `SetActiveGameStage` 之前：

```csharp
public void EnterBattleStageGroup(BattleStageStartupData startupData)
{
    if (startupData == null)
        throw new ArgumentNullException(nameof(startupData));

    var battleStage = PakGameStages.CreateBattleStage(this, startupData);
    StageWrapper.SetActiveGameStage(m_BaseStage, battleStage);
}
```

## 4. StartupData 的定义

- 命名为 `XxxStageStartupData`，按 Stage 划分，一个 Stage 使用一个顶层启动对象。
- 使用明确字段和业务类型；禁止用 `Dictionary<string, object>`、JSON 字符串或 Editor 序列化对象绕过类型约束。
- 只保存 ID、枚举、小型数值和必要的组合关系。例如战斗 Stage 可保存 `PlayerShipId`、`EnemyIds`，不复制 `ShipCsvData`、`EnemyCsvData` 的完整字段。
- 对数组、列表和其它可变引用做防御性复制。Stage 保存的是独立快照，不受入口窗口后续编辑影响。
- 不保存 `EditorWindow`、`SerializedObject`、Editor-only 类型或仅为绘制 Inspector 存在的引用。
- 如果必须引用 Unity 资产，应传递稳定标识或运行时允许的只读引用，并继续由正式资源层管理其生命周期。

StartupData 至少提供两类能力：

1. `ValidateStructure()`：不访问尚未加载的 DataGroup，只检查空值、集合长度、重复项、数值范围和字段间关系。
2. `CreateSnapshot()`：返回与调用方可变数据解耦的运行时快照。

方法名可按项目风格调整，但职责不得合并进 Editor 窗口或 System。

## 5. Stage 工厂

需要启动数据的 Stage 工厂必须显式接收它：

```csharp
private const string BattleStageStartupDataKey = "BattleStage.StartupData";

public static GameStage CreateBattleStage(
    PakGameEngine engine,
    BattleStageStartupData startupData)
{
    if (startupData == null)
        throw new ArgumentNullException(nameof(startupData));

    var snapshot = startupData.CreateSnapshot();
    snapshot.ValidateStructure();

    var stage = engine.StageWrapper.CreateStage("BattleStage");
    stage.SetStageData(BattleStageStartupDataKey, snapshot);

    stage.AddDataGroup(ShipCsvData.DataGroupName);
    stage.AddDataGroup(EnemyCsvData.DataGroupName);
    stage.AddUpdateSystem<BattleStatusSystem>();
    stage.AddLateLoadItem<InitializeBattleStageRuntime>();
    return stage;
}
```

必须遵守：

- 不提供会在内部偷偷补默认值的无参重载。
- 正式游戏入口和 Editor 测试入口调用同一个 Stage 工厂，只是构造 StartupData 的来源不同。
- 正式默认值由正式流程显式构造；测试值由入口配置资产构造。Stage 不识别调用方身份。
- 当前 `StageData` 为字符串键 API，因此键集中定义为常量，读写只发生在工厂和对应初始化项。若底层以后提供类型键 API，再统一迁移，不在业务层自造第二套容器。

## 6. 两阶段校验

### 工厂阶段：结构校验

在 Stage 被创建或激活前失败，检查无需 DataGroup 的规则，例如：

- 必填 ID 是否为空或非法；
- 敌方编成是否为空、超限或包含不允许的重复项；
- 互斥选项是否同时出现；
- 集合是否能被安全复制。

### 数据就绪阶段：引用校验

DataGroup 加载后，初始化项通过 `DataApi` 检查所有 ID 是否存在、配置之间是否兼容、必需资源是否可解析。失败信息必须指出 Stage、字段和具体值，不能静默选择第一条配置作为兜底。

Editor 窗口可以提前复用部分校验来改善编辑体验，但运行时工厂和初始化项仍必须自行校验，不能信任资产一定由窗口创建。

## 7. 初始化项与生命周期

为每个有启动输入的 Stage 设置一个职责明确的 `IStageLoad`，例如 `InitializeBattleStageRuntime`：

1. 从 `StageData` 取得并强制转换对应 StartupData；缺失或类型错误立即报告。
2. 用 StartupData 中的 ID 从 `DataApi` 取得正式配置并完成引用校验。
3. 创建 ECS 单例 Component、初始 Entity、必要对象池或绑定。
4. 将所有持续变化的状态交给 ECS；之后不再把变化回写 StartupData。
5. 在 `Unload` 中对称移除本项创建的 Component、Entity、池和绑定，使同一 Stage 实例可以再次加载。

理想执行位置是 **DataGroup 已加载，System 与 StageListener 尚未启用**。当前底层没有这一公开阶段；临时兼容时只能把该项注册为第一个 `LateLoadItem`，并确保 StageListener 不依赖初始化结果、System 能在初始化标记就绪前安全休眠。只要无法满足这些限制，就必须先补齐底层数据就绪阶段。待底层能力补齐后，应将初始化项迁移过去；不得假设一个尚未实现的 API 已存在。

## 8. 消费规则

- System 每帧读取 ECS Component；静态配置按 ID 从 `DataApi` 获取。
- UI 读取或监听 ECS Component，不读取 Editor 入口资产。
- `IStageLoad` 可以在加载时读取 StartupData，普通 System 不应把 `StageData` 当 Service Locator。
- 后续运行中发生重生、换装、波次推进等变化，只更新 ECS 状态；除非切换到新的 Stage 实例，否则不重建 StartupData。

## 9. 编写完成检查

- Stage 缺少输入时能否在创建或加载阶段明确失败？
- 工厂是否显式接收一个强类型 StartupData，并保存防御性快照？
- StartupData 是否只保存选择和 ID，没有复制正式配置或运行状态？
- 正式入口与 Editor 入口是否调用同一个 Stage 工厂？
- DataGroup 加载后是否完成 ID 与配置引用校验？
- 可变状态是否只有 ECS Component 一份？
- 初始化项的 `Unload` 是否与 `Load` 对称，支持再次加载？
- 当前使用 `LateLoadItem` 时，它是否排在第一位，StageListener 是否无依赖，System 是否能在就绪前休眠？
- 是否由命名明确的 Group 入口在激活前准备数据、创建 Stage，并用一次 `SetActiveGameStage` 声明完整集合？
- 需要不同输入时是否创建了新的 Stage 实例，而不是修改已保存的 StartupData 快照？
