using System.IO;
using UnityEditor;
using UnityEngine;

public static class KartExhaustSmokePrefabBuilder
{
    private const string RootFolder = "Assets/Generated/KartVFX";
    private const string MaterialFolder = RootFolder + "/Materials";
    private const string TexturePath = RootFolder + "/T_KartExhaustSmoke.png";
    private const string MaterialPath = MaterialFolder + "/M_KartExhaustSmoke.mat";
    private const string PrefabPath = RootFolder + "/PF_KartExhaustSmoke.prefab";
    private const string KartPrefabPath = "Assets/Prefabs/Kart/PF_ArcadeKart.prefab";
    private const string InstallableSmokePrefabPath = "Assets/Prefabs/Kart/PF_KartExhaustSmoke.prefab";
    private const string InstalledSmokeName = "PF_KartExhaustSmoke";

    [MenuItem("Tools/Kart/Rebuild Exhaust Smoke Prefab")]
    public static void Rebuild()
    {
        EnsureFolders();
        Texture2D texture = CreateSmokeTexture();
        Material material = CreateSmokeMaterial(texture);
        GameObject prefab = CreateSmokePrefab(material);

        PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        Object.DestroyImmediate(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Kart exhaust smoke prefab rebuilt at {PrefabPath}");
    }

    [MenuItem("Tools/Kart/Install Exhaust Smoke On Arcade Kart")]
    public static void InstallOnArcadeKart()
    {
        GameObject smokePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(InstallableSmokePrefabPath);
        if (smokePrefab == null)
        {
            Debug.LogError($"Kart exhaust smoke prefab not found at {InstallableSmokePrefabPath}");
            return;
        }

        GameObject kartRoot = PrefabUtility.LoadPrefabContents(KartPrefabPath);
        if (kartRoot == null)
        {
            Debug.LogError($"Arcade kart prefab not found at {KartPrefabPath}");
            return;
        }

        try
        {
            Transform existing = kartRoot.transform.Find(InstalledSmokeName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject smoke = (GameObject)PrefabUtility.InstantiatePrefab(smokePrefab, kartRoot.transform);
            smoke.name = InstalledSmokeName;
            smoke.transform.localPosition = new Vector3(0f, 0.45f, -1.25f);
            smoke.transform.localRotation = Quaternion.identity;
            smoke.transform.localScale = Vector3.one;

            PrefabUtility.SaveAsPrefabAsset(kartRoot, KartPrefabPath);
            Debug.Log($"Installed {InstalledSmokeName} on {KartPrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(kartRoot);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
        {
            AssetDatabase.CreateFolder("Assets", "Generated");
        }

        if (!AssetDatabase.IsValidFolder(RootFolder))
        {
            AssetDatabase.CreateFolder("Assets/Generated", "KartVFX");
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder(RootFolder, "Materials");
        }
    }

    private static Texture2D CreateSmokeTexture()
    {
        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 offset = (new Vector2(x, y) - center) / center.x;
                float radius = offset.magnitude;
                float angleNoise = Mathf.Sin(offset.x * 17.3f + offset.y * 5.1f) * 0.08f
                    + Mathf.Sin(offset.y * 19.7f - offset.x * 4.4f) * 0.06f;
                float alpha = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((radius + angleNoise - 0.18f) / 0.82f));
                alpha *= Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((radius - 0.72f) / 0.28f));
                float shade = Mathf.Lerp(0.42f, 0.72f, Mathf.Clamp01(1f - radius));
                texture.SetPixel(x, y, new Color(shade, shade, shade, alpha));
            }
        }

        texture.Apply();
        File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(TexturePath);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();

        Object.DestroyImmediate(texture);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
    }

    private static Material CreateSmokeMaterial(Texture2D texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null || material.shader != shader)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.name = "M_KartExhaustSmoke";
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }

        ConfigureTransparentMaterial(material, new Color(0.38f, 0.38f, 0.36f, 0.44f));
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureTransparentMaterial(Material material, Color color)
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
        SetFloatIfPresent(material, "_Blend", 0f);
        SetFloatIfPresent(material, "_AlphaClip", 0f);
        SetFloatIfPresent(material, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        SetFloatIfPresent(material, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
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

    private static GameObject CreateSmokePrefab(Material material)
    {
        GameObject root = new GameObject("PF_KartExhaustSmoke");
        CreateEmitter(root.transform, "ExhaustSmoke_L", new Vector3(-0.18f, 0f, 0f), material);
        CreateEmitter(root.transform, "ExhaustSmoke_R", new Vector3(0.18f, 0f, 0f), material);
        return root;
    }

    private static void CreateEmitter(Transform parent, string name, Vector3 localPosition, Material material)
    {
        GameObject emitterObject = new GameObject(name);
        emitterObject.transform.SetParent(parent, false);
        emitterObject.transform.localPosition = localPosition;
        emitterObject.transform.localRotation = Quaternion.identity;

        ParticleSystem particles = emitterObject.AddComponent<ParticleSystem>();
        ConfigureMain(particles);
        ConfigureEmission(particles);
        ConfigureShape(particles);
        ConfigureColorOverLifetime(particles);
        ConfigureSizeOverLifetime(particles);
        ConfigureVelocityOverLifetime(particles);
        ConfigureNoise(particles);
        ConfigureRenderer(particles, material);
    }

    private static void ConfigureMain(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.duration = 2.2f;
        main.loop = true;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 1.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.36f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.18f, 0.18f, 0.17f, 0.24f),
            new Color(0.52f, 0.50f, 0.45f, 0.18f));
        main.maxParticles = 160;
    }

    private static void ConfigureEmission(ParticleSystem particles)
    {
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 18f;
        emission.rateOverDistance = 0f;
    }

    private static void ConfigureShape(ParticleSystem particles)
    {
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.035f;
        shape.length = 0.04f;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;
    }

    private static void ConfigureColorOverLifetime(ParticleSystem particles)
    {
        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.17f, 0.17f, 0.16f), 0f),
                new GradientColorKey(new Color(0.36f, 0.35f, 0.32f), 0.45f),
                new GradientColorKey(new Color(0.62f, 0.61f, 0.56f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.34f, 0.1f),
                new GradientAlphaKey(0.16f, 0.58f),
                new GradientAlphaKey(0f, 1f),
            });
        color.color = gradient;
    }

    private static void ConfigureSizeOverLifetime(ParticleSystem particles)
    {
        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;

        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.18f, 0.9f),
            new Keyframe(1f, 1.85f));
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private static void ConfigureVelocityOverLifetime(ParticleSystem particles)
    {
        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.08f, 0.28f);
        velocity.z = new ParticleSystem.MinMaxCurve(-1.35f, -0.65f);
    }

    private static void ConfigureNoise(ParticleSystem particles)
    {
        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        noise.frequency = 0.65f;
        noise.scrollSpeed = 0.45f;
        noise.damping = true;
    }

    private static void ConfigureRenderer(ParticleSystem particles, Material material)
    {
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.material = material;
        renderer.sortingFudge = -0.2f;
        renderer.minParticleSize = 0.02f;
        renderer.maxParticleSize = 0.8f;
    }
}
