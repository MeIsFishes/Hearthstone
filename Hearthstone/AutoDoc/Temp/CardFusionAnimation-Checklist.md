# 卡牌融合动画优化检查清单

## 1. 用户需求

- [通过] 融合开始时屏幕变暗。证据：`FusionRevealOverlay` 保留 `78%` 深灰全屏遮罩，Controller 淡入后保持 Alpha 为 1。
- [通过] 将所选卡牌铺在屏幕上，再由脚本控制一同移动并缩向屏幕中心 `(0, 0)`。证据：融合事务快照生成 2～4 个素材卡条目，`ApplyFusionRevealMaterialTransform()` 将 `anchoredPosition` 从铺开坐标插值到 `Vector2.zero`，缩放从 `0.78` 插值到 `0.05`。
- [通过] 复用现有空中转圈动画，并允许用现有帧多拼一圈以适配延长后的缩放时序。证据：封印面/蓝金卡背继续按局部 Y 角切换，`FusionRevealRotationTurns = 2f` 让现有正背面表现完整旋转两圈。
- [通过] 空中动画按“先放大、再缩小、最后放很大”的节奏播放，最终卡面至少达到屏幕尺寸的三分之二。证据：三段缩放为 `0.12 → 1.28 → 0.82 → 动态结果尺寸`；结果尺寸取 `2` 倍与遮罩高度三分之二换算值的较大者。
- [通过] 以画面闪白/闪光揭晓融合结果。证据：Prefab 的 `ScreenFlash` 为全屏白色、无射线 Image，Alpha 正弦升降，并在峰值切换结果卡。
- [通过] 揭晓后结果卡不自动消失。证据：动画结束调用 `CompleteFusionReveal()` 进入 `m_FusionRevealAwaitingDismiss`，不再按时间调用复位或淡出。
- [通过] 仅当玩家点击结果卡面外任意位置时关闭结果；点击卡面内不关闭。证据：全屏遮罩 Button 只在等待关闭状态启用；结果卡自身的全卡面 `UiEventListener` 是更内层 Pointer 处理目标，空白点击才落到遮罩 Button。
- [通过] 结果卡保持普通卡面交互能力，鼠标悬停可查看词条说明。证据：完成演出时调用共享卡 Controller 的 `SetFusionRevealInteraction(true)`，沿用既有关键词 Tooltip；演出期间保持关闭。
- [通过] 不显示“按任意键继续”等额外提示文本。证据：Builder 未创建提示节点；Prefab 测试遍历揭晓层 TMP 并断言不含该文字。

## 2. 项目修改与代码质量

- [通过] 定位现有融合页面的 View Prefab、View、Controller、所属 UI 配置及动画资源，沿用既有页面与生命周期，不另建平行 UI。证据：仅扩展 `PreparationView`、`PreparationController`、共享 `BattleCardItemController`、一一对应 Builder 与原 Prefab；UiScene、UiGroup 和 Stage 配置未变。
- [通过] 静态 UI 结构与序列化引用保留在 Prefab/View，交互、动画、计时与运行时更新保留在 Controller 或既有业务组件中。证据：遮罩 Button、素材列表、CardRoot 与全屏闪光由 Builder 固化；View 只保存引用；Controller 驱动时序。
- [不适用] 若修改项目自定义 UI 组件，确认其继承 `BbxUiItem`，不写入具体玩法规则，并同步 `AutoDoc/UIItem/` 组件文档与索引。证据：本次未新增或修改 `BbxUiItem`/通用 UI 组件；只修改页面 View/Controller、业务卡 Controller、Builder 和 Prefab。
- [通过] 检查新增字段与函数是否确有复用价值，删除不必要的一次性抽象。证据：新增函数分别承担素材快照绑定、素材布局插值、三段缩放、动态结果尺寸、结束态和关闭交互；没有新增仅转调一次且无语义的类型。
- [通过] 检查无关文件未被误改，直接依赖未遗漏，现有用户改动未被覆盖。证据：工作区开始时已有大量未提交修改；本次只在既有融合相关文件上增量编辑，并由 Builder 重建原页面 Prefab；未修改任何 `.meta`。
- [通过] 运行适合当前环境的静态验证或编译验证；除非用户另行要求，不主动进入游戏验证。证据：Unity 脚本刷新与编译完成；定向 2 项 EditMode 测试通过；完整 82 项测试完成但有 1 项既有攻击音频列表测试失败。按项目默认未进入 Play Mode。

## 3. 框架边界审计

- [通过] 未绕过 `UiApi` / View-Controller 生命周期、公开 API、Prefab/编辑器配置源或资源流程。证据：继续使用 `UiList`、现有预加载映射和页面生命周期；Prefab 通过 `PreparationViewUiBuilder.Build()` 在 Unity Editor 中实际重建。
- [通过] 未在业务层重复实现框架已有能力，未直接操作 UI 管理器、Controller 池或手写导出资产。证据：素材卡和结果卡均由 `UiList.ItemWrapper` 创建/清理；没有访问内部管理器，没有手写 Prefab YAML 或导出 Asset。
- [不适用] 若发现框架能力缺口，按最小范围处理并记录影响与回归证据。证据：现有 View-Controller、UiList、Button、CanvasGroup 与共享卡面能力足够，无框架改动。

## 4. 文档同步门槛

- [通过] 完整读取玩家视角设计文档格式 skill，核对相关现有文档与实际玩家可见变化。证据：读取 `design-doc-format`，更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md` 的融合揭晓体验与关闭交互。
- [通过] 完整读取美术文档格式 skill，核对相关现有文档与实际美术表现变化。证据：读取 `art-doc-writer` 与模块格式，更新 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md` 的素材铺开、三段缩放、全屏闪白和资产风格引用；无新增位图。
- [通过] 完整读取程序文档格式 skill，核对相关现有文档与实际程序行为变化。证据：读取 `program-doc-format` 与 UI 界面格式，更新 `AutoDoc/Program/UI/preparation/preparation.md` 的数据快照、Prefab 层级、时序、对象池与点击生命周期。

## 5. 结束审计与报告

- [通过] 逐项以实际文件、命令结果或其他证据复核并标记通过、未通过或不适用。证据：以上各项已对照代码、Prefab、文档、Unity 编译与测试结果复核。
- [通过] 结束时只运行一次 `AutoDoc/CleanupTempDocs.bat`，记录退出结果。证据：执行前后 Markdown 数均为 `215`，退出码 `0`，因未超过阈值而未删除文件。
- [通过] 清理后创建 `AutoDoc/Temp/CardFusionAnimation-Report.md`，记录结果、证据、验证、偏差、风险、文档处理和清理结果。证据：报告已创建并包含全部规定章节。
