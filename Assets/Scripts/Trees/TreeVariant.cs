using UnityEngine;

public enum TreeFoliageStyle
{
    Round,
    Pine,
    Birch,
    Blossom
}

/// <summary>
/// [General] Definisi satu jenis pohon. Dipakai oleh forest generator, edit mode,
/// toko bibit, dekomposer, dan katalog item inventory supaya id/warna selalu konsisten.
/// </summary>
[System.Serializable]
public class TreeVariant
{
    [Header("Identitas")]
    public string id = "tree_oak";
    public string displayName = "Pohon Oak";
    public TreeFoliageStyle style = TreeFoliageStyle.Round;

    [Header("Ekonomi")]
    [Tooltip("Item bibit yang dibeli di toko dan ditanam dari inventory.")]
    public string seedItemId = "seed_oak";

    [Tooltip("Harga beli 1 bibit di toko (koin).")]
    public int seedPrice = 10;

    [Tooltip("Item komoditas unik yang didapat saat pohon ditebang.")]
    public string commodityItemId = "getah_oak";

    public string commodityName = "Getah Oak";

    [Tooltip("Harga jual 1 komoditas di toko (koin).")]
    public int commodityPrice = 8;

    [Tooltip("Jumlah kayu yang didapat saat pohon ditebang.")]
    public int woodYield = 2;

    [Header("Visual")]
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
                id = "tree_oak",
                displayName = "Pohon Oak",
                style = TreeFoliageStyle.Round,
                seedItemId = "seed_oak",
                seedPrice = 10,
                commodityItemId = "getah_oak",
                commodityName = "Getah Oak",
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
                id = "tree_birch",
                displayName = "Pohon Birch",
                style = TreeFoliageStyle.Birch,
                seedItemId = "seed_birch",
                seedPrice = 12,
                commodityItemId = "air_birch",
                commodityName = "Air Birch",
                commodityPrice = 10,
                woodYield = 2,
                foliageColor = new Color(0.55f, 0.76f, 0.32f),
                trunkColor = new Color(0.85f, 0.83f, 0.76f),
                trunkHeight = 4.2f,
                trunkRadius = 0.35f,
                maxHealth = 160f,
                barHeight = 5.9f
            },
            new TreeVariant
            {
                id = "tree_blossom",
                displayName = "Pohon Sakura",
                style = TreeFoliageStyle.Blossom,
                seedItemId = "seed_blossom",
                seedPrice = 15,
                commodityItemId = "kelopak_sakura",
                commodityName = "Kelopak Sakura",
                commodityPrice = 12,
                woodYield = 2,
                foliageColor = new Color(0.95f, 0.55f, 0.72f),
                trunkColor = new Color(0.38f, 0.24f, 0.20f),
                trunkHeight = 2.8f,
                trunkRadius = 0.45f,
                maxHealth = 180f,
                barHeight = 4.9f
            }
        };
    }

    /// <summary>
    /// Isi field ekonomi yang kosong (scene lama / data hasil deserialize) berdasarkan id.
    /// Aman dipanggil berulang kali.
    /// </summary>
    public void EnsureDefaults()
    {
        switch (id)
        {
            case "tree_oak":
                if (string.IsNullOrEmpty(seedItemId)) seedItemId = "seed_oak";
                if (string.IsNullOrEmpty(commodityItemId)) commodityItemId = "getah_oak";
                if (string.IsNullOrEmpty(commodityName)) commodityName = "Getah Oak";
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

            case "tree_birch":
                if (string.IsNullOrEmpty(seedItemId)) seedItemId = "seed_birch";
                if (string.IsNullOrEmpty(commodityItemId)) commodityItemId = "air_birch";
                if (string.IsNullOrEmpty(commodityName)) commodityName = "Air Birch";
                if (seedPrice <= 0) seedPrice = 12;
                if (commodityPrice <= 0) commodityPrice = 10;
                break;

            case "tree_blossom":
                if (string.IsNullOrEmpty(seedItemId)) seedItemId = "seed_blossom";
                if (string.IsNullOrEmpty(commodityItemId)) commodityItemId = "kelopak_sakura";
                if (string.IsNullOrEmpty(commodityName)) commodityName = "Kelopak Sakura";
                if (seedPrice <= 0) seedPrice = 15;
                if (commodityPrice <= 0) commodityPrice = 12;
                break;
        }

        if (woodYield <= 0) woodYield = 2;
        if (string.IsNullOrEmpty(displayName)) displayName = id;
    }
}
