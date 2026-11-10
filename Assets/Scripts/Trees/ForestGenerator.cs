using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// [General] Menaruh 40 pohon acak dari 4 variasi di area 100x100 sekitar player.
/// Pohon yang dibuat ikut didaftarkan ke PestDefenseGame supaya bisa diserang hama.
///
/// Layout hanya diacak SEKALI. Seed dan daftar pohon yang sudah hancur disimpan di
/// PlayerPrefs per scene, jadi setiap run hutan yang sama muncul lagi di posisi yang sama.
/// </summary>
[DisallowMultipleComponent]
public class ForestGenerator : MonoBehaviour
{
    [Header("References")]
    public TreeCatalog catalog;
    public PestDefenseGame game;
    public Transform player;
    public Transform treesParent;

    [Header("Placement")]
    public int treeCount = 40;
    public float areaSize = 100f;
    public float minSpacing = 4f;
    public float minDistanceFromPlayer = 8f;
    public float minDistanceFromNest = 6f;
    public float minScale = 0.9f;
    public float maxScale = 1.15f;

    [Header("Persistence")]
    [Tooltip("Simpan layout hutan supaya tidak diacak ulang setiap run.")]
    public bool persistLayout = true;

    [Tooltip("0 = pilih seed otomatis sekali lalu disimpan. Isi manual untuk layout tetap.")]
    public int layoutSeed;

    readonly List<Vector3> usedPositions = new List<Vector3>();
    readonly HashSet<int> deadIndices = new HashSet<int>();

    bool generated;

    const string KeyPrefix = "GG.Forest.";

    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        if (generated) return;
        generated = true;

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
            if (parent == null) parent = new GameObject("ForestTrees");
            treesParent = parent.transform;
        }

        if (catalog == null || catalog.Count == 0)
        {
            Debug.LogWarning("[ForestGenerator] TreeCatalog kosong, tidak ada pohon random.");
            return;
        }

        int seed = ResolveSeed();
        System.Random rng = new System.Random(seed);

        Vector3 center = player != null ? player.position : transform.position;
        center.y = 0f;

        usedPositions.Clear();
        if (game != null)
        {
            IReadOnlyList<TreeDurability> existing = game.AllTrees;
            for (int i = 0; i < existing.Count; i++)
                if (existing[i] != null) usedPositions.Add(existing[i].transform.position);
        }

        PestSpawner[] spawners = FindObjectsByType<PestSpawner>(FindObjectsSortMode.None);
        List<Vector3> nests = new List<Vector3>();
        for (int i = 0; i < spawners.Length; i++) nests.Add(spawners[i].transform.position);

        int placed = 0;
        int created = 0;
        int attempts = 0;
        int maxAttempts = Mathf.Max(1, treeCount) * 40;

        while (placed < treeCount && attempts < maxAttempts)
        {
            attempts++;

            float x = center.x + Range(rng, -areaSize * 0.5f, areaSize * 0.5f);
            float z = center.z + Range(rng, -areaSize * 0.5f, areaSize * 0.5f);
            Vector3 position = new Vector3(x, 0f, z);

            if (HorizontalDistance(position, center) < minDistanceFromPlayer) continue;
            if (TooClose(position, usedPositions, minSpacing)) continue;
            if (TooClose(position, nests, minDistanceFromNest)) continue;

            int index = placed;
            placed++;

            // Sudah dihancurkan di run sebelumnya: tempatnya tetap dipakai supaya
            // layout pohon lain tidak bergeser, tapi pohonnya tidak dibuat lagi.
            if (deadIndices.Contains(index))
            {
                // Tetap konsumsi RNG untuk scale & yaw, supaya urutan acak pohon
                // sesudahnya tetap sama seperti saat layout pertama dibuat.
                Range(rng, minScale, maxScale);
                Range(rng, 0f, 360f);
                usedPositions.Add(position);
                continue;
            }

            TreeVariant variant = catalog.GetByIndex(index % catalog.Count);
            if (variant == null) continue;

            float scale = Range(rng, minScale, maxScale);
            float yaw = Range(rng, 0f, 360f);
            TreeDurability tree = TreeFactory.Create(variant, position, treesParent, false, scale, yaw);
            if (tree == null) continue;

            tree.name = "Forest_" + variant.id + "_" + index;
            if (game != null) game.RegisterTree(tree);

            int captured = index;
            if (tree.onDestroyed == null) tree.onDestroyed = new UnityEvent();
            tree.onDestroyed.AddListener(() => MarkTreeDead(captured));

            usedPositions.Add(position);
            created++;
        }

        Debug.Log("[ForestGenerator] " + created + " pohon random (" + areaSize + "x" + areaSize +
                  " area) dibuat dalam " + attempts + " percobaan. Seed " + seed +
                  ", tersimpan: " + deadIndices.Count + ".");
    }

    // ------------------------------------------------------------------
    // Persistence
    // ------------------------------------------------------------------
    int ResolveSeed()
    {
        if (!persistLayout)
            return layoutSeed != 0 ? layoutSeed : Random.Range(1, int.MaxValue);

        if (layoutSeed != 0)
        {
            PlayerPrefs.SetInt(SaveKey("seed"), layoutSeed);
            LoadDeadIndices();
            return layoutSeed;
        }

        if (PlayerPrefs.HasKey(SaveKey("seed")))
        {
            LoadDeadIndices();
            return PlayerPrefs.GetInt(SaveKey("seed"));
        }

        int seed = Random.Range(1, int.MaxValue);
        PlayerPrefs.SetInt(SaveKey("seed"), seed);
        PlayerPrefs.Save();
        return seed;
    }

    void LoadDeadIndices()
    {
        deadIndices.Clear();

        string raw = PlayerPrefs.GetString(SaveKey("dead"), string.Empty);
        if (string.IsNullOrEmpty(raw)) return;

        string[] parts = raw.Split(',');
        for (int i = 0; i < parts.Length; i++)
            if (int.TryParse(parts[i], out int index)) deadIndices.Add(index);
    }

    void MarkTreeDead(int index)
    {
        if (!persistLayout) return;
        if (!deadIndices.Add(index)) return;

        string raw = string.Join(",", deadIndices);
        PlayerPrefs.SetString(SaveKey("dead"), raw);
        PlayerPrefs.Save();
    }

    /// <summary>Hapus save layout hutan untuk scene aktif (menu Tools > Tree Edit > Reset Forest Save).</summary>
    public static void ResetSavedLayout()
    {
        PlayerPrefs.DeleteKey(SaveKey("seed"));
        PlayerPrefs.DeleteKey(SaveKey("dead"));
        PlayerPrefs.Save();
        Debug.Log("[ForestGenerator] Save layout hutan dihapus untuk scene " +
                  SceneManager.GetActiveScene().name + ".");
    }

    static string SaveKey(string suffix)
    {
        return KeyPrefix + SceneManager.GetActiveScene().name + "." + suffix;
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    static float Range(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    static bool TooClose(Vector3 position, List<Vector3> others, float minDistance)
    {
        float sqr = minDistance * minDistance;
        for (int i = 0; i < others.Count; i++)
            if ((others[i] - position).sqrMagnitude < sqr) return true;
        return false;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
