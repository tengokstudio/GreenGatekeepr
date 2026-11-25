using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Membuat prefab "siap pakai" dari FBX di <c>Assets/3dAssets</c>.
///
/// Masalah model sumber: tiap FBX punya transform bawaan (scale 100, rotasi 270Â°,
/// dan pivot yang jauh dari geometri). Prefab hasil tool ini membungkus model
/// dengan wrapper identity dan memindahkan anaknya supaya:
///   - titik pivot ada di tengah footprint (x/z = 0)
///   - alas model menempel di y = 0
/// sehingga bisa langsung di-instantiate di posisi dunia mana pun.
///
/// Jalankan: Tools &gt; Models &gt; Rebuild Model Prefabs
/// </summary>
public static class ModelSetup
{
    const string SourceRoot = "Assets/3dAssets";
    const string ResourceRoot = "Assets/Resources/Models";

    struct Entry
    {
        public string key;
        public string source;
        public float yaw;

        public Entry(string key, string source, float yaw = 0f)
        {
            this.key = key;
            this.source = source;
            this.yaw = yaw;
        }
    }

    static readonly Entry[] Entries =
    {
        // --- Pohon: 3 famili x 3 ukuran tumbuh ---
        new Entry("tree_pohon_1", "pohon/pohon 1.fbx"),
        new Entry("tree_pohon_2", "pohon/pohon 2.fbx"),
        new Entry("tree_pohon_3", "pohon/pohon 3.fbx"),

        new Entry("tree_pine_1", "pohon/pine 1.fbx"),
        new Entry("tree_pine_2", "pohon/pine 2.fbx"),
        new Entry("tree_pine_3", "pohon/pine 3.fbx"),

        new Entry("tree_sawit_1", "pohon/sawit 1.fbx"),
        new Entry("tree_sawit_2", "pohon/sawit 2.fbx"),
        new Entry("tree_sawit_3", "pohon/sawit 3.fbx"),

        // --- Bangunan & lingkungan ---
        new Entry("shop_toko", "environment/toko kebun.fbx"),
        new Entry("shop_komposer", "environment/komposer.fbx", -27f),
        new Entry("ground_main", "ground/gropund utama.fbx"),
        new Entry("ground_patch", "ground/ground dikit.fbx"),
        new Entry("scatter_grass", "environment/rumput.fbx"),
        new Entry("scatter_rocks", "environment/batuuu.fbx"),
        new Entry("scatter_logs", "environment/pohon tumbang.fbx"),
        new Entry("prop_pond", "environment/aerr.fbx"),

        // --- Peralatan ---
        new Entry("prop_watering_can", "equipment/water can.fbx"),
        new Entry("prop_spray", "equipment/semprotan.fbx"),
        new Entry("prop_lamp", "equipment/lampu.fbx"),
    };

    [MenuItem("Tools/Models/Rebuild Model Prefabs", priority = 60)]
    public static void RebuildFromMenu()
    {
        Build(true);
        EditorUtility.DisplayDialog("Model Setup",
            "Prefab model dibuat di " + ResourceRoot + ".\n\n" +
            "Jumlah: " + Entries.Length + " prefab.", "OK");
    }

    public static void Build(bool verbose)
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(ResourceRoot);

        int created = 0;
        for (int i = 0; i < Entries.Length; i++)
        {
            if (BuildPrefab(Entries[i])) created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ModelLibrary.ClearCache();

        RemoveStalePrefabs();

        if (verbose)
            Debug.Log("[ModelSetup] " + created + "/" + Entries.Length +
                      " prefab model dibuat di " + ResourceRoot + ".");
    }

    // ------------------------------------------------------------------
    // Prefab
    // ------------------------------------------------------------------
    static bool BuildPrefab(Entry entry)
    {
        string sourcePath = SourceRoot + "/" + entry.source;
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null)
        {
            Debug.LogWarning("[ModelSetup] Model tidak ditemukan: " + sourcePath);
            return false;
        }

        GameObject wrapper = new GameObject(entry.key);

        GameObject child = (GameObject)PrefabUtility.InstantiatePrefab(source);
        child.name = entry.key;
        child.transform.SetParent(wrapper.transform, false);

        if (Mathf.Abs(entry.yaw) > 0.01f)
        {
            Vector3 angles = child.transform.localEulerAngles;
            child.transform.localEulerAngles = new Vector3(angles.x, angles.y + entry.yaw, angles.z);
        }

        // Pivot: tengah X/Z, alas di Y = 0.
        Bounds bounds = ComputeBounds(wrapper);
        Vector3 offset = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        child.transform.position -= offset;

        string prefabPath = ResourceRoot + "/" + entry.key + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
        Object.DestroyImmediate(wrapper);
        return true;
    }

    static Bounds ComputeBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    /// <summary>Hapus prefab model yang sudah tidak ada di daftar (jenis pohon lama).</summary>
    static void RemoveStalePrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { ResourceRoot });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

            bool known = false;
            for (int e = 0; e < Entries.Length; e++)
            {
                if (Entries[e].key == fileName)
                {
                    known = true;
                    break;
                }
            }

            if (!known) AssetDatabase.DeleteAsset(path);
        }
    }

    // ------------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------------
    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        int lastSlash = path.LastIndexOf('/');
        string parent = path.Substring(0, lastSlash);
        string leaf = path.Substring(lastSlash + 1);

        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
