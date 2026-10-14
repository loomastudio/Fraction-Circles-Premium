using UnityEngine;
using UnityEngine.UI;

namespace FractionCircles
{
    /// <summary>A text that always shows <see cref="key"/> in the current language (see <see cref="Loc"/>).</summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        public string key;
        Text text;

        public static Text Attach(Text text, string key)
        {
            var l = text.gameObject.AddComponent<LocalizedText>();
            l.key = key;
            text.text = Loc.Get(key);
            return text;
        }

        void Awake()
        {
            text = GetComponent<Text>();
            Settings.Changed += Refresh;
            Refresh();
        }

        void OnDestroy() => Settings.Changed -= Refresh;

        void Refresh()
        {
            if (text) text.text = Loc.Get(key);
        }
    }
}
