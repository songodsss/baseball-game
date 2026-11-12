using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if HAS_LIT_MOTION
using LitMotion;
#endif
#if HAS_UI_EFFECT
using Coffee.UIEffects;
#endif
#if HAS_UI_PARTICLE
using Coffee.UIExtensions;
#endif

namespace BaseballGame
{
    /// <summary>
    /// The game's "hand feel" in one place: confetti over the UI, a shine sweeping across a card,
    /// springy scale/shake/count-up motion, phone vibration and the share sheet.
    ///
    /// Each one uses a free MIT package when it is installed (Packages/manifest.json):
    ///   UIEffect (mob-sakai)            → Shine
    ///   ParticleEffectForUGUI (mob-sakai) → Confetti / Sparkle
    ///   LitMotion (annulusgames)        → SlamIn / Punch / Shake / CountUp
    ///   HapticFeedback (CandyCoded)     → Vibrate
    ///   NativeShare (yasirkula)         → ShareScreen
    /// The defines come from versionDefines in BaseballGame.asmdef, so if a package is missing the game
    /// still compiles and falls back to a plain version (or nothing) instead of breaking.
    /// </summary>
    public static class Juice
    {
        // ---------- Vibration ----------

        public enum Buzz { Light, Medium, Heavy }

        const string HapticsKey = "haptics_on";

