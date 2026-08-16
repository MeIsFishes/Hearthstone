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

备战奖励批次成功应用后登记批次卡号；融合成功后登记结果卡号；备战页打开时补登记当前局已有卡。图鉴打开时先从卡表生成卡位，再从存档快照判断锁定状态并刷新计数。

## 3. 养成规则链路

### 3.1 货币和积分流转

当前无。

### 3.2 提升与解锁流程

登记入口先通过 `CardCollectionCatalog.IsCollectible` 校验范围，再向集合添加卡号。重复获得不会重复计数或写入；四卡融合结果与 99 号位被拒绝。

### 3.3 奖励和状态刷新

图鉴每次显示时重新读取解锁快照、重建 147 个卡位并更新左上角计数。未解锁项绑定锁定面且禁用点击；已解锁项绑定标准卡面。

### 3.4 存档读写影响

新增解锁会立即覆盖单一存档；调试清除只删除主文件与同名临时文件。解锁记录不影响局内卡牌实例、卡组或战斗数值。

## 4. 所属GameStage

解锁写入发生于 `PreparationStage` 奖励应用与备战 UI 生命周期；浏览和清除发生于 `MainMenuStage`。
