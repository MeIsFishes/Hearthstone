# 卡牌编号与双 CSV 配置任务检查清单

- [x] **通过｜核实现状**：已核对现有五种卡、食人魔 `CardTypeId=5`、`BattleStage → BattleCardRawComponent → BattleCardItemController` 链路、五张 Sprite 与 `BattleCardItem.prefab` 布局。
- [x] **通过｜数据框架**：已完整读取 `config-data-design`、`DataApi.md`、`CsvData.md`；两表均继承 `CsvDataBase<T>`、由默认数据组反射加载并以 `DataApi.SetData(int, this)` 登记。
- [x] **通过｜第一张 CSV**：`BattleCardTypeCsvData.csv` 共五行，字段为名称、生命最小/最大、攻击最小/最大；范围均为合法整数闭区间。
- [x] **通过｜第二张 CSV**：`BattleCardCsvData.csv` 经过滤规范注释后共 98 行；编号 1~98 连续、无缺失、无重复，种类与立绘键齐全。
- [x] **通过｜编号分配**：类型计数为 `20/20/20/19/19`；食人魔编号为 `40,44,45,49,59,60,66,67,68,72,74,76,78,79,86,91,92,95,97`，平均 `70.42`，其余类型平均 `41.35~49.11`；35~98 内另有 45 个非食人魔编号。
- [x] **通过｜运行时接入**：编号表取得 `CardTypeId/ArtworkKey`，种类表取得名称与攻血范围；会话随机数在闭区间生成攻血，Component 保存编号与种类并在回池时重置。
- [x] **通过｜UI**：编号在卡面左上角显示；六边形 Image 与 TMP 文本只在条目首次绑定时创建，之后复用，解绑隐藏、换绑更新。
- [x] **通过｜编号框资源**：imagegen 生成图经检查和缩放后保存为 `CardNumberBadgeHex.png`；尺寸 `384×256`、ARGB、四角 Alpha 均为 0；`ResourceApi.LoadSprite("CardNumberBadgeHex")` Editor 测试通过。
- [x] **通过｜代码质量**：种类范围的随机方法复用于攻血；编号 UI 缓存随条目复用；未新增一次性管理器、平行随机池或每次刷新重复创建对象。
- [x] **通过｜直接依赖与回归**：资源字典含两张 CSV 与六边形 Sprite；五张立绘引用均存在；相关 5 项 EditMode 测试 `5/5` 通过，新增 Sprite 资源测试 `1/1` 通过。
- [x] **通过｜框架边界审计**：配置只走 CsvData/DataApi，Sprite 只走 ResourceApi，资源字典由 Unity 官方菜单中的既有 Builder 生成，UI 走既有 Controller 生命周期；无手写 Prefab/YAML、平行 CSV 解析或管理器绕过。
- [x] **通过｜玩家视角设计文档**：已读取基础 skill 与战斗系统专项格式，更新 `AutoDoc/Design/Specific/combat-system/combat-system.md` 的编号、范围、双表与六边形表现。
- [x] **通过｜美术文档**：已读取基础 skill、模块/UI 专项格式，更新战斗卡牌模块与 UI 总览，并登记 `CardNumberBadgeHex.png` 的当前资产规格。
- [x] **通过｜程序文档**：已读取基础 skill、战斗系统与 UI 界面专项格式，更新双 CSV、运行时随机属性、编号 Component、资源加载和 UI 复用链路。
- [x] **通过｜验证**：Unity 编译无 `CS` 错误；静态 CSV/资源/Alpha 校验通过；未进入 Play Mode。全量 EditMode `13/14` 通过，唯一失败为既有中文字体字形测试；本次相关测试全部通过。Console 另保留 Test Runner 写出结果路径的无堆栈 Exception 级日志，不是编译错误。
- [x] **通过｜结束审计**：已逐项对照实际文件、Unity 编译、资源字典、测试结果和静态统计；修正了随机数参数传递、CSV 统计注释过滤与旧文档引用。
- [x] **通过｜临时文档清理与报告**：已且仅已执行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 0；清理阈值未触发，Markdown 数量保持 `50 → 50`。随后创建同任务报告。