        public static bool HapticsOn
        {
            get => PlayerPrefs.GetInt(HapticsKey, 1) == 1;
            set { PlayerPrefs.SetInt(HapticsKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static void Vibrate(Buzz buzz)
        {
            if (!HapticsOn) return;
#if HAS_HAPTICS
            switch (buzz)
            {
                case Buzz.Light: CandyCoded.HapticFeedback.HapticFeedback.LightFeedback(); break;
                case Buzz.Medium: CandyCoded.HapticFeedback.HapticFeedback.MediumFeedback(); break;
                default: CandyCoded.HapticFeedback.HapticFeedback.HeavyFeedback(); break;
            }
#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (buzz == Buzz.Heavy) Handheld.Vibrate();
#endif
        }

        /// <summary>Two or three quick buzzes in a row, for the biggest moments (SSR, 끝내기).</summary>
        public static void VibrateRoll(MonoBehaviour host, int times = 3)
        {
            if (!HapticsOn || host == null || !host.isActiveAndEnabled) return;
            host.StartCoroutine(Roll(times));
        }

        static IEnumerator Roll(int times)
        {
            for (int i = 0; i < times; i++)
            {
                Vibrate(i == times - 1 ? Buzz.Heavy : Buzz.Medium);
                yield return new WaitForSecondsRealtime(0.09f);
            }
        }

        // ---------- Motion ----------

        /// <summary>Drops a big thing onto the screen: starts large and transparent, lands at full size with a bounce.</summary>
        public static void SlamIn(Component c, float from = 2.2f, float seconds = 0.4f, float delay = 0f)
        {
            var t = c.transform;
            var group = c.GetComponent<CanvasGroup>();
            if (group == null) group = c.gameObject.AddComponent<CanvasGroup>();
            t.localScale = Vector3.one * from;
            group.alpha = 0;
#if HAS_LIT_MOTION
            LMotion.Create(0f, 1f, seconds).WithEase(Ease.OutBack).WithDelay(delay)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(k =>
                {
                    if (t == null) return;
                    t.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1f, k);
                    group.alpha = Mathf.Clamp01(k * 2.5f);
                })
                .AddTo(c.gameObject);
#else
            Runner.Of(c).Go(SlamRoutine(t, group, from, seconds, delay));
#endif
        }

        /// <summary>A short springy kick in size, to say "this just changed".</summary>
        public static void Punch(Component c, float strength = 0.25f, float seconds = 0.45f)
        {
            var t = c.transform;
#if HAS_LIT_MOTION
            LMotion.Punch.Create(1f, strength, seconds).WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(s => { if (t != null) t.localScale = Vector3.one * s; })
                .AddTo(c.gameObject);
#else
            UiAnim.Pulse(c, 1f + strength, seconds);
#endif
        }

        /// <summary>Rattles a panel sideways, like a camera shake on a home run.</summary>
        public static void Shake(RectTransform rt, float strength = 18f, float seconds = 0.4f)
        {
            Vector2 home = rt.anchoredPosition;
#if HAS_LIT_MOTION
            LMotion.Shake.Create(Vector2.zero, new Vector2(strength, strength * 0.6f), seconds)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() => { if (rt != null) rt.anchoredPosition = home; })
                .Bind(o => { if (rt != null) rt.anchoredPosition = home + o; })
                .AddTo(rt.gameObject);
#else
            Runner.Of(rt).Go(ShakeRoutine(rt, home, strength, seconds));
#endif
        }

        /// <summary>Counts a number up on a label (coins, 홈런 수), formatting each step.</summary>
        public static void CountUp(Text label, int from, int to, float seconds, Func<int, string> format)
        {
#if HAS_LIT_MOTION
            LMotion.Create(from, to, seconds).WithEase(Ease.OutCubic).WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(v => { if (label != null) label.text = format(v); })
                .AddTo(label.gameObject);
#else
            Runner.Of(label).Go(CountRoutine(label, from, to, seconds, format));
#endif
        }

        // ---------- Shine ----------

        /// <summary>
        /// A band of light sweeps across a card or a big letter, again and again (or once).
        /// Used on SR/SSR results, a new grade and the trophies.
        /// </summary>
        public static void Shine(Graphic g, Color color, float seconds = 1.1f, bool loop = true, float pause = 0.7f)
        {
            if (g == null) return;
#if HAS_UI_EFFECT
            var effect = g.GetComponent<UIEffect>();
            if (effect == null) effect = g.gameObject.AddComponent<UIEffect>();
            effect.transitionFilter = TransitionFilter.Shiny;
            effect.transitionWidth = 0.22f;
            effect.transitionSoftness = 1f;
            effect.transitionColorFilter = ColorFilter.Additive;
            effect.transitionColor = color;
            effect.transitionRate = 0f;
            Runner.Of(g).Go(ShineRoutine(effect, seconds, loop, pause));
#endif
        }

#if HAS_UI_EFFECT
        static IEnumerator ShineRoutine(UIEffect effect, float seconds, bool loop, float pause)
        {
            do
            {
                for (float t = 0; t < seconds && effect != null; t += Time.unscaledDeltaTime)
                {
                    effect.transitionRate = t / seconds;
                    yield return null;
                }
                if (effect == null) yield break;
                effect.transitionRate = 0f;
                yield return new WaitForSecondsRealtime(pause);
            } while (loop && effect != null);
        }
#endif

        // ---------- Particles ----------

        public static readonly Color[] GoldColors = { new Color(1f, 0.82f, 0.3f), new Color(1f, 0.93f, 0.6f), Color.white };
        public static readonly Color[] PartyColors =
        {
            new Color(1f, 0.82f, 0.3f), new Color(0.37f, 0.66f, 1f), new Color(0.88f, 0.28f, 0.36f),
            new Color(0.49f, 0.85f, 0.57f), new Color(0.77f, 0.55f, 1f), Color.white,
        };

        /// <summary>
        /// Paper confetti bursting up from a point of the screen (0..1 coordinates) and falling.
        /// Removes itself after a few seconds; needs ParticleEffectForUGUI, otherwise does nothing.
        /// </summary>
        public static void Confetti(Transform canvas, Vector2 at, Color[] colors, int count = 80, float power = 1f)
        {
#if HAS_UI_PARTICLE
            var system = Emitter(canvas, "Confetti", at, Square, 3.5f);
            if (system == null) return;
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 3.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(110f * power, 190f * power);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(1.6f, 2.6f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(0.9f, 1.4f);
            main.startSizeZ = 1f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2);
            main.gravityModifier = 22f;
            main.startColor = RandomColors(colors);
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 32f;
            shape.radius = 1f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // fire upward on screen
            var rot = system.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            Fade(system, 0.75f);
            Fire(system, count);
#endif
        }

        /// <summary>Soft glowing sparks spreading out from a point and fading (a new grade, a trophy).</summary>
        public static void Sparkle(Transform canvas, Vector2 at, Color[] colors, int count = 40, float radius = 1f)
        {
#if HAS_UI_PARTICLE
            var system = Emitter(canvas, "Sparkle", at, Dot, 2f);
            if (system == null) return;
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(20f * radius, 70f * radius);
            main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 3.4f);
            main.gravityModifier = 0f;
            main.startColor = RandomColors(colors);
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 4f * radius;
            shape.rotation = Vector3.zero;
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.4f), new Keyframe(0.2f, 1f), new Keyframe(1, 0f)));
            Fade(system, 0.5f);
            Fire(system, count);
