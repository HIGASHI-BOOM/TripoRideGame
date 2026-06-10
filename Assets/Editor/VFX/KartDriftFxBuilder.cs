using System.IO;
using UnityEditor;
using UnityEngine;

public static class KartDriftFxBuilder
{
    private const string RootFolder = "Assets/Generated/KartDriftFx";
    private const string MaterialFolder = RootFolder + "/Materials";
    private const string TextureFolder = RootFolder + "/Textures";
    private const string MeshFolder = RootFolder + "/Meshes";
    private const string TrailTexturePath = TextureFolder + "/T_DriftTrail_Glow.png";
    private const string SparkTexturePath = TextureFolder + "/T_DriftSpark_Glow.png";
    private const string MiniBoostSpeedLineTexturePath = TextureFolder + "/T_MiniBoostSpeedLines_Radial.png";
    private const string MiniBoostNoiseTexturePath = TextureFolder + "/T_MiniBoostSeamlessNoise_BW.png";
    private const string HemisphereShaderPath = RootFolder + "/Shaders/MiniBoostHemisphere.shader";
    private const string HemisphereMeshPath = MeshFolder + "/MESH_MiniBoostHemisphereShell.asset";
    private const string HalfDomeMembraneMeshPath = MeshFolder + "/MESH_MiniBoostHalfDomeMembrane.asset";
    private const string HalfDomeRimMeshPath = MeshFolder + "/MESH_MiniBoostHalfDomeRim.asset";
    private const string TrailMaterialPath = MaterialFolder + "/M_DriftTrail_Neon.mat";
    private const string SparkMaterialPath = MaterialFolder + "/M_DriftSpark_Neon.mat";
    private const string PulseMaterialPath = MaterialFolder + "/M_DriftPulse_Neon.mat";
    private const string HemisphereMaterialPath = MaterialFolder + "/M_MiniBoostHemisphere_Neon.mat";
    private const string AirflowTrailMaterialPath = MaterialFolder + "/M_MiniBoostAirflowTrail.mat";
    private const string MiniBoostFxPrefabPath = RootFolder + "/PF_KartMiniBoostFrontWaveFX.prefab";
    private const string KartPrefabPath = "Assets/Prefabs/Kart/PF_ArcadeKart.prefab";
    private const string InstalledMiniBoostFxName = "PF_KartMiniBoostFrontWaveFX";

    [MenuItem("Tools/Kart/Rebuild Neon Drift FX Assets")]
    public static void RebuildAssets()
    {
        EnsureDriftFxAssets();
        CreateMiniBoostFxPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Neon drift FX assets rebuilt in {RootFolder}");
    }

