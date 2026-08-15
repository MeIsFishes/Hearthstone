# 嘲讽盾牌轮廓检查清单

- [x] 通过｜生成银灰与暗钢空心盾牌；实际查看确认无文字、徽记或水印，中心、画布四角均为 Alpha 0。
- [x] 通过｜`ApplyCardContent()` 使用 `BattleKeywordRules.Has(keywords, EBattleKeyword.Taunt)` 控制显隐；无嘲讽时关闭。
- [x] 通过｜运行矩形 `278 × 360`，保持原图比例后的实际宽约 `270 px`，相对 `250 px` 卡面每侧约露 `10 px`；Prefab 根节点索引为 0。
- [x] 通过｜已读取 imagegen、generate-art-assets、项目总风格、战斗卡模块文档及现有卡框/徽章/空卡槽参考，并保留最终提示词供报告记录。
- [x] 通过｜使用内置 imagegen 生成一张透明 PNG，随后用 `view_image` 和 ARGB 像素采样检查主体、材质、透明中心与边缘。
- [x] 通过｜最终资源为唯一英文名 `Assets/Resources/Art/BattleCards/UI/TauntShieldOutline.png`；未手工创建、编辑或删除 `.meta`，导入元数据由 Unity 管理。
- [x] 通过｜已定位并复用 `BattleCardItem` Prefab、View、Controller、Builder、`ApplyCardContent()` 关键词链与既有 UI 对象池。
- [x] 通过｜静态对象和序列化引用由 `BattleCardItemUiBuilder.Build()` 创建并通过 Unity Editor 正式执行写回；未手写 Prefab YAML。
- [x] 通过｜Controller 绑定内容时刷新盾牌，`HideCardPresentation()` 在换绑、隐藏、空卡和回池链路强制关闭；死亡/复活不改写静态词条且无跨对象残留。
- [x] 通过｜资源经 Builder 的 `AssetDatabase`/`LoadSprite()` 既有导入流程固化，运行时不新增资源系统；UI 仍由 View/Controller/Builder 管理。
- [x] 通过｜仅新增一个 View 引用和一个 Builder 配置函数；没有新增平行服务、无状态包装或一次性业务抽象。
- [x] 通过｜新增 Prefab/资源/关键词断言；定向测试 1/1、关键词测试 9/9 通过；三个相关程序集均 0 错误。整组 BattleRules 另有两项与本任务无关的既有失败，已记录。
- [x] 通过｜PNG 为 `1086 × 1448`、`Format32bppArgb`，角点和中心 Alpha 均为 0；Prefab 默认关闭、根层索引 0、Sprite 导入为 Single/Alpha/无 Mipmap/Clamp。
- [x] 通过｜定向 diff 审查未覆盖或回退 Controller 中既有备战拖拽改动；仅追加本任务字段、显隐和测试。
- [x] 通过｜已按设计文档基础与战斗专项格式更新当前嘲讽盾牌玩家反馈。
- [x] 通过｜已按美术模块格式记录盾牌风格、生成规格、尺寸、用途与真实项目路径。
- [x] 通过｜已按战斗系统与战斗 UI 程序文档格式记录关键词来源、静态层级与生命周期。
- [x] 通过｜本清单已在结束审计中逐项复核；下一步仅执行一次清理脚本并创建同名报告。
