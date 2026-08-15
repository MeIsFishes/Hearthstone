# 怪物战斗词条与融合继承实施 Checklist

- [x] 通过：仅修改关键词案独占源码、配置、四类动态条目 Builder/Prefab 与独立测试；Continue/Stage 文件只读核对。
- [x] 通过：新增强类型、不可变且防御性复制的三对三 `BattleScenarioStartupData` / `BattleCardSlotStartupData`，覆盖空槽、RunState/Explicit、当前生命和固定随机种子校验。
- [x] 通过：稳定 `BattleCardRawComponent` 公共契约，保留 `int Attack`，新增同步 `AttackValue`、词条、本场增益、延迟生命/死亡提交及对称回收。
- [x] 通过：新增词条枚举与唯一权威 CSV；名称/顺序、远射与爆裂比例、爆裂距离、冲锋增益、反伤门控均由配置驱动；五类普通怪物映射为单个嘲讽/远射/爆裂/冲锋/None。
- [x] 通过：Run 卡实例持有词条，奖励/初始卡自动取类型词条；融合使用去重并集且排除 None，保持 99 禁作素材、Batch 指纹与事务原子语义。
- [x] 通过：战斗实现嘲讽候选、远射向下取整且免主目标反伤、爆裂相邻槽、冲锋存活友军增益、统一延迟死亡/胜负提交和结构化日志。
- [x] 通过：无词条互伤规则、攻击轮换、池化回收、旧 `Attack` 调用和默认 Startup null 契约保持兼容。
- [x] 通过：四类动态条目 View/Controller 使用统一词条格式，Battle 攻击显示监听 `AttackValue`，空 Entity/换绑/回池清理监听和文本。
- [x] 通过：四个独占 UiBuilder 生成对应独占 Prefab，只做 Pre-UiInit；最终 Battle Builder 重建未改变共享 FontAsset mtime，源码不调用共享 Exporter/PreLoad/资源索引/字体补字/Preparation 场景导出。
- [x] 通过：新增独立 `BattleKeywordRulesTests.cs`，9项覆盖配置行为、旧CSV兼容/非法初始词条拒绝、稳定两行显示、融合、战斗计算、DTO、Attack兼容、延迟死亡、回收及四Prefab字段。
- [x] 通过：官方 Unity 全量 refresh 后关键词 EditMode 9/9，独立编译0错，四个独占 Prefab 字段核对通过；收尾 Console 0 Error。
- [x] 通过：未修改 Continue/Stage 独占源码、专属 Entry、正式现状文档、Review/状态；Unity 仅为新独占文件正常导入 `.meta`，未手写或编辑 `.meta`。共享字体/索引的既有并发改动已交唯一整合者处理。
