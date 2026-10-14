using UnityEngine;
using UnityEngine.EventSystems;

namespace FractionCircles
{
    /// <summary>Springy hover / press feedback and a click sound for buttons.</summary>
    public class ButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public float hoverScale = 1.07f;
        public Sfx sound = Sfx.Tap;
        bool over, down;
        float baseScale = 1f;

        void OnEnable() => baseScale = 1f;

        public void OnPointerEnter(PointerEventData e) => over = true;
        public void OnPointerExit(PointerEventData e) => over = false;
        public void OnPointerDown(PointerEventData e) => down = true;
        public void OnPointerUp(PointerEventData e) => down = false;
        public void OnPointerClick(PointerEventData e) => AudioManager.Play(sound, 0.7f);

        public void Press() { down = true; }
        public void Release() { down = false; }

        void Update()
        {
            float target = baseScale * (down ? 0.9f : over ? hoverScale : 1f);
            float s = Mathf.Lerp(transform.localScale.x, target, 1f - Mathf.Exp(-20f * Time.deltaTime));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
