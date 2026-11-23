using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [General] Panel toko dengan dua tab: BELI BIBIT dan JUAL HASIL.
/// Baris dibuat otomatis saat runtime supaya harga &amp; stok selalu sinkron.
/// Panel memakai blocker transparan (tanpa layar dim) untuk menutup saat tap luar.
/// </summary>
[DisallowMultipleComponent]
public class ShopPanel : MonoBehaviour
{
    public static ShopPanel Instance { get; private set; }

    [Header("Root")]
    public GameObject root;
    public GameObject blocker;

    [Header("Header")]
    public Text titleText;
    public Text coinsText;
    public Text hintText;

    [Header("Tabs")]
    public Button buyTabButton;
    public Button sellTabButton;
    public Text buyTabLabel;
    public Text sellTabLabel;
    public GameObject buyPage;
    public GameObject sellPage;

    [Header("Lists")]
    public RectTransform buyContent;
    public RectTransform sellContent;

    [Header("Footer")]
    public Text messageText;

    [Header("Buttons")]
    public Button closeButton;
    public Button blockerButton;

    [Header("References")]
    public ShopStation station;
    public TreeCatalog catalog;
    public InventorySystem inventory;
    public CurrencySystem currency;

    public bool IsOpen => root != null && root.activeSelf;
    public static bool AnyOpen => Instance != null && Instance.IsOpen;

    class Row
    {
        public string itemId;
        public int price;
        public bool buy;
        public Text name;
        public Text detail;
        public Text buttonLabel;
        public Button button;
        public Image accent;
    }

    static readonly Color RowColor = new Color(0.10f, 0.13f, 0.16f, 0.96f);
    static readonly Color RowColorAlt = new Color(0.13f, 0.17f, 0.20f, 0.96f);
    static readonly Color BuyColor = new Color(0.16f, 0.45f, 0.26f, 1f);
    static readonly Color SellColor = new Color(0.52f, 0.37f, 0.12f, 1f);
    static readonly Color TabActive = new Color(0.20f, 0.55f, 0.32f, 1f);
    static readonly Color TabIdle = new Color(0.14f, 0.17f, 0.20f, 1f);

    readonly List<Row> rows = new List<Row>();
    bool built;
    bool showingBuy = true;
    float messageUntil;
    float nextRefresh;

    void Awake()
    {
        Instance = this;
        if (catalog == null) catalog = FindFirstObjectByType<TreeCatalog>();
        if (inventory == null) inventory = InventorySystem.Instance;
        if (currency == null) currency = CurrencySystem.Instance;
        EnsureBlockerBehind();
        SetVisible(false);
    }

    /// <summary>
    /// Blocker harus berada di belakang panel. Kalau tidak, blocker menelan
    /// semua klik tombol dan panel terasa "tidak bisa dipencet".
    /// </summary>
    void EnsureBlockerBehind()
    {
        if (blocker == null) return;
        blocker.transform.SetAsFirstSibling();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Open(ShopStation shopStation)
    {
        station = shopStation;

        if (TreeCarePanel.Instance != null) TreeCarePanel.Instance.Close();
        if (DecomposerPanel.Instance != null) DecomposerPanel.Instance.Close();

        if (catalog == null) catalog = FindFirstObjectByType<TreeCatalog>();
        if (inventory == null) inventory = InventorySystem.Instance;
        if (currency == null) currency = CurrencySystem.Instance;

        SetVisible(true);
        BuildRows();
        ShowPage(true);
        ShowMessage("Pilih bibit untuk ditanam, atau jual hasil panen.", new Color(0.82f, 0.9f, 1f));
        Refresh();
    }

    public void Close()
    {
        SetVisible(false);
    }

    void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
        if (blocker != null) blocker.SetActive(visible);
    }

    void Update()
    {
        if (!IsOpen) return;

        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.2f;
            Refresh();
        }

