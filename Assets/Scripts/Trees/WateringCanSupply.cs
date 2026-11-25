using UnityEngine;

/// <summary>
/// [General] Persediaan air untuk Penyiram.
///
/// Alat Penyiram tetap permanen di inventory, tapi airnya terbatas:
/// maksimum <see cref="maxCharges"/> siraman (default 15). Tiap siraman yang
/// berhasil memakai 1 air. Saat air habis, pemain mengisi ulang dengan
/// men-tap kolam (lihat <see cref="PondRefillStation"/>).
/// </summary>
[DisallowMultipleComponent]
public class WateringCanSupply : MonoBehaviour
{
    public const int DefaultMaxCharges = 15;

    public static WateringCanSupply Instance { get; private set; }

    [Header("Air Penyiram")]
    [Tooltip("Maksimum air per pengisian.")]
    public int maxCharges = DefaultMaxCharges;

    [Tooltip("Sisa air saat ini. Jangan diubah manual.")]
    public int charges = DefaultMaxCharges;

    public int MaxCharges => Mathf.Max(1, maxCharges);
    public bool HasWater => charges > 0;
    public bool IsFull => charges >= MaxCharges;
    public float FillRatio => Mathf.Clamp01(charges / (float)MaxCharges);

    /// <summary>Contoh: "12/15".</summary>
    public string Label => charges + "/" + MaxCharges;

    void Awake()
    {
        Instance = this;
        if (maxCharges <= 0) maxCharges = DefaultMaxCharges;
        charges = Mathf.Clamp(charges, 0, MaxCharges);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Ambil komponen yang ada, atau buat otomatis kalau scene lama belum punya.
    /// Aman dipanggil dari mana saja saat play mode.
    /// </summary>
    public static WateringCanSupply Ensure()
    {
        if (Instance != null) return Instance;

        Instance = FindFirstObjectByType<WateringCanSupply>(FindObjectsInactive.Include);
        if (Instance != null) return Instance;

        GameObject root = new GameObject("WateringCanSupply");
        Instance = root.AddComponent<WateringCanSupply>();
        return Instance;
    }

    /// <summary>Pakai air. Return false kalau air tidak cukup.</summary>
    public bool Consume(int amount = 1)
    {
        if (amount <= 0) return true;
        if (charges < amount) return false;
        charges -= amount;
        return true;
    }

    /// <summary>Isi penuh. Return true kalau sebelumnya memang belum penuh.</summary>
    public bool Refill()
    {
        if (IsFull)
        {
            charges = MaxCharges;
            return false;
        }

        charges = MaxCharges;
        return true;
    }
}
