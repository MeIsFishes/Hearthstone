namespace Hearthstone
{
    // Compatibility entry for existing automation. It no longer owns a separate card visual.
    public static class PreparationCardItemUiBuilder
    {
        public static void Build()
        {
            BattleCardItemUiBuilder.Build();
        }
    }
}
