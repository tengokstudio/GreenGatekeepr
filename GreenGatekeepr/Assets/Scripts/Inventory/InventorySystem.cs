using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Satu slot inventory. Kosong kalau itemId null/amount 0.
/// </summary>
[System.Serializable]
public class InventorySlot
{
    public string itemId;
    public int amount;

    public bool IsEmpty => string.IsNullOrEmpty(itemId) || amount <= 0;

    public void Clear()
    {
        itemId = null;
        amount = 0;
    }
}

/// <summary>Item awal untuk testing (misal 2 pohon tiap variasi).</summary>
[System.Serializable]
public class StartingItem
{
    public string id;
    public int amount = 1;
}

/// <summary>
/// [General] Penyimpanan item dengan grid tetap 4x8 (32 slot).
/// Bisa dipakai di mode isometric maupun FPS karena tidak bergantung pada kamera.
/// </summary>
[DisallowMultipleComponent]
public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Grid")]
    public int columns = 4;
    public int rows = 8;

    [Header("Item Catalog")]
    public InventoryItemDefinition[] itemDefinitions;

    [Header("Starting Items (test)")]
    public StartingItem[] startingItems;

    [SerializeField] List<InventorySlot> slots = new List<InventorySlot>();

    /// <summary>Dipanggil setiap isi inventory berubah.</summary>
    public event Action OnChanged;

    public int SlotCount => Mathf.Max(1, columns * rows);
    public IReadOnlyList<InventorySlot> Slots => slots;

    public int UsedSlots
    {
        get
        {
            int count = 0;
            for (int i = 0; i < slots.Count; i++)
                if (!slots[i].IsEmpty) count++;
            return count;
        }
    }

    public int TotalItems
    {
        get
        {
            int count = 0;
            for (int i = 0; i < slots.Count; i++)
                if (!slots[i].IsEmpty) count += slots[i].amount;
            return count;
        }
    }

    void Awake()
    {
        Instance = this;
        EnsureSlots();
    }

    void Start()
    {
        ApplyStartingItems();
    }

    void ApplyStartingItems()
    {
        if (startingItems == null) return;

        for (int i = 0; i < startingItems.Length; i++)
        {
            StartingItem item = startingItems[i];
            if (item != null && !string.IsNullOrEmpty(item.id) && item.amount > 0)
                AddItem(item.id, item.amount);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void EnsureSlots()
    {
        int count = SlotCount;

        while (slots.Count < count) slots.Add(new InventorySlot());
        if (slots.Count > count) slots.RemoveRange(count, slots.Count - count);
    }

    public InventoryItemDefinition GetDefinition(string id)
    {
        if (string.IsNullOrEmpty(id) || itemDefinitions == null) return null;

        for (int i = 0; i < itemDefinitions.Length; i++)
        {
            InventoryItemDefinition definition = itemDefinitions[i];
            if (definition != null && definition.id == id) return definition;
        }

        return null;
    }

    public int CountItem(string id)
    {
        int count = 0;
        for (int i = 0; i < slots.Count; i++)
            if (!slots[i].IsEmpty && slots[i].itemId == id) count += slots[i].amount;
        return count;
    }

    /// <summary>
    /// Menambah item: isi stack yang sudah ada dulu, lalu slot kosong.
    /// Return true kalau setidaknya sebagian item masuk.
    /// </summary>
    public bool AddItem(string id, int amount = 1)
    {
        if (amount <= 0) return false;

        InventoryItemDefinition definition = GetDefinition(id);
        if (definition == null)
        {
            Debug.LogWarning("[Inventory] Item id tidak dikenal: " + id);
            return false;
        }

        EnsureSlots();

        int remaining = amount;
        int maxStack = Mathf.Max(1, definition.maxStack);

        // 1) Tumpuk ke stack yang sudah ada.
        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (slot.IsEmpty || slot.itemId != id || slot.amount >= maxStack) continue;

            int add = Mathf.Min(maxStack - slot.amount, remaining);
            slot.amount += add;
            remaining -= add;
        }

        // 2) Pakai slot kosong.
        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (!slot.IsEmpty) continue;

            int add = Mathf.Min(maxStack, remaining);
            slot.itemId = id;
            slot.amount = add;
            remaining -= add;
        }

        bool added = remaining < amount;
        if (added) OnChanged?.Invoke();

        if (remaining > 0)
            Debug.LogWarning("[Inventory] Penuh, " + remaining + " item '" + id + "' tidak masuk.");

        return added;
    }

    public bool RemoveItem(string id, int amount = 1)
    {
        if (amount <= 0 || string.IsNullOrEmpty(id)) return false;
        if (CountItem(id) < amount) return false;

        int remaining = amount;
        for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            InventorySlot slot = slots[i];
            if (slot.IsEmpty || slot.itemId != id) continue;

            int remove = Mathf.Min(slot.amount, remaining);
            slot.amount -= remove;
            remaining -= remove;

            if (slot.amount <= 0) slot.Clear();
        }

        OnChanged?.Invoke();
        return true;
    }

    public void ClearAll()
    {
        for (int i = 0; i < slots.Count; i++) slots[i].Clear();
        OnChanged?.Invoke();
    }
}
