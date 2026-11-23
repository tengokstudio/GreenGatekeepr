using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tool sekali pakai untuk menambahkan mekanik pembasmian hama ke scene aktif:
///  - 4 pohon dummy + durability + health bar
///  - 2 sarang hama (kiri & kanan pohon)
///  - prefab hama dummy
///  - HUD (pohon / hama / kill / waktu)
/// Jalankan: Tools > Pest Defense > Setup (Active Scene)
/// </summary>
public static class PestDefenseSetup
{
    const string RootName = "PestDefense";
    const string HudName = "PestHUD";
    const string PrefabPath = "Assets/Prefabs/Pest.prefab";
    const string MaterialFolder = "Assets/Materials/PestDefense";

    const float TreeZ = 7.5f;
    const float NestX = 12f;
    const float TreeHealth = 200f;
    const float PestHealth = 30f;

    static readonly float[] TreeX = { -7.5f, -2.5f, 2.5f, 7.5f };

    [MenuItem("Tools/Pest Defense/Setup (Active Scene)", priority = 20)]
    public static void SetupFromMenu()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            EditorUtility.DisplayDialog("Pest Defense Setup",
                "Simpan scene dulu, lalu jalankan setup lagi.", "OK");
            return;
        }

        Setup(scene);
    }

    [MenuItem("Tools/Pest Defense/Remove (Active Scene)", priority = 21)]
    public static void RemoveFromMenu()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        Remove();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[PestDefenseSetup] Mekanik hama dihapus dari scene.");
    }

    static void Setup(Scene scene)
    {
        Remove();

        EnsureFolder(MaterialFolder);

        Material nestMat = EnsureMaterial("PestNest", new Color(0.30f, 0.20f, 0.12f));
        Material pestBodyMat = EnsureMaterial("PestBody", new Color(0.58f, 0.24f, 0.72f));
        Material pestHeadMat = EnsureMaterial("PestHead", new Color(0.38f, 0.14f, 0.50f));
        Material pestEyeMat = EnsureMaterial("PestEye", new Color(0.05f, 0.03f, 0.06f));

        // ---------------- Root + manager ----------------
        GameObject root = new GameObject(RootName);
        PestDefenseGame game = root.AddComponent<PestDefenseGame>();

        GameObject treesParent = new GameObject("Trees");
        treesParent.transform.SetParent(root.transform, false);

        GameObject spawnersParent = new GameObject("Spawners");
        spawnersParent.transform.SetParent(root.transform, false);

        GameObject pestsParent = new GameObject("Pests");
        pestsParent.transform.SetParent(root.transform, false);

        GameObject pestPrefab = EnsurePestPrefab(pestBodyMat, pestHeadMat, pestEyeMat);

        // ---------------- 4 pohon (4 variasi) ----------------
        TreeVariant[] variants = TreeVariant.CreateDefaults();

        List<TreeDurability> trees = new List<TreeDurability>();
        for (int i = 0; i < TreeX.Length; i++)
        {
            TreeVariant variant = variants[i % variants.Length];
            Material variantTrunk = EnsureMaterial("TreeTrunk_" + variant.id, variant.trunkColor);
            Material variantFoliage = EnsureMaterial("TreeFoliage_" + variant.id, variant.foliageColor);

            Vector3 position = new Vector3(TreeX[i], 0f, TreeZ);
            trees.Add(CreateTree("Tree_" + (i + 1), position, treesParent.transform,
                variantTrunk, variantFoliage, variant));
        }

        // ---------------- 2 sarang ----------------
        List<PestSpawner> spawners = new List<PestSpawner>();
        spawners.Add(CreateSpawner("Nest_Left", new Vector3(-NestX, 0f, TreeZ), spawnersParent.transform,
            nestMat, pestPrefab, pestsParent.transform));

        spawners.Add(CreateSpawner("Nest_Right", new Vector3(NestX, 0f, TreeZ), spawnersParent.transform,
            nestMat, pestPrefab, pestsParent.transform));

        // ---------------- Wiring ----------------
        game.trees = trees.ToArray();
        game.spawners = spawners.ToArray();
        game.pestParent = pestsParent.transform;
        game.pestPrefab = pestPrefab;
        game.roundDuration = 60f;

        Text hudText = CreateHud(out GameObject hudRoot);
        game.hudText = hudText;

        foreach (PestSpawner spawner in spawners)
        {
            spawner.pestPrefab = pestPrefab;
            spawner.pestParent = pestsParent.transform;
            spawner.game = game;
            spawner.firstSpawnDelay = 1f;
            spawner.spawnInterval = 5f;
            spawner.spawnDuration = 60f;
            spawner.spawnRadius = 1.6f;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[PestDefenseSetup] Selesai: 4 pohon (HP " + TreeHealth + "), 2 sarang hama, prefab " + PrefabPath +
                  ", HUD " + hudRoot.name + ". Hama spawn tiap 5 detik selama 60 detik. Scene disimpan.");
    }

    static TreeDurability CreateTree(string name, Vector3 position, Transform parent,
        Material trunkMat, Material foliageMat, TreeVariant variant)
    {
        GameObject tree = new GameObject(name);
        tree.transform.SetParent(parent, false);
        tree.transform.position = position;

        TreeDurability durability = tree.AddComponent<TreeDurability>();
        durability.maxHealth = variant != null ? variant.maxHealth : TreeHealth;
        durability.lootItemId = variant != null ? variant.id : "tree_oak";
        durability.giveLootToInventory = true;
        durability.alwaysShowHealthBar = true;

        GameObject trunk = CreatePrimitive(PrimitiveType.Cylinder, "Trunk", tree.transform, trunkMat, true);
        trunk.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        trunk.transform.localScale = new Vector3(0.55f, 1.6f, 0.55f);

        List<Renderer> foliage = new List<Renderer>();
        foliage.Add(CreateFoliage("Foliage_Low", new Vector3(0f, 3.5f, 0f), 2.6f, tree.transform, foliageMat));
        foliage.Add(CreateFoliage("Foliage_Mid", new Vector3(0.65f, 4.4f, 0.15f), 1.8f, tree.transform, foliageMat));
        foliage.Add(CreateFoliage("Foliage_Top", new Vector3(-0.5f, 4.35f, -0.25f), 1.5f, tree.transform, foliageMat));

        GameObject barRoot = new GameObject("HealthBar");
        barRoot.transform.SetParent(tree.transform, false);
        barRoot.transform.localPosition = new Vector3(0f, 5.3f, 0f);
        barRoot.AddComponent<HealthBarBillboard>();

        Material bgMat = EnsureMaterial("HealthBarBG", new Color(0.07f, 0.07f, 0.08f));
        Material fillMat = EnsureMaterial("HealthBarFill", new Color(0.30f, 0.90f, 0.32f), true);

        GameObject bg = CreatePrimitive(PrimitiveType.Cube, "BG", barRoot.transform, bgMat, false);
        bg.transform.localScale = new Vector3(1.8f, 0.26f, 0.02f);
        bg.transform.localPosition = Vector3.zero;
        DisableShadows(bg);

        GameObject fill = CreatePrimitive(PrimitiveType.Cube, "Fill", barRoot.transform, fillMat, false);
        fill.transform.localScale = new Vector3(1.7f, 0.18f, 0.03f);
        fill.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        DisableShadows(fill);

        durability.healthBarRoot = barRoot;
        durability.healthBarFill = fill.transform;
        durability.foliageRenderers = foliage.ToArray();
        durability.healthyColor = variant != null ? variant.foliageColor : new Color(0.31f, 0.66f, 0.24f);
        durability.damagedColor = new Color(0.55f, 0.42f, 0.12f);

        return durability;
    }

    static Renderer CreateFoliage(string name, Vector3 localPosition, float diameter, Transform parent, Material material)
    {
        GameObject foliage = CreatePrimitive(PrimitiveType.Sphere, name, parent, material, false);
        foliage.transform.localPosition = localPosition;
        foliage.transform.localScale = Vector3.one * diameter;
        return foliage.GetComponent<Renderer>();
    }

    static PestSpawner CreateSpawner(string name, Vector3 position, Transform parent, Material nestMat,
        GameObject pestPrefab, Transform pestParent)
    {
        GameObject nest = new GameObject(name);
        nest.transform.SetParent(parent, false);
        nest.transform.position = position;

        GameObject mound = CreatePrimitive(PrimitiveType.Sphere, "Mound", nest.transform, nestMat, false);
        mound.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        mound.transform.localScale = new Vector3(2.6f, 1.0f, 2.6f);

        GameObject baseRing = CreatePrimitive(PrimitiveType.Cylinder, "Base", nest.transform, nestMat, true);
        baseRing.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        baseRing.transform.localScale = new Vector3(3.4f, 0.1f, 3.4f);

        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f * Mathf.Deg2Rad;
            GameObject spike = CreatePrimitive(PrimitiveType.Cube, "Spike_" + (i + 1), nest.transform, nestMat, false);
            spike.transform.localPosition = new Vector3(Mathf.Cos(angle) * 1.1f, 0.65f, Mathf.Sin(angle) * 1.1f);
            spike.transform.localRotation = Quaternion.Euler(0f, -i * 120f, 12f);
            spike.transform.localScale = new Vector3(0.18f, 0.9f, 0.18f);
        }

        PestSpawner spawner = nest.AddComponent<PestSpawner>();
        spawner.pestPrefab = pestPrefab;
        spawner.pestParent = pestParent;
        return spawner;
    }

    static GameObject EnsurePestPrefab(Material bodyMat, Material headMat, Material eyeMat)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            AssetDatabase.DeleteAsset(PrefabPath);

        GameObject pest = new GameObject("Pest");

        PestEnemy enemy = pest.AddComponent<PestEnemy>();
        enemy.maxHealth = PestHealth;
        enemy.moveSpeed = 3.5f;
        enemy.attackRange = 1.8f;
        enemy.attackInterval = 0.9f;
        enemy.damagePerBite = 3f;
        enemy.despawnAfterTreesGone = 3f;
        enemy.lootItemId = "pest_corpse";
        enemy.corpseLifetime = 20f;
        enemy.pickupRange = 6f;

        CapsuleCollider capsule = pest.AddComponent<CapsuleCollider>();
        capsule.radius = 0.3f;
        capsule.height = 1.0f;
        capsule.center = new Vector3(0f, 0.5f, 0f);

        Rigidbody body = pest.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        GameObject torso = CreatePrimitive(PrimitiveType.Sphere, "Body", pest.transform, bodyMat, false);
        torso.transform.localPosition = new Vector3(0f, 0.42f, 0f);
        torso.transform.localScale = new Vector3(0.78f, 0.58f, 0.95f);

        GameObject head = CreatePrimitive(PrimitiveType.Sphere, "Head", pest.transform, headMat, false);
        head.transform.localPosition = new Vector3(0f, 0.68f, 0.34f);
        head.transform.localScale = Vector3.one * 0.46f;

        for (int i = 0; i < 2; i++)
        {
            GameObject eye = CreatePrimitive(PrimitiveType.Sphere, "Eye_" + (i + 1), head.transform, eyeMat, false);
            eye.transform.localPosition = new Vector3(i == 0 ? -0.17f : 0.17f, 0.05f, 0.42f);
            eye.transform.localScale = Vector3.one * 0.24f;
        }

        for (int i = 0; i < 4; i++)
        {
            int row = i < 2 ? 1 : -1;
            int col = i % 2 == 0 ? -1 : 1;
            GameObject leg = CreatePrimitive(PrimitiveType.Cube, "Leg_" + (i + 1), pest.transform, headMat, false);
            leg.transform.localPosition = new Vector3(col * 0.3f, 0.14f, row * 0.22f);
            leg.transform.localRotation = Quaternion.Euler(0f, col * 20f, 0f);
            leg.transform.localScale = new Vector3(0.1f, 0.26f, 0.32f);
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(pest, PrefabPath);
        Object.DestroyImmediate(pest);
        AssetDatabase.SaveAssets();
        return prefab;
    }

    static Text CreateHud(out GameObject hudRoot)
    {
        hudRoot = new GameObject(HudName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        hudRoot.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = hudRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;

        CanvasScaler scaler = hudRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform safeArea = CreateRect("SafeArea", hudRoot.transform);
        safeArea.anchorMin = Vector2.zero;
        safeArea.anchorMax = Vector2.one;
        safeArea.offsetMin = Vector2.zero;
        safeArea.offsetMax = Vector2.zero;
        safeArea.gameObject.AddComponent<SafeAreaFitter>();

        RectTransform bar = CreateRect("TopBar", safeArea);
        bar.anchorMin = new Vector2(0f, 1f);
        bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.offsetMin = new Vector2(12f, -72f);
        bar.offsetMax = new Vector2(-12f, -8f);

        Image background = bar.gameObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.5f);
        background.raycastTarget = false;

        RectTransform textRect = CreateRect("HudText", bar);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(22f, 0f);
        textRect.offsetMax = new Vector2(-22f, 0f);

        Text text = textRect.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 34;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = "POHON 4/4   |   HAMA 0   |   KILL 0   |   00:00 / 01:00";

        return text;
    }

    static void Remove()
    {
        GameObject oldRoot = GameObject.Find(RootName);
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);

        GameObject oldHud = GameObject.Find(HudName);
        if (oldHud != null) Object.DestroyImmediate(oldHud);
    }

    static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Material material, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);

        if (!keepCollider)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
        }

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;

        return go;
    }

    static void DisableShadows(GameObject go)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;

        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    static Material EnsureMaterial(string name, Color color, bool emission = false)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.color = color;

        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.6f);
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent)) parent = parent.Replace('\\', '/');

        string leaf = System.IO.Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }
}
