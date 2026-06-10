using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public sealed class KartVegetationPainterWindow : EditorWindow
{
    private const string GeneratedRootFolder = "Assets/Generated/KartVegetation";
    private const string PrefabFolder = GeneratedRootFolder + "/Prefabs";
    private const string MaterialFolder = GeneratedRootFolder + "/Materials";
    private const string TreePrefabPath = PrefabFolder + "/PF_Veg_LowPolyTree.prefab";
    private const string BushPrefabPath = PrefabFolder + "/PF_Veg_Bush.prefab";
    private const string GrassPrefabPath = PrefabFolder + "/PF_Veg_GrassClump.prefab";
    private const string DefaultIncludeSurfaceKeywords = "Grass,Ground,Dirt,Terrain,Landscape";
    private const string DefaultExcludeSurfaceKeywords = "Road,Track,Curb,Boost,Checkpoint,Barrier,Item,Player,ArcadeKart,Guardrail";
    private const string PreviousExcludeSurfaceKeywords = "Road,Track,Curb,Boost,Checkpoint,Barrier,Item,Kart,Guardrail";
    private const string PlacementBoxName = "Vegetation_PlacementBox";

    [Serializable]
    private sealed class VegetationPrototype
    {
        public GameObject prefab;
        public float weight = 1f;
        public Vector2 scaleRange = new Vector2(0.8f, 1.25f);
        public Vector3 rotationOffsetEuler;
        public bool alignToSurfaceNormal;
    }

    [SerializeField] private GameObject placementBox;
    [SerializeField] private string generatedParentName = "Generated_KartVegetation";
    [SerializeField] private LayerMask surfaceMask = ~0;
    [SerializeField] private string includeSurfaceKeywords = DefaultIncludeSurfaceKeywords;
    [SerializeField] private string excludeSurfaceKeywords = DefaultExcludeSurfaceKeywords;
    [SerializeField] private int seed = 5147;
    [SerializeField] private float densityPer100SquareMeters = 2.4f;
    [SerializeField] private int maxInstances = 360;
    [SerializeField] private float minSpacing = 2.2f;
    [SerializeField] private float groundOffset = 0.02f;
    [SerializeField] private float maxSlopeDegrees = 32f;
    [SerializeField] private float noiseScale = 0.055f;
    [SerializeField] private float noiseThreshold = 0.18f;
    [SerializeField] private bool clearBeforeGenerate = true;
    [SerializeField] private List<VegetationPrototype> prototypes = new List<VegetationPrototype>();

    private SerializedObject serializedWindow;
    private int rejectedNoSurface;
    private int rejectedKeyword;
    private int rejectedSlope;
    private int rejectedNoise;
    private int rejectedSpacing;
    private int rejectedOutsideBox;

    [MenuItem("Tools/Kart/Procedural Vegetation Painter")]
    public static void Open()
    {
        GetWindow<KartVegetationPainterWindow>("Kart Vegetation");
    }

    [MenuItem("Tools/Kart/Create Default Vegetation Prefabs")]
    public static void CreateDefaultVegetationPrefabs()
    {
        EnsureAssetFolders();
        Material trunk = GetOrCreateMaterial(MaterialFolder + "/M_Veg_Trunk.mat", new Color(0.39f, 0.22f, 0.11f));
        Material leaf = GetOrCreateMaterial(MaterialFolder + "/M_Veg_Leaf.mat", new Color(0.12f, 0.48f, 0.17f));
        Material leafLight = GetOrCreateMaterial(MaterialFolder + "/M_Veg_LeafLight.mat", new Color(0.38f, 0.68f, 0.18f));
        Material grass = GetOrCreateMaterial(MaterialFolder + "/M_Veg_GrassBlade.mat", new Color(0.28f, 0.62f, 0.16f));

        CreateTreePrefab(trunk, leaf, leafLight);
        CreateBushPrefab(leaf, leafLight);
        CreateGrassPrefab(grass);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Default vegetation prefabs created under {PrefabFolder}");
    }

    private void OnEnable()
    {
        serializedWindow = new SerializedObject(this);
        if (excludeSurfaceKeywords == PreviousExcludeSurfaceKeywords)
        {
            excludeSurfaceKeywords = DefaultExcludeSurfaceKeywords;
        }

        SceneView.duringSceneGui += DrawPlacementBoxSceneGui;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DrawPlacementBoxSceneGui;
    }

    private void OnGUI()
    {
        if (serializedWindow == null)
        {
            serializedWindow = new SerializedObject(this);
        }

        serializedWindow.Update();

        EditorGUILayout.HelpBox(
            "Move and scale the placement box in the scene, assign prefab prototypes here, then Generate.",
            MessageType.Info);

        DrawProperty("placementBox");
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create/Select Placement Box"))
            {
                CreateOrSelectPlacementBox();
            }

            if (GUILayout.Button("Use Selected As Box"))
            {
                placementBox = Selection.activeGameObject;
            }
        }

        EditorGUILayout.Space(8f);
        DrawProperty("generatedParentName");
        DrawProperty("surfaceMask");
        DrawProperty("includeSurfaceKeywords");
        DrawProperty("excludeSurfaceKeywords");
        if (GUILayout.Button("Reset Surface Filters"))
        {
            includeSurfaceKeywords = DefaultIncludeSurfaceKeywords;
            excludeSurfaceKeywords = DefaultExcludeSurfaceKeywords;
        }

        EditorGUILayout.Space(8f);
        DrawProperty("seed");
        DrawProperty("densityPer100SquareMeters");
        DrawProperty("maxInstances");
        DrawProperty("minSpacing");
        DrawProperty("groundOffset");
        DrawProperty("maxSlopeDegrees");
        DrawProperty("noiseScale");
        DrawProperty("noiseThreshold");
        DrawProperty("clearBeforeGenerate");

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Prefab Prototypes", EditorStyles.boldLabel);
        DrawProperty("prototypes");
        if (GUILayout.Button("Add Selected Prefab"))
        {
            AddSelectedPrefabPrototype();
        }

        serializedWindow.ApplyModifiedProperties();

        EditorGUILayout.Space(12f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create Default Prefabs", GUILayout.Height(28f)))
            {
                CreateDefaultVegetationPrefabs();
                LoadDefaultPrototypesIfEmpty(true);
            }

            if (GUILayout.Button("Generate Vegetation", GUILayout.Height(28f)))
            {
                GenerateVegetation();
            }
        }

        if (GUILayout.Button("Clear Generated Vegetation"))
        {
            ClearGeneratedVegetation();
        }
    }

    private void DrawProperty(string propertyName)
    {
        EditorGUILayout.PropertyField(serializedWindow.FindProperty(propertyName), true);
    }

    private void AddSelectedPrefabPrototype()
    {
        GameObject selectedPrefab = Selection.activeObject as GameObject;
        if (selectedPrefab == null)
        {
            Debug.LogWarning("Select a prefab asset in the Project window first.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(selectedPrefab);
        if (string.IsNullOrEmpty(assetPath) || PrefabUtility.GetPrefabAssetType(selectedPrefab) == PrefabAssetType.NotAPrefab)
        {
            Debug.LogWarning("The selected object is not a prefab asset. Drag a prefab into the Prototypes list, or select one in Project and click Add Selected Prefab.");
            return;
        }

        prototypes.Add(new VegetationPrototype
        {
            prefab = selectedPrefab,
            weight = 1f,
            scaleRange = new Vector2(0.85f, 1.15f),
            rotationOffsetEuler = Vector3.zero
        });

        EditorUtility.SetDirty(this);
    }

    private void GenerateVegetation()
    {
        if (!HasUsablePrototype())
        {
            Debug.LogError("No vegetation prefabs are assigned. Add at least one prototype prefab.");
            return;
        }

        if (placementBox == null)
        {
            placementBox = GameObject.Find(PlacementBoxName);
        }

        if (placementBox == null)
        {
            Debug.LogError("No placement box assigned. Click Create/Select Placement Box first.");
            return;
        }

        Bounds bounds = GetPlacementBounds();
        if (bounds.size.x <= 0.01f || bounds.size.z <= 0.01f)
        {
            Debug.LogError("The placement box is too small. Scale it wider on X/Z.");
            return;
        }

        if (clearBeforeGenerate)
        {
            ClearGeneratedVegetation();
        }

        GameObject parent = GameObject.Find(generatedParentName);
        if (parent == null)
        {
            parent = new GameObject(generatedParentName);
            Undo.RegisterCreatedObjectUndo(parent, "Create vegetation parent");
        }

        Random.InitState(seed);
        rejectedNoSurface = 0;
        rejectedKeyword = 0;
        rejectedSlope = 0;
        rejectedNoise = 0;
        rejectedSpacing = 0;
        rejectedOutsideBox = 0;

        int desiredCount = Mathf.Clamp(
            Mathf.RoundToInt(bounds.size.x * bounds.size.z * densityPer100SquareMeters / 100f),
            0,
            maxInstances);
        int attemptBudget = Mathf.Max(64, desiredCount * 18);
        List<Vector3> placedPositions = new List<Vector3>(desiredCount);
        int placed = 0;

        for (int attempt = 0; attempt < attemptBudget && placed < desiredCount; attempt++)
        {
            Vector3 rayStart = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y + 50f,
                Random.Range(bounds.min.z, bounds.max.z));

            if (!TryFindSurface(rayStart, bounds, out RaycastHit hit))
            {
                continue;
            }

            if (Vector3.Angle(hit.normal, Vector3.up) > maxSlopeDegrees)
            {
                rejectedSlope++;
                continue;
            }

            float noise = Mathf.PerlinNoise((hit.point.x + seed) * noiseScale, (hit.point.z - seed) * noiseScale);
            if (noise < noiseThreshold)
            {
                rejectedNoise++;
                continue;
            }

            if (!HasEnoughSpacing(hit.point, placedPositions))
            {
                rejectedSpacing++;
                continue;
            }

            VegetationPrototype prototype = PickPrototype();
            if (prototype == null || prototype.prefab == null)
            {
                continue;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prototype.prefab, parent.transform) as GameObject;
            if (instance == null)
            {
                instance = Instantiate(prototype.prefab, parent.transform);
            }

            Undo.RegisterCreatedObjectUndo(instance, "Create vegetation instance");
            instance.name = prototype.prefab.name;
            Quaternion surfaceRotation = prototype.alignToSurfaceNormal
                ? Quaternion.FromToRotation(Vector3.up, hit.normal)
                : Quaternion.identity;
            Quaternion randomYaw = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Quaternion rotationOffset = Quaternion.Euler(prototype.rotationOffsetEuler);
            instance.transform.SetPositionAndRotation(
                hit.point + hit.normal * groundOffset,
                surfaceRotation * randomYaw * rotationOffset);
            float uniformScale = Random.Range(
                Mathf.Max(0.01f, prototype.scaleRange.x),
                Mathf.Max(0.01f, prototype.scaleRange.y));
            instance.transform.localScale = Vector3.one * uniformScale;

            placedPositions.Add(hit.point);
            placed++;
        }

        Selection.activeGameObject = parent;
        EditorSceneManager.MarkSceneDirty(parent.scene);
        if (placed == 0)
        {
            Debug.LogWarning(
                $"Generated 0 vegetation instances. Rejected: noSurface={rejectedNoSurface}, surfaceFilter={rejectedKeyword}, slope={rejectedSlope}, noise={rejectedNoise}, spacing={rejectedSpacing}. " +
                $"outsideBox={rejectedOutsideBox}. Check that the placement box covers a raycastable ground surface.");
        }
        else
        {
            Debug.Log($"Generated {placed} vegetation instances in {parent.name} using seed {seed}.");
        }
    }

    private bool TryFindSurface(Vector3 rayStart, Bounds bounds, out RaycastHit hit)
    {
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, bounds.size.y + 120f, surfaceMask);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit candidate in hits)
        {
            if (candidate.point.y < bounds.min.y - 0.01f || candidate.point.y > bounds.max.y + 0.01f)
            {
                continue;
            }

            if (placementBox != null && candidate.collider.transform.IsChildOf(placementBox.transform))
            {
                continue;
            }

            if (!IsInsidePlacementBox(candidate.point))
            {
                rejectedOutsideBox++;
                continue;
            }

            bool included = MatchesKeyword(candidate.collider.gameObject, includeSurfaceKeywords, true);
            bool excluded = MatchesKeyword(candidate.collider.gameObject, excludeSurfaceKeywords, false);
            if (excluded || !included)
            {
                rejectedKeyword++;
                hit = default;
                return false;
            }

            hit = candidate;
            return true;
        }

        hit = default;
        rejectedNoSurface++;
        return false;
    }

    private bool HasEnoughSpacing(Vector3 point, List<Vector3> placedPositions)
    {
        float sqrSpacing = minSpacing * minSpacing;
        for (int i = 0; i < placedPositions.Count; i++)
        {
            Vector3 delta = placedPositions[i] - point;
            delta.y = 0f;
            if (delta.sqrMagnitude < sqrSpacing)
            {
                return false;
            }
        }

        return true;
    }

    private VegetationPrototype PickPrototype()
    {
        float totalWeight = 0f;
        for (int i = 0; i < prototypes.Count; i++)
        {
            if (prototypes[i].prefab != null)
            {
                totalWeight += Mathf.Max(0f, prototypes[i].weight);
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float pick = Random.value * totalWeight;
        for (int i = 0; i < prototypes.Count; i++)
        {
            if (prototypes[i].prefab == null)
            {
                continue;
            }

            pick -= Mathf.Max(0f, prototypes[i].weight);
            if (pick <= 0f)
            {
                return prototypes[i];
            }
        }

        return prototypes[prototypes.Count - 1];
    }

    private bool HasUsablePrototype()
    {
        for (int i = 0; i < prototypes.Count; i++)
        {
            if (prototypes[i].prefab != null && prototypes[i].weight > 0f)
            {
                return true;
            }
        }

        return false;
    }

    private void ClearGeneratedVegetation()
    {
        GameObject parent = GameObject.Find(generatedParentName);
        if (parent == null)
        {
            return;
        }

        Undo.DestroyObjectImmediate(parent);
        Scene scene = SceneManagerBridge.GetActiveScene();
        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }

    private void CreateOrSelectPlacementBox()
    {
        GameObject existing = placementBox != null ? placementBox : GameObject.Find(PlacementBoxName);
        if (existing == null)
        {
            existing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            existing.name = PlacementBoxName;
            existing.transform.position = new Vector3(0f, 4f, 0f);
            existing.transform.localScale = new Vector3(80f, 8f, 70f);
            BoxCollider collider = existing.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            Renderer renderer = existing.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetOrCreatePlacementBoxMaterial();
            }

            Undo.RegisterCreatedObjectUndo(existing, "Create vegetation placement box");
        }

        placementBox = existing;
        Selection.activeGameObject = placementBox;
        EditorSceneManager.MarkSceneDirty(placementBox.scene);
    }

    private Bounds GetPlacementBounds()
    {
        BoxCollider box = placementBox.GetComponent<BoxCollider>();
        if (box != null)
        {
            return box.bounds;
        }

        return new Bounds(placementBox.transform.position, placementBox.transform.lossyScale);
    }

    private bool IsInsidePlacementBox(Vector3 worldPoint)
    {
        if (placementBox == null)
        {
            return false;
        }

        BoxCollider box = placementBox.GetComponent<BoxCollider>();
        Vector3 localPoint = placementBox.transform.InverseTransformPoint(worldPoint);
        Vector3 center = box != null ? box.center : Vector3.zero;
        Vector3 size = box != null ? box.size : Vector3.one;
        Vector3 halfSize = size * 0.5f;
        Vector3 delta = localPoint - center;
        return Mathf.Abs(delta.x) <= halfSize.x
            && Mathf.Abs(delta.y) <= halfSize.y
            && Mathf.Abs(delta.z) <= halfSize.z;
    }

    private void DrawPlacementBoxSceneGui(SceneView sceneView)
    {
        if (placementBox == null)
        {
            return;
        }

        BoxCollider box = placementBox.GetComponent<BoxCollider>();
        Vector3 center = box != null ? box.center : Vector3.zero;
        Vector3 size = box != null ? box.size : Vector3.one;
        Handles.color = new Color(0.2f, 0.85f, 0.3f, 0.9f);
        using (new Handles.DrawingScope(placementBox.transform.localToWorldMatrix))
        {
            Handles.DrawWireCube(center, size);
        }
    }

    private static bool MatchesKeyword(GameObject target, string csv, bool emptyResult)
    {
        string[] keywords = SplitKeywords(csv);
        if (keywords.Length == 0)
        {
            return emptyResult;
        }

        Transform current = target.transform;
        while (current != null)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (current.name.IndexOf(keywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            current = current.parent;
        }

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = renderer.sharedMaterial;
            if (material != null)
            {
                for (int i = 0; i < keywords.Length; i++)
                {
                    if (material.name.IndexOf(keywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static string[] SplitKeywords(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return Array.Empty<string>();
        }

        string[] raw = csv.Split(',');
        List<string> result = new List<string>(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            string keyword = raw[i].Trim();
            if (!string.IsNullOrEmpty(keyword))
            {
                result.Add(keyword);
            }
        }

        return result.ToArray();
    }

    private void LoadDefaultPrototypesIfEmpty(bool force = false)
    {
        if (!force && prototypes.Count > 0)
        {
            return;
        }

        GameObject tree = AssetDatabase.LoadAssetAtPath<GameObject>(TreePrefabPath);
        GameObject bush = AssetDatabase.LoadAssetAtPath<GameObject>(BushPrefabPath);
        GameObject grass = AssetDatabase.LoadAssetAtPath<GameObject>(GrassPrefabPath);
        prototypes.Clear();

        if (tree != null)
        {
            prototypes.Add(new VegetationPrototype
            {
                prefab = tree,
                weight = 0.7f,
                scaleRange = new Vector2(0.85f, 1.35f),
                rotationOffsetEuler = Vector3.zero
            });
        }

        if (bush != null)
        {
            prototypes.Add(new VegetationPrototype
            {
                prefab = bush,
                weight = 1.3f,
                scaleRange = new Vector2(0.65f, 1.25f),
                rotationOffsetEuler = Vector3.zero
            });
        }

        if (grass != null)
        {
            prototypes.Add(new VegetationPrototype
            {
                prefab = grass,
                weight = 2.6f,
                scaleRange = new Vector2(0.75f, 1.5f),
                rotationOffsetEuler = Vector3.zero,
                alignToSurfaceNormal = true
            });
        }
    }

    private static void CreateTreePrefab(Material trunkMaterial, Material leafMaterial, Material leafLightMaterial)
    {
        GameObject root = new GameObject("PF_Veg_LowPolyTree");
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        trunk.transform.localScale = new Vector3(0.22f, 0.85f, 0.22f);
        trunk.GetComponent<Renderer>().sharedMaterial = trunkMaterial;
        Object.DestroyImmediate(trunk.GetComponent<Collider>());

        AddLeafSphere(root.transform, "LeafCrown_Main", new Vector3(0f, 1.9f, 0f), new Vector3(1.05f, 0.82f, 1.05f), leafMaterial);
        AddLeafSphere(root.transform, "LeafCrown_Top", new Vector3(0.08f, 2.45f, -0.05f), new Vector3(0.72f, 0.55f, 0.72f), leafLightMaterial);
        AddLeafSphere(root.transform, "LeafCrown_Side", new Vector3(-0.38f, 1.72f, 0.24f), new Vector3(0.62f, 0.48f, 0.62f), leafMaterial);

        SaveGeneratedPrefab(root, TreePrefabPath);
    }

    private static void CreateBushPrefab(Material leafMaterial, Material leafLightMaterial)
    {
        GameObject root = new GameObject("PF_Veg_Bush");
        AddLeafSphere(root.transform, "Bush_Main", new Vector3(0f, 0.42f, 0f), new Vector3(0.95f, 0.62f, 0.95f), leafMaterial);
        AddLeafSphere(root.transform, "Bush_Light", new Vector3(0.24f, 0.56f, -0.16f), new Vector3(0.52f, 0.38f, 0.52f), leafLightMaterial);
        AddLeafSphere(root.transform, "Bush_Side", new Vector3(-0.42f, 0.35f, 0.18f), new Vector3(0.48f, 0.34f, 0.48f), leafMaterial);
        SaveGeneratedPrefab(root, BushPrefabPath);
    }

    private static void CreateGrassPrefab(Material grassMaterial)
    {
        GameObject root = new GameObject("PF_Veg_GrassClump");
        for (int i = 0; i < 7; i++)
        {
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "GrassBlade";
            blade.transform.SetParent(root.transform, false);
            float angle = i * 51.4f;
            blade.transform.localRotation = Quaternion.Euler(Random.Range(-9f, 9f), angle, Random.Range(-4f, 4f));
            blade.transform.localPosition = Quaternion.Euler(0f, angle, 0f) * new Vector3(0.12f, 0.18f + i * 0.012f, 0f);
            blade.transform.localScale = new Vector3(0.035f, 0.32f + i * 0.035f, 0.08f);
            blade.GetComponent<Renderer>().sharedMaterial = grassMaterial;
            Object.DestroyImmediate(blade.GetComponent<Collider>());
        }

        SaveGeneratedPrefab(root, GrassPrefabPath);
    }

    private static void AddLeafSphere(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = position;
        sphere.transform.localScale = scale;
        sphere.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(sphere.GetComponent<Collider>());
    }

    private static void SaveGeneratedPrefab(GameObject root, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
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

        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", 0.18f);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GetOrCreatePlacementBoxMaterial()
    {
        const string path = MaterialFolder + "/M_Veg_PlacementBox.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        Color color = new Color(0.2f, 0.85f, 0.3f, 0.18f);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureAssetFolders()
    {
        Directory.CreateDirectory(PrefabFolder);
        Directory.CreateDirectory(MaterialFolder);
        AssetDatabase.Refresh();
    }

    private static class SceneManagerBridge
    {
        public static UnityEngine.SceneManagement.Scene GetActiveScene()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        }
    }
}
