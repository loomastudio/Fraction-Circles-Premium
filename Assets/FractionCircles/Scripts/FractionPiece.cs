using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FractionCircles
{
    /// <summary>
    /// A 1/n fraction piece on the table. Its RectTransform position is the centre of the circle it belongs to,
    /// so pieces that share a centre form one circle. Pieces dropped near another circle snap into a free gap.
    /// </summary>
    public class FractionPiece : DraggableItem, IScrollHandler
    {
        const int Lcm = 2520; // lcm(1..10) — lets us add fractions exactly with integers

        public int Denominator { get; private set; }

        /// <summary>Rotation in degrees (counter-clockwise). The wedge starts at the top and runs clockwise.</summary>
        public float Angle { get; private set; }

        float visualAngle;
        bool lifted;
        RectTransform spin, shadowRT, label;
        SectorGraphic body;

        float Sweep => 360f / Denominator;
        float R => Theme.PieceRadius;

        /// <summary>Counter-clockwise start of the occupied arc, in degrees.</summary>
        public float ArcStart => 90f + Angle - Sweep;
        public float ArcEnd => 90f + Angle;

        protected override float ClampMargin => R * 0.4f;

        public static FractionPiece Create(int denominator, Vector3 worldPos, float angle)
        {
            var rt = UIFactory.Rect("Piece 1/" + denominator, FractionApp.I.ItemsLayer);
            rt.sizeDelta = new Vector2(Theme.PieceRadius * 2, Theme.PieceRadius * 2);
            rt.position = worldPos;
            var p = rt.gameObject.AddComponent<FractionPiece>();
            p.Build(denominator, angle);
            return p;
        }

        void Build(int n, float angle)
        {
            Denominator = n;
            Angle = visualAngle = angle;

            shadowRT = UIFactory.Rect("Shadow", transform).Stretch();
            var shadow = shadowRT.gameObject.AddComponent<SectorGraphic>();
            shadow.Configure(n, new Color(0, 0, 0, 0.28f), false, false);
            shadow.raycastTarget = false;

            spin = UIFactory.Rect("Body", transform).Stretch();
            body = spin.gameObject.AddComponent<SectorGraphic>();
            body.Configure(n, Theme.PieceColor(n), true, true);
            body.UseThemeColor();

            label = BuildLabel(transform, n);
            SettingsListener.Attach(label.gameObject, () => label.gameObject.SetActive(Settings.ShowLabels));
            ApplyVisuals();
        }

        /// <summary>A stacked "1 over n" label. Shared with the tray.</summary>
        public static RectTransform BuildLabel(Transform parent, int n, int fontSize = 26)
        {
            var root = UIFactory.Rect("Label", parent);
            root.sizeDelta = new Vector2(40, 70);
            float scale = n >= 7 ? 0.8f : 1f;
            root.localScale = new Vector3(scale, scale, 1);

            if (n == 1)
            {
                var one = UIFactory.Text(root, "Whole", "1", fontSize + 6, Color.white);
                one.rectTransform.Stretch();
                UIFactory.AddShadow(one, new Vector2(1.5f, -1.5f));
                return root;
            }

            var num = UIFactory.Text(root, "Num", "1", fontSize, Color.white);
            num.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 17), new Vector2(40, 30));
            UIFactory.AddShadow(num, new Vector2(1.5f, -1.5f));

            var bar = UIFactory.Image(root, "Bar", null, Color.white);
            bar.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(n >= 10 ? 30 : 22, 3));
            UIFactory.AddShadow(bar, new Vector2(1.5f, -1.5f));

            var den = UIFactory.Text(root, "Den", n.ToString(), fontSize, Color.white);
            den.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -17), new Vector2(40, 30));
            UIFactory.AddShadow(den, new Vector2(1.5f, -1.5f));
            return root;
        }

        /// <summary>Where to put the label inside a wedge whose leading edge is at <paramref name="startAngle"/>.</summary>
        public static Vector2 LabelOffset(int n, float startAngle)
        {
            if (n == 1) return Vector2.zero;
            float dist = Theme.PieceRadius * (n == 2 ? 0.48f : n >= 7 ? 0.68f : 0.6f);
            float mid = (startAngle - 180f / n) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(mid), Mathf.Sin(mid)) * dist;
        }

        protected override void Update()
        {
            base.Update();
            visualAngle = Mathf.LerpAngle(visualAngle, Angle, 1f - Mathf.Exp(-20f * Time.deltaTime));
            ApplyVisuals();
        }

        void ApplyVisuals()
        {
            var rot = Quaternion.Euler(0, 0, visualAngle);
            spin.localRotation = rot;
            shadowRT.localRotation = rot;
            shadowRT.anchoredPosition = lifted ? new Vector2(10, -14) : new Vector2(4, -5);
            label.anchoredPosition = LabelOffset(Denominator, 90f + visualAngle);
        }

        protected override void OnLifted(bool isLifted) => lifted = isLifted;

        // ---------- turning ----------

        protected override void OnTap(bool alternate)
        {
            Turn(alternate ? 1 : -1);
        }

        public void OnScroll(PointerEventData e)
        {
            if (IsRemoving || Mathf.Approximately(e.scrollDelta.y, 0)) return;
            Turn(e.scrollDelta.y > 0 ? 1 : -1);
        }

        /// <summary>Turn by one piece-width: -1 = clockwise, +1 = counter-clockwise.</summary>
        void Turn(int dir)
        {
            AudioManager.Play(Sfx.Turn, 0.6f);
            Angle = Mathf.Repeat(Angle + dir * Sweep, 360f);
            CheckWhole();
        }

        // ---------- snapping ----------

        protected override void OnDropped()
        {
            if (TrySnap(out var center, out var angle))
            {
                Angle = angle;
                MoveTo(center, 0.14f, () => { AudioManager.Play(Sfx.Snap, 0.9f); CheckWhole(); });
            }
            else
            {
                AudioManager.Play(Sfx.Drop, 0.6f);
            }
        }

        IEnumerable<FractionPiece> PiecesAt(Vector3 center)
        {
            float eps = 2f * FractionApp.I.Scale;
            return FractionApp.I.Workspace.Pieces.Where(p =>
                p != this && !p.IsDragging && (p.RestPosition - center).sqrMagnitude < eps * eps);
        }

        bool TrySnap(out Vector3 center, out float angle)
        {
            center = rt.position;
            angle = Angle;

            float threshold = R * 0.6f * FractionApp.I.Scale;
            FractionPiece nearest = null;
            float best = threshold;
            foreach (var p in FractionApp.I.Workspace.Pieces)
            {
                if (p == this || p.IsDragging) continue;
                float d = Vector2.Distance(p.RestPosition, rt.position);
                if (d < best) { best = d; nearest = p; }
            }
            if (nearest == null) return false;

            center = nearest.RestPosition;
            var members = PiecesAt(center).ToList();

            // Candidate rotations: where it is now, on the nearest "tick", and flush against each neighbour.
            var candidates = new List<float> { Angle, Mathf.Round(Angle / Sweep) * Sweep };
            foreach (var m in members)
            {
                candidates.Add(m.ArcStart - 90f);          // our end meets their start
                candidates.Add(m.ArcEnd - 90f + Sweep);   // our start meets their end
            }

            float bestTurn = float.MaxValue;
            bool found = false;
            foreach (var c in candidates)
            {
                float start = 90f + c - Sweep;
                if (members.Any(m => ArcsOverlap(start, Sweep, m.ArcStart, m.Sweep))) continue;
                float turn = Mathf.Abs(Mathf.DeltaAngle(Angle, c));
                if (turn < bestTurn) { bestTurn = turn; angle = Mathf.Repeat(c, 360f); found = true; }
            }
            return found;
        }

        static bool ArcsOverlap(float startA, float lenA, float startB, float lenB)
        {
            const float eps = 0.5f;
            float d = Mathf.Repeat(startB - startA, 360f);
            return d < lenA - eps || d > 360f - lenB + eps;
        }

        // ---------- "you made a whole!" ----------

        void CheckWhole()
        {
            if (Edition.Basic) return;
            if (IsDragging || IsRemoving) return;
            var group = PiecesAt(RestPosition).ToList();
            group.Add(this);
            if (group.Count < 2 || group.Sum(p => Lcm / p.Denominator) != Lcm) return;

            // Make sure the pieces really tile the circle (no overlaps after turning).
            for (int i = 0; i < group.Count; i++)
            for (int j = i + 1; j < group.Count; j++)
                if (ArcsOverlap(group[i].ArcStart, group[i].Sweep, group[j].ArcStart, group[j].Sweep)) return;

            foreach (var p in group) p.Pulse();
            AudioManager.Play(Sfx.Whole, 0.8f, 1f, 0f);
            if (AudioManager.I) AudioManager.I.Duck(1.6f);

            var parts = group.GroupBy(p => p.Denominator).OrderBy(g => g.Key)
                .Select(g => $"{g.Count()}/{g.Key}");
            string sum = string.Join(" + ", parts) + " = 1";
            FractionApp.I.FloatText(sum, RestPosition + Vector3.up * (R + 30) * FractionApp.I.Scale);
        }
    }
}