#endif
        }

#if HAS_UI_PARTICLE
        static Texture2D _square, _dot;
        static Material _squareMat, _dotMat;

        static Material Square => _squareMat != null ? _squareMat : (_squareMat = MakeMaterial(_square = MakeTexture(false)));
        static Material Dot => _dotMat != null ? _dotMat : (_dotMat = MakeMaterial(_dot = MakeTexture(true)));

        static Texture2D MakeTexture(bool round)
        {
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float a;
                if (round)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    a = Mathf.Clamp01(1f - d);
                    a *= a;
                }
                else a = 1f;
                pixels[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        static Material MakeMaterial(Texture2D tex)
        {
            var shader = Shader.Find("UI/Default");
            return shader == null ? null : new Material(shader) { mainTexture = tex };
        }

        static ParticleSystem Emitter(Transform canvas, string name, Vector2 at, Material material, float life)
        {
            if (canvas == null || material == null) return null;
            var holder = new GameObject(name, typeof(RectTransform));
            holder.transform.SetParent(canvas, false);
            var rt = (RectTransform)holder.transform;
            rt.anchorMin = rt.anchorMax = at;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            holder.transform.SetAsLastSibling();
            var le = holder.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            var child = new GameObject("Particles");
            child.transform.SetParent(holder.transform, false);
            var system = child.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.2f;
            main.maxParticles = 300;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = system.emission;
            emission.rateOverTime = 0;
            var renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            var ui = holder.AddComponent<UIParticle>();
            ui.scale = 10f;
            ui.RefreshParticles();
            UnityEngine.Object.Destroy(holder, life);
            return system;
        }

        static ParticleSystem.MinMaxGradient RandomColors(Color[] colors)
        {
            var g = new Gradient { mode = GradientMode.Fixed };
            int n = Mathf.Clamp(colors.Length, 1, 8);
            var keys = new GradientColorKey[n];
            for (int i = 0; i < n; i++) keys[i] = new GradientColorKey(colors[i], (i + 1f) / n);
            g.SetKeys(keys, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            return new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
        }

        static void Fade(ParticleSystem system, float from)
        {
            var col = system.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, from), new GradientAlphaKey(0, 1) });
            col.color = g;
        }

        static void Fire(ParticleSystem system, int count)
        {
            var emission = system.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            system.Play(true);
        }
