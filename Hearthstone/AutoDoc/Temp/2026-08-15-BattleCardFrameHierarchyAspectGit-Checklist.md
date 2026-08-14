# 战斗卡牌边框层级与立绘比例检查清单

## 需求与实现

- [x] 将双方战斗卡牌框调整为能完整包住立绘与下侧说明栏的窄边尺寸，同时不覆盖外露属性标志。证据：卡面 `250 × 360`，三层边框均为 `230 × 348`；立绘视窗 `222.5 × 297`，说明栏 `180 × 75.6`。
- [x] 确保基础边框、攻击高亮框、目标高亮框的渲染层级均低于生命、攻击和卡牌编号。证据：Prefab 同级索引分别为 `2/3/4` 与 `6/7/8`。
- [x] 在 Prefab 与运行时绑定中确保卡牌立绘保持原始宽高比，不做非等比拉伸。证据：Builder 固化 `Image.Type.Simple + preserveAspect=true`，Controller 绑定时再次设置 `preserveAspect=true`。
- [x] 保持生命标志在左、攻击标志在右，攻击标志无剑图标且数值清晰。证据：生命/攻击锚点与中心位置为左下 `(30,30)`、右下 `(-30,30)`，攻击继续使用 `AttackBadgeFrame`，文字为 30 号白色粗体并带深色描边。
- [x] 更新编辑器测试，覆盖边框尺寸、层级、立绘比例与属性标志要求。证据：两项定向 EditMode 测试通过。
- [x] 通过项目 UiBuilder 重建 `BattleCardItem.prefab`，未手写 Prefab YAML。证据：由官方 `unityMCP execute_code` 调用 `BattleCardItemUiBuilder.Build()` 成功保存。

## 框架与验证

- [x] 未绕过 UI 框架边界；只扩展已有一一对应 Builder 的静态布局配置，没有新增运行时查找或重复的一次性抽象。
- [x] Unity 编译通过，活动场景为 `Main` 且未脏，最终 Console 错误数为 0。
- [x] 完整 `Hearthstone.Tests` EditMode 测试执行 16 项；本次 UI 测试通过。另有 1 项既存数据断言失败：配置为 `Boar_001`，旧断言期待 `Boar`，未纳入本次卡框范围。

## 文档同步

- [x] 玩家视角设计文档：已完整读取 `design-doc-format` 及战斗系统专项格式；当前变化是边框覆盖范围、标志层级和立绘等比展示；已更新 `AutoDoc/Design/Specific/combat-system/combat-system.md`，证据为 Prefab 探针与 Builder/Controller 实现。
- [x] 美术文档：已完整读取 `art-doc-writer`、UI 美术格式与模块美术格式；当前变化是边框尺寸、层级和原画缩放约束；已更新 `AutoDoc/Art/UI/ui-art-overview.md` 与 `AutoDoc/Art/Modules/battle-card/battle-card.md`。
- [x] 程序文档：已完整读取 `program-doc-format` 与 UI 界面格式；当前变化是 Builder 固化尺寸、层级及 `preserveAspect`；已更新 `AutoDoc/Program/UI/battle/battle.md`。

## Git 与结束审计

- [x] 已核对 Git 根目录、`main` 分支与 `origin` 远端；明确排除当前并行任务的资源字典、加载数据、策划案、卡图 `.meta` 和其他检查清单。
- [x] 已完成差异检查，任务文件无空白错误；将只显式暂存本清单列出的实现、Prefab、测试与现状文档。
- [ ] 执行一次 `AutoDoc/CleanupTempDocs.bat` 并生成对应任务报告。
- [ ] 提交 Git 并推送到 `origin/main`；在 Git 操作完成后更新最终审计记录。
