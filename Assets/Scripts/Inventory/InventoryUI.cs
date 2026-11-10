using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [General] Sidebar inventory dari kanan layar.
///  - Tab panah di tepi kanan untuk buka/tutup.
///  - Layar belakang di-dim saat inventory terbuka (sekaligus memblok input game).
///  - Grid 4 kolom x 8 baris (32 slot) di dalam ScrollRect, jadi tetap muat
///    di layar kecil maupun besar.
/// Slot UI dibuat otomatis saat runtime berdasarkan jumlah slot InventorySystem.
/// </summary>
[DisallowMultipleComponent]
public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public InventorySystem inventory;
    public RectTransform panel;
    public Image dimImage;
    public RectTransform content;
    public Text capacityText;
    public Text tabLabel;

    [Header("Buttons")]
    public Button tabButton;
    public Button closeButton;
    public Button dimButton;

    [Header("Animation")]
    public float slideDuration = 0.22f;
    [Range(0f, 1f)] public float dimAlpha = 0.55f;

    public bool IsOpen { get; private set; }

    /// <summary>Item yang sedang dipilih (dipakai edit mode untuk menanam pohon).</summary>
    public string SelectedItemId { get; private set; }
    public event Action<string> OnSelectionChanged;

    static readonly Color SlotColor = new Color(0.16f, 0.18f, 0.21f, 0.88f);
    static readonly Color SelectedSlotColor = new Color(0.20f, 0.48f, 0.30f, 0.96f);

    readonly List<SlotView> slotViews = new List<SlotView>();

    float openProgress;
    float closedX;
    int selectedIndex = -1;
    bool subscribed;

    class SlotView
    {
        public Image background;
        public Image icon;
        public Text iconLabel;
        public Text itemName;
        public Text count;
    }

    void Awake()
    {
        if (inventory == null) inventory = InventorySystem.Instance;

        closedX = panel != null ? panel.sizeDelta.x : 760f;
        if (panel != null) panel.anchoredPosition = new Vector2(closedX, 0f);

        if (dimImage != null)
        {
            Color color = dimImage.color;
            color.a = 0f;
            dimImage.color = color;
            dimImage.raycastTarget = false;
        }

        if (tabLabel != null) tabLabel.text = "<";
    }

    void OnEnable()
    {
        if (inventory == null) inventory = InventorySystem.Instance;
        Subscribe();
    }

    void Start()
    {
        if (inventory == null) inventory = InventorySystem.Instance;
        Subscribe();

        BuildSlots();
        Refresh();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    void Subscribe()
    {
        if (subscribed || inventory == null) return;
        inventory.OnChanged += Refresh;
        subscribed = true;
    }

    void Unsubscribe()
    {
        if (!subscribed || inventory == null) return;
        inventory.OnChanged -= Refresh;
        subscribed = false;
    }

    void Update()
    {
        float target = IsOpen ? 1f : 0f;
        if (Mathf.Approximately(openProgress, target)) return;

        openProgress = Mathf.MoveTowards(openProgress, target,
            Time.unscaledDeltaTime / Mathf.Max(0.01f, slideDuration));

        ApplyProgress();
    }

    void ApplyProgress()
    {
        // Smoothstep supaya gerakan sidebar terasa halus.
        float eased = openProgress * openProgress * (3f - 2f * openProgress);

        if (panel != null)
            panel.anchoredPosition = new Vector2(Mathf.Lerp(closedX, 0f, eased), 0f);

        if (dimImage != null)
        {
            Color color = dimImage.color;
            color.a = eased * dimAlpha;
            dimImage.color = color;
            dimImage.raycastTarget = openProgress > 0.001f;
        }
    }

    // ------------------------------------------------------------------
    // Public API (dipakai Button.onClick)
    // ------------------------------------------------------------------
    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (IsOpen) return;

        IsOpen = true;
        if (tabLabel != null) tabLabel.text = ">";
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        if (tabLabel != null) tabLabel.text = "<";
    }

    /// <summary>Dipanggil saat slot di-tap. Dipakai edit mode untuk memilih pohon.</summary>
    public void SelectSlot(int index)
    {
        if (inventory == null) return;
        if (index < 0 || index >= inventory.Slots.Count) return;

        InventorySlot slot = inventory.Slots[index];
        if (slot.IsEmpty)
        {
            ClearSelection();
            return;
        }

        selectedIndex = index;
        SelectedItemId = slot.itemId;
        RefreshSelection();
        OnSelectionChanged?.Invoke(SelectedItemId);
    }

    public void ClearSelection()
    {
        if (SelectedItemId == null && selectedIndex < 0) return;

        selectedIndex = -1;
        SelectedItemId = null;
        RefreshSelection();
        OnSelectionChanged?.Invoke(null);
    }

    void RefreshSelection()
    {
        for (int i = 0; i < slotViews.Count; i++)
        {
            if (slotViews[i].background == null) continue;
            slotViews[i].background.color = i == selectedIndex ? SelectedSlotColor : SlotColor;
        }
    }

    // ------------------------------------------------------------------
    // Slot UI
    // ------------------------------------------------------------------
    void BuildSlots()
    {
        if (content == null || inventory == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        slotViews.Clear();

        for (int i = 0; i < inventory.SlotCount; i++)
            slotViews.Add(CreateSlot(i));
    }

    SlotView CreateSlot(int index)
    {
        SlotView view = new SlotView();

        GameObject root = new GameObject("Slot_" + index, typeof(RectTransform), typeof(Image));
        root.transform.SetParent(content, false);

        Image background = root.GetComponent<Image>();
        background.color = SlotColor;
        background.raycastTarget = true;

        view.background = background;

        Button slotButton = root.AddComponent<Button>();
        slotButton.targetGraphic = background;
        slotButton.transition = Selectable.Transition.None;
        int capturedIndex = index;
        slotButton.onClick.AddListener(() => SelectSlot(capturedIndex));

        // Icon (kotak warna item + huruf awal).
        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(root.transform, false);

        RectTransform iconRect = (RectTransform)iconObject.transform;
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.sizeDelta = new Vector2(76f, 76f);
        iconRect.anchoredPosition = new Vector2(0f, -14f);

        view.icon = iconObject.GetComponent<Image>();
        view.icon.raycastTarget = false;

        GameObject iconLabelObject = new GameObject("IconLabel", typeof(RectTransform), typeof(Text));
        iconLabelObject.transform.SetParent(iconObject.transform, false);

        RectTransform iconLabelRect = (RectTransform)iconLabelObject.transform;
        Stretch(iconLabelRect);

        view.iconLabel = iconLabelObject.GetComponent<Text>();
        view.iconLabel.font = UiFont;
        view.iconLabel.fontSize = 34;
        view.iconLabel.fontStyle = FontStyle.Bold;
        view.iconLabel.color = Color.white;
        view.iconLabel.alignment = TextAnchor.MiddleCenter;
        view.iconLabel.raycastTarget = false;
        view.iconLabel.text = "";

        // Nama item.
        GameObject nameObject = new GameObject("Name", typeof(RectTransform), typeof(Text));
        nameObject.transform.SetParent(root.transform, false);

        RectTransform nameRect = (RectTransform)nameObject.transform;
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 0f);
        nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.sizeDelta = new Vector2(-8f, 24f);
        nameRect.anchoredPosition = new Vector2(0f, 6f);

        view.itemName = nameObject.GetComponent<Text>();
        view.itemName.font = UiFont;
        view.itemName.fontSize = 16;
        view.itemName.color = new Color(0.92f, 0.94f, 0.96f);
        view.itemName.alignment = TextAnchor.MiddleCenter;
        view.itemName.horizontalOverflow = HorizontalWrapMode.Wrap;
        view.itemName.verticalOverflow = VerticalWrapMode.Truncate;
        view.itemName.raycastTarget = false;
        view.itemName.text = "";

        // Jumlah stack.
        GameObject countObject = new GameObject("Count", typeof(RectTransform), typeof(Text));
        countObject.transform.SetParent(root.transform, false);

        RectTransform countRect = (RectTransform)countObject.transform;
        countRect.anchorMin = new Vector2(1f, 1f);
        countRect.anchorMax = new Vector2(1f, 1f);
        countRect.pivot = new Vector2(1f, 1f);
        countRect.sizeDelta = new Vector2(72f, 26f);
        countRect.anchoredPosition = new Vector2(-6f, -6f);

        view.count = countObject.GetComponent<Text>();
        view.count.font = UiFont;
        view.count.fontSize = 18;
        view.count.fontStyle = FontStyle.Bold;
        view.count.color = new Color(1f, 0.86f, 0.35f);
        view.count.alignment = TextAnchor.MiddleRight;
        view.count.raycastTarget = false;
        view.count.text = "";

        return view;
    }

    void Refresh()
    {
        if (inventory == null) return;

        if (capacityText != null)
            capacityText.text = inventory.UsedSlots + "/" + inventory.SlotCount;

        if (!string.IsNullOrEmpty(SelectedItemId) && inventory.CountItem(SelectedItemId) <= 0)
        {
            selectedIndex = -1;
            SelectedItemId = null;
            OnSelectionChanged?.Invoke(null);
        }

        int count = Mathf.Min(slotViews.Count, inventory.Slots.Count);
        for (int i = 0; i < count; i++)
        {
            SlotView view = slotViews[i];
            InventorySlot slot = inventory.Slots[i];

            if (slot.IsEmpty)
            {
                view.icon.enabled = false;
                view.iconLabel.text = "";
                view.itemName.text = "";
                view.count.text = "";
                continue;
            }

            InventoryItemDefinition definition = inventory.GetDefinition(slot.itemId);

            view.icon.enabled = true;
            view.icon.color = definition != null ? definition.color : Color.gray;
            view.iconLabel.text = definition != null ? definition.ShortLabel : "?";
            view.itemName.text = definition != null ? definition.displayName : slot.itemId;
            view.count.text = slot.amount > 1 ? "x" + slot.amount : "";
        }

        RefreshSelection();
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    static Font UiFont
    {
        get
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
