using System.Collections;
using UnityEngine;

public enum TreeCareType
{
    Fertilizer,
    Water,
    Lamp
}

public struct TreeCareResult
{
    public bool success;
    public string message;

    public TreeCareResult(bool success, string message)
    {
        this.success = success;
        this.message = message;
    }
}

/// <summary>
/// [General] Pertumbuhan pohon tanam: Bibit -> Tunas -> Pohon Muda -> Pohon Dewasa.
/// Tiap tahap butuh 1 pupuk + air + lampu, lalu tumbuh selama <see cref="stageDuration"/> detik.
/// Air &amp; lampu memakai alat permanen (tidak habis); pupuk consumable.
/// Setelah dewasa, pohon otomatis menghasilkan komoditas tiap <see cref="commodityInterval"/> detik.
/// Komponen ini HANYA ada di pohon yang ditanam player. Pohon hutan / defense
/// tidak punya komponen ini dan dianggap sudah dewasa.
/// </summary>
[DisallowMultipleComponent]
public class TreeGrowth : MonoBehaviour
{
    public const int MatureStage = 3;

    [Header("Data")]
    public TreeDurability tree;
    public TreeVariant variant;

    [Header("Progress")]
    public int stageIndex;
    public float stageDuration = 10f;
    public float stageTimer;

    [Header("Perawatan tahap ini")]
    public bool fertilizerApplied;
    public bool waterApplied;
    public bool lampApplied;

    [Header("Hasil pohon dewasa")]
    [Tooltip("Pohon dewasa otomatis menambah 1 komoditas ke inventory tiap sekian detik.")]
    public float commodityInterval = 20f;

    [Tooltip("Timer internal, jangan diubah manual.")]
    public float commodityTimer;

    [Tooltip("Tinggi teks popup hasil panen di atas pohon.")]
    public float popupHeight = 7.4f;

    [Header("Visual")]
    public float[] stageScales = { 0.38f, 0.55f, 0.78f, 1f };
    public float[] stageHealthRatio = { 0.35f, 0.55f, 0.78f, 1f };
    public float labelHeight = 6.4f;

    public string[] stageNames = { "Bibit", "Tunas", "Pohon Muda", "Pohon Dewasa" };

    TextMesh label;
    GameObject labelObject;
    float nextLabelRefresh;
    float matureAt = -1f;

    public bool IsMature => stageIndex >= MatureStage;
    public bool IsGrowing => CareComplete && !IsMature;
    public bool CareComplete => fertilizerApplied && waterApplied && lampApplied;

    public string StageName
    {
        get
        {
            int index = Mathf.Clamp(stageIndex, 0, stageNames.Length - 1);
            return stageNames[index];
        }
    }

    public float RemainingTime => IsGrowing ? Mathf.Max(0f, stageDuration - stageTimer) : stageDuration;

    void Update()
    {
        if (tree == null) return;

        if (!tree.IsAlive)
        {
            DestroyLabel();
            return;
        }

        if (IsMature)
        {
            if (labelObject != null && matureAt > 0f && Time.time - matureAt > 4f)
                DestroyLabel();

            TickCommodity();
            return;
        }

        if (IsGrowing)
        {
            stageTimer += Time.deltaTime;
            if (stageTimer >= stageDuration)
                AdvanceStage();
        }

        if (labelObject != null && Time.unscaledTime >= nextLabelRefresh)
        {
            nextLabelRefresh = Time.unscaledTime + 0.15f;
            RefreshLabel();
        }
    }

    void LateUpdate()
    {
        if (labelObject == null || tree == null) return;
        labelObject.transform.position = tree.transform.position + Vector3.up * labelHeight;
    }

    /// <summary>Dipanggil TreeEditController tepat setelah pohon dibuat.</summary>
    public void Initialize(TreeDurability owner, TreeVariant treeVariant)
    {
        tree = owner;
        variant = treeVariant;

        stageIndex = 0;
        stageTimer = 0f;
        fertilizerApplied = false;
        waterApplied = false;
        lampApplied = false;
        matureAt = -1f;

        ApplyStageVisuals();
        CreateLabel();
        RefreshLabel();
    }

