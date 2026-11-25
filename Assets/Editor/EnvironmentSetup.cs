using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [General] Memasang aset lingkungan dari <c>Assets/3dAssets</c> ke scene aktif:
///   - ground utama jadi pulau, papan catur diturunkan jadi laut biru,
///   - sebaran rumput / batu / pohon tumbang,
///   - kolam + palem dekorasi,
///   - properti alat (penyiram, semprotan, lampu) di dekat toko.
///
/// Jalankan: Tools &gt; Models &gt; Setup Environment (Active Scene)
/// </summary>
public static class EnvironmentSetup
{
    const string RootName = "Environment3D";

    /// <summary>Permukaan laut (papan catur biru) di bawah pulau.</summary>
    public const float SeaLevel = -0.6f;

    [MenuItem("Tools/Models/Setup Environment (Active Scene)", priority = 61)]
    public static void SetupFromMenu()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            EditorUtility.DisplayDialog("Environment Setup",
                "Simpan scene dulu, lalu jalankan setup lagi.", "OK");
            return;
        }

        Setup();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[EnvironmentSetup] Pulau (dengan collider), laut biru di y=" + SeaLevel +
                  ", rumput, batu, pohon tumbang, kolam, dan properti dipasang. Scene disimpan.");
    }

    static void Setup()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null) Object.DestroyImmediate(existing);

        GameObject root = new GameObject(RootName);

        ConfigureSea();
        HideLegacyPatch();

        // ---- Ground (pulau) ----
        // Prefab ground punya pivot di dasar tebal tanahnya, jadi digeser ke atas
        // supaya permukaan rumputnya jatuh tepat di y = 0, yaitu 0.6m di atas laut.
        float surface = MeasureSurface(ModelLibrary.GroundModel);
        GameObject ground = ModelLibrary.Spawn(ModelLibrary.GroundModel, root.transform,
            new Vector3(0f, -surface, 0f));
        if (ground != null)
        {
            ground.name = "Ground3D";

            // Collider pulau dipakai player untuk berjalan; laut ada di bawahnya.
            // MeshCollider harus menempel di objek yang punya MeshFilter, karena
            // transform model FBX tidak identity (scale/offset besar).
            MeshFilter filter = ground.GetComponentInChildren<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                MeshCollider existingCollider = filter.GetComponent<MeshCollider>();
                if (existingCollider != null) Object.DestroyImmediate(existingCollider);

                MeshCollider islandCollider = filter.gameObject.AddComponent<MeshCollider>();
                islandCollider.sharedMesh = filter.sharedMesh;
            }
        }

        // ---- Sebaran ----
        SpawnScatter(root.transform, ModelLibrary.GrassScatter, 0.02f, "GrassScatter");
        SpawnScatter(root.transform, ModelLibrary.RockScatter, 0.02f, "RockScatter");
        SpawnScatter(root.transform, ModelLibrary.LogScatter, 0.02f, "LogScatter");

        // ---- Kolam + palem ----
        SpawnProp(root.transform, ModelLibrary.PondModel, new Vector3(-3.4f, 0f, 5.2f), 18f, 2.2f, "Pond");

        SpawnProp(root.transform, ModelLibrary.PalmKey(3), new Vector3(-1.6f, 0f, 6.4f), 0f, 1f, "Palm_A");
        SpawnProp(root.transform, ModelLibrary.PalmKey(3), new Vector3(-5.2f, 0f, 6.6f), 140f, 0.95f, "Palm_B");
        SpawnProp(root.transform, ModelLibrary.PalmKey(2), new Vector3(-2.4f, 0f, 3.4f), 250f, 1f, "Palm_C");

        // ---- Properti alat di dekat toko ----
        SpawnProp(root.transform, ModelLibrary.WateringCanModel, new Vector3(3.6f, 0f, 0.6f), 24f, 1.6f, "WateringCan");
        SpawnProp(root.transform, ModelLibrary.SprayModel, new Vector3(3.1f, 0f, 0.0f), -32f, 1.5f, "Spray");
        SpawnProp(root.transform, ModelLibrary.LampModel, new Vector3(4.1f, 0f, 0.1f), 0f, 1.5f, "Lamp");
    }

    /// <summary>
    /// Papan catur sekarang dipakai sebagai laut biru (material diubah user).
    /// Cukup diturunkan supaya tidak z-fighting dengan permukaan pulau.
    /// </summary>
    static void ConfigureSea()
    {
        GameObject plane = GameObject.Find("Checkerboard Stage/Ground");
        if (plane == null) return;

        plane.transform.position = new Vector3(0f, SeaLevel, 0f);

        MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.enabled = true;
    }

    /// <summary>
    /// Ground_Mesh adalah sisa dekorasi scene lama yang materialnya null (magenta).
    /// </summary>
    static void HideLegacyPatch()
    {
        GameObject legacyPatch = GameObject.Find("Ground_Mesh");
        if (legacyPatch != null)
        {
            MeshRenderer renderer = legacyPatch.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.enabled = false;
        }
    }

    static void SpawnScatter(Transform parent, string key, float y, string name)
    {
        GameObject instance = ModelLibrary.Spawn(key, parent, new Vector3(0f, y, 0f));
        if (instance != null) instance.name = name;
    }

    static void SpawnProp(Transform parent, string key, Vector3 position, float yaw, float scale, string name)
    {
        GameObject instance = ModelLibrary.Spawn(key, parent, position, yaw, scale);
        if (instance != null) instance.name = name;
    }

    /// <summary>
    /// Cari tinggi permukaan pijak model ground: bidang datar terluas yang menghadap
    /// ke atas. Bukan sekadar vertex terbanyak, karena tepi miring model punya
    /// kepadatan vertex tinggi dan bisa menang dari bidang tanahnya.
    /// </summary>
    static float MeasureSurface(string key)
    {
        GameObject prefab = ModelLibrary.Load(key);
        if (prefab == null) return 0f;

        GameObject instance = Object.Instantiate(prefab);
        instance.transform.position = Vector3.zero;

        // x = tinggi centroid, y = luas segitiga.
        List<Vector2> patches = new List<Vector2>();
        float min = float.MaxValue;
        float max = float.MinValue;

        MeshFilter[] filters = instance.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            Mesh mesh = filters[i].sharedMesh;
            if (mesh == null) continue;

            Transform transform = filters[i].transform;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                Vector3 a = transform.TransformPoint(vertices[triangles[t]]);
                Vector3 b = transform.TransformPoint(vertices[triangles[t + 1]]);
                Vector3 c = transform.TransformPoint(vertices[triangles[t + 2]]);

                Vector3 cross = Vector3.Cross(b - a, c - a);
                float doubleArea = cross.magnitude;
                if (doubleArea < 0.0001f) continue;

                // Hanya permukaan yang menghadap ke atas. Bagian bawah model (menghadap
                // ke bawah) luasnya sama besar dan akan menang kalau ikut dihitung.
                if (cross.y / doubleArea < 0.7f) continue;

                float y = (a.y + b.y + c.y) / 3f;
                patches.Add(new Vector2(y, doubleArea * 0.5f));

                if (y < min) min = y;
                if (y > max) max = y;
            }
        }

        Object.DestroyImmediate(instance);

        if (patches.Count == 0 || max - min < 0.01f) return 0f;

        const int buckets = 128;
        float[] areas = new float[buckets];

        for (int i = 0; i < patches.Count; i++)
        {
            int index = Mathf.Clamp((int)((patches[i].x - min) / (max - min) * (buckets - 1)),
                0, buckets - 1);
            areas[index] += patches[i].y;
        }

        int best = 0;
        for (int i = 1; i < buckets; i++)
            if (areas[i] > areas[best]) best = i;

        return min + (best + 0.5f) / buckets * (max - min);
    }
}
