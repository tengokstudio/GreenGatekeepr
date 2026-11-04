using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Virtual joystick berbasis uGUI (mendukung mouse di Editor dan touch di device).
/// Nilai keluaran: Value (x = kanan, y = atas), panjang 0..1 setelah dead zone.
/// </summary>
[DisallowMultipleComponent]
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    [Tooltip("Area lingkaran joystick (RectTransform). Boleh dikosongkan, otomatis pakai RectTransform sendiri.")]
    public RectTransform background;

    [Tooltip("Knob yang ikut bergerak.")]
    public RectTransform handle;

    [Header("Tuning")]
    [Range(0f, 0.5f)] public float deadZone = 0.12f;

    [Tooltip("Seberapa jauh knob boleh bergeser dari pusat (fraksi radius background).")]
    [Range(0.2f, 1f)] public float handleRange = 0.72f;

    /// <summary>Input saat ini, x = horizontal, y = vertikal, magnitudo 0..1.</summary>
    public Vector2 Value { get; private set; }

    public bool IsPressed => pointerId >= 0;

    int pointerId = -1;
    Canvas canvas;

    void Awake()
    {
        if (background == null) background = transform as RectTransform;
        if (handle == null && background != null && background.childCount > 0)
            handle = background.GetChild(0) as RectTransform;

        canvas = GetComponentInParent<Canvas>();
        ResetHandle();
    }

    void OnDisable()
    {
        pointerId = -1;
        Value = Vector2.zero;
        ResetHandle();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (pointerId >= 0) return; // satu jari saja
        pointerId = eventData.pointerId;
        UpdateFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != pointerId) return;
        UpdateFromPointer(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != pointerId) return;
        pointerId = -1;
        Value = Vector2.zero;
        ResetHandle();
    }

    void UpdateFromPointer(PointerEventData eventData)
    {
        if (background == null) return;

        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, cam, out Vector2 local)) return;

        float radius = background.rect.width * 0.5f * handleRange;
        if (radius <= 0f) return;

        Vector2 clamped = Vector2.ClampMagnitude(local, radius);
        if (handle != null) handle.anchoredPosition = clamped;

        float magnitude = clamped.magnitude / radius;
        if (magnitude <= deadZone)
        {
            Value = Vector2.zero;
            return;
        }

        // Remap supaya di luar dead zone langsung terasa penuh.
        float remapped = Mathf.InverseLerp(deadZone, 1f, magnitude);
        Value = clamped.normalized * remapped;
    }

    void ResetHandle()
    {
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }
}
