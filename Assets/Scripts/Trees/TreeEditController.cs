using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [Isometric] Edit mode untuk menata pohon.
///  - Tombol EDIT di kanan atas (hanya tampil di mode isometrik).
///  - Grid samar mengikuti fokus kamera (bukan player) supaya area tanam bisa digeser bebas.
///  - Pilih item pohon di inventory, lalu tap sel grid untuk menanam (stok -1).
///  - Tap pohon yang ditanam untuk menghapusnya dan mengembalikan 1 item ke inventory.
///  - Selama edit mode, spawner & AI hama di-pause lewat <see cref="PestDefenseGame.EditMode"/>.
/// </summary>
[DisallowMultipleComponent]
public class TreeEditController : MonoBehaviour
{
    public static TreeEditController Instance { get; private set; }

    [Header("References")]
    public CameraSwitcher switcher;
    public MobileCameraRig rig;
    public InventoryUI inventoryUI;
    public InventorySystem inventory;
    public TreeCatalog catalog;
    public GridOverlay grid;
    public PestDefenseGame game;
    public Transform player;
    public Transform treesParent;

    [Header("UI")]
    public Button editButton;
    public Text buttonLabel;
    public GameObject hintRoot;
    public Text hintText;

    [Header("Placement")]
    [Tooltip("Ukuran area tanam (mengikuti grid 100x100).")]
    public float areaSize = 100f;
    public float cellSize = 2f;
    public float minTreeSpacing = 1.8f;
    public float minDistanceFromPlayer = 2f;

    [Tooltip("Lama pesan hint tampil (detik).")]
    public float hintDuration = 2.5f;

    bool editing;
    float hintUntil;
    string defaultHint = "Pilih pohon di inventory, lalu tap grid untuk menanam. Tap pohon tanam untuk menghapus.";

    public bool IsEditing => editing;