#endif

        // ---------- Share ----------

        /// <summary>
        /// Takes a screenshot of what's on screen right now and opens the phone's share sheet
        /// (카카오톡, 인스타그램 …) with a line of text. In the editor it just saves the picture.
        /// </summary>
        public static void ShareScreen(MonoBehaviour host, string text)
        {
            if (host == null || !host.isActiveAndEnabled) return;
            host.StartCoroutine(Capture(text));
        }

        static IEnumerator Capture(string text)
        {
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            ShareTexture(shot, text);
            UnityEngine.Object.Destroy(shot);
        }

        /// <summary>True when the phone can open a share sheet (NativeShare on Android/iOS).</summary>
        public static bool CanShare
        {
            get
            {
#if HAS_NATIVE_SHARE && (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Opens the share sheet with a picture and a line of text. Where there is no share sheet
        /// (editor, PC) it saves the picture in the app's folder instead. Returns where it went.
        /// </summary>
        public static string ShareTexture(Texture2D picture, string text)
        {
#if HAS_NATIVE_SHARE && (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            new NativeShare().AddFile(picture, "baseball_moment.png").SetSubject("프로야구 선수 키우기").SetText(text).Share();
            return "공유 창을 열었어요";
#else
            try
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, "baseball_moment.png");
                System.IO.File.WriteAllBytes(path, picture.EncodeToPNG());
                Debug.Log($"[Share] {text}\n저장: {path}");
                return $"저장했어요 (공유는 폰에서): {path}";
            }
            catch (Exception e)
            {
                return "저장하지 못했어요: " + e.Message;
            }
#endif
        }

        /// <summary>A small "공유" button tucked into a corner of an overlay; hides itself while the picture is taken.</summary>
        public static Button ShareButton(RectTransform parent, MonoBehaviour host, Func<string> text)
        {
            Button b = null;
            b = UiKit.Button(parent, "공유", () =>
            {
                GameAudio.Sfx(GameAudio.Popup, 0.6f);
                host.StartCoroutine(HideWhileSharing(b, text()));
            }, new Color(0.25f, 0.55f, 0.95f));
            return b;
        }

        static IEnumerator HideWhileSharing(Button b, string text)
        {
            if (b != null) b.gameObject.SetActive(false);
            yield return null;
            yield return Capture(text);
            if (b != null) b.gameObject.SetActive(true);
        }

        // ---------- Fallbacks (no LitMotion) ----------

        /// <summary>Holds coroutines for the fallbacks and the shine loop on the object itself, so they end with it.</summary>
        class Runner : MonoBehaviour
        {
            public static Runner Of(Component c)
            {
                var r = c.GetComponent<Runner>();
                return r != null ? r : c.gameObject.AddComponent<Runner>();
            }

            public void Go(IEnumerator routine)
            {
                if (isActiveAndEnabled) StartCoroutine(routine);
            }
        }

#if !HAS_LIT_MOTION
        static IEnumerator SlamRoutine(Transform t, CanvasGroup group, float from, float seconds, float delay)
        {
            for (float time = 0; time < delay; time += Time.unscaledDeltaTime) yield return null;
            for (float time = 0; time < seconds && t != null; time += Time.unscaledDeltaTime)
            {
                float k = time / seconds;
                float back = 1 + 2.70158f * Mathf.Pow(k - 1, 3) + 1.70158f * Mathf.Pow(k - 1, 2);
                t.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1f, back);
                group.alpha = Mathf.Clamp01(k * 2.5f);
                yield return null;
            }
            if (t == null) yield break;
            t.localScale = Vector3.one;
            group.alpha = 1;
        }

        static IEnumerator ShakeRoutine(RectTransform rt, Vector2 home, float strength, float seconds)
        {
            for (float t = 0; t < seconds && rt != null; t += Time.unscaledDeltaTime)
            {
                float fall = 1 - t / seconds;
                rt.anchoredPosition = home + new Vector2(UnityEngine.Random.Range(-1f, 1f) * strength, UnityEngine.Random.Range(-1f, 1f) * strength * 0.6f) * fall;
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = home;
        }

        static IEnumerator CountRoutine(Text label, int from, int to, float seconds, Func<int, string> format)
        {
            for (float t = 0; t < seconds && label != null; t += Time.unscaledDeltaTime)
            {
                float k = 1 - Mathf.Pow(1 - t / seconds, 3);
                label.text = format(Mathf.RoundToInt(Mathf.Lerp(from, to, k)));
                yield return null;
            }
            if (label != null) label.text = format(to);
        }
#endif
    }
}
