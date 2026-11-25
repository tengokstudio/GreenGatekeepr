/// <summary>
/// [General] Kumpulan id item yang dipakai lintas sistem (inventory, toko,
/// dekomposer, perawatan pohon). Semua id harus sama dengan yang
/// didaftarkan di InventorySetup supaya tidak ada item "tidak dikenal".
/// </summary>
public static class GameItems
{
    // Bibit pohon. Satu id per famili model 3D di Assets/3dAssets/pohon.
    public const string SeedPohon = "seed_pohon";
    public const string SeedPine = "seed_pine";
    public const string SeedSawit = "seed_sawit";

    // Perawatan pohon.
    public const string Fertilizer = "fertilizer";
    public const string WateringCan = "watering_can";
    public const string SunLamp = "sun_lamp";

    // Hasil panen / loot.
    public const string Wood = "wood";
    public const string PestCorpse = "pest_corpse";
    public const string GetahPohon = "getah_pohon";
    public const string GetahPinus = "getah_pinus";
    public const string NiraSawit = "nira_sawit";

    public static readonly string[] AllSeeds =
    {
        SeedPohon, SeedPine, SeedSawit
    };

    public static bool IsSeed(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        for (int i = 0; i < AllSeeds.Length; i++)
            if (AllSeeds[i] == id) return true;

        return false;
    }
}
