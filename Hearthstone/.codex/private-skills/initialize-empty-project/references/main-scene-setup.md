# 主场景搭建

## 目录

1. 目标与边界
2. 场景职责判定
3. 默认启动层级
4. Unity Editor API 搭建
5. Build Settings
6. 安全与增量规则
7. 验证清单

## 1. 目标与边界

主场景的目标是把唯一 GameEngine、相机、UI Canvas 原型和 EventSystem 接入一个可直接 Play 的启动入口。场景只承担启动和静态场景内容；运行时玩法对象、UI 页面、实体和监听仍由 GameStage、IStageLoad、UiScene 与 ECS 管理。

Scene、Prefab 和 `.meta` 必须由目标版本 Unity 创建或保存，不得手写 YAML。能够操作 Unity Editor 时，优先编写一次性可重复执行的 Editor 构建器完成资产创建和验证；不能操作 Editor 时，才把精确步骤交付给用户。

## 2. 场景职责判定

先检查 GameEngine 和初始 Stage，再决定场景数量，不要按参考项目文件名机械复制：

### 单启动场景

满足以下条件时，默认只创建 `Assets/Scenes/Main.unity`：

- 初始 Stage 没有 `AddScene(...)`；
- 背景、角色、玩法表现或 UI 由 IStageLoad、UiScene 或其它运行时入口创建；
- 没有主菜单与战斗等必须分开的静态场景。

`Main.unity` 同时是启动主场景和唯一 Build Settings 场景。

### 启动场景 + Additive 内容场景

初始 Stage 明确调用 `AddScene("Main")` 或加载其它静态内容场景时：

- 创建 `Assets/Scenes/Launcher.unity` 作为启动场景；
- Launcher 持有 GameEngine、相机、Canvas 原型和 EventSystem；
- 创建 `Assets/Scenes/Main.unity` 作为 Stage 管理的内容场景；
- Launcher 排在 Build Settings 第一位，Main 紧随其后；
- 不在 Launcher 和 Main 中重复放置 GameEngine、EventSystem 或主相机，除非内容场景有明确独立相机职责。

这与 ChaosCombat 的结构一致：Launcher 负责启动基础设施，空 Main 由 CombatStage Additive 加载并在运行时填充。

### 已有项目约定

项目已经使用 `Bootstrap.unity`、`Persistent.unity` 或其它命名时沿用现有约定。不得为了套用 `Main/Launcher` 命名移动已有 Scene；修改已有 Build Settings 顺序属于迁移，必须符合增量初始化的授权边界。

## 3. 默认启动层级

推荐把启动基础设施封装成一个 `Bootstrap.prefab`，主场景只保留一个 Prefab 实例：

```text
Main 或 Launcher
└── Bootstrap [ProjectGameEngine]
    ├── Main Camera
    └── EventSystem
```

要求：

- 场景中只有一个项目 GameEngine，且组件挂在 Bootstrap 根节点，确保 `DontDestroyOnLoad` 能作用于根 GameObject；
- Main Camera 带 `MainCamera` Tag，投影方式、背景色和裁剪范围符合项目类型；
- GameEngine 的 `UiCanvasProto` 引用可实例化的 Canvas Prefab；
- EventSystem 与项目输入方案匹配；
- Canvas Prefab 至少包含 Canvas、CanvasScaler 和 GraphicRaycaster；
- 不把 BattleStage 运行时创建的背景、战舰、敌人、HUD 页面或对象池对象预放进启动场景；
- 如果项目没有 UI，记录决定并省略 Canvas 与 EventSystem。

Canvas 原型可以作为独立 Prefab 资产引用，不要求在启动场景中同时存在一个可见 Canvas 实例。GameEngine 会按 BbxCommon 生命周期创建 UI 根时，不要额外预放第二个运行时 Canvas。

## 4. Unity Editor API 搭建

Editor 构建器应放在 Editor 程序集，或用 `#if UNITY_EDITOR` 隔离。典型流程：

1. 用 `AssetDatabase.IsValidFolder` / `AssetDatabase.CreateFolder` 确保 `Assets/Scenes` 存在；
2. 用 `AssetDatabase.LoadAssetAtPath<GameObject>` 读取 Bootstrap Prefab；
3. 检查当前活动场景：
   - 已保存且有未保存修改时停止；
   - 未保存且为空时可以直接作为新主场景；
   - 只含 Unity 新建场景默认 Main Camera 和 Directional Light 时，必须逐个核对名称与组件后才可替换；
   - 包含其它对象时停止并要求先保存，不得清空；
4. 用 `PrefabUtility.InstantiatePrefab(prefab, scene)` 创建 Prefab 实例；
5. 用 `EditorSceneManager.SaveScene(scene, scenePath)` 保存；
6. 用 `EditorBuildSettings.scenes` 配置场景顺序；
7. 用 `AssetDatabase.SaveAssets()` 保存引用；
8. 立即执行结构和引用校验。

构建器应可重复执行：

- 目标 Scene 已存在时打开并校验，不覆盖其中未知对象；
- Build Settings 中已存在目标路径时去重；
- 创建新入口时保留其它已登记场景，除非用户明确要求替换；
- 失败时输出具体路径、对象名或缺失字段，不留下“看似成功”的完成标记。

## 5. Build Settings

- 启动场景必须是第一项且启用；
- Stage 通过名字加载的内容场景必须存在并登记；
- 单场景项目默认只有 Main；
- Launcher + Main 项目默认顺序为 Launcher、Main；
- 保留无关已有场景的 enabled 状态和相对顺序；
- 不直接手写 `ProjectSettings/EditorBuildSettings.asset`，通过 Unity Editor API 修改。

## 6. 安全与增量规则

- 不复制参考项目的 Scene、Prefab、GUID 或 `.meta`；
- 只参考职责分层、层级和 Build Settings 顺序；
- 不销毁未保存场景中的未知对象；
- 不创建第二个 GameEngine、Main Camera、EventSystem 或运行时 Canvas；
- 已有 Bootstrap Prefab 时复用并补校验，不在 Scene 中重新拼一套同义对象；
- 需要改变已有启动场景或顺序时，先确认这是用户要求或已获明确授权；
- 场景生成器属于 Editor 工具，不得进入 Player Runtime。

## 7. 验证清单

- [ ] Scene 由目标 Unity 保存并能重新打开。
- [ ] 启动场景只有一个 Bootstrap 根或一套明确的启动根对象。
- [ ] Bootstrap 是预期 Prefab 的实例，没有 Missing Script。
- [ ] 场景中只有一个项目 GameEngine。
- [ ] `UiCanvasProto` 已赋值，且不会产生双 Canvas。
- [ ] Main Camera 与 EventSystem 存在且唯一。
- [ ] Build Settings 第一项是启用的启动场景。
- [ ] 所有 `AddScene` 名称都能在 Build Settings 找到对应 Scene。
- [ ] Console 无编译错误、资源 key 错误或重复系统错误。
- [ ] 从启动场景进入 PlayMode 后，初始 Stage、运行时表现和 UI 正常加载。
- [ ] 退出 PlayMode 后场景无非预期脏修改。
