# 音频播放程序文档

## 1. 核心数据来源

### 1.1 Component

当前无。音频播放状态由跨场景常驻的 `AudioRuntimeDriver` 和内部 `Playback` 记录持有，不写入 ECS Component。

### 1.2 Csv和ScriptableObject配置项

当前无业务 Csv 或 ScriptableObject 音频配置。`ResourceApi.LoadAudio()` 通过统一资源索引按不含路径与扩展名的 key 异步加载 AudioClip；当前 BGM 资源为 `Assets/Resources/BGM/Lobby.mp3`、`Battle.mp3`、`Win.mp3` 与 `Failed.mp3`，索引 key 分别为 `Lobby`、`Battle`、`Win` 与 `Failed`。`Battle.mp3` 是当前业务所称的第一首战斗曲；`Battle2.mp3` 已存在于资源目录，但当前流程没有请求它。结算横幅继续点击使用 `Assets/Resources/BbxCommon/Audio/Library/UI Audio/click1.ogg`，唯一索引 key 为 `click1`。

## 2. 逻辑驱动

### 2.1 System

当前无 ECS System。`AudioManager` 按需创建 `DontDestroyOnLoad` 的 `AudioRuntimeDriver`，后者在 `Update()` 中使用 `Time.unscaledDeltaTime` 推进淡入淡出包络、检测非循环 AudioSource 的自然结束，并把停止的声源归还 `GameObjectPool<AudioSource>`。播放音量由调用音量、并发衰减和包络倍率共同计算，包络不改写调用方设置的目标音量。

#### 2.1.1 重要的System顺序依赖

当前无 ECS System 顺序依赖。淡入和淡出由同一个 Driver 更新；对同一播放项发起淡出时会终止尚未结束的淡入，并从当时的包络倍率继续衰减，避免音量跳变。

### 2.2 StageListener

`BattleBgmStageListener` 属于 `BattleStage`，监听战斗会话的正式结果。玩家胜利与敌方胜利分别请求 `Win`、`Failed`，过渡时长均为 `0.5 s`，并明确设置 `loop: false`。

### 2.3 关联Task启动入口

当前无。

### 2.4 调用链路梳理

1. 普通音效继续通过 `AudioApi.Play()` 创建独立播放句柄，可按句柄或分组停止，并沿用优先级、并发限制、声像和音量能力。战斗结算横幅的有效继续点击以 `0.7` 音量播放一次 `click1`；播放发生在点击被消费后、备战或主菜单分流前，因此无效点击和重复点击不会叠播。
2. BGM 通过 `AudioApi.SetBgm(string key, float transitionDurationSeconds = 0f, bool loop = true)` 设置。该入口把所有 BGM 的目标音量统一设为 `0.7`，不改变普通 `AudioApi.Play()` 音效的默认音量。切换时长为可选参数，默认即时切换；大于零时，新曲从零音量淡入，旧曲从当前包络淡出，二者共享同一时长。
3. 当前 key 仍在播放时再次请求同一 BGM 会直接返回现有句柄，不重新加载或从头播放。请求不同 key 时先登记新播放，再淡出旧句柄；空 key 会返回无效句柄且不打断当前 BGM，非有限或非正时长按即时切换处理。
4. Driver 通过 `ResourceApi.LoadAudio()` 异步取得 AudioClip。句柄若在加载完成前已经停止，完成回调不会再分配声源；资源不存在时播放项会被清理。
5. 非循环 BGM 在 AudioSource 自然结束后仅停止并回收，不自动恢复上一首、不维护播放列表，也不隐式请求新曲。调用方仍可使用 `SetBgm()` 返回的句柄通过现有 `AudioApi.Stop()` 或 `FadeOut()` 主动停止。

## 3. 所属GameStage

音频管理器属于跨 Stage 常驻的框架服务，不归某个业务 GameStage 独占。`HearthstoneGameEngine.OnStageLoadingCompleted()` 只在目标 StageGroup 已确认激活后设置阶段 BGM：MainMenu 与 Preparation 使用循环 `Lobby`，Battle 使用循环 `Battle`；大厅进入首轮备战时因同 key 保持连续。`BattleStage` 另外注册 `BattleBgmStageListener`，在正式胜负产生时把循环战斗曲切换为一次性结算曲。
