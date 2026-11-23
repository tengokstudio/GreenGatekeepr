/// <summary>
/// [General] Kumpulan id item yang dipakai lintas sistem (inventory, toko,
/// dekomposer, perawatan pohon). Semua id harus sama dengan yang
/// didaftarkan di InventorySetup supaya tidak ada item "tidak dikenal".
/// </summary>
public static class GameItems
{
    // Bibit pohon (menggantikan item pohon lama).
    public const string SeedOak = "seed_oak";
    public const string SeedPine = "seed_pine";
    public const string SeedBirch = "seed_birch";
    public const string SeedBlossom = "seed_blossom";

    // Perawatan pohon.
    public const string Fertilizer = "fertilizer";
    public const string WateringCan = "watering_can";
    public const string SunLamp = "sun_lamp";

    // Hasil panen / loot.
    public const string Wood = "wood";
    public const string PestCorpse = "pest_corpse";
    public const string GetahOak = "getah_oak";
    public const string GetahPinus = "getah_pinus";
    public const string AirBirch = "air_birch";
    public const string KelopakSakura = "kelopak_sakura";

    public static readonly string[] AllSeeds =
    {
        SeedOak, SeedPine, SeedBirch, SeedBlossom
    };

    public static bool IsSeed(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        for (int i = 0; i < AllSeeds.Length; i++)
            if (AllSeeds[i] == id) return true;

        return false;
    }
}
