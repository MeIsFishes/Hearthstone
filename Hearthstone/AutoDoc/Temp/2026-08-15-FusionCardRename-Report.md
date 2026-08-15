# 融合卡名称调整报告

## 结果

以下九个融合结果显示名已更新：

- 100：武士
- 120：劲弩手
- 124：掠阵先锋
- 128：十夫长
- 129：军阀
- 130：火枪手
- 131：火箭兵
- 146：野猪王
- 148：野猪王骑兵

仅修改 `BattleCardTypeCsvData.csv` 中对应类型的 `DisplayName`。卡号、类型 ID、融合公式、运行时属性、攻击表现和美术资源均未改变。

## 直接依赖与文档

- `BattleRulesTests.cs` 中 100 号融合类型名称断言已同步为“武士”。
- 玩家视角文档 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md` 中的示例已同步。
- Assets 与正式设计、美术、程序文档范围内已检索不到九个被替换的旧名称。
- 本次不影响视觉规范、资源清单、程序结构和加载行为，因此美术文档与程序文档无需修改。

## 验证

- 配方与名称映射校验：`CheckedNames=9`、`Failures=0`。
- `dotnet build Hearthstone.Tests.csproj --no-restore -v:q -clp:ErrorsOnly`：成功，0 错误；保留 8 个项目既有程序集版本警告。
- Unity Editor 最新日志尾部未发现编译错误。
- 定向 `git diff --check` 未发现本次文件的空白错误。
- 未进入 Play Mode；名称属于纯配置文本调整，未另启 Unity Test Runner。

## 框架与变更范围

- 名称继续通过既有 CSV/DataApi 配置链路加载，没有引入平行数据源或绕过框架。
- 未修改运行时代码结构，未新增字段或函数。
- 工作区原有与并行修改均保留；本次没有创建、修改或删除 `.meta` 文件。

## 偏差与风险

无需求偏差。剩余风险仅为未在 Play Mode 中人工观察九个名称的具体排版长度，其中“野猪王骑兵”为本组最长的新名称。

## 清理

`AutoDoc/CleanupTempDocs.bat` 只执行一次，退出码为 0。
