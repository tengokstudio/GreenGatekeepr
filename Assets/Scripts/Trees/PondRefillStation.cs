using UnityEngine;

/// <summary>
/// [General] Kolam tempat mengisi ulang Penyiram.
///
/// Men-tap kolam (isometrik maupun FPS) mengisi air Penyiram sampai penuh.
/// Komponen ini menambahkan BoxCollider sendiri kalau belum ada, supaya tap
/// bisa mengenai model kolam yang aslinya tidak punya collider.
/// </summary>
[DisallowMultipleComponent]
public class PondRefillStation : MonoBehaviour
{
    [Header("Interaksi")]
    [Tooltip("Jarak maksimum pemain dari kolam. 0 = tidak ada batas jarak.")]
    public float interactRadius = 0f;

    [Tooltip("Tinggi titik tap (meter di atas pivot kolam).")]
    public float tapHeight = 1.4f;

    void Awake()
    {
        EnsureCollider();
    }

    void EnsureCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
        {
            RebuildCollider();
            return;
        }

        if (box.size.y < 0.05f) RebuildCollider();
    }

    /// <summary>
    /// Bentuk ulang collider mengikuti bounds renderer kolam.
    /// Dipanggil dari tool setup editor dan saat Awake kalau collider belum ada.
    /// </summary>
    public void RebuildCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) box = gameObject.AddComponent<BoxCollider>();

        box.isTrigger = false;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            box.center = new Vector3(0f, 1.2f, 0f);
            box.size = new Vector3(3f, 2.4f, 3f);
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            if (renderers[i] != null) bounds.Encapsulate(renderers[i].bounds);

        box.center = transform.InverseTransformPoint(bounds.center);

        Vector3 size = transform.InverseTransformVector(bounds.size);
        box.size = new Vector3(
            Mathf.Max(0.5f, Mathf.Abs(size.x)) + 0.2f,
            Mathf.Max(0.5f, Mathf.Abs(size.y)) + 0.2f,
            Mathf.Max(0.5f, Mathf.Abs(size.z)) + 0.2f);
    }

    void Refill()
    {
        WateringCanSupply supply = WateringCanSupply.Ensure();
        if (supply == null) return;

        if (supply.IsFull)
        {
            ShowHint("Penyiram sudah penuh (" + supply.Label + " air).", 2.2f);
            return;
        }

        supply.Refill();
        ShowHint("Penyiram diisi ulang. Air: " + supply.Label + ".", 2.6f);
    }

    /// <summary>Dipanggil WorldTapRouter. Return true kalau tap sudah ditangani.</summary>
    public static bool TryHandleTapFromScreen(Vector2 screenPosition, Camera camera, Transform player)
    {
        PondRefillStation[] stations =
            FindObjectsByType<PondRefillStation>(FindObjectsSortMode.None);
        if (stations.Length == 0) return false;

        if (camera == null) camera = Camera.main;
        if (camera == null) return false;

        PondRefillStation hitStation = null;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Collide);
        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null) continue;
                PondRefillStation station = hits[i].collider.GetComponentInParent<PondRefillStation>();
                if (station == null) continue;

                hitStation = station;
                break;
            }
        }

        // Cadangan: tap di sekitar kolam di layar tetap dianggap kena kolam.
        if (hitStation == null)
        {
            float maxPixels = MaxPickPixels(camera);

            for (int i = 0; i < stations.Length; i++)
            {
                PondRefillStation station = stations[i];
                if (station == null) continue;

                Vector3 screenPoint = camera.WorldToScreenPoint(
                    station.transform.position + Vector3.up * station.tapHeight);
                if (screenPoint.z <= 0f) continue;

                float dx = screenPoint.x - screenPosition.x;
                float dy = screenPoint.y - screenPosition.y;
                if (dx * dx + dy * dy > maxPixels * maxPixels) continue;

                hitStation = station;
                break;
            }
        }

        if (hitStation == null) return false;

        if (player != null && hitStation.interactRadius > 0f)
        {
            Vector3 delta = hitStation.transform.position - player.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > hitStation.interactRadius * hitStation.interactRadius)
            {
                ShowHint("Terlalu jauh dari kolam.", 2f);
                return true;
            }
        }

        hitStation.Refill();
        return true;
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
            Debug.Log("[Pond] " + message);
    }
}
