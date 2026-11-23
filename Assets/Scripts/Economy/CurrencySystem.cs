using System;
using UnityEngine;

/// <summary>
/// [General] Mata uang koin untuk toko. Dipakai lintas mode kamera.
/// </summary>
[DisallowMultipleComponent]
public class CurrencySystem : MonoBehaviour
{
    public static CurrencySystem Instance { get; private set; }

    [Header("Koin")]
    public int startingCoins = 30;
    public int coins;

    [Tooltip("Batas maksimum koin (0 = tanpa batas).")]
    public int maxCoins = 999999;

    public event Action OnChanged;

    void Awake()
    {
        Instance = this;
        if (coins <= 0) coins = Mathf.Max(0, startingCoins);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;

        coins += amount;
        if (maxCoins > 0) coins = Mathf.Min(coins, maxCoins);
        OnChanged?.Invoke();
    }

    public bool CanAfford(int amount) => amount >= 0 && coins >= amount;

    public bool TrySpend(int amount)
    {
        if (amount < 0) return false;
        if (coins < amount) return false;

        coins -= amount;
        OnChanged?.Invoke();
        return true;
    }

    public void Set(int amount)
    {
        coins = Mathf.Max(0, amount);
        if (maxCoins > 0) coins = Mathf.Min(coins, maxCoins);
        OnChanged?.Invoke();
    }
}
