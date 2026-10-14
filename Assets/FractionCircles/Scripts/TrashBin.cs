using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FractionCircles
{
    /// <summary>The recycle bin: drop things on it, or tap twice to clear the table.</summary>
    public class TrashBin : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] RectTransform rt;
        [SerializeField] Image bg;
        [SerializeField] GameObject hint;
        float armedUntil;
        bool hover;

        public static TrashBin Build(RectTransform parent)
        {
            var rt = UIFactory.Rect("Bin", parent).Anchor(new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-74, 74), new Vector2(100, 100));
            var shadow = UIFactory.Image(rt, "Shadow", ProceduralArt.Rounded, new Color(0, 0, 0, 0.25f));
            shadow.rectTransform.Stretch(4, -6, -4, 6);
            var bg = UIFactory.Image(rt, "Bg", ProceduralArt.Rounded, Color.white, true);
            bg.rectTransform.Stretch();

            // A simple drawn bin icon.
            var ink = new Color(0.3f, 0.33f, 0.36f);
            UIFactory.Image(bg.transform, "Handle", ProceduralArt.Rounded, ink).rectTransform.Centered(new Vector2(18, 8)).anchoredPosition = new Vector2(0, 29);
            UIFactory.Image(bg.transform, "Lid", ProceduralArt.Rounded, ink).rectTransform.Centered(new Vector2(52, 8)).anchoredPosition = new Vector2(0, 23);
            var can = UIFactory.Image(bg.transform, "Can", ProceduralArt.Rounded, ink).rectTransform.Centered(new Vector2(40, 46));
            can.anchoredPosition = new Vector2(0, -8);
            for (int i = -1; i <= 1; i++)
                UIFactory.Image(can, "Slot", null, new Color(1, 1, 1, 0.6f)).rectTransform.Centered(new Vector2(4, 30)).anchoredPosition = new Vector2(i * 11, 0);

            var hint = UIFactory.Text(rt, "Hint", "", 24, Color.white);
            Loc.Bind(hint, "binHint");
            hint.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(460, 40));
            hint.alignment = TextAnchor.MiddleRight;
            UIFactory.AddShadow(hint, new Vector2(2, -2), 0.7f);

            var bin = rt.gameObject.AddComponent<TrashBin>();
            bin.rt = rt;
            bin.bg = bg;
            bin.hint = hint.gameObject;
            hint.gameObject.SetActive(false);
            return bin;
        }

        public bool Contains(Vector2 screenPos) => RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, null);

        public void SetHover(bool value) => hover = value;

        public void OnPointerClick(PointerEventData e) => Tap();

        public void Tap()
        {
            if (Time.time < armedUntil)
            {
                FractionApp.I.Workspace.ClearAll();
                AudioManager.Play(Sfx.Clear, 0.8f);
                armedUntil = 0;
            }
            else if (FractionApp.I.Workspace.Items.Count > 0)
            {
                AudioManager.Play(Sfx.Tap, 0.6f);
                armedUntil = Time.time + 2.5f;
            }
        }

        void Update()
        {
            bool armed = Time.time < armedUntil;
            float target = hover ? 1.2f : armed ? 1.08f : 1f;
            float s = Mathf.Lerp(rt.localScale.x, target, 1f - Mathf.Exp(-16f * Time.deltaTime));
            rt.localScale = new Vector3(s, s, 1);
            bg.color = armed || hover ? new Color(1f, 0.82f, 0.8f) : Color.white;
            if (hint.activeSelf != armed) hint.SetActive(armed);
        }
    }
}
