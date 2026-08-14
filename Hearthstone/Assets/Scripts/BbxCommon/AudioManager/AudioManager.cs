using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BbxCommon.Internal
{
    internal static class AudioManager
    {
        private const int DefaultInitialCapacity = 8;
        private static AudioRuntimeDriver m_Driver;

        internal static AudioHandle Play(string key, AudioPlayOptions options)
        {
            return EnsureDriver(DefaultInitialCapacity).Play(key, options);
        }

        internal static void Stop(AudioHandle handle)
        {
            if (m_Driver != null)
                m_Driver.Stop(handle);
        }

        internal static void StopGroup(string groupKey)
        {
            if (m_Driver != null)
                m_Driver.StopGroup(groupKey);
        }

        internal static bool IsPlaying(AudioHandle handle)
        {
            return m_Driver != null && m_Driver.IsPlaying(handle);
        }

        internal static void SetPanStereo(AudioHandle handle, float panStereo)
        {
            if (m_Driver != null)
                m_Driver.SetPanStereo(handle, panStereo);
        }

        internal static void SetVolume(AudioHandle handle, float volume)
        {
            if (m_Driver != null)
                m_Driver.SetVolume(handle, volume);
        }

        internal static void FadeOut(AudioHandle handle, float durationSeconds)
        {
            if (m_Driver != null)
                m_Driver.FadeOut(handle, durationSeconds);
        }

        internal static void Prewarm(int count)
        {
            EnsureDriver(Mathf.Max(0, count)).Prewarm(count);
        }

        internal static void NotifyDriverDestroyed(AudioRuntimeDriver driver)
        {
            if (ReferenceEquals(m_Driver, driver))
                m_Driver = null;
        }

        private static AudioRuntimeDriver EnsureDriver(int initialCapacity)
        {
            if (m_Driver != null)
                return m_Driver;

            var gameObject = new GameObject(nameof(AudioManager));
            Object.DontDestroyOnLoad(gameObject);
            m_Driver = gameObject.AddComponent<AudioRuntimeDriver>();
            m_Driver.Initialize(initialCapacity);
            return m_Driver;
        }
    }

    internal sealed class AudioRuntimeDriver : MonoBehaviour
    {
        private const int HardVoiceLimit = 64;

        private sealed class Playback
        {
            public AudioHandle Handle;
            public string Key;
            public AudioPlayOptions Options;
            public long Sequence;
            public AudioSource Source;
            public AudioGainFilter GainFilter;
            public float ConcurrencyVolumeMultiplier = 1f;
            public float FadeOutDuration;
            public float FadeOutElapsed;
            public float FadeOutStartVolume;
            public bool IsFadingOut;
        }

        private readonly Dictionary<int, Playback> m_Playbacks = new();
        private readonly Dictionary<int, int> m_HandleVersions = new();
        private readonly Stack<int> m_FreeHandleIds = new();
        private readonly List<AudioHandle> m_StopBuffer = new(64);
        private readonly List<AudioSource> m_PrewarmBuffer = new(32);
        private readonly List<Playback> m_ConcurrencyBuffer = new(8);

        private GameObjectPool<AudioSource> m_SourcePool;
        private AudioSource m_SourcePrototype;
        private int m_NextHandleId;
        private long m_NextSequence;
        private bool m_IsDestroying;

        internal void Initialize(int initialCapacity)
        {
            var prototypeObject = new GameObject("AudioSourcePrototype");
            prototypeObject.transform.SetParent(transform, false);
            prototypeObject.SetActive(false);
            m_SourcePrototype = prototypeObject.AddComponent<AudioSource>();
            prototypeObject.AddComponent<AudioGainFilter>();
            ResetSource(m_SourcePrototype);
            m_SourcePool = new GameObjectPool<AudioSource>(
                m_SourcePrototype,
                transform,
                Mathf.Max(0, initialCapacity));
        }

        internal AudioHandle Play(string key, AudioPlayOptions options)
        {
            if (string.IsNullOrWhiteSpace(key))
                return default;

            options = NormalizeOptions(options);
            if (options.MaxConcurrent > 0 && !string.IsNullOrEmpty(options.ConcurrencyKey) &&
                CountConcurrentPlaybacks(options.GroupKey, options.ConcurrencyKey) >= options.MaxConcurrent)
                return default;
            if (m_Playbacks.Count >= HardVoiceLimit && !TryStealVoice(options.Priority))
                return default;

            var handle = AllocateHandle();
            var playback = new Playback
            {
                Handle = handle,
                Key = key,
                Options = options,
                Sequence = ++m_NextSequence,
            };
            m_Playbacks.Add(handle.Id, playback);
            RebalanceConcurrency(options.GroupKey, options.ConcurrencyKey);
            LoadAndPlay(playback).Forget();
            return handle;
        }

        internal void Stop(AudioHandle handle)
        {
            if (!TryGetPlayback(handle, out var playback))
                return;

            var groupKey = playback.Options.GroupKey;
            var concurrencyKey = playback.Options.ConcurrencyKey;
            m_Playbacks.Remove(handle.Id);
            CollectSource(playback.Source);
            playback.Source = null;
            playback.GainFilter = null;
            m_FreeHandleIds.Push(handle.Id);
            RebalanceConcurrency(groupKey, concurrencyKey);
        }

        internal void StopGroup(string groupKey)
        {
            groupKey ??= string.Empty;
            m_StopBuffer.Clear();
            foreach (var playback in m_Playbacks.Values)
            {
                if (playback.Options.GroupKey == groupKey)
                    m_StopBuffer.Add(playback.Handle);
            }
            for (int i = 0; i < m_StopBuffer.Count; i++)
                Stop(m_StopBuffer[i]);
            m_StopBuffer.Clear();
        }

        internal bool IsPlaying(AudioHandle handle)
        {
            return TryGetPlayback(handle, out _);
        }

        internal void SetPanStereo(AudioHandle handle, float panStereo)
        {
            if (!TryGetPlayback(handle, out var playback))
                return;

            playback.Options.PanStereo = Mathf.Clamp(panStereo, -1f, 1f);
            if (playback.Source != null)
                playback.Source.panStereo = playback.Options.PanStereo;
        }

        internal void SetVolume(AudioHandle handle, float volume)
        {
            if (!TryGetPlayback(handle, out var playback))
                return;

            playback.Options.Volume = NormalizeVolume(volume);
            ApplyPlaybackVolume(playback);
        }

        internal void FadeOut(AudioHandle handle, float durationSeconds)
        {
            if (!TryGetPlayback(handle, out var playback))
                return;
            if (float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds) ||
                durationSeconds <= 0f)
            {
                Stop(handle);
                return;
            }

            playback.FadeOutDuration = durationSeconds;
            playback.FadeOutElapsed = 0f;
            playback.FadeOutStartVolume = playback.Options.Volume;
            playback.IsFadingOut = true;
        }

        internal void Prewarm(int count)
        {
            if (m_SourcePool == null)
                return;
            count = Mathf.Max(0, count);
            var missing = count - m_SourcePool.Count;
            if (missing <= 0)
                return;

            m_PrewarmBuffer.Clear();
            var allocationCount = m_SourcePool.AvailableCount + missing;
            for (int i = 0; i < allocationCount; i++)
                m_PrewarmBuffer.Add(m_SourcePool.Alloc());
            for (int i = 0; i < m_PrewarmBuffer.Count; i++)
                m_SourcePool.Collect(m_PrewarmBuffer[i]);
            m_PrewarmBuffer.Clear();
        }

        private async UniTask LoadAndPlay(Playback playback)
        {
            var clip = await ResourceApi.LoadAudio(playback.Key);
            if (!TryGetPlayback(playback.Handle, out var current) || !ReferenceEquals(playback, current))
                return;
            if (clip == null)
            {
                Stop(playback.Handle);
                return;
            }

            var source = m_SourcePool.Alloc();
            playback.Source = source;
            playback.GainFilter = source.GetComponent<AudioGainFilter>() ??
                                  source.gameObject.AddComponent<AudioGainFilter>();
            source.clip = clip;
            ApplyPlaybackVolume(playback);
            source.pitch = playback.Options.Pitch;
            source.panStereo = playback.Options.PanStereo;
            source.loop = playback.Options.Loop;
            source.priority = playback.Options.Priority;
            source.Play();
        }

        private void Update()
        {
            m_StopBuffer.Clear();
            var deltaTime = Mathf.Max(0f, Time.unscaledDeltaTime);
            foreach (var playback in m_Playbacks.Values)
            {
                if (playback.IsFadingOut)
                {
                    playback.FadeOutElapsed += deltaTime;
                    var progress = Mathf.Clamp01(
                        playback.FadeOutElapsed / playback.FadeOutDuration);
                    playback.Options.Volume = Mathf.Lerp(
                        playback.FadeOutStartVolume, 0f, progress);
                    ApplyPlaybackVolume(playback);
                    if (progress >= 1f)
                    {
                        m_StopBuffer.Add(playback.Handle);
                        continue;
                    }
                }
                if (playback.Source != null && !playback.Options.Loop && !playback.Source.isPlaying)
                    m_StopBuffer.Add(playback.Handle);
            }
            for (int i = 0; i < m_StopBuffer.Count; i++)
                Stop(m_StopBuffer[i]);
            m_StopBuffer.Clear();
        }

        private void OnDestroy()
        {
            m_IsDestroying = true;
            m_StopBuffer.Clear();
            foreach (var playback in m_Playbacks.Values)
                m_StopBuffer.Add(playback.Handle);
            for (int i = 0; i < m_StopBuffer.Count; i++)
                Stop(m_StopBuffer[i]);
            m_StopBuffer.Clear();
            m_ConcurrencyBuffer.Clear();

            m_SourcePool?.Dispose();
            m_SourcePool = null;
            m_SourcePrototype = null;
            AudioManager.NotifyDriverDestroyed(this);
        }

        private bool TryStealVoice(int newPriority)
        {
            Playback candidate = null;
            foreach (var playback in m_Playbacks.Values)
            {
                if (candidate == null || playback.Options.Priority > candidate.Options.Priority ||
                    playback.Options.Priority == candidate.Options.Priority && playback.Sequence < candidate.Sequence)
                    candidate = playback;
            }

            if (candidate == null || candidate.Options.Priority <= newPriority)
                return false;
            Stop(candidate.Handle);
            return true;
        }

        private int CountConcurrentPlaybacks(string groupKey, string concurrencyKey)
        {
            var count = 0;
            foreach (var playback in m_Playbacks.Values)
            {
                if (playback.Options.GroupKey == groupKey &&
                    playback.Options.ConcurrencyKey == concurrencyKey)
                    count++;
            }
            return count;
        }

        private void RebalanceConcurrency(string groupKey, string concurrencyKey)
        {
            if (string.IsNullOrEmpty(concurrencyKey))
                return;

            m_ConcurrencyBuffer.Clear();
            foreach (var playback in m_Playbacks.Values)
            {
                if (playback.Options.GroupKey == groupKey &&
                    playback.Options.ConcurrencyKey == concurrencyKey)
                    m_ConcurrencyBuffer.Add(playback);
            }
            if (m_ConcurrencyBuffer.Count == 0)
                return;

            m_ConcurrencyBuffer.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
            var falloff = m_ConcurrencyBuffer[0].Options.ConcurrencyVolumeFalloff;
            if (falloff <= 0f)
            {
                for (var i = 0; i < m_ConcurrencyBuffer.Count; i++)
                {
                    m_ConcurrencyBuffer[i].ConcurrencyVolumeMultiplier = 1f;
                    ApplyPlaybackVolume(m_ConcurrencyBuffer[i]);
                }
                m_ConcurrencyBuffer.Clear();
                return;
            }

            var totalWeight = 0f;
            var weight = 1f;
            for (var i = 0; i < m_ConcurrencyBuffer.Count; i++)
            {
                totalWeight += weight;
                weight *= falloff;
            }

            weight = 1f;
            for (var i = 0; i < m_ConcurrencyBuffer.Count; i++)
            {
                var playback = m_ConcurrencyBuffer[i];
                playback.ConcurrencyVolumeMultiplier = weight / totalWeight;
                ApplyPlaybackVolume(playback);
                weight *= falloff;
            }
            m_ConcurrencyBuffer.Clear();
        }

        private static void ApplyPlaybackVolume(Playback playback)
        {
            if (playback.Source == null)
                return;

            playback.Source.volume = 1f;
            if (playback.GainFilter != null)
                playback.GainFilter.Gain =
                    playback.Options.Volume * playback.ConcurrencyVolumeMultiplier;
        }

        private AudioHandle AllocateHandle()
        {
            var id = m_FreeHandleIds.Count > 0 ? m_FreeHandleIds.Pop() : ++m_NextHandleId;
            var version = m_HandleVersions.TryGetValue(id, out var previous)
                ? unchecked(previous + 1)
                : 1;
            if (version <= 0)
                version = 1;
            m_HandleVersions[id] = version;
            return new AudioHandle(id, version);
        }

        private bool TryGetPlayback(AudioHandle handle, out Playback playback)
        {
            playback = null;
            if (!handle.IsValid || !m_Playbacks.TryGetValue(handle.Id, out playback))
                return false;
            return playback.Handle.Version == handle.Version;
        }

        private void CollectSource(AudioSource source)
        {
            if (source == null)
                return;
            ResetSource(source);
            if (!m_IsDestroying)
                m_SourcePool?.Collect(source);
        }

        private static AudioPlayOptions NormalizeOptions(AudioPlayOptions options)
        {
            if (options.Pitch <= 0f)
                options.Pitch = 1f;
            options.Volume = NormalizeVolume(options.Volume);
            options.Pitch = Mathf.Clamp(options.Pitch, -3f, 3f);
            options.PanStereo = Mathf.Clamp(options.PanStereo, -1f, 1f);
            options.Priority = Mathf.Clamp(options.Priority, 0, 256);
            options.GroupKey ??= string.Empty;
            options.ConcurrencyKey ??= string.Empty;
            options.MaxConcurrent = Mathf.Max(0, options.MaxConcurrent);
            options.ConcurrencyVolumeFalloff = Mathf.Clamp01(options.ConcurrencyVolumeFalloff);
            return options;
        }

        private static float NormalizeVolume(float volume)
        {
            return float.IsNaN(volume) || float.IsInfinity(volume)
                ? 0f
                : Mathf.Max(0f, volume);
        }

        private static void ResetSource(AudioSource source)
        {
            source.Stop();
            source.playOnAwake = false;
            source.clip = null;
            source.loop = false;
            source.volume = 1f;
            var gainFilter = source.GetComponent<AudioGainFilter>();
            if (gainFilter != null)
                gainFilter.Gain = 1f;
            source.pitch = 1f;
            source.panStereo = 0f;
            source.spatialBlend = 0f;
            source.priority = 128;
        }
    }

    [DisallowMultipleComponent]
    internal sealed class AudioGainFilter : MonoBehaviour
    {
        internal volatile float Gain = 1f;

        private void OnAudioFilterRead(float[] data, int channels)
        {
            var gain = Gain;
            if (gain == 1f)
                return;

            for (var i = 0; i < data.Length; i++)
                data[i] *= gain;
        }
    }
}
