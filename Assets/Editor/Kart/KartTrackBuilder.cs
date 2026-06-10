using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class KartTrackBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/Kart";
    private const string MaterialFolder = "Assets/Materials/Kart";
    private const string ConfigFolder = "Assets/ScriptableObjects/Kart";
    private const string TextureFolder = "Assets/Textures/Kart";
    private const string EntryScenePath = "Assets/Scenes/KartEntryGame.unity";
    private const string VehicleConfigScenePath = "Assets/Scenes/KartVehicleConfig.unity";
    private const string FormalGameScenePath = "Assets/Scenes/KartFormalGame.unity";
    private const string LegacyCombinedScenePath = "Assets/Scenes/KartRaceMap.unity";
    private const string ConfigPath = ConfigFolder + "/SC_ArcadeKart.asset";
    private const string MenuBackgroundPath = TextureFolder + "/T_Kart_Menu_Background.png";
    private const string GrassTexturePath = TextureFolder + "/T_Kart_Grass_Mottled.png";
    private const string ImportedKartVisualPath = "Assets/Generated/BlenderCars/PF_CurrentBlenderCar.prefab";
    private const string ExhaustSmokePrefabPath = "Assets/Prefabs/Kart/PF_KartExhaustSmoke.prefab";
    private const string ImportedItemBoxVisualPath = "Assets/Generated/BlenderItemBoxes/CurrentBlenderItemBox.fbx";
    private const string CelebrationRibbonEffectPath = "Assets/Generated/CelebrationRibbons/PF_CelebrationRibbonBrush.prefab";
    private const string SpeedDashboardPrefabPath = "Assets/Generated/KartDashboardUI/PF_SpeedDashboardGauge.prefab";

    [MenuItem("Tools/Kart/Build Mario Kart Style Map")]
    public static void BuildMap()
    {
        EnsureFolders();

        Material grass = GetOrCreateMaterial($"{MaterialFolder}/M_Kart_Grass.mat", new Color(0.22f, 0.56f, 0.18f));
        ConfigureGrassMaterial(grass);
        Material road = GetOrCreateMaterial($"{MaterialFolder}/M_Kart_Road.mat", new Color(0.12f, 0.12f, 0.14f));
        Material boost = GetOrCreateMaterial($"{MaterialFolder}/M_Kart_Boost.mat", new Color(0.05f, 0.9f, 1f));
        Material item = GetOrCreateMaterial($"{MaterialFolder}/M_Kart_ItemBox.mat", new Color(0.95f, 0.72f, 0.12f));
        Material kartBody = GetOrCreateMaterial($"{MaterialFolder}/M_Kart_Body.mat", new Color(1f, 0.18f, 0.08f));
        Material kartAccent = GetOrCreateMaterial($"{MaterialFolder}/M_Kart_Accent.mat", new Color(1f, 0.9f, 0.05f));
        Material wheel = GetOrCreateMaterial($"{MaterialFolder}/M_Kart_Wheel.mat", new Color(0.025f, 0.025f, 0.025f));

        KartDriveConfig driveConfig = GetOrCreateDriveConfig();
        GameObject groundPrefab = CreateGroundPrefab(grass);
        GameObject straightPrefab = CreateTrackStraightPrefab(road);
        GameObject cornerPrefab = CreateTrackCornerPrefab(road);
        GameObject boostPadPrefab = CreateBoostPadPrefab(boost);
        GameObject itemBoxPrefab = CreateItemBoxPrefab(item);
        GameObject checkpointPrefab = CreateCheckpointPrefab();
        GameObject smallTirePrefab = CreateTirePrefab("PF_Tire_Small", KartTireSize.Small, 0.28f, 0.18f, wheel, kartAccent);
        GameObject mediumTirePrefab = CreateTirePrefab("PF_Tire_Medium", KartTireSize.Medium, 0.34f, 0.22f, wheel, kartAccent);
        GameObject largeTirePrefab = CreateTirePrefab("PF_Tire_Large", KartTireSize.Large, 0.42f, 0.28f, wheel, kartAccent);
        GameObject kartPrefab = CreateKartPrefab(driveConfig, kartBody, kartAccent, smallTirePrefab, mediumTirePrefab, largeTirePrefab);
        Sprite menuBackground = GetMenuBackgroundSprite();
        GameObject entryMenuUiPrefab = CreateEntryMenuUiPrefab(menuBackground);
        GameObject configMenuUiPrefab = CreateVehicleConfigUiPrefab(menuBackground);

        ApplyProjectAspectSettings();
        BuildEntryScene(entryMenuUiPrefab);
        BuildVehicleConfigScene(kartPrefab, configMenuUiPrefab, road);
        BuildFormalGameScene(
            groundPrefab,
            straightPrefab,
            cornerPrefab,
            boostPadPrefab,
            itemBoxPrefab,
            checkpointPrefab,
            kartPrefab);

        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Kart scene flow built: {EntryScenePath}, {VehicleConfigScenePath}, {FormalGameScenePath}.");
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets", "Prefabs");
        CreateFolder("Assets/Prefabs", "Kart");
        CreateFolder("Assets", "Materials");
        CreateFolder("Assets/Materials", "Kart");
        CreateFolder("Assets", "ScriptableObjects");
        CreateFolder("Assets/ScriptableObjects", "Kart");
        CreateFolder("Assets", "Scenes");
        CreateFolder("Assets", "Textures");
        CreateFolder("Assets/Textures", "Kart");
        CreateFolder("Assets", "Scripts");
        CreateFolder("Assets/Scripts", "Kart");
        CreateFolder("Assets", "Editor");
        CreateFolder("Assets/Editor", "Kart");
    }

    private static void CreateFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static void ApplyProjectAspectSettings()
    {
        PlayerSettings.defaultScreenWidth = 1080;
        PlayerSettings.defaultScreenHeight = 1920;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
    }

    private static GameObject CreateGroundPrefab(Material material)
    {
        GameObject root = new GameObject("PF_Kart_Ground");
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "GrassBase";
        ground.transform.SetParent(root.transform, false);
        ground.transform.localPosition = new Vector3(0f, -0.08f, 0f);
        ground.transform.localScale = new Vector3(160f, 0.12f, 130f);
        ground.GetComponent<Renderer>().sharedMaterial = material;
        return SavePrefab(root, $"{PrefabFolder}/PF_Kart_Ground.prefab");
    }

    private static GameObject CreateTrackStraightPrefab(Material road)
    {
        GameObject root = new GameObject("PF_Track_Straight");
        GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesh.name = "Road";
        mesh.transform.SetParent(root.transform, false);
        mesh.transform.localScale = new Vector3(10f, 0.12f, 12f);
        mesh.GetComponent<Renderer>().sharedMaterial = road;
        return SavePrefab(root, $"{PrefabFolder}/PF_Track_Straight.prefab");
    }

    private static GameObject CreateTrackCornerPrefab(Material road)
    {
        GameObject root = new GameObject("PF_Track_Corner");
        GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesh.name = "CornerRoad";
        mesh.transform.SetParent(root.transform, false);
        mesh.transform.localScale = new Vector3(14f, 0.12f, 14f);
        mesh.GetComponent<Renderer>().sharedMaterial = road;
        return SavePrefab(root, $"{PrefabFolder}/PF_Track_Corner.prefab");
    }

    private static GameObject CreateBoostPadPrefab(Material material)
    {
        GameObject root = new GameObject("PF_BoostPad");
        GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesh.name = "BoostSurface";
        mesh.transform.SetParent(root.transform, false);
        mesh.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        mesh.transform.localScale = new Vector3(5.5f, 0.12f, 2.2f);
        mesh.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(mesh.GetComponent<Collider>());

        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.3f, 0f);
        trigger.size = new Vector3(5.5f, 0.8f, 2.2f);
        root.AddComponent<KartBoostPad>();
        return SavePrefab(root, $"{PrefabFolder}/PF_BoostPad.prefab");
    }

    private static GameObject CreateItemBoxPrefab(Material material)
    {
        GameObject root = new GameObject("PF_ItemBox");
        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.85f, 0f);
        trigger.size = new Vector3(1.2f, 1.5f, 1.2f);
        KartPickupBox pickup = root.AddComponent<KartPickupBox>();

        Transform visualRoot = new GameObject("VisualRoot").transform;
        visualRoot.SetParent(root.transform, false);
        visualRoot.localPosition = new Vector3(0f, 0.85f, 0f);

        GameObject importedItemBox = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedItemBoxVisualPath);
        if (importedItemBox != null)
        {
            GameObject itemBoxVisual = PrefabUtility.InstantiatePrefab(importedItemBox) as GameObject;
            if (itemBoxVisual == null)
            {
                itemBoxVisual = Object.Instantiate(importedItemBox);
            }

            itemBoxVisual.name = "ImportedBlenderItemBox";
            itemBoxVisual.transform.SetParent(visualRoot, false);
            itemBoxVisual.transform.localPosition = Vector3.zero;
            itemBoxVisual.transform.localRotation = Quaternion.identity;
            itemBoxVisual.transform.localScale = Vector3.one;
        }
        else
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "QuestionBox";
            cube.transform.SetParent(visualRoot, false);
            cube.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            cube.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);
            cube.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
        }

        SerializedObject serialized = new SerializedObject(pickup);
        serialized.FindProperty("visualRoot").objectReferenceValue = visualRoot;
        GameObject celebrationEffect = AssetDatabase.LoadAssetAtPath<GameObject>(CelebrationRibbonEffectPath);
        if (celebrationEffect != null)
        {
            serialized.FindProperty("pickupEffectPrefab").objectReferenceValue = celebrationEffect.GetComponent<CelebrationRibbonBrush>();
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        return SavePrefab(root, $"{PrefabFolder}/PF_ItemBox.prefab");
    }

    private static GameObject CreateCheckpointPrefab()
    {
        GameObject root = new GameObject("PF_CheckpointGate");
        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 1.7f, 0f);
        trigger.size = new Vector3(11f, 3.5f, 1.2f);
        root.AddComponent<KartCheckpoint>();
        return SavePrefab(root, $"{PrefabFolder}/PF_CheckpointGate.prefab");
    }

    private static Sprite GetMenuBackgroundSprite()
    {
        if (!File.Exists(MenuBackgroundPath))
        {
            Debug.LogWarning($"Menu background not found at {MenuBackgroundPath}.");
            return null;
        }

        TextureImporter importer = AssetImporter.GetAtPath(MenuBackgroundPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        else
        {
            AssetDatabase.ImportAsset(MenuBackgroundPath, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(MenuBackgroundPath);
    }

    private static GameObject CreateEntryMenuUiPrefab(Sprite backgroundSprite)
    {
        GameObject root = CreateMenuCanvas(
            "PF_KartEntryMenuUI",
            backgroundSprite,
            includeBackdrop: true,
            out KartMenuFlow flow,
            out Transform contentRoot);

        GameObject mainPage = CreatePage(contentRoot, "MainPage");
        Image mainPanel = CreatePanel(mainPage.transform, "MainPanel", new Vector2(0f, -80f), new Vector2(760f, 520f));
        CreateText(mainPanel.transform, "Title", "KART TEST", 78, FontStyle.Bold, new Vector2(0f, 130f), new Vector2(640f, 96f));
        CreateText(mainPanel.transform, "Subtitle", "Arcade tire setup prototype", 30, FontStyle.Normal, new Vector2(0f, 46f), new Vector2(640f, 56f));
        Button startGameButton = CreateButton(mainPanel.transform, "StartGameButton", "START GAME", new Vector2(0f, -120f), new Vector2(430f, 92f));

        ConfigureMenuFlow(
            flow,
            mainPage,
            null,
            startGameButton,
            null,
            null,
            null,
            null,
            null,
            KartMenuInitialPage.MainMenu,
            startGameLoadsConfigScene: true,
            startRaceLoadsRaceScene: true);

        return SavePrefab(root, $"{PrefabFolder}/PF_KartEntryMenuUI.prefab");
    }

    private static GameObject CreateVehicleConfigUiPrefab(Sprite backgroundSprite)
    {
        GameObject root = CreateMenuCanvas(
            "PF_KartVehicleConfigUI",
            backgroundSprite,
            includeBackdrop: false,
            out KartMenuFlow flow,
            out Transform contentRoot);

        GameObject tirePage = CreatePage(contentRoot, "TireSelectPage");

        Image topPanel = CreatePanel(tirePage.transform, "TopBar", new Vector2(0f, 826f), new Vector2(940f, 118f));
        CreateText(topPanel.transform, "Title", "GARAGE", 46, FontStyle.Bold, new Vector2(0f, 14f), new Vector2(420f, 62f));
        CreateText(topPanel.transform, "Coins", "1250", 28, FontStyle.Bold, new Vector2(348f, 10f), new Vector2(150f, 50f));

        Image statsPanel = CreatePanel(tirePage.transform, "StatsPanel", new Vector2(310f, 180f), new Vector2(250f, 340f));
        CreateText(statsPanel.transform, "StatsTitle", "TIRE FEEL", 24, FontStyle.Bold, new Vector2(0f, 126f), new Vector2(210f, 36f));
        CreateStatBar(statsPanel.transform, "SpeedStat", "SPEED", 0.72f, new Vector2(0f, 60f));
        CreateStatBar(statsPanel.transform, "GripStat", "GRIP", 0.86f, new Vector2(0f, 0f));
        CreateStatBar(statsPanel.transform, "DriftStat", "DRIFT", 0.58f, new Vector2(0f, -60f));

        Image tirePanel = CreatePanel(tirePage.transform, "TirePanel", new Vector2(0f, -652f), new Vector2(940f, 540f));
        Text selectedText = CreateText(tirePanel.transform, "SelectedTireText", "Selected: Medium", 30, FontStyle.Bold, new Vector2(0f, 196f), new Vector2(820f, 48f));
        Button smallButton = CreateButton(tirePanel.transform, "SmallTireButton", "SMALL", new Vector2(-282f, 82f), new Vector2(244f, 118f));
        Button mediumButton = CreateButton(tirePanel.transform, "MediumTireButton", "MEDIUM", new Vector2(0f, 82f), new Vector2(244f, 118f));
        Button largeButton = CreateButton(tirePanel.transform, "LargeTireButton", "LARGE", new Vector2(282f, 82f), new Vector2(244f, 118f));
        Button startRaceButton = CreateButton(tirePanel.transform, "StartRaceButton", "START RACE", new Vector2(0f, -126f), new Vector2(520f, 96f));

        ConfigureMenuFlow(
            flow,
            null,
            tirePage,
            null,
            startRaceButton,
            smallButton,
            mediumButton,
            largeButton,
            selectedText,
            KartMenuInitialPage.TireSelect,
            startGameLoadsConfigScene: true,
            startRaceLoadsRaceScene: true);

        return SavePrefab(root, $"{PrefabFolder}/PF_KartVehicleConfigUI.prefab");
    }

    private static GameObject CreateMenuCanvas(
        string name,
        Sprite backgroundSprite,
        bool includeBackdrop,
        out KartMenuFlow flow,
        out Transform contentRoot)
    {
        GameObject root = new GameObject(name);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();
        flow = root.AddComponent<KartMenuFlow>();

        GameObject frame = new GameObject("AspectFrame", typeof(RectTransform), typeof(AspectRatioFitter));
        frame.transform.SetParent(root.transform, false);
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        Stretch(frameRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        AspectRatioFitter aspectFitter = frame.GetComponent<AspectRatioFitter>();
        aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspectFitter.aspectRatio = 9f / 16f;
        contentRoot = frame.transform;

        if (includeBackdrop)
        {
            Image background = CreateImage(contentRoot, "Background", backgroundSprite, Color.white);
            Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image shade = CreateImage(contentRoot, "BackgroundShade", null, new Color(0f, 0f, 0f, 0.34f));
            Stretch(shade.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        return root;
    }

    private static void ConfigureMenuFlow(
        KartMenuFlow flow,
        GameObject mainPage,
        GameObject tirePage,
        Button startGameButton,
        Button startRaceButton,
        Button smallButton,
        Button mediumButton,
        Button largeButton,
        Text selectedText,
        KartMenuInitialPage initialPage,
        bool startGameLoadsConfigScene,
        bool startRaceLoadsRaceScene)
    {
        SerializedObject flowObject = new SerializedObject(flow);
        flowObject.FindProperty("mainPage").objectReferenceValue = mainPage;
        flowObject.FindProperty("tireSelectPage").objectReferenceValue = tirePage;
        flowObject.FindProperty("startGameButton").objectReferenceValue = startGameButton;
        flowObject.FindProperty("startRaceButton").objectReferenceValue = startRaceButton;
        flowObject.FindProperty("smallTireButton").objectReferenceValue = smallButton;
        flowObject.FindProperty("mediumTireButton").objectReferenceValue = mediumButton;
        flowObject.FindProperty("largeTireButton").objectReferenceValue = largeButton;
        flowObject.FindProperty("selectedTireText").objectReferenceValue = selectedText;
        flowObject.FindProperty("initialPage").enumValueIndex = (int)initialPage;
        flowObject.FindProperty("startGameLoadsConfigScene").boolValue = startGameLoadsConfigScene;
        flowObject.FindProperty("startRaceLoadsRaceScene").boolValue = startRaceLoadsRaceScene;
        flowObject.FindProperty("vehicleConfigSceneName").stringValue = Path.GetFileNameWithoutExtension(VehicleConfigScenePath);
        flowObject.FindProperty("raceSceneName").stringValue = Path.GetFileNameWithoutExtension(FormalGameScenePath);
        flowObject.FindProperty("selectedTireSize").enumValueIndex = (int)KartTireSize.Medium;
        flowObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreatePage(Transform parent, string name)
    {
        GameObject page = new GameObject(name, typeof(RectTransform));
        page.transform.SetParent(parent, false);
        RectTransform rect = page.GetComponent<RectTransform>();
        Stretch(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return page;
    }

    private static Image CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        Image image = CreateImage(parent, name, null, new Color(0f, 0f, 0f, 0.68f));
        image.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        image.rectTransform.anchoredPosition = anchoredPosition;
        image.rectTransform.sizeDelta = size;
        return image;
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(Transform parent, string name, string text, int fontSize, FontStyle style, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text label = textObject.GetComponent<Text>();
        label.text = text;
        label.font = GetUiFont();
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 0.86f, 0.12f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 0.86f, 0.12f, 0.95f);
        colors.highlightedColor = new Color(1f, 0.96f, 0.35f, 1f);
        colors.pressedColor = new Color(0.95f, 0.56f, 0.05f, 1f);
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.78f);
        button.colors = colors;

        Text text = CreateText(buttonObject.transform, "Label", label, 22, FontStyle.Bold, Vector2.zero, size);
        text.color = new Color(0.08f, 0.07f, 0.03f, 1f);
        return button;
    }

    private static void CreateStatBar(Transform parent, string name, string label, float fillAmount, Vector2 anchoredPosition)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(210f, 44f);

        CreateText(root.transform, "Label", label, 18, FontStyle.Bold, new Vector2(-68f, 10f), new Vector2(80f, 28f));

        Image track = CreateImage(root.transform, "Track", null, new Color(1f, 1f, 1f, 0.24f));
        track.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        track.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        track.rectTransform.pivot = new Vector2(0f, 0.5f);
        track.rectTransform.anchoredPosition = new Vector2(-20f, 7f);
        track.rectTransform.sizeDelta = new Vector2(142f, 14f);

        Image fill = CreateImage(root.transform, "Fill", null, new Color(0.1f, 0.9f, 1f, 0.94f));
        fill.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        fill.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        fill.rectTransform.anchoredPosition = new Vector2(-20f, 7f);
        fill.rectTransform.sizeDelta = new Vector2(142f * Mathf.Clamp01(fillAmount), 14f);
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static Font GetUiFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static GameObject CreateTirePrefab(
        string prefabName,
        KartTireSize size,
        float radius,
        float width,
        Material tireMaterial,
        Material hubMaterial)
    {
        GameObject root = new GameObject(prefabName);
        KartTire tireMetadata = root.AddComponent<KartTire>();
        tireMetadata.Configure(size, radius);

        GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tire.name = "Tire";
        tire.transform.SetParent(root.transform, false);
        tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        tire.transform.localScale = new Vector3(radius * 2f, width * 0.5f, radius * 2f);
        tire.GetComponent<Renderer>().sharedMaterial = tireMaterial;
        Object.DestroyImmediate(tire.GetComponent<Collider>());

        GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hub.name = "Hub";
        hub.transform.SetParent(root.transform, false);
        hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        hub.transform.localScale = new Vector3(radius * 1.05f, width * 0.56f, radius * 1.05f);
        hub.GetComponent<Renderer>().sharedMaterial = hubMaterial;
        Object.DestroyImmediate(hub.GetComponent<Collider>());

        return SavePrefab(root, $"{PrefabFolder}/{prefabName}.prefab");
    }

    private static GameObject CreateKartPrefab(
        KartDriveConfig config,
        Material bodyMaterial,
        Material accentMaterial,
        GameObject smallTirePrefab,
        GameObject mediumTirePrefab,
        GameObject largeTirePrefab)
    {
        GameObject root = new GameObject("PF_ArcadeKart");
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = config.mass;
        rb.centerOfMass = config.centerOfMass;

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.55f, 0f);
        collider.size = new Vector3(1.5f, 0.95f, 2.2f);

        Transform cameraTarget = new GameObject("CameraTarget").transform;
        cameraTarget.SetParent(root.transform, false);
        cameraTarget.localPosition = new Vector3(0f, 1.05f, -0.25f);

        bool usesImportedVisual = TryAddImportedKartVisual(root.transform);
        if (!usesImportedVisual)
        {
            CreateKartBlock(root.transform, "Body", new Vector3(0f, 0.55f, 0f), new Vector3(1.55f, 0.45f, 2.15f), bodyMaterial);
            CreateKartBlock(root.transform, "Nose", new Vector3(0f, 0.68f, 0.78f), new Vector3(1.1f, 0.32f, 0.95f), accentMaterial);
            CreateKartBlock(root.transform, "Seat", new Vector3(0f, 0.96f, -0.45f), new Vector3(0.8f, 0.42f, 0.7f), bodyMaterial);
        }

        Transform fl = CreateWheelSocket(root.transform, "WheelSocket_FL", new Vector3(-0.88f, 0.35f, 0.72f));
        Transform fr = CreateWheelSocket(root.transform, "WheelSocket_FR", new Vector3(0.88f, 0.35f, 0.72f));
        Transform rl = CreateWheelSocket(root.transform, "WheelSocket_RL", new Vector3(-0.88f, 0.35f, -0.72f));
        Transform rr = CreateWheelSocket(root.transform, "WheelSocket_RR", new Vector3(0.88f, 0.35f, -0.72f));

        KartController controller = root.AddComponent<KartController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("driveConfig").objectReferenceValue = config;
        serialized.FindProperty("body").objectReferenceValue = rb;
        serialized.FindProperty("cameraTarget").objectReferenceValue = cameraTarget;
        serialized.FindProperty("inputEnabled").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        KartWheelSet wheelSet = root.AddComponent<KartWheelSet>();
        wheelSet.Configure(controller, fl, fr, rl, rr, smallTirePrefab, mediumTirePrefab, largeTirePrefab);
        SerializedObject wheelSetObject = new SerializedObject(wheelSet);
        wheelSetObject.FindProperty("showManagedTireVisuals").boolValue = !usesImportedVisual;
        wheelSetObject.ApplyModifiedPropertiesWithoutUndo();
        wheelSet.ApplyWheelSize(KartTireSize.Medium);
        TryAddExhaustSmoke(root.transform);
        TryAddDriftFx(root, controller, rb, rl, rr);

        return SavePrefab(root, $"{PrefabFolder}/PF_ArcadeKart.prefab");
    }

    private static bool TryAddImportedKartVisual(Transform parent)
    {
        GameObject importedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedKartVisualPath);
        if (importedPrefab == null)
        {
            return false;
        }

        GameObject visual = PrefabUtility.InstantiatePrefab(importedPrefab) as GameObject;
        if (visual == null)
        {
            return false;
        }

        visual.name = "ImportedBlenderCarVisual";
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * 2.2f;
        return true;
    }

    private static bool TryAddExhaustSmoke(Transform parent)
    {
        GameObject smokePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExhaustSmokePrefabPath);
        if (smokePrefab == null)
        {
            Debug.LogWarning($"Kart exhaust smoke prefab not found at {ExhaustSmokePrefabPath}; skipping exhaust effect.");
            return false;
        }

        GameObject smoke = PrefabUtility.InstantiatePrefab(smokePrefab) as GameObject;
        if (smoke == null)
        {
            Debug.LogWarning($"Failed to instantiate kart exhaust smoke prefab at {ExhaustSmokePrefabPath}.");
            return false;
        }

        smoke.name = "PF_KartExhaustSmoke";
        smoke.transform.SetParent(parent, false);
        smoke.transform.localPosition = new Vector3(0f, 0.45f, -1.25f);
        smoke.transform.localRotation = Quaternion.identity;
        smoke.transform.localScale = Vector3.one;
        return true;
    }

    private static bool TryAddDriftFx(GameObject root, KartController controller, Rigidbody body, Transform rearLeft, Transform rearRight)
    {
        if (root == null)
        {
            return false;
        }

        KartDriftFxBuilder.EnsureDriftFxAssets();
        KartDriftFxController driftFx = root.GetComponent<KartDriftFxController>();
        if (driftFx == null)
        {
            driftFx = root.AddComponent<KartDriftFxController>();
        }

        KartDriftFxBuilder.ConfigureComponent(driftFx, controller, body, rearLeft, rearRight);

        KartDriftFxBuilder.InstallMiniBoostFxInstance(root.transform, controller, body);
        return true;
    }

    private static void CreateKartBlock(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = localPosition;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(block.GetComponent<Collider>());
    }

    private static Transform CreateWheelSocket(Transform parent, string name, Vector3 localPosition)
    {
        Transform socket = new GameObject(name).transform;
        socket.SetParent(parent, false);
        socket.localPosition = localPosition;
        return socket;
    }

    private static void BuildEntryScene(GameObject entryMenuUiPrefab)
    {
        EnsureSceneLoaded(EntryScenePath);
        ClearKartSceneObjects();

        CreateLetterboxCamera();
        PlacePrefab(entryMenuUiPrefab, "Kart_EntryMenuUI", Vector3.zero, Quaternion.identity, Vector3.one);
        CreateStaticCamera("Kart_EntryCamera", new Vector3(0f, 2.5f, -8f), Quaternion.Euler(15f, 0f, 0f), 60f);
        CreateEventSystem();
        CreateLight();
        SaveActiveScene();
    }

    private static void BuildVehicleConfigScene(GameObject kartPrefab, GameObject configMenuUiPrefab, Material displayMaterial)
    {
        EnsureSceneLoaded(VehicleConfigScenePath);
        ClearKartSceneObjects();

        CreateLetterboxCamera();
        Material floorMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Kart_ConfigFloor.mat", new Color(0.12f, 0.14f, 0.18f));
        Material wallMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Kart_ConfigWall.mat", new Color(0.22f, 0.28f, 0.35f));
        Material trimMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Kart_ConfigTrim.mat", new Color(0.04f, 0.82f, 0.92f));
        CreateGarageBackdrop(floorMaterial, wallMaterial, trimMaterial);
        CreateDisplayPad(displayMaterial, trimMaterial);

        GameObject previewKart = PlacePrefab(kartPrefab, "Kart_ConfigPreviewKart", new Vector3(0f, 0.78f, 0f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.1f);
        KartController kart = previewKart.GetComponent<KartController>();
        KartWheelSet wheelSet = previewKart.GetComponent<KartWheelSet>();
        wheelSet?.ApplyWheelSize(KartTireSize.Medium);

        GameObject cameraObject = CreateStaticCamera("Kart_ConfigCamera", new Vector3(0f, 2.55f, -8.8f), Quaternion.identity, 45f);
        cameraObject.transform.LookAt(previewKart.transform.position + Vector3.up * 0.72f);

        GameObject menuUi = PlacePrefab(configMenuUiPrefab, "Kart_VehicleConfigUI", Vector3.zero, Quaternion.identity, Vector3.one);
        KartMenuFlow menuFlow = menuUi.GetComponent<KartMenuFlow>();
        SerializedObject menuObject = new SerializedObject(menuFlow);
        menuObject.FindProperty("playerKart").objectReferenceValue = kart;
        menuObject.FindProperty("wheelSet").objectReferenceValue = wheelSet;
        menuObject.FindProperty("selectedTireSize").enumValueIndex = (int)KartTireSize.Medium;
        menuObject.ApplyModifiedPropertiesWithoutUndo();

        CreateEventSystem();
        CreateLight();
        SaveActiveScene();
    }

    private static void BuildFormalGameScene(
        GameObject groundPrefab,
        GameObject straightPrefab,
        GameObject cornerPrefab,
        GameObject boostPadPrefab,
        GameObject itemBoxPrefab,
        GameObject checkpointPrefab,
        GameObject kartPrefab)
    {
        EnsureSceneLoaded(FormalGameScenePath);
        ClearKartSceneObjects();

        CreateLetterboxCamera();
        PlacePrefab(groundPrefab, "Kart_Ground", Vector3.zero, Quaternion.identity, Vector3.one);

        PlacePrefab(straightPrefab, "Kart_Road_StartStraight", new Vector3(0f, 0f, 0f), Quaternion.identity, new Vector3(1.15f, 1f, 3.8f));
        PlacePrefab(cornerPrefab, "Kart_Road_NorthWestCorner", new Vector3(-7f, 0f, 26f), Quaternion.identity, Vector3.one);
        PlacePrefab(straightPrefab, "Kart_Road_NorthStraight", new Vector3(20f, 0f, 26f), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.15f, 1f, 3.7f));
        PlacePrefab(cornerPrefab, "Kart_Road_NorthEastCorner", new Vector3(47f, 0f, 20f), Quaternion.identity, Vector3.one);
        PlacePrefab(straightPrefab, "Kart_Road_EastDrop", new Vector3(47f, 0f, -6f), Quaternion.identity, new Vector3(1.15f, 1f, 3.5f));
        PlacePrefab(cornerPrefab, "Kart_Road_SouthEastCorner", new Vector3(41f, 0f, -34f), Quaternion.identity, Vector3.one);
        PlacePrefab(straightPrefab, "Kart_Road_SouthStraight", new Vector3(12f, 0f, -34f), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.15f, 1f, 4.7f));
        PlacePrefab(cornerPrefab, "Kart_Road_SouthWestCorner", new Vector3(-28f, 0f, -28f), Quaternion.identity, Vector3.one);
        PlacePrefab(straightPrefab, "Kart_Road_WestClimb", new Vector3(-34f, 0f, -4f), Quaternion.identity, new Vector3(1.15f, 1f, 3.5f));
        PlacePrefab(cornerPrefab, "Kart_Road_WestReturnCorner", new Vector3(-24f, 0f, 16f), Quaternion.identity, Vector3.one);
        PlacePrefab(straightPrefab, "Kart_Road_ReturnStraight", new Vector3(-10f, 0f, 16f), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.15f, 1f, 2.2f));

        PlacePrefab(boostPadPrefab, "Kart_Boost_Start", new Vector3(0f, 0.12f, 8f), Quaternion.identity, Vector3.one);
        PlacePrefab(boostPadPrefab, "Kart_Boost_North", new Vector3(18f, 0.12f, 26f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        PlacePrefab(boostPadPrefab, "Kart_Boost_South", new Vector3(8f, 0.12f, -34f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);

        PlaceItemBoxes(itemBoxPrefab);
        PlaceCheckpoints(checkpointPrefab);

        GameObject player = PlacePrefab(kartPrefab, "Kart_Player", new Vector3(0f, 0.75f, -13f), Quaternion.identity, Vector3.one);
        KartController kart = player.GetComponent<KartController>();
        KartWheelSet wheelSet = player.GetComponent<KartWheelSet>();

        GameObject cameraObject = new GameObject("Kart_MainCamera", typeof(Camera), typeof(AudioListener), typeof(KartFollowCamera));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 6f, -22f), Quaternion.Euler(22f, 0f, 0f));
        cameraObject.GetComponent<Camera>().fieldOfView = 110f;
        cameraObject.AddComponent<KartFixedAspectCamera>();
        KartFollowCamera followCamera = cameraObject.GetComponent<KartFollowCamera>();
        followCamera.Target = kart != null ? kart.CameraTarget : player.transform;
        SerializedObject followObject = new SerializedObject(followCamera);
        followObject.FindProperty("speedFovKphRange").vector2Value = new Vector2(125f, 250f);
        followObject.FindProperty("fovRange").vector2Value = new Vector2(110f, 155f);
        followObject.FindProperty("fovSharpness").floatValue = 8f;
        followObject.ApplyModifiedPropertiesWithoutUndo();

        GameObject raceSystem = new GameObject("Kart_RaceSystem");
        KartRaceManager raceManager = raceSystem.AddComponent<KartRaceManager>();
        KartHud hud = raceSystem.AddComponent<KartHud>();
        SerializedObject managerObject = new SerializedObject(raceManager);
        managerObject.FindProperty("playerKart").objectReferenceValue = kart;
        managerObject.FindProperty("checkpointCount").intValue = 4;
        managerObject.FindProperty("targetLaps").intValue = 3;
        managerObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject hudObject = new SerializedObject(hud);
        hudObject.FindProperty("raceManager").objectReferenceValue = raceManager;
        hudObject.FindProperty("playerKart").objectReferenceValue = kart;
        hudObject.ApplyModifiedPropertiesWithoutUndo();
        hud.enabled = false;

        KartRaceSceneBootstrap bootstrap = raceSystem.AddComponent<KartRaceSceneBootstrap>();
        SerializedObject bootstrapObject = new SerializedObject(bootstrap);
        bootstrapObject.FindProperty("playerKart").objectReferenceValue = kart;
        bootstrapObject.FindProperty("wheelSet").objectReferenceValue = wheelSet;
        bootstrapObject.FindProperty("hud").objectReferenceValue = hud;
        bootstrapObject.FindProperty("enableLegacyHud").boolValue = false;
        bootstrapObject.ApplyModifiedPropertiesWithoutUndo();

        CreateSpeedDashboardUi(kart, cameraObject.GetComponent<Camera>());
        CreateEventSystem();
        CreateLight();
        SaveActiveScene();
    }

    private static void CreateSpeedDashboardUi(KartController playerKart, Camera uiCamera)
    {
        GameObject dashboardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpeedDashboardPrefabPath);
        if (dashboardPrefab == null)
        {
            Debug.LogWarning($"Speed dashboard prefab not found at {SpeedDashboardPrefabPath}.");
            return;
        }

        GameObject canvasObject = new GameObject("Kart_DashboardCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = uiCamera;
        canvas.planeDistance = 1f;
        canvas.sortingOrder = 35;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject dashboard = PrefabUtility.InstantiatePrefab(dashboardPrefab, canvasObject.transform) as GameObject;
        if (dashboard == null)
        {
            dashboard = Object.Instantiate(dashboardPrefab, canvasObject.transform);
        }

        dashboard.name = "SpeedDashboardGauge";
        RectTransform dashboardRect = dashboard.GetComponent<RectTransform>();
        dashboardRect.anchorMin = new Vector2(0.5f, 0f);
        dashboardRect.anchorMax = new Vector2(0.5f, 0f);
        dashboardRect.pivot = new Vector2(0.5f, 0f);
        dashboardRect.anchoredPosition = new Vector2(-150f, 34f);
        dashboardRect.sizeDelta = new Vector2(600f, 184f);
        dashboardRect.localScale = Vector3.one;

        SpeedDashboardGaugeUi dashboardUi = dashboard.GetComponent<SpeedDashboardGaugeUi>();
        if (dashboardUi != null)
        {
            SerializedObject dashboardObject = new SerializedObject(dashboardUi);
            dashboardObject.FindProperty("playerKart").objectReferenceValue = playerKart;
            dashboardObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void CreateTopSpeedDisplay(Transform parent, KartController playerKart)
    {
        GameObject panelObject = new GameObject("TopSpeedDisplay", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -54f);
        panelRect.sizeDelta = new Vector2(260f, 74f);

        Image panel = panelObject.GetComponent<Image>();
        panel.color = new Color(0f, 0f, 0f, 0.54f);
        panel.raycastTarget = false;

        GameObject textObject = new GameObject("SpeedText", typeof(RectTransform), typeof(Text), typeof(Outline), typeof(KartTopSpeedUi));
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.text = "000 KM/H";
        text.font = GetUiFont();
        text.fontSize = 42;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.72f);
        outline.effectDistance = new Vector2(2f, -2f);

        KartTopSpeedUi speedUi = textObject.GetComponent<KartTopSpeedUi>();
        SerializedObject speedObject = new SerializedObject(speedUi);
        speedObject.FindProperty("playerKart").objectReferenceValue = playerKart;
        speedObject.FindProperty("speedText").objectReferenceValue = text;
        speedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateStaticCamera(string name, Vector3 position, Quaternion rotation, float fieldOfView)
    {
        GameObject cameraObject = new GameObject(name, typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetPositionAndRotation(position, rotation);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.fieldOfView = fieldOfView;
        camera.depth = 0f;
        cameraObject.AddComponent<KartFixedAspectCamera>();
        return cameraObject;
    }

    private static void CreateLetterboxCamera()
    {
        GameObject cameraObject = new GameObject("Kart_LetterboxCamera", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
        camera.depth = -100f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 1f;
    }

    private static void CreateGarageBackdrop(Material floorMaterial, Material wallMaterial, Material trimMaterial)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Kart_ConfigGarageFloor";
        floor.transform.position = new Vector3(0f, -0.08f, 1.5f);
        floor.transform.localScale = new Vector3(10f, 0.1f, 12f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

        GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backWall.name = "Kart_ConfigGarageBackWall";
        backWall.transform.position = new Vector3(0f, 2.2f, 3.8f);
        backWall.transform.localScale = new Vector3(10f, 4.6f, 0.16f);
        backWall.GetComponent<Renderer>().sharedMaterial = wallMaterial;

        GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trim.name = "Kart_ConfigGarageTrim";
        trim.transform.position = new Vector3(0f, 1.1f, 3.68f);
        trim.transform.localScale = new Vector3(7.5f, 0.08f, 0.08f);
        trim.GetComponent<Renderer>().sharedMaterial = trimMaterial;
        Object.DestroyImmediate(trim.GetComponent<Collider>());
    }

    private static void CreateDisplayPad(Material platformMaterial, Material trimMaterial)
    {
        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        glow.name = "Kart_ConfigDisplayRing";
        glow.transform.position = new Vector3(0f, 0.005f, 0f);
        glow.transform.localScale = new Vector3(3.6f, 0.025f, 3.6f);
        glow.GetComponent<Renderer>().sharedMaterial = trimMaterial;
        Object.DestroyImmediate(glow.GetComponent<Collider>());

        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "Kart_ConfigDisplayPad";
        pad.transform.position = new Vector3(0f, 0.045f, 0f);
        pad.transform.localScale = new Vector3(3.15f, 0.06f, 3.15f);
        pad.GetComponent<Renderer>().sharedMaterial = platformMaterial;
        Object.DestroyImmediate(pad.GetComponent<Collider>());
    }

    private static void PlaceItemBoxes(GameObject itemBoxPrefab)
    {
        Vector3[] positions =
        {
            new Vector3(-2.2f, 0f, 18f),
            new Vector3(0f, 0f, 18f),
            new Vector3(2.2f, 0f, 18f),
            new Vector3(41f, 0f, 8f),
            new Vector3(45f, 0f, 8f),
            new Vector3(-18f, 0f, -30f),
            new Vector3(-14f, 0f, -30f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            PlacePrefab(itemBoxPrefab, $"Kart_ItemBox_{i + 1}", positions[i], Quaternion.identity, Vector3.one);
        }
    }

    private static void PlaceCheckpoints(GameObject checkpointPrefab)
    {
        PlaceCheckpoint(checkpointPrefab, "Kart_Checkpoint_Start", 0, true, new Vector3(0f, 0f, -16f), Quaternion.identity);
        PlaceCheckpoint(checkpointPrefab, "Kart_Checkpoint_North", 1, false, new Vector3(24f, 0f, 26f), Quaternion.Euler(0f, 90f, 0f));
        PlaceCheckpoint(checkpointPrefab, "Kart_Checkpoint_East", 2, false, new Vector3(47f, 0f, -16f), Quaternion.identity);
        PlaceCheckpoint(checkpointPrefab, "Kart_Checkpoint_South", 3, false, new Vector3(0f, 0f, -34f), Quaternion.Euler(0f, 90f, 0f));
    }

    private static void PlaceCheckpoint(GameObject prefab, string name, int index, bool startLine, Vector3 position, Quaternion rotation)
    {
        GameObject checkpoint = PlacePrefab(prefab, name, position, rotation, Vector3.one);
        KartCheckpoint component = checkpoint.GetComponent<KartCheckpoint>();
        if (component != null)
        {
            component.Configure(index, startLine);
            EditorUtility.SetDirty(component);
        }
    }

    private static GameObject PlacePrefab(GameObject prefab, string name, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.transform.localScale = scale;
        return instance;
    }

    private static void EnsureSceneLoaded(string scenePath)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path == scenePath)
        {
            return;
        }

        if (File.Exists(scenePath))
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            return;
        }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
    }

    private static void SaveActiveScene()
    {
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    private static void UpdateBuildSettings()
    {
        string[] kartScenes =
        {
            EntryScenePath,
            VehicleConfigScenePath,
            FormalGameScenePath
        };

        HashSet<string> sceneSet = new HashSet<string>(kartScenes);
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        for (int i = 0; i < kartScenes.Length; i++)
        {
            scenes.Add(new EditorBuildSettingsScene(kartScenes[i], true));
        }

        EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
        for (int i = 0; i < existingScenes.Length; i++)
        {
            string path = existingScenes[i].path;
            if (sceneSet.Contains(path) || path == LegacyCombinedScenePath)
            {
                continue;
            }

            scenes.Add(existingScenes[i]);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void ClearKartSceneObjects()
    {
        GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null)
            {
                continue;
            }

            string objectName = objects[i].name;
            if (objectName.StartsWith("Kart_"))
            {
                Object.DestroyImmediate(objects[i]);
            }
        }
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("Kart_EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();
        eventSystem.firstSelectedGameObject = null;
    }

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Kart_DirectionalLight", typeof(Light));
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 2.4f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
    }

    private static KartDriveConfig GetOrCreateDriveConfig()
    {
        KartDriveConfig config = AssetDatabase.LoadAssetAtPath<KartDriveConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<KartDriveConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }

        config.mass = 420f;
        config.centerOfMass = new Vector3(0f, -0.45f, 0.05f);
        config.linearDrag = 0.12f;
        config.angularDrag = 2.8f;
        config.accelerationForce = 20000f;
        config.reverseForce = 8000f;
        config.maxSpeed = 41.666668f;
        config.maxReverseSpeed = 13.888889f;
        config.brakeForce = 8500f;
        config.lowSpeedTurnRate = 120f;
        config.highSpeedTurnRate = 58f;
        config.reverseSteerScale = 0.45f;
        config.driftMinSpeed = 8f;
        config.driftSteerThreshold = 0.28f;
        config.driftGripScale = 0.62f;
        config.driftTurnMultiplier = 1.18f;
        config.driftCounterSteerMultiplier = 0.95f;
        config.driftEntrySideImpulse = 1.4f;
        config.driftEntryYawKick = 3f;
        config.driftRecoveryGripMultiplier = 1.15f;
        config.driftRecoveryDuration = 0.25f;
        config.driftMiniBoostMinDuration = 0.5f;
        config.driftMiniBoostSpeedGain = 2.5f;
        config.driftMiniBoostDuration = 0.28f;
        config.lateralGrip = 10f;
        config.downforce = 0f;
        config.airborneGravityMultiplier = 2f;
        config.uprightStability = 0.35f;
        config.boostSpeedMultiplier = 1.35f;
        config.boostMaxSpeedMultiplier = 3f;
        config.boostOverspeedReturnRate = 35f;
        config.boostForce = 9000f;
        config.boostDuration = 1.25f;
        config.enableScreenSpeedLines = true;
        config.screenSpeedLineVisibleAtSpeedKph = 45f;
        config.screenSpeedLineFullAtSpeedKph = 150f;
        config.screenSpeedLineIntensitySmoothTime = 0.12f;
        config.screenSpeedLineMinCount = 5;
        config.screenSpeedLineMaxCount = 54;
        config.screenSpeedLineMinLength = 180f;
        config.screenSpeedLineMaxLength = 620f;
        config.screenSpeedLineMinWidth = 5f;
        config.screenSpeedLineMaxWidth = 24f;
        config.screenSpeedLineCenterClearRadius = 0.34f;
        config.screenSpeedLineColor = new Color(1f, 1f, 1f, 0.82f);
        config.screenSpeedLineFlowSpeed = 1.35f;
        config.useScreenSpeedLinePreview = false;
        config.screenSpeedLinePreviewSpeedKph = 150f;
        EditorUtility.SetDirty(config);
        return config;
    }

    private static Material GetOrCreateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureGrassMaterial(Material material)
    {
        Texture2D grassTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassTexturePath);
        if (grassTexture == null)
        {
            grassTexture = CreateGrassTexture(GrassTexturePath);
        }

        if (grassTexture == null)
        {
            return;
        }

        Color grassColor = new Color(0.22f, 0.56f, 0.18f, 1f);
        material.SetColor("_BaseColor", grassColor);
        material.SetColor("_Color", grassColor);
        material.SetTexture("_BaseMap", grassTexture);
        material.SetTexture("_MainTex", grassTexture);
        material.SetTextureScale("_BaseMap", new Vector2(28f, 23f));
        material.SetTextureScale("_MainTex", new Vector2(28f, 23f));
        material.SetFloat("_Smoothness", 0.12f);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
    }

    private static Texture2D CreateGrassTexture(string path)
    {
        const int size = 512;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color dark = new Color(0.10f, 0.34f, 0.09f, 1f);
        Color mid = new Color(0.24f, 0.56f, 0.18f, 1f);
        Color light = new Color(0.58f, 0.82f, 0.30f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float broad = Mathf.PerlinNoise(x * 0.018f + 8.5f, y * 0.018f + 3.1f);
                float noise = Mathf.PerlinNoise(x * 0.052f, y * 0.052f);
                float fine = Mathf.PerlinNoise(x * 0.22f + 37.6f, y * 0.22f + 11.2f);
                Color color = Color.Lerp(dark, mid, Mathf.Clamp01(noise * 0.9f + broad * 0.25f));
                color = Color.Lerp(color, light, Mathf.Clamp01((fine - 0.55f) * 1.5f));
                texture.SetPixel(x, y, color);
            }
        }

        DrawGrassBlades(texture, size);
        texture.Apply();

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void DrawGrassBlades(Texture2D texture, int size)
    {
        Color blade = new Color(0.68f, 0.92f, 0.34f, 1f);
        Color shadow = new Color(0.04f, 0.24f, 0.05f, 1f);

        for (int i = 0; i < 2200; i++)
        {
            unchecked
            {
                int seed = i * 1103515245 + 12345;
                int x = Mathf.Abs(seed) % size;
                int y = Mathf.Abs(seed / 9973) % size;
                int length = 6 + Mathf.Abs(seed / 31) % 18;
                int lean = (seed % 9) - 4;

                for (int step = 0; step < length; step++)
                {
                    int px = (x + (lean * step) / length + size) % size;
                    int py = (y + step) % size;
                    Color current = texture.GetPixel(px, py);
                    Color tint = step % 4 == 0 ? blade : shadow;
                    texture.SetPixel(px, py, Color.Lerp(current, tint, 0.42f));
                    if (step % 5 == 0)
                    {
                        int side = (px + 1) % size;
                        texture.SetPixel(side, py, Color.Lerp(texture.GetPixel(side, py), blade, 0.18f));
                    }
                }
            }
        }
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }
}
