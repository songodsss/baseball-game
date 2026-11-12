namespace BaseballGame
{
    /// <summary>The button sound every UiKit button plays. All sound lives in <see cref="GameAudio"/>.</summary>
    public static class UiSound
    {
        /// <summary>The old on/off switch for effects; GameAudio reads it once to carry the choice over.</summary>
        public const string MutePref = "sound_muted";

        /// <summary>A button press.</summary>
        public static void Tap() => GameAudio.Sfx(GameAudio.Tap, 0.7f);
    }
}