    [MenuItem("Tools/Kart/Install Neon Drift FX On Arcade Kart")]
    public static void InstallOnArcadeKart()
    {
        EnsureDriftFxAssets();
        CreateMiniBoostFxPrefab();

        GameObject kartRoot = PrefabUtility.LoadPrefabContents(KartPrefabPath);
        if (kartRoot == null)
        {
            Debug.LogError($"Arcade kart prefab not found at {KartPrefabPath}");
            return;
        }

        try
        {
            KartController kart = kartRoot.GetComponent<KartController>();
            Rigidbody body = kartRoot.GetComponent<Rigidbody>();
            Transform rearLeft = kartRoot.transform.Find("WheelSocket_RL");
            Transform rearRight = kartRoot.transform.Find("WheelSocket_RR");
            KartDriftFxController fx = kartRoot.GetComponent<KartDriftFxController>();
            if (fx == null)
            {
                fx = kartRoot.AddComponent<KartDriftFxController>();
            }

            ConfigureComponent(fx, kart, body, rearLeft, rearRight);
            InstallMiniBoostFxInstance(kartRoot.transform, kart, body);
            PrefabUtility.SaveAsPrefabAsset(kartRoot, KartPrefabPath);
            Debug.Log($"Installed neon drift and mini-boost FX on {KartPrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(kartRoot);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    public static void EnsureDriftFxAssets()
    {
        EnsureFolders();
        CreateHemisphereShader();
        Texture2D trailTexture = CreateTrailTexture();
        Texture2D sparkTexture = CreateSparkTexture();
        ConfigureMiniBoostTextureImporters();
        Material trailMaterial = CreateMaterial(TrailMaterialPath, "M_DriftTrail_Neon", trailTexture, new Color(0.1f, 0.95f, 1f, 0.9f));
        Material sparkMaterial = CreateMaterial(SparkMaterialPath, "M_DriftSpark_Neon", sparkTexture, new Color(1f, 0.25f, 0.95f, 0.95f));
        Material pulseMaterial = CreateMaterial(PulseMaterialPath, "M_DriftPulse_Neon", sparkTexture, new Color(0.35f, 1f, 1f, 0.9f));
        Material airflowTrailMaterial = CreateMaterial(AirflowTrailMaterialPath, "M_MiniBoostAirflowTrail", trailTexture, new Color(0.55f, 1f, 1f, 1f));
        Material hemisphereMaterial = CreateHemisphereMaterial();

        EditorUtility.SetDirty(trailMaterial);
        EditorUtility.SetDirty(sparkMaterial);
        EditorUtility.SetDirty(pulseMaterial);
        EditorUtility.SetDirty(airflowTrailMaterial);
        EditorUtility.SetDirty(hemisphereMaterial);
    }

    public static GameObject CreateMiniBoostFxPrefab()
    {
        EnsureDriftFxAssets();
        Mesh hemisphereMesh = CreateHemisphereMesh(HemisphereMeshPath, 29, 96);
        Mesh membraneMesh = CreateHalfDomeMesh(HalfDomeMembraneMeshPath, 1.28f, 1.64f, 8, 72, false);
        Mesh rimMesh = CreateHalfDomeMesh(HalfDomeRimMeshPath, 1.46f, 1.74f, 5, 72, true);
        Material hemisphereMaterial = AssetDatabase.LoadAssetAtPath<Material>(HemisphereMaterialPath);
        Material waveMaterial = AssetDatabase.LoadAssetAtPath<Material>(AirflowTrailMaterialPath);
        Material sparkMaterial = AssetDatabase.LoadAssetAtPath<Material>(SparkMaterialPath);

        UnityEditor.SceneManagement.PrefabStage currentStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (currentStage != null && currentStage.assetPath == MiniBoostFxPrefabPath)
        {
            GameObject stageRoot = currentStage.prefabContentsRoot;
            RebuildMiniBoostFxRoot(stageRoot, hemisphereMesh, membraneMesh, rimMesh, hemisphereMaterial, waveMaterial, sparkMaterial);
            KartMiniBoostFxController stageFx = stageRoot.GetComponent<KartMiniBoostFxController>();
            ConfigureMiniBoostComponent(stageFx, null, null, null, null);
            EditorUtility.SetDirty(stageRoot);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(stageRoot.scene);
            return AssetDatabase.LoadAssetAtPath<GameObject>(MiniBoostFxPrefabPath);
        }

        GameObject root = new GameObject(InstalledMiniBoostFxName);
        RebuildMiniBoostFxRoot(root, hemisphereMesh, membraneMesh, rimMesh, hemisphereMaterial, waveMaterial, sparkMaterial);
        KartMiniBoostFxController miniBoostFx = root.GetComponent<KartMiniBoostFxController>();
        ConfigureMiniBoostComponent(miniBoostFx, null, null, null, null);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, MiniBoostFxPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    public static void ConfigureComponent(
        KartDriftFxController fx,
        KartController kart,
        Rigidbody body,
        Transform rearLeft,
        Transform rearRight)
    {
        SerializedObject serialized = new SerializedObject(fx);
        serialized.FindProperty("kart").objectReferenceValue = kart;
        serialized.FindProperty("body").objectReferenceValue = body;
        serialized.FindProperty("rearLeftAnchor").objectReferenceValue = rearLeft;
        serialized.FindProperty("rearRightAnchor").objectReferenceValue = rearRight;
        serialized.FindProperty("trailMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(TrailMaterialPath);
        serialized.FindProperty("sparkMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(SparkMaterialPath);
        serialized.FindProperty("pulseMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(PulseMaterialPath);
        serialized.FindProperty("groundRayLength").floatValue = 1.4f;
        serialized.FindProperty("groundOffset").floatValue = 0.035f;
        serialized.FindProperty("minVisualSpeed").floatValue = 7f;
        serialized.FindProperty("lateralSpeedForFullIntensity").floatValue = 7f;
        serialized.FindProperty("trailMinTime").floatValue = 0.12f;
        serialized.FindProperty("trailMaxTime").floatValue = 0.55f;
        serialized.FindProperty("trailMinWidth").floatValue = 0.12f;
        serialized.FindProperty("trailMaxWidth").floatValue = 0.26f;
        serialized.FindProperty("sparksPerSecond").floatValue = 48f;
        serialized.FindProperty("pulseDuration").floatValue = 0.24f;
        serialized.FindProperty("pulseRadius").floatValue = 0.42f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(fx);
    }

    public static void ConfigureMiniBoostComponent(
        KartMiniBoostFxController fx,
        KartController kart,
        Rigidbody body,
        Transform rearLeft,
        Transform rearRight)
    {
        SerializedObject serialized = new SerializedObject(fx);
        serialized.FindProperty("kart").objectReferenceValue = kart;
        serialized.FindProperty("body").objectReferenceValue = body;
        ParticleSystem[] systems = fx.GetComponentsInChildren<ParticleSystem>(true);
        SerializedProperty effectSystems = serialized.FindProperty("effectSystems");
        effectSystems.arraySize = systems.Length;
        for (int i = 0; i < systems.Length; i++)
        {
            effectSystems.GetArrayElementAtIndex(i).objectReferenceValue = systems[i];
        }

        MeshRenderer[] renderers = fx.GetComponentsInChildren<MeshRenderer>(true);
        SerializedProperty shellRenderers = serialized.FindProperty("shellRenderers");
        shellRenderers.arraySize = renderers.Length;
        for (int i = 0; i < renderers.Length; i++)
        {
            shellRenderers.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
        }

        serialized.FindProperty("showPrefabPreview").boolValue = true;
        serialized.FindProperty("previewTime").floatValue = 0.32f;
        serialized.FindProperty("previewCharge").floatValue = 0.85f;
        serialized.FindProperty("burstDuration").floatValue = 0.42f;
        serialized.FindProperty("lowChargeColor").colorValue = new Color(0.18f, 0.92f, 1f, 1f);
        serialized.FindProperty("highChargeColor").colorValue = new Color(1f, 0.72f, 0.18f, 1f);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(fx);
    }

    public static GameObject InstallMiniBoostFxInstance(Transform parent, KartController kart, Rigidbody body)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == InstalledMiniBoostFxName || parent.GetComponent<KartMiniBoostFxController>() != null && parent.GetComponent<KartController>() == null)
        {
            Debug.LogWarning("Skipped installing mini boost FX inside its own prefab to avoid cyclic nesting.");
            return parent.gameObject;
        }

        CleanupLegacyMiniBoostFx(parent);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MiniBoostFxPrefabPath);
        if (prefab == null)
        {
            prefab = CreateMiniBoostFxPrefab();
        }

        if (prefab == null)
        {
            Debug.LogWarning($"Mini boost front wave FX prefab missing at {MiniBoostFxPrefabPath}.");
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
        {
            Debug.LogWarning($"Failed to instantiate mini boost front wave FX prefab at {MiniBoostFxPrefabPath}.");
            return null;
        }

        instance.name = InstalledMiniBoostFxName;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        KartMiniBoostFxController miniBoostFx = instance.GetComponent<KartMiniBoostFxController>();
        if (miniBoostFx != null)
        {
            ConfigureMiniBoostComponent(miniBoostFx, kart, body, null, null);
        }

        return instance;
    }

    public static void CleanupLegacyMiniBoostFx(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        KartMiniBoostFxController rootController = parent.GetComponent<KartMiniBoostFxController>();
        if (rootController != null)
        {
            Object.DestroyImmediate(rootController, true);
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (IsMiniBoostFxChild(child.name))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static bool IsMiniBoostFxChild(string childName)
    {
        return childName == InstalledMiniBoostFxName
            || childName == "MiniBoostFrontWave"
            || childName == "MiniBoostFrontSparks"
            || childName == "MiniBoostHemisphereShell"
            || childName == "PS_HalfDomeMembrane"
            || childName == "PS_HalfDomeRim"
            || childName == "PS_HalfDomeHighlight"
            || childName == "PS_SpeedLines"
            || childName == "PS_GraffitiSparks"
            || childName == "PS_TailJet_L"
            || childName == "PS_TailJet_R"
            || childName == "PS_TailWindLines"
            || childName == "PS_TailEmberSparks"
            || childName == "PS_HemisphereAirflowTrails"
            || childName.StartsWith("MiniBoostWaveArc_")
            || childName.StartsWith("MiniBoostSpeedLine_");
    }

    private static void RebuildMiniBoostFxRoot(
        GameObject root,
        Mesh hemisphereMesh,
        Mesh membraneMesh,
        Mesh rimMesh,
        Material hemisphereMaterial,
        Material waveMaterial,
        Material sparkMaterial)
    {
        if (root == null)
        {
            return;
        }

        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        }

        KartMiniBoostFxController controller = root.GetComponent<KartMiniBoostFxController>();
        if (controller == null)
        {
            controller = root.AddComponent<KartMiniBoostFxController>();
        }

        CreateHemisphereRenderer(root.transform, membraneMesh, hemisphereMaterial);
        CreateMeshParticleSystem(root.transform, "PS_HalfDomeMembrane", membraneMesh, hemisphereMaterial, new Color(1f, 0.34f, 0.32f, 0.34f), 0.34f, 0.72f, 1.18f);
        CreateMeshParticleSystem(root.transform, "PS_HalfDomeRim", rimMesh, hemisphereMaterial, new Color(1f, 0.46f, 0.38f, 0.74f), 0.42f, 0.86f, 1.26f);
        CreateMeshParticleSystem(root.transform, "PS_HalfDomeHighlight", rimMesh, waveMaterial, new Color(1f, 0.78f, 0.72f, 0.66f), 0.28f, 0.72f, 1.1f);
        CreateHemisphereAirflowTrailSystem(root.transform, "PS_HemisphereAirflowTrails", hemisphereMesh, waveMaterial);
        CreateSpeedLineParticleSystem(root.transform, "PS_SpeedLines", waveMaterial);
        CreateSparkParticleSystem(root.transform, "PS_GraffitiSparks", sparkMaterial);
        CreateTailJetParticleSystem(root.transform, "PS_TailJet_L", waveMaterial, -0.46f);
        CreateTailJetParticleSystem(root.transform, "PS_TailJet_R", waveMaterial, 0.46f);
        CreateTailWindLineParticleSystem(root.transform, "PS_TailWindLines", waveMaterial);
        CreateTailEmberParticleSystem(root.transform, "PS_TailEmberSparks", sparkMaterial);

        ConfigureMiniBoostComponent(controller, null, null, null, null);
        controller.SendMessage("OnValidate", SendMessageOptions.DontRequireReceiver);
    }

    private static void CreateHemisphereAirflowTrailSystem(Transform parent, string name, Mesh emitterMesh, Material trailMaterial)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);

        ParticleSystem particles = child.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.72f;
        main.loop = false;
        main.prewarm = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.34f, 0.52f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4.4f, 6.8f);
        main.startSize = 0.03f;
        main.startColor = new Color(0.7f, 1f, 1f, 1f);
        main.maxParticles = 120;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 110),
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Mesh;
        shape.mesh = emitterMesh;
        shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
        shape.meshSpawnMode = ParticleSystemShapeMultiModeValue.Random;
        shape.useMeshColors = false;
        shape.normalOffset = 0.015f;
        shape.randomPositionAmount = 0.02f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.34f, 0.34f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.08f, 0.16f);
        velocity.z = new ParticleSystem.MinMaxCurve(-5.8f, -3.8f);

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        noise.frequency = 0.9f;
        noise.scrollSpeed = 1.2f;
        noise.octaveCount = 2;
        noise.damping = true;

        ParticleSystem.TrailModule trails = particles.trails;
        trails.enabled = true;
        trails.mode = ParticleSystemTrailMode.PerParticle;
        trails.ratio = 1f;
        trails.lifetime = new ParticleSystem.MinMaxCurve(0.92f, new AnimationCurve(
            new Keyframe(0f, 0.0f),
            new Keyframe(0.12f, 1.0f),
            new Keyframe(0.82f, 0.82f),
            new Keyframe(1f, 0.0f)));
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.062f),
            new Keyframe(0.2f, 0.12f),
            new Keyframe(1f, 0.022f)));
        trails.colorOverTrail = CreateAirflowTrailGradient();
        trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
        trails.minVertexDistance = 0.025f;
        trails.dieWithParticles = true;
        trails.inheritParticleColor = false;
        trails.worldSpace = false;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.None;
        renderer.trailMaterial = trailMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingFudge = 0.65f;
    }

    private static void CreateHemisphereRenderer(Transform parent, Mesh mesh, Material material)
    {
        GameObject child = new GameObject("MiniBoostHemisphereShell");
        child.transform.SetParent(parent, false);

        MeshFilter filter = child.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = child.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
    }

    private static void CreateMeshParticleSystem(
        Transform parent,
        string name,
        Mesh mesh,
        Material material,
        Color startColor,
        float lifetime,
        float startScale,
        float endScale)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);

        ParticleSystem particles = child.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.62f;
        main.loop = false;
        main.prewarm = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = startScale;
        main.startColor = startColor;
        main.maxParticles = 8;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = false;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, startScale),
            new Keyframe(0.28f, Mathf.Lerp(startScale, endScale, 0.72f)),
            new Keyframe(1f, endScale)));

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = CreateFadeGradient(startColor);

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingFudge = 0.35f;
    }

    private static void CreateSpeedLineParticleSystem(Transform parent, string name, Material material)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = new Vector3(0f, 0.42f, 1.4f);

        ParticleSystem particles = child.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.62f;
        main.loop = false;
        main.prewarm = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4.6f, 7.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.12f);
        main.startColor = new Color(0.12f, 0.95f, 1f, 0.52f);
        main.maxParticles = 120;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(3.2f, 0.22f, 0.24f);
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.04f, 0.08f);
        velocity.z = new ParticleSystem.MinMaxCurve(-7.5f, -4.8f);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = CreateFadeGradient(new Color(0.15f, 1f, 1f, 0.55f));

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 3.4f;
        renderer.velocityScale = 0.5f;
        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingFudge = 0.4f;
    }

    private static void CreateSparkParticleSystem(Transform parent, string name, Material material)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = new Vector3(0f, 0.48f, 2.15f);

        ParticleSystem particles = child.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.62f;
        main.loop = false;
        main.prewarm = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
        main.startColor = new Color(1f, 0.35f, 0.95f, 0.95f);
        main.maxParticles = 160;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 36) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1.45f;
        shape.arc = 150f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.15f, 0.5f);
        velocity.z = new ParticleSystem.MinMaxCurve(-3.2f, -0.8f);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = CreateSparkGradient();

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingFudge = 0.55f;
    }

    private static void CreateTailJetParticleSystem(Transform parent, string name, Material material, float localX)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = new Vector3(localX, 0.36f, -0.92f);
        child.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        ParticleSystem particles = child.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.48f;
        main.loop = false;
        main.prewarm = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.6f, 6.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.42f);
        main.startColor = new Color(0.38f, 0.98f, 1f, 0.92f);
        main.maxParticles = 130;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 58) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.13f;
        shape.length = 0.22f;

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = CreateJetFlameGradient();

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.35f),
            new Keyframe(0.22f, 1.15f),
            new Keyframe(1f, 0f)));

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2.8f;
        renderer.velocityScale = 0.55f;
        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingFudge = 0.55f;
    }

    private static void CreateTailWindLineParticleSystem(Transform parent, string name, Material material)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = new Vector3(0f, 0.42f, -0.78f);

        ParticleSystem particles = child.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.48f;
        main.loop = false;
        main.prewarm = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.38f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.11f);
        main.startColor = new Color(0.76f, 1f, 1f, 0.58f);
        main.maxParticles = 80;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1.45f, 0.16f, 0.12f);

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.03f, 0.08f);
        velocity.z = new ParticleSystem.MinMaxCurve(-6.4f, -4.2f);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = CreateFadeGradient(new Color(0.68f, 1f, 1f, 0.62f));

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 3.2f;
        renderer.velocityScale = 0.58f;
        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingFudge = 0.42f;
    }

    private static void CreateTailEmberParticleSystem(Transform parent, string name, Material material)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = new Vector3(0f, 0.36f, -1.08f);

        ParticleSystem particles = child.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.48f;
        main.loop = false;
        main.prewarm = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
        main.startColor = new Color(1f, 0.74f, 0.18f, 0.88f);
        main.maxParticles = 90;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1.25f, 0.12f, 0.08f);

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.65f, 0.65f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.05f, 0.34f);
        velocity.z = new ParticleSystem.MinMaxCurve(-2.7f, -0.9f);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = CreateEmberGradient();

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingFudge = 0.6f;
    }

    private static Gradient CreateJetFlameGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.26f, 0.95f, 1f), 0.28f),
                new GradientColorKey(new Color(1f, 0.66f, 0.16f), 0.86f),
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.98f, 0.08f),
                new GradientAlphaKey(0.76f, 0.38f),
                new GradientAlphaKey(0f, 1f),
            });
        return gradient;
    }

    private static Gradient CreateEmberGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.8f, 1f, 1f), 0f),
                new GradientColorKey(new Color(1f, 0.76f, 0.18f), 0.42f),
                new GradientColorKey(new Color(1f, 0.32f, 0.08f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.72f, 0.22f),
                new GradientAlphaKey(0f, 1f),
            });
        return gradient;
    }

    private static Gradient CreateFadeGradient(Color color)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.Lerp(color, Color.white, 0.28f), 0f),
                new GradientColorKey(color, 0.5f),
                new GradientColorKey(color, 1f),
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(color.a, 0.1f),
                new GradientAlphaKey(color.a * 0.6f, 0.45f),
                new GradientAlphaKey(0f, 1f),
            });
        return gradient;
    }

    private static Gradient CreateSparkGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.1f, 1f, 1f), 0f),
                new GradientColorKey(new Color(1f, 0.08f, 0.92f), 0.45f),
                new GradientColorKey(new Color(0.9f, 1f, 0.12f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.28f),
                new GradientAlphaKey(0f, 1f),
            });
        return gradient;
    }

    private static Gradient CreateAirflowTrailGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.92f, 1f, 1f), 0f),
                new GradientColorKey(new Color(0.24f, 0.96f, 1f), 0.34f),
                new GradientColorKey(new Color(0.92f, 1f, 0.34f), 0.82f),
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.08f),
                new GradientAlphaKey(0.74f, 0.54f),
                new GradientAlphaKey(0f, 1f),
            });
        return gradient;
    }

    private static void CreateHemisphereShader()
    {
        const string shaderText = @"Shader ""TripoRide/Kart/MiniBoostHemisphere""
{
    Properties
    {
        _SpeedLineMap (""Speed Line Map"", 2D) = ""black"" {}
        _NoiseMap (""Noise Map"", 2D) = ""white"" {}
        _EmissionBoost (""Emission Boost"", Color) = (0, 0.38, 0.52, 1)
        _FlowSpeed (""Reverse V Flow Speed"", Float) = 2.8
        _NoiseFlowSpeed (""Noise Reverse V Flow Speed"", Float) = 1.0
        _NoiseTiling (""Noise Tiling"", Vector) = (2.45, 1.65, 0.23, 0)
        _SpeedAlphaMin (""Speed Alpha Min"", Range(0, 1)) = 0.075
        _SpeedAlphaMax (""Speed Alpha Max"", Range(0, 1)) = 0.46
        _NoiseAlphaMin (""Noise Alpha Min"", Range(0, 1)) = 0.15
        _NoiseAlphaMax (""Noise Alpha Max"", Range(0, 1)) = 0.84
        _VFadeInStart (""V Fade In Start"", Range(0, 1)) = 0.16
        _VFadeInEnd (""V Fade In End"", Range(0, 1)) = 0.34
        _VFadeOutStart (""V Fade Out Start"", Range(0, 1)) = 0.72
        _VFadeOutEnd (""V Fade Out End"", Range(0, 1)) = 0.94
        _AlphaPower (""Alpha Power"", Range(0.2, 2)) = 0.82
        _AlphaScale (""Alpha Scale"", Range(0, 1)) = 0.92
    }

    SubShader
    {
        Tags
        {
            ""RenderPipeline"" = ""UniversalPipeline""
            ""RenderType"" = ""Transparent""
            ""Queue"" = ""Transparent""
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name ""ForwardUnlit""
            Tags { ""LightMode"" = ""UniversalForward"" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_SpeedLineMap);
            SAMPLER(sampler_SpeedLineMap);
            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _EmissionBoost;
                half _FlowSpeed;
                half _NoiseFlowSpeed;
                half4 _NoiseTiling;
                half _SpeedAlphaMin;
                half _SpeedAlphaMax;
                half _NoiseAlphaMin;
                half _NoiseAlphaMax;
                half _VFadeInStart;
                half _VFadeInEnd;
                half _VFadeOutStart;
                half _VFadeOutEnd;
                half _AlphaPower;
                half _AlphaScale;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;
                float2 speedUv = uv + float2(0.0, -time * _FlowSpeed);
                float2 noiseUv = uv * _NoiseTiling.xy + float2(_NoiseTiling.z, -time * _NoiseFlowSpeed);

                half4 speed = SAMPLE_TEXTURE2D(_SpeedLineMap, sampler_SpeedLineMap, speedUv);
                half noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noiseUv).r;
                half speedAlpha = smoothstep(_SpeedAlphaMin, _SpeedAlphaMax, speed.a);
                half noiseAlpha = lerp(0.16, 1.0, smoothstep(_NoiseAlphaMin, _NoiseAlphaMax, noise));
                half fadeIn = smoothstep(_VFadeInStart, _VFadeInEnd, uv.y);
                half fadeOut = 1.0 - smoothstep(_VFadeOutStart, _VFadeOutEnd, uv.y);
                half vMask = saturate(fadeIn * fadeOut);
                half alpha = pow(saturate(speedAlpha * noiseAlpha * vMask), _AlphaPower) * _AlphaScale * input.color.a;
                half3 color = (speed.rgb * 1.8 + _EmissionBoost.rgb * 0.18) * input.color.rgb;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack ""Universal Render Pipeline/Unlit""
}";

        if (!File.Exists(HemisphereShaderPath) || File.ReadAllText(HemisphereShaderPath) != shaderText)
        {
            File.WriteAllText(HemisphereShaderPath, shaderText);
            AssetDatabase.ImportAsset(HemisphereShaderPath);
        }
    }

    private static Material CreateHemisphereMaterial()
    {
        Shader shader = Shader.Find("TripoRide/Kart/MiniBoostHemisphere");
        if (shader == null)
        {
            AssetDatabase.ImportAsset(HemisphereShaderPath);
            shader = Shader.Find("TripoRide/Kart/MiniBoostHemisphere");
        }

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(HemisphereMaterialPath);
        if (material == null || material.shader != shader)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, HemisphereMaterialPath);
        }

        material.name = "M_MiniBoostHemisphere_Neon";
        Texture2D speedLineTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MiniBoostSpeedLineTexturePath);
        Texture2D noiseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MiniBoostNoiseTexturePath);
        SetTextureIfPresent(material, "_SpeedLineMap", speedLineTexture);
        SetTextureIfPresent(material, "_NoiseMap", noiseTexture);
        SetColorIfPresent(material, "_EmissionBoost", new Color(0f, 0.38f, 0.52f, 1f));
        SetFloatIfPresent(material, "_FlowSpeed", 2.8f);
        SetFloatIfPresent(material, "_NoiseFlowSpeed", 1.0f);
        SetVectorIfPresent(material, "_NoiseTiling", new Vector4(2.45f, 1.65f, 0.23f, 0f));
        SetFloatIfPresent(material, "_SpeedAlphaMin", 0.075f);
        SetFloatIfPresent(material, "_SpeedAlphaMax", 0.46f);
        SetFloatIfPresent(material, "_NoiseAlphaMin", 0.15f);
        SetFloatIfPresent(material, "_NoiseAlphaMax", 0.84f);
        SetFloatIfPresent(material, "_VFadeInStart", 0.16f);
        SetFloatIfPresent(material, "_VFadeInEnd", 0.34f);
        SetFloatIfPresent(material, "_VFadeOutStart", 0.72f);
        SetFloatIfPresent(material, "_VFadeOutEnd", 0.94f);
        SetFloatIfPresent(material, "_AlphaPower", 0.82f);
        SetFloatIfPresent(material, "_AlphaScale", 0.92f);
        ConfigureAdditiveMaterial(material, Color.white);
        return material;
    }

    private static Mesh CreateHemisphereMesh(string path, int rings, int segments)
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(mesh, path);
        }
        else
        {
            mesh.Clear();
        }

        Vector3 center = new Vector3(0f, 0.68f, 0.62f);
        Vector3[] vertices = new Vector3[(segments + 1) * rings];
        Color[] colors = new Color[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[segments * (rings - 1) * 6];
        const float thetaMax = 88f * Mathf.Deg2Rad;
        const float radiusX = 2.25f;
        const float radiusForward = 1.55f;
        const float radiusY = 1.22f;

        for (int i = 0; i <= segments; i++)
        {
            float u = (float)i / segments;
            float phi = u * Mathf.PI * 2f;
            float phiSin = Mathf.Sin(phi);
            float phiCos = Mathf.Cos(phi);

            for (int ring = 0; ring < rings; ring++)
            {
                float v = (float)ring / (rings - 1);
                float theta = thetaMax * v;
                float thetaSin = Mathf.Sin(theta);
                float thetaCos = Mathf.Cos(theta);
                int index = i * rings + ring;

                vertices[index] = center + new Vector3(
                    phiCos * thetaSin * radiusX,
                    phiSin * thetaSin * radiusY,
                    thetaCos * radiusForward);
                uvs[index] = new Vector2(u, v);
                colors[index] = Color.white;
            }
        }

        int triangleIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            for (int ring = 0; ring < rings - 1; ring++)
            {
                int baseIndex = i * rings + ring;
                triangles[triangleIndex++] = baseIndex;
                triangles[triangleIndex++] = baseIndex + 1;
                triangles[triangleIndex++] = baseIndex + rings;
                triangles[triangleIndex++] = baseIndex + 1;
                triangles[triangleIndex++] = baseIndex + rings + 1;
                triangles[triangleIndex++] = baseIndex + rings;
            }
        }

        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    private static Mesh CreateHalfDomeMesh(string path, float innerRadius, float outerRadius, int rows, int segments, bool rim)
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(mesh, path);
        }
        else
        {
            mesh.Clear();
        }

        Vector3 center = new Vector3(0f, 0.68f, 0.86f);
        Vector3[] vertices = new Vector3[(segments + 1) * rows];
        Color[] colors = new Color[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[segments * (rows - 1) * 6];

        for (int i = 0; i <= segments; i++)
        {
            float arc01 = (float)i / segments;
            float yaw = Mathf.Lerp(-86f, 86f, arc01) * Mathf.Deg2Rad;
            float side = Mathf.Sin(yaw);
            float forward = Mathf.Cos(yaw);
            float yawNoise = Mathf.Sin(arc01 * Mathf.PI * 13.5f) * 0.07f + Mathf.Sin(arc01 * Mathf.PI * 24.5f) * 0.035f;

            for (int row = 0; row < rows; row++)
            {
                float row01 = (float)row / (rows - 1);
                float pitch = Mathf.Lerp(-42f, 62f, row01) * Mathf.Deg2Rad;
                float vertical = Mathf.Sin(pitch);
                float domeWidth = Mathf.Cos(pitch);
                float rimFalloff = rim ? Mathf.Abs(row01 - 0.5f) * 2f : 0f;
                float radius = Mathf.Lerp(innerRadius, outerRadius, rim ? 0.88f + rimFalloff * 0.12f : 1f);
                float localNoise = yawNoise + Mathf.Sin((arc01 * 9.5f + row01 * 4.75f) * Mathf.PI) * (rim ? 0.055f : 0.035f);
                float noisyRadius = radius + localNoise;
                int index = i * rows + row;
                Vector3 shellPoint = new Vector3(
                    side * domeWidth * noisyRadius * 1.05f,
                    vertical * noisyRadius * 0.68f,
                    forward * domeWidth * noisyRadius * 0.82f);
                vertices[index] = center + shellPoint;
                uvs[index] = new Vector2(arc01, row01);

                Color color = rim
                    ? Color.Lerp(new Color(0.1f, 1f, 1f, 0.9f), new Color(1f, 0.1f, 0.95f, 0.96f), Mathf.PingPong(arc01 * 1.3f + row01 * 0.55f, 1f))
                    : Color.Lerp(new Color(0f, 0.92f, 1f, 0.22f), new Color(0.85f, 1f, 0.2f, 0.48f), Mathf.SmoothStep(0f, 1f, row01));
                colors[index] = color;
            }
        }

        int triangleIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            for (int row = 0; row < rows - 1; row++)
            {
                int baseIndex = i * rows + row;
                triangles[triangleIndex++] = baseIndex;
                triangles[triangleIndex++] = baseIndex + 1;
                triangles[triangleIndex++] = baseIndex + rows;
                triangles[triangleIndex++] = baseIndex + 1;
                triangles[triangleIndex++] = baseIndex + rows + 1;
                triangles[triangleIndex++] = baseIndex + rows;
            }
        }

        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Generated");
        EnsureFolder("Assets/Generated", "KartDriftFx");
        EnsureFolder(RootFolder, "Materials");
        EnsureFolder(RootFolder, "Textures");
        EnsureFolder(RootFolder, "Shaders");
        EnsureFolder(RootFolder, "Meshes");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static Texture2D CreateTrailTexture()
    {
        const int width = 128;
        const int height = 16;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);

        for (int y = 0; y < height; y++)
        {
            float v = Mathf.Abs((y + 0.5f) / height - 0.5f) * 2f;
            float alpha = Mathf.Pow(1f - Mathf.Clamp01(v), 1.65f);
            for (int x = 0; x < width; x++)
            {
                float u = (x + 0.5f) / width;
                float edgeFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.08f))
                    * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - u) / 0.08f));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * edgeFade));
            }
        }

        texture.Apply();
        File.WriteAllBytes(TrailTexturePath, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(TrailTexturePath);
        ConfigureTextureImporter(TrailTexturePath, TextureWrapMode.Clamp);
        Object.DestroyImmediate(texture);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(TrailTexturePath);
    }

    private static Texture2D CreateSparkTexture()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 offset = (new Vector2(x, y) - center) / center.x;
                float radius = offset.magnitude;
                float alpha = Mathf.Pow(1f - Mathf.Clamp01(radius), 2.7f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        File.WriteAllBytes(SparkTexturePath, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(SparkTexturePath);
        ConfigureTextureImporter(SparkTexturePath, TextureWrapMode.Clamp);
        Object.DestroyImmediate(texture);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(SparkTexturePath);
    }

    private static void ConfigureTextureImporter(string path, TextureWrapMode wrapMode)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = wrapMode;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }

    private static void ConfigureMiniBoostTextureImporters()
    {
        ConfigureMiniBoostTextureImporter(MiniBoostSpeedLineTexturePath, true, true);
        ConfigureMiniBoostTextureImporter(MiniBoostNoiseTexturePath, false, false);
    }

    private static void ConfigureMiniBoostTextureImporter(string path, bool sRgb, bool alpha)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Mini boost texture missing at {path}. The material will be rebuilt when the texture exists.");
            return;
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = sRgb;
        importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        importer.alphaIsTransparency = alpha;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static Material CreateMaterial(string path, string materialName, Texture2D texture, Color color)
    {
        Shader shader = Shader.Find("TripoRide/Kart/NeonVertexColorAdditive");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null || material.shader != shader)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.name = materialName;
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }

        ConfigureAdditiveMaterial(material, color);
        return material;
    }

    private static void ConfigureAdditiveMaterial(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        SetFloatIfPresent(material, "_Surface", 1f);
        SetFloatIfPresent(material, "_Mode", 2f);
        SetFloatIfPresent(material, "_Blend", 1f);
        SetFloatIfPresent(material, "_AlphaClip", 0f);
        SetFloatIfPresent(material, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        SetFloatIfPresent(material, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        SetFloatIfPresent(material, "_ZWrite", 0f);
        SetFloatIfPresent(material, "_Cull", (float)UnityEngine.Rendering.CullMode.Off);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static void SetFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static void SetColorIfPresent(Material material, string propertyName, Color value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, value);
        }
    }

    private static void SetTextureIfPresent(Material material, string propertyName, Texture value)
    {
        if (value != null && material.HasProperty(propertyName))
        {
            material.SetTexture(propertyName, value);
        }
    }

    private static void SetVectorIfPresent(Material material, string propertyName, Vector4 value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetVector(propertyName, value);
        }
    }
}