    void Awake()
    {
        Instance = this;

        if (switcher == null) switcher = FindFirstObjectByType<CameraSwitcher>();
        if (rig == null) rig = FindFirstObjectByType<MobileCameraRig>();
        if (inventoryUI == null) inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventory == null) inventory = InventorySystem.Instance;
        if (catalog == null) catalog = FindFirstObjectByType<TreeCatalog>();
        if (game == null) game = PestDefenseGame.Instance;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }

        if (treesParent == null)
        {
            GameObject parent = GameObject.Find("PestDefense/Trees");
            if (parent != null) treesParent = parent.transform;
        }
    }

    void OnDestroy()
    {
        if (inventoryUI != null) inventoryUI.OnSelectionChanged -= HandleSelectionChanged;
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (inventory == null) inventory = InventorySystem.Instance;
        if (inventoryUI != null) inventoryUI.OnSelectionChanged += HandleSelectionChanged;

        SetHintVisible(false);
        UpdateButtonLabel();
        UpdateButtonVisibility();
    }

    void Update()
    {
        if (switcher == null) switcher = FindFirstObjectByType<CameraSwitcher>();

        // Edit mode hanya untuk isometrik: pindah ke FPS otomatis menutup edit mode.
        if (editing && switcher != null && switcher.CurrentMode != CameraSwitcher.Mode.Isometric)
            ExitEditMode();

        if (editing && grid != null)
        {
            Vector3 center = rig != null ? rig.FocusPoint : (player != null ? player.position : grid.transform.position);
            grid.SetCenter(center);
        }

        if (hintRoot != null && hintRoot.activeSelf && Time.unscaledTime >= hintUntil)
            SetHintVisible(false);

        UpdateButtonVisibility();
    }

    // ------------------------------------------------------------------
    // Tombol EDIT
    // ------------------------------------------------------------------
    public void ToggleEditMode()
    {
        if (editing)
        {
            ExitEditMode();
            return;
        }

        if (switcher != null && switcher.CurrentMode != CameraSwitcher.Mode.Isometric)
        {
            ShowHint("Edit pohon hanya bisa di mode isometrik.", 2.5f);
            return;
        }

        EnterEditMode();
    }

    void EnterEditMode()
    {
        editing = true;

        if (game == null) game = PestDefenseGame.Instance;
        if (game != null) game.EditMode = true;

        // [Isometric] Kamera lepas dari player selama edit mode.
        if (rig == null) rig = FindFirstObjectByType<MobileCameraRig>();
        if (rig != null) rig.SetFreePan(true);

        if (grid != null)
        {
            Vector3 center = rig != null ? rig.FocusPoint : (player != null ? player.position : grid.transform.position);
            grid.SetCenter(center);
            grid.SetVisible(true);
        }

        UpdateButtonLabel();
        ShowHint("EDIT MODE: pilih pohon lalu tap grid. Tap pohon tanam untuk hapus.", 3f);

        // Langsung buka inventory supaya player tinggal memilih variasi pohon.
        if (inventoryUI != null && !inventoryUI.IsOpen) inventoryUI.Open();
    }

    void ExitEditMode()
    {
        if (!editing) return;
        editing = false;

        if (game == null) game = PestDefenseGame.Instance;
        if (game != null) game.EditMode = false;

        if (rig != null) rig.SetFreePan(false);

        if (grid != null) grid.SetVisible(false);

        if (inventoryUI != null && inventoryUI.IsOpen) inventoryUI.Close();

        UpdateButtonLabel();
        ShowHint("Edit mode selesai.", 1.8f);
    }

    void UpdateButtonLabel()
    {
        if (buttonLabel != null) buttonLabel.text = editing ? "SELESAI" : "EDIT";
    }

    void UpdateButtonVisibility()
    {
        if (editButton == null || switcher == null) return;

        bool isometric = switcher.CurrentMode == CameraSwitcher.Mode.Isometric;
        if (editButton.gameObject.activeSelf != isometric)
            editButton.gameObject.SetActive(isometric);
    }

    // ------------------------------------------------------------------
    // Tap: hapus pohon tanam, lalu tanam baru
    // ------------------------------------------------------------------
    /// <summary>
    /// Dipanggil oleh WorldTapRouter saat grid di-tap di mode isometrik.
    /// Prioritas: tap pohon yang ditanam -> hapus ke inventory. Kalau tidak kena pohon, tanam.
    /// </summary>
    public bool TryHandleTapFromScreen(Vector2 screenPosition)
    {
        if (!editing) return false;
        if (switcher != null && switcher.CurrentMode != CameraSwitcher.Mode.Isometric) return false;
        if (inventoryUI != null && inventoryUI.IsOpen) return false;

        if (TryRemovePlantedTree(screenPosition)) return true;
        return TryPlaceFromScreen(screenPosition);
    }

    bool TryRemovePlantedTree(Vector2 screenPosition)
    {
        Camera camera = Camera.main;
        if (camera == null) return false;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        TreeDurability tree = null;

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, ~0, QueryTriggerInteraction.Ignore))
            tree = hit.collider.GetComponentInParent<TreeDurability>();

        // Daun tidak punya collider, jadi tap yang kena kanopi tidak mengenai apa pun.
        // Cadangkan: cari pohon tanam yang paling dekat dengan titik tap di layar.
        if (tree == null)
            tree = FindPlantedTreeNearScreen(camera, screenPosition, MaxPickPixels(camera));

        if (tree == null || !tree.plantedByPlayer || !tree.IsAlive) return false;

        RemovePlantedTree(tree);
        return true;
    }

    /// <summary>
    /// Radius tap (pixel) yang masih dianggap kena pohon. Dihitung dari ukuran
    /// kamera supaya tetap akurat di resolusi layar apa pun.
    /// </summary>
    static float MaxPickPixels(Camera camera)
    {
        if (camera != null && camera.orthographic)
        {
            float pixelsPerUnit = Screen.height / Mathf.Max(0.01f, 2f * camera.orthographicSize);
            return Mathf.Clamp(pixelsPerUnit * 0.9f, 10f, 160f);
        }

        return Mathf.Clamp(Screen.height * 0.05f, 20f, 160f);
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

    void RemovePlantedTree(TreeDurability tree)
    {
        string itemId = tree.lootItemId;

        if (game == null) game = PestDefenseGame.Instance;
        if (game != null) game.UnregisterTree(tree);

        if (!string.IsNullOrEmpty(itemId))
        {
            if (inventory == null) inventory = InventorySystem.Instance;
            if (inventory != null) inventory.AddItem(itemId, 1);
        }

        if (catalog == null) catalog = FindFirstObjectByType<TreeCatalog>();
        TreeVariant variant = catalog != null ? catalog.Get(itemId) : null;
        string label = variant != null ? variant.displayName : itemId;

        ShowHint("Dihapus: " + label + " (kembali ke inventory).", 2f);

        StartCoroutine(ShrinkAndRemove(tree));
    }

    IEnumerator ShrinkAndRemove(TreeDurability tree)
    {
        if (tree == null) yield break;

        Collider[] colliders = tree.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = false;

        Vector3 startScale = tree.transform.localScale;
        float time = 0f;
        const float duration = 0.18f;

        while (time < duration && tree != null)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            tree.transform.localScale = Vector3.Lerp(startScale, startScale * 0.6f, t);
            yield return null;
        }

        if (tree != null) Destroy(tree.gameObject);
    }

    // ------------------------------------------------------------------
    // Penanaman
    // ------------------------------------------------------------------
    /// <summary>Dipanggil oleh WorldTapRouter saat grid di-tap di mode isometrik.</summary>
    public bool TryPlaceFromScreen(Vector2 screenPosition)
    {
        if (!editing) return false;
        if (switcher != null && switcher.CurrentMode != CameraSwitcher.Mode.Isometric) return false;
        if (inventoryUI != null && inventoryUI.IsOpen) return false;

        if (inventory == null) inventory = InventorySystem.Instance;
        if (inventory == null) return false;

        string itemId = inventoryUI != null ? inventoryUI.SelectedItemId : null;
        if (string.IsNullOrEmpty(itemId))
        {
            ShowHint("Pilih pohon di inventory dulu, lalu tap grid.", 2.5f);
            return false;
        }

        if (catalog == null) catalog = FindFirstObjectByType<TreeCatalog>();
        TreeVariant variant = catalog != null ? catalog.Get(itemId) : null;
        if (variant == null)
        {
            ShowHint("Item ini bukan pohon. Pilih salah satu variasi pohon.", 2.5f);
            return false;
        }

        Camera camera = Camera.main;
        if (camera == null) return false;

        Plane ground = new Plane(Vector3.up, Vector3.zero);
        Ray ray = camera.ScreenPointToRay(screenPosition);
        if (!ground.Raycast(ray, out float distance)) return false;

        Vector3 point = ray.GetPoint(distance);
        Vector3 position = SnapToGrid(point);

        if (!InsideArea(position))
        {
            ShowHint("Di luar area tanam (100x100).", 2f);
            return false;
        }

        if (TooCloseToTree(position))
        {
            ShowHint("Terlalu dekat dengan pohon lain.", 2f);
            return false;
        }

        if (player != null &&
            HorizontalDistance(player.position, position) < minDistanceFromPlayer)
        {
            ShowHint("Terlalu dekat dengan pemain.", 2f);
            return false;
        }

        if (!inventory.RemoveItem(itemId, 1))
        {
            ShowHint("Stok pohon habis.", 2f);
            return false;
        }

        if (treesParent == null)
        {
            GameObject parent = GameObject.Find("PestDefense/Trees");
            treesParent = parent != null ? parent.transform : transform;
        }

        TreeDurability tree = TreeFactory.Create(variant, position, treesParent, false, 1f,
            Random.Range(0f, 360f));
        if (tree == null) return false;

        tree.name = "Planted_" + variant.id;
        tree.plantedByPlayer = true;

        if (game == null) game = PestDefenseGame.Instance;
        if (game != null) game.RegisterTree(tree);

        int left = inventory.CountItem(itemId);
        ShowHint(left > 0
            ? "Ditanam: " + variant.displayName + " (sisa " + left + ")"
            : "Ditanam: " + variant.displayName + ". Stok habis.", 2f);

        return true;
    }

    Vector3 SnapToGrid(Vector3 point)
    {
        float size = cellSize > 0.01f ? cellSize : 2f;
        float x = Mathf.Round(point.x / size) * size;
        float z = Mathf.Round(point.z / size) * size;
        return new Vector3(x, 0f, z);
    }

    bool InsideArea(Vector3 position)
    {
        if (player == null) return true;

        float half = Mathf.Max(1f, areaSize) * 0.5f;

        // Pusat area mengikuti grid (yang sudah di-snap), jadi area tanam selalu
        // sama persis dengan grid yang terlihat.
        Vector3 center = grid != null ? grid.transform.position : player.position;

        return Mathf.Abs(position.x - center.x) <= half &&
               Mathf.Abs(position.z - center.z) <= half;
    }

    bool TooCloseToTree(Vector3 position)
    {
        if (game == null) game = PestDefenseGame.Instance;
        if (game == null) return false;

        float minSqr = minTreeSpacing * minTreeSpacing;
        var trees = game.AllTrees;
        for (int i = 0; i < trees.Count; i++)
        {
            TreeDurability tree = trees[i];
            if (tree == null) continue;

            Vector3 delta = tree.transform.position - position;
            delta.y = 0f;
            if (delta.sqrMagnitude < minSqr) return true;
        }

        return false;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    // ------------------------------------------------------------------
    // Hint / toast
    // ------------------------------------------------------------------
    void HandleSelectionChanged(string itemId)
    {
        if (!editing) return;

        if (string.IsNullOrEmpty(itemId))
        {
            ShowHint(defaultHint, 2.5f);
            return;
        }

        if (catalog == null) catalog = FindFirstObjectByType<TreeCatalog>();
        TreeVariant variant = catalog != null ? catalog.Get(itemId) : null;

        if (variant == null)
        {
            ShowHint("Item ini bukan pohon. Pilih salah satu variasi pohon.", 2.5f);
            return;
        }

        int count = inventory != null ? inventory.CountItem(itemId) : 0;
        ShowHint("Tap grid untuk menanam " + variant.displayName + " (stok " + count + ").", 3f);

        // Inventory menutupi layar (dim), jadi tutup otomatis setelah memilih pohon.
        if (inventoryUI != null && inventoryUI.IsOpen) inventoryUI.Close();
    }

    public void ShowHint(string message, float duration)
    {
        if (hintText != null) hintText.text = message;
        if (hintRoot != null) hintRoot.SetActive(true);
        hintUntil = Time.unscaledTime + Mathf.Max(0.5f, duration);
    }

    void SetHintVisible(bool visible)
    {
        if (hintRoot != null) hintRoot.SetActive(visible);
    }
}
