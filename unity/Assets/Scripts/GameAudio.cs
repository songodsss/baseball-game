using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BaseballGame
{
    /// <summary>
    /// Background music, the crowd under a match, and short sound effects.
    /// Clips come from Resources/Sounds/ (effects) and Resources/Sounds/Music/ (songs); replacing a
    /// file with one of the same name swaps the sound, and a missing clip is simply silent.
    /// Music and effects each have their own volume step in the settings (0 = off).
    /// </summary>
    public static class GameAudio
    {
        // ---------- Song and effect names (file names without .ogg) ----------

        public const string MusicHome = "home", MusicTraining = "training", MusicMatch = "match";

        public const string Tap = "tap", Back = "back", Confirm = "confirm", Error = "error", Popup = "popup",
            Exp = "exp", LevelUp = "level_up", Coin = "coin", Achievement = "achievement", Event = "event",
            SeasonEnd = "season_end", BatHit = "bat_hit", BatHomeRun = "bat_homerun", Mitt = "mitt",
            CheerSmall = "cheer_small", CheerBig = "cheer_big", Win = "win", Lose = "lose", Crowd = "crowd_loop";

        // ---------- Volume settings ----------

        public const string MusicPref = "music_level", SfxPref = "sfx_level";
        /// <summary>The volume steps the settings screen offers: off, quiet, normal, loud.</summary>
        public static readonly string[] LevelNames = { "끄기", "작게", "보통", "크게" };
        static readonly float[] LevelVolumes = { 0f, 0.35f, 0.7f, 1f };
        const int DefaultLevel = 2;

        public static int MusicLevel
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(MusicPref, DefaultLevel), 0, LevelVolumes.Length - 1);
            set { PlayerPrefs.SetInt(MusicPref, value); PlayerPrefs.Save(); Speakers.ApplyVolumes(); }
        }

        public static int SfxLevel
        {
            get
            {
                // Before the volume steps there was only an on/off switch for effects.
                if (!PlayerPrefs.HasKey(SfxPref) && PlayerPrefs.GetInt(UiSound.MutePref, 0) == 1) return 0;
                return Mathf.Clamp(PlayerPrefs.GetInt(SfxPref, DefaultLevel), 0, LevelVolumes.Length - 1);
            }
            set { PlayerPrefs.SetInt(SfxPref, value); PlayerPrefs.Save(); Speakers.ApplyVolumes(); }
        }

        static float MusicVolume => LevelVolumes[MusicLevel];
        static float SfxVolume => LevelVolumes[SfxLevel];

        // ---------- Playback ----------

        /// <summary>The song playing now (or fading in), so a screen can put it back when it closes.</summary>
        public static string CurrentMusic => Speakers.Instance.Song;

        /// <summary>Starts a looping song, fading out the one before. The same song keeps playing.</summary>
        public static void PlayMusic(string name) => Speakers.Instance.Music(name);

        /// <summary>A looping crowd under the music during a match; null stops it.</summary>
        public static void PlayAmbience(string name) => Speakers.Instance.Ambience(name);

        /// <summary>A short effect. <paramref name="pitchJitter"/> varies the pitch a little so repeats don't sound copied.</summary>
        public static void Sfx(string name, float volume = 1f, float pitchJitter = 0f)
        {
            if (SfxVolume <= 0) return;
            var clip = Load("Sounds/" + name);
            if (clip == null) return;
            Speakers.Instance.OneShot(clip, volume * SfxVolume, pitchJitter);
        }

        static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        static AudioClip Load(string path)
        {
            if (!Cache.TryGetValue(path, out var clip))
            {
                clip = Resources.Load<AudioClip>(path);
                Cache[path] = clip;
            }
            return clip;
        }

        /// <summary>The one object that owns the speakers; it lives across screens.</summary>
        class Speakers : MonoBehaviour
        {
            static Speakers _instance;

            public static Speakers Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        var go = new GameObject("GameAudio");
                        DontDestroyOnLoad(go);
                        _instance = go.AddComponent<Speakers>();
                    }
                    return _instance;
                }
            }

            const float MusicGain = 0.55f, AmbienceGain = 0.45f, FadeSeconds = 0.8f;

            AudioSource _musicA, _musicB, _ambience;
            readonly List<AudioSource> _effects = new List<AudioSource>();
            string _song, _crowd;
            Coroutine _fade;

            void Awake()
            {
                _musicA = NewSource(true);
                _musicB = NewSource(true);
                _ambience = NewSource(true);
            }

            AudioSource NewSource(bool loop)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = loop;
                source.spatialBlend = 0;
                return source;
            }

            public static void ApplyVolumes()
            {
                if (_instance == null) return;
                _instance.Resume(_instance._musicA, MusicVolume, _instance._fade == null ? MusicGain : -1);
                _instance.Resume(_instance._ambience, SfxVolume, AmbienceGain);
            }

            /// <summary>Pauses a looping source at volume 0 and picks it back up when turned on again.</summary>
            void Resume(AudioSource source, float volume, float gain)
            {
                // A negative gain: a fade is running and sets the volume itself.
                if (gain >= 0) source.volume = volume * gain;
                if (volume <= 0) source.Pause();
                else if (source.clip != null && !source.isPlaying)
                {
                    if (source.time > 0) source.UnPause();
                    else source.Play();
                }
            }

            public string Song => _song;

            public void Music(string name)
            {
                if (name == _song) return;
                _song = name;
                var clip = name == null ? null : Load("Sounds/Music/" + name);
                if (_fade != null) StopCoroutine(_fade);
                _fade = StartCoroutine(CrossFade(clip));
            }

            IEnumerator CrossFade(AudioClip next)
            {
                // _musicA is always the song we end on.
                (_musicA, _musicB) = (_musicB, _musicA);
                float fromVolume = _musicB.volume;
                _musicA.clip = next;
                _musicA.volume = 0;
                if (next != null && MusicVolume > 0) _musicA.Play();
                for (float t = 0; t < 1; t += Time.unscaledDeltaTime / FadeSeconds)
                {
                    _musicB.volume = fromVolume * (1 - t);
                    _musicA.volume = MusicVolume * MusicGain * t;
                    yield return null;
                }
                _musicB.Stop();
                _musicB.clip = null;
                _musicA.volume = MusicVolume * MusicGain;
                _fade = null;
            }

            public void Ambience(string name)
            {
                if (name == _crowd) return;
                _crowd = name;
                _ambience.Stop();
                _ambience.clip = name == null ? null : Load("Sounds/" + name);
                if (_ambience.clip == null) return;
                _ambience.volume = SfxVolume * AmbienceGain;
                if (SfxVolume > 0) _ambience.Play();
            }

            public void OneShot(AudioClip clip, float volume, float pitchJitter)
            {
                // A few voices, so a crack and a cheer can overlap without cutting each other off.
                AudioSource free = null;
                foreach (var s in _effects)
                    if (!s.isPlaying) { free = s; break; }
                if (free == null)
                {
                    if (_effects.Count < 8) { free = NewSource(false); _effects.Add(free); }
                    else free = _effects[0];
                }
                free.pitch = 1f + (pitchJitter > 0 ? Random.Range(-pitchJitter, pitchJitter) : 0f);
                free.clip = clip;
                free.volume = Mathf.Clamp01(volume);
                free.Play();
            }
        }
    }
}
