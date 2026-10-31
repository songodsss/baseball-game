using UnityEngine;

namespace BaseballGame
{
    /// <summary>Keeps a full-screen RectTransform inside the phone's safe area, clear of notches and rounded corners.</summary>
    public class SafeArea : MonoBehaviour
    {
        Rect _applied;
        Vector2Int _screen;

        void Awake() => Apply();

        void Update()
        {
            if (Screen.safeArea != _applied || Screen.width != _screen.x || Screen.height != _screen.y) Apply();
        }

        void Apply()
        {
            var area = Screen.safeArea;
            _applied = area;
            _screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width == 0 || Screen.height == 0) return;

            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rt.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
