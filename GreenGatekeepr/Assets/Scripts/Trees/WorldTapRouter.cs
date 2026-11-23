using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Menerjemahkan tap singkat di area swipe menjadi aksi dunia:
///  - [Isometric] tap grid saat edit mode -> tanam pohon.
///  - [FPS] tap mayat hama -> ambil ke inventory.
///
/// Dipasang di GameObject yang sama dengan <see cref="TouchSwipeArea"/>.
/// Tap dibedakan dari swipe lewat jarak & durasi, jadi menggeser kamera tidak
/// akan memicu penanaman atau pengambilan.
/// </summary>
[DisallowMultipleComponent]
public class WorldTapRouter : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Jarak maksimum jari bergeser (pixel) supaya masih dihitung tap.")]
    public float maxTapDistance = 28f;

    [Tooltip("Durasi maksimum tap (detik).")]
    public float maxTapDuration = 0.45f;

    CameraSwitcher switcher;
    int pointerId = -1;
    Vector2 startPosition;
    float startTime;

    void Awake()
    {
        switcher = FindFirstObjectByType<CameraSwitcher>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (pointerId >= 0) return;

        pointerId = eventData.pointerId;
        startPosition = eventData.position;
        startTime = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != pointerId) return;
        pointerId = -1;

        if ((eventData.position - startPosition).magnitude > maxTapDistance) return;
        if (Time.unscaledTime - startTime > maxTapDuration) return;

        if (switcher == null) switcher = FindFirstObjectByType<CameraSwitcher>();
        if (switcher == null) return;

        if (switcher.CurrentMode == CameraSwitcher.Mode.Isometric)
        {
            if (TreeEditController.Instance != null)
                TreeEditController.Instance.TryHandleTapFromScreen(eventData.position);
        }
        else
        {
            PestPickupController.TryPickupFromScreen(eventData.position, Camera.main, switcher);
        }
    }
}
