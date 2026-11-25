using UnityEngine;

public enum TreeFoliageStyle
{
    Round,
    Pine,
    Palm
}

/// <summary>
/// [General] Definisi satu jenis pohon. Dipakai oleh forest generator, edit mode,
/// toko bibit, dekomposer, dan katalog item inventory supaya id/warna selalu konsisten.
///
/// Jenis pohon mengikuti isi folder Assets/3dAssets/pohon:
///   pohon (bulat), pine (kerucut), sawit (palem).
/// </summary>
[System.Serializable]
public class TreeVariant
{
    [Header("Identitas")]
    public string id = "tree_pohon";
    public string displayName = "Pohon";
    public TreeFoliageStyle style = TreeFoliageStyle.Round;

    [Header("Ekonomi")]
    [Tooltip("Item bibit yang dibeli di toko dan ditanam dari inventory.")]
    public string seedItemId = "seed_pohon";

    [Tooltip("Harga beli 1 bibit di toko (koin).")]
    public int seedPrice = 10;

    [Tooltip("Item komoditas unik yang didapat saat pohon ditebang.")]
    public string commodityItemId = "getah_pohon";

    public string commodityName = "Getah Pohon";

    [Tooltip("Harga jual 1 komoditas di toko (koin).")]
    public int commodityPrice = 8;

    [Tooltip("Jumlah kayu yang didapat saat pohon ditebang.")]
    public int woodYield = 2;

    [Header("Visual")]
    [Tooltip("Famili model 3D: pohon / pine / sawit. Kosong = pohon prosedural.")]
    public string modelFamily = "pohon";

    public Color foliageColor = new Color(0.30f, 0.62f, 0.26f);
    public Color trunkColor = new Color(0.42f, 0.29f, 0.16f);

    public float trunkHeight = 3.2f;
    public float trunkRadius = 0.55f;
    public float maxHealth = 200f;
    public float barHeight = 5.3f;

    public static TreeVariant[] CreateDefaults()
    {
        return new TreeVariant[]
        {
            new TreeVariant
            {
                id = "tree_pohon",
                displayName = "Pohon",
                style = TreeFoliageStyle.Round,
                modelFamily = "pohon",
                seedItemId = "seed_pohon",
                seedPrice = 10,
                commodityItemId = "getah_pohon",
                commodityName = "Getah Pohon",
                commodityPrice = 8,
                woodYield = 2,
                foliageColor = new Color(0.30f, 0.62f, 0.26f),
                trunkColor = new Color(0.42f, 0.29f, 0.16f),
                trunkHeight = 3.2f,
                trunkRadius = 0.55f,
                maxHealth = 200f,
                barHeight = 5.3f
            },
            new TreeVariant
            {
                id = "tree_pine",
                displayName = "Pohon Pinus",
                style = TreeFoliageStyle.Pine,
                modelFamily = "pine",
                seedItemId = "seed_pine",
                seedPrice = 12,
                commodityItemId = "getah_pinus",
                commodityName = "Getah Pinus",
                commodityPrice = 8,
                woodYield = 2,
                foliageColor = new Color(0.16f, 0.42f, 0.22f),
                trunkColor = new Color(0.35f, 0.24f, 0.14f),
                trunkHeight = 3.0f,
                trunkRadius = 0.50f,
                maxHealth = 220f,
                barHeight = 6.4f
            },
            new TreeVariant
            {
                id = "tree_sawit",
                displayName = "Pohon Sawit",
                style = TreeFoliageStyle.Palm,
                modelFamily = "sawit",
                seedItemId = "seed_sawit",
                seedPrice = 15,
                commodityItemId = "nira_sawit",
                commodityName = "Nira Sawit",
                commodityPrice = 12,
                woodYield = 2,
                foliageColor = new Color(0.26f, 0.58f, 0.24f),
                trunkColor = new Color(0.48f, 0.38f, 0.24f),
                trunkHeight = 4.6f,
                trunkRadius = 0.34f,
                maxHealth = 180f,
                barHeight = 6.2f
            }
        };
    }

    /// <summary>
    /// Famili model 3D yang dipakai. Data scene lama belum punya field ini,
    /// jadi diturunkan dari <see cref="style"/>.
    /// </summary>
    public string ResolveModelFamily()
    {
        if (!string.IsNullOrEmpty(modelFamily)) return modelFamily;

        switch (style)
        {
            case TreeFoliageStyle.Pine: return "pine";
            case TreeFoliageStyle.Palm: return "sawit";
            default: return "pohon";
        }
    }

    /// <summary>
    /// Isi field ekonomi yang kosong (scene lama / data hasil deserialize) berdasarkan id.
    /// Aman dipanggil berulang kali.
    /// </summary>
    public void EnsureDefaults()
    {
        switch (id)
        {
            case "tree_pohon":
                if (string.IsNullOrEmpty(seedItemId)) seedItemId = "seed_pohon";
                if (string.IsNullOrEmpty(commodityItemId)) commodityItemId = "getah_pohon";
                if (string.IsNullOrEmpty(commodityName)) commodityName = "Getah Pohon";
                if (seedPrice <= 0) seedPrice = 10;
                if (commodityPrice <= 0) commodityPrice = 8;
                break;

            case "tree_pine":
                if (string.IsNullOrEmpty(seedItemId)) seedItemId = "seed_pine";
                if (string.IsNullOrEmpty(commodityItemId)) commodityItemId = "getah_pinus";
                if (string.IsNullOrEmpty(commodityName)) commodityName = "Getah Pinus";
                if (seedPrice <= 0) seedPrice = 12;
                if (commodityPrice <= 0) commodityPrice = 8;
                break;

            case "tree_sawit":
                if (string.IsNullOrEmpty(seedItemId)) seedItemId = "seed_sawit";
                if (string.IsNullOrEmpty(commodityItemId)) commodityItemId = "nira_sawit";
                if (string.IsNullOrEmpty(commodityName)) commodityName = "Nira Sawit";
                if (seedPrice <= 0) seedPrice = 15;
                if (commodityPrice <= 0) commodityPrice = 12;
                break;
        }

        if (woodYield <= 0) woodYield = 2;
        if (string.IsNullOrEmpty(displayName)) displayName = id;
        if (string.IsNullOrEmpty(modelFamily)) modelFamily = ResolveModelFamily();
    }
}
