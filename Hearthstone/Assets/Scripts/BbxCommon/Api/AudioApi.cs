namespace BbxCommon
{
    public static class AudioApi
    {
        private const float DefaultBgmVolume = 0.7f;

        public static AudioHandle Play(string key)
        {
            return Internal.AudioManager.Play(key, AudioPlayOptions.Default);
        }

        public static AudioHandle Play(string key, float volume)
        {
            var options = AudioPlayOptions.Default;
            options.Volume = volume;
            return Internal.AudioManager.Play(key, options);
        }

        public static AudioHandle Play(string key, AudioPlayOptions options)
        {
            return Internal.AudioManager.Play(key, options);
        }

        public static AudioHandle SetBgm(
            string key,
            float transitionDurationSeconds = 0f,
            bool loop = true)
        {
            var options = AudioPlayOptions.Default;
            options.Volume = DefaultBgmVolume;
            options.Loop = loop;
            return Internal.AudioManager.SetBgm(
                key,
                options,
                transitionDurationSeconds);
        }

        public static void Stop(AudioHandle handle)
        {
            Internal.AudioManager.Stop(handle);
        }

        public static void StopGroup(string groupKey)
        {
            Internal.AudioManager.StopGroup(groupKey);
        }

        public static bool IsPlaying(AudioHandle handle)
        {
            return Internal.AudioManager.IsPlaying(handle);
        }

        public static void SetPanStereo(AudioHandle handle, float panStereo)
        {
            Internal.AudioManager.SetPanStereo(handle, panStereo);
        }

        public static void SetVolume(AudioHandle handle, float volume)
        {
            Internal.AudioManager.SetVolume(handle, volume);
        }

        public static void FadeOut(AudioHandle handle, float durationSeconds)
        {
            Internal.AudioManager.FadeOut(handle, durationSeconds);
        }

        public static void Prewarm(int count)
        {
            Internal.AudioManager.Prewarm(count);
        }
    }
}
