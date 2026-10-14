using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FractionCircles
{
    /// <summary>
    /// Start screen: softly drifting fraction pieces, a title that fades in letter by letter,
    /// a circle that assembles itself from its eighths, and the main buttons.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] Language titleLanguage;
        [SerializeField] RectTransform root;
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform floatersRoot, wheel, wheelShadow, titleRoot, subtitle;
        [SerializeField] List<Floater> floaters = new List<Floater>();
        [SerializeField] List<RectTransform> letters = new List<RectTransform>();
        [SerializeField] List<float> letterX = new List<float>();
        [SerializeField] List<RectTransform> wedges = new List<RectTransform>();
        [SerializeField] List<RectTransform> buttons = new List<RectTransform>();
        Coroutine intro, fade;
        float introTime;
        bool introDone;

        [field: SerializeField] public Button StartButton { get; private set; }
        [field: SerializeField] public Button TourButton { get; private set; }
        public bool IsVisible => root.gameObject.activeSelf && group.alpha > 0.5f;

        [System.Serializable]
        class Floater
        {
            public RectTransform rt;
            public Vector2 velocity;
            public float spin, wobble, phase;
        }

        public static MainMenu Build(RectTransform canvas)
        {
            var rt = UIFactory.Rect("Main Menu", canvas).Stretch();
            var menu = rt.gameObject.AddComponent<MainMenu>();
            menu.root = rt;
            menu.group = rt.gameObject.AddComponent<CanvasGroup>();
            menu.BuildContents();
            return menu;
        }

        void BuildContents()
        {
            // Background: felt + soft light in the middle.
            var bg = UIFactory.Rect("Background", root).Stretch();
            var felt = bg.gameObject.AddComponent<RawImage>();
            felt.texture = ProceduralArt.FeltTexture;
            felt.color = new Color(0.82f, 0.9f, 0.82f);
            bg.gameObject.AddComponent<TiledRawImage>().tileSize = 256;
            UIFactory.Image(root, "Light", ProceduralArt.Glow, new Color(1f, 1f, 0.7f, 0.28f)).rectTransform.Centered(new Vector2(1700, 1300));

            floatersRoot = UIFactory.Rect("Floaters", root).Stretch();
            var rng = new System.Random(42);
            for (int i = 0; i < 12; i++)
            {
                int n = 2 + rng.Next(9);
                float size = 70 + (float)rng.NextDouble() * 110;
                var f = UIFactory.Rect("Floater", floatersRoot).Centered(new Vector2(size, size));
                var g = f.gameObject.AddComponent<SectorGraphic>();
                g.Configure(n, Theme.Rainbow[rng.Next(Theme.Rainbow.Length)], true, true);
                g.outlineWidth = 2;
                g.color = new Color(1, 1, 1, 0.16f);
                g.raycastTarget = false;
                floaters.Add(new Floater
                {
                    rt = f,
                    velocity = new Vector2((float)rng.NextDouble() * 10 - 5, 8 + (float)rng.NextDouble() * 12),
                    spin = ((float)rng.NextDouble() * 2 - 1) * 10f,
                    wobble = 10 + (float)rng.NextDouble() * 25,
                    phase = (float)rng.NextDouble() * 10f,
                });
                f.anchoredPosition = new Vector2(((float)rng.NextDouble() - 0.5f) * 1900, ((float)rng.NextDouble() - 0.5f) * 1200);
                f.localRotation = Quaternion.Euler(0, 0, (float)rng.NextDouble() * 360);
            }

            // Title.
            subtitle = UIFactory.Rect("Subtitle", root).Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -300), new Vector2(800, 40));
            var sub = UIFactory.Text(subtitle, "Text", "", 30, new Color(1, 1, 1, 0.85f));
            Loc.Bind(sub, "subtitle");
            sub.rectTransform.Stretch();
            UIFactory.AddShadow(sub, new Vector2(2, -2), 0.4f);

            titleRoot = UIFactory.Rect("Title", root).Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -190), new Vector2(1200, 140));
            BuildTitle();

            // Self-assembling wheel.
            wheel = UIFactory.Rect("Wheel", root).Centered(new Vector2(300, 300));
            wheel.anchoredPosition = new Vector2(0, 10);
            wheelShadow = UIFactory.Image(wheel, "Shadow", ProceduralArt.Circle, new Color(0, 0, 0, 0.25f)).rectTransform;
            wheelShadow.Centered(new Vector2(306, 306)).anchoredPosition = new Vector2(8, -12);
            for (int k = 0; k < 8; k++)
            {
                var w = UIFactory.Rect("Wedge", wheel).Stretch();
                w.localRotation = Quaternion.Euler(0, 0, -k * 45);
                var g = w.gameObject.AddComponent<SectorGraphic>();
                g.Configure(8, Theme.Rainbow[k + 1 < 10 ? k + 1 : 0], true, true);
                g.outlineWidth = 4;
                g.raycastTarget = false;
                wedges.Add(w);
            }

            // Buttons.
            var row = UIFactory.Rect("Buttons", root).Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(1100, 120));
            TourButton = MenuButton(row, "tour", new Color(0.27f, 0.55f, 0.9f), new Vector2(-375, 0), new Vector2(330, 84), 30);
            StartButton = MenuButton(row, "start", new Color(0.3f, 0.75f, 0.32f), Vector2.zero, new Vector2(350, 110), 46);
            var ex = MenuButton(row, "exercises", Theme.Gold, new Vector2(375, 0), new Vector2(330, 84), 30);
            var badge = UIFactory.Image(ex.transform, "Badge", ProceduralArt.Pill, new Color(0.85f, 0.25f, 0.3f));
            badge.rectTransform.Anchor(new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-30, -2), new Vector2(120, 32));
            badge.rectTransform.localRotation = Quaternion.Euler(0, 0, -8);
            var soon = UIFactory.Text(badge.transform, "Text", "", 17, Color.white);
            soon.rectTransform.Stretch();
            Loc.Bind(soon, "soon");

            UIAction.Add(StartButton, UIActionKind.Start);
            UIAction.Add(TourButton, UIActionKind.Tour);
            UIAction.Add(ex, UIActionKind.Exercises);

            // Sound + music toggles in the corner.
            var corner = UIFactory.Rect("Audio", root).Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(260, 76));
            var snd = RoundButton(corner, new Vector2(-38, -38));
            FractionApp.SoundIcon(snd, ProceduralArt.Speaker, SettingKind.Sound);
            var mus = RoundButton(corner, new Vector2(-128, -38));
            FractionApp.SoundIcon(mus, ProceduralArt.Note, SettingKind.Music);
            buttons.Add((RectTransform)snd.transform.parent);
            buttons.Add((RectTransform)mus.transform.parent);
            var lang = RoundButton(corner, new Vector2(-218, -38));
            FractionApp.LanguageButton(lang, 26);
            buttons.Add((RectTransform)lang.transform.parent);
        }

        void BuildTitle()
        {
            foreach (var l in letters)
            {
                if (!l) continue;
                if (Application.isPlaying) Destroy(l.gameObject);
                else DestroyImmediate(l.gameObject);
            }
            letters.Clear();
            letterX.Clear();

            titleLanguage = Loc.Current;
            string title = Loc.Get("title");
            const int size = 96;
            float x = 0;
            var widths = new List<float>();
            for (int i = 0; i < title.Length; i++)
            {
                var holder = UIFactory.Rect("Letter", titleRoot);
                holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0.5f);
                holder.sizeDelta = new Vector2(size, size * 1.3f);
                var t = UIFactory.Text(holder, "Glyph", title[i].ToString(), size, Color.white);
                t.rectTransform.Stretch();
                UIFactory.AddShadow(t, new Vector2(3, -5), 0.35f);

                float w = title[i] == ' ' ? size * 0.35f : t.preferredWidth + 4;
                widths.Add(w);
                letters.Add(holder);
                x += w;
            }
            float left = -x / 2f;
            for (int i = 0; i < letters.Count; i++)
            {
                letterX.Add(left + widths[i] / 2f);
                left += widths[i];
            }
        }

        /// <summary>Puts the title letters straight into their final place (after a language change).</summary>
        void ShowTitleNow()
        {
            for (int i = 0; i < letters.Count; i++)
            {
                letters[i].localScale = Vector3.one;
                letters[i].anchoredPosition = new Vector2(letterX[i], 0);
            }
        }

        Button MenuButton(RectTransform parent, string key, Color color, Vector2 pos, Vector2 size, int fontSize)
        {
            var holder = UIFactory.Rect(key, parent).Centered(size);
            holder.anchoredPosition = pos;
            var shadow = UIFactory.Image(holder, "Shadow", ProceduralArt.Pill, new Color(0, 0, 0, 0.3f));
            shadow.rectTransform.Stretch(4, -8, -4, 8);
            var face = UIFactory.Image(holder, "Face", ProceduralArt.Pill, color, true);
            face.rectTransform.Stretch();
            var shine = UIFactory.Image(face.transform, "Shine", ProceduralArt.Pill, new Color(1, 1, 1, 0.22f));
            shine.rectTransform.Stretch(12, size.y * 0.5f, 12, 6);
            var t = UIFactory.Text(face.transform, "Label", "", fontSize, Color.white);
            t.rectTransform.Stretch(0, 4, 0, 0);
            Loc.Bind(t, key);
            UIFactory.AddShadow(t, new Vector2(2, -3), 0.35f);
            var b = face.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            face.gameObject.AddComponent<ButtonFx>();
            buttons.Add(holder);
            return b;
        }

        Button RoundButton(RectTransform parent, Vector2 pos)
        {
            var holder = UIFactory.Rect("Toggle", parent).Anchor(new Vector2(1, 1), new Vector2(0.5f, 0.5f), pos, new Vector2(72, 72));
            UIFactory.Image(holder, "Shadow", ProceduralArt.Circle, new Color(0, 0, 0, 0.3f)).rectTransform.Stretch(3, -6, -3, 6);
            var face = UIFactory.Image(holder, "Face", ProceduralArt.Circle, Color.white, true);
            face.rectTransform.Stretch();
            var b = face.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            face.gameObject.AddComponent<ButtonFx>().sound = Sfx.Toggle;
            return b;
        }

        // ------------------------------------------------------------------ show / hide

        void Awake()
        {
            Settings.Changed += OnSettingsChanged;
            if (titleLanguage != Loc.Current || letters.Count == 0) BuildTitle();
        }

        void OnDestroy() => Settings.Changed -= OnSettingsChanged;

        void OnSettingsChanged()
        {
            if (Loc.Current == titleLanguage) return;
            BuildTitle();
            ShowTitleNow();
        }

        void Start() => Show();

        public void Show()
        {
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            if (FractionApp.I.TopLayer) FractionApp.I.TopLayer.SetAsLastSibling();
            group.blocksRaycasts = true;
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(FadeRoutine(true));
            if (intro != null) StopCoroutine(intro);
            intro = StartCoroutine(Intro());
        }

        public void Hide()
        {
            group.blocksRaycasts = false;
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(FadeRoutine(false));
        }

        IEnumerator FadeRoutine(bool show)
        {
            float from = group.alpha, to = show ? 1f : 0f;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.45f)
            {
                group.alpha = Mathf.Lerp(from, to, Ease.InOutCubic(t));
                float s = show ? Mathf.Lerp(1.08f, 1f, Ease.OutCubic(t)) : Mathf.Lerp(1f, 1.12f, Ease.InOutCubic(t));
                root.localScale = new Vector3(s, s, 1);
                yield return null;
            }
            group.alpha = to;
            root.localScale = Vector3.one;
            if (!show) root.gameObject.SetActive(false);
            fade = null;
        }

        IEnumerator Intro()
        {
            introDone = false;
            introTime = 0;
            foreach (var l in letters) l.localScale = Vector3.zero;
            foreach (var b in buttons) b.localScale = Vector3.zero;
            subtitle.localScale = Vector3.zero;
            wheelShadow.localScale = Vector3.zero;
            for (int k = 0; k < wedges.Count; k++) wedges[k].localScale = Vector3.zero;

            yield return new WaitForSeconds(0.25f);

            // Letters drop in.
            for (int i = 0; i < letters.Count; i++)
            {
                StartCoroutine(DropLetter(letters[i], letterX[i]));
                yield return new WaitForSeconds(0.045f);
            }
            StartCoroutine(PopIn(subtitle, 0.4f));

            // Wedges fly in from all directions and click into a circle.
            for (int k = 0; k < wedges.Count; k++)
            {
                StartCoroutine(FlyWedge(wedges[k], k));
                yield return new WaitForSeconds(0.11f);
            }
            StartCoroutine(PopIn(wheelShadow, 0.3f));
            yield return new WaitForSeconds(0.45f);
            AudioManager.Play(Sfx.Whole, 0.7f, 1f, 0f);

            for (int b = 0; b < buttons.Count; b++)
            {
                StartCoroutine(PopIn(buttons[b], 0.45f));
                yield return new WaitForSeconds(0.08f);
            }
            introDone = true;
            intro = null;
        }

        IEnumerator DropLetter(RectTransform l, float x)
        {
            var glyph = l.GetComponentInChildren<Text>();
            l.localScale = Vector3.one;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.5f)
            {
                if (!l) yield break;
                float e = Ease.OutCubic(t);
                l.anchoredPosition = new Vector2(x, Mathf.Lerp(-40f, 0f, e));
                glyph.color = new Color(1, 1, 1, e);
                yield return null;
            }
            if (!l) yield break;
            l.anchoredPosition = new Vector2(x, 0);
            glyph.color = Color.white;
        }

        IEnumerator FlyWedge(RectTransform w, int k)
        {
            float mid = (90f - k * 45f - 22.5f) * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(mid), Mathf.Sin(mid));
            var from = dir * 520f;
            float spin = (k % 2 == 0 ? 1 : -1) * 40f;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.55f)
            {
                float e = Ease.OutCubic(t);
                w.anchoredPosition = Vector2.LerpUnclamped(from, Vector2.zero, e);
                w.localRotation = Quaternion.Euler(0, 0, -k * 45f + spin * (1f - Ease.OutCubic(t)));
                w.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, Ease.OutCubic(t));
                yield return null;
            }
            w.anchoredPosition = Vector2.zero;
            w.localRotation = Quaternion.Euler(0, 0, -k * 45f);
            w.localScale = Vector3.one;
            AudioManager.Play(Sfx.Snap, 0.55f, 1f + k * 0.06f, 0f);
        }

        static IEnumerator PopIn(RectTransform rt, float duration)
        {
            for (float t = 0; t < 1f; t += Time.deltaTime / duration)
            {
                float s = Ease.OutBack(t, 1.4f);
                rt.localScale = new Vector3(s, s, 1);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        // ------------------------------------------------------------------ idle animation

        void Update()
        {
            if (!root.gameObject.activeSelf) return;
            float dt = Time.deltaTime;
            introTime += dt;
            var size = root.rect.size;

            foreach (var f in floaters)
            {
                var p = f.rt.anchoredPosition + f.velocity * dt;
                p.x += Mathf.Sin(Time.time * 0.6f + f.phase) * f.wobble * dt;
                if (p.y > size.y * 0.5f + 150) { p.y = -size.y * 0.5f - 150; }
                if (p.x > size.x * 0.5f + 150) p.x = -size.x * 0.5f - 150;
                if (p.x < -size.x * 0.5f - 150) p.x = size.x * 0.5f + 150;
                f.rt.anchoredPosition = p;
                f.rt.localRotation = Quaternion.Euler(0, 0, f.rt.localEulerAngles.z + f.spin * dt);
            }

            if (!introDone) return;

            float time = introTime;
            wheel.localRotation = Quaternion.Euler(0, 0, -time * 5f);
            float breathe = 1f + Mathf.Sin(time * 1.2f) * 0.012f;
            wheel.localScale = new Vector3(breathe, breathe, 1);
        }
    }
}
