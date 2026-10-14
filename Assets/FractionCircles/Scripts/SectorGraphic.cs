using UnityEngine;
using UnityEngine.UI;

namespace FractionCircles
{
    /// <summary>
    /// Draws one 1/n wedge of a circle (or the whole circle when n == 1) with a radial gradient and outline.
    /// The circle's centre is the rect's centre; the wedge runs clockwise from <see cref="startAngle"/>.
    /// Only the wedge itself receives clicks.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class SectorGraphic : MaskableGraphic, ICanvasRaycastFilter
    {
        int denominator = 1;
        Color fill = Theme.ClassicRed;
        bool gradient = true;
        bool outline = true;
        bool autoColor;

        /// <summary>Local angle (degrees, counter-clockwise from +X) of the wedge's leading edge.</summary>
        public float startAngle = 90f;
        public float outlineWidth = 3f;

        public int Denominator
        {
            get => denominator;
            set { denominator = Mathf.Clamp(value, 1, 12); SetVerticesDirty(); }
        }

        public float Sweep => 360f / denominator;

        public void Configure(int n, Color fillColor, bool useGradient, bool useOutline)
        {
            denominator = Mathf.Clamp(n, 1, 12);
            fill = fillColor;
            gradient = useGradient;
            outline = useOutline;
            SetVerticesDirty();
        }

        /// <summary>Follow the global colour setting for this denominator.</summary>
        public void UseThemeColor()
        {
            if (autoColor) return;
            autoColor = true;
            Settings.Changed += RefreshThemeColor;
            RefreshThemeColor();
        }

        void RefreshThemeColor()
        {
            fill = Theme.PieceColor(denominator);
            SetVerticesDirty();
        }

        protected override void OnDestroy()
        {
            if (autoColor) Settings.Changed -= RefreshThemeColor;
            base.OnDestroy();
        }

        float Radius => Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;

        static Vector2 Dir(float deg) => new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float r = Radius;
            if (r <= 0) return;

            Vector2 c = rectTransform.rect.center;
            float sweep = Sweep;
            int segs = Mathf.Max(6, Mathf.CeilToInt(sweep / 3f));

            Color inner = gradient ? Color.Lerp(fill, Color.white, 0.25f) : fill;
            Color outer = gradient ? Color.Lerp(fill, Color.black, 0.10f) : fill;
            inner *= color;
            outer *= color;
            Color line = Theme.PieceOutline;
            line.a *= color.a;

            // Filled fan.
            vh.AddVert(c, inner, Vector2.zero);
            for (int i = 0; i <= segs; i++)
                vh.AddVert(c + Dir(startAngle - sweep * i / segs) * r, outer, Vector2.zero);
            for (int i = 1; i <= segs; i++)
                vh.AddTriangle(0, i, i + 1);

            if (!outline) return;

            float w = outlineWidth;

            // Rim.
            int start = vh.currentVertCount;
            for (int i = 0; i <= segs; i++)
            {
                var d = Dir(startAngle - sweep * i / segs);
                vh.AddVert(c + d * r, line, Vector2.zero);
                vh.AddVert(c + d * (r - w), line, Vector2.zero);
            }
            for (int i = 0; i < segs; i++)
            {
                int a = start + i * 2;
                vh.AddTriangle(a, a + 1, a + 3);
                vh.AddTriangle(a, a + 3, a + 2);
            }

            // Straight edges (drawn on the inside, so two neighbouring pieces form one line).
            if (denominator > 1)
            {
                AddEdge(vh, c, startAngle, startAngle - 90f, r, w * 0.6f, line);
                AddEdge(vh, c, startAngle - sweep, startAngle - sweep + 90f, r, w * 0.6f, line);
            }
        }

        static void AddEdge(VertexHelper vh, Vector2 c, float edgeAngle, float inwardAngle, float r, float w, Color col)
        {
            var d = Dir(edgeAngle);
            var n = Dir(inwardAngle) * w;
            int i = vh.currentVertCount;
            vh.AddVert(c, col, Vector2.zero);
            vh.AddVert(c + d * r, col, Vector2.zero);
            vh.AddVert(c + d * r + n, col, Vector2.zero);
            vh.AddVert(c + n, col, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var lp))
                return false;
            var d = lp - rectTransform.rect.center;
            if (d.magnitude > Radius) return false;
            if (denominator <= 1) return true;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            return Mathf.Repeat(startAngle - ang, 360f) <= Sweep;
        }
    }
}
