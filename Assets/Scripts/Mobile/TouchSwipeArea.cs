using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Area transparan penuh layar untuk menangkap swipe (satu jari).
/// Dipakai untuk menggeser kamera (isometric) atau memutar kamera (first-person).
/// </summary>
[DisallowMultipleComponent]
public class TouchSwipeArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Tooltip("Rig kamera yang menerima input swipe.")]
    public MobileCameraRig rig;

    [Tooltip("Pengali sensitivitas swipe tambahan.")]
    [Range(0.1f, 3f)] public float sensitivityScale = 1f;

    int pointerId = -1;
    Vector2 lastPosition;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (pointerId >= 0) return;
        pointerId = eventData.pointerId;
        lastPosition = eventData.position;
        if (rig != null) rig.OnSwipeStart();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != pointerId) return;

        Vector2 delta = eventData.position - lastPosition;
        lastPosition = eventData.position;
        if (rig != null) rig.OnSwipe(delta * sensitivityScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != pointerId) return;
        pointerId = -1;
        if (rig != null) rig.OnSwipeEnd();
    }
}
