# 卡牌鼠标悬停边框高亮检查清单

## 用户要求与实现

- [通过] 仅备战阶段已持有的共享卡牌响应鼠标进入与移出；`SetHoverEnabled(preparationMode && occupied)` 控制监听和射线，战斗阶段默认关闭悬停与拖拽。
- [通过] 敌方卡牌默认红色 `#D23730`，我方卡牌及备战卡牌默认蓝色 `#3773EB`。
- [通过] 备战鼠标进入切换黄色 `#FFD230`，移出恢复绑定上下文的默认颜色。
- [通过] 基础框、攻击者框和目标框运行时只加载 `CardFrame-v3`，三色通过 `Image.color` 生成；`CardFrameBlue-v2` 仅为未引用历史资源。
- [通过] `ResetBinding()`、关闭备战悬停和空槽切换会清理 `m_IsHovered` 并重新应用默认颜色，对象池换绑不保留黄色。
- [通过] `CardHoverInput` 与 `CardHoverListener` 均由 View 序列化持有，Controller 在 Init 生命周期注册事件，不在 View 中写交互逻辑或运行时查找组件。
- [通过] `BattleCardItemUiBuilder.Build()` 经 Unity Editor 执行并保存 Prefab；没有手写 Prefab YAML。
- [通过] 颜色表、统一 Sprite 入口和悬停开关集中在共享 Controller/Builder，没有新增平行卡框实现。

## 验证与边界

- [通过] 四个相关脚本标准校验为 0 error；Unity Console 最终 0 error；3 项目标 Editor 测试通过，相关规则回归 33/33 通过。按项目默认边界未进入 Play Mode。
- [通过] `git diff --name-only` 未发现本任务修改 `.meta`；并行任务的资源、逻辑和临时文档均保留未回退。
- [通过] 框架边界审计确认使用 View/Controller、`UiEventListener`、`BattleCardItemUiBuilder`、既有唯一预加载和对象池换绑生命周期。

## 文档同步

- [通过] 已完整读取玩家视角设计文档格式，并同步备战悬停、战斗禁用和敌红我蓝表现。
- [通过] 已完整读取美术文档、模块与 UI 美术格式，并同步中性单素材三色着色及历史蓝框未引用状态。
- [通过] 已完整读取程序文档格式及 UI 界面专项格式，并同步序列化输入、事件开关、颜色与对象池清理逻辑。

## 收尾

- [通过] 已在收尾前重新打开本清单并逐项写入状态与证据。
- [通过] 已只运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 `0`。
- [通过] 已在清理后创建 `AutoDoc/Temp/BattleCardHoverHighlight-Report.md`，记录产物、验证、偏差和风险。
