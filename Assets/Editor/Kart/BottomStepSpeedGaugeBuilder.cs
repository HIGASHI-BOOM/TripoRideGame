using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BottomStepSpeedGaugeBuilder
{
    private const string SpriteFolder = "Assets/Generated/KartDashboardUI/HudStepGaugeSprites";
    private const string PrefabPath = "Assets/Generated/KartDashboardUI/PF_SpeedDashboardGauge.prefab";

    private static readonly Vector2[] SegmentPositions =
    {
        new Vector2(-244f, 36f),
        new Vector2(-192f, 39f),
        new Vector2(-138f, 44f),
        new Vector2(-80f, 50f),
        new Vector2(-20f, 58f),
        new Vector2(44f, 67f),
        new Vector2(110f, 77f),
        new Vector2(180f, 89f),
        new Vector2(252f, 104f),
    };

    private static readonly Vector2[] SegmentSizes =
    {
        new Vector2(48f, 31f),
        new Vector2(54f, 36f),
        new Vector2(60f, 44f),
        new Vector2(66f, 52f),
        new Vector2(73f, 62f),
        new Vector2(79f, 74f),
        new Vector2(85f, 88f),
        new Vector2(92f, 104f),
        new Vector2(100f, 128f),
    };

    [MenuItem("Tools/Kart/Rebuild Bottom Step Speed Gauge")]
    public static void Rebuild()
    {
        ImportSprites();
        CreatePrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Bottom step speed gauge rebuilt from HUD sprite elements.");
    }

    private static void ImportSprites()
    {
        string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteFolder });
        for (int i = 0; i < paths.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(paths[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
    }

    private static void CreatePrefab()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        GameObject root = new GameObject("PF_SpeedDashboardGauge", typeof(RectTransform), typeof(CanvasRenderer), typeof(SpeedDashboardGaugeUi));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(600f, 184f);
        rootRect.pivot = new Vector2(0.5f, 0f);

        Image backplate = CreateImage(root.transform, "HudGlassBackplate", LoadSprite("T_HudStepGauge_Backplate"), new Vector2(600f, 178f), new Vector2(0f, 0f), new Vector2(0.5f, 0f));
        backplate.color = new Color(1f, 1f, 1f, 0.86f);

        Image yellowLight = CreateImage(root.transform, "HudYellowLight", LoadSprite("T_HudStepGauge_Light_Yellow"), new Vector2(34f, 14f), new Vector2(-250f, 48f), new Vector2(0.5f, 0.5f));
        Image redLight = CreateImage(root.transform, "HudRedLight", LoadSprite("T_HudStepGauge_Light_Red"), new Vector2(34f, 14f), new Vector2(248f, 48f), new Vector2(0.5f, 0.5f));

        Text speedText = CreateSpeedText(root.transform, font);
        Image[] segmentImages = new Image[SegmentPositions.Length];
        for (int i = 0; i < segmentImages.Length; i++)
        {
            Image segment = CreateImage(
                root.transform,
                string.Format("HudSpeedBlock_{0:00}", i + 1),
                LoadSprite(string.Format("T_HudStepGauge_Segment_{0:00}", i)),
                SegmentSizes[i],
                SegmentPositions[i],
                new Vector2(0.5f, 0f));
            segment.color = new Color(1f, 1f, 1f, 0.76f);
            segmentImages[i] = segment;
        }

        Image unit = CreateImage(root.transform, "HudUnitLabel_KMh", LoadSprite("T_HudStepGauge_Label_KMh"), new Vector2(62f, 22f), new Vector2(0f, 49f), new Vector2(0.5f, 0.5f));
        unit.color = new Color(1f, 1f, 1f, 0.92f);
        Graphic[] otherHudGraphics = { backplate, yellowLight, redLight, unit, speedText };

        SerializedObject serialized = new SerializedObject(root.GetComponent<SpeedDashboardGaugeUi>());
        serialized.FindProperty("speedText").objectReferenceValue = speedText;
        SerializedProperty segments = serialized.FindProperty("speedSegments");
        segments.arraySize = segmentImages.Length;
        for (int i = 0; i < segmentImages.Length; i++)
        {
            segments.GetArrayElementAtIndex(i).objectReferenceValue = segmentImages[i];
        }

        serialized.FindProperty("activeSegmentAlpha").floatValue = 1f;
        serialized.FindProperty("inactiveSegmentAlpha").floatValue = 0f;
        serialized.FindProperty("segmentSmoothSpeed").floatValue = 10f;
        serialized.FindProperty("otherHudAlpha").floatValue = 1f;
        SerializedProperty otherHud = serialized.FindProperty("otherHudGraphics");
        otherHud.arraySize = otherHudGraphics.Length;
        for (int i = 0; i < otherHudGraphics.Length; i++)
        {
            otherHud.GetArrayElementAtIndex(i).objectReferenceValue = otherHudGraphics[i];
        }

        serialized.FindProperty("maxSpeedKph").floatValue = 160f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
    }

    private static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "/" + name + ".png");
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position, Vector2 pivot)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = gameObject.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private static Text CreateSpeedText(Transform parent, Font font)
    {
        GameObject gameObject = new GameObject("HudSpeedNumber", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-292f, 45f);
        rect.sizeDelta = new Vector2(88f, 42f);

        Text text = gameObject.GetComponent<Text>();
        text.text = "000";
        text.font = font;
        text.fontSize = 34;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 1f, 1f, 0.92f);
        text.raycastTarget = false;

        Outline outline = gameObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.74f);
        outline.effectDistance = new Vector2(2f, -2f);
        return text;
    }
}
