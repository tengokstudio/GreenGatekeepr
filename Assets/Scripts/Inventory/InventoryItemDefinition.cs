using UnityEngine;

/// <summary>
/// Definisi satu tipe item inventory. Cukup data ringan (bukan ScriptableObject)
/// supaya bisa ditambah langsung dari inspector InventorySystem.
/// </summary>
[System.Serializable]
public class InventoryItemDefinition
{
    public string id = "item";
    public string displayName = "Item";
    public Color color = Color.white;
    public int maxStack = 20;

    public string ShortLabel
    {
        get
        {
            if (string.IsNullOrEmpty(displayName)) return "?";

            // Ambil huruf pertama dari kata terakhir supaya "Pohon Oak" -> O,
            // "Pohon Pinus" -> P, "Mayat Hama" -> H (lebih mudah dibedakan).
            string trimmed = displayName.Trim();
            int lastSpace = trimmed.LastIndexOf(' ');
            if (lastSpace >= 0 && lastSpace < trimmed.Length - 1)
                trimmed = trimmed.Substring(lastSpace + 1);

            return trimmed.Substring(0, 1).ToUpperInvariant();
        }
    }
}
