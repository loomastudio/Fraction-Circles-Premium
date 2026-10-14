using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FractionCircles
{
    /// <summary>Keeps a RawImage's texture tiled at a fixed size whatever the rect size.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RawImage))]
    public class TiledRawImage : UIBehaviour
    {
        public float tileSize = 512f;

        protected override void Start() => Apply();
        protected override void OnRectTransformDimensionsChange() => Apply();

        void Apply()
        {
            var ri = GetComponent<RawImage>();
            var r = ((RectTransform)transform).rect;
            if (ri) ri.uvRect = new Rect(0, 0, r.width / tileSize, r.height / tileSize);
        }
    }
}
