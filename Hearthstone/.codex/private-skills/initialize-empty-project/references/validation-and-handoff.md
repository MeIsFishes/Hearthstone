# 验证与交付

## 目录

1. 验证分层
2. 静态检查
3. Unity 编译
4. PlayMode 完整流程
5. 卸载与重入
6. 编辑器待办
7. 最终报告

## 1. 验证分层

按以下顺序验证，前一层失败时先修复，不继续堆叠代码：

```text
文件与引用
  → C# / asmdef 编译
  → Bootstrap Scene 启动
  → Stage 加载
  → ECS 数据和 System
  → 占位 UI/MVC 代码编译
  → Stage 卸载与重入
```

## 2. 静态检查

检查：

- 目录、文件名、namespace 和 asmdef rootNamespace 一致；
- 业务 asmdef 已根据源码直接使用的 namespace、继承链、泛型约束和公开成员签名补齐直接程序集引用；不得以依赖包已安装或 BbxCommon 已引用为由省略；
- 没有未标明用途的 `Example*`/`Test*` 类型；允许本 skill 固定的 `Placeholder*`，但必须记录替换方向；
- 只有一个 GameEngine 子类被用作项目入口；
- 至少存在一个由 GameEngine 实际调用的具名初始 Group 入口，且内部使用一次 `SetActiveGameStage`；
- Runtime 没有误用 `UnityEditor`；
- System 有 `[DisableAutoCreation]` 并被恰当 Stage 注册；
- Component Collect 钩子清理池化状态；
- IStageLoad 的 Load/Unload 所有权成对；
- DataGroup、Resources key、配置组一致；
- View/Controller 配对，Model 指向 ECS/Data；
- 已通读 `.codex/agents/` 与 `.codex/project-files/agents/` 下的全部 subagent TOML；底层 subagent 没有越过 extension 直接关联项目 skill，所有项目 skill 与 extension 路径均存在；
- 新 skill/文档中的所有相对链接存在。

可再次运行 `inspect-unity-project.ps1`，比较初始化前后的信号。脚本建议状态只作提示，必须人工确认完整运行流程。

## 3. Unity 编译

优先使用项目现有的 Unity 批处理验证脚本或 CI。没有现成入口时，可让用户在目标 Unity 版本打开项目并等待首次导入，然后确认 Console 无编译错误。

不要假设本机任意 `Unity.exe` 就是目标版本。若能定位与 `ProjectVersion.txt` 一致的 Editor，可使用项目约定的 batchmode 命令；否则报告“未运行 Unity 编译”，不要用普通 C# 编译器替代 Unity 编译结论。

编译检查重点：

- asmdef 引用名/GUID；
- 默认占位业务 asmdef 是否能直接解析 `BbxCommon`、`CrossLibrary`、`Unity.Entities`、`Unity.Collections` 和 `Unity.TextMeshPro`；
- Entities 版本 API；
- BbxCommon 类型可见性；
- UI 程序集引用；
- Editor/Runtime 边界；
- 文件名和 public MonoBehaviour 类型名一致。

## 4. PlayMode 完整流程

让用户或自动测试从 Bootstrap Scene 验证：

1. GameEngine 实例唯一且未被重复销毁/创建；
2. GameEngine 调用初始 Group 入口，BbxCommon 内部 Stage 和 Group 声明的项目 Stage 集合加载；
3. BaseStage 创建占位 Singleton 并注册占位 System；有真实模式时再验证模式 Stage；
4. `IStageLoad` 创建预期 Entity/Singleton；
5. System 改变占位状态或真实状态；
6. 可通过日志、断言、UI 或调试窗口观察结果；
7. Console 无重复 System、空引用和资源 key 错误。

只用日志作为临时检查手段，验证完成后删除噪声日志或改为项目正式诊断方式。

## 5. 卸载与重入

基础架构最容易在第二次进入时失败。至少验证一次：

1. 从 InitialModeStage 切回只保留 BaseStage；
2. 确认 Mode Entity、Singleton、UiScene、监听和 Scene 已释放；
3. 再次进入 InitialModeStage；
4. 确认无重复注册、旧池化数据、悬挂监听和重复按钮回调；
5. 多 System 相对顺序仍符合 GameEngine 的 `RegisterSystemOrder` 类型列表，未登记 System 仍位于末尾。

## 6. 编辑器待办

以下必须由 Unity Editor 创建或设置；代理能操作目标 Unity 时应直接完成，否则列为用户待办：

- 创建/保存 Scene；
- 把 GameEngine 挂到 GameObject；
- 配置 `UiCanvasProto`；
- 创建 UI Prefab 并绑定 View 字段；
- UiScene 导出与 UiSceneAsset 路径；
- 创建 ScriptableObject 资产并设置 LoadingType/GroupName；
- 在 Stage 入口框架可用时，为初始 Group 创建 `GameStageEntryAsset` 配置到 `Assets/Resources/Editor/`，并确认它调用同一个运行时 Group 入口；
- 修改 Build Settings；
- PlayMode 手工观察。

主场景结构、Build Settings 顺序与自动化安全检查按 [主场景搭建](main-scene-setup.md) 执行。

以下由 BbxCommon 工具生成或更新，只要求用户运行对应工具：

- ResourcesDictionary；
- ScriptableObjectAssets；
- PreLoadUiData；
- LoadingTimeData；
- UiSceneAsset 导出内容。

交付清单要写出具体资产路径、对象名、组件和字段，不能只写“在 Unity 中配置一下”。

## 7. 最终报告

使用四类状态：

### 已完成

列出创建/修改的目录、占位脚本、asmdef、manifest 和 Stage 注册；同时列出已读取的全部 subagent TOML，以及被解除的无效项目级 skill 关联和原因。没有移除项时明确记录“未发现无效关联”。

### 已验证

列出实际运行过的命令、编译、测试或只读检查，以及结果。

### 用户需在 Unity 完成

按执行顺序列出 Scene、Prefab、Inspector、导出、Build Settings 和 PlayMode 步骤。

### 未完成或风险

列出缺少的框架来源、包版本冲突、未运行的 Unity 版本、待替换的 Placeholder 类型、资产依赖和任何暂存假设。

只有“已完成 + 已验证”覆盖完成定义，且编辑器待办已由用户完成或明确接受时，才称项目初始化完成。
