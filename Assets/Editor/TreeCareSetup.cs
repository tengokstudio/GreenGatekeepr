using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tool sekali pakai untuk memasang lapisan gameplay Green Gatekeeper:
///  - CurrencySystem (koin)
///  - Pos warung toko + panel dua tab (beli bibit / jual hasil)
///  - Pos warung dekomposer 3 kotak + panel kartu (2 hama -> 1 pupuk, 10 detik)
///  - Panel perawatan pohon (pupuk + air + lampu)
///  - Patch katalog pohon, inventory, dan pohon lama di scene
/// Jalankan setelah Mobile, Pest Defense, Inventory, dan Tree Edit setup:
///   Tools &gt; Tree Care &gt; Setup (Active Scene)
/// </summary>
public static class TreeCareSetup
{
    const string RootName = "TreeCare";
    const string EconomyName = "Economy";
    const string ShopStationName = "ShopStation";
    const string DecomposerStationName = "DecomposerStation";
    const string ShopUiName = "ShopUI";
    const string DecomposerUiName = "DecomposerUI";
    const string CareUiName = "TreeCareUI";
    const string MaterialFolder = "Assets/Materials/TreeCare";

    const float ShopX = 6.5f;
    const float DecomposerX = -6.5f;
    const float PostZ = -3f;

