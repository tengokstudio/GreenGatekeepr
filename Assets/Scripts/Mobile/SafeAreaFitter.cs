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
    Vector2Int lastScreenSize;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        // Layar juga bisa berubah (rotasi / resize Game view), bukan hanya safe area.
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        if (Screen.safeArea != lastSafeArea || screenSize != lastScreenSize) Apply();
    }

    void Apply()
    {
        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        if (Screen.width <= 0 || Screen.height <= 0) return;

        // Di Editor Game view, Screen.safeArea kadang dilaporkan memakai resolusi
        // device simulator (contoh 2204x1080) sementara Screen.width/height mengikuti
        // ukuran Game view (contoh 902x313). Kalau dipakai apa adanya, rect SafeArea
        // membengkak berkali-kali lipat dan UI yang di-anchor kanan/atas terdorong
        // keluar layar (tombol attack tidak kelihatan). Kalau tidak konsisten, pakai
        // layar penuh saja.
        const float tolerance = 1f;
        bool valid = safeArea.width > 0f && safeArea.height > 0f
                     && safeArea.xMin >= -tolerance
                     && safeArea.yMin >= -tolerance
                     && safeArea.xMax <= Screen.width + tolerance
                     && safeArea.yMax <= Screen.height + tolerance;

        Vector2 anchorMin;
        Vector2 anchorMax;
        if (valid)
        {
            anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
        }
        else
        {
            anchorMin = Vector2.zero;
            anchorMax = Vector2.one;
        }

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
