# 播放与生命周期

在新增或修改音频播放代码前，读取实际接口：

- `Assets/Scripts/BbxCommon/Api/AudioApi.cs`
- `Assets/Scripts/BbxCommon/AudioManager/AudioPlayOptions.cs`
- `Assets/Scripts/BbxCommon/AudioManager/AudioHandle.cs`
- `Assets/Scripts/BbxCommon/Api/ResourceApi.cs`

## 基础播放

简单单次音效直接传资源键；需要初始音量时使用音量重载：

```csharp
AudioHandle handle = AudioApi.Play("ImpactWood001");
AudioHandle quietHandle = AudioApi.Play("MissileFlight001", 0.35f);
```

需要更多控制时，必须从 `AudioPlayOptions.Default` 开始。不要用 `new AudioPlayOptions()` 代替默认值，否则结构体字段会从零开始，语义不同。

```csharp
var options = AudioPlayOptions.Default;
options.Volume = 0.6f;
options.Pitch = 1.05f;
options.PanStereo = 0.2f;
options.Loop = false;
options.Priority = 96;
options.GroupKey = "Combat";
options.ConcurrencyKey = "PlayerBulletImpact";
options.MaxConcurrent = 6;
options.ConcurrencyVolumeFalloff = 0.65f;

AudioHandle handle = AudioApi.Play("ImpactWood001", options);
if (!handle.IsValid)
{
    // 请求因空键、并发上限或总声部上限被拒绝；不要继续操作该句柄。
}
```

参数语义：

- `Volume`：0～1；运行中可用 `AudioApi.SetVolume` 修改。
- `Pitch`：播放速度和音高；当前底层规范化到 -3～3，非正值会回退为 1。通常使用小范围随机变化，避免夸张失真。
- `PanStereo`：-1～1；运行中可用 `AudioApi.SetPanStereo` 修改。
- `Loop`：是否循环。循环声音必须保存句柄或分组，并定义停止时机。
- `Priority`：0 最重要，256 最不重要。底层声部达到硬上限时，低重要度且较早的声音可能被更重要的新声音替换。
- `GroupKey`：生命周期和业务域分组，供 `StopGroup` 及并发隔离使用。
- `ConcurrencyKey`：同组内同类叠音键；为空表示不参与该类并发管理。
- `MaxConcurrent`：大于 0 时限制同一 `GroupKey + ConcurrencyKey` 的请求数，达到上限的新声音被拒绝。
- `ConcurrencyVolumeFalloff`：0 表示不做叠音响度重平衡；大于 0 时按播放先后使用 `1, f, f²...` 权重并归一化，后入声音更弱。它不是延迟播放队列。

## 高频叠音

子弹命中、连续射击、导弹飞行、引擎等高频声音必须设置稳定的 `GroupKey` 和 `ConcurrencyKey`。先按可辨识度确定并发上限，再用衰减控制总响度。例如导弹飞行声可共享 `MissileFlight` 并发键，设置较低初始音量、有限并发数和小于 1 的衰减值，避免数量增长时响度线性叠加。

不要只依赖底层 64 声部硬上限；业务系统可以设置更低的总声部限制。连续事件还可组合以下手段：

- 为同类事件准备少量变体，随机选键或轻微改变 `Pitch`。
- 对不重要的密集命中声降低优先级和音量。
- 对长循环使用每个所有者唯一句柄，避免同一实体重复启动。
- 若 `Play` 返回无效句柄，直接跳过登记，不把它加入活动声音表。

## 循环与 BGM

```csharp
private AudioHandle m_BgmHandle;

void StartBgm()
{
    AudioApi.StopGroup("BGM");

    var options = AudioPlayOptions.Default;
    options.Volume = 0.55f;
    options.Loop = true;
    options.Priority = 32;
    options.GroupKey = "BGM";
    options.ConcurrencyKey = "MainBgm";
    options.MaxConcurrent = 1;
    m_BgmHandle = AudioApi.Play("BgmWaiting8BitClear", options);
}

void StopBgm()
{
    AudioApi.Stop(m_BgmHandle);
    m_BgmHandle = default;
}
```

替换 BGM 时先停止旧组再启动新曲；如果先播放，`MaxConcurrent = 1` 会拒绝新请求。对象销毁、Stage 卸载、战斗结束或实体回收时调用 `Stop(handle)`；同域全部结束时调用 `StopGroup(groupKey)`。

`AudioHandle.IsValid` 只表示播放请求已被底层接受，不表示异步资源已经加载完成。请求加载期间调用 `Stop` 是安全的；不要直接持有或销毁底层 `AudioSource`。

## 加载与预热

- `AudioApi.Play` 已自动调用 `ResourceApi.LoadAudio`，一般不需要预加载 `AudioClip`。
- 明确需要复用 `AudioClip` 的非播放逻辑才调用 `await ResourceApi.LoadAudio(key)`，并处理 `null`。
- 预计短时间爆发大量声音时，在对应 Stage 初始化阶段调用 `AudioApi.Prewarm(count)`，减少运行时创建；不要每帧调用。
- Resources 音频由 Unity 加载；Mods 目录音频异步解码，当前支持 OGG、WAV、MP3、AIF/AIFF。

## 静态核对

完成后检查所有循环播放都有对应停止路径，所有资源键不含目录和扩展名，高频事件均有并发策略，音量在 0～1 范围内。除非用户要求，不以进入游戏作为默认验证步骤。
