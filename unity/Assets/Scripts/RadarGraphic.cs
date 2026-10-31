using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BaseballGame
{
    /// <summary>
    /// A radar (spider) chart drawn as a UI mesh: a filled polygon for the values, a fainter one for
    /// the ceiling behind it, and the grid rings. Values are 0..1, one per axis, clockwise from the top.
    /// </summary>
    public class RadarGraphic : MaskableGraphic
    {
        public List<float> Values = new List<float>();
        public List<float> Ceiling = new List<float>();
        public Color FillColor = new Color(0.25f, 0.55f, 0.95f, 0.75f);
        public Color CeilingColor = new Color(1f, 0.82f, 0.3f, 0.25f);
        public Color GridColor = new Color(1f, 1f, 1f, 0.12f);
        const int Rings = 4;
        const float Line = 2f;

        /// <summary>Where axis i points, for placing labels; radius 1 is the chart's edge.</summary>
        public Vector2 AxisPoint(int i, float radius)
        {
            int n = Mathf.Max(3, Values.Count);
            float angle = Mathf.PI / 2 - i * 2 * Mathf.PI / n;
            float r = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) / 2 * radius;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r + rectTransform.rect.center;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            int n = Values.Count;
            if (n < 3) return;
            for (int ring = 1; ring <= Rings; ring++) Outline(vh, n, ring / (float)Rings, GridColor);
            for (int i = 0; i < n; i++) Segment(vh, AxisPoint(i, 0), AxisPoint(i, 1), GridColor);
            if (Ceiling.Count == n) Polygon(vh, Ceiling, CeilingColor);
            Polygon(vh, Values, FillColor);
        }

        void Polygon(VertexHelper vh, List<float> values, Color c)
        {
            int start = vh.currentVertCount;
            vh.AddVert(rectTransform.rect.center, c, Vector2.zero);
            for (int i = 0; i < values.Count; i++) vh.AddVert(AxisPoint(i, Mathf.Clamp01(values[i])), c, Vector2.zero);
            for (int i = 0; i < values.Count; i++)
                vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % values.Count);
        }

        void Outline(VertexHelper vh, int n, float radius, Color c)
        {
            for (int i = 0; i < n; i++) Segment(vh, AxisPoint(i, radius), AxisPoint((i + 1) % n, radius), c);
        }

        void Segment(VertexHelper vh, Vector2 a, Vector2 b, Color c)
        {
            var normal = new Vector2(a.y - b.y, b.x - a.x).normalized * Line / 2;
            int start = vh.currentVertCount;
            vh.AddVert(a - normal, c, Vector2.zero);
            vh.AddVert(a + normal, c, Vector2.zero);
            vh.AddVert(b + normal, c, Vector2.zero);
            vh.AddVert(b - normal, c, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
