using UnityEngine;
using UnityEngine.EventSystems;

namespace KonstructionUI
{
    public class DragWindow
        : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        [SerializeField]
        private KonstructorWindow window;

        public void OnDrag(PointerEventData eventData)
        {
            window.RectTransform.anchoredPosition
                += eventData.delta / window.Canvas.scaleFactor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            window.RectTransform.SetAsLastSibling();
        }
    }
}
