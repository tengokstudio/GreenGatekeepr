using UnityEngine;

/// <summary>
/// [General] Pos toko. Tap untuk membuka panel jual/beli.
/// Bisa dipakai di mode isometrik maupun FPS.
/// </summary>
[DisallowMultipleComponent]
public class ShopStation : MonoBehaviour
{
    [Header("References")]
    public ShopPanel panel;

    [Header("Interaksi")]
    public string stationName = "Toko";
    public float interactRadius = 8f;

    void Awake()
    {
        if (panel == null) panel = FindFirstObjectByType<ShopPanel>(FindObjectsInactive.Include);
    }

    public void Open()
    {
        if (panel == null) panel = FindFirstObjectByType<ShopPanel>(FindObjectsInactive.Include);
        if (panel == null) return;

        panel.Open(this);
    }

    public static bool TryHandleTapFromScreen(Vector2 screenPosition, Camera camera, Transform player)
    {
        ShopStation[] stations = FindObjectsByType<ShopStation>(FindObjectsSortMode.None);
        if (stations.Length == 0) return false;

        if (camera == null) camera = Camera.main;
        if (camera == null) return false;

        ShopStation hitStation = null;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Ignore);
        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                ShopStation station = hits[i].collider.GetComponentInParent<ShopStation>();
                if (station == null) continue;

                hitStation = station;
                break;
            }
        }

        if (hitStation == null)
        {
            for (int i = 0; i < stations.Length; i++)
            {
                ShopStation station = stations[i];
                if (station == null) continue;

                float maxPixels = MaxPickPixels(camera);
                Vector3 screenPoint = camera.WorldToScreenPoint(station.transform.position + Vector3.up * 1.2f);
                if (screenPoint.z <= 0f) continue;

                float dx = screenPoint.x - screenPosition.x;
                float dy = screenPoint.y - screenPosition.y;
                if (dx * dx + dy * dy > maxPixels * maxPixels) continue;

                hitStation = station;
                break;
            }
        }

        if (hitStation == null) return false;

        if (!IsPlayerNear(hitStation, player))
        {
            ShowHint("Terlalu jauh dari toko.", 2f);
            return true;
        }

        hitStation.Open();
        return true;
    }

    static bool IsPlayerNear(ShopStation station, Transform player)
    {
        if (player == null) return true;

        Vector3 a = station.transform.position;
        Vector3 b = player.position;
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        float radius = Mathf.Max(1f, station.interactRadius);
        return dx * dx + dz * dz <= radius * radius;
    }

    static float MaxPickPixels(Camera camera)
    {
        if (camera != null && camera.orthographic)
        {
            float pixelsPerUnit = Screen.height / Mathf.Max(0.01f, 2f * camera.orthographicSize);
            return Mathf.Clamp(pixelsPerUnit * 1.4f, 16f, 200f);
        }

        return Mathf.Clamp(Screen.height * 0.09f, 30f, 220f);
    }

    static void ShowHint(string message, float duration)
    {
        if (TreeEditController.Instance != null)
            TreeEditController.Instance.ShowHint(message, duration);
        else
            Debug.Log("[Shop] " + message);
    }
}
