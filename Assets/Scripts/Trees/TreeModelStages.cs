using UnityEngine;

/// <summary>
/// [General] Menukar mesh pohon sesuai tahap tumbuh memakai 3 model ukuran
/// (kecil / sedang / besar) dari <see cref="ModelLibrary"/>.
/// Tahap 0 = model 1, tahap 1 = model 2, tahap 2 &amp; 3 = model 3.
/// Komponen ini hanya dipasang kalau prefab model tersedia; kalau tidak,
/// TreeFactory memakai pohon prosedural lama.
/// </summary>
[DisallowMultipleComponent]
public class TreeModelStages : MonoBehaviour
{
    [Tooltip("Instance model per ukuran (kecil, sedang, besar).")]
    public GameObject[] stageObjects;

    [Tooltip("Tinggi tiap model dalam satuan lokal root.")]
    public float[] stageHeights;

    [Tooltip("Skala root dari TreeFactory (variasi acak hutan).")]
    public float rootScale = 1f;

    int current = -1;

    public bool IsReady => stageObjects != null && stageObjects.Length > 0;

    /// <summary>Dipanggil TreeFactory saat pohon dibuat (root masih nonaktif).</summary>
    public static TreeModelStages Build(Transform root, GameObject[] prefabs, float scale)
    {
        if (root == null || prefabs == null || prefabs.Length == 0) return null;

        TreeModelStages stages = root.gameObject.AddComponent<TreeModelStages>();
        stages.rootScale = Mathf.Max(0.1f, scale);
        stages.stageObjects = new GameObject[prefabs.Length];
        stages.stageHeights = new float[prefabs.Length];

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] == null) continue;

            GameObject instance = Instantiate(prefabs[i], root);
            instance.name = "Stage_" + (i + 1);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            stages.stageObjects[i] = instance;
            stages.stageHeights[i] = ModelLibrary.MeasureHeight(instance);
        }

        stages.SetStage(3);
        return stages;
    }

    /// <summary>Tahap TreeGrowth (0..3) dipetakan ke 3 ukuran model.</summary>
    public void SetStage(int stageIndex)
    {
        if (!IsReady) return;

        int model = Mathf.Clamp(stageIndex, 0, stageObjects.Length - 1);
        if (model == current) return;

        current = model;
        for (int i = 0; i < stageObjects.Length; i++)
            if (stageObjects[i] != null) stageObjects[i].SetActive(i == model);
    }

    /// <summary>Tinggi model untuk tahap tertentu (dipakai health bar &amp; label).</summary>
    public float HeightFor(int stageIndex)
    {
        if (stageHeights == null || stageHeights.Length == 0) return 4f;

        int index = Mathf.Clamp(stageIndex, 0, stageHeights.Length - 1);
        return Mathf.Max(0.5f, stageHeights[index]);
    }

    public Renderer[] AllRenderers()
    {
        return GetComponentsInChildren<Renderer>(true);
    }
}
