using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FractionCircles
{
    /// <summary>Small helpers for building uGUI hierarchies from code.</summary>
    public static class UIFactory
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Anchor(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Centered(this RectTransform rt, Vector2 size) =>
            rt.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public static Text Text(Transform parent, string name, string text, int size, Color color,
            FontStyle style = FontStyle.Bold, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = Rect(name, parent).gameObject.AddComponent<Text>();
            t.font = Theme.Font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static void AddShadow(Graphic g, Vector2 distance, float alpha = 0.5f)
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, alpha);
            s.effectDistance = distance;
        }
    }

    /// <summary>Keeps a RawImage's texture tiled at a fixed size whatever the rect size.</summary>
    [RequireComponent(typeof(RawImage))]
    public class TiledRawImage : UIBehaviour
    {
        public float tileSize = 512f;

        protected override void Start() => Apply();
        protected override void OnRectTransformDimensionsChange() => Apply();

        void Apply()
        {
            var ri = GetComponent<RawImage>();
            var r = ((RectTransform)transform).rect;
            if (ri) ri.uvRect = new Rect(0, 0, r.width / tileSize, r.height / tileSize);
        }
    }
}
