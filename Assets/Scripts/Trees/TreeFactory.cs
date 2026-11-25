using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// [General] Membuat pohon runtime dari <see cref="TreeVariant"/>.
/// Dipakai forest generator dan edit mode isometric.
/// </summary>
public static class TreeFactory
{
    static readonly Dictionary<Color, Material> barMaterialCache = new Dictionary<Color, Material>();
    static readonly Dictionary<Color, Material> materialCache = new Dictionary<Color, Material>();
    static readonly Dictionary<string, Mesh> coneCache = new Dictionary<string, Mesh>();

    public static TreeDurability Create(TreeVariant variant, Vector3 position, Transform parent,
        bool alwaysShowHealthBar, float scale = 1f, float yaw = 0f)
    {
        if (variant == null) return null;

        // Dibuat nonaktif dulu supaya Awake TreeDurability jalan setelah semua field diisi.
        GameObject root = new GameObject(variant.id + "_Tree");
        root.SetActive(false);
        root.transform.SetParent(parent, false);
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        string family = variant.ResolveModelFamily();
        GameObject[] stagePrefabs = ModelLibrary.GetTreeStages(family);

        List<Renderer> foliage = new List<Renderer>();
        TreeModelStages stages = null;
        Renderer[] foliageRenderers = null;
        float barHeight = variant.barHeight;
        Color healthyColor = variant.foliageColor;

        if (stagePrefabs != null)
        {
            // Model 3D low-poly: 3 ukuran untuk tahap tumbuh.
            stages = TreeModelStages.Build(root.transform, stagePrefabs, scale);
            root.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);

            float modelHeight = ModelLibrary.GetHeight(ModelLibrary.TreeKey(family, 3));
            barHeight = modelHeight + 0.9f;

            // Simpan renderer daun SEKARANG, sebelum health bar dibuat.
            // AllRenderers() membaca semua renderer di root, jadi kalau dipanggil
            // setelah bar dibuat, BG & Fill ikut terwarnai putih/daun.
            foliageRenderers = stages.AllRenderers();
            if (foliageRenderers.Length > 0 && foliageRenderers[0].sharedMaterial != null &&
                foliageRenderers[0].sharedMaterial.HasProperty(BaseColorId))
            {
                healthyColor = foliageRenderers[0].sharedMaterial.GetColor(BaseColorId);
            }

            AddTreeCollider(root, modelHeight, Mathf.Max(0.35f, modelHeight * 0.18f));
        }
        else
        {
            // Fallback prosedural kalau prefab model belum dibuat.
            root.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);

            Material trunkMaterial = GetMaterial(variant.trunkColor);
            Material foliageMaterial = GetMaterial(variant.foliageColor);

            BuildTrunk(root.transform, variant, trunkMaterial);
            foliage = BuildFoliage(root.transform, variant, foliageMaterial);

