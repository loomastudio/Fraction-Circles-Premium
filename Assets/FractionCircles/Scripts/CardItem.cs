using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace FractionCircles
{
    public enum CardKind { Number, Symbol, FractionBar }

    /// <summary>A number card, operation sign or fraction bar on the table. Cards snap above / below a fraction bar.</summary>
    public class CardItem : DraggableItem
    {
        public CardKind Kind { get; private set; }
        public string Value { get; private set; }

        RectTransform shadow;
        bool lifted;

        protected override float ClampMargin => Kind == CardKind.FractionBar ? Theme.BarWidth * 0.5f : Theme.CardSize * 0.5f;

        public static CardItem Create(CardKind kind, string value, Vector3 worldPos)
        {
            var rt = UIFactory.Rect("Card " + value, FractionApp.I.ItemsLayer);
            rt.position = worldPos;
            var card = rt.gameObject.AddComponent<CardItem>();
            card.Kind = kind;
            card.Value = value;
            card.shadow = BuildArt(rt, kind, value, true);
            return card;
        }

        /// <summary>Builds the visuals of a card under <paramref name="root"/> (shared with the card strip). Returns the shadow.</summary>
        public static RectTransform BuildArt(RectTransform root, CardKind kind, string value, bool withShadow)
        {
            RectTransform shadowRT = null;
            if (kind == CardKind.FractionBar)
            {
                root.sizeDelta = new Vector2(Theme.BarWidth + 10, 48);
                UIFactory.Image(root, "Hit", null, Color.clear, true).rectTransform.Stretch();
                if (withShadow)
                {
                    shadowRT = UIFactory.Image(root, "Shadow", ProceduralArt.Rounded, new Color(0, 0, 0, 0.25f)).rectTransform
                        .Centered(new Vector2(Theme.BarWidth, Theme.BarThickness));
                }
                UIFactory.Image(root, "Bar", ProceduralArt.Rounded, Color.black).rectTransform
                    .Centered(new Vector2(Theme.BarWidth, Theme.BarThickness));
                return shadowRT;
            }

            root.sizeDelta = new Vector2(Theme.CardSize, Theme.CardSize);
            if (withShadow)
            {
                shadowRT = UIFactory.Image(root, "Shadow", ProceduralArt.Rounded, new Color(0, 0, 0, 0.25f)).rectTransform
                    .Centered(new Vector2(Theme.CardSize, Theme.CardSize));
            }
            var face = UIFactory.Image(root, "Face", ProceduralArt.Card, Theme.CardFace, true);
            face.rectTransform.Stretch();
            if (value == "−")
            {
                // Drawn rather than typed: the minus glyph is missing from some fonts (e.g. on the web).
                UIFactory.Image(face.transform, "Minus", ProceduralArt.Rounded, Theme.Ink).rectTransform.Centered(new Vector2(24, 5));
                return shadowRT;
            }
            var txt = UIFactory.Text(face.transform, "Value", value, kind == CardKind.Symbol ? 38 : 32, Theme.Ink);
            txt.rectTransform.Stretch(0, 2, 0, 0);
            return shadowRT;
        }

        protected override void Update()
        {
            base.Update();
            if (shadow) shadow.anchoredPosition = lifted ? new Vector2(8, -10) : new Vector2(3, -4);
        }

        protected override void OnLifted(bool isLifted) => lifted = isLifted;

        protected override void OnDropped()
        {
            AudioManager.Play(Sfx.Card, 0.7f);
            if (Kind == CardKind.FractionBar) return;

            float s = FractionApp.I.Scale;
            var pos = rt.position;
            var bar = FractionApp.I.Workspace.Items.OfType<CardItem>()
                .Where(c => c.Kind == CardKind.FractionBar && !c.IsDragging && !c.IsRemoving)
                .Where(c => Mathf.Abs(c.RestPosition.x - pos.x) < Theme.BarWidth * 0.6f * s &&
                            Mathf.Abs(c.RestPosition.y - pos.y) < (Theme.CardSize + 30) * s)
                .OrderBy(c => (c.RestPosition - pos).sqrMagnitude)
                .FirstOrDefault();
            if (bar == null) return;

            float offset = (Theme.CardSize * 0.5f + 12f) * s;
            var target = bar.RestPosition + Vector3.up * (pos.y >= bar.RestPosition.y ? offset : -offset);
            MoveTo(target, 0.12f, () => AudioManager.Play(Sfx.Snap, 0.6f, 1.3f));
        }
    }
}
