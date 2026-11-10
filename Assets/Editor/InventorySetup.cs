using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tool sekali pakai untuk memasang inventory [General] ke scene yang sedang aktif:
///  - InventorySystem (grid 4x8 = 32 slot + katalog item)
///  - Sidebar dari kanan + tab panah + layar dim
///  - ScrollRect 4 kolom x 8 baris
/// Jalankan: Tools > Inventory > Setup (Active Scene)
/// </summary>
public static class InventorySetup
{
    const string RootName = "Inventory";
    const string UiName = "InventoryUI";

    const float PanelWidth = 760f;

    [MenuItem("Tools/Inventory/Setup (Active Scene)", priority = 30)]
    public static void SetupFromMenu()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            EditorUtility.DisplayDialog("Inventory Setup",
                "Simpan scene dulu, lalu jalankan setup lagi.", "OK");
            return;
        }

        Setup(scene);
    }

    [MenuItem("Tools/Inventory/Remove (Active Scene)", priority = 31)]
    public static void RemoveFromMenu()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        Remove();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[InventorySetup] Inventory dihapus dari scene.");
    }

    static void Setup(Scene scene)
    {
        Remove();
        EnsureEventSystem();

        // ---------------- Data ----------------
        GameObject root = new GameObject(RootName);
        InventorySystem system = root.AddComponent<InventorySystem>();
        system.columns = 4;
        system.rows = 8;
        system.itemDefinitions = new InventoryItemDefinition[]
        {
            new InventoryItemDefinition { id = "tree_oak", displayName = "Pohon Oak", color = new Color(0.30f, 0.62f, 0.26f), maxStack = 10 },
            new InventoryItemDefinition { id = "tree_pine", displayName = "Pohon Pinus", color = new Color(0.16f, 0.42f, 0.22f), maxStack = 10 },
            new InventoryItemDefinition { id = "tree_birch", displayName = "Pohon Birch", color = new Color(0.55f, 0.76f, 0.32f), maxStack = 10 },
            new InventoryItemDefinition { id = "tree_blossom", displayName = "Pohon Sakura", color = new Color(0.95f, 0.55f, 0.72f), maxStack = 10 },
            new InventoryItemDefinition { id = "pest_corpse", displayName = "Mayat Hama", color = new Color(0.58f, 0.24f, 0.72f), maxStack = 20 },
            new InventoryItemDefinition { id = "wood", displayName = "Kayu", color = new Color(0.55f, 0.38f, 0.20f), maxStack = 50 },
            new InventoryItemDefinition { id = "stone", displayName = "Batu", color = new Color(0.55f, 0.58f, 0.62f), maxStack = 50 }
        };

        // [General] 2 pohon dari tiap variasi supaya edit mode bisa langsung dites.
        system.startingItems = new StartingItem[]
        {
            new StartingItem { id = "tree_oak", amount = 2 },
            new StartingItem { id = "tree_pine", amount = 2 },
            new StartingItem { id = "tree_birch", amount = 2 },
            new StartingItem { id = "tree_blossom", amount = 2 }
        };

        // ---------------- Canvas ----------------
        GameObject canvasObject = new GameObject(UiName,
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(InventoryUI));
        canvasObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // ---------------- Dim (full screen, di belakang panel) ----------------
        RectTransform dimRect = CreateRect("Dim", canvasObject.transform);
        Stretch(dimRect);

        Image dimImage = dimRect.gameObject.AddComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0f);
        dimImage.raycastTarget = false;

        Button dimButton = dimRect.gameObject.AddComponent<Button>();
        dimButton.targetGraphic = dimImage;
        dimButton.transition = Selectable.Transition.None;

        // ---------------- Safe area ----------------
        RectTransform safeArea = CreateRect("SafeArea", canvasObject.transform);
        Stretch(safeArea);
        safeArea.gameObject.AddComponent<SafeAreaFitter>();

        // ---------------- Panel sidebar ----------------
        RectTransform panel = CreateRect("Panel", safeArea);
        panel.anchorMin = new Vector2(1f, 0f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 0.5f);
        panel.sizeDelta = new Vector2(PanelWidth, 0f);
        panel.anchoredPosition = new Vector2(PanelWidth, 0f);

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.07f, 0.09f, 0.97f);

        // Aksen tipis di tepi kiri panel.
        RectTransform accent = CreateRect("Accent", panel);
        accent.anchorMin = new Vector2(0f, 0f);
        accent.anchorMax = new Vector2(0f, 1f);
        accent.pivot = new Vector2(0f, 0.5f);
        accent.sizeDelta = new Vector2(5f, 0f);
        accent.anchoredPosition = Vector2.zero;
        Image accentImage = accent.gameObject.AddComponent<Image>();
        accentImage.color = new Color(0.30f, 0.72f, 0.40f, 0.9f);
        accentImage.raycastTarget = false;

        // ---------------- Header ----------------
        RectTransform header = CreateRect("Header", panel);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.offsetMin = new Vector2(24f, -100f);
        header.offsetMax = new Vector2(-24f, -16f);

        Text title = CreateText("Title", header, "INVENTORY", 34, TextAnchor.MiddleLeft);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 0.5f);
        titleRect.sizeDelta = new Vector2(340f, 0f);
        titleRect.anchoredPosition = Vector2.zero;

        Text capacity = CreateText("Capacity", header, "0/32", 22, TextAnchor.MiddleRight);
        RectTransform capacityRect = capacity.rectTransform;
        capacityRect.anchorMin = new Vector2(1f, 0f);
        capacityRect.anchorMax = new Vector2(1f, 1f);
        capacityRect.pivot = new Vector2(1f, 0.5f);
        capacityRect.sizeDelta = new Vector2(180f, 0f);
        capacityRect.anchoredPosition = new Vector2(-68f, 0f);
        capacity.color = new Color(0.72f, 0.78f, 0.84f);

        RectTransform closeRect = CreateRect("CloseButton", header);
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(52f, 52f);
        closeRect.anchoredPosition = Vector2.zero;

        Image closeImage = closeRect.gameObject.AddComponent<Image>();
        closeImage.color = new Color(0.72f, 0.22f, 0.22f, 0.95f);

        Button closeButton = closeRect.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;

        Text closeLabel = CreateText("X", closeRect, "X", 26, TextAnchor.MiddleCenter);
        Stretch(closeLabel.rectTransform);

        // ---------------- Scroll view ----------------
        RectTransform viewport = CreateRect("Viewport", panel);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(24f, 20f);
        viewport.offsetMax = new Vector2(-24f, -108f);

        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);
        viewportImage.raycastTarget = true;

        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(150f, 150f);
        grid.spacing = new Vector2(12f, 12f);
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.1f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;
        scroll.scrollSensitivity = 30f;

        // ---------------- Tab panah (nempel di tepi kiri panel) ----------------
        RectTransform tab = CreateRect("Tab", panel);
        tab.anchorMin = new Vector2(0f, 0.5f);
        tab.anchorMax = new Vector2(0f, 0.5f);
        tab.pivot = new Vector2(1f, 0.5f);
        tab.sizeDelta = new Vector2(64f, 170f);
        tab.anchoredPosition = Vector2.zero;

        Image tabImage = tab.gameObject.AddComponent<Image>();
        tabImage.color = new Color(0.12f, 0.16f, 0.14f, 0.98f);

        Button tabButton = tab.gameObject.AddComponent<Button>();
        tabButton.targetGraphic = tabImage;

        Text tabLabel = CreateText("Arrow", tab, "<", 38, TextAnchor.MiddleCenter);
        Stretch(tabLabel.rectTransform);
        tabLabel.fontStyle = FontStyle.Bold;
        tabLabel.color = new Color(0.55f, 0.92f, 0.62f);

        // ---------------- Wiring ----------------
        InventoryUI ui = canvasObject.GetComponent<InventoryUI>();
        ui.inventory = system;
        ui.panel = panel;
        ui.dimImage = dimImage;
        ui.content = content;
        ui.capacityText = capacity;
        ui.tabLabel = tabLabel;
        ui.tabButton = tabButton;
        ui.closeButton = closeButton;
        ui.dimButton = dimButton;

        UnityEventTools.AddPersistentListener(tabButton.onClick, ui.Toggle);
        UnityEventTools.AddPersistentListener(closeButton.onClick, ui.Close);
        UnityEventTools.AddPersistentListener(dimButton.onClick, ui.Close);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[InventorySetup] Selesai: InventorySystem (4x8) + sidebar kanan + tab panah + dim + ScrollRect. Scene disimpan.");
    }

    static void Remove()
    {
        GameObject oldRoot = GameObject.Find(RootName);
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);

        GameObject oldUi = GameObject.Find(UiName);
        if (oldUi != null) Object.DestroyImmediate(oldUi);
    }

    static void EnsureEventSystem()
    {
        EventSystem existing = Object.FindFirstObjectByType<EventSystem>();
        if (existing != null)
        {
            if (existing.GetComponent<InputSystemUIInputModule>() == null
                && existing.GetComponent<StandaloneInputModule>() != null)
            {
                Debug.LogWarning("[InventorySetup] EventSystem memakai StandaloneInputModule (legacy). " +
                                 "Sebaiknya ganti ke InputSystemUIInputModule.");
            }
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem",
            typeof(EventSystem), typeof(InputSystemUIInputModule));
        InputSystemUIInputModule module = eventSystemObject.GetComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
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
