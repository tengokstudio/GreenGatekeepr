using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [General/Isometric/FPS] Memasang mekanik baru ke scene aktif:
///   - langit gelap + kabut + cahaya matahari redup,
///   - grid statis bergaris tipis,
///   - Lampu Matahari placeable + syarat tanam,
///   - laut + teleport saat player jatuh,
///   - semprotan sebagai viewmodel senjata FPS dengan tembakan tahan.
///
/// Jalankan: Tools &gt; Gameplay &gt; Setup New Mechanics (Active Scene)
/// </summary>
public static class GameplayV2Setup
{
    const string MaterialFolder = "Assets/Materials/Atmosphere";
    const string SkyMaterialPath = MaterialFolder + "/Mat_CloudySky.mat";

    [MenuItem("Tools/Gameplay/Setup New Mechanics (Active Scene)", priority = 70)]
    public static void SetupFromMenu()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            EditorUtility.DisplayDialog("Gameplay Setup", "Simpan scene dulu, lalu jalankan setup lagi.", "OK");
            return;
        }

        SetupAtmosphere();
        SetupSeaAndRespawn();
        SetupWaterAndPond();
        SetupGrid();
        SetupSunLamp();
        SetupSprayWeapon();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[GameplayV2Setup] Polusi+kabut, awan tebal FPS, laut+respawn, grid statis, " +
                  "lampu placeable, dan senjata semprotan terpasang. Scene disimpan.");
    }

    // ------------------------------------------------------------------
    // Langit gelap + kabut
    // ------------------------------------------------------------------
    static void SetupAtmosphere()
    {
        // Kabut = polusi udara parah: abu-abu kekuningan, lebih gelap supaya
        // cahaya lampu terlihat jelas.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.028f;
        RenderSettings.fogColor = new Color(0.38f, 0.37f, 0.32f, 1f);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.41f, 0.37f, 1f);
        RenderSettings.ambientIntensity = 0.62f;
        RenderSettings.reflectionIntensity = 0.45f;

        Material sky = EnsureSkyMaterial();
        if (sky != null) RenderSettings.skybox = sky;

        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null || lights[i].type != LightType.Directional) continue;
            lights[i].intensity = 0.55f;
            lights[i].color = new Color(0.93f, 0.91f, 0.84f, 1f);
        }

        BuildCloudCeilingSetup();
        SetupPollutionFog();
    }

    /// <summary>Kabut berkurang otomatis saat jumlah pohon tanam bertambah.</summary>
    static void SetupPollutionFog()
    {
        PollutionFogController controller =
            Object.FindFirstObjectByType<PollutionFogController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            GameObject root = new GameObject("PollutionFogController");
            controller = root.AddComponent<PollutionFogController>();
        }

        controller.targetTrees = 20;
        controller.pollutedDensity = 0.028f;
        controller.pollutedColor = new Color(0.38f, 0.37f, 0.32f, 1f);
        controller.pollutedAmbient = 0.62f;
        controller.pollutedSunIntensity = 0.55f;
        controller.clearDensity = 0.004f;
        controller.clearColor = new Color(0.60f, 0.66f, 0.63f, 1f);
        controller.clearAmbient = 0.95f;
        controller.clearSunIntensity = 1f;
        controller.sunLight = FindDirectionalLight();
        EditorUtility.SetDirty(controller);
    }

    static Light FindDirectionalLight()
    {
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < lights.Length; i++)
            if (lights[i].type == LightType.Directional) return lights[i];
        return null;
    }

    static Material EnsureSkyMaterial()
    {
        EnsureFolder("Assets/Materials");
        EnsureFolder(MaterialFolder);

        Material material = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
        Shader shader = Shader.Find("Skybox/Procedural");
        if (shader == null) return material;

        if (material == null)
        {
            material = new Material(shader) { name = "Mat_CloudySky" };
            AssetDatabase.CreateAsset(material, SkyMaterialPath);
        }

        material.shader = shader;
        // Tanah dan langit kelabu seragam; matahari sengaja dibuat sangat kecil
        // supaya cahayanya terasa tertutup awan kabut.
        material.SetColor("_SkyTint", new Color(0.40f, 0.39f, 0.35f, 1f));
        material.SetColor("_GroundColor", new Color(0.24f, 0.23f, 0.21f, 1f));
        material.SetFloat("_AtmosphereThickness", 0.65f);
        material.SetFloat("_SunSize", 0f);
        material.SetFloat("_SunSizeConvergence", 12f);
        material.SetFloat("_Exposure", 0.70f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void BuildCloudCeilingSetup()
    {
        // Awan lama (awan-awan kecil) sudah dihapus pengguna. Jangan dibuat lagi.
        GameObject legacy = GameObject.Find("CloudCover");
        if (legacy != null) Object.DestroyImmediate(legacy);

        CloudCeilingController controller =
            Object.FindFirstObjectByType<CloudCeilingController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            GameObject root = new GameObject("CloudCeiling");
            controller = root.AddComponent<CloudCeilingController>();
        }

        controller.ceilingHeight = 20f;
        controller.coverSize = 130f;
        controller.tiles = 7;
        controller.puffDiameter = 38f;
        controller.puffThickness = 4.2f;
        controller.pollutedColor = new Color(0.21f, 0.21f, 0.20f, 1f);
        controller.pollutedAlpha = 0.97f;
        controller.clearAlpha = 0f;
        EditorUtility.SetDirty(controller);
    }

    // ------------------------------------------------------------------
    // Laut + teleport
    // ------------------------------------------------------------------
    static void SetupSeaAndRespawn()
    {
        GameObject sea = GameObject.Find("Checkerboard Stage/Ground");
        if (sea != null)
        {
            sea.transform.position = new Vector3(0f, EnvironmentSetup.SeaLevel, 0f);
            MeshRenderer renderer = sea.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.enabled = true;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        OceanRespawn respawn = player.GetComponent<OceanRespawn>();
        if (respawn == null) respawn = player.AddComponent<OceanRespawn>();
        respawn.seaLevel = -0.2f;
        respawn.respawnDelay = 0.12f;
        respawn.spawnPoint = null;
        EditorUtility.SetDirty(respawn);
    }

    // ------------------------------------------------------------------
    // Air penyiram + kolam isi ulang
    // ------------------------------------------------------------------
    static void SetupWaterAndPond()
    {
        WateringCanSupply supply =
            Object.FindFirstObjectByType<WateringCanSupply>(FindObjectsInactive.Include);
        if (supply == null)
        {
            GameObject root = new GameObject("WateringCanSupply");
            supply = root.AddComponent<WateringCanSupply>();
        }

        supply.maxCharges = WateringCanSupply.DefaultMaxCharges;
        supply.charges = supply.maxCharges;
        EditorUtility.SetDirty(supply);

        GameObject pond = GameObject.Find("Environment3D/Pond");
        if (pond == null) pond = GameObject.Find("Pond");
        if (pond == null)
        {
            Debug.LogWarning("[GameplayV2Setup] Pond tidak ditemukan; isi ulang penyiram dilewati.");
            return;
        }

        PondRefillStation station = pond.GetComponent<PondRefillStation>();
        if (station == null) station = pond.AddComponent<PondRefillStation>();

        station.interactRadius = 0f;
        station.tapHeight = 1.4f;
        station.RebuildCollider();
        EditorUtility.SetDirty(station);
    }

    // ------------------------------------------------------------------
    // Grid statis
    // ------------------------------------------------------------------
    static void SetupGrid()
    {
        GridOverlay grid = Object.FindFirstObjectByType<GridOverlay>(FindObjectsInactive.Include);
        SunLampPlacer placer = Object.FindFirstObjectByType<SunLampPlacer>(FindObjectsInactive.Include);
        PestDefenseGame game = Object.FindFirstObjectByType<PestDefenseGame>(FindObjectsInactive.Include);
        GameObject player = GameObject.FindWithTag("Player");

        if (grid != null)
        {
            grid.areaSize = 56f;
            grid.cellSize = 2f;
            grid.height = 0.10f;
            grid.cellInset = 0.06f;
            grid.validColor = new Color(0.06f, 0.78f, 0.20f, 0.34f);
            grid.invalidColor = new Color(0.78f, 0.10f, 0.12f, 0.32f);
            grid.lampPlacer = placer;
            grid.game = game;
            grid.player = player != null ? player.transform : null;
            grid.minTreeSpacing = 1.8f;
            grid.minDistanceFromPlayer = 2f;

            // Y = 0: tinggi tanah dibakar ke vertex quad oleh GridOverlay.
            grid.transform.position = new Vector3(0f, 0f, 0f);
            grid.Rebuild();
            EditorUtility.SetDirty(grid);
        }

        TreeEditController controller = Object.FindFirstObjectByType<TreeEditController>(FindObjectsInactive.Include);
        if (controller != null)
        {
            controller.areaSize = 56f;
            controller.cellSize = 2f;
            controller.grid = grid;
            controller.lampPlacer = placer;
            EditorUtility.SetDirty(controller);
        }
    }

    // ------------------------------------------------------------------
    // Lampu matahari placeable
    // ------------------------------------------------------------------
    static void SetupSunLamp()
    {
        SunLampPlacer placer = Object.FindFirstObjectByType<SunLampPlacer>(FindObjectsInactive.Include);
        if (placer == null)
        {
            GameObject root = new GameObject("SunLampSystem");
            placer = root.AddComponent<SunLampPlacer>();
        }

        placer.radius = 10f;
        placer.modelScale = 0.9f;
        placer.pulseSpeed = 2.2f;
        placer.visualParent = placer.transform;
        EditorUtility.SetDirty(placer);

        TreeEditController controller = Object.FindFirstObjectByType<TreeEditController>(FindObjectsInactive.Include);
        if (controller != null)
        {
            controller.lampPlacer = placer;
            EditorUtility.SetDirty(controller);
        }
    }

    // ------------------------------------------------------------------
    // Senjata semprotan FPS
    // ------------------------------------------------------------------
    static void SetupSprayWeapon()
    {
        GameObject cameraObject = GameObject.Find("Perspective Camera");
        if (cameraObject == null) return;

        MobileAttackInput attack = Object.FindFirstObjectByType<MobileAttackInput>(FindObjectsInactive.Include);
        if (attack == null) return;

        Transform firePoint = cameraObject.transform.Find("FirePoint");
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(cameraObject.transform, false);
            firePoint = fp.transform;
        }

        // FirePoint di ujung nozzle: sedikit kanan, bawah, dan depan kamera.
        firePoint.localPosition = new Vector3(0.26f, -0.05f, 0.86f);
        firePoint.localRotation = Quaternion.identity;
        attack.firePoint = firePoint;
        attack.holdToFire = true;
        attack.cooldown = 0.09f;
        attack.bulletSpeed = 48f;
        attack.magazineSize = 12;
        attack.reloadDuration = 1.5f;
        EditorUtility.SetDirty(attack);

        // Viewmodel (child kamera, BUKAN child Player — renderer Player mati di FPS).
        GameObject viewRoot = FindOrCreateChild(cameraObject.transform, "SprayViewModel");
        viewRoot.transform.localPosition = new Vector3(0.28f, -0.34f, 0.55f);
        // +90 detik memutar nozzle (mulanya menghadap -X) supaya menghadap +Z,
        // searah pandangan kamera FPS.
        viewRoot.transform.localRotation = Quaternion.Euler(-12f, 72f, 4f);
        viewRoot.transform.localScale = Vector3.one;

        GameObject model = FindOrCreateChild(viewRoot.transform, "SprayModel");

        GameObject prefab = ModelLibrary.Load(ModelLibrary.SprayModel);
        if (prefab != null && model.transform.childCount == 0)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(model.transform, false);
        }

        GameObject sprayModel = model.transform.childCount > 0 ? model.transform.GetChild(0).gameObject : null;
        if (sprayModel != null)
        {
            sprayModel.transform.localPosition = Vector3.zero;
            sprayModel.transform.localRotation = Quaternion.identity;
            sprayModel.transform.localScale = Vector3.one * 0.38f;

            Collider[] colliders = sprayModel.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
        }

        SprayWeapon weapon = cameraObject.GetComponent<SprayWeapon>();
        if (weapon == null) weapon = cameraObject.AddComponent<SprayWeapon>();
        weapon.viewModel = viewRoot;
        weapon.muzzle = firePoint;
        weapon.restPosition = new Vector3(0.28f, -0.34f, 0.55f);
        weapon.restEuler = new Vector3(-12f, 72f, 4f);
        weapon.sprayEffect = EnsureSprayEffect(firePoint);
        Transform hudSafeArea = FindHUDSafeArea(weapon);
        weapon.crosshair = EnsureCrosshair(hudSafeArea);
        weapon.attackInput = attack;
        EnsureReloadBar(weapon, hudSafeArea);
        EditorUtility.SetDirty(weapon);

        // Klik biasa diganti tahan (HoldAttackButton), supaya mirip FPS.
        GameObject attackButton = GameObject.Find("AttackButton");
        if (attackButton != null)
        {
            Button button = attackButton.GetComponent<Button>();
            if (button != null)
                UnityEventTools.RemovePersistentListener(button.onClick, attack.Attack);

            HoldAttackButton hold = attackButton.GetComponent<HoldAttackButton>();
            if (hold == null) hold = attackButton.AddComponent<HoldAttackButton>();
            hold.attackInput = attack;
            EditorUtility.SetDirty(hold);
        }

        // Umpan balik visual tiap kali tembakan dilepas.
        UnityEventTools.RemovePersistentListener(attack.onAttack, weapon.OnFire);
        UnityEventTools.AddPersistentListener(attack.onAttack, weapon.OnFire);
    }

    static ParticleSystem EnsureSprayEffect(Transform firePoint)
    {
        GameObject existing = FindOrCreateChild(firePoint, "SprayMist");
        ParticleSystem ps = existing.GetComponent<ParticleSystem>();
        if (ps == null) ps = existing.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.26f;
        main.startSpeed = 3.2f;
        main.startSize = 0.05f;
        main.maxParticles = 150;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(0.70f, 0.95f, 0.78f, 0.85f);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[0]);

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 11f;
        shape.radius = 0.02f;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader != null && renderer != null)
            renderer.material = new Material(shader);

        return ps;
    }

    /// <summary>
    /// crosshair / reload bar harus tinggal satu, di parent yang sama dengan
    /// referensi SprayWeapon. Setup lama kadang membuat duplikat di SafeArea
    /// canvas lain (TreeCareUI, MobileUI, ...) dan menimpanya dari atas.
    /// </summary>
    static Transform FindHUDSafeArea(SprayWeapon weapon)
    {
        if (weapon != null && weapon.crosshair != null && weapon.crosshair.transform.parent != null)
            return weapon.crosshair.transform.parent;

        GameObject preferred = GameObject.Find("PestHUD/SafeArea");
        if (preferred != null) return preferred.transform;

        GameObject fallback = GameObject.Find("SafeArea");
        return fallback != null ? fallback.transform : null;
    }

    static void RemoveStaleHUDElement(string name, Transform keepParent)
    {
        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Transform candidate = all[i];
            if (candidate == null || candidate.name != name) continue;
            if (keepParent != null && candidate.parent == keepParent) continue;
            Object.DestroyImmediate(candidate.gameObject);
        }
    }

    static GameObject EnsureCrosshair(Transform safeArea)
    {
        if (safeArea == null) return null;

        GameObject root = FindOrCreateChild(safeArea, "Crosshair");
        RectTransform rootRect = root.GetComponent<RectTransform>();
        if (rootRect == null) rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(30f, 30f);

        // Crosshair sekarang satu gambar "+", bukan garis-garis terpisah.
        for (int i = root.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

        Image image = root.GetComponent<Image>();
        if (image == null) image = root.AddComponent<Image>();

        Sprite sprite = EnsureCrosshairSprite();
        if (sprite == null)
            Debug.LogWarning("[GameplayV2Setup] Sprite crosshair gagal dibuat.");

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = new Color(1f, 1f, 1f, 0.92f);
        image.raycastTarget = false;

        RemoveStaleHUDElement("Crosshair", safeArea);
        return root;
    }

    static Sprite EnsureCrosshairSprite()
    {
        const string folder = "Assets/Textures";
        const string path = folder + "/CrosshairPlus.png";

        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        EnsureFolder("Assets");
        EnsureFolder(folder);

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, clear);

        int center = size / 2;
        int half = 3;
        int inner = 8;
        int outer = size - 8;

        // Bayangan gelap tipis supaya "+" tetap terbaca di langit terang.
        DrawPlus(texture, new Color(0f, 0f, 0f, 0.55f), center, inner - 1, outer + 1, half + 1);
        DrawPlus(texture, Color.white, center, inner, outer, half);

        texture.Apply();
        File.WriteAllBytes(Path.GetFullPath(path), texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void DrawPlus(Texture2D texture, Color color, int center, int from, int to, int half)
    {
        for (int i = from; i < to; i++)
        {
            for (int t = -half; t <= half; t++)
            {
                int hx = i;
                int hz = center + t;
                if (hx >= 0 && hx < texture.width && hz >= 0 && hz < texture.height)
                    texture.SetPixel(hx, hz, color);

                int vx = center + t;
                int vz = i;
                if (vx >= 0 && vx < texture.width && vz >= 0 && vz < texture.height)
                    texture.SetPixel(vx, vz, color);
            }
        }
    }

    /// <summary>Bar pengisi di bawah crosshair, hanya tampil saat reload.</summary>
    static void EnsureReloadBar(SprayWeapon weapon, Transform safeArea)
    {
        if (safeArea == null) return;

        GameObject root = FindOrCreateChild(safeArea, "ReloadBar");

        RectTransform rootRect = root.GetComponent<RectTransform>();
        if (rootRect == null) rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = new Vector2(0f, -42f);
        rootRect.sizeDelta = new Vector2(130f, 10f);

        Image background = root.GetComponent<Image>();
        if (background == null) background = root.AddComponent<Image>();
        background.color = new Color(0.06f, 0.09f, 0.08f, 0.85f);
        background.raycastTarget = false;

        GameObject fillObject = FindOrCreateChild(root.transform, "Fill");
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        if (fillRect == null) fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        Image fill = fillObject.GetComponent<Image>();
        if (fill == null) fill = fillObject.AddComponent<Image>();
        fill.color = new Color(0.55f, 1f, 0.62f, 0.95f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        fill.raycastTarget = false;

        root.SetActive(false);
        weapon.reloadBarRoot = root;
        weapon.reloadFill = fill;

        RemoveStaleHUDElement("ReloadBar", safeArea);
    }

    static GameObject FindOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) return child.gameObject;

        GameObject created = new GameObject(name);
        created.transform.SetParent(parent, false);
        return created;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        int lastSlash = path.LastIndexOf('/');
        string parent = path.Substring(0, lastSlash);
        string leaf = path.Substring(lastSlash + 1);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
