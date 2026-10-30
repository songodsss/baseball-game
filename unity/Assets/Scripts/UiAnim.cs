using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BaseballGame
{
    /// <summary>Small code-driven UI animations: fades, pop-ins and pulses. They use unscaled time.</summary>
    public class UiAnim : MonoBehaviour
    {
        static UiAnim On(Component c)
        {
            var anim = c.GetComponent<UiAnim>();
            return anim != null ? anim : c.gameObject.AddComponent<UiAnim>();
        }

        /// <summary>Fades a whole overlay in from transparent.</summary>
        public static void FadeIn(Component c, float seconds = 0.18f)
        {
            var group = c.GetComponent<CanvasGroup>();
            if (group == null) group = c.gameObject.AddComponent<CanvasGroup>();
            On(c).Run(Fade(group, seconds));
        }

        /// <summary>Grows a card from slightly small with a little overshoot.</summary>
        public static void PopIn(Component c, float seconds = 0.25f) => On(c).Run(Pop(c.transform, seconds));

        /// <summary>A quick swell and settle, to draw the eye to a value that changed.</summary>
        public static void Pulse(Component c, float scale = 1.25f, float seconds = 0.35f) =>
            On(c).Run(Swell(c.transform, scale, seconds));

        void Run(IEnumerator routine)
        {
            if (!isActiveAndEnabled) return;
            StopAllCoroutines();
            StartCoroutine(routine);
        }

        static IEnumerator Fade(CanvasGroup group, float seconds)
        {
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = t / seconds;
                yield return null;
            }
            group.alpha = 1;
        }

        static IEnumerator Pop(Transform t, float seconds)
        {
            for (float time = 0; time < seconds; time += Time.unscaledDeltaTime)
            {
                t.localScale = Vector3.one * Mathf.LerpUnclamped(0.88f, 1f, EaseOutBack(time / seconds));
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        static IEnumerator Swell(Transform t, float scale, float seconds)
        {
            for (float time = 0; time < seconds; time += Time.unscaledDeltaTime)
            {
                float k = Mathf.Sin(time / seconds * Mathf.PI);
                t.localScale = Vector3.one * Mathf.Lerp(1f, scale, k);
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
        }
    }

    /// <summary>Shrinks a button a little while it's held down, so taps feel physical.</summary>
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float Pressed = 0.94f;
        float _target = 1f;

        public void OnPointerDown(PointerEventData e)
        {
            var b = GetComponent<UnityEngine.UI.Selectable>();
            if (b == null || b.interactable) _target = Pressed;
        }

        public void OnPointerUp(PointerEventData e) => _target = 1f;
        public void OnPointerExit(PointerEventData e) => _target = 1f;

        void OnDisable()
        {
            _target = 1f;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            float s = Mathf.MoveTowards(transform.localScale.x, _target, Time.unscaledDeltaTime * 2.5f);
            if (!Mathf.Approximately(s, transform.localScale.x)) transform.localScale = Vector3.one * s;
        }
    }
}