    // ------------------------------------------------------------------
    // Perawatan
    // ------------------------------------------------------------------
    public TreeCareResult ApplyCare(TreeCareType type)
    {
        if (tree == null || !tree.IsAlive)
            return new TreeCareResult(false, "Pohon sudah tidak ada.");

        if (IsMature)
            return new TreeCareResult(false, "Pohon sudah dewasa dan otomatis menghasilkan komoditas.");

        if (IsGrowing)
            return new TreeCareResult(false, "Pohon sedang tumbuh. Tunggu " +
                Mathf.CeilToInt(RemainingTime) + " detik lagi.");

        InventorySystem inventory = InventorySystem.Instance;

        switch (type)
        {
            case TreeCareType.Fertilizer:
                if (fertilizerApplied)
                    return new TreeCareResult(false, "Pupuk untuk tahap ini sudah diberikan.");
                if (inventory == null || inventory.CountItem(GameItems.Fertilizer) <= 0)
                    return new TreeCareResult(false, "Pupuk habis. Buat pupuk di dekomposer.");
                if (!inventory.RemoveItem(GameItems.Fertilizer, 1))
                    return new TreeCareResult(false, "Pupuk gagal dipakai.");
                fertilizerApplied = true;
                break;

            case TreeCareType.Water:
                if (waterApplied)
                    return new TreeCareResult(false, "Air untuk tahap ini sudah diberikan.");
                if (inventory == null || inventory.CountItem(GameItems.WateringCan) <= 0)
                    return new TreeCareResult(false, "Butuh Penyiram (watering can) di inventory.");
                waterApplied = true;
                break;

            case TreeCareType.Lamp:
                if (lampApplied)
                    return new TreeCareResult(false, "Lampu untuk tahap ini sudah diberikan.");
                if (inventory == null || inventory.CountItem(GameItems.SunLamp) <= 0)
                    return new TreeCareResult(false, "Butuh Lampu Matahari (sun lamp) di inventory.");
                lampApplied = true;
                break;
        }

        if (CareComplete)
        {
            stageTimer = 0f;
            RefreshLabel();
            return new TreeCareResult(true, "Perawatan lengkap! " + StageName +
                " akan tumbuh dalam " + Mathf.RoundToInt(stageDuration) + " detik.");
        }

        RefreshLabel();
        return new TreeCareResult(true, CareSummary());
    }

    string CareSummary()
    {
        string fertilizer = fertilizerApplied ? "Pupuk:OK" : "Pupuk:-";
        string water = waterApplied ? "Air:OK" : "Air:-";
        string lamp = lampApplied ? "Lampu:OK" : "Lampu:-";
        return StageName + "  " + fertilizer + "  " + water + "  " + lamp;
    }

    // ------------------------------------------------------------------
    // Tahap berikutnya
    // ------------------------------------------------------------------
    void AdvanceStage()
    {
        stageIndex = Mathf.Min(MatureStage, stageIndex + 1);
        stageTimer = 0f;
        fertilizerApplied = false;
        waterApplied = false;
        lampApplied = false;

        ApplyStageVisuals();

        if (IsMature)
        {
            matureAt = Time.time;
            commodityTimer = 0f;

            if (label != null) label.text = "Pohon Dewasa";

            if (TreeEditController.Instance != null)
            {
                string hasil = variant != null && !string.IsNullOrEmpty(variant.commodityName)
                    ? variant.commodityName
                    : "komoditas";

                TreeEditController.Instance.ShowHint("Pohon dewasa! Menghasilkan " + hasil +
                    " tiap " + Mathf.RoundToInt(commodityInterval) + " detik.", 3f);
            }

            return;
        }

        RefreshLabel();

        if (TreeEditController.Instance != null)
            TreeEditController.Instance.ShowHint("Tumbuh menjadi " + StageName + ". Beri pupuk + air + lampu lagi.", 3f);
    }

