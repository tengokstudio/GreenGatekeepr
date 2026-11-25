using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [Isometric] Lampu Matahari sebagai item yang bisa dipasang di grid.
///
/// - Dipilih dari inventory saat edit mode, lalu tap tengah sel untuk memasang.
/// - Sel dalam radius <see cref="radius"/> dari lampu dianggap "tersinari" dan
///   boleh ditanami bibit. Di luar radius, penanaman ditolak.
/// - Cahaya dan cincin cahaya di tanah berdenyut supaya jelas area yang tersinari.
/// - Lampu permanen; tap lampu dengan item ini aktif untuk mengambilnya lagi.
/// </summary>
[DisallowMultipleComponent]
public class SunLampPlacer : MonoBehaviour
{
    public const float DefaultRadius = 10f;

    public static SunLampPlacer Instance { get; private set; }

    [Header("Referensi")]
    public Transform visualParent;

    [Header("Penerangan")]
    [Tooltip("Jarak maksimum dari lampu supaya sebuah sel boleh ditanami.")]
    public float radius = DefaultRadius;

    [Tooltip("Skala model lampu di dunia.")]
    public float modelScale = 0.9f;

    [Tooltip("Kecepatan denyut cahaya.")]
    public float pulseSpeed = 2.2f;

    public Material glowMaterial;

    readonly List<Vector3> lamps = new List<Vector3>();
    readonly List<GameObject> visuals = new List<GameObject>();
    readonly List<GameObject> glows = new List<GameObject>();
    readonly List<GameObject> bulbs = new List<GameObject>();
    readonly List<GameObject> halos = new List<GameObject>();
    readonly List<Light> lights = new List<Light>();

    Material bulbMaterial;
    Material haloMaterial;

    public IReadOnlyList<Vector3> Lamps => lamps;
    public int LampCount => lamps.Count;

    void Awake()
    {
        Instance = this;
        if (visualParent == null) visualParent = transform;
        if (glowMaterial == null) glowMaterial = CreateGlowMaterial();
        if (bulbMaterial == null) bulbMaterial = CreateBulbMaterial(false);
        if (haloMaterial == null) haloMaterial = CreateBulbMaterial(true);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (glowMaterial != null)
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
            Color color = glowMaterial.HasProperty("_BaseColor")
                ? glowMaterial.GetColor("_BaseColor")
                : new Color(0.72f, 1f, 0.45f, 1f);
            color.a = 0.20f + 0.20f * wave;

            if (glowMaterial.HasProperty("_BaseColor")) glowMaterial.SetColor("_BaseColor", color);
            glowMaterial.color = color;
        }

