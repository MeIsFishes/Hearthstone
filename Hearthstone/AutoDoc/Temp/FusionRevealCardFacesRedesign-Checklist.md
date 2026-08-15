# 融合揭晓卡面重绘检查清单

## 1. 用户要求

- [通过] 移除融合动效中卡牌自身的灰色蒙底/矩形阴影，不影响全屏压暗遮罩。
  - 证据：Builder 与重建 Prefab 均不存在 `FloatingShadow`；专项 EditMode 测试同时确认全屏遮罩 Alpha 仍为 `0.78`。
- [通过] 重新设计融合问号面，参考项目现有卡面原画，采用中世纪与轻度油画风格。
  - 证据：最终资产 `FusionRevealQuestionFace.png` 已目检并由 Prefab 的 `SealedFace.Image` 引用。
- [通过] 问号面以做旧羊皮纸为主要视觉材质，并保留清晰、无文字歧义的问号焦点。
  - 证据：最终图为满幅做旧羊皮纸、中央暗金锻铁问号，不含其他文字、水印或现代图形。
- [通过] 重新设计融合卡背，参考项目现有卡面原画，采用中世纪与轻度油画风格，避免卡通感。
  - 证据：最终采用用户指定恢复的第一版深靛蓝皮革、旧木、暗金锻铁卡背；严格镜像迭代版未接入项目。
- [通过] 两张新图适配当前 `25:36` 竖版卡面比例，在放大揭晓时仍有足够细节与清晰轮廓。
  - 证据：问号面 `1047 × 1503`、卡背 `1044 × 1507`，均约为 `25:36`，Builder 绑定到 `250 × 360` 卡体。

## 2. 图像生成与资产接入

- [通过] 使用内置图像生成工具，每张独立资产单独生成；现有卡牌原画仅作为风格参考，不直接修改。
  - 证据：问号面与卡背分别调用内置 imagegen；参考图只用于风格与安全区约束。
- [通过] 生成图不包含品牌、水印、额外文字、现代图形、人物或与卡面无关的场景元素。
  - 证据：两张最终图已目检；只含卡面材质、纹饰和问号/中央徽记。
- [通过] 目检问号面和卡背的主体、材质、构图、风格与比例；必要时单项迭代。
  - 证据：问号面一次通过；卡背曾按用户反馈生成严格镜像迭代，随后遵照用户决定恢复第一版作为最终图。
- [通过] 将最终图保存到项目 `Assets/Resources/Art/Preparation/UI/`，使用新文件名，不覆盖未获授权的既有位图。
  - 证据：新增 `FusionRevealQuestionFace.png`、`FusionRevealCardBack.png`；仅按用户明确选择覆盖任务内刚生成的卡背草案。
- [通过] 通过项目资源流程生成/确认 Unity 导入配置，并在 Builder 中按资源路径加载；不手写 `.meta`。
  - 证据：Unity TextureImporter 确认为 Single Sprite、Alpha Is Transparency、无 Mipmap、Clamp；`.meta` 由 Unity 自动导入生成，未手工编辑。

## 3. UI 与代码质量

- [通过] 定位并只移除卡牌自身的 `FloatingShadow` 灰色蒙底，不删除全屏 `FusionRevealOverlay` 遮罩。
  - 证据：`PreparationViewUiBuilder.CreateFusionReveal()` 不再创建该节点；专项测试确认遮罩仍存在且 Alpha=`0.78`。
- [通过] 由 `PreparationViewUiBuilder` 将新问号面与新卡背固化进 `PreparationView.prefab`，不在 Controller 运行时拼装静态层级。
  - 证据：Builder 用 `LoadSprite()` 读取两张资源；当前 Builder 编译后已执行 Build，Prefab 中对应 Image GUID 与两张新图一致。
- [通过] View 继续只保存引用，Controller 动画与生命周期保持原职责；不引入无复用价值的字段或函数。
  - 证据：未修改 View 或 Controller；只把既有一次性面板生成 helper 的参数从 `Color` 改为 `Sprite`。
- [通过] 检查无关文件未被误改，直接依赖未遗漏，现有用户改动未被覆盖，且不手工创建或修改任何 `.meta`。
  - 证据：修改限定于两张新图、Builder、关联测试、Prefab 与三类现状文档；工作区其余大量并行改动未被回退或重写。
- [通过] 运行 Unity 编译、Prefab 结构/引用测试和相关 EditMode 回归；按项目默认不主动进入 Play Mode。
  - 证据：Unity Console 清空后 `0` error；三个针对性 EditMode 测试通过；未进入 Play Mode。

## 4. 框架边界审计

- [通过] 未绕过 View-Controller、UiBuilder、ResourceApi/Resources、Prefab 配置源或 Unity 导入流程。
  - 证据：静态层级由现有 `PreparationViewUiBuilder` 定义，资源沿用其 `LoadSprite()` 与 Unity TextureImporter，动画职责仍在原 Controller。
- [通过] 未直接编辑 `UiSceneAsset`、未手写 Prefab YAML、未建立平行 UI 或资源加载流程。
  - 证据：Prefab 由 Unity API/Builder 保存；Scene、UiSceneAsset、Group、DefaultShow、整体 Transform 均未改动。
- [不适用] 若发现框架能力缺口，按最小范围处理并记录证据。
  - 证据：现有 Builder、Sprite 导入和 Prefab 流程能够完整承载需求，无框架能力缺口。

## 5. 文档同步门槛

- [通过] 完整读取玩家视角设计文档格式 skill，核对现有备战融合文档与实际玩家可见变化，按需同步。
  - 证据：已读取 `design-doc-format`，并更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md` 的揭晓表现。
- [通过] 完整读取美术文档格式及模块格式，更新新卡面资产的视觉规格、参考与资产列表。
  - 证据：已读取 `art-doc-writer` 与 `art-module-format`，并更新模块风格、参考图和现有资产列表。
- [通过] 完整读取程序文档格式及 UI 界面格式，更新 Builder 层级、资源引用与灰色蒙底移除情况。
  - 证据：已读取 `program-doc-format` 与 `ui-screen-doc-format`，并更新 Preparation UI 的 Builder/Prefab 现状。

## 6. 结束审计与报告

- [通过] 逐项以实际资产、Prefab、代码、Unity 导入结果和测试证据复核。
  - 证据：已核对图像尺寸、导入配置、Prefab GUID/节点、Builder、文档、Console 与测试结果。
- [通过] 结束时只运行一次 `AutoDoc/CleanupTempDocs.bat` 并记录结果。
  - 证据：本任务结束阶段仅执行一次，退出码 `0`。
- [通过] 清理后创建 `AutoDoc/Temp/FusionRevealCardFacesRedesign-Report.md`。
  - 证据：清理完成后已创建对应报告，包含结果、提示词、验证、偏差、风险、文档与清理记录。