        if (messageText != null && messageUntil > 0f && Time.unscaledTime >= messageUntil)
        {
            messageUntil = 0f;
            messageText.text = "";
        }
    }

    // ------------------------------------------------------------------
    // Tab
    // ------------------------------------------------------------------
    public void ShowBuy() => ShowPage(true);
    public void ShowSell() => ShowPage(false);

    void ShowPage(bool buy)
    {
        showingBuy = buy;

        if (buyPage != null) buyPage.SetActive(buy);
        if (sellPage != null) sellPage.SetActive(!buy);

        if (buyTabButton != null)
            buyTabButton.image.color = buy ? TabActive : TabIdle;
        if (sellTabButton != null)
            sellTabButton.image.color = buy ? TabIdle : TabActive;

        if (buyTabLabel != null) buyTabLabel.color = buy ? Color.white : new Color(0.72f, 0.78f, 0.82f);
        if (sellTabLabel != null) sellTabLabel.color = buy ? new Color(0.72f, 0.78f, 0.82f) : Color.white;

        if (hintText != null)
            hintText.text = buy
                ? "Bibit ditanam lewat tombol EDIT di mode isometrik."
                : "Jual pupuk & komoditas hasil pohon dewasa.";
    }

    // ------------------------------------------------------------------
    // Bangun baris
    // ------------------------------------------------------------------
    void BuildRows()
    {
        if (built || buyContent == null || sellContent == null) return;

        ClearChildren(buyContent);
        ClearChildren(sellContent);
        rows.Clear();

        int index = 0;

        if (catalog != null && catalog.variants != null)
        {
            for (int i = 0; i < catalog.variants.Length; i++)
            {
                TreeVariant variant = catalog.variants[i];
                if (variant == null) continue;

                Row row = AddRow(buyContent, variant.seedItemId, variant.seedPrice, true, index++);
                if (row != null && row.name != null)
                    row.name.text = "Bibit " + ShortTreeName(variant);
            }
        }

        index = 0;

        AddSellRow(GameItems.Fertilizer, "Pupuk", 6, index++);

        if (catalog != null && catalog.variants != null)
        {
            for (int i = 0; i < catalog.variants.Length; i++)
            {
                TreeVariant variant = catalog.variants[i];
                if (variant == null) continue;

                AddSellRow(variant.commodityItemId, variant.commodityName, variant.commodityPrice, index++);
            }
        }

        built = true;
    }

    static void ClearChildren(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    static string ShortTreeName(TreeVariant variant)
    {
        if (variant == null) return "Pohon";

        string name = variant.displayName;
        if (name.StartsWith("Pohon ")) name = name.Substring(6);
        return name;
    }

    void AddSellRow(string itemId, string label, int price, int index)
    {
        Row row = AddRow(sellContent, itemId, price, false, index);
        if (row != null && row.name != null) row.name.text = label;
    }

    Row AddRow(RectTransform parent, string itemId, int price, bool buy, int index)
    {
        if (string.IsNullOrEmpty(itemId)) return null;

        Row row = new Row { itemId = itemId, price = price, buy = buy };

        GameObject rowObject = new GameObject("Row_" + itemId, typeof(RectTransform), typeof(LayoutElement));
        rowObject.transform.SetParent(parent, false);

        RectTransform rowRect = (RectTransform)rowObject.transform;
        rowRect.sizeDelta = new Vector2(0f, 96f);

        LayoutElement layout = rowObject.GetComponent<LayoutElement>();
        layout.preferredHeight = 96f;

        Image background = rowObject.AddComponent<Image>();
        background.color = index % 2 == 0 ? RowColor : RowColorAlt;

        Color itemColor = ItemColor(itemId);

        // Strip warna item di sisi kiri.
        RectTransform accentRect = MakeRect("Accent", rowObject.transform);
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.sizeDelta = new Vector2(8f, 0f);
        accentRect.anchoredPosition = Vector2.zero;
        row.accent = accentRect.gameObject.AddComponent<Image>();
        row.accent.color = itemColor;
        row.accent.raycastTarget = false;

        // Kotak ikon.
        RectTransform iconRect = MakeRect("Icon", rowObject.transform);
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(56f, 56f);
        iconRect.anchoredPosition = new Vector2(24f, 0f);

        Image icon = iconRect.gameObject.AddComponent<Image>();
        icon.color = itemColor;
        icon.raycastTarget = false;

        // Nama item.
        RectTransform nameRect = MakeRect("Name", rowObject.transform);
        nameRect.anchorMin = new Vector2(0f, 0.5f);
        nameRect.anchorMax = new Vector2(0f, 0.5f);
        nameRect.pivot = new Vector2(0f, 0.5f);
        nameRect.sizeDelta = new Vector2(430f, 34f);
        nameRect.anchoredPosition = new Vector2(96f, 16f);

        row.name = nameRect.gameObject.AddComponent<Text>();
        row.name.font = UiFont;
        row.name.fontSize = 26;
        row.name.fontStyle = FontStyle.Bold;
        row.name.alignment = TextAnchor.MiddleLeft;
        row.name.color = new Color(0.94f, 0.96f, 0.98f);
        row.name.raycastTarget = false;

        // Keterangan harga / stok.
        RectTransform detailRect = MakeRect("Detail", rowObject.transform);
        detailRect.anchorMin = new Vector2(0f, 0.5f);
        detailRect.anchorMax = new Vector2(0f, 0.5f);
        detailRect.pivot = new Vector2(0f, 0.5f);
        detailRect.sizeDelta = new Vector2(430f, 28f);
        detailRect.anchoredPosition = new Vector2(96f, -18f);

        row.detail = detailRect.gameObject.AddComponent<Text>();
        row.detail.font = UiFont;
        row.detail.fontSize = 20;
        row.detail.alignment = TextAnchor.MiddleLeft;
        row.detail.color = new Color(0.68f, 0.76f, 0.82f);
        row.detail.raycastTarget = false;

        // Tombol aksi.
        GameObject buttonObject = new GameObject("Action", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(rowObject.transform, false);

        RectTransform buttonRect = (RectTransform)buttonObject.transform;
        buttonRect.anchorMin = new Vector2(1f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.pivot = new Vector2(1f, 0.5f);
        buttonRect.sizeDelta = new Vector2(210f, 64f);
        buttonRect.anchoredPosition = new Vector2(-16f, 0f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = buy ? BuyColor : SellColor;

        row.button = buttonObject.GetComponent<Button>();
        row.button.targetGraphic = buttonImage;
        row.button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = row.button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 1f, 0.9f);
        colors.pressedColor = new Color(0.7f, 0.88f, 0.76f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.55f);
        row.button.colors = colors;

        RectTransform labelRect = MakeRect("Label", buttonObject.transform);
        Stretch(labelRect);

        row.buttonLabel = labelRect.gameObject.AddComponent<Text>();
        row.buttonLabel.font = UiFont;
        row.buttonLabel.text = (buy ? "BELI " : "JUAL ") + price;
        row.buttonLabel.fontSize = 23;
        row.buttonLabel.fontStyle = FontStyle.Bold;
        row.buttonLabel.alignment = TextAnchor.MiddleCenter;
        row.buttonLabel.color = Color.white;
        row.buttonLabel.raycastTarget = false;

        Row captured = row;
        row.button.onClick.AddListener(() => HandleRow(captured));

        rows.Add(row);
        return row;
    }

    // ------------------------------------------------------------------
    // Aksi
    // ------------------------------------------------------------------
    void HandleRow(Row row)
    {
        if (row == null) return;

        if (inventory == null) inventory = InventorySystem.Instance;
        if (currency == null) currency = CurrencySystem.Instance;
        if (inventory == null || currency == null) return;

        if (row.buy)
        {
            if (!currency.TrySpend(row.price))
            {
                ShowMessage("Koin tidak cukup.", new Color(1f, 0.7f, 0.6f));
                return;
            }

            if (!inventory.AddItem(row.itemId, 1))
            {
                currency.Add(row.price);
                ShowMessage("Inventory penuh.", new Color(1f, 0.7f, 0.6f));
                return;
            }

            ShowMessage("Membeli 1 " + ItemName(row.itemId) + " (-" + row.price + " koin).",
                new Color(0.72f, 1f, 0.76f));
        }
        else
        {
            if (inventory.CountItem(row.itemId) <= 0)
            {
                ShowMessage("Tidak ada " + ItemName(row.itemId) + " untuk dijual.",
                    new Color(1f, 0.7f, 0.6f));
                return;
            }

            if (!inventory.RemoveItem(row.itemId, 1)) return;

            currency.Add(row.price);
            ShowMessage("Menjual 1 " + ItemName(row.itemId) + " (+" + row.price + " koin).",
                new Color(0.72f, 1f, 0.76f));
        }

        Refresh();
    }

    string ItemName(string itemId)
    {
        if (inventory != null)
        {
            InventoryItemDefinition definition = inventory.GetDefinition(itemId);
            if (definition != null) return definition.displayName;
        }

        return itemId;
    }

    Color ItemColor(string itemId)
    {
        if (inventory != null)
        {
            InventoryItemDefinition definition = inventory.GetDefinition(itemId);
            if (definition != null) return definition.color;
        }

        return new Color(0.6f, 0.7f, 0.6f);
    }

    // ------------------------------------------------------------------
    // Refresh
    // ------------------------------------------------------------------
    void Refresh()
    {
        if (inventory == null) inventory = InventorySystem.Instance;
        if (currency == null) currency = CurrencySystem.Instance;

        if (coinsText != null)
            coinsText.text = (currency != null ? currency.coins : 0) + " koin";

        for (int i = 0; i < rows.Count; i++)
        {
            Row row = rows[i];
            int owned = inventory != null ? inventory.CountItem(row.itemId) : 0;

            if (row.detail != null)
            {
                row.detail.text = row.buy
                    ? "Harga " + row.price + " koin   -   punya " + owned
                    : "Laku " + row.price + " koin   -   punya " + owned;
            }

            if (row.button != null)
            {
                row.button.interactable = row.buy
                    ? currency != null && currency.CanAfford(row.price)
                    : owned > 0;
            }

            if (row.accent != null && row.buy)
                row.accent.color = ItemColor(row.itemId);
        }
    }

    void ShowMessage(string message, Color color)
    {
        if (messageText == null) return;

        messageText.text = message;
        messageText.color = color;
        messageUntil = Time.unscaledTime + 3f;
    }

    // ------------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------------
    static RectTransform MakeRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static Font UiFont
    {
        get
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
