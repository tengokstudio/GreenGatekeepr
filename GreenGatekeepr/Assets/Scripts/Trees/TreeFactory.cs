using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// [General] Membuat pohon runtime dari <see cref="TreeVariant"/>.
/// Dipakai forest generator dan edit mode isometric.
/// </summary>
public static class TreeFactory
{
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
        root.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);

        Material trunkMaterial = GetMaterial(variant.trunkColor);
        Material foliageMaterial = GetMaterial(variant.foliageColor);

        BuildTrunk(root.transform, variant, trunkMaterial);
        List<Renderer> foliage = BuildFoliage(root.transform, variant, foliageMaterial);

        GameObject barRoot = BuildHealthBar(root.transform, variant.barHeight, out Transform fill);

        TreeDurability durability = root.AddComponent<TreeDurability>();
        durability.maxHealth = variant.maxHealth;
        durability.healthBarRoot = barRoot;
        durability.healthBarFill = fill;
        durability.foliageRenderers = foliage.ToArray();
        durability.healthyColor = variant.foliageColor;
        durability.damagedColor = new Color(0.55f, 0.42f, 0.12f);
        durability.lootItemId = variant.id;
        durability.alwaysShowHealthBar = alwaysShowHealthBar;

        root.SetActive(true);
        return durability;
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

            case TreeFoliageStyle.Birch:
                foliage.Add(AddSphere(parent, "Foliage_Low", new Vector3(0f, h + 0.20f, 0f), 1.80f, material));
                foliage.Add(AddSphere(parent, "Foliage_Mid", new Vector3(0.40f, h + 0.85f, 0.10f), 1.40f, material));
                foliage.Add(AddSphere(parent, "Foliage_Top", new Vector3(-0.30f, h + 0.90f, -0.20f), 1.20f, material));
                break;

            case TreeFoliageStyle.Blossom:
                foliage.Add(AddSphere(parent, "Foliage_Low", new Vector3(0f, h + 0.30f, 0f), 2.30f, material));
                foliage.Add(AddSphere(parent, "Foliage_Mid", new Vector3(0.50f, h + 1.00f, 0.20f), 1.70f, material));
                foliage.Add(AddSphere(parent, "Foliage_Top", new Vector3(-0.40f, h + 0.90f, -0.20f), 1.40f, material));
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

        Material bgMaterial = GetMaterial(new Color(0.07f, 0.07f, 0.08f));
        Material fillMaterial = GetMaterial(new Color(0.30f, 0.90f, 0.32f));

        GameObject bg = CreateCube("BG", barRoot.transform, bgMaterial,
            new Vector3(1.80f, 0.26f, 0.02f), Vector3.zero);
        GameObject fillObject = CreateCube("Fill", barRoot.transform, fillMaterial,
            new Vector3(1.70f, 0.18f, 0.03f), new Vector3(0f, 0f, -0.02f));

        fill = fillObject.transform;
        barRoot.SetActive(false);
        return barRoot;
    }

    static GameObject CreateCube(string name, Transform parent, Material material, Vector3 scale, Vector3 position)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;

        Collider collider = cube.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);

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
        if (collider != null) Object.Destroy(collider);

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
