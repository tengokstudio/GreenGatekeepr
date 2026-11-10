using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tool sekali pakai untuk memasang fitur [General] 4 variasi pohon + forest random,
/// dan [Isometric] edit mode (tombol EDIT kanan atas, grid samar, tanam dari inventory).
/// Jalankan setelah Pest Defense &amp; Inventory setup:
///   Tools &gt; Tree Edit &gt; Setup (Active Scene)
/// </summary>
public static class TreeEditSetup
{
    const string RootName = "TreeEdit";
    const string UiName = "TreeEditUI";
    const string ForestName = "Forest";
    const string CatalogName = "TreeCatalog";

    [MenuItem("Tools/Tree Edit/Setup (Active Scene)", priority = 40)]
    public static void SetupFromMenu()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            EditorUtility.DisplayDialog("Tree Edit Setup",
                "Simpan scene dulu, lalu jalankan setup lagi.", "OK");
            return;
        }

        Setup(scene);
    }

    [MenuItem("Tools/Tree Edit/Remove (Active Scene)", priority = 41)]
    public static void RemoveFromMenu()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        Remove();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[TreeEditSetup] Fitur pohon & edit mode dihapus dari scene.");
    }

    [MenuItem("Tools/Tree Edit/Reset Forest Save", priority = 42)]
    public static void ResetForestSaveFromMenu()
    {
        ForestGenerator.ResetSavedLayout();
    }

    static void Setup(Scene scene)
    {
        Remove();

        // ---------------- References ----------------
        CameraSwitcher switcher = Object.FindFirstObjectByType<CameraSwitcher>();
        MobileCameraRig rig = Object.FindFirstObjectByType<MobileCameraRig>();
        InventoryUI inventoryUI = Object.FindFirstObjectByType<InventoryUI>();
        InventorySystem inventory = Object.FindFirstObjectByType<InventorySystem>();
        PestDefenseGame game = Object.FindFirstObjectByType<PestDefenseGame>();

        GameObject playerObject = GameObject.FindWithTag("Player");
        Transform player = playerObject != null ? playerObject.transform : null;

        GameObject treesObject = GameObject.Find("PestDefense/Trees");
        Transform treesParent = treesObject != null ? treesObject.transform : null;

        if (player == null)
            Debug.LogWarning("[TreeEditSetup] Player (tag 'Player') tidak ditemukan.");
        if (game == null)
            Debug.LogWarning("[TreeEditSetup] PestDefenseGame tidak ditemukan. Jalankan Pest Defense setup dulu.");
        if (inventory == null || inventoryUI == null)
            Debug.LogWarning("[TreeEditSetup] Inventory belum ada. Jalankan Inventory setup dulu.");

        // ---------------- Tree catalog ----------------
        GameObject catalogObject = new GameObject(CatalogName);
        TreeCatalog catalog = catalogObject.AddComponent<TreeCatalog>();
        catalog.variants = TreeVariant.CreateDefaults();

        // ---------------- Forest generator ----------------
        GameObject forestObject = new GameObject(ForestName);
        ForestGenerator forest = forestObject.AddComponent<ForestGenerator>();
        forest.catalog = catalog;
        forest.game = game;
        forest.player = player;
        forest.treesParent = treesParent;
        forest.treeCount = 40;
        forest.areaSize = 100f;
        forest.minSpacing = 4f;
        forest.minDistanceFromPlayer = 8f;
        forest.minDistanceFromNest = 6f;
        forest.persistLayout = true;

        // ---------------- Edit mode root ----------------
        GameObject editRoot = new GameObject(RootName);
        TreeEditController controller = editRoot.AddComponent<TreeEditController>();

        GameObject gridObject = new GameObject("GridOverlay");
        gridObject.transform.SetParent(editRoot.transform, false);
        GridOverlay grid = gridObject.AddComponent<GridOverlay>();
        grid.areaSize = 100f;
        grid.cellSize = 2f;

        // ---------------- UI ----------------
        GameObject canvasObject = new GameObject(UiName,
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform safeArea = CreateRect("SafeArea", canvasObject.transform);
        Stretch(safeArea);
        safeArea.gameObject.AddComponent<SafeAreaFitter>();

        // Tombol EDIT di pojok kanan atas (di bawah HUD).
        RectTransform buttonRect = CreateRect("EditButton", safeArea);
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.sizeDelta = new Vector2(230f, 86f);
        buttonRect.anchoredPosition = new Vector2(-24f, -92f);

        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = new Color(0.16f, 0.34f, 0.22f, 0.96f);

        Button editButton = buttonRect.gameObject.AddComponent<Button>();
        editButton.targetGraphic = buttonImage;
        editButton.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = editButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 1f, 0.9f);
        colors.pressedColor = new Color(0.7f, 0.9f, 0.75f);
        editButton.colors = colors;

        Text buttonLabel = CreateText("Label", buttonRect, "EDIT", 30, TextAnchor.MiddleCenter);
        Stretch(buttonLabel.rectTransform);
        buttonLabel.fontStyle = FontStyle.Bold;
        buttonLabel.color = new Color(0.75f, 1f, 0.82f);

        // Hint / toast di bawah layar.
        RectTransform hintRect = CreateRect("Hint", safeArea);
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.sizeDelta = new Vector2(1000f, 96f);
        hintRect.anchoredPosition = new Vector2(0f, 130f);

        Image hintBackground = hintRect.gameObject.AddComponent<Image>();
        hintBackground.color = new Color(0.05f, 0.08f, 0.06f, 0.78f);
        hintBackground.raycastTarget = false;

        Text hintText = CreateText("Text", hintRect,
            "Pilih pohon di inventory, lalu tap grid untuk menanam. Tap pohon tanam untuk menghapus.",
            26, TextAnchor.MiddleCenter);
        RectTransform hintTextRect = hintText.rectTransform;
        hintTextRect.anchorMin = Vector2.zero;
        hintTextRect.anchorMax = Vector2.one;
        hintTextRect.offsetMin = new Vector2(20f, 10f);
        hintTextRect.offsetMax = new Vector2(-20f, -10f);
        hintText.color = new Color(0.85f, 1f, 0.88f);

        hintRect.gameObject.SetActive(false);

        // ---------------- Wiring ----------------
        controller.switcher = switcher;
        controller.rig = rig;
        controller.inventoryUI = inventoryUI;
        controller.inventory = inventory;
        controller.catalog = catalog;
        controller.grid = grid;
        controller.game = game;
        controller.player = player;
        controller.treesParent = treesParent;
        controller.editButton = editButton;
        controller.buttonLabel = buttonLabel;
        controller.hintRoot = hintRect.gameObject;
        controller.hintText = hintText;
        controller.areaSize = 100f;
        controller.cellSize = 2f;

        UnityEventTools.AddPersistentListener(editButton.onClick, controller.ToggleEditMode);

        // ---------------- Tap router di area swipe ----------------
        EnsureTapRouter();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[TreeEditSetup] Selesai: TreeCatalog (4 variasi), ForestGenerator (40 pohon), " +
                  "edit mode + grid + tombol EDIT + tap router. Scene disimpan.");
    }

    static void EnsureTapRouter()
    {
        TouchSwipeArea swipeArea = Object.FindFirstObjectByType<TouchSwipeArea>(FindObjectsInactive.Include);
        if (swipeArea == null)
        {
            Debug.LogWarning("[TreeEditSetup] TouchSwipeArea tidak ditemukan; tap dunia belum aktif.");
            return;
        }

        if (swipeArea.GetComponent<WorldTapRouter>() == null)
            swipeArea.gameObject.AddComponent<WorldTapRouter>();
    }

    static void Remove()
    {
        DestroyObject(RootName);
        DestroyObject(UiName);
        DestroyObject(ForestName);
        DestroyObject(CatalogName);
    }

    static void DestroyObject(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) Object.DestroyImmediate(existing);
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    static Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);

        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
    }
}
