# 基础占位模板

## 目录

1. 使用条件
2. 创建命令
3. 模板内容
4. 运行方式
5. Unity Editor 接入
6. 替换规则

## 1. 使用条件

完全空白或只有 BbxCommon 框架的项目默认使用整套模板。用户没有说明 ECS、MVC、配置等模块时，不再等待模块设计，先创建占位文件。

只有占位结构的近空项目必须先阅读现有 GameEngine、Stage 和 asmdef。整套模板遇到任一不同内容的目标文件会停止且不写入；此时保留已有文件，只从模板中选择缺失部分并适配现有命名。

用户明确说明项目不需要某模块时，可以在实例化后、Unity 首次导入前删除对应模板文件，或不运行整套脚本而按需复制其余模板。不得默默跳过用户未提及的模块。

## 2. 创建命令

先预览：

```powershell
python .codex/private-skills/initialize-empty-project/scripts/instantiate-basic-placeholder.py `
  --project-root . `
  --project-name Aurora `
  --namespace Studio.Aurora `
  --dry-run
```

确认目标后执行：

```powershell
python .codex/private-skills/initialize-empty-project/scripts/instantiate-basic-placeholder.py `
  --project-root . `
  --project-name Aurora `
  --namespace Studio.Aurora
```

参数规则：

- `project-name`：单个合法 C# 标识符，用于类型名和默认业务文件夹；
- `namespace`：合法的点分隔 C# namespace；
- `project-folder`：可选，只能是单个目录名；不传时使用 project-name；
- `dry-run`：只报告，不创建文件。

脚本先检查所有目标。发现不同内容的已有文件会返回 `conflict` 和退出码 2，且不写入任何文件。重复执行时，相同文件报告为 `unchanged`。

## 3. 模板内容

模板位于 `assets/basic-placeholder/`，包含：

| 文件 | 用途 |
|---|---|
| `__PROJECT_NAMESPACE__.asmdef` | 业务程序集及 BbxCommon/CrossLibrary/Entities/Collections/TMP 直接引用 |
| `Bootstrap/__PROJECT_NAME__GameEngine.cs` | 唯一入口，登记 System 顺序并加载 BaseStage |
| `GameStage/PlaceholderStages.cs` | 创建 Stage、ECS 状态并按条件接入 UI |
| `Ecs/RawComponent/Singleton/PlaceholderStateSingletonRawComponent.cs` | 可监听的占位运行时状态 |
| `Ecs/System/PlaceholderStateSystem.cs` | 把占位状态改为已初始化 |
| `Config/ScriptableObject/PlaceholderSettingsData.cs` | BbxScriptableObject 占位配置类型 |
| `Ui/Scene/PlaceholderUiScene.cs` | 一个 Main UI 组 |
| `Ui/View/PlaceholderView.cs` | 带 TMP_Text 引用的占位 View |
| `Ui/Controller/PlaceholderController.cs` | 监听 ECS 占位状态并刷新 View |

模板不包含真实玩法、CSV、Aspect、Hud、Task、Scene、Prefab、`.asset`、`.meta` 或测试程序集。

## 4. 运行方式

无需 UI 资产时，代码侧基础流程也能运行：

```text
GameEngine
  → RegisterSystemOrder 登记 Input、占位 System、Task
  → EnterInitialStageGroup
  → SetActiveGameStage(BaseStage)
  → InitializePlaceholderState.Load
  → 创建 PlaceholderStateSingletonRawComponent
  → PlaceholderStateSystem 运行
  → Initialized = true
```

卸载 BaseStage 时，`InitializePlaceholderState.Unload` 移除单例；Component 回收时使监听失效并恢复默认值。

占位 System 只执行一次状态切换，不承载真实玩法。它存在的目的是让开发者能确认 Stage 注册、ECS 数据和 System 更新已经接通。

`EnterInitialStageGroup()` 是初始化模板必须提供的第一个 Group 入口。后续真实主菜单、战斗或关卡组合应新增各自具名入口，在入口边界准备必要 StartupData、创建对应 Stage，并用一次 `SetActiveGameStage(...)` 声明完整集合；不要恢复为分散的 `LoadStage` 调用。

## 5. Unity Editor 接入

代码创建后按 [主场景搭建](main-scene-setup.md) 判断单 Main 或 Launcher + 内容场景结构。可操作目标 Unity 时由 Editor API 完成；否则交付给用户：

1. 创建并保存启动 Scene；
2. 创建唯一 `GameEngine` GameObject，挂载 `<ProjectName>GameEngine`；
3. 创建 Canvas 原型 Prefab，并绑定到 `UiCanvasProto`；
4. 创建占位 UI Prefab，根节点挂 `PlaceholderView`，绑定 `StatusText`；
5. 使用 UiSceneExporter 把页面放入 `PlaceholderUiGroup.Main`；
6. 将 UiSceneAsset 导出为 `Assets/Resources/Ui/Placeholder.asset`；
7. 运行 BbxCommon Resources Dictionary 构建工具；
8. 将启动 Scene 放入 Build Settings 首位，并登记 Stage 通过 `AddScene` 加载的内容 Scene；
9. 从启动 Scene 进入 PlayMode，验证文字从 `Initializing` 更新为 `Initialized`。

不需要测试配置加载时，可以不创建 `PlaceholderSettingsData.asset`。需要验证时，在 Unity 菜单创建该资产并保存，由 BbxCommon 自动登记到 ScriptableObjectAssets。

## 6. 替换规则

首个真实模块建立后按顺序替换：

1. 用真实 Component/System 替换占位 ECS 状态和规则；
2. 把新 System 注册到正确 Stage、加入 GameEngine 的 `RegisterSystemOrder` 类型列表，并补齐创建/释放；
3. 用真实页面替换 Placeholder View/Controller/UiSceneAsset；
4. 用真实配置替换 PlaceholderSettingsData；
5. 确认没有消费者后删除 `Placeholder*` 类型和 Stage 注册；
6. 如果业务 asmdef 不再直接使用 TMP，可删除 `Unity.TextMeshPro` 程序集引用，但 BbxCommon 包依赖仍保留；
7. 再次验证 Stage 卸载和重新进入。

不要让占位状态与真实启动状态长期并存，也不要把 `Placeholder` 重命名后当成业务实现而不补真实职责。