    [MenuItem("Tools/Tree Care/Setup (Active Scene)", priority = 50)]
    public static void SetupFromMenu()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            EditorUtility.DisplayDialog("Tree Care Setup",
                "Simpan scene dulu, lalu jalankan setup lagi.", "OK");
            return;
        }

        Setup(scene);
    }

    [MenuItem("Tools/Tree Care/Remove (Active Scene)", priority = 51)]
    public static void RemoveFromMenu()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        Remove();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[TreeCareSetup] Fitur tree care / ekonomi dihapus dari scene.");
    }

    static void Setup(Scene scene)
    {
        Remove();
        EnsureEventSystem();
        EnsureFolder(MaterialFolder);

        CameraSwitcher switcher = Object.FindFirstObjectByType<CameraSwitcher>();
        InventorySystem inventory = Object.FindFirstObjectByType<InventorySystem>(FindObjectsInactive.Include);
        InventoryUI inventoryUI = Object.FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        PestDefenseGame game = Object.FindFirstObjectByType<PestDefenseGame>(FindObjectsInactive.Include);

        if (switcher == null)
            Debug.LogWarning("[TreeCareSetup] CameraSwitcher tidak ditemukan. Jalankan Mobile setup dulu.");
        if (inventory == null)
            Debug.LogWarning("[TreeCareSetup] InventorySystem tidak ditemukan. Jalankan Inventory setup dulu.");
        if (inventoryUI == null)
            Debug.LogWarning("[TreeCareSetup] InventoryUI tidak ditemukan. Jalankan Inventory setup dulu.");

        TreeCatalog catalog = PatchCatalog();
        PatchInventory(inventory);
        PatchSceneTrees(catalog);

        // ---------------- Economy ----------------
        GameObject economyObject = new GameObject(EconomyName);
        CurrencySystem currency = economyObject.AddComponent<CurrencySystem>();
        currency.startingCoins = 30;
        currency.coins = 30;

        // ---------------- UI ----------------
        ShopPanel shopPanel = BuildShopUi();
        DecomposerPanel decomposerPanel = BuildDecomposerUi();
        TreeCarePanel carePanel = BuildCareUi();

        // ---------------- Posts ----------------
        ShopStation shop = BuildShopStation(shopPanel);
        DecomposerStation decomposer = BuildDecomposerStation(decomposerPanel);

        // ---------------- Care controller ----------------
        GameObject careRoot = new GameObject(RootName);
        TreeCareController careController = careRoot.AddComponent<TreeCareController>();
        careController.switcher = switcher;
        careController.panel = carePanel;
        careController.game = game;

        // ---------------- Wiring ----------------
        if (inventoryUI != null) inventoryUI.inventory = inventory;

        EnsureTapRouter();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[TreeCareSetup] Selesai: koin 30, warung toko (" + ShopX + ", " + PostZ +
                  "), warung dekomposer (" + DecomposerX + ", " + PostZ +
                  "), panel rawat, katalog & inventory dipatch. Scene disimpan. Shop=" +
                  (shop != null) + ", Decomposer=" + (decomposer != null) + ".");
    }

    // ==================================================================
    // Katalog / inventory / pohon lama
    // ==================================================================
    static TreeCatalog PatchCatalog()
    {
        TreeCatalog catalog = Object.FindFirstObjectByType<TreeCatalog>(FindObjectsInactive.Include);
        if (catalog == null)
        {
            GameObject catalogObject = new GameObject("TreeCatalog");
            catalog = catalogObject.AddComponent<TreeCatalog>();
            catalog.variants = TreeVariant.CreateDefaults();
        }

        catalog.EnsureDefaults();
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    static void PatchInventory(InventorySystem inventory)
    {
        if (inventory == null) return;

        List<InventoryItemDefinition> definitions = new List<InventoryItemDefinition>();
        if (inventory.itemDefinitions != null)
        {
            for (int i = 0; i < inventory.itemDefinitions.Length; i++)
                if (inventory.itemDefinitions[i] != null) definitions.Add(inventory.itemDefinitions[i]);
        }

        EnsureDefinition(definitions, GameItems.SeedOak, "Bibit Oak", new Color(0.30f, 0.62f, 0.26f), 20);
        EnsureDefinition(definitions, GameItems.SeedPine, "Bibit Pinus", new Color(0.16f, 0.42f, 0.22f), 20);
        EnsureDefinition(definitions, GameItems.SeedBirch, "Bibit Birch", new Color(0.55f, 0.76f, 0.32f), 20);
        EnsureDefinition(definitions, GameItems.SeedBlossom, "Bibit Sakura", new Color(0.95f, 0.55f, 0.72f), 20);
        EnsureDefinition(definitions, GameItems.Fertilizer, "Pupuk", new Color(0.45f, 0.62f, 0.25f), 30);
        EnsureDefinition(definitions, GameItems.WateringCan, "Penyiram", new Color(0.30f, 0.55f, 0.85f), 1);
        EnsureDefinition(definitions, GameItems.SunLamp, "Lampu Matahari", new Color(1f, 0.85f, 0.30f), 1);
        EnsureDefinition(definitions, GameItems.PestCorpse, "Mayat Hama", new Color(0.58f, 0.24f, 0.72f), 20);
        EnsureDefinition(definitions, GameItems.Wood, "Kayu", new Color(0.55f, 0.38f, 0.20f), 50);
        EnsureDefinition(definitions, GameItems.GetahOak, "Getah Oak", new Color(0.78f, 0.55f, 0.16f), 30);
        EnsureDefinition(definitions, GameItems.GetahPinus, "Getah Pinus", new Color(0.52f, 0.36f, 0.12f), 30);
        EnsureDefinition(definitions, GameItems.AirBirch, "Air Birch", new Color(0.55f, 0.85f, 0.92f), 30);
        EnsureDefinition(definitions, GameItems.KelopakSakura, "Kelopak Sakura", new Color(1f, 0.62f, 0.80f), 30);

        inventory.itemDefinitions = definitions.ToArray();
        inventory.startingItems = new StartingItem[]
        {
            new StartingItem { id = GameItems.SeedOak, amount = 2 },
            new StartingItem { id = GameItems.SeedPine, amount = 2 },
            new StartingItem { id = GameItems.SeedBirch, amount = 2 },
            new StartingItem { id = GameItems.SeedBlossom, amount = 2 },
            new StartingItem { id = GameItems.WateringCan, amount = 1 },
            new StartingItem { id = GameItems.SunLamp, amount = 1 },
            new StartingItem { id = GameItems.Fertilizer, amount = 6 }
        };

        EditorUtility.SetDirty(inventory);
    }

    static void EnsureDefinition(List<InventoryItemDefinition> definitions, string id,
        string displayName, Color color, int maxStack)
    {
        for (int i = 0; i < definitions.Count; i++)
        {
            if (definitions[i].id != id) continue;
            if (string.IsNullOrEmpty(definitions[i].displayName)) definitions[i].displayName = displayName;
            return;
        }

        definitions.Add(new InventoryItemDefinition
        {
            id = id,
            displayName = displayName,
            color = color,
            maxStack = maxStack
        });
    }

    static void PatchSceneTrees(TreeCatalog catalog)
    {
        if (catalog == null) return;

        TreeDurability[] trees = Object.FindObjectsByType<TreeDurability>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < trees.Length; i++)
        {
            TreeDurability tree = trees[i];
            if (tree == null) continue;
            if (tree.GetComponent<TreeGrowth>() != null) continue;

            TreeVariant variant = null;

            if (!string.IsNullOrEmpty(tree.commodityItemId))
                variant = catalog.GetByCommodity(tree.commodityItemId);

            if (variant == null && !string.IsNullOrEmpty(tree.lootItemId))
                variant = catalog.Get(tree.lootItemId);

            if (variant == null)
            {
                string name = tree.name.ToLowerInvariant();
                if (name.Contains("oak")) variant = catalog.Get("tree_oak");
                else if (name.Contains("pine") || name.Contains("pinus")) variant = catalog.Get("tree_pine");
                else if (name.Contains("birch")) variant = catalog.Get("tree_birch");
                else if (name.Contains("blossom") || name.Contains("sakura")) variant = catalog.Get("tree_blossom");
            }

            if (variant == null) variant = catalog.Get("tree_oak");
            if (variant == null) continue;

            // Pohon lama tidak lagi memberi loot saat hancur.
            tree.lootItemId = "";
            tree.commodityItemId = variant.commodityItemId;
            tree.commodityYield = 1;
            tree.seedItemId = variant.seedItemId;

            EditorUtility.SetDirty(tree);
        }
    }

    // ==================================================================
    // UI toko (dua tab)
    // ==================================================================
    static ShopPanel BuildShopUi()
    {
        GameObject canvasObject = CreateCanvas(ShopUiName, 128, out RectTransform safeArea);

        GameObject blocker = CreateBlocker(canvasObject, out Button blockerButton);

        RectTransform panel = CreateRect("Panel", safeArea);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(1060f, 900f);
        panel.anchoredPosition = Vector2.zero;

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.055f, 0.07f, 0.085f, 0.98f);

        Text title = CreateText("Title", panel, "TOKO", 40, TextAnchor.MiddleLeft);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(420f, 64f);
        titleRect.anchoredPosition = new Vector2(28f, -16f);
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(0.62f, 0.94f, 0.7f);

        Text coins = CreateText("Coins", panel, "0 koin", 28, TextAnchor.MiddleRight);
        RectTransform coinsRect = coins.rectTransform;
        coinsRect.anchorMin = new Vector2(1f, 1f);
        coinsRect.anchorMax = new Vector2(1f, 1f);
        coinsRect.pivot = new Vector2(1f, 1f);
        coinsRect.sizeDelta = new Vector2(320f, 64f);
        coinsRect.anchoredPosition = new Vector2(-100f, -16f);
        coins.color = new Color(1f, 0.86f, 0.4f);
        coins.fontStyle = FontStyle.Bold;

        Button closeButton = CreateButton(panel, "CloseButton", "X",
            new Color(0.72f, 0.22f, 0.22f, 0.95f), out Text closeLabel);
        RectTransform closeRect = (RectTransform)closeButton.transform;
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(58f, 58f);
        closeRect.anchoredPosition = new Vector2(-24f, -18f);

        // ---------------- Tab ----------------
        RectTransform tabRow = CreateRect("Tabs", panel);
        tabRow.anchorMin = new Vector2(0f, 1f);
        tabRow.anchorMax = new Vector2(1f, 1f);
        tabRow.pivot = new Vector2(0.5f, 1f);
        tabRow.offsetMin = new Vector2(28f, -152f);
        tabRow.offsetMax = new Vector2(-28f, -92f);

        Button buyTab = CreateButton(tabRow, "BuyTab", "BELI BIBIT",
            new Color(0.20f, 0.55f, 0.32f, 1f), out Text buyTabLabel);
        RectTransform buyTabRect = (RectTransform)buyTab.transform;
        buyTabRect.anchorMin = new Vector2(0f, 0f);
        buyTabRect.anchorMax = new Vector2(0.5f, 1f);
        buyTabRect.offsetMin = new Vector2(0f, 0f);
        buyTabRect.offsetMax = new Vector2(-6f, 0f);
        buyTabLabel.fontSize = 26;

        Button sellTab = CreateButton(tabRow, "SellTab", "JUAL HASIL",
            new Color(0.14f, 0.17f, 0.20f, 1f), out Text sellTabLabel);
        RectTransform sellTabRect = (RectTransform)sellTab.transform;
        sellTabRect.anchorMin = new Vector2(0.5f, 0f);
        sellTabRect.anchorMax = new Vector2(1f, 1f);
        sellTabRect.offsetMin = new Vector2(6f, 0f);
        sellTabRect.offsetMax = new Vector2(0f, 0f);
        sellTabLabel.fontSize = 26;

        Text hint = CreateText("Hint", panel, "", 21, TextAnchor.MiddleLeft);
        RectTransform hintRect = hint.rectTransform;
        hintRect.anchorMin = new Vector2(0f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.offsetMin = new Vector2(32f, -190f);
        hintRect.offsetMax = new Vector2(-32f, -156f);
        hint.color = new Color(0.72f, 0.8f, 0.86f);

        // ---------------- Halaman daftar ----------------
        RectTransform pageArea = CreateRect("Pages", panel);
        pageArea.anchorMin = Vector2.zero;
        pageArea.anchorMax = Vector2.one;
        pageArea.offsetMin = new Vector2(24f, 92f);
        pageArea.offsetMax = new Vector2(-24f, -198f);

        RectTransform buyPage = CreateListPage("BuyPage", pageArea, out RectTransform buyContent);
        RectTransform sellPage = CreateListPage("SellPage", pageArea, out RectTransform sellContent);

        Text message = CreateText("Message", panel, "", 22, TextAnchor.MiddleCenter);
        RectTransform messageRect = message.rectTransform;
        messageRect.anchorMin = new Vector2(0f, 0f);
        messageRect.anchorMax = new Vector2(1f, 0f);
        messageRect.pivot = new Vector2(0.5f, 0f);
        messageRect.offsetMin = new Vector2(24f, 26f);
        messageRect.offsetMax = new Vector2(-24f, 80f);

        ShopPanel shop = canvasObject.AddComponent<ShopPanel>();
        shop.root = panel.gameObject;
        shop.blocker = blocker;
        shop.titleText = title;
        shop.coinsText = coins;
        shop.hintText = hint;
        shop.buyTabButton = buyTab;
        shop.sellTabButton = sellTab;
        shop.buyTabLabel = buyTabLabel;
        shop.sellTabLabel = sellTabLabel;
        shop.buyPage = buyPage.gameObject;
        shop.sellPage = sellPage.gameObject;
        shop.buyContent = buyContent;
        shop.sellContent = sellContent;
        shop.messageText = message;
        shop.closeButton = closeButton;
        shop.blockerButton = blockerButton;
        shop.inventory = Object.FindFirstObjectByType<InventorySystem>(FindObjectsInactive.Include);
        shop.currency = Object.FindFirstObjectByType<CurrencySystem>(FindObjectsInactive.Include);
        shop.catalog = Object.FindFirstObjectByType<TreeCatalog>(FindObjectsInactive.Include);

        UnityEventTools.AddPersistentListener(buyTab.onClick, shop.ShowBuy);
        UnityEventTools.AddPersistentListener(sellTab.onClick, shop.ShowSell);
        UnityEventTools.AddPersistentListener(closeButton.onClick, shop.Close);
        UnityEventTools.AddPersistentListener(blockerButton.onClick, shop.Close);

        sellPage.gameObject.SetActive(false);
        panel.gameObject.SetActive(false);
        return shop;
    }

    static RectTransform CreateListPage(string name, Transform parent, out RectTransform content)
    {
        RectTransform page = CreateRect(name, parent);
        Stretch(page);

        RectTransform viewport = CreateRect("Viewport", page);
        Stretch(viewport);

        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);
        viewportImage.raycastTarget = true;

        viewport.gameObject.AddComponent<RectMask2D>();

        content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = page.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;
        scroll.scrollSensitivity = 40f;

        return page;
    }

    // ==================================================================
    // UI dekomposer
    // ==================================================================
    static DecomposerPanel BuildDecomposerUi()
    {
        GameObject canvasObject = CreateCanvas(DecomposerUiName, 127, out RectTransform safeArea);

        GameObject blocker = CreateBlocker(canvasObject, out Button blockerButton);

        RectTransform panel = CreateRect("Panel", safeArea);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(1160f, 820f);
        panel.anchoredPosition = Vector2.zero;

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.055f, 0.07f, 0.085f, 0.98f);

        Text title = CreateText("Title", panel, "DEKOMPOSER HAMA", 36, TextAnchor.MiddleLeft);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(520f, 58f);
        titleRect.anchoredPosition = new Vector2(28f, -14f);
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(0.62f, 0.94f, 0.7f);

        Text pestCount = CreateText("PestCount", panel, "0 hama", 24, TextAnchor.MiddleLeft);
        RectTransform pestRect = pestCount.rectTransform;
        pestRect.anchorMin = new Vector2(0f, 1f);
        pestRect.anchorMax = new Vector2(0f, 1f);
        pestRect.pivot = new Vector2(0f, 1f);
        pestRect.sizeDelta = new Vector2(260f, 38f);
        pestRect.anchoredPosition = new Vector2(30f, -74f);
        pestCount.color = new Color(0.86f, 0.72f, 1f);
        pestCount.fontStyle = FontStyle.Bold;

        Text fertilizerCount = CreateText("FertilizerCount", panel, "0 pupuk", 24, TextAnchor.MiddleLeft);
        RectTransform fertRect = fertilizerCount.rectTransform;
        fertRect.anchorMin = new Vector2(0f, 1f);
        fertRect.anchorMax = new Vector2(0f, 1f);
        fertRect.pivot = new Vector2(0f, 1f);
        fertRect.sizeDelta = new Vector2(260f, 38f);
        fertRect.anchoredPosition = new Vector2(300f, -74f);
        fertilizerCount.color = new Color(0.72f, 1f, 0.76f);
        fertilizerCount.fontStyle = FontStyle.Bold;

        Text hint = CreateText("Hint", panel,
            "Tiap kotak butuh 2 hama, memproses 10 detik, lalu menghasilkan 1 pupuk.",
            21, TextAnchor.MiddleLeft);
        RectTransform hintRect = hint.rectTransform;
        hintRect.anchorMin = new Vector2(0f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.offsetMin = new Vector2(32f, -146f);
        hintRect.offsetMax = new Vector2(-32f, -112f);
        hint.color = new Color(0.72f, 0.8f, 0.86f);

        Button closeButton = CreateButton(panel, "CloseButton", "X",
            new Color(0.72f, 0.22f, 0.22f, 0.95f), out Text closeLabel);
        RectTransform closeRect = (RectTransform)closeButton.transform;
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(58f, 58f);
        closeRect.anchoredPosition = new Vector2(-24f, -18f);

        RectTransform boxRow = CreateRect("BoxRow", panel);
        boxRow.anchorMin = Vector2.zero;
        boxRow.anchorMax = Vector2.one;
        boxRow.offsetMin = new Vector2(24f, 112f);
        boxRow.offsetMax = new Vector2(-24f, -158f);

        HorizontalLayoutGroup rowLayout = boxRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 24f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        Text message = CreateText("Message", panel, "", 22, TextAnchor.MiddleCenter);
        RectTransform messageRect = message.rectTransform;
        messageRect.anchorMin = new Vector2(0f, 0f);
        messageRect.anchorMax = new Vector2(1f, 0f);
        messageRect.pivot = new Vector2(0.5f, 0f);
        messageRect.offsetMin = new Vector2(24f, 26f);
        messageRect.offsetMax = new Vector2(-24f, 80f);

        DecomposerPanel decomposer = canvasObject.AddComponent<DecomposerPanel>();
        decomposer.root = panel.gameObject;
        decomposer.blocker = blocker;
        decomposer.titleText = title;
        decomposer.hintText = hint;
        decomposer.pestCountText = pestCount;
        decomposer.fertilizerCountText = fertilizerCount;
        decomposer.messageText = message;
        decomposer.boxRow = boxRow;
        decomposer.closeButton = closeButton;
        decomposer.blockerButton = blockerButton;
        decomposer.inventory = Object.FindFirstObjectByType<InventorySystem>(FindObjectsInactive.Include);

        UnityEventTools.AddPersistentListener(closeButton.onClick, decomposer.Close);
        UnityEventTools.AddPersistentListener(blockerButton.onClick, decomposer.Close);

        panel.gameObject.SetActive(false);
        return decomposer;
    }

    // ==================================================================
    // UI perawatan pohon
    // ==================================================================
    static TreeCarePanel BuildCareUi()
    {
        GameObject canvasObject = CreateCanvas(CareUiName, 126, out RectTransform safeArea);

        GameObject blocker = CreateBlocker(canvasObject, out Button blockerButton);

        RectTransform panel = CreateRect("Panel", safeArea);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(900f, 460f);
        panel.anchoredPosition = new Vector2(0f, 20f);

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.055f, 0.08f, 0.065f, 0.98f);

        Text title = CreateText("Title", panel, "Pohon", 34, TextAnchor.MiddleLeft);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(600f, 54f);
        titleRect.anchoredPosition = new Vector2(28f, -14f);
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(0.72f, 1f, 0.78f);

        Text stage = CreateText("Stage", panel, "Bibit", 24, TextAnchor.MiddleLeft);
        RectTransform stageRect = stage.rectTransform;
        stageRect.anchorMin = new Vector2(0f, 1f);
        stageRect.anchorMax = new Vector2(1f, 1f);
        stageRect.pivot = new Vector2(0f, 1f);
        stageRect.offsetMin = new Vector2(28f, -112f);
        stageRect.offsetMax = new Vector2(-90f, -66f);
        stage.color = new Color(0.86f, 0.92f, 1f);

        Text tools = CreateText("Tools", panel, "Pupuk: 0   |   Penyiram: -   |   Lampu: -",
            20, TextAnchor.MiddleLeft);
        RectTransform toolsRect = tools.rectTransform;
        toolsRect.anchorMin = new Vector2(0f, 1f);
        toolsRect.anchorMax = new Vector2(1f, 1f);
        toolsRect.pivot = new Vector2(0f, 1f);
        toolsRect.offsetMin = new Vector2(28f, -150f);
        toolsRect.offsetMax = new Vector2(-90f, -114f);
        tools.color = new Color(0.78f, 0.82f, 0.86f);

        Button closeButton = CreateButton(panel, "CloseButton", "X",
            new Color(0.72f, 0.22f, 0.22f, 0.95f), out Text closeLabel);
        RectTransform closeRect = (RectTransform)closeButton.transform;
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(54f, 54f);
        closeRect.anchoredPosition = new Vector2(-20f, -16f);

        RectTransform buttonRow = CreateRect("Buttons", panel);
        buttonRow.anchorMin = new Vector2(0f, 0f);
        buttonRow.anchorMax = new Vector2(1f, 0f);
        buttonRow.pivot = new Vector2(0.5f, 0f);
        buttonRow.offsetMin = new Vector2(24f, 92f);
        buttonRow.offsetMax = new Vector2(-24f, 198f);

        HorizontalLayoutGroup rowLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 16f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;

        Button fertilizerButton = CreateButton(buttonRow, "FertilizerButton", "PUPUK",
            new Color(0.24f, 0.52f, 0.28f, 0.98f), out Text fertilizerLabel);
        Button waterButton = CreateButton(buttonRow, "WaterButton", "AIR",
            new Color(0.18f, 0.38f, 0.62f, 0.98f), out Text waterLabel);
        Button lampButton = CreateButton(buttonRow, "LampButton", "LAMPU",
            new Color(0.62f, 0.52f, 0.14f, 0.98f), out Text lampLabel);

        fertilizerLabel.fontSize = 26;
        waterLabel.fontSize = 26;
        lampLabel.fontSize = 26;

        Text message = CreateText("Message", panel, "", 20, TextAnchor.MiddleCenter);
        RectTransform messageRect = message.rectTransform;
        messageRect.anchorMin = new Vector2(0f, 0f);
        messageRect.anchorMax = new Vector2(1f, 0f);
        messageRect.pivot = new Vector2(0.5f, 0f);
        messageRect.offsetMin = new Vector2(24f, 28f);
        messageRect.offsetMax = new Vector2(-24f, 80f);
        message.color = new Color(0.84f, 0.9f, 0.94f);

        TreeCarePanel care = canvasObject.AddComponent<TreeCarePanel>();
        care.root = panel.gameObject;
        care.blocker = blocker;
        care.titleText = title;
        care.stageText = stage;
        care.toolsText = tools;
        care.messageText = message;
        care.fertilizerButton = fertilizerButton;
        care.waterButton = waterButton;
        care.lampButton = lampButton;
        care.closeButton = closeButton;
        care.blockerButton = blockerButton;
        care.fertilizerLabel = fertilizerLabel;
        care.waterLabel = waterLabel;
        care.lampLabel = lampLabel;

        UnityEventTools.AddPersistentListener(fertilizerButton.onClick, care.ApplyFertilizer);
        UnityEventTools.AddPersistentListener(waterButton.onClick, care.ApplyWater);
        UnityEventTools.AddPersistentListener(lampButton.onClick, care.ApplyLamp);
        UnityEventTools.AddPersistentListener(closeButton.onClick, care.Close);
        UnityEventTools.AddPersistentListener(blockerButton.onClick, care.Close);

        panel.gameObject.SetActive(false);
        return care;
    }

    // ==================================================================
    // Pos dunia (warung kayu beratap)
    // ==================================================================
    static ShopStation BuildShopStation(ShopPanel panel)
    {
        Warung warung = BuildWarung(ShopStationName, new Vector3(ShopX, 0f, PostZ), "TOKO",
            new Color(1f, 0.92f, 0.45f), new Color(0.42f, 0.29f, 0.16f),
            new Color(0.55f, 0.24f, 0.18f), new Color(0.16f, 0.34f, 0.22f));

        ShopStation station = warung.root.AddComponent<ShopStation>();
        station.panel = panel;
        station.stationName = "Toko";
        station.interactRadius = 8f;

        // Barang dagangan di atas meja.
        Material crateMaterial = EnsureMaterial("ShopCrate", new Color(0.62f, 0.44f, 0.24f));
        Material sackMaterial = EnsureMaterial("ShopSack", new Color(0.74f, 0.66f, 0.46f));

        GameObject crateA = CreatePrimitive(PrimitiveType.Cube, "CrateA", warung.root.transform,
            crateMaterial, false);
        crateA.transform.localPosition = new Vector3(-1.85f, 1.32f, 1.45f);
        crateA.transform.localScale = new Vector3(0.52f, 0.5f, 0.52f);

        GameObject crateB = CreatePrimitive(PrimitiveType.Cube, "CrateB", warung.root.transform,
            crateMaterial, false);
        crateB.transform.localPosition = new Vector3(-1.35f, 1.2f, 1.45f);
        crateB.transform.localScale = new Vector3(0.4f, 0.3f, 0.4f);

        GameObject sack = CreatePrimitive(PrimitiveType.Cube, "Sack", warung.root.transform,
            sackMaterial, false);
        sack.transform.localPosition = new Vector3(1.75f, 1.28f, 1.45f);
        sack.transform.localScale = new Vector3(0.5f, 0.44f, 0.46f);
        sack.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);

        // Bibit contoh: tiga pot kecil warna hijau di tengah meja.
        Material potMaterial = EnsureMaterial("ShopPot", new Color(0.48f, 0.30f, 0.18f));
        Material leafMaterial = EnsureMaterial("ShopLeaf", new Color(0.34f, 0.70f, 0.30f));

        for (int i = 0; i < 3; i++)
        {
            float x = -0.45f + i * 0.45f;

            GameObject pot = CreatePrimitive(PrimitiveType.Cylinder, "Pot_" + i,
                warung.root.transform, potMaterial, false);
            pot.transform.localPosition = new Vector3(x, 1.15f, 1.42f);
            pot.transform.localScale = new Vector3(0.18f, 0.11f, 0.18f);

            GameObject leaf = CreatePrimitive(PrimitiveType.Sphere, "Leaf_" + i,
                warung.root.transform, leafMaterial, false);
            leaf.transform.localPosition = new Vector3(x, 1.42f, 1.42f);
            leaf.transform.localScale = new Vector3(0.26f, 0.26f, 0.26f);
        }

        // Lampu warung.
        AddWarmLight(warung.root.transform, new Vector3(0f, 2.25f, 0.6f));

        BoxCollider collider = warung.root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1.35f, -0.2f);
        collider.size = new Vector3(5.6f, 2.7f, 4.2f);

        return station;
    }

    static DecomposerStation BuildDecomposerStation(DecomposerPanel panel)
    {
        Warung warung = BuildWarung(DecomposerStationName, new Vector3(DecomposerX, 0f, PostZ),
            "DEKOMPOSER", new Color(0.88f, 0.72f, 1f), new Color(0.36f, 0.27f, 0.19f),
            new Color(0.32f, 0.22f, 0.40f), new Color(0.26f, 0.16f, 0.32f));

        DecomposerStation station = warung.root.AddComponent<DecomposerStation>();
        station.panel = panel;
        station.stationName = "Dekomposer Hama";
        station.interactRadius = 8f;

        Material boxMaterial = EnsureMaterial("DecomposerBox", new Color(0.34f, 0.24f, 0.14f));
        Material crateMaterial = EnsureMaterial("DecompCrate", new Color(0.40f, 0.34f, 0.20f));

        List<DecomposerBox> boxes = new List<DecomposerBox>();

        for (int i = 0; i < 3; i++)
        {
            GameObject boxObject = CreatePrimitive(PrimitiveType.Cube, "Box_" + (i + 1),
                warung.root.transform, boxMaterial, true);
            boxObject.transform.localPosition = new Vector3((i - 1) * 1.3f, 1.52f, 1.45f);
            boxObject.transform.localScale = new Vector3(0.82f, 0.82f, 0.82f);

            DecomposerBox box = boxObject.AddComponent<DecomposerBox>();
            box.boxRenderer = boxObject.GetComponent<Renderer>();
            box.pestRequirement = 2;
            box.processDuration = 10f;
            box.maxReadyFertilizer = 2;
            box.labelHeight = 0.78f;
            boxes.Add(box);
        }

        // Peti kompos di belakang meja.
        GameObject compost = CreatePrimitive(PrimitiveType.Cube, "CompostCrate",
            warung.root.transform, crateMaterial, false);
        compost.transform.localPosition = new Vector3(0f, 0.62f, -1.2f);
        compost.transform.localScale = new Vector3(1.5f, 0.75f, 0.9f);

        GameObject compostLid = CreatePrimitive(PrimitiveType.Cube, "CompostLid",
            warung.root.transform, boxMaterial, false);
        compostLid.transform.localPosition = new Vector3(0f, 1.05f, -1.2f);
        compostLid.transform.localScale = new Vector3(1.62f, 0.12f, 1f);

        AddWarmLight(warung.root.transform, new Vector3(0f, 2.25f, 0.6f));

        BoxCollider collider = warung.root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1.35f, -0.2f);
        collider.size = new Vector3(5.6f, 2.7f, 4.2f);

        station.boxes = boxes.ToArray();
        return station;
    }

    struct Warung
    {
        public GameObject root;
    }

    /// <summary>
    /// Warung kayu sederhana: dek, 4 tiang, atap pelana, meja counter di depan,
    /// papan nama di atas atap, dan lampu hangat di bawah atap.
    /// Meja sengaja diletakkan di depan atap supaya tetap terlihat dari kamera isometrik.
    /// </summary>
    static Warung BuildWarung(string name, Vector3 position, string signText, Color signTextColor,
        Color woodColor, Color roofColor, Color signColor)
    {
        GameObject root = new GameObject(name);
        root.transform.position = position;

        Material wood = EnsureMaterial("WarungWood", woodColor);
        Material woodDark = EnsureMaterial("WarungWoodDark", new Color(0.30f, 0.20f, 0.11f));
        Material roof = EnsureMaterial("WarungRoof", roofColor);
        Material sign = EnsureMaterial("WarungSign", signColor);
        Material trim = EnsureMaterial("WarungTrim", new Color(0.68f, 0.52f, 0.30f));

        // Dek / lantai panggung.
        GameObject deck = CreatePrimitive(PrimitiveType.Cube, "Deck", root.transform, woodDark, false);
        deck.transform.localPosition = new Vector3(0f, 0.1f, -0.1f);
        deck.transform.localScale = new Vector3(5.8f, 0.2f, 4.2f);

        // 4 tiang.
        float[] postX = { -2.4f, 2.4f };
        float[] postZ = { -1.75f, 0.25f };

        for (int x = 0; x < postX.Length; x++)
        {
            for (int z = 0; z < postZ.Length; z++)
            {
                GameObject post = CreatePrimitive(PrimitiveType.Cylinder,
                    "Post_" + x + "_" + z, root.transform, wood, false);
                post.transform.localPosition = new Vector3(postX[x], 1.5f, postZ[z]);
                post.transform.localScale = new Vector3(0.1f, 1.4f, 0.1f);
            }
        }

        // Balok atas.
        GameObject beamFront = CreatePrimitive(PrimitiveType.Cube, "BeamFront", root.transform,
            wood, false);
        beamFront.transform.localPosition = new Vector3(0f, 2.92f, 0.25f);
        beamFront.transform.localScale = new Vector3(5.1f, 0.14f, 0.14f);

        GameObject beamBack = CreatePrimitive(PrimitiveType.Cube, "BeamBack", root.transform,
            wood, false);
        beamBack.transform.localPosition = new Vector3(0f, 2.92f, -1.75f);
        beamBack.transform.localScale = new Vector3(5.1f, 0.14f, 0.14f);

        // Atap pelana (dua bidang miring).
        GameObject roofFront = CreatePrimitive(PrimitiveType.Cube, "RoofFront", root.transform,
            roof, false);
        roofFront.transform.localPosition = new Vector3(0f, 2.68f, 0.28f);
        roofFront.transform.localScale = new Vector3(6.3f, 0.16f, 2.35f);
        roofFront.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);

        GameObject roofBack = CreatePrimitive(PrimitiveType.Cube, "RoofBack", root.transform,
            roof, false);
        roofBack.transform.localPosition = new Vector3(0f, 2.68f, -1.78f);
        roofBack.transform.localScale = new Vector3(6.3f, 0.16f, 2.35f);
        roofBack.transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);

        GameObject ridge = CreatePrimitive(PrimitiveType.Cube, "Ridge", root.transform,
            trim, false);
        ridge.transform.localPosition = new Vector3(0f, 3.08f, -0.75f);
        ridge.transform.localScale = new Vector3(6.4f, 0.16f, 0.22f);

        // Meja counter di depan atap.
        GameObject counterTop = CreatePrimitive(PrimitiveType.Cube, "CounterTop", root.transform,
            trim, false);
        counterTop.transform.localPosition = new Vector3(0f, 1.02f, 1.45f);
        counterTop.transform.localScale = new Vector3(4.9f, 0.16f, 1.0f);

        GameObject counterBody = CreatePrimitive(PrimitiveType.Cube, "CounterBody", root.transform,
            wood, false);
        counterBody.transform.localPosition = new Vector3(0f, 0.56f, 1.88f);
        counterBody.transform.localScale = new Vector3(4.9f, 0.92f, 0.14f);

        // Papan nama di atas atap.
        GameObject signBoard = CreatePrimitive(PrimitiveType.Cube, "SignBoard", root.transform,
            sign, false);
        signBoard.transform.localPosition = new Vector3(0f, 3.62f, -0.75f);
        signBoard.transform.localScale = new Vector3(3.6f, 0.74f, 0.16f);

        GameObject signLegL = CreatePrimitive(PrimitiveType.Cube, "SignLegL", root.transform,
            wood, false);
        signLegL.transform.localPosition = new Vector3(-1.2f, 3.22f, -0.75f);
        signLegL.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);

        GameObject signLegR = CreatePrimitive(PrimitiveType.Cube, "SignLegR", root.transform,
            wood, false);
        signLegR.transform.localPosition = new Vector3(1.2f, 3.22f, -0.75f);
        signLegR.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);

        CreateBillboardText("SignText", root.transform, signText,
            new Vector3(0f, 3.62f, -0.6f), 0.11f, signTextColor, 72);

        Warung warung;
        warung.root = root;
        return warung;
    }

    static void AddWarmLight(Transform parent, Vector3 localPosition)
    {
        GameObject lightObject = new GameObject("WarungLight");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition;

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.82f, 0.55f);
        light.range = 9f;
        light.intensity = 1.1f;
        light.shadows = LightShadows.None;
    }

    // ==================================================================
    // Helpers
    // ==================================================================
    static void Remove()
    {
        DestroyObject(RootName);
        DestroyObject(EconomyName);
        DestroyObject(ShopStationName);
        DestroyObject(DecomposerStationName);
        DestroyObject(ShopUiName);
        DestroyObject(DecomposerUiName);
        DestroyObject(CareUiName);

        RemoveLegacyAxe();
    }

    /// <summary>
    /// Kapak FPS sudah dihapus dari project. Scene lama mungkin masih menyimpan
    /// GameObject / komponen bernama Axe*. Dibersihkan lewat nama tipe supaya
    /// setup ini tidak butuh referensi ke class yang sudah tidak ada.
    /// </summary>
    static void RemoveLegacyAxe()
    {
        GameObject[] objects = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject go = objects[i];
            if (go == null) continue;

            if (go.name == "AxeUI" || go.name == "AxePivot")
            {
                Object.DestroyImmediate(go);
                continue;
            }

            Component[] components = go.GetComponents<Component>();
            for (int c = 0; c < components.Length; c++)
            {
                Component component = components[c];
                if (component == null) continue;

                if (component.GetType().Name == "AxeController")
                    Object.DestroyImmediate(component);
            }

            // Komponen yang script-nya sudah dihapus tampil sebagai "Missing (Mono Script)".
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }
    }

    static void DestroyObject(string name)
    {
        GameObject[] matches = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < matches.Length; i++)
        {
            if (matches[i] != null && matches[i].name == name)
                Object.DestroyImmediate(matches[i]);
        }
    }

    static GameObject CreateCanvas(string name, int sortingOrder, out RectTransform safeArea)
    {
        GameObject canvasObject = new GameObject(name,
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        safeArea = CreateRect("SafeArea", canvasObject.transform);
        Stretch(safeArea);
        safeArea.gameObject.AddComponent<SafeAreaFitter>();

        return canvasObject;
    }

    static GameObject CreateBlocker(GameObject canvasObject, out Button button)
    {
        RectTransform rect = CreateRect("Blocker", canvasObject.transform);
        Stretch(rect);

        // Blocker harus di belakang panel; kalau tidak, semua klik tombol tertelan.
        rect.SetAsFirstSibling();

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.001f);
        image.raycastTarget = true;

        button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        rect.gameObject.SetActive(false);
        return rect.gameObject;
    }

    static Button CreateButton(Transform parent, string name, string label, Color color,
        out Text labelText)
    {
        GameObject buttonObject = new GameObject(name,
            typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.layer = LayerMask.NameToLayer("UI");
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.88f, 1f, 0.9f);
        colors.pressedColor = new Color(0.7f, 0.9f, 0.75f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.55f);
        button.colors = colors;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.layer = LayerMask.NameToLayer("UI");
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = (RectTransform)labelObject.transform;
        Stretch(labelRect);

        labelText = labelObject.GetComponent<Text>();
        labelText.font = UiFont;
        labelText.text = label;
        labelText.fontSize = 22;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;
        labelText.raycastTarget = false;

        return button;
    }

    static void CreateBillboardText(string name, Transform parent, string content,
        Vector3 localPosition, float characterSize, Color color, int fontSize)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.characterSize = characterSize;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontStyle = FontStyle.Bold;
        text.color = color;

        MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 5;
        }

        textObject.AddComponent<HealthBarBillboard>();
    }

    static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent,
        Material material, bool keepCollider)
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

    static Material EnsureMaterial(string name, Color color)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.color = color;

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static void EnsureTapRouter()
    {
        TouchSwipeArea swipeArea = Object.FindFirstObjectByType<TouchSwipeArea>(FindObjectsInactive.Include);
        if (swipeArea == null)
        {
            Debug.LogWarning("[TreeCareSetup] TouchSwipeArea tidak ditemukan; tap dunia belum aktif.");
            return;
        }

        if (swipeArea.GetComponent<WorldTapRouter>() == null)
            swipeArea.gameObject.AddComponent<WorldTapRouter>();
    }

    static void EnsureEventSystem()
    {
        EventSystem existing = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (existing != null) return;

        GameObject eventSystemObject = new GameObject("EventSystem",
            typeof(EventSystem), typeof(InputSystemUIInputModule));
        InputSystemUIInputModule module = eventSystemObject.GetComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
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

    static Text CreateText(string name, Transform parent, string content, int fontSize,
        TextAnchor anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);

        Text text = go.GetComponent<Text>();
        text.font = UiFont;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    static Font UiFont
    {
        get
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
