using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FractionCircles
{
    /// <summary>
    /// Anything that lives on the wooden table: can be dragged (multi-touch friendly), tapped,
    /// dropped back on the tray / bin to put it away, and animated into place.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public abstract class DraggableItem : MonoBehaviour,
        IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public const int ScriptedPointer = -999;

        protected RectTransform rt;
        CanvasGroup group;
        Vector3 grabOffset;
        int pointerId = int.MinValue;
        Coroutine moveRoutine;
        Vector3 moveTarget;
        bool moving;

        protected float scaleTarget = 1f;

        public RectTransform Rect => rt;
        public bool IsDragging { get; private set; }
        public bool IsRemoving { get; private set; }

        /// <summary>Where the item is, or will be once its current move animation ends.</summary>
        public Vector3 RestPosition => moving ? moveTarget : rt.position;

        protected abstract float ClampMargin { get; }

        protected virtual void Awake()
        {
            rt = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
            FractionApp.I.Workspace.Register(this);
        }

        protected virtual void OnDestroy()
        {
            if (FractionApp.I) FractionApp.I.Workspace.Unregister(this);
        }

        protected virtual void Update()
        {
            float k = 1f - Mathf.Exp(-18f * Time.deltaTime);
            float s = Mathf.Lerp(rt.localScale.x, scaleTarget, k);
            rt.localScale = new Vector3(s, s, 1f);
        }

        // ---------- pointer events ----------

        public void OnPointerDown(PointerEventData e)
        {
            if (!IsDragging && !IsRemoving) rt.SetAsLastSibling();
        }

        public void OnBeginDrag(PointerEventData e) => BeginDrag(e.position, e.pointerId);

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId == pointerId) DragTo(e.position);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (e.pointerId == pointerId) EndDrag(e.position);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging || IsDragging || IsRemoving) return;
            Tap(e.button == PointerEventData.InputButton.Right);
        }

        // ---------- drag API (also used by the guided tour) ----------

        public void BeginDrag(Vector2 screenPos, int pointer = ScriptedPointer)
        {
            if (IsDragging || IsRemoving) return;
            IsDragging = true;
            pointerId = pointer;
            StopMove();
            rt.SetAsLastSibling();
            grabOffset = rt.position - (Vector3)screenPos;
            grabOffset.z = 0;
            scaleTarget = 1.06f;
            OnLifted(true);
            AudioManager.Play(Sfx.Pick, 0.55f);
        }

        public void DragTo(Vector2 screenPos)
        {
            if (!IsDragging) return;
            rt.position = (Vector3)screenPos + grabOffset;
            FractionApp.I.Bin.SetHover(FractionApp.I.Bin.Contains(screenPos));
        }

        public void EndDrag(Vector2 screenPos)
        {
            if (!IsDragging) return;
            IsDragging = false;
            pointerId = int.MinValue;
            scaleTarget = 1f;
            OnLifted(false);
            FractionApp.I.Bin.SetHover(false);

            var ws = FractionApp.I.Workspace;
            if (ws.IsPutAwayZone(screenPos))
            {
                AudioManager.Play(Sfx.PutAway, 0.7f);
                Remove();
                return;
            }

            rt.position = ws.ClampToTable(rt.position, ClampMargin);
            OnDropped();
        }

        public void Tap(bool alternate = false)
        {
            if (IsDragging || IsRemoving) return;
            OnTap(alternate);
        }

        // ---------- hooks ----------

        protected virtual void OnLifted(bool lifted) { }
        protected virtual void OnDropped() { }
        protected virtual void OnTap(bool alternate) { }

        // ---------- animation helpers ----------

        public void Pulse(float amount = 1.12f)
        {
            rt.localScale = new Vector3(amount, amount, 1f);
        }

        public void MoveTo(Vector3 target, float duration, Action done = null)
        {
            StopMove();
            moveRoutine = StartCoroutine(MoveRoutine(target, duration, done));
        }

        void StopMove()
        {
            if (moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = null;
            moving = false;
        }

        IEnumerator MoveRoutine(Vector3 target, float duration, Action done)
        {
            moving = true;
            moveTarget = target;
            Vector3 from = rt.position;
            for (float t = 0; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, duration))
            {
                float e = 1f - Mathf.Pow(1f - t, 3f);
                rt.position = Vector3.LerpUnclamped(from, target, e);
                yield return null;
            }
            rt.position = target;
            moving = false;
            moveRoutine = null;
            done?.Invoke();
        }

        public void Remove()
        {
            if (IsRemoving) return;
            IsRemoving = true;
            StopMove();
            group.blocksRaycasts = false;
            FractionApp.I.Workspace.Unregister(this);
            StartCoroutine(RemoveRoutine());
        }

        IEnumerator RemoveRoutine()
        {
            scaleTarget = 0.2f;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.18f)
            {
                group.alpha = 1f - t;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
