using UnityEngine;

/// <summary>
/// [General] Satu kotak dekomposer. Butuh 2 mayat hama, lalu memproses 10 detik
/// dan menghasilkan 1 pupuk. Tiga kotak berjalan independen.
/// </summary>
[DisallowMultipleComponent]
public class DecomposerBox : MonoBehaviour
{
    [Header("Proses")]
    public int pestRequirement = 2;
    public float processDuration = 10f;
    public int maxReadyFertilizer = 2;

    [Header("State")]
    public int storedPests;
    public float processTimer;
    public bool processing;
    public int readyFertilizer;

    [Header("Visual")]
    public Renderer boxRenderer;
    public float labelHeight = 1.7f;

    public Color idleColor = new Color(0.34f, 0.24f, 0.14f);
    public Color halfColor = new Color(0.46f, 0.30f, 0.55f);
    public Color processingColor = new Color(0.75f, 0.48f, 0.12f);
    public Color readyColor = new Color(0.24f, 0.68f, 0.30f);

    TextMesh label;
    GameObject labelObject;
    MaterialPropertyBlock block;

    public bool IsBusy => processing;
    public bool IsReady => readyFertilizer > 0;
    public float RemainingTime => processing ? Mathf.Max(0f, processDuration - processTimer) : 0f;

    /// <summary>0..1 untuk progress bar di panel.</summary>
    public float ProcessProgress
    {
        get
        {
            if (processing) return Mathf.Clamp01(processTimer / Mathf.Max(0.01f, processDuration));
            return readyFertilizer > 0 ? 1f : 0f;
        }
    }

    public int StoredPests => storedPests;
    public int PestRequirement => Mathf.Max(1, pestRequirement);

    public string StatusText
    {
        get
        {
            if (processing) return "Memproses " + Mathf.CeilToInt(RemainingTime) + "s";
            if (readyFertilizer > 0) return "Pupuk siap: " + readyFertilizer;
            return "Hama " + storedPests + "/" + Mathf.Max(1, pestRequirement);
        }
    }

    void Awake()
    {
        if (boxRenderer == null) boxRenderer = GetComponentInChildren<Renderer>();
        CreateLabel();
        RefreshVisuals();
    }

    void Update()
    {
        if (!processing) return;

        processTimer += Time.deltaTime;
        if (processTimer >= processDuration)
        {
            processTimer = 0f;
            processing = false;
            storedPests = 0;
            readyFertilizer = Mathf.Min(Mathf.Max(1, maxReadyFertilizer), readyFertilizer + 1);
        }

        RefreshVisuals();
    }

    void LateUpdate()
    {
        if (labelObject == null) return;
        labelObject.transform.position = transform.position + Vector3.up * labelHeight;
    }

    // ------------------------------------------------------------------
    // API panel
    // ------------------------------------------------------------------
    public bool TryAddPest(out string message)
    {
        if (processing)
        {
            message = "Kotak ini sedang memproses.";
            return false;
        }

        if (readyFertilizer >= Mathf.Max(1, maxReadyFertilizer))
        {
            message = "Kotak penuh. Ambil pupuk dulu.";
            return false;
        }

        if (readyFertilizer > 0)
        {
            message = "Ambil pupuk yang sudah jadi dulu.";
            return false;
        }

        storedPests++;
        if (storedPests >= Mathf.Max(1, pestRequirement))
        {
            storedPests = 0;
            processing = true;
            processTimer = 0f;
            message = "2 hama masuk. Memproses " + Mathf.RoundToInt(processDuration) + " detik.";
        }
        else
        {
            message = "Hama masuk (" + storedPests + "/" + Mathf.Max(1, pestRequirement) + ").";
        }

        RefreshVisuals();
        return true;
    }

    public bool TryCollectFertilizer(out string message)
    {
        if (readyFertilizer <= 0)
        {
            message = "Belum ada pupuk di kotak ini.";
            return false;
        }

        if (InventorySystem.Instance == null ||
            !InventorySystem.Instance.AddItem(GameItems.Fertilizer, 1))
        {
            message = "Inventory penuh. Pupuk tidak bisa diambil.";
            return false;
        }

        readyFertilizer--;
        message = "1 pupuk masuk inventory.";
        RefreshVisuals();
        return true;
    }

    // ------------------------------------------------------------------
    // Visual
    // ------------------------------------------------------------------
    void RefreshVisuals()
    {
        if (label != null)
        {
            label.text = StatusText;
            label.color = processing
                ? new Color(1f, 0.82f, 0.45f)
                : readyFertilizer > 0
                    ? new Color(0.65f, 1f, 0.68f)
                    : new Color(0.92f, 0.86f, 0.74f);
        }

        if (boxRenderer == null) return;

        Color color = idleColor;
        if (processing) color = processingColor;
        else if (readyFertilizer > 0) color = readyColor;
        else if (storedPests > 0) color = halfColor;

        if (block == null) block = new MaterialPropertyBlock();
        boxRenderer.GetPropertyBlock(block);
        block.SetColor(Shader.PropertyToID("_BaseColor"), color);
        block.SetColor(Shader.PropertyToID("_Color"), color);
        boxRenderer.SetPropertyBlock(block);
    }

    void CreateLabel()
    {
        labelObject = new GameObject("DecomposerLabel");
        labelObject.transform.SetParent(transform, true);
        labelObject.transform.position = transform.position + Vector3.up * labelHeight;

        label = labelObject.AddComponent<TextMesh>();
        label.text = "Kosong";
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 64;
        label.characterSize = 0.055f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontStyle = FontStyle.Bold;
        label.color = new Color(0.92f, 0.86f, 0.74f);

        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 6;
        }

        labelObject.AddComponent<HealthBarBillboard>();
    }

    void OnDestroy()
    {
        if (labelObject != null) Destroy(labelObject);
    }
}
