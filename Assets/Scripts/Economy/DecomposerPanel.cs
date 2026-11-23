using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [General] Panel dekomposer: 3 kotak independen. Tiap kotak butuh 2 mayat hama,
/// memproses 10 detik, lalu menghasilkan 1 pupuk.
/// Kartu menampilkan slot hama, progress bar, dan satu tombol aksi yang berubah
/// sesuai keadaan kotak.
/// </summary>
[DisallowMultipleComponent]
public class DecomposerPanel : MonoBehaviour
{
    public static DecomposerPanel Instance { get; private set; }

    [Header("Root")]
    public GameObject root;
    public GameObject blocker;

    [Header("Text")]
    public Text titleText;
    public Text hintText;
    public Text pestCountText;
    public Text fertilizerCountText;
    public Text messageText;

    [Header("Cards")]
    public RectTransform boxRow;

    [Header("Buttons")]
    public Button closeButton;
    public Button blockerButton;

    [Header("References")]
    public DecomposerStation station;
    public InventorySystem inventory;

    public bool IsOpen => root != null && root.activeSelf;
    public static bool AnyOpen => Instance != null && Instance.IsOpen;

    class Card
    {
        public DecomposerBox box;
        public Image background;
        public Text badge;
        public Text status;
        public Image[] slots;
        public Image fill;
        public Button button;
        public Text buttonLabel;
    }

    readonly List<Card> cards = new List<Card>();
    bool built;
    float messageUntil;
    float nextRefresh;

    const float CardWidth = 320f;
    const float CardHeight = 400f;

    static readonly Color CardIdle = new Color(0.11f, 0.14f, 0.17f, 0.98f);
    static readonly Color CardHalf = new Color(0.20f, 0.15f, 0.26f, 0.98f);
    static readonly Color CardBusy = new Color(0.26f, 0.19f, 0.10f, 0.98f);
    static readonly Color CardReady = new Color(0.11f, 0.24f, 0.15f, 0.98f);

    static readonly Color SlotEmpty = new Color(0.24f, 0.27f, 0.30f, 1f);
    static readonly Color SlotFull = new Color(0.72f, 0.45f, 0.95f, 1f);

    void Awake()
    {
        Instance = this;
        if (inventory == null) inventory = InventorySystem.Instance;
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

    public void Open(DecomposerStation decomposer, DecomposerBox focus = null)
    {
        station = decomposer;

        if (TreeCarePanel.Instance != null) TreeCarePanel.Instance.Close();
        if (ShopPanel.Instance != null) ShopPanel.Instance.Close();

        if (inventory == null) inventory = InventorySystem.Instance;

        SetVisible(true);
        BuildCards();
        ShowMessage("Tap MASUKKAN HAMA dua kali di satu kotak, lalu tunggu 10 detik.",
            new Color(0.82f, 0.9f, 1f));
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
            nextRefresh = Time.unscaledTime + 0.15f;
            Refresh();
        }

        if (messageText != null && messageUntil > 0f && Time.unscaledTime >= messageUntil)
        {
            messageUntil = 0f;
            messageText.text = "";
        }
    }

    // ------------------------------------------------------------------
    // Kartu kotak
    // ------------------------------------------------------------------
    void BuildCards()
    {
        if (boxRow == null) return;

        bool needsRebuild = !built || cards.Count == 0 || cards[0].box == null;
        if (!needsRebuild && station != null && station.boxes != null)
        {
            for (int i = 0; i < cards.Count && i < station.boxes.Length; i++)
                if (cards[i].box != station.boxes[i]) needsRebuild = true;
        }

        if (!needsRebuild) return;

        for (int i = boxRow.childCount - 1; i >= 0; i--)
            Destroy(boxRow.GetChild(i).gameObject);

        cards.Clear();

        if (station == null || station.boxes == null) return;

        for (int i = 0; i < station.boxes.Length; i++)
        {
            DecomposerBox box = station.boxes[i];
            if (box == null) continue;

            cards.Add(CreateCard(i, box));
        }

        built = true;
    }

    Card CreateCard(int index, DecomposerBox box)
    {
        Card card = new Card { box = box };

        GameObject cardObject = new GameObject("BoxCard_" + index,
            typeof(RectTransform), typeof(LayoutElement), typeof(Image));
        cardObject.transform.SetParent(boxRow, false);

        RectTransform cardRect = (RectTransform)cardObject.transform;
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);

        LayoutElement layout = cardObject.GetComponent<LayoutElement>();
        layout.preferredWidth = CardWidth;
        layout.preferredHeight = CardHeight;

        card.background = cardObject.GetComponent<Image>();
        card.background.color = CardIdle;

        // Judul kotak.
        RectTransform titleRect = MakeRect("Title", cardObject.transform);
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(16f, -58f);
        titleRect.offsetMax = new Vector2(-16f, -14f);

