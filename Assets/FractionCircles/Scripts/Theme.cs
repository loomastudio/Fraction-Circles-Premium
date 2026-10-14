using System;
using UnityEngine;

namespace FractionCircles
{
    /// <summary>Sizes, colours and fonts shared by the whole app (all sizes are in canvas reference units).</summary>
    public static class Theme
    {
        public const float PieceRadius = 80f;
        public const float CardSize = 64f;
        public const float BarWidth = 120f;
        public const float BarThickness = 14f;

        public const float TrayHeight = 400f;
        public const float StripHeight = 110f;
        public const float ToolbarWidth = 120f;

        public static readonly Color ClassicRed = new Color(0.88f, 0.27f, 0.29f);

        // One colour per denominator (1..10) for the "colourful" mode.
        public static readonly Color[] Rainbow =
        {
            new Color(0.90f, 0.30f, 0.30f), // 1   red
            new Color(0.98f, 0.58f, 0.22f), // 1/2 orange
            new Color(0.97f, 0.78f, 0.22f), // 1/3 yellow
            new Color(0.45f, 0.75f, 0.35f), // 1/4 green
            new Color(0.22f, 0.68f, 0.66f), // 1/5 teal
            new Color(0.30f, 0.55f, 0.90f), // 1/6 blue
            new Color(0.47f, 0.42f, 0.86f), // 1/7 indigo
            new Color(0.72f, 0.42f, 0.82f), // 1/8 purple
            new Color(0.93f, 0.45f, 0.66f), // 1/9 pink
            new Color(0.70f, 0.50f, 0.34f), // 1/10 brown
        };

        public static readonly Color PieceOutline = new Color(0.13f, 0.06f, 0.06f);
        public static readonly Color Felt = new Color(0.12f, 0.43f, 0.13f);
        public static readonly Color CellLine = new Color(0.58f, 0.40f, 0.18f);
        public static readonly Color StripBg = new Color(1f, 0.87f, 0.74f);
        public static readonly Color StripEdge = new Color(0.58f, 0.33f, 0.12f);
        public static readonly Color CardFace = new Color(0.95f, 0.95f, 0.95f);
        public static readonly Color Ink = new Color(0.16f, 0.16f, 0.18f);
        public static readonly Color Gold = new Color(1f, 0.78f, 0.2f);

        public static Color PieceColor(int denominator) =>
            Settings.Colorful ? Rainbow[(Mathf.Max(1, denominator) - 1) % Rainbow.Length] : ClassicRed;

        static Font font;
        public static Font Font => font ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
    }

    /// <summary>User preferences, persisted in PlayerPrefs.</summary>
    public static class Settings
    {
        public static event Action Changed;

        public static void NotifyChanged() => Changed?.Invoke();

        public static bool ShowLabels
        {
            get => Edition.Premium && PlayerPrefs.GetInt("fc_labels", 0) == 1;
            set { PlayerPrefs.SetInt("fc_labels", value ? 1 : 0); Changed?.Invoke(); }
        }

        public static bool Colorful
        {
            get => Edition.Premium && PlayerPrefs.GetInt("fc_colorful", 0) == 1;
            set { PlayerPrefs.SetInt("fc_colorful", value ? 1 : 0); Changed?.Invoke(); }
        }

        public static bool Sound
        {
            get => PlayerPrefs.GetInt("fc_sound", 1) == 1;
            set { PlayerPrefs.SetInt("fc_sound", value ? 1 : 0); Changed?.Invoke(); }
        }

        public static bool Music
        {
            get => Edition.Premium && PlayerPrefs.GetInt("fc_music", 1) == 1;
            set { PlayerPrefs.SetInt("fc_music", value ? 1 : 0); Changed?.Invoke(); }
        }
    }
}