            AddTreeCollider(root, variant.trunkHeight + 1.2f, Mathf.Max(0.4f, variant.trunkRadius));
        }

        GameObject barRoot = BuildHealthBar(root.transform, barHeight, out Transform fill);

        // Jaga-jaga: buang renderer health bar dari daftar daun.
        if (foliageRenderers != null && barRoot != null)
        {
            List<Renderer> filtered = new List<Renderer>(foliageRenderers.Length);
            for (int i = 0; i < foliageRenderers.Length; i++)
            {
                Renderer renderer = foliageRenderers[i];
                if (renderer == null) continue;
                if (renderer.transform.IsChildOf(barRoot.transform)) continue;
                filtered.Add(renderer);
            }
            foliageRenderers = filtered.ToArray();
        }

        TreeDurability durability = root.AddComponent<TreeDurability>();
        durability.maxHealth = variant.maxHealth;
        durability.healthBarRoot = barRoot;
        durability.healthBarFill = fill;
        durability.foliageRenderers = stages != null ? foliageRenderers : foliage.ToArray();
        durability.healthyColor = healthyColor;
        durability.damagedColor = new Color(0.55f, 0.42f, 0.12f);
        durability.lootItemId = "";
        durability.commodityItemId = variant.commodityItemId;
        durability.commodityYield = 1;
        durability.woodYield = Mathf.Max(1, variant.woodYield);
        durability.seedItemId = variant.seedItemId;
        durability.alwaysShowHealthBar = alwaysShowHealthBar;

        root.SetActive(true);
        return durability;
    }

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    /// <summary>Collider tab/klik &amp; damage. Ditambahkan di root supaya mudah dimatikan saat pohon tumbang.</summary>
    static void AddTreeCollider(GameObject root, float height, float radius)
    {
        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, height * 0.5f, 0f);
        collider.height = Mathf.Max(1f, height);
        collider.radius = Mathf.Max(0.25f, radius);
        collider.direction = 1;
    }

    static void BuildTrunk(Transform parent, TreeVariant variant, Material material)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(parent, false);
        trunk.transform.localPosition = new Vector3(0f, variant.trunkHeight * 0.5f, 0f);
        trunk.transform.localScale = new Vector3(
            variant.trunkRadius, variant.trunkHeight * 0.5f, variant.trunkRadius);

        SetMaterial(trunk, material);
    }

    static List<Renderer> BuildFoliage(Transform parent, TreeVariant variant, Material material)
    {
        List<Renderer> foliage = new List<Renderer>();
        float h = variant.trunkHeight;

        switch (variant.style)
        {
            case TreeFoliageStyle.Pine:
                foliage.Add(AddCone(parent, "Foliage_Low", new Vector3(0f, h * 0.45f, 0f), 1.70f, 2.60f, material));
                foliage.Add(AddCone(parent, "Foliage_Mid", new Vector3(0f, h * 0.45f + 1.40f, 0f), 1.30f, 2.20f, material));
                foliage.Add(AddCone(parent, "Foliage_Top", new Vector3(0f, h * 0.45f + 2.70f, 0f), 0.85f, 1.80f, material));
                break;

            case TreeFoliageStyle.Palm:
                // Cadangan prosedural: tajuk palem dari beberapa daun pipih.
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * Mathf.PI * 2f / 6f;
                    Vector3 offset = new Vector3(Mathf.Cos(angle) * 0.85f, h + 0.15f, Mathf.Sin(angle) * 0.85f);
                    foliage.Add(AddSphere(parent, "Frond_" + i, offset, 1.10f, material));
                }
                break;

            default:
                foliage.Add(AddSphere(parent, "Foliage_Low", new Vector3(0f, h + 0.30f, 0f), 2.60f, material));
                foliage.Add(AddSphere(parent, "Foliage_Mid", new Vector3(0.65f, h + 1.20f, 0.15f), 1.80f, material));
                foliage.Add(AddSphere(parent, "Foliage_Top", new Vector3(-0.50f, h + 1.15f, -0.25f), 1.50f, material));
                break;
        }

        return foliage;
    }

    static GameObject BuildHealthBar(Transform parent, float height, out Transform fill)
    {
        GameObject barRoot = new GameObject("HealthBar");
        barRoot.transform.SetParent(parent, false);
        barRoot.transform.localPosition = new Vector3(0f, height, 0f);
        barRoot.AddComponent<HealthBarBillboard>();

        // Health bar memakai shader unlit TANPA fog supaya tetap vibrant dan
        // terbaca lewat kabut polusi. Warna HDR (>1) membuatnya lebih menonjol.
        Material borderMaterial = GetBarMaterial(new Color(0.62f, 0.88f, 1f, 1f));
        Material bgMaterial = GetBarMaterial(new Color(0.015f, 0.02f, 0.025f, 1f));
        Material fillMaterial = GetBarMaterial(new Color(0.20f, 1.85f, 0.28f, 1f));

        CreateCube("Border", barRoot.transform, borderMaterial,
            new Vector3(2.30f, 0.36f, 0.01f), new Vector3(0f, 0f, 0.01f));
        CreateCube("BG", barRoot.transform, bgMaterial,
            new Vector3(2.18f, 0.28f, 0.02f), Vector3.zero);
        GameObject fillObject = CreateCube("Fill", barRoot.transform, fillMaterial,
            new Vector3(2.06f, 0.20f, 0.03f), new Vector3(0f, 0f, -0.02f));

        fill = fillObject.transform;
        barRoot.SetActive(false);
        return barRoot;
    }

    static GameObject CreateCube(string name, Transform parent, Material material, Vector3 scale, Vector3 position)    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;

        Collider collider = cube.GetComponent<Collider>();
        if (collider != null) DestroyNow(collider);

        SetMaterial(cube, material);

        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        return cube;
    }

    static Renderer AddSphere(Transform parent, string name, Vector3 position, float diameter, Material material)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = position;
        sphere.transform.localScale = Vector3.one * diameter;

        Collider collider = sphere.GetComponent<Collider>();
        if (collider != null) DestroyNow(collider);

        SetMaterial(sphere, material);
        return sphere.GetComponent<Renderer>();
    }

    static Renderer AddCone(Transform parent, string name, Vector3 position, float radius, float height, Material material)
    {
        GameObject cone = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        cone.transform.SetParent(parent, false);
        cone.transform.localPosition = position;

        cone.GetComponent<MeshFilter>().sharedMesh = GetConeMesh(radius, height);
        SetMaterial(cone, material);
        return cone.GetComponent<Renderer>();
    }

    static Mesh GetConeMesh(float radius, float height)
    {
        string key = radius.ToString("F2") + "_" + height.ToString("F2");
        if (coneCache.TryGetValue(key, out Mesh cached)) return cached;

        const int segments = 18;
        Vector3[] vertices = new Vector3[segments + 2];
        vertices[0] = new Vector3(0f, height, 0f);

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        vertices[segments + 1] = Vector3.zero;

        int[] triangles = new int[segments * 6];
        int t = 0;

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            triangles[t++] = 0;
            triangles[t++] = i + 1;
            triangles[t++] = next + 1;
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            triangles[t++] = segments + 1;
            triangles[t++] = next + 1;
            triangles[t++] = i + 1;
        }

        Mesh mesh = new Mesh { name = "Cone_" + key };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        coneCache[key] = mesh;
        return mesh;
    }

    static void SetMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    /// <summary>Destroy yang aman dipakai saat edit mode (dipanggil tool editor).</summary>
    static void DestroyNow(Object target)
    {
        if (target == null) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Object.DestroyImmediate(target);
            return;
        }
#endif
        Object.Destroy(target);
    }

    /// <summary>Health bar: unlit, tanpa kabut, warna bisa HDR.</summary>
    static Material GetBarMaterial(Color color)
    {
        if (barMaterialCache.TryGetValue(color, out Material cached)) return cached;

        Shader shader = Shader.Find("GreenGatekeeper/UnlitNoFog");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.color = color;

        barMaterialCache[color] = material;
        return material;
    }

    static Material GetMaterial(Color color)
    {
        if (materialCache.TryGetValue(color, out Material cached)) return cached;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.color = color;

        materialCache[color] = material;
        return material;
    }
}