        Text title = titleRect.gameObject.AddComponent<Text>();
        title.font = UiFont;
        title.text = "KOTAK " + (index + 1);
        title.fontSize = 26;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.70f, 0.96f, 0.76f);
        title.raycastTarget = false;

        // Badge keadaan.
        RectTransform badgeRect = MakeRect("Badge", cardObject.transform);
        badgeRect.anchorMin = new Vector2(0.5f, 1f);
        badgeRect.anchorMax = new Vector2(0.5f, 1f);
        badgeRect.pivot = new Vector2(0.5f, 1f);
        badgeRect.sizeDelta = new Vector2(200f, 34f);
        badgeRect.anchoredPosition = new Vector2(0f, -62f);

        card.badge = badgeRect.gameObject.AddComponent<Text>();
        card.badge.font = UiFont;
        card.badge.text = "KOSONG";
        card.badge.fontSize = 20;
        card.badge.fontStyle = FontStyle.Bold;
        card.badge.alignment = TextAnchor.MiddleCenter;
        card.badge.color = new Color(0.66f, 0.74f, 0.8f);
        card.badge.raycastTarget = false;

        // Slot hama (2 bulatan).
        RectTransform slotRow = MakeRect("Slots", cardObject.transform);
        slotRow.anchorMin = new Vector2(0.5f, 1f);
        slotRow.anchorMax = new Vector2(0.5f, 1f);
        slotRow.pivot = new Vector2(0.5f, 1f);
        slotRow.sizeDelta = new Vector2(160f, 52f);
        slotRow.anchoredPosition = new Vector2(0f, -104f);

        HorizontalLayoutGroup slotLayout = slotRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        slotLayout.spacing = 16f;
        slotLayout.childAlignment = TextAnchor.MiddleCenter;
        slotLayout.childControlWidth = false;
        slotLayout.childControlHeight = false;
        slotLayout.childForceExpandWidth = false;
        slotLayout.childForceExpandHeight = false;

        int slotCount = Mathf.Max(1, box.pestRequirement);
        card.slots = new Image[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            RectTransform slotRect = MakeRect("Slot_" + i, slotRow);
            slotRect.sizeDelta = new Vector2(44f, 44f);

            card.slots[i] = slotRect.gameObject.AddComponent<Image>();
            card.slots[i].color = SlotEmpty;
            card.slots[i].raycastTarget = false;
        }

        // Status.
        RectTransform statusRect = MakeRect("Status", cardObject.transform);
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.pivot = new Vector2(0.5f, 1f);
        statusRect.offsetMin = new Vector2(16f, -230f);
        statusRect.offsetMax = new Vector2(-16f, -166f);

        card.status = statusRect.gameObject.AddComponent<Text>();
        card.status.font = UiFont;
        card.status.text = "Hama 0/2";
        card.status.fontSize = 24;
        card.status.fontStyle = FontStyle.Bold;
        card.status.alignment = TextAnchor.MiddleCenter;
        card.status.color = new Color(0.94f, 0.9f, 0.8f);
        card.status.raycastTarget = false;

        // Progress bar.
        RectTransform barRect = MakeRect("Progress", cardObject.transform);
        barRect.anchorMin = new Vector2(0f, 1f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.offsetMin = new Vector2(24f, -262f);
        barRect.offsetMax = new Vector2(-24f, -240f);

        Image barBackground = barRect.gameObject.AddComponent<Image>();
        barBackground.color = new Color(0f, 0f, 0f, 0.45f);
        barBackground.raycastTarget = false;

        RectTransform fillRect = MakeRect("Fill", barRect);
        Stretch(fillRect);

        card.fill = fillRect.gameObject.AddComponent<Image>();
        card.fill.color = new Color(0.72f, 0.52f, 0.16f, 1f);
        card.fill.raycastTarget = false;
        SetBarFill(card.fill, 0f);

        // Tombol aksi.
        GameObject buttonObject = new GameObject("Action", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(cardObject.transform, false);

        RectTransform buttonRect = (RectTransform)buttonObject.transform;
        buttonRect.anchorMin = new Vector2(0f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.offsetMin = new Vector2(18f, 24f);
        buttonRect.offsetMax = new Vector2(-18f, 100f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.36f, 0.24f, 0.52f, 1f);

        card.button = buttonObject.GetComponent<Button>();
        card.button.targetGraphic = buttonImage;
        card.button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = card.button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 1f, 0.9f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.45f);
        card.button.colors = colors;

        RectTransform labelRect = MakeRect("Label", buttonObject.transform);
        Stretch(labelRect);

        card.buttonLabel = labelRect.gameObject.AddComponent<Text>();
        card.buttonLabel.font = UiFont;
        card.buttonLabel.text = "MASUKKAN HAMA";
        card.buttonLabel.fontSize = 21;
        card.buttonLabel.fontStyle = FontStyle.Bold;
        card.buttonLabel.alignment = TextAnchor.MiddleCenter;
        card.buttonLabel.color = Color.white;
        card.buttonLabel.raycastTarget = false;

        DecomposerBox captured = box;
        card.button.onClick.AddListener(() => HandleCardClick(captured));

        return card;
    }

    // ------------------------------------------------------------------
    // Aksi
    // ------------------------------------------------------------------
    void HandleCardClick(DecomposerBox box)
    {
        if (box == null) return;

        if (box.readyFertilizer > 0)
            CollectFertilizer(box);
        else
            InsertPest(box);
    }

    void InsertPest(DecomposerBox box)
    {
        if (box == null) return;
        if (inventory == null) inventory = InventorySystem.Instance;
        if (inventory == null) return;

        if (box.processing)
        {
            ShowMessage("Kotak " + (cards.FindIndex(c => c.box == box) + 1) + " masih memproses.",
                new Color(1f, 0.8f, 0.55f));
            return;
        }

        if (inventory.CountItem(GameItems.PestCorpse) <= 0)
        {
            ShowMessage("Tidak ada mayat hama. Bunuh hama dulu di mode FPS.",
                new Color(1f, 0.7f, 0.6f));
            return;
        }

        if (!inventory.RemoveItem(GameItems.PestCorpse, 1))
        {
            ShowMessage("Gagal mengambil mayat hama.", new Color(1f, 0.7f, 0.6f));
            return;
        }

        if (!box.TryAddPest(out string message))
        {
            inventory.AddItem(GameItems.PestCorpse, 1);
            ShowMessage(message, new Color(1f, 0.7f, 0.6f));
            return;
        }

        ShowMessage(message, new Color(0.72f, 1f, 0.76f));
        Refresh();
    }

    void CollectFertilizer(DecomposerBox box)
    {
        if (box == null) return;

        bool success = box.TryCollectFertilizer(out string message);
        ShowMessage(message, success
            ? new Color(0.72f, 1f, 0.76f)
            : new Color(1f, 0.7f, 0.6f));

        Refresh();
    }

    // ------------------------------------------------------------------
    // Refresh
    // ------------------------------------------------------------------
    void Refresh()
    {
        if (inventory == null) inventory = InventorySystem.Instance;

        if (titleText != null)
            titleText.text = station != null ? station.stationName : "Dekomposer Hama";

        int pests = inventory != null ? inventory.CountItem(GameItems.PestCorpse) : 0;
        int fertilizer = inventory != null ? inventory.CountItem(GameItems.Fertilizer) : 0;

        if (pestCountText != null) pestCountText.text = pests + " hama";
        if (fertilizerCountText != null) fertilizerCountText.text = fertilizer + " pupuk";

        for (int i = 0; i < cards.Count; i++)
        {
            Card card = cards[i];
            if (card.box == null) continue;

            DecomposerBox box = card.box;

            if (card.status != null) card.status.text = box.StatusText;

            if (card.fill != null)
            {
                SetBarFill(card.fill, box.ProcessProgress);
                card.fill.color = box.readyFertilizer > 0
                    ? new Color(0.35f, 0.85f, 0.42f, 1f)
                    : new Color(0.85f, 0.62f, 0.18f, 1f);
            }

            if (card.slots != null)
            {
                int filled = box.processing ? card.slots.Length : box.StoredPests;
                for (int s = 0; s < card.slots.Length; s++)
                {
                    if (card.slots[s] == null) continue;
                    card.slots[s].color = s < filled ? SlotFull : SlotEmpty;
                }
            }

            if (card.background != null)
            {
                card.background.color = box.processing
                    ? CardBusy
                    : box.readyFertilizer > 0
                        ? CardReady
                        : box.StoredPests > 0
                            ? CardHalf
                            : CardIdle;
            }

            if (card.badge != null)
            {
                card.badge.text = box.processing
                    ? "MEMPROSES"
                    : box.readyFertilizer > 0
                        ? "SIAP"
                        : box.StoredPests > 0
                            ? "TERISI"
                            : "KOSONG";

                card.badge.color = box.processing
                    ? new Color(1f, 0.82f, 0.45f)
                    : box.readyFertilizer > 0
                        ? new Color(0.65f, 1f, 0.68f)
                        : new Color(0.66f, 0.74f, 0.8f);
            }

            if (card.button != null)
            {
                card.button.interactable = box.readyFertilizer > 0 ||
                                           (!box.processing && pests > 0);
            }

            if (card.buttonLabel != null)
            {
                if (box.readyFertilizer > 0) card.buttonLabel.text = "AMBIL PUPUK";
                else if (box.processing) card.buttonLabel.text = "TUNGGU...";
                else if (pests <= 0) card.buttonLabel.text = "BUTUH HAMA";
                else card.buttonLabel.text = "MASUKKAN HAMA (" + box.StoredPests + "/" +
                                              box.PestRequirement + ")";
            }
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
    static void SetBarFill(Image fill, float amount)
    {
        if (fill == null) return;

        RectTransform rect = fill.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

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
