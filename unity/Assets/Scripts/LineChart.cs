using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BaseballGame
{
    /// <summary>
    /// A small line chart drawn straight into the UI mesh, so it stretches with its layout slot.
    /// Each series is a list of values over the same x positions; labels are added as Text children.
    /// </summary>
    public class LineChart : MaskableGraphic
    {
        public class Series
        {
            public string Name;
            public Color Color;
            public List<float> Values = new List<float>();
        }

        public readonly List<Series> Lines = new List<Series>();
        public float Min, Max = 1;
        public int GridLines = 4;
        const float LineWidth = 5f, Dot = 7f;
        static readonly Color Grid = new Color(1, 1, 1, 0.08f);

        /// <summary>A chart in a layout slot, with x labels under it and a legend above it.</summary>
        public static LineChart Create(Transform parent, float height, float min, float max, IList<string> xLabels, params Series[] lines)
        {
            var box = UiKit.Empty(parent, "Chart");
            UiKit.Height(box, height);
            UiKit.Column(box, 0, 4);

            var legend = UiKit.Empty(box, "Legend");
            UiKit.Height(legend, 40);
            var legendRow = UiKit.Row(legend, 16);
            legendRow.childForceExpandWidth = false;
            foreach (var s in lines)
                UiKit.Width(UiKit.Label(legend, $"<color=#{ColorUtility.ToHtmlStringRGB(s.Color)}>━</color> {s.Name}", 24, TextAnchor.MiddleLeft, UiKit.Muted), 160);

            var area = UiKit.Empty(box, "Plot");
            UiKit.Height(area, 0, 1);
            var chart = area.gameObject.AddComponent<LineChart>();
            chart.raycastTarget = false;
            chart.Min = min;
            chart.Max = max;
            chart.Lines.AddRange(lines);
            for (int g = 0; g <= chart.GridLines; g++)
            {
                float v = min + (max - min) * g / chart.GridLines;
                var label = UiKit.Label(area, v.ToString(max - min < 3 ? "0.00" : "0"), 20, TextAnchor.MiddleLeft, UiKit.Muted);
                var rt = label.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, (float)g / chart.GridLines);
                rt.pivot = new Vector2(0, 0.5f);
                rt.sizeDelta = new Vector2(70, 30);
            }

            var xs = UiKit.Empty(box, "X");
            UiKit.Height(xs, 30);
            for (int i = 0; i < xLabels.Count; i++)
            {
                var label = UiKit.Label(xs, xLabels[i], 20, TextAnchor.MiddleCenter, UiKit.Muted);
                var rt = label.rectTransform;
                float x = XAt(i, xLabels.Count);
                rt.anchorMin = new Vector2(x, 0);
                rt.anchorMax = new Vector2(x, 1);
                rt.sizeDelta = new Vector2(90, 0);
            }
            return chart;
        }

        const float LeftPad = 0.08f;
        static float XAt(int i, int count) => count <= 1 ? 0.5f : LeftPad + (1 - LeftPad - 0.02f) * i / (count - 1);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            for (int g = 0; g <= GridLines; g++)
            {
                float y = r.yMin + r.height * g / GridLines;
                Segment(vh, new Vector2(r.xMin + r.width * LeftPad, y), new Vector2(r.xMax, y), 2, Grid);
            }
            foreach (var s in Lines)
            {
                Vector2? prev = null;
                for (int i = 0; i < s.Values.Count; i++)
                {
                    float t = Mathf.InverseLerp(Min, Max, s.Values[i]);
                    var p = new Vector2(r.xMin + r.width * XAt(i, s.Values.Count), r.yMin + r.height * t);
                    if (prev.HasValue) Segment(vh, prev.Value, p, LineWidth, s.Color);
                    Square(vh, p, Dot, s.Color);
                    prev = p;
                }
            }
        }

        static void Segment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            var dir = (b - a).normalized;
            var n = new Vector2(-dir.y, dir.x) * width / 2;
            Quad(vh, a - n, a + n, b + n, b - n, color);
        }

        static void Square(VertexHelper vh, Vector2 c, float half, Color color) =>
            Quad(vh, c + new Vector2(-half, -half), c + new Vector2(-half, half), c + new Vector2(half, half), c + new Vector2(half, -half), color);

        static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert;
            v.color = color;
            v.position = a; vh.AddVert(v);
            v.position = b; vh.AddVert(v);
            v.position = c; vh.AddVert(v);
            v.position = d; vh.AddVert(v);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
