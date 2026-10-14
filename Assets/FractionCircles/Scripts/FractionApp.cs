using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace FractionCircles
{
    /// <summary>
    /// Root of the tool. The whole layout (tray, card strip, table, toolbar...) is saved in the scene;
    /// <see cref="BuildScene"/> creates it, and it only runs again if the scene is empty.
    /// </summary>
    public class FractionApp : MonoBehaviour
    {
        public static FractionApp I { get; private set; }

        [field: SerializeField] public RectTransform CanvasRect { get; private set; }
        [field: SerializeField] public RectTransform ItemsLayer { get; private set; }
        [field: SerializeField] public RectTransform OverlayLayer { get; private set; }
        [field: SerializeField] public RectTransform TopLayer { get; private set; }
        [field: SerializeField] public Workspace Workspace { get; private set; }
        [field: SerializeField] public TrashBin Bin { get; private set; }
        [field: SerializeField] public MainMenu Menu { get; private set; }
        [field: SerializeField] public Button HomeButton { get; private set; }
        [field: SerializeField] public Button LabelsButton { get; private set; }
        [field: SerializeField] public Button ColorsButton { get; private set; }
        public GuidedTour Tour { get; private set; }

        public float Scale => CanvasRect.localScale.x;

        readonly Dictionary<(int, int), SpawnSource> traySources = new Dictionary<(int, int), SpawnSource>();
        readonly Dictionary<string, SpawnSource> cardSources = new Dictionary<string, SpawnSource>();

        public SpawnSource TraySource(int denominator, int index) => traySources[(denominator, index)];
        public SpawnSource CardSource(string value) => cardSources[value];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Edition.Detect();
            if (FindAnyObjectByType<FractionApp>() == null)
                new GameObject("Fraction Circles").AddComponent<FractionApp>();
        }

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
#if !UNITY_WEBGL
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
#endif

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.11f, 0.35f, 0.12f);
            }

            EnsureEventSystem();
            if (GetComponentInChildren<AudioManager>(true) == null) AudioManager.Create(transform);
            if (CanvasRect == null) BuildScene();

            foreach (var s in GetComponentsInChildren<SpawnSource>(true))
            {
                if (s.kind == SpawnSource.Kind.Piece) traySources[(s.denominator, s.index)] = s;
                else cardSources[s.value] = s;
            }

            Tour = gameObject.AddComponent<GuidedTour>();
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        public static EventSystem EnsureEventSystem()
        {
            var es = FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.gameObject.AddComponent<InputSystemUIInputModule>();
#else
                es.gameObject.AddComponent<StandaloneInputModule>();
#endif
            }
#if ENABLE_INPUT_SYSTEM
            var module = es.GetComponent<InputSystemUIInputModule>();
            if (module != null && module.actionsAsset == null && Application.isPlaying) module.AssignDefaultActions();
