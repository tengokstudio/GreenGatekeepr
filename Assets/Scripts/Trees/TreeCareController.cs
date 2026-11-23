using UnityEngine;

/// <summary>
/// [General] Membuka panel perawatan saat pohon tanam (bibit/tunas) di-tap.
/// Bekerja di mode isometrik maupun FPS, KECUALI saat edit mode aktif:
/// di edit mode tap hanya untuk menanam / menghapus pohon.
/// Pohon dewasa tidak membuka panel karena sudah menghasilkan komoditas otomatis.
/// </summary>
[DisallowMultipleComponent]
public class TreeCareController : MonoBehaviour
{
    public static TreeCareController Instance { get; private set; }

    [Header("References")]
    public CameraSwitcher switcher;
    public TreeCarePanel panel;
    public PestDefenseGame game;

    void Awake()
    {
        Instance = this;
        if (switcher == null) switcher = FindFirstObjectByType<CameraSwitcher>();
        if (game == null) game = PestDefenseGame.Instance;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void OpenFor(TreeGrowth growth)
    {
        if (growth == null) return;
        if (panel == null) panel = FindFirstObjectByType<TreeCarePanel>(FindObjectsInactive.Include);
        if (panel == null) return;

        panel.Open(growth);
    }

    /// <summary>Dipanggil WorldTapRouter. Return true kalau tap sudah ditangani.</summary>
    public static bool TryHandleTapFromScreen(Vector2 screenPosition)
    {
        TreeCareController controller = Instance;
        if (controller == null)
        {
            controller = FindFirstObjectByType<TreeCareController>(FindObjectsInactive.Include);
            if (controller == null) return false;
        }

        return controller.HandleTap(screenPosition);
    }

    bool HandleTap(Vector2 screenPosition)
    {
        // [Isometric] Edit mode: jangan pernah buka panel rawat.
        // Tap hanya untuk menanam atau menghapus pohon.
        if (TreeEditController.Instance != null && TreeEditController.Instance.IsEditing)
            return false;

        Camera camera = Camera.main;
        if (camera == null) return false;

        TreeGrowth growth = null;
        TreeDurability tree = null;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, ~0, QueryTriggerInteraction.Ignore))
        {
            // Tap pada mayat hama jangan dianggap tap pohon; biar PestPickupController
            // yang mengambilnya.
            if (hit.collider.GetComponentInParent<PestEnemy>() != null)
                return false;

            tree = hit.collider.GetComponentInParent<TreeDurability>();
            if (tree != null) growth = tree.GetComponent<TreeGrowth>();
        }

        // Daun tidak punya collider, jadi cari pohon tanam terdekat di layar.
        if (tree == null)
        {
            tree = FindPlantedTreeNearScreen(camera, screenPosition, MaxPickPixels(camera));
            if (tree != null) growth = tree.GetComponent<TreeGrowth>();
        }

        if (tree == null || !tree.IsAlive) return false;

        if (growth == null)
        {
            // Pohon hutan / defense: bukan objek perawatan.
            return false;
        }

        if (growth.IsMature)
        {
            ShowHint("Pohon dewasa. Hasil panen masuk otomatis tiap " +
                     Mathf.RoundToInt(growth.commodityInterval) + " detik.", 3f);
            return true;
        }

        OpenFor(growth);
        return true;
    }

    static float MaxPickPixels(Camera camera)
    {
        if (camera != null && camera.orthographic)
        {
            float pixelsPerUnit = Screen.height / Mathf.Max(0.01f, 2f * camera.orthographicSize);
            return Mathf.Clamp(pixelsPerUnit * 1.1f, 10f, 180f);
        }

        return Mathf.Clamp(Screen.height * 0.06f, 20f, 180f);
    }

    TreeDurability FindPlantedTreeNearScreen(Camera camera, Vector2 screenPosition, float maxPixels)
    {
        if (game == null) game = PestDefenseGame.Instance;
        if (game == null) return null;

        float bestSqr = maxPixels * maxPixels;
        TreeDurability best = null;

        var trees = game.AllTrees;
        for (int i = 0; i < trees.Count; i++)
        {
            TreeDurability tree = trees[i];
            if (tree == null || !tree.plantedByPlayer || !tree.IsAlive) continue;
            if (tree.GetComponent<TreeGrowth>() == null) continue;

            Vector3 screenPoint = camera.WorldToScreenPoint(tree.transform.position + Vector3.up * 1.4f);
            if (screenPoint.z <= 0f) continue;

            float dx = screenPoint.x - screenPosition.x;
            float dy = screenPoint.y - screenPosition.y;
            float sqr = dx * dx + dy * dy;
            if (sqr > bestSqr) continue;

            bestSqr = sqr;
            best = tree;
        }

        return best;
    }

    static void ShowHint(string message, float duration)
    {
        if (TreeEditController.Instance != null)
            TreeEditController.Instance.ShowHint(message, duration);
        else
            Debug.Log("[TreeCare] " + message);
    }
}
