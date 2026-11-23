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
/// dan katalog item inventory supaya id/warna selalu konsisten.
/// </summary>
[System.Serializable]
public class TreeVariant
{
    public string id = "tree_oak";
    public string displayName = "Pohon Oak";
    public TreeFoliageStyle style = TreeFoliageStyle.Round;

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
                foliageColor = new Color(0.95f, 0.55f, 0.72f),
                trunkColor = new Color(0.38f, 0.24f, 0.20f),
                trunkHeight = 2.8f,
                trunkRadius = 0.45f,
                maxHealth = 180f,
                barHeight = 4.9f
            }
        };
    }
}
