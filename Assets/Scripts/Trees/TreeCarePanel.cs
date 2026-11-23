using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [General] Panel perawatan pohon. Terbuka saat bibit/tunas di-tap.
/// Tiga tombol: Pupuk (consumable), Air (butuh penyiram), Lampu (butuh sun lamp).
/// Panel tidak memakai layar dim; ada blocker transparan full screen supaya tap
/// di luar panel menutup panel dan tidak tembus ke dunia.
/// </summary>
[DisallowMultipleComponent]
public class TreeCarePanel : MonoBehaviour
{
    public static TreeCarePanel Instance { get; private set; }

    [Header("Root")]
    public GameObject root;
    public GameObject blocker;

    [Header("Text")]
    public Text titleText;
    public Text stageText;
    public Text toolsText;
    public Text messageText;

    [Header("Buttons")]
    public Button fertilizerButton;
    public Button waterButton;
    public Button lampButton;
    public Button closeButton;
    public Button blockerButton;

    [Header("Button Labels")]
    public Text fertilizerLabel;
    public Text waterLabel;
    public Text lampLabel;

    public TreeGrowth CurrentTree { get; private set; }
    public bool IsOpen => root != null && root.activeSelf;

    float messageUntil;

    void Awake()
    {
        Instance = this;
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

    public static bool AnyOpen => Instance != null && Instance.IsOpen;

    // ------------------------------------------------------------------
    // Buka / tutup
    // ------------------------------------------------------------------
    public void Open(TreeGrowth growth)
    {
        if (growth == null) return;

        if (ShopPanel.Instance != null) ShopPanel.Instance.Close();
        if (DecomposerPanel.Instance != null) DecomposerPanel.Instance.Close();

        CurrentTree = growth;
        SetVisible(true);
        ShowMessage("Beri pupuk + air + lampu untuk menaikkan tahap.", new Color(0.82f, 0.9f, 1f));
        Refresh();
    }

    public void Close()
    {
        CurrentTree = null;
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

        if (CurrentTree == null || CurrentTree.tree == null || !CurrentTree.tree.IsAlive)
        {
            Close();
            return;
        }

        if (CurrentTree.IsMature)
        {
            Close();
            return;
        }

        Refresh();

        if (messageText != null && messageUntil > 0f && Time.unscaledTime >= messageUntil)
        {
            messageUntil = 0f;
            messageText.text = "";
        }
    }

    // ------------------------------------------------------------------
    // Tombol
    // ------------------------------------------------------------------
    public void ApplyFertilizer() => Apply(TreeCareType.Fertilizer);
    public void ApplyWater() => Apply(TreeCareType.Water);
    public void ApplyLamp() => Apply(TreeCareType.Lamp);

    void Apply(TreeCareType type)
    {
        if (CurrentTree == null) return;

        TreeCareResult result = CurrentTree.ApplyCare(type);
        ShowMessage(result.message, result.success
            ? new Color(0.72f, 1f, 0.76f)
            : new Color(1f, 0.72f, 0.6f));

        Refresh();
    }

    // ------------------------------------------------------------------
    // Refresh
    // ------------------------------------------------------------------
    void Refresh()
    {
        if (CurrentTree == null) return;

        TreeGrowth growth = CurrentTree;
        TreeVariant variant = growth.variant;

        if (titleText != null)
            titleText.text = variant != null ? variant.displayName : "Pohon";

        if (stageText != null)
        {
            if (growth.IsGrowing)
            {
                stageText.text = growth.StageName + "  -  tumbuh " +
                                 Mathf.CeilToInt(growth.RemainingTime) + " detik lagi";
            }
            else
            {
                stageText.text = growth.StageName + "  (tahap " +
                                 (growth.stageIndex + 1) + "/3)  -  butuh pupuk + air + lampu";
            }
        }

        InventorySystem inventory = InventorySystem.Instance;

        if (toolsText != null)
        {
            int fertilizer = inventory != null ? inventory.CountItem(GameItems.Fertilizer) : 0;
            bool can = inventory != null && inventory.CountItem(GameItems.WateringCan) > 0;
            bool lamp = inventory != null && inventory.CountItem(GameItems.SunLamp) > 0;

            toolsText.text = "Pupuk: " + fertilizer +
                             "   |   Penyiram: " + (can ? "ada" : "tidak ada") +
                             "   |   Lampu: " + (lamp ? "ada" : "tidak ada");
        }

        bool locked = growth.IsGrowing || growth.IsMature;

        SetButton(fertilizerButton, fertilizerLabel, "PUPUK", growth.fertilizerApplied, locked);
        SetButton(waterButton, waterLabel, "AIR", growth.waterApplied, locked);
        SetButton(lampButton, lampLabel, "LAMPU", growth.lampApplied, locked);
    }

    static void SetButton(Button button, Text label, string baseLabel, bool applied, bool locked)
    {
        if (label != null) label.text = applied ? baseLabel + " (OK)" : baseLabel;
        if (button != null) button.interactable = !applied && !locked;
    }

    void ShowMessage(string message, Color color)
    {
        if (messageText == null) return;

        messageText.text = message;
        messageText.color = color;
        messageUntil = Time.unscaledTime + 3f;
    }
}
