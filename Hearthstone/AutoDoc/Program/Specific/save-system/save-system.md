# 存档系统程序文档

## 1. 核心数据来源

### 1.1 存档数据结构

图鉴使用单个 JSON 文件 `card-collection.json`。根对象包含整数 `Version`（当前为 1）和升序去重的 `UnlockedCardNumbers` 数组。写入先生成同目录 `.tmp` 文件，再替换或移动为正式文件。

### 1.2 Component

`RunStateSingletonRawComponent` 只用于补采集当前局已拥有卡号，不直接序列化。

### 1.3 Csv和ScriptableObject配置项

读取时通过 `BattleCardCsvData` 重新校验每个卡号是否属于当前可收藏范围；无 ScriptableObject 存档配置。

## 2. GameStage加载卸载链路

### 2.1 相关GameStage

`MainMenuStage` 承载图鉴读取与调试清除入口；`PreparationStage` 在奖励应用成功后写入新增解锁。

### 2.2 LoadItem和LateLoadItem

`PreparationStages.InitializePreparationRuntime` 在 `RunCardRules.ApplyRewardBatch` 返回 `Applied` 后登记奖励批次。当前无专用读档 LoadItem 或 LateLoadItem；存档由 Repository 首次访问时延迟读取。

### 2.3 加载顺序与依赖

CSV 数据必须先完成初始化，随后 Repository 才能按当前卡表过滤存档卡号。备战奖励必须先成功写入局内状态，之后才登记永久解锁。

### 2.4 卸载与清理

GameStage 卸载不会清除永久解锁。主菜单“清除数据”只删除图鉴主文件与同路径临时文件，并把内存集合重置为空。

## 3. 存档读写链路

### 3.1 手动存档流程

当前无手动保存入口。

### 3.2 自动存档流程

抽牌批次成功应用、融合成功或备战页补登记发现新增卡时调用批量或单卡登记；只有集合确实新增卡号时才写文件。

### 3.3 读档与状态恢复

首次访问 Repository 时读取 JSON，过滤无效和不可收藏卡号后形成内存集合。图鉴页面每次显示时读取集合快照，不恢复局内状态。

### 3.4 版本兼容与异常处理

文件包含版本字段，当前尚无迁移分支。文件缺失视为空收藏；反序列化或读取异常会记录警告并忽略该文件。写入目录不存在时自动创建。

## 4. 辅助逻辑项

### 4.1 System

当前无。

### 4.2 StageListener

当前无。

### 4.3 关联Task启动入口

当前无。