    void ApplyStageVisuals()
    {
        int index = Mathf.Clamp(stageIndex, 0, stageScales.Length - 1);
        float scale = stageScales[index];
        transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);

        if (tree != null && variant != null)
        {
            float ratio = stageHealthRatio[Mathf.Clamp(stageIndex, 0, stageHealthRatio.Length - 1)];
            tree.SetMaxHealth(variant.maxHealth * ratio, true);
        }
    }

    // ------------------------------------------------------------------
    // Hasil panen otomatis (pohon dewasa)
    // ------------------------------------------------------------------
    void TickCommodity()
    {
        if (variant == null || string.IsNullOrEmpty(variant.commodityItemId)) return;

        float interval = Mathf.Max(1f, commodityInterval);
        commodityTimer += Time.deltaTime;
        if (commodityTimer < interval) return;

        InventorySystem inventory = InventorySystem.Instance;
        if (inventory == null)
        {
            commodityTimer = 0f;
            return;
        }

        if (!inventory.AddItem(variant.commodityItemId, 1))
        {
            commodityTimer = 0f;
            SpawnPopup("Inventory penuh", new Color(1f, 0.68f, 0.55f));
            return;
        }

        commodityTimer = 0f;

        string label = string.IsNullOrEmpty(variant.commodityName)
            ? variant.commodityItemId
            : variant.commodityName;

        SpawnPopup("+1 " + label, new Color(1f, 0.9f, 0.45f));
    }

    void SpawnPopup(string message, Color color)
    {
        if (tree == null) return;

        GameObject popup = new GameObject("GainPopup");
        popup.transform.SetParent(tree.transform.parent, true);
        popup.transform.position = tree.transform.position + Vector3.up * popupHeight;

        TextMesh text = popup.AddComponent<TextMesh>();
        text.text = message;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 64;
        text.characterSize = 0.05f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontStyle = FontStyle.Bold;
        text.color = color;

        MeshRenderer renderer = popup.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 7;
        }

        popup.AddComponent<HealthBarBillboard>();
        StartCoroutine(AnimatePopup(popup, text));
    }

    IEnumerator AnimatePopup(GameObject popup, TextMesh text)
    {
        const float duration = 1.6f;
        float time = 0f;
        Vector3 start = popup != null ? popup.transform.position : Vector3.zero;
        Color baseColor = text != null ? text.color : Color.white;

        while (time < duration && popup != null)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            popup.transform.position = start + Vector3.up * (1.1f * t);
            if (text != null)
                text.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t);

            yield return null;
        }

        if (popup != null) Destroy(popup);
    }

    // ------------------------------------------------------------------
    // Label dunia
    // ------------------------------------------------------------------
    void CreateLabel()
    {
        if (labelObject != null) return;

        labelObject = new GameObject("GrowthLabel");
        labelObject.transform.SetParent(tree.transform.parent, true);
        labelObject.transform.position = tree.transform.position + Vector3.up * labelHeight;

        label = labelObject.AddComponent<TextMesh>();
        label.text = StageName;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 64;
        label.characterSize = 0.06f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontStyle = FontStyle.Bold;
        label.color = new Color(0.78f, 1f, 0.82f);

        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 6;
        }

        labelObject.AddComponent<HealthBarBillboard>();
    }

    void RefreshLabel()
    {
        if (label == null) return;

        if (IsMature)
        {
            label.text = "Pohon Dewasa";
            label.color = new Color(1f, 0.92f, 0.45f);
        }
        else if (IsGrowing)
        {
            label.text = StageName + "  -  tumbuh " + Mathf.CeilToInt(RemainingTime) + "s";
            label.color = new Color(0.72f, 0.92f, 1f);
        }
        else
        {
            label.text = CareSummary();
            label.color = new Color(0.78f, 1f, 0.82f);
        }
    }

    void DestroyLabel()
    {
        if (labelObject != null) Destroy(labelObject);
        labelObject = null;
        label = null;
    }

    void OnDestroy()
    {
        DestroyLabel();
    }
}
