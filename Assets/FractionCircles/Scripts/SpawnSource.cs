using UnityEngine;
using UnityEngine.EventSystems;

namespace FractionCircles
{
    /// <summary>
    /// Sits on the tray pieces and the card strip. Dragging it pulls out a fresh copy;
    /// tapping it sends a copy to a free spot on the table.
    /// </summary>
    public class SpawnSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public enum Kind { Piece, Card }

        public Kind kind;
        [Header("Piece")] public int denominator = 1;
        public int index;
        public float angle;
        [Header("Card")] public CardKind cardKind;
        public string value;
        public float FootprintRadius = Theme.PieceRadius;

        public static SpawnSource ForPiece(GameObject host, int denominator, int index)
        {
            var s = host.AddComponent<SpawnSource>();
            s.kind = Kind.Piece;
            s.denominator = denominator;
            s.index = index;
            s.angle = -index * 360f / denominator;
            return s;
        }

        public static SpawnSource ForCard(GameObject host, CardKind cardKind, string value)
        {
            var s = host.AddComponent<SpawnSource>();
            s.kind = Kind.Card;
            s.cardKind = cardKind;
            s.value = value;
            s.FootprintRadius = cardKind == CardKind.FractionBar ? Theme.BarWidth * 0.5f : Theme.CardSize * 0.6f;
            return s;
        }

        /// <summary>A fresh copy of this piece or card, placed exactly over the source.</summary>
        public DraggableItem Factory() => kind == Kind.Piece
            ? (DraggableItem)FractionPiece.Create(denominator, transform.position, angle)
            : CardItem.Create(cardKind, value, transform.position);

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
}
