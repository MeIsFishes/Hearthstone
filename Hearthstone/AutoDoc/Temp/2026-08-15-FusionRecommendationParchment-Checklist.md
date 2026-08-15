# 智能推荐弹窗羊皮纸视觉检查清单

- [x] 通过——智能推荐弹窗已移除可见白色内容底块，中央面板改为战斗场景同款木质金边做旧羊皮纸。
- [x] 通过——无合法组合时正文为“无可用组合”，TMP 使用 `Center` 对齐且 Content 至少等于 Viewport 高度，因此在内容区水平、垂直居中。
- [x] 通过——已核对 `PreparationView`、`PreparationController`、`PreparationView.prefab`、`PreparationViewUiBuilder`、`Assets/Scenes/Ui/Preparation.unity`、`Preparation.asset`、`EPreparationUiGroup.Main` 与 `PreparationStage` 链路；内部弹窗结构未改变场景级导出字段。
- [x] 通过——面板直接复用 `Assets/Resources/Art/BattleCards/UI/BattleBoardBackground.png`，做旧层复用 `ParchmentAgingOverlay.png`，未新增或修改图片资产。
- [x] 通过——有组合时 Controller 切回 `TopLeft` 对齐并保留原 Rich Text 暖金高亮；ScrollRect 透明 Image 继续接收射线，Viewport 保留裁剪，关闭按钮与原生命周期清理逻辑未变。
- [x] 通过——资源导出测试新增战斗/推荐底板同 Sprite、做旧层同 Sprite、14% Alpha、透明滚动射线层、Viewport 无 Image、空状态文字与居中对齐断言；Controller 测试覆盖两种对齐和新文案。
- [x] 通过——静态面板、纹理、标题、滚动层级均归 Builder，运行时结果与对齐归 Controller；Prefab 由 Unity Editor 执行 Builder 生成，未手写 YAML；UiSceneAsset 未改。
- [x] 通过——仅增加局部 `hasRecommendations` 变量用于同一次刷新分支，未增加字段或 helper；修改范围限定于融合推荐弹窗、测试和直接相关文档。
- [x] 通过——已按 `design-doc-format` 更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`，记录羊皮纸弹窗与中央空状态。
- [x] 通过——已按 `art-doc-writer` 及 UI/模块格式更新 UI 美术总览和备战卡池模块文档，把跨界面复用的战场底板纳入通用资产分组并记录弹窗视觉构成。
- [x] 通过——已按 `program-doc-format` 的 UI 界面格式更新 `AutoDoc/Program/UI/preparation/preparation.md`，记录 Builder 层级、透明射线区和空状态对齐逻辑。
- [x] 通过——Unity 编译完成，最终 Console 错误为 0；Prefab 实例检查确认 `1240 × 700`、底板/做旧 Sprite 与战斗页相同、做旧 Alpha=`0.14`、滚动底图 Alpha=`0`、Viewport 无 Image；两个相关 EditMode 测试分别 1/1 通过；代码与文档 `git diff --check` 通过。
- [x] 通过——结束审计后已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0；随后创建同名 Report。
