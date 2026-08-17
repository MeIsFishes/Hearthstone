# 局外养成系统程序文档

## 1. 核心数据来源

### 1.1 Component

`RunStateSingletonRawComponent` 仅在备战界面打开时用于补登记当前局已拥有卡牌，不作为永久收藏的权威数据源。

### 1.2 Csv和ScriptableObject配置项

`BattleCardCsvData` 决定卡牌是否存在及融合配方素材数；`BattleCardTypeCsvData` 提供图鉴卡面显示内容。当前可收藏规则为：排除 99 号分隔位，排除配方长度为 4 的融合结果。

### 1.3 持久化数据

`CardCollectionRepository` 保存格式版本和去重后的已解锁卡号集合。生产入口由 `CardCollectionSave.Repository` 提供，文件位于 Windows 用户本地应用数据目录下的 `DefaultCompany/99升变/card-collection.json`。

## 2. 逻辑驱动

### 2.1 System

当前无专用 ECS System。

#### 2.1.1 重要的System顺序依赖

当前无。

### 2.2 StageListener

当前无。

### 2.3 关联Task启动入口

当前无。

### 2.4 调用链路梳理

备战奖励批次成功应用后登记批次卡号；融合成功后登记结果实例的 `PresentationCardNumber`，因此双卡和三卡登记自身，四卡登记点数最高三张对应的三卡表现卡；备战页打开时逐副本补登记当前局已有实例的同一表现卡号。图鉴打开时先从卡表生成卡位，再从存档快照判断锁定状态并刷新计数。

## 3. 养成规则链路

### 3.1 货币和积分流转

当前无。

### 3.2 提升与解锁流程

登记入口先通过 `CardCollectionCatalog.IsCollectible` 校验范围，再向集合添加卡号。重复获得不会重复计数或写入；四卡融合结果与 99 号位本身仍被拒绝，但四卡结果实例携带的三卡 `PresentationCardNumber` 可以正常登记。

### 3.3 奖励和状态刷新

图鉴每次显示时重新读取解锁快照、重建 147 个卡位并更新右上角计数。未解锁项绑定锁定面、禁用点击并从卡牌配置生成合成配方悬停说明；基础卡显示无合成配方。已解锁项绑定标准卡面。共享卡牌 View 为悬停、点击和拖拽分别保存独立 `UiEventListener`，各绑定上下文通过 Controller 显式设置权限；图鉴始终关闭拖拽，只保留悬停、滚轮和已解锁卡点击。

### 3.4 存档读写影响

新增解锁会立即覆盖单一存档；调试清除只删除主文件与同名临时文件。解锁记录不影响局内卡牌实例、卡组或战斗数值。

## 4. 所属GameStage

解锁写入发生于 `PreparationStage` 奖励应用与备战 UI 生命周期；浏览和清除发生于 `MainMenuStage`。
