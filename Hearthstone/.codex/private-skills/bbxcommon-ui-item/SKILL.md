---
name: bbxcommon-ui-item
description: 新增或修改 BbxCommon UI 组件时使用。
---

# UI 组件新增与改动规则

当用户要求新增、修改、重构或补充 BbxCommon UI 组件时，使用本 skill。

## 1. UI 组件定义

本 skill 中的 **UI 组件** 指挂载在 UI GameObject 上、服务于 UI 表现或 UI 交互的项目自定义组件，不包含 Unity 原生组件和第三方插件组件。

属于 UI 组件的类型包括：

- 继承 `BbxUiItem` 的组件。
- 位于 BbxCommon UI 模块内、供 `UiView`、`UiController`、`HudView` 或 `HudController` 使用的自定义组件。
- 为 UI 提供通用能力的自定义组件，例如列表排列、选项管理、拖拽、Tween、事件封装、Transform 设置等。

不属于 UI 组件的类型包括：

- `RectTransform`、`CanvasGroup`、`Button`、`Image`、`TextMeshProUGUI` 等 Unity 或 Unity UI 组件。
- 只服务于具体玩法规则的业务脚本。
- 不挂载在 UI GameObject 上的系统、数据类、配置类和工具类。

## 2. 代码约束

新增 UI 组件必须继承 `BbxUiItem`。

修改现有 UI 组件时，若该组件尚未继承 `BbxUiItem`，本次改动必须同时处理继承关系；只有用户明确要求不处理继承关系时，才保留原继承方式。

UI 组件不得直接写入具体玩法规则。组件需要业务数据、业务判断或业务回调时，由 `UiController`、`HudController` 或外部调用方传入。

## 3. 组件文档路径

新增或改动 UI 组件后，必须同步新增或更新组件文档。

UI 组件必须维护一篇总文档，总文档固定放在：

```text
AutoDoc/UIItem/UiItemIndex.md
```

总文档必须收录目前已有的全部 UI 组件，并为每个组件写一句简短应用场景说明。新增 UI 组件时，必须把新组件加入总文档；删除或改名 UI 组件时，必须同步更新总文档。

总文档不使用单个组件文档模板。总文档必须使用以下格式：

```markdown
# UiItemIndex

## 1. 文档用途

## 2. 底层组件

## 3. 业务组件
```

`底层组件` 指一般不直接作为业务 UI 使用，而是辅助业务组件功能实现的组件。

`业务组件` 指业务 UI 可以直接挂载、配置和调用，用来完成明确 UI 表现或交互需求的组件。

组件文档固定放在：

```text
AutoDoc/UIItem/<ComponentName>/<ComponentName>.md
```

`<ComponentName>` 必须使用组件类名。

示例：

```text
AutoDoc/UIItem/UiList/UiList.md
AutoDoc/UIItem/UiOptional/UiOptional.md
AutoDoc/UIItem/UiTweenAlpha/UiTweenAlpha.md
```

一个组件对应一篇组件文档。一次改动涉及多个 UI 组件时，必须分别维护每个组件的文档。

## 4. 组件文档内容

组件文档只写组件使用方式，不写代码底层实现。

组件文档必须包含以下章节：

```markdown
# <ComponentName>

## 1. 组件用途

## 2. 基本使用流程

## 3. 配置项

## 4. 常用 API

## 5. 使用示例
```

每个章节只记录使用者需要知道的信息：

- `组件用途`：说明组件解决什么 UI 使用问题，以及应挂载在哪类 UI GameObject 上。
- `基本使用流程`：按实际操作顺序说明如何添加组件、填写配置项、在 View 或 Controller 中调用。
- `配置项`：列出每一个 Inspector 配置项、公开字段、公开属性和常用 Wrapper 字段。
- `常用 API`：只列使用者会直接调用的公开方法、事件和回调。
- `使用示例`：给出最小可用配置或最小调用示例。

禁止写入以下内容：

- 底层类关系。
- 缓存结构。
- 内部算法。
- 源码走读。
- 底层调用链。
- 与当前组件无关的 UI 框架总览。

## 5. 配置项写法

每个配置项只允许使用以下三项说明：

```markdown
### <配置项名>

- 描述：
- 默认行为：
- 配置值说明：
```

`配置值说明` 必须覆盖该配置项的全部可用取值。

如果配置项是 `bool`，必须分别说明 `true` 和 `false`：

```markdown
- 配置值说明：
  - `true`：
  - `false`：
```

如果配置项是枚举，必须列出每一个枚举值：

```markdown
- 配置值说明：
  - `EnumValueA`：
  - `EnumValueB`：
```

如果配置项是数字、字符串、对象引用、列表、字典或 Wrapper，必须说明可填写内容、空值行为和运行时访问方式。

## 6. 示例文档

组件文档示例位于：

```text
.codex/private-skills/bbxcommon-ui-item/examples/UiList-doc-example.md
```

该示例展示使用型组件文档的目标格式。新增或改动组件文档时，按该示例的章节组织方式和配置项写法执行。
