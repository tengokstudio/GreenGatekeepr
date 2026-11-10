using UnityEngine;

/// <summary>
/// [General] Katalog 4 variasi pohon. Kalau array kosong, otomatis memakai
/// definisi default supaya scene lama tetap jalan.
/// </summary>
[DisallowMultipleComponent]
public class TreeCatalog : MonoBehaviour
{
    public TreeVariant[] variants;

    void Awake()
    {
        if (variants == null || variants.Length == 0)
            variants = TreeVariant.CreateDefaults();
    }

    public int Count => variants != null ? variants.Length : 0;

    public TreeVariant Get(string id)
    {
        if (string.IsNullOrEmpty(id) || variants == null) return null;

        for (int i = 0; i < variants.Length; i++)
            if (variants[i] != null && variants[i].id == id) return variants[i];

        return null;
    }

    public TreeVariant GetByIndex(int index)
    {
        if (variants == null || variants.Length == 0) return null;
        return variants[Mathf.Clamp(index, 0, variants.Length - 1)];
    }
}
