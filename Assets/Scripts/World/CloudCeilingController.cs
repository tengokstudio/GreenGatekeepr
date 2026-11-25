using UnityEngine;

/// <summary>
/// [General] Langit-langit awan tebal yang menutup seluruh dunia saat mode FPS.
/// Sengaja DISSEMBUNYIKAN di mode isometrik supaya tidak menghalangi editing.
///
/// Awan ikut reaksi dengan polusi: makin banyak pohon ditanam, makin tipis /
/// makin transparan awannya (mengikuti PollutionFogController.Progress).
/// Tidak ada lagi awan-awan kecil bertebaran.
/// </summary>
[DisallowMultipleComponent]
public class CloudCeilingController : MonoBehaviour
{
    [Header("Bentuk awan")]
    [Tooltip("Tinggi langit-langit awan di atas tanah.")]
    public float ceilingHeight = 20f;

    [Tooltip("Lebar area yang ditutup awan (meter).")]
    public float coverSize = 130f;

    [Tooltip("Jumlah ubin awan per sisi.")]
    public int tiles = 7;

    [Tooltip("Diameter tiap ubin awan (meter).")]
    public float puffDiameter = 38f;

    [Tooltip("Ketebalan tiap ubin awan (meter).")]
    public float puffThickness = 4.2f;

    [Header("Polusi -> bersih")]
    public Color pollutedColor = new Color(0.21f, 0.21f, 0.20f, 1f);
    public float pollutedAlpha = 0.97f;
    public float clearAlpha = 0f;

    GameObject puffsRoot;
    Material material;
    PollutionFogController pollution;
    CameraSwitcher switcher;

    void Awake()
    {
        if (switcher == null) switcher = FindFirstObjectByType<CameraSwitcher>();
        Build();
    }

    void Build()
    {
        if (puffsRoot != null) return;

        puffsRoot = new GameObject("CloudCeiling_Puffs");
        puffsRoot.hideFlags = HideFlags.DontSave;
        puffsRoot.transform.SetParent(transform, false);
        puffsRoot.transform.localPosition = Vector3.zero;

        Shader shader = Shader.Find("GreenGatekeeper/UnlitNoFogTransparent");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");

        material = shader != null ? new Material(shader) : null;
        if (material != null)
        {
            material.hideFlags = HideFlags.DontSave;
            Color color = pollutedColor;
            color.a = pollutedAlpha;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            material.color = color;
            material.renderQueue = 3100;
        }

        int count = Mathf.Max(3, tiles);
        float step = coverSize / count;
        float offset = (count - 1) * 0.5f;

        for (int x = 0; x < count; x++)
        {
            for (int z = 0; z < count; z++)
            {
                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = "CloudSlab_" + x + "_" + z;
                puff.hideFlags = HideFlags.DontSave;
                puff.transform.SetParent(puffsRoot.transform, false);

                float jitter = Mathf.Sin((x + 1) * 2.7f + (z + 1) * 5.1f);
                puff.transform.localPosition = new Vector3(
                    (x - offset) * step,
                    ceilingHeight + jitter * 1.3f,
                    (z - offset) * step);
                puff.transform.localScale = new Vector3(
                    puffDiameter, puffThickness * (1f + 0.12f * jitter), puffDiameter);

                Collider collider = puff.GetComponent<Collider>();
                if (collider != null) Destroy(collider);

                MeshRenderer renderer = puff.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
        }

        puffsRoot.SetActive(false);
    }

    void Update()
    {
        if (switcher == null) switcher = FindFirstObjectByType<CameraSwitcher>();
        if (pollution == null) pollution = FindFirstObjectByType<PollutionFogController>();

        float progress = pollution != null ? pollution.Progress : 0f;
        float alpha = Mathf.Lerp(pollutedAlpha, clearAlpha, Mathf.Clamp01(progress));

        if (material != null)
        {
            Color color = pollutedColor;
            color.a = alpha;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            material.color = color;
        }

        bool fps = switcher != null &&
                   switcher.CurrentMode == CameraSwitcher.Mode.FirstPerson;
        bool show = fps && alpha > 0.01f;

        if (puffsRoot != null && puffsRoot.activeSelf != show)
            puffsRoot.SetActive(show);
    }
}
