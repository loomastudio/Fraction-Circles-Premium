using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FractionCircles
{
    /// <summary>Keeps track of everything on the wooden table.</summary>
    public class Workspace : MonoBehaviour
    {
        public readonly List<DraggableItem> Items = new List<DraggableItem>();
        public RectTransform Table;

        public IEnumerable<FractionPiece> Pieces
        {
            get
            {
                foreach (var i in Items)
                    if (i is FractionPiece p && !p.IsRemoving) yield return p;
            }
        }

        public void Register(DraggableItem item)
        {
            if (!Items.Contains(item)) Items.Add(item);
        }

        public void Unregister(DraggableItem item) => Items.Remove(item);

        public void ClearAll()
        {
            foreach (var i in Items.ToArray()) i.Remove();
        }

        /// <summary>Dropping something outside the table (tray, card strip) or on the bin puts it away.</summary>
        public bool IsPutAwayZone(Vector2 screenPos) =>
            FractionApp.I.Bin.Contains(screenPos) || !RectTransformUtility.RectangleContainsScreenPoint(Table, screenPos, null);

        Rect TableWorldRect()
        {
            var c = new Vector3[4];
            Table.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
        }

        public Vector3 ClampToTable(Vector3 pos, float marginRef)
        {
            var r = TableWorldRect();
            float m = marginRef * FractionApp.I.Scale;
            pos.x = r.xMax - r.xMin > 2 * m ? Mathf.Clamp(pos.x, r.xMin + m, r.xMax - m) : r.center.x;
            pos.y = r.yMax - r.yMin > 2 * m ? Mathf.Clamp(pos.y, r.yMin + m, r.yMax - m) : r.center.y;
            return pos;
        }

        /// <summary>First free spot on the table (left to right, top to bottom) for an item of the given radius.</summary>
        public Vector3 FindFreeSpot(float radiusRef)
        {
            var r = TableWorldRect();
            float s = FractionApp.I.Scale;
            float rad = radiusRef * s;
            float step = rad * 2.2f;
            float binGuard = 140f * s;
            for (float y = r.yMax - rad - 20 * s; y > r.yMin + rad; y -= step)
            for (float x = r.xMin + rad + 30 * s; x < r.xMax - rad; x += step)
            {
                var p = new Vector3(x, y, 0);
                if (x > r.xMax - binGuard - rad && y < r.yMin + binGuard + rad) continue; // keep the bin clear
                bool free = true;
                foreach (var i in Items)
                {
                    if (i.IsRemoving) continue;
                    if (Vector2.Distance(i.RestPosition, p) < rad * 1.9f) { free = false; break; }
                }
                if (free) return p;
            }
            return new Vector3(r.center.x + UnityEngine.Random.Range(-60f, 60f) * s, r.center.y + UnityEngine.Random.Range(-40f, 40f) * s, 0);
        }
    }

    /// <summary>
    /// Sits on the tray pieces and the card strip. Dragging it pulls out a fresh copy;
    /// tapping it sends a copy to a free spot on the table.
    /// </summary>
    public class SpawnSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public Func<DraggableItem> Factory;
        public float FootprintRadius = Theme.PieceRadius;

        public void OnBeginDrag(PointerEventData e)
        {
            var item = Factory();
            // Hand the drag over to the new item so it follows the finger.
            e.pointerDrag = item.gameObject;
            ExecuteEvents.Execute(item.gameObject, e, ExecuteEvents.beginDragHandler);
        }

        public void OnDrag(PointerEventData e) { }
        public void OnEndDrag(PointerEventData e) { }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging) return;
            SpawnTo(FractionApp.I.Workspace.FindFreeSpot(FootprintRadius));
        }

        /// <summary>Sends a fresh copy flying to <paramref name="target"/> (screen space).</summary>
        public DraggableItem SpawnTo(Vector3 target)
        {
            var item = Factory();
            item.Rect.SetAsLastSibling();
            AudioManager.Play(Sfx.Spawn, 0.5f);
            item.MoveTo(target, 0.35f, () => AudioManager.Play(Sfx.Drop, 0.5f));
            return item;
        }
    }

    /// <summary>The recycle bin: drop things on it, or tap twice to clear the table.</summary>
    public class TrashBin : MonoBehaviour, IPointerClickHandler
    {
        RectTransform rt;
        Image bg;
        GameObject hint;
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
