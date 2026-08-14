# 自动生成文件边界

## Unity、IDE 与 Package Manager 自动生成

初始化流程不得主动创建或维护：

- `*.meta`：Unity 导入 Assets 后生成和维护；
- `Library/`、`Temp/`、`Logs/`、`obj/`：Unity/构建过程生成；
- `*.csproj`、`*.sln`：Unity 与 IDE 集成生成；
- `Packages/packages-lock.json`：Unity Package Manager 根据 manifest 解析生成；
- 大部分 `ProjectSettings/` 内容：由 Unity Hub/Editor 创建项目和用户设置时维护。

`Packages/manifest.json` 由 Unity 项目创建流程提供。本 skill 只在它已经存在时向 `dependencies` 添加明确缺失的包，不负责从零创建 Unity manifest。

## BbxCommon 工具生成或维护

不要把下列文件放入基础模板，也不要手写其内容：

- `Assets/Resources/ResourcesDictionary.json`：运行 `Tools/Build Resources Dictionary` 生成；
- `Assets/Resources/BbxCommon/ScriptableObjectAssets.asset`：保存/移动 `BbxScriptableObject` 时由编辑器登记流程创建或更新；
- `Assets/Resources/BbxCommon/Ui/PreLoadUiData.asset`：通过 View 的预加载导出流程创建或更新；
- `Assets/Resources/BbxCommon/LoadingTimeData.asset`：GameEngine 编辑器运行流程在需要时创建；
- `UiSceneAsset`：由 `UiSceneExporter.ExportUiScene()` 根据场景中的 UI 布局导出。

初始化交付时告诉用户运行对应工具，并在工具运行后验证文件存在和内容已更新。

## 需要通过 Unity Editor 创建

下列内容不是自动出现的普通文本文件，也不应由代理手写 Unity YAML：

- `Bootstrap.unity` 和其它 Scene；
- Canvas 原型 Prefab；
- UI Prefab 及 View 字段绑定；
- 项目自己的 ScriptableObject `.asset`；
- 材质、动画、Timeline 等 Unity 资产；
- Build Settings 中的 Scene 列表与顺序。

可操作与项目版本一致的 Unity Editor 时，代理可以通过 `UnityEditor` API、项目已有构建器、菜单或 Inspector 自动化创建并保存这些资产；Unity 负责序列化 YAML 和生成 `.meta`。场景搭建需遵循 [主场景搭建](main-scene-setup.md) 的安全检查。

无法操作 Unity Editor 时，代理只创建相应 C# 类型并提供具体 Editor 操作清单。在用户完成前，状态必须写为“代码已准备，Editor 资产待创建”。

## 初始化流程可以创建

以下是普通文本配置或源码，可以由本 skill 创建：

- 业务 `.cs`；
- 业务 `.asmdef`；
- `Packages/manifest.json` 中缺失的依赖项；
- 普通 `.csv`、`.json` 配置源文件，但只有真实字段已确定时才创建；
- AutoDoc/Temp 任务记录。

创建这些文件后等待 Unity 自动生成 `.meta` 和项目文件，不要补写配套生成文件。
