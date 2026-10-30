using UnityEngine;

namespace BaseballGame
{
    /// <summary>
    /// Short UI sound effects from Resources/Sounds/ (Kenney UI Pack, CC0). Missing clips are silent,
    /// and the settings screen can turn them off.
    /// </summary>
    public static class UiSound
    {
        public const string MutePref = "sound_muted";
        static AudioSource _source;

        public static bool Muted
        {
            get => PlayerPrefs.GetInt(MutePref, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(MutePref, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>A button press.</summary>
        public static void Tap() => Play("tap", 0.7f);

        static void Play(string name, float volume)
        {
            if (Muted) return;
            var clip = Resources.Load<AudioClip>("Sounds/" + name);
            if (clip == null) return;
            if (_source == null)
            {
                var go = new GameObject("UiSound");
                Object.DontDestroyOnLoad(go);
                _source = go.AddComponent<AudioSource>();
                _source.playOnAwake = false;
            }
            _source.PlayOneShot(clip, volume);
        }
    }
}