        float lightWave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed + 0.8f);
        for (int i = 0; i < lights.Count; i++)
            if (lights[i] != null) lights[i].intensity = 3.8f + 1.6f * lightWave;

        // Inti bola lampu + halo ikut berdenyut. Shader tanpa kabut supaya
        // pancaran lampu tetap terlihat jelas di tengah polusi.
        if (bulbMaterial != null)
        {
            Color core = Color.Lerp(new Color(1.5f, 1.0f, 0.42f),
                new Color(3.0f, 2.4f, 1.2f), lightWave);
            if (bulbMaterial.HasProperty("_BaseColor")) bulbMaterial.SetColor("_BaseColor", core);
            bulbMaterial.color = core;
        }

        if (haloMaterial != null)
        {
            Color halo = new Color(1f, 0.86f, 0.48f, Mathf.Lerp(0.18f, 0.40f, lightWave));
            if (haloMaterial.HasProperty("_BaseColor")) haloMaterial.SetColor("_BaseColor", halo);
            haloMaterial.color = halo;
        }
    }

    /// <summary>Sel berada dalam jangkauan lampu mana pun?</summary>
    public bool IsIlluminated(Vector3 worldPosition)
    {
        float maxSqr = Mathf.Max(0.01f, radius) * Mathf.Max(0.01f, radius);

        for (int i = 0; i < lamps.Count; i++)
        {
            Vector3 delta = lamps[i] - worldPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= maxSqr) return true;
        }

        return false;
    }

    public bool HasLampAt(Vector3 snappedCell)
    {
        return FindLamp(snappedCell) >= 0;
    }

    /// <summary>
    /// Pasang lampu di sel atau ambil lagi kalau sudah ada lampu di sel itu.
    /// </summary>
    public bool ToggleAt(Vector3 snappedCell, out string message)
    {
        int index = FindLamp(snappedCell);
        if (index >= 0)
        {
            if (visuals.Count > index && visuals[index] != null) Destroy(visuals[index]);
            if (glows.Count > index && glows[index] != null) Destroy(glows[index]);
            if (bulbs.Count > index && bulbs[index] != null) Destroy(bulbs[index]);
            if (halos.Count > index && halos[index] != null) Destroy(halos[index]);

            if (visuals.Count > index) visuals.RemoveAt(index);
            if (glows.Count > index) glows.RemoveAt(index);
            if (bulbs.Count > index) bulbs.RemoveAt(index);
            if (halos.Count > index) halos.RemoveAt(index);
            if (index < lights.Count) lights.RemoveAt(index);
            lamps.RemoveAt(index);

            if (InventorySystem.Instance != null)
                InventorySystem.Instance.AddItem(GameItems.SunLamp, 1);

            message = "Lampu Matahari diambil lagi (kembali ke inventory).";
            return true;
        }

        InventorySystem inventory = InventorySystem.Instance;
        if (inventory == null || inventory.CountItem(GameItems.SunLamp) <= 0)
        {
            message = "Stok Lampu Matahari habis.";
            return false;
        }

        if (!inventory.RemoveItem(GameItems.SunLamp, 1))
        {
            message = "Gagal mengambil Lampu Matahari dari inventory.";
            return false;
        }

        GameObject lamp = ModelLibrary.Spawn(ModelLibrary.LampModel, visualParent,
            snappedCell, 0f, modelScale);
        if (lamp == null)
        {
            inventory.AddItem(GameItems.SunLamp, 1);
            message = "Model lampu tidak ditemukan.";
            return false;
        }

        lamp.name = "SunLamp_" + (lamps.Count + 1);

        // Model aslinya menghadap -Z. Dimiringkan 90° supaya memancar ke atas.
        lamp.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        Light light = lamp.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.93f, 0.66f);
        light.intensity = 4.2f;
        light.range = Mathf.Max(10f, radius * 1.7f);
        light.shadows = LightShadows.None;

        // Buat model lampu ikut tampak menyala (emisi hangat).
        Renderer[] lampRenderers = lamp.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < lampRenderers.Length; i++)
        {
            if (lampRenderers[i] == null) continue;
            Material material = lampRenderers[i].material;
            if (material == null) continue;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.55f, 0.46f, 0.22f));
            }
        }

        Collider[] colliders = lamp.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;

        GameObject glow = CreateGlowRing(snappedCell);
        if (glowMaterial == null) glowMaterial = CreateGlowMaterial();

        // Bola cahaya yang benar-benar terlihat, mengapung di atas model lampu.
        Vector3 bulbPosition = snappedCell + Vector3.up * 1.15f;
        GameObject bulb = CreateGlowSphere("LampGlowBulb", bulbPosition, 0.36f, bulbMaterial);
        GameObject halo = CreateGlowSphere("LampGlowHalo", bulbPosition, 1.15f, haloMaterial);

        lamps.Add(snappedCell);
        visuals.Add(lamp);
        glows.Add(glow);
        bulbs.Add(bulb);
        halos.Add(halo);
        lights.Add(light);

        message = "Lampu Matahari dipasang. Cahayanya menerangi area di sekitarnya.";
        return true;
    }

    GameObject CreateGlowSphere(string name, Vector3 worldPosition, float diameter, Material material)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(visualParent, false);
        sphere.transform.position = worldPosition;
        sphere.transform.localScale = Vector3.one * diameter;

        Collider collider = sphere.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }

        MeshRenderer renderer = sphere.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        return sphere;
    }

    GameObject CreateGlowRing(Vector3 snappedCell)
    {
        GameObject holder = new GameObject("LampGlow");
        holder.transform.SetParent(visualParent, false);
        holder.transform.position = snappedCell;

        MeshFilter filter = holder.AddComponent<MeshFilter>();
        MeshRenderer renderer = holder.AddComponent<MeshRenderer>();
        filter.sharedMesh = BuildAnnulus(snappedCell, radius * 0.84f, radius, 56);
        renderer.sharedMaterial = glowMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return holder;
    }

    static Mesh BuildAnnulus(Vector3 center, float inner, float outer, int segments)
    {
        Vector3[] vertices = new Vector3[segments * 2];
        int[] triangles = new int[segments * 6];

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            float innerY = GridOverlay.SampleGroundHeight(
                new Vector3(center.x + dir.x * inner, center.y, center.z + dir.z * inner),
                center.y) - center.y + 0.12f;
            float outerY = GridOverlay.SampleGroundHeight(
                new Vector3(center.x + dir.x * outer, center.y, center.z + dir.z * outer),
                center.y) - center.y + 0.12f;

            vertices[i * 2] = new Vector3(dir.x * inner, innerY, dir.z * inner);
            vertices[i * 2 + 1] = new Vector3(dir.x * outer, outerY, dir.z * outer);
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            int t = i * 6;
            triangles[t] = i * 2;
            triangles[t + 1] = i * 2 + 1;
            triangles[t + 2] = next * 2 + 1;
            triangles[t + 3] = i * 2;
            triangles[t + 4] = next * 2 + 1;
            triangles[t + 5] = next * 2;
        }

        Mesh mesh = new Mesh { name = "LampGlowRing" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    static Material CreateGlowMaterial()
    {
        Shader shader = Shader.Find("GreenGatekeeper/UnlitNoFogTransparent");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material material = shader != null ? new Material(shader) : null;
        if (material == null) return null;

        Color color = new Color(1f, 0.92f, 0.55f, 0.28f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.color = color;

        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = 3100;
        return material;
    }

    static Material CreateBulbMaterial(bool halo)
    {
        Shader shader = Shader.Find(halo
            ? "GreenGatekeeper/UnlitNoFogTransparent"
            : "GreenGatekeeper/UnlitNoFog");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) return null;

        Material material = new Material(shader);
        Color color = halo
            ? new Color(1f, 0.86f, 0.48f, 0.28f)
            : new Color(2.2f, 1.6f, 0.7f, 1f);

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.color = color;
        if (halo) material.renderQueue = 3100;
        return material;
    }

    int FindLamp(Vector3 snappedCell)
    {
        for (int i = 0; i < lamps.Count; i++)
        {
            Vector3 delta = lamps[i] - snappedCell;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.01f) return i;
        }

        return -1;
    }
}
