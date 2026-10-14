using System;
using UnityEngine;

namespace FractionCircles
{
    /// <summary>Calls <see cref="onChange"/> once now and every time a setting changes.</summary>
    public class SettingsListener : MonoBehaviour
    {
        Action onChange;

        public static void Attach(GameObject go, Action onChange)
        {
            var l = go.AddComponent<SettingsListener>();
            l.onChange = onChange;
            Settings.Changed += l.Fire;
            onChange();
        }

        void Fire() => onChange?.Invoke();
        void OnDestroy() => Settings.Changed -= Fire;
    }
}
