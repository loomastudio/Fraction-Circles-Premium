using UnityEngine;
using UnityEngine.UI;

namespace FractionCircles
{
    public enum UIActionKind { Help, ToggleSound, ToggleMusic, ToggleLanguage, ToggleLabels, ToggleColours, Home, Start, Tour, Exercises }

    /// <summary>What a button does when pressed. Set in the scene, wired up when the tool starts.</summary>
    [RequireComponent(typeof(Button))]
    public class UIAction : MonoBehaviour
    {
        public UIActionKind action;

        public static UIAction Add(Button button, UIActionKind action)
        {
            var a = button.gameObject.AddComponent<UIAction>();
            a.action = action;
            return a;
        }

        void Awake() => GetComponent<Button>().onClick.AddListener(Run);

        public void Run()
        {
            var app = FractionApp.I;
            switch (action)
            {
                case UIActionKind.Help: app.ShowHelp(); break;
                case UIActionKind.ToggleSound: Settings.Sound = !Settings.Sound; break;
                case UIActionKind.ToggleMusic: Settings.Music = !Settings.Music; break;
                case UIActionKind.ToggleLanguage: Loc.Toggle(); break;
                case UIActionKind.ToggleLabels: Settings.ShowLabels = !Settings.ShowLabels; break;
                case UIActionKind.ToggleColours: Settings.Colorful = !Settings.Colorful; break;
                case UIActionKind.Home: app.GoHome(); break;
                case UIActionKind.Start: app.OpenTool(); break;
                case UIActionKind.Tour: app.StartTour(); break;
                case UIActionKind.Exercises: app.ShowPremium(); break;
            }
        }
    }
}
