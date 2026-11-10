using UnityEngine;

/// <summary>
/// Menyesuaikan RectTransform dengan Screen.safeArea (notch / gesture bar Android).
/// Pasang di child Canvas yang membungkus seluruh UI.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform rectTransform;
    Rect lastSafeArea;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea != lastSafeArea) Apply();
    }

    void Apply()
    {
        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;

        if (Screen.width <= 0 || Screen.height <= 0) return;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
