using UnityEngine;

/// <summary>
/// [General] Pos dekomposer. Berisi 3 kotak (<see cref="DecomposerBox"/>).
/// Tap pos / kotak untuk membuka panel dekomposer. Bisa dipakai di kedua mode kamera.
/// </summary>
[DisallowMultipleComponent]
public class DecomposerStation : MonoBehaviour
{
    [Header("References")]
    public DecomposerBox[] boxes;
    public DecomposerPanel panel;

    [Header("Interaksi")]
    public string stationName = "Dekomposer Hama";
    public float interactRadius = 8f;

    void Awake()
    {
        if (boxes == null || boxes.Length == 0)
            boxes = GetComponentsInChildren<DecomposerBox>(true);
        if (panel == null) panel = FindFirstObjectByType<DecomposerPanel>(FindObjectsInactive.Include);
    }

    public void Open(DecomposerBox focus = null)
    {
        if (panel == null) panel = FindFirstObjectByType<DecomposerPanel>(FindObjectsInactive.Include);
        if (panel == null) return;

        panel.Open(this, focus);
    }

    public static bool TryHandleTapFromScreen(Vector2 screenPosition, Camera camera, Transform player)
    {
        DecomposerStation[] stations = FindObjectsByType<DecomposerStation>(FindObjectsSortMode.None);
        if (stations.Length == 0) return false;

        if (camera == null) camera = Camera.main;
        if (camera == null) return false;

        DecomposerBox hitBox = null;
        DecomposerStation hitStation = null;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Ignore);
        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                DecomposerBox box = hits[i].collider.GetComponentInParent<DecomposerBox>();
                if (box != null)
                {
                    hitBox = box;
                    hitStation = box.GetComponentInParent<DecomposerStation>();
                    break;
                }

                DecomposerStation station = hits[i].collider.GetComponentInParent<DecomposerStation>();
                if (station != null)
                {
                    hitStation = station;
                    break;
                }
            }
        }

        if (hitStation == null)
        {
            // Fallback layar: tap dekat pos tetap membuka panel (misal kena daun/objek lain).
            for (int i = 0; i < stations.Length; i++)
            {
                DecomposerStation station = stations[i];
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
            ShowHint("Terlalu jauh dari dekomposer.", 2f);
            return true;
        }

        hitStation.Open(hitBox);
        return true;
    }

    static bool IsPlayerNear(DecomposerStation station, Transform player)
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
            Debug.Log("[Decomposer] " + message);
    }
}
