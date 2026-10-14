using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace FractionCircles
{
    /// <summary>Shows how the tool works with an animated finger: building circles, writing a fraction, tidying up.</summary>
    public class GuidedTour : MonoBehaviour
    {
        RectTransform hand, ripple;
        Image rippleImage;
        GameObject blocker;
        Coroutine run;
        Action onDone;
        bool pressed, savedLabels, savedColorful;
        DraggableItem held;
        float rippleTime = 99f;

        public bool Running { get; private set; }

        FractionApp App => FractionApp.I;
        float S => App.Scale;

        void EnsureUI()
        {
            if (hand) return;
            var top = App.TopLayer;

            var b = UIFactory.Image(top, "Tour Blocker", null, Color.clear, true);
            b.rectTransform.Stretch();
            var stopBtn = b.gameObject.AddComponent<Button>();
            stopBtn.transition = Selectable.Transition.None;
            stopBtn.onClick.AddListener(() => { Stop(); App.Menu.Show(); });
            var tag = UIFactory.Image(b.transform, "Tag", ProceduralArt.Pill, new Color(0, 0, 0, 0.45f));
            tag.rectTransform.Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-60, 22), new Vector2(560, 50));
            var banner = UIFactory.Text(tag.transform, "Text", "", 22, Color.white);
            banner.rectTransform.Stretch();
            Loc.Bind(banner, "tourBanner");
            blocker = b.gameObject;
            blocker.SetActive(false);

            hand = UIFactory.Rect("Finger", top);
            hand.sizeDelta = Vector2.zero;
            rippleImage = UIFactory.Image(hand, "Ripple", ProceduralArt.Ring, new Color(1, 1, 1, 0));
            ripple = rippleImage.rectTransform.Centered(new Vector2(60, 60));
            UIFactory.Image(hand, "Shadow", ProceduralArt.Circle, new Color(0, 0, 0, 0.28f)).rectTransform.Centered(new Vector2(54, 54)).anchoredPosition = new Vector2(5, -8);
            UIFactory.Image(hand, "Touch", ProceduralArt.Circle, new Color(1, 1, 1, 0.8f)).rectTransform.Centered(new Vector2(50, 50));
            UIFactory.Image(hand, "Edge", ProceduralArt.Ring, new Color(0.15f, 0.15f, 0.2f, 0.8f)).rectTransform.Centered(new Vector2(52, 52));
            hand.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ control

        /// <summary>Runs the guided tour. <paramref name="recording"/> hides the "tap to stop" banner.</summary>
        public void Run(bool recording, Action done)
        {
            EnsureUI();
            Stop();
            onDone = done;
            Running = true;
            blocker.SetActive(true);
            blocker.transform.GetChild(0).gameObject.SetActive(!recording);
            run = StartCoroutine(RunRoutine());
        }

        public void Stop()
        {
            if (!Running) return;
            if (run != null) StopCoroutine(run);
            run = null;
            Finish();
        }

        IEnumerator RunRoutine()
        {
            yield return TableScript();
            Finish();
            var done = onDone;
            onDone = null;
            done?.Invoke();
        }

        void Finish()
        {
            Running = false;
            if (held != null && held) held.Remove();
            held = null;
            pressed = false;
            if (blocker) blocker.SetActive(false);
            if (hand) hand.gameObject.SetActive(false);
            App.Workspace.ClearAll();
            Settings.ShowLabels = savedLabels;
            Settings.Colorful = savedColorful;
        }

        void Update()
        {
            if (!hand || !hand.gameObject.activeSelf) return;
            float s = Mathf.Lerp(hand.localScale.x, pressed ? 0.8f : 1f, 1f - Mathf.Exp(-25f * Time.deltaTime));
            hand.localScale = new Vector3(s, s, 1);

            rippleTime += Time.deltaTime;
            float k = Mathf.Clamp01(rippleTime / 0.45f);
            ripple.localScale = Vector3.one * Mathf.Lerp(0.6f, 2.2f, Ease.OutCubic(k));
            rippleImage.color = new Color(1, 1, 1, (1f - k) * 0.9f);
        }

        // ------------------------------------------------------------------ the script

        IEnumerator TableScript()
        {
            savedLabels = Settings.ShowLabels;
            savedColorful = Settings.Colorful;
            Settings.ShowLabels = false;
            Settings.Colorful = false;
            App.Workspace.ClearAll();

            ShowHand(App.TablePoint(0.5f, 0.3f));
            yield return Wait(0.6f);

            // A whole, sent to the table with a tap.
            var c1 = App.TablePoint(0.09f, 0.6f);
            var whole = App.TraySource(1, 0);
            yield return TapAt(whole.transform.position, () => whole.SpawnTo(c1));
            yield return Wait(0.5f);

            // Two halves make a whole.
            var c2 = App.TablePoint(0.27f, 0.6f);
            yield return DragFromTray(2, 0, c2, new Vector2(10, -6));
            yield return DragFromTray(2, 1, c2, new Vector2(-12, 9));
            yield return Wait(1.0f);

            // Colour each size.
            if (App.ColorsButton) yield return TapButton(App.ColorsButton);
            yield return Wait(0.4f);

            // One half and two quarters.
            var c3 = App.TablePoint(0.45f, 0.6f);
            yield return DragFromTray(2, 1, c3, new Vector2(6, 4));
            yield return DragFromTray(4, 0, c3, new Vector2(-10, 12));
            yield return DragFromTray(4, 1, c3, new Vector2(12, -9));
            yield return Wait(0.9f);

            // Show the names.
            if (App.LabelsButton) yield return TapButton(App.LabelsButton);
            yield return Wait(0.4f);

            // Thirds and sixths.
            var c4 = App.TablePoint(0.63f, 0.6f);
            yield return DragFromTray(3, 0, c4, new Vector2(-8, 6));
            yield return DragFromTray(3, 1, c4, new Vector2(12, 6));
            yield return DragFromTray(6, 4, c4, new Vector2(-11, -8));
            yield return DragFromTray(6, 5, c4, new Vector2(8, 12));
            yield return Wait(0.9f);

            // Write 3/4 with the cards.
            var cb = App.TablePoint(0.79f, 0.6f);
            yield return DragCard("bar", cb);
            yield return DragCard("3", cb + new Vector2(10, 70) * S);
            yield return DragCard("4", cb + new Vector2(-8, -64) * S);
            yield return Wait(0.3f);

            // Three quarters next to it, then complete the circle with eighths.
            var c5 = App.TablePoint(0.925f, 0.6f);
            yield return DragFromTray(4, 0, c5, new Vector2(6, -10));
            yield return DragFromTray(4, 1, c5, new Vector2(-9, 8));
            yield return DragFromTray(4, 2, c5, new Vector2(11, 5));
            yield return Wait(0.6f);
            yield return DragFromTray(8, 6, c5, new Vector2(-7, -9));
            yield return DragFromTray(8, 7, c5, new Vector2(8, 10));
            yield return Wait(1.2f);

            // Put the whole back in the tray.
            var wholeOnTable = App.Workspace.Pieces.FirstOrDefault(p => p.Denominator == 1);
            if (wholeOnTable != null) yield return DragItem(wholeOnTable, whole.transform.position);
            yield return Wait(0.5f);

            // Admire, then clear the table with the bin.
            yield return MoveHand(App.TablePoint(0.55f, 0.25f));
            yield return Wait(1.0f);
            yield return TapAt(App.Bin.transform.position, () => App.Bin.Tap());
            yield return Wait(0.25f);
            yield return TapAt(App.Bin.transform.position, () => App.Bin.Tap());
            yield return Wait(0.6f);

            Settings.ShowLabels = savedLabels;
            Settings.Colorful = savedColorful;
            HideHand();
            yield return Wait(0.4f);
        }

        // ------------------------------------------------------------------ finger helpers

        public void ShowHand(Vector2 at)
        {
            EnsureUI();
            if (hand.gameObject.activeSelf) return;
            hand.SetAsLastSibling();
            hand.position = at;
            hand.localScale = Vector3.zero;
            hand.gameObject.SetActive(true);
        }

        public void HideHand()
        {
            if (hand) hand.gameObject.SetActive(false);
        }

        static IEnumerator Wait(float seconds)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime) yield return null;
        }

        IEnumerator MoveHand(Vector2 to, float slow = 1f, Action<Vector2> onMove = null)
        {
            Vector2 from = hand.position;
            float dist = (to - from).magnitude / S;
            float duration = Mathf.Clamp(0.25f + dist / 1500f, 0.3f, 0.95f) * slow;
            var perp = new Vector2(-(to - from).y, (to - from).x).normalized;
            var ctrl = (from + to) * 0.5f + perp * dist * 0.12f * S;
            for (float t = 0; t < 1f; t += Time.deltaTime / duration)
            {
                float e = Ease.InOutCubic(t);
                var p = (1 - e) * (1 - e) * from + 2 * (1 - e) * e * ctrl + e * e * to;
                hand.position = p;
                onMove?.Invoke(p);
                yield return null;
            }
            hand.position = to;
            onMove?.Invoke(to);
        }

        IEnumerator Press()
        {
            pressed = true;
            rippleTime = 0;
            yield return Wait(0.1f);
        }

        IEnumerator Release()
        {
            pressed = false;
            yield return Wait(0.1f);
        }

        IEnumerator TapAt(Vector2 at, Action action)
        {
            yield return MoveHand(at);
            yield return Press();
            action();
            yield return Release();
            yield return Wait(0.15f);
        }

        public IEnumerator TapButton(Button b)
        {
            yield return MoveHand(b.transform.position);
            var fx = b.GetComponent<ButtonFx>();
            yield return Press();
            if (fx) { fx.Press(); AudioManager.Play(fx.sound, 0.7f); }
            yield return Wait(0.06f);
            if (fx) fx.Release();
            b.onClick.Invoke();
            yield return Release();
            yield return Wait(0.15f);
        }

        IEnumerator Carry(DraggableItem item, Vector2 grab, Vector2 to)
        {
            held = item;
            item.BeginDrag(grab);
            yield return MoveHand(to, 1.2f, p => item.DragTo(p));
            yield return Wait(0.05f);
            item.EndDrag(to);
            held = null;
            yield return Release();
            yield return Wait(0.12f);
        }

        /// <summary>Pull piece k of the 1/n frame out and drop it so it joins the circle at <paramref name="center"/>.</summary>
        IEnumerator DragFromTray(int n, int k, Vector2 center, Vector2 jitter)
        {
            var src = App.TraySource(n, k);
            Vector2 origin = src.transform.position;
            var grab = origin + FractionPiece.LabelOffset(n, 90f - k * 360f / n) * S;
            yield return MoveHand(grab);
            yield return Press();
            var piece = src.Factory();
            yield return Carry(piece, grab, center + (grab - origin) + jitter * S);
        }

        IEnumerator DragCard(string value, Vector2 to)
        {
            var src = App.CardSource(value);
            Vector2 grab = src.transform.position;
            yield return MoveHand(grab);
            yield return Press();
            yield return Carry(src.Factory(), grab, to);
        }

        IEnumerator DragItem(DraggableItem item, Vector2 to)
        {
            Vector2 grab = item.Rect.position;
            if (item is FractionPiece p) grab += FractionPiece.LabelOffset(p.Denominator, 90f + p.Angle) * S;
            yield return MoveHand(grab);
            yield return Press();
            yield return Carry(item, grab, to + (grab - (Vector2)item.Rect.position));
        }
    }
}
