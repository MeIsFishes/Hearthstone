namespace BbxCommon
{
    public struct AudioPlayOptions
    {
        public float Volume;
        public float Pitch;
        public float PanStereo;
        public bool Loop;
        public int Priority;
        public string GroupKey;
        public string ConcurrencyKey;
        public int MaxConcurrent;
        public float ConcurrencyVolumeFalloff;

        public static AudioPlayOptions Default => new AudioPlayOptions
        {
            Volume = 1f,
            Pitch = 1f,
            PanStereo = 0f,
            Loop = false,
            Priority = 128,
            GroupKey = string.Empty,
            ConcurrencyKey = string.Empty,
            MaxConcurrent = 0,
            ConcurrencyVolumeFalloff = 0f,
        };
    }
}
