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
}
