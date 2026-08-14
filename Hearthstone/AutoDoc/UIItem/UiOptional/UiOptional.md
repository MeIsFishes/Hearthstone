# UiOptional

## 1. 组件用途

`UiOptional` 用于管理一组 `Button` 的选择状态，支持按名称或索引管理按钮，并提供选中、取消选中和选中列表变化回调。

适合用于标签页、单选按钮组、多选筛选项、技能选择等按钮组选项。

## 2. 基本使用流程

1. 在按钮组根节点或控制节点上挂载 `UiOptional`。
2. 设置 `SelectLimit` 和 `ClickWhenSelected`。
3. 选择用名称或索引管理按钮。
4. 使用自动搜索或手动填写 `ButtonDic` / `ButtonList`。
5. 在 Controller 中通过 `NameWrapper` 或 `IndexWrapper` 注册回调或主动切换选中状态。

## 3. 配置项

### SelectLimit

- 描述：允许同时选中的按钮数量上限。
- 默认行为：默认值为 `1`，表现为单选。
- 配置值说明：填写整数；`1` 表示单选，大于 `1` 表示最多可选多个。

### ClickWhenSelected

- 描述：控制已选中按钮再次点击时的行为。
- 默认行为：使用 Inspector 当前保存的枚举值。
- 配置值说明：
  - `Unselect`：再次点击已选中的按钮会取消选中。
  - `KeepSelected`：再次点击已选中的按钮保持选中。

### TransformOverride

- 描述：自动搜索按钮时使用的根节点。
- 默认行为：为空时使用当前组件所在 Transform。
- 配置值说明：填写一个 Transform；自动搜索会从该节点子级查找 `Button`。

### StoreButtonsWith

- 描述：控制按钮按名称还是按索引存储。
- 默认行为：使用 Inspector 当前保存的枚举值。
- 配置值说明：
  - `Name`：使用 GameObject 名称作为按钮 key，适合按钮含义固定的场景。
  - `Index`：使用列表顺序作为按钮 key，适合按顺序访问的按钮组。

### AutoSearchButtons

- 描述：控制 PreInit 时是否自动搜索按钮。
- 默认行为：默认值为 `true`。
- 配置值说明：
  - `true`：编辑器 PreInit 时从 `TransformOverride` 下自动收集按钮。
  - `false`：不自动搜索，使用手动填写的 `ButtonDic` 或 `ButtonList`。

### ButtonDic

- 描述：按名称存储的按钮字典。
- 默认行为：`StoreButtonsWith` 为 `Name` 时使用；自动搜索会用按钮 GameObject 名称填充 key。
- 配置值说明：key 为按钮名称，value 为 `Button` 引用；运行时通过 `NameWrapper` 使用 key 访问。

### ButtonList

- 描述：按索引存储的按钮列表。
- 默认行为：`StoreButtonsWith` 为 `Index` 时使用；自动搜索会按搜索顺序填充列表。
- 配置值说明：元素为 `Button` 引用；运行时通过 `IndexWrapper` 使用下标访问。

### NameWrapper

- 描述：按名称操作按钮的运行时入口。
- 默认行为：不在 Inspector 中配置，通过代码访问。
- 配置值说明：当 `StoreButtonsWith` 为 `Name` 时使用，key 必须存在于 `ButtonDic`。

### IndexWrapper

- 描述：按索引操作按钮的运行时入口。
- 默认行为：不在 Inspector 中配置，通过代码访问。
- 配置值说明：当 `StoreButtonsWith` 为 `Index` 时使用，index 必须在 `ButtonList` 范围内。

## 4. 常用 API

- `NameWrapper.ToggleSelected(string name)` / `IndexWrapper.ToggleSelected(int index)`：切换选中状态。
- `NameWrapper.Select(string name)` / `IndexWrapper.Select(int index)`：选中指定按钮。
- `NameWrapper.Unselect(string name)` / `IndexWrapper.Unselect(int index)`：取消选中指定按钮。
- `AddOnClickCallback`：注册按钮点击回调。
- `AddOnSelectedCallback`：注册进入选中状态时的回调。
- `OnButtonSelected`：按钮被选中时触发。
- `OnButtonUnselected`：按钮取消选中时触发。
- `OnButtonDirty`：选中列表变化时触发。

## 5. 使用示例

```csharp
m_View.TabOptional.NameWrapper.OnButtonSelected += OnTabSelected;
m_View.TabOptional.NameWrapper.Select("Inventory");
```
