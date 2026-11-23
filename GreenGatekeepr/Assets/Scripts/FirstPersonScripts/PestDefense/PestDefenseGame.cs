using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manajer mekanik pembasmian hama:
///  - menyimpan daftar pohon & sarang,
///  - mencari pohon hidup terdekat untuk hama,
///  - mencatat hama yang hidup dan jumlah kill,
///  - menampilkan HUD (pohon, hama, kill, waktu).
/// </summary>
[DisallowMultipleComponent]
public class PestDefenseGame : MonoBehaviour
{
    public static PestDefenseGame Instance { get; private set; }

    [Header("References")]
    public TreeDurability[] trees;
    public PestSpawner[] spawners;
    public Transform pestParent;
    public GameObject pestPrefab;
    public Text hudText;

    [Header("Round")]
    [Tooltip("Durasi total spawner aktif (detik).")]
    public float roundDuration = 60f;

    readonly List<PestEnemy> pests = new List<PestEnemy>();
    readonly List<TreeDurability> allTrees = new List<TreeDurability>();

    int kills;
    float startTime;
    float pausedTime;
    float pauseStart;
    bool editMode;
    bool ready;

    public int PestsAlive => pests.Count;
    public int Kills => kills;
    public float Elapsed => ready ? Mathf.Max(0f, Time.time - startTime - pausedTime -
        (editMode ? Time.time - pauseStart : 0f)) : 0f;

    public IReadOnlyList<TreeDurability> AllTrees
    {
        get
        {
            EnsureTreeList();
            return allTrees;
        }
    }

    /// <summary>
    /// [Isometric] Saat edit mode aktif, spawner & AI hama berhenti dan timer round ikut pause.
    /// </summary>
    public bool EditMode
    {
        get => editMode;
        set
        {
            if (editMode == value) return;

            editMode = value;
            if (value) pauseStart = Time.time;
            else pausedTime += Time.time - pauseStart;
        }
    }

    public int TreesAlive
    {
        get
        {
            EnsureTreeList();

            int count = 0;
            for (int i = 0; i < allTrees.Count; i++)
                if (allTrees[i] != null && allTrees[i].IsAlive) count++;
            return count;
        }
    }

    public bool AllTreesDestroyed => TreesAlive == 0;
    public bool TimeUp => Elapsed >= roundDuration;

    void Awake()
    {
        Instance = this;
        EnsureTreeList();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        startTime = Time.time;
        ready = true;

        if (trees == null || trees.Length == 0)
            trees = FindObjectsByType<TreeDurability>(FindObjectsSortMode.None);

        EnsureTreeList();

        if (spawners == null || spawners.Length == 0)
            spawners = FindObjectsByType<PestSpawner>(FindObjectsSortMode.None);
    }

    void EnsureTreeList()
    {
        if (trees == null) return;

        for (int i = 0; i < trees.Length; i++)
        {
            TreeDurability tree = trees[i];
            if (tree != null && !allTrees.Contains(tree)) allTrees.Add(tree);
        }
    }

    /// <summary>Daftarkan pohon baru (forest generator / edit mode) supaya ikut dihitung & dicari hama.</summary>
    public void RegisterTree(TreeDurability tree)
    {
        EnsureTreeList();
        if (tree != null && !allTrees.Contains(tree)) allTrees.Add(tree);
    }

    /// <summary>
    /// Hapus pohon dari daftar. Dipakai saat pohon tanam player dihapus di edit mode,
    /// supaya tidak ikut dihitung sebagai pohon hidup.
    /// </summary>
    public void UnregisterTree(TreeDurability tree)
    {
        if (tree == null) return;

        allTrees.Remove(tree);

        if (trees == null) return;
        for (int i = 0; i < trees.Length; i++)
            if (trees[i] == tree) trees[i] = null;
    }

    void Update()
    {
        // Buang referensi hama yang sudah dihancurkan.
        for (int i = pests.Count - 1; i >= 0; i--)
            if (pests[i] == null || !pests[i].IsAlive) pests.RemoveAt(i);

        UpdateHud();
    }

    public void RegisterPest(PestEnemy pest)
    {
        if (pest == null || pests.Contains(pest)) return;
        pests.Add(pest);
    }

    public void UnregisterPest(PestEnemy pest)
    {
        if (pest == null) return;
        pests.Remove(pest);
    }

    public void OnPestKilled(PestEnemy pest)
    {
        kills++;
        if (pest != null) pests.Remove(pest);
    }

    public TreeDurability GetNearestTree(Vector3 from)
    {
        EnsureTreeList();

        TreeDurability best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < allTrees.Count; i++)
        {
            TreeDurability tree = allTrees[i];
            if (tree == null || !tree.IsAlive) continue;

            float distance = (tree.transform.position - from).sqrMagnitude;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            best = tree;
        }

        return best;
    }

    void UpdateHud()
    {
        if (hudText == null) return;

        EnsureTreeList();

        int totalTrees = allTrees.Count;
        int alive = TreesAlive;
        string time = FormatTime(Elapsed) + " / " + FormatTime(roundDuration);

        string state;
        if (EditMode) state = "  |  EDIT MODE";
        else if (AllTreesDestroyed) state = "  |  SEMUA POHON HANCUR";
        else if (TimeUp) state = "  |  SPAWN BERHENTI";
        else state = "";

        hudText.text = "POHON " + alive + "/" + totalTrees +
                       "   |   HAMA " + PestsAlive +
                       "   |   KILL " + kills +
                       "   |   " + time + state;
    }

    static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }
}
