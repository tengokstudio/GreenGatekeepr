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
/// Satu-satunya syarat tumbuh adalah disiram memakai Penyiram (alat permanen).
/// Setelah disiram, tahap berjalan <see cref="stageDuration"/> detik.
/// Pupuk opsional: mempercepat 30% dan menambah HP maksimum 25%, hanya untuk
/// pohon yang belum dewasa. Lampu matahari bukan lagi perawatan — ia dipasang
/// di grid saat edit mode dan menentukan sel mana yang boleh ditanami.
/// Setelah dewasa, pohon otomatis menghasilkan komoditas tiap <see cref="commodityInterval"/> detik.
/// Komponen ini HANYA ada di pohon yang ditanam player. Pohon hutan / defense
/// tidak punya komponen ini dan dianggap sudah dewasa.
/// </summary>
[DisallowMultipleComponent]
public class TreeGrowth : MonoBehaviour
{
    public const int MatureStage = 3;

    // Pupuk: 10 detik -> 7 detik, HP maks 200 -> 250.
    public const float FertilizerSpeedFactor = 0.70f;
    public const float FertilizerHealthFactor = 1.25f;

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

    [System.Obsolete("Lampu sekarang dipasang di grid, bukan dipakai saat rawat.")]
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
    float baseMaxHealth;

    public bool IsMature => stageIndex >= MatureStage;
    public bool IsGrowing => waterApplied && !IsMature;
    public bool CareComplete => waterApplied;

    /// <summary>Durasi tahap setelah memperhitungkan pupuk.</summary>
    public float EffectiveStageDuration => stageDuration *
        (fertilizerApplied ? FertilizerSpeedFactor : 1f);

    public string StageName
    {
        get
        {
            int index = Mathf.Clamp(stageIndex, 0, stageNames.Length - 1);
            return stageNames[index];
        }
    }

    public float RemainingTime => IsGrowing
        ? Mathf.Max(0f, EffectiveStageDuration - stageTimer)
        : EffectiveStageDuration;

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
            if (stageTimer >= EffectiveStageDuration)
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
        matureAt = -1f;
        baseMaxHealth = variant != null && variant.maxHealth > 0f
            ? variant.maxHealth
            : (tree != null ? tree.maxHealth : 150f);

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
            return new TreeCareResult(false, "Pohon sudah dewasa. Pupuk tidak bisa dipakai lagi.");

        InventorySystem inventory = InventorySystem.Instance;

        switch (type)
        {
            case TreeCareType.Fertilizer:
                if (fertilizerApplied)
                    return new TreeCareResult(false, "Pupuk untuk tahap ini sudah dipakai.");
                if (inventory == null || inventory.CountItem(GameItems.Fertilizer) <= 0)
                    return new TreeCareResult(false, "Pupuk habis. Buat pupuk di dekomposer.");
                if (!inventory.RemoveItem(GameItems.Fertilizer, 1))
                    return new TreeCareResult(false, "Pupuk gagal dipakai.");

                fertilizerApplied = true;
                tree.SetMaxHealth(baseMaxHealth * FertilizerHealthFactor, true);
                RefreshLabel();
                return new TreeCareResult(true, "Pupuk dipakai: tumbuh 30% lebih cepat, " +
                    "HP maks +25%." + (waterApplied
                        ? " Sisa " + Mathf.CeilToInt(RemainingTime) + " detik."
                        : " Siram dengan Penyiram untuk mulai tumbuh."));

            case TreeCareType.Water:
                if (waterApplied)
                    return new TreeCareResult(false, "Pohon sudah disiram dan sedang tumbuh.");
                if (inventory == null || inventory.CountItem(GameItems.WateringCan) <= 0)
                    return new TreeCareResult(false, "Butuh Penyiram (watering can) di inventory.");

                WateringCanSupply supply = WateringCanSupply.Ensure();
                if (supply == null)
                    return new TreeCareResult(false, "Persediaan air Penyiram tidak ditemukan.");
                if (!supply.HasWater)
                    return new TreeCareResult(false,
                        "Air Penyiram habis. Isi ulang dengan men-tap kolam.");
                if (!supply.Consume(1))
                    return new TreeCareResult(false,
                        "Air Penyiram habis. Isi ulang dengan men-tap kolam.");

                if (!waterApplied) stageTimer = 0f;
                waterApplied = true;
                RefreshLabel();
                return new TreeCareResult(true, StageName + " disiram! Tumbuh dalam " +
                    Mathf.RoundToInt(EffectiveStageDuration) + " detik. Air tersisa " +
                    supply.Label + ".");

            case TreeCareType.Lamp:
                return new TreeCareResult(false,
                    "Lampu Matahari dipasang di grid saat edit mode, bukan dipakai di sini.");
        }

        RefreshLabel();
        return new TreeCareResult(true, CareSummary());
    }

    string CareSummary()
    {
        string water = waterApplied ? "Air:OK" : "Air:perlu disiram";
        string fertilizer = fertilizerApplied ? "Pupuk:OK (+30% cepat, +25% HP)" : "Pupuk:opsional";
        return StageName + "  " + water + "  " + fertilizer;
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

        // Bonus pupuk tidak menumpuk antar tahap; ApplyStageVisuals mengembalikan
        // HP maksimum ke nilai dasar tahap ini.
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
            TreeEditController.Instance.ShowHint("Tumbuh menjadi " + StageName + ". Siram lagi dengan Penyiram (pupuk opsional).", 3f);
    }

    void ApplyStageVisuals()
    {
        TreeModelStages modelStages = GetComponent<TreeModelStages>();

        if (modelStages != null && modelStages.IsReady)
        {
            // Model 3D: tiap tahap memakai ukuran mesh sendiri, jadi tidak perlu
            // menskalakan root secara berlebihan.
            modelStages.SetStage(stageIndex);
            transform.localScale = Vector3.one * modelStages.rootScale;

            float height = modelStages.HeightFor(stageIndex);
            if (tree != null && tree.healthBarRoot != null)
                tree.healthBarRoot.transform.localPosition = new Vector3(0f, height + 0.7f, 0f);

            labelHeight = height + 1.1f;
        }
        else
        {
            int index = Mathf.Clamp(stageIndex, 0, stageScales.Length - 1);
            float scale = stageScales[index];
            transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }

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
