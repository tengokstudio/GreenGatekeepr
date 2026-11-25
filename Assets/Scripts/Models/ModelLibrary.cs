using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [General] Akses ke model 3D low-poly yang sudah dinormalisasi pivot-nya.
/// Prefab disimpan di <c>Resources/Models</c> supaya bisa dimuat dari mana saja
/// tanpa referensi scene. Kalau prefab belum dibuat (menu Tools &gt; Models &gt;
/// Rebuild Model Prefabs), semua helper di sini mengembalikan null dan pemanggil
/// harus punya fallback prosedural.
/// </summary>
public static class ModelLibrary
{
    public const string Folder = "Models/";

    static readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
    static readonly Dictionary<string, float> heightCache = new Dictionary<string, float>();

    // ------------------------------------------------------------------
    // Kunci model yang dipakai game.
    // ------------------------------------------------------------------
    public const string ShopModel = "shop_toko";
    public const string DecomposerModel = "shop_komposer";
    public const string GroundModel = "ground_main";
    public const string GroundPatchModel = "ground_patch";
    public const string GrassScatter = "scatter_grass";
    public const string RockScatter = "scatter_rocks";
    public const string LogScatter = "scatter_logs";
    public const string PondModel = "prop_pond";
    public const string WateringCanModel = "prop_watering_can";
    public const string SprayModel = "prop_spray";
    public const string LampModel = "prop_lamp";

    public static string TreeKey(string family, int stage)
    {
        return "tree_" + family + "_" + Mathf.Clamp(stage, 1, 3);
    }

    public static string PalmKey(int size)
    {
        return TreeKey("sawit", size);
    }

    // ------------------------------------------------------------------
    // Load / spawn
    // ------------------------------------------------------------------
    public static GameObject Load(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (prefabCache.TryGetValue(key, out GameObject cached)) return cached;

        GameObject prefab = Resources.Load<GameObject>(Folder + key);
        prefabCache[key] = prefab;
        return prefab;
    }

    public static bool Has(string key)
    {
        return Load(key) != null;
    }

    public static GameObject Spawn(string key, Transform parent, Vector3 localPosition,
        float yaw = 0f, float scale = 1f)
    {
        GameObject prefab = Load(key);
        if (prefab == null) return null;

        GameObject instance = Object.Instantiate(prefab, parent);
        instance.name = prefab.name;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        instance.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        return instance;
    }

    /// <summary>Tinggi model (satuan dunia) dari prefab, sudah di-cache.</summary>
    public static float GetHeight(string key)
    {
        if (heightCache.TryGetValue(key, out float cached)) return cached;

        float height = 1f;
        GameObject prefab = Load(key);
        if (prefab != null)
        {
            Bounds bounds = Measure(prefab);
            if (bounds.size.y > 0.001f) height = bounds.size.y;
        }

        heightCache[key] = height;
        return height;
    }

    /// <summary>Ukuran footprint (x, z) model untuk collider dan penempatan.</summary>
    public static Vector2 GetFootprint(string key)
    {
        GameObject prefab = Load(key);
        if (prefab == null) return Vector2.one;

        Bounds bounds = Measure(prefab);
        return new Vector2(Mathf.Max(0.1f, bounds.size.x), Mathf.Max(0.1f, bounds.size.z));
    }

    /// <summary>Prefab 3 ukuran pohon untuk satu famili, atau null kalau tidak lengkap.</summary>
    public static GameObject[] GetTreeStages(string family)
    {
        if (string.IsNullOrEmpty(family)) return null;

        GameObject[] stages = new GameObject[3];
        for (int i = 0; i < stages.Length; i++)
        {
            stages[i] = Load(TreeKey(family, i + 1));
            if (stages[i] == null) return null;
        }

        return stages;
    }

    /// <summary>Buang cache supaya prefab yang baru dibuat langsung terpakai.</summary>
    public static void ClearCache()
    {
        prefabCache.Clear();
        heightCache.Clear();
    }

    // ------------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------------
    static Bounds Measure(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    /// <summary>Tinggi gabungan renderer aktif (dipakai komponen tahap pohon).</summary>
    public static float MeasureHeight(GameObject root)
    {
        if (root == null) return 1f;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return 1f;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return Mathf.Max(0.1f, bounds.size.y);
    }
}
