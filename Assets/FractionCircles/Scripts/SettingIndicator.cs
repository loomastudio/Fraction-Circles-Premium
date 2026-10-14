using UnityEngine;
using UnityEngine.UI;

namespace FractionCircles
{

    public enum SettingKind { Labels, Colourful, Sound, Music }

    /// <summary>Shows the state of a setting: tints a ring, shows a "muted" slash, or enables a caption.</summary>
    public class SettingIndicator : MonoBehaviour
    {
        public SettingKind setting;
        public Graphic tint;
        public Color onColor = Theme.Gold;
        public Color offColor = Color.clear;
        public GameObject showWhenOff;
        public Behaviour enableWhenOn;

        public static SettingIndicator Add(GameObject host, SettingKind setting)
        {
            var s = host.AddComponent<SettingIndicator>();
            s.setting = setting;
            return s;
        }

        public static bool Value(SettingKind kind)
        {
            switch (kind)
            {
                case SettingKind.Labels: return Settings.ShowLabels;
                case SettingKind.Colourful: return Settings.Colorful;
                case SettingKind.Music: return Settings.Music;
                default: return Settings.Sound;
            }
        }

        void Awake()
        {
            Settings.Changed += Refresh;
            Refresh();
        }

        void OnDestroy() => Settings.Changed -= Refresh;

        public void Refresh()
        {
            bool on = Value(setting);
            if (tint) tint.color = on ? onColor : offColor;
            if (showWhenOff) showWhenOff.SetActive(!on);
            if (enableWhenOn) enableWhenOn.enabled = on;
        }
    }
}