#endif
            return es;
        }

        // ------------------------------------------------------------------ screens

        public void OpenTool()
        {
            AudioManager.Play(Sfx.Start, 0.8f, 1f, 0f);
            if (AudioManager.I) AudioManager.I.Duck(1.2f);
            Menu.Hide();
        }

        public void GoHome()
        {
            if (Menu == null) return;
            Tour.Stop();
            Menu.Show();
        }

        public void StartTour()
        {
            AudioManager.Play(Sfx.Start, 0.8f, 1f, 0f);
            Menu.Hide();
            Tour.Run(false, () => Menu.Show());
        }

        /// <summary>Screen-space point on the table, (0,0) = bottom-left, (1,1) = top-right.</summary>
        public Vector2 TablePoint(float u, float v)
        {
            var c = new Vector3[4];
            Workspace.Table.GetWorldCorners(c);
            return new Vector2(Mathf.Lerp(c[0].x, c[2].x, u), Mathf.Lerp(c[0].y, c[2].y, v));
        }

        // ------------------------------------------------------------------ layout

        /// <summary>Creates the whole layout under this object (used once, when the scene is set up).</summary>
        public void BuildScene()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            CanvasRect = (RectTransform)canvasGo.transform;

            float top = Theme.TrayHeight + Theme.StripHeight;

            var table = UIFactory.Rect("Table", CanvasRect).Stretch(0, 0, 0, top);
            var wood = table.gameObject.AddComponent<RawImage>();
            wood.texture = ProceduralArt.WoodTexture;
            wood.raycastTarget = false;
            table.gameObject.AddComponent<TiledRawImage>().tileSize = 460;

            BuildTray();
            BuildCardStrip();

            ItemsLayer = UIFactory.Rect("Items", CanvasRect).Stretch();
            Workspace = ItemsLayer.gameObject.AddComponent<Workspace>();
            Workspace.Table = table;

            var tableOverlay = UIFactory.Rect("Table Overlay", CanvasRect).Stretch(0, 0, 0, top);
            Bin = TrashBin.Build(tableOverlay);

            BuildToolbar();

            OverlayLayer = UIFactory.Rect("Overlay", CanvasRect).Stretch();
            if (Edition.Premium) Menu = MainMenu.Build(CanvasRect);
            TopLayer = UIFactory.Rect("Top", CanvasRect).Stretch();
        }

        void BuildTray()
        {
            var tray = UIFactory.Rect("Tray", CanvasRect);
            tray.anchorMin = new Vector2(0, 1);
            tray.anchorMax = new Vector2(1, 1);
            tray.pivot = new Vector2(0.5f, 1);
            tray.offsetMin = new Vector2(0, -Theme.TrayHeight);
            tray.offsetMax = new Vector2(-Theme.ToolbarWidth, 0);

            var felt = tray.gameObject.AddComponent<RawImage>();
            felt.texture = ProceduralArt.FeltTexture;
            tray.gameObject.AddComponent<TiledRawImage>().tileSize = 256;

            for (int i = 0; i < 10; i++)
            {
                int col = i % 5, row = i / 5;
                var cell = UIFactory.Rect("Cell 1/" + (i + 1), tray);
                cell.anchorMin = new Vector2(col / 5f, 1f - (row + 1) / 2f);
                cell.anchorMax = new Vector2((col + 1) / 5f, 1f - row / 2f);
                cell.offsetMin = cell.offsetMax = Vector2.zero;
                BuildInset(cell, i + 1);
            }

            for (int c = 1; c < 5; c++)
            {
                var line = UIFactory.Image(tray, "VLine", null, Theme.CellLine).rectTransform;
                line.anchorMin = new Vector2(c / 5f, 0);
                line.anchorMax = new Vector2(c / 5f, 1);
                line.sizeDelta = new Vector2(3, 0);
            }
            var h = UIFactory.Image(tray, "HLine", null, Theme.CellLine).rectTransform;
            h.anchorMin = new Vector2(0, 0.5f);
            h.anchorMax = new Vector2(1, 0.5f);
            h.sizeDelta = new Vector2(0, 3);
        }

        static void BuildInset(RectTransform cell, int n)
        {
            var glow = UIFactory.Image(cell, "Glow", ProceduralArt.Glow, new Color(0.6f, 1f, 0.5f, 0.22f));
            glow.rectTransform.Centered(new Vector2(330, 260));

            float d = Theme.PieceRadius * 2;
            UIFactory.Image(cell, "Hole", ProceduralArt.Circle, new Color(0, 0.08f, 0, 0.45f)).rectTransform.Centered(new Vector2(d + 12, d + 12));

            float sweep = 360f / n;
            for (int k = 0; k < n; k++)
            {
                var srt = UIFactory.Rect("Piece " + (k + 1) + "/" + n, cell).Centered(new Vector2(d, d));
                srt.localRotation = Quaternion.Euler(0, 0, -k * sweep);
                var g = srt.gameObject.AddComponent<SectorGraphic>();
                g.Configure(n, Theme.PieceColor(n), true, true);
                g.UseThemeColor();
                SpawnSource.ForPiece(srt.gameObject, n, k);
            }

            var caption = UIFactory.Text(cell, "Caption", n == 1 ? "1" : "1/" + n, 22, new Color(1, 1, 1, 0.75f));
            caption.rectTransform.Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -8), new Vector2(60, 28));
            caption.alignment = TextAnchor.UpperLeft;
            caption.enabled = Settings.ShowLabels;
            SettingIndicator.Add(cell.gameObject, SettingKind.Labels).enableWhenOn = caption;
        }

        void BuildCardStrip()
        {
            var strip = UIFactory.Rect("Card Strip", CanvasRect);
            strip.anchorMin = new Vector2(0, 1);
            strip.anchorMax = new Vector2(1, 1);
            strip.pivot = new Vector2(0.5f, 1);
            strip.offsetMin = new Vector2(0, -(Theme.TrayHeight + Theme.StripHeight));
            strip.offsetMax = new Vector2(0, -Theme.TrayHeight);
            strip.gameObject.AddComponent<Image>().color = Theme.StripBg;

            foreach (var edge in new[] { 0f, 1f })
            {
                var e = UIFactory.Image(strip, "Edge", null, Theme.StripEdge).rectTransform;
                e.anchorMin = new Vector2(0, edge);
                e.anchorMax = new Vector2(1, edge);
                e.sizeDelta = new Vector2(0, 6);
            }

            var row = UIFactory.Rect("Cards", strip).Stretch();
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            for (int i = 1; i <= 10; i++) AddCardSource(row, CardKind.Number, i.ToString());
            foreach (var s in new[] { "+", "−", "×", "÷", "=" }) AddCardSource(row, CardKind.Symbol, s);
            AddCardSource(row, CardKind.FractionBar, "bar");
        }

        static void AddCardSource(RectTransform row, CardKind kind, string value)
        {
            var rt = UIFactory.Rect("Source " + value, row);
            CardItem.BuildArt(rt, kind, value, false);
            SpawnSource.ForCard(rt.gameObject, kind, value);
        }

        // ------------------------------------------------------------------ toolbar

        void BuildToolbar()
        {
            var bar = UIFactory.Rect("Toolbar", CanvasRect);
            bar.anchorMin = bar.anchorMax = new Vector2(1, 1);
            bar.pivot = new Vector2(1, 1);
            bar.sizeDelta = new Vector2(Theme.ToolbarWidth, Theme.TrayHeight);
            bar.anchoredPosition = Vector2.zero;
            var bg = bar.gameObject.AddComponent<RawImage>();
            bg.texture = ProceduralArt.FeltTexture;
            bg.color = new Color(0.62f, 0.62f, 0.62f);
            bar.gameObject.AddComponent<TiledRawImage>().tileSize = 256;

            var col = UIFactory.Rect("Buttons", bar).Stretch(0, 4, 0, 6);
            var layout = col.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 0;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            if (Edition.Premium)
            {
                HomeButton = ToolButton(col, "menu", out _);
                Icon(HomeButton, ProceduralArt.House, 32);
                UIAction.Add(HomeButton, UIActionKind.Home);
            }

            var help = ToolButton(col, "help", out _);
            UIFactory.Text(help.transform, "Icon", "?", 30, Theme.Ink).rectTransform.Stretch(0, 2, 0, 0);
            UIAction.Add(help, UIActionKind.Help);

            if (Edition.Premium) AddDisplayOptions(col);

            var sound = ToolButton(col, "sound", out _);
            SoundIcon(sound, ProceduralArt.Speaker, SettingKind.Sound, 32);

            LanguageButton(ToolButton(col, "language", out _), 22);
        }

        void AddDisplayOptions(RectTransform col)
        {
            LabelsButton = ToolButton(col, "labels", out var labelsRing);
            UIFactory.Text(LabelsButton.transform, "Icon", "½", 30, Theme.Ink).rectTransform.Stretch(0, 2, 0, 0);
            UIAction.Add(LabelsButton, UIActionKind.ToggleLabels);
            SettingIndicator.Add(LabelsButton.transform.parent.gameObject, SettingKind.Labels).tint = labelsRing;

            ColorsButton = ToolButton(col, "colours", out var colorsRing);
            for (int k = 0; k < 4; k++)
            {
                var srt = UIFactory.Rect("Wedge", ColorsButton.transform).Centered(new Vector2(32, 32));
                srt.localRotation = Quaternion.Euler(0, 0, -k * 90);
                var g = srt.gameObject.AddComponent<SectorGraphic>();
                g.Configure(4, Theme.Rainbow[k * 2], false, false);
                g.raycastTarget = false;
            }
            UIAction.Add(ColorsButton, UIActionKind.ToggleColours);
            SettingIndicator.Add(ColorsButton.transform.parent.gameObject, SettingKind.Colourful).tint = colorsRing;
        }

        /// <summary>A round toolbar button with a translated caption. <paramref name="ring"/> shows the "on" state.</summary>
        static Button ToolButton(RectTransform parent, string captionKey, out Image ring)
        {
            var holder = UIFactory.Rect(captionKey, parent);
            holder.sizeDelta = new Vector2(Theme.ToolbarWidth, 64);

            ring = UIFactory.Image(holder, "Ring", ProceduralArt.Circle, Color.clear);
            ring.rectTransform.Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(52, 52));

            var face = UIFactory.Image(holder, "Face", ProceduralArt.Circle, Color.white, true);
            face.rectTransform.Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(44, 44));
            var btn = face.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            face.gameObject.AddComponent<ButtonFx>();

            var txt = UIFactory.Text(holder, "Caption", "", 14, Color.white);
            txt.rectTransform.Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 1), new Vector2(118, 16));
            UIFactory.AddShadow(txt, new Vector2(1, -1), 0.6f);
            Loc.Bind(txt, captionKey);
            return btn;
        }

        static void Icon(Button b, Sprite s, float size)
        {
            UIFactory.Image(b.transform, "Icon", s, Theme.Ink).rectTransform.Centered(new Vector2(size, size));
        }

        /// <summary>An on/off icon button: draws a red slash when the setting is off.</summary>
        public static void SoundIcon(Button b, Sprite s, SettingKind setting, float size = 40)
        {
            Icon(b, s, size);
            var slash = UIFactory.Image(b.transform, "Off", ProceduralArt.Rounded, new Color(0.85f, 0.2f, 0.2f)).rectTransform;
            slash.Centered(new Vector2(size * 0.15f, size * 1.25f));
            slash.localRotation = Quaternion.Euler(0, 0, 45);
            slash.gameObject.SetActive(!SettingIndicator.Value(setting));
            UIAction.Add(b, setting == SettingKind.Music ? UIActionKind.ToggleMusic : UIActionKind.ToggleSound);
            SettingIndicator.Add(b.gameObject, setting).showWhenOff = slash.gameObject;
        }

        /// <summary>Shows the current language code (EN / FR); tapping switches language.</summary>
        public static void LanguageButton(Button b, int fontSize)
        {
            var t = UIFactory.Text(b.transform, "Code", "", fontSize, Theme.Ink);
            t.rectTransform.Stretch(0, 1, 0, 0);
            Loc.Bind(t, "langCode");
            UIAction.Add(b, UIActionKind.ToggleLanguage);
        }

        // ------------------------------------------------------------------ dialogs

        public void ShowHelp()
        {
            var lines = new System.Collections.Generic.List<string>
            {
                "-  " + Loc.Get("help1"),
                "-  " + Loc.Get("help2"),
                "-  " + Loc.Get("help3"),
                "-  " + Loc.Get("help4") + " " + Loc.Get("help5"),
                "-  " + Loc.Get("help6") + " " + Loc.Get("help7"),
            };
            if (Edition.Premium) lines.Add("-  " + Loc.Get("help8"));
            lines.Add("-  " + Loc.Get("help9"));
            ShowDialog(Loc.Get("helpTitle"), string.Join("\n", lines));
        }

        public void ShowPremium()
        {
            ShowDialog(Loc.Get("exTitle"), Loc.Get("exBody"));
        }

        void ShowDialog(string title, string body)
        {
            AudioManager.Play(Sfx.Pick, 0.6f);
            var dim = UIFactory.Image(TopLayer, "Dialog", null, new Color(0, 0, 0, 0.55f), true);
            dim.rectTransform.Stretch();
            var group = dim.gameObject.AddComponent<CanvasGroup>();

            var panel = UIFactory.Image(dim.transform, "Panel", ProceduralArt.Rounded, new Color(1f, 0.98f, 0.94f), true);
            panel.rectTransform.Centered(new Vector2(1120, 700));
            panel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            var t = UIFactory.Text(panel.transform, "Title", title, 46, Theme.Ink);
            t.rectTransform.Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(1040, 60));

            var b = UIFactory.Text(panel.transform, "Body", body, 26, Theme.Ink, FontStyle.Normal, TextAnchor.UpperLeft);
            b.horizontalOverflow = HorizontalWrapMode.Wrap;
            b.lineSpacing = 1.15f;
            b.rectTransform.Stretch(60, 120, 60, 120);

            var ok = UIFactory.Image(panel.transform, "OK", ProceduralArt.Pill, new Color(0.25f, 0.65f, 0.3f), true);
            ok.rectTransform.Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(200, 64));
            UIFactory.Text(ok.transform, "Label", Loc.Get("ok"), 32, Color.white).rectTransform.Stretch();
            ok.gameObject.AddComponent<ButtonFx>();

            void Close() => Destroy(dim.gameObject);
            var okBtn = ok.gameObject.AddComponent<Button>();
            okBtn.transition = Selectable.Transition.None;
            okBtn.onClick.AddListener(Close);
            var dimBtn = dim.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Close);

            panel.rectTransform.localScale = Vector3.one * 0.85f;
            StartCoroutine(PopDialog(group, panel.rectTransform));
        }

        static IEnumerator PopDialog(CanvasGroup g, RectTransform panel)
        {
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.22f)
            {
                if (!g) yield break;
                g.alpha = t;
                float s = Ease.OutBack(t);
                panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, s);
                yield return null;
            }
            if (g) { g.alpha = 1; panel.localScale = Vector3.one; }
        }

        // ------------------------------------------------------------------ feedback

        public void FloatText(string text, Vector3 worldPos)
        {
            var t = UIFactory.Text(OverlayLayer, "Float", text, 34, Color.white);
            t.rectTransform.sizeDelta = new Vector2(400, 50);
            t.rectTransform.position = worldPos;
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.1f, 0.3f, 0.1f, 0.9f);
            o.effectDistance = new Vector2(2, -2);
            StartCoroutine(FloatRoutine(t));
        }

        IEnumerator FloatRoutine(Text t)
        {
            var start = t.rectTransform.position;
            for (float k = 0; k < 1f; k += Time.deltaTime / 1.8f)
            {
                if (!t) yield break;
                t.rectTransform.position = start + Vector3.up * (70f * Scale * k);
                t.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, Ease.OutBack(Mathf.Clamp01(k * 5f)));
                t.color = new Color(1, 1, 1, k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f);
                yield return null;
            }
            if (t) Destroy(t.gameObject);
        }
    }
}
