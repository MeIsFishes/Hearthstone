# 目录与基础文件

## 目录

1. 布局原则
2. 默认目录树
3. 默认占位文件
4. 条件目录与文件
5. 命名与程序集边界
6. 不应初始化的内容

## 1. 布局原则

优先沿用当前仓库已经形成的路径。BbxCommon 项目的常见业务根目录是 `Assets/Scripts/<ProjectName>/`，资源位于 `Assets/Scenes/` 与 `Assets/Resources/`。如果项目已经使用 `Assets/<ProjectName>/Runtime` 等布局，不要为了套模板搬迁。

用户未说明模块时，默认创建本 skill 的固定占位文件。占位文件不是空类：GameEngine 能加载 Stage，Stage 能创建/释放 ECS 状态，System 能修改该状态，Controller 能监听同一状态。它们统一使用 `Placeholder` 命名，方便后续查找和替换。

不要预建默认模板之外的空目录。Unity 会根据实际文件自行生成目录 `.meta`。

## 2. 默认目录树

```text
Assets/
└── Scripts/
    └── <ProjectName>/
        ├── <ProjectNamespace>.asmdef
        ├── Bootstrap/
        │   └── <ProjectName>GameEngine.cs
        ├── GameStage/
        │   └── PlaceholderStages.cs
        ├── Ecs/
        │   ├── RawComponent/
        │   │   └── Singleton/
        │   │       └── PlaceholderStateSingletonRawComponent.cs
        │   └── System/
        │       └── PlaceholderStateSystem.cs
        ├── Config/
        │   └── ScriptableObject/
        │       └── PlaceholderSettingsData.cs
        └── Ui/
            ├── Scene/
            │   └── PlaceholderUiScene.cs
            ├── View/
            │   └── PlaceholderView.cs
            └── Controller/
                └── PlaceholderController.cs
```

这棵目录只描述初始化脚本主动创建的文本文件。下列内容不在模板中：

- `.meta`；
- 启动 Scene（例如 `Main.unity`、`Launcher.unity` 或已有约定的 `Bootstrap.unity`）；
- Canvas/UI Prefab；
- `PlaceholderSettingsData.asset`；
- `Placeholder.asset`（UiSceneAsset）；
- `ResourcesDictionary.json` 与 BbxCommon 内部索引资产；
- `packages-lock.json`。

它们分别由 Unity、Package Manager、BbxCommon 工具或用户在 Editor 中创建。

## 3. 默认占位文件

### 3.1 业务 asmdef

默认模板创建 `<ProjectNamespace>.asmdef`，引用：

- `BbxCommon`；
- `CrossLibrary`；
- `Unity.Entities`；
- `Unity.Collections`；
- `Unity.TextMeshPro`。

这些是当前占位模板根据源码、继承链和公开类型签名确认的直接程序集依赖，不能因为 BbxCommon 自身已经引用它们就省略。如果现有项目明确使用 Assembly-CSharp，或已经有业务 asmdef，不创建第二份；检查和补齐现有引用。后续增删模板模块时，再按实际类型使用同步增删引用。

### 3.2 GameEngine

`<ProjectName>GameEngine.cs`：

- 继承 `GameEngineBase<T>`；
- 至少提供并实际调用一个 `EnterInitialStageGroup()`；
- 由该 Group 入口创建 BaseStage，并通过一次 `SetActiveGameStage(...)` 声明初始完整集合；
- 不包含玩法逻辑；
- 由用户在 Bootstrap Scene 中挂载。

### 3.3 Stage

`PlaceholderStages.cs`：

- 创建 `BaseStage`；
- 注册 `InitializePlaceholderState` 和 `PlaceholderStateSystem`；
- `Load` 创建单例 Component，`Unload` 移除；
- 当 `UiCanvasProto` 和 `Resources/Ui/Placeholder.asset` 都存在时接入占位 UiScene；
- 不依赖 UI 资产也能运行 ECS 占位流程。

### 3.4 ECS

`PlaceholderStateSingletonRawComponent.cs`：

- 保存一个可监听的 `Initialized` 状态；
- 回收时使监听失效并恢复默认值。

`PlaceholderStateSystem.cs`：

- 带 `[DisableAutoCreation]`；
- 由 BaseStage 注册；
- 将 `Initialized` 从 `false` 改为 `true`，用于证明 System 已运行。

占位 ECS 文件必须保留到首个真实 Component/System 已接入同一 Stage。替换后删除占位文件和对应注册，不要长期保留两套启动状态。

### 3.5 配置

`PlaceholderSettingsData.cs` 只提供一个 `BbxScriptableObject` 类型和 `DataApi.SetData(this)` 示例。模板不创建 `.asset`。如果首个真实模块已经有配置，直接用真实配置替换该类。

### 3.6 MVC/UI

默认创建：

- `PlaceholderUiScene` 与 `PlaceholderUiGroup.Main`；
- `PlaceholderView`，包含一个待绑定的 `TMP_Text`；
- `PlaceholderController`，监听 `PlaceholderStateSingletonRawComponent.Initialized`。

模板不创建 Canvas、Prefab 或 UiSceneAsset。用户明确项目不需要运行时 UI 时，可跳过这三个代码文件，并从业务 asmdef 中移除不再需要的 `Unity.TextMeshPro` 引用；BbxCommon 本身仍直接依赖 TextMeshPro 包。

## 4. 条件目录与文件

只在条件成立时增加：

- `Ecs/RawAspect/`：System 总是共同使用多个 Component，或需要绑定 GameObject Component；
- `Ecs/EntityCreation/`：首个真实模块需要多实体生成、对象池或复杂初始化；
- `Config/Csv/`：已经有多行同结构配置；
- `Ui/Hud/`：UI 需要跟随 Entity；
- `Gameplay/`：首个真实玩法模块已定义；
- `Task/`：项目采用 BbxCommon Task 图；
- `Editor/`：确有编辑器扩展，并使用独立 Editor asmdef；
- `Tests/EditMode`、`Tests/PlayMode`：确认要接入 Unity Test Framework；
- 网络、存档、音频、成就、热更新等：用户明确提出对应模块。

不要为空目录创建 `.gitkeep`。有第一个真实文件时再创建目录。

## 5. 命名与程序集边界

- `project-name` 必须能用于 C# 类型名，例如 `Aurora`；不能直接使用 `aurora-game`。
- 根 namespace 使用点分隔的合法 C# 标识符，例如 `Studio.Aurora`。
- 文件夹名表达职责，类名表达业务意义；纯占位类统一以 `Placeholder` 开头。
- 框架代码保留 `BbxCommon` namespace，业务代码不得写入框架目录。
- Runtime 程序集不得引用 Editor 程序集或 `UnityEditor` API。
- 一个业务类型只存在于一个程序集；创建 asmdef 后检查重复定义和不可见引用。
- 不复制其他项目 asmdef 的 GUID 引用；使用当前项目可解析的程序集名称或由 Unity Inspector 选择。

## 6. 不应初始化的内容

- 默认模板以外的空 Manager、Service、Repository 或抽象接口；
- 没有需求的模块目录；
- 第二套事件总线、对象池或资源管理器；
- 与 BbxCommon ECS 并行的自制 ECS；
- 与 GameStage 并行的全局 MonoBehaviour 更新树；
- Unity、IDE、Package Manager 或 BbxCommon 工具负责生成的文件；
- Scene、Prefab、ScriptableObject 与 UiSceneAsset 的手写 YAML。
