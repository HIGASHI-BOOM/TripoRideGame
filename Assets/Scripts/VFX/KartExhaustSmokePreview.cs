using UnityEngine;

[ExecuteAlways]
public sealed class KartExhaustSmokePreview : MonoBehaviour
{
    [SerializeField] private KartController kart;
    [SerializeField] private Vector3 leftEmitterLocalPosition = new Vector3(-0.18f, 0f, 0f);
    [SerializeField] private Vector3 rightEmitterLocalPosition = new Vector3(0.18f, 0f, 0f);
    [SerializeField] private float emissionRate = 18f;
    [SerializeField] private float tailSpeed = 1.15f;
    [SerializeField] private float upwardDrift = 0.2f;
    [SerializeField] private float startSizeMin = 0.16f;
    [SerializeField] private float startSizeMax = 0.36f;
    [SerializeField] private float lifetimeMin = 0.65f;
    [SerializeField] private float lifetimeMax = 1.35f;
    [SerializeField] private Color smokeColor = new Color(0.32f, 0.32f, 0.3f, 1f);
    [SerializeField] private Color smokeEndColor = new Color(0.66f, 0.65f, 0.58f, 1f);
    [SerializeField] private float smokeAlpha = 0.34f;

    private Material previewMaterial;
    private Texture2D previewTexture;
    private bool editorRebuildQueued;
    private bool emittersVisible = true;

    private void OnEnable()
    {
        ResolveKart();
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            QueueEditorRebuild();
            return;
        }
#endif

        RebuildEmitters(true);
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ResolveKart();
        ApplyVisibility(ShouldShowExhaustSmoke());
    }

    private void OnValidate()
    {
        if (isActiveAndEnabled)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                QueueEditorRebuild();
                return;
            }
#endif

            RebuildEmitters(true);
        }
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        DestroyRuntimePreviewAssets();
    }

    private void RebuildEmitters(bool play)
    {
        bool visible = ShouldShowExhaustSmoke();
        Material material = GetPreviewMaterial();
        ConfigureEmitter(GetOrCreateEmitter("ExhaustSmoke_L"), leftEmitterLocalPosition, material, play);
        ConfigureEmitter(GetOrCreateEmitter("ExhaustSmoke_R"), rightEmitterLocalPosition, material, play);
        ApplyVisibility(visible);
    }

#if UNITY_EDITOR
    private void QueueEditorRebuild()
    {
        if (editorRebuildQueued)
        {
            return;
        }

        editorRebuildQueued = true;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            editorRebuildQueued = false;
            if (this == null || !isActiveAndEnabled)
            {
                return;
            }

            RebuildEmitters(true);
        };
    }
#endif

    private ParticleSystem GetOrCreateEmitter(string emitterName)
    {
        Transform child = transform.Find(emitterName);
        if (child == null)
        {
            GameObject emitterObject = new GameObject(emitterName);
            emitterObject.transform.SetParent(transform, false);
            child = emitterObject.transform;
        }

        ParticleSystem particles = child.GetComponent<ParticleSystem>();
        if (particles == null)
        {
            particles = child.gameObject.AddComponent<ParticleSystem>();
        }

        return particles;
    }

    private void ResolveKart()
    {
        if (kart == null)
        {
            kart = GetComponentInParent<KartController>();
        }
    }

    private bool ShouldShowExhaustSmoke()
    {
        KartDriveConfig config = kart != null ? kart.DriveConfig : null;
        return config == null || config.enableExhaustSmokeFx;
    }

    private void ApplyVisibility(bool visible)
    {
        if (emittersVisible == visible)
        {
            return;
        }

        emittersVisible = visible;
        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem particles = systems[i];
            if (particles == null)
            {
                continue;
            }

            if (visible && Application.isPlaying)
            {
                particles.Play(true);
            }
            else
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }

    private void ConfigureEmitter(ParticleSystem particles, Vector3 localPosition, Material material, bool play)
    {
        Transform emitter = particles.transform;
        emitter.localPosition = localPosition;
        emitter.localRotation = Quaternion.identity;
        emitter.localScale = Vector3.one;

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.duration = 2.2f;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(startSizeMin, startSizeMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            WithAlpha(smokeColor * 0.58f, smokeAlpha * 0.75f),
            WithAlpha(smokeEndColor * 0.78f, smokeAlpha * 0.55f));
        main.maxParticles = 160;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = Mathf.Max(0f, emissionRate);
        emission.rateOverDistance = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.035f;
        shape.length = 0.04f;

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = CreateSmokeGradient();

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.18f, 0.9f),
            new Keyframe(1f, 1.85f)));

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        velocity.y = new ParticleSystem.MinMaxCurve(upwardDrift * 0.4f, upwardDrift * 1.4f);
        velocity.z = new ParticleSystem.MinMaxCurve(-tailSpeed * 1.05f, -tailSpeed * 0.55f);

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        noise.frequency = 0.65f;
        noise.scrollSpeed = 0.45f;
        noise.damping = true;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.material = material;
        renderer.sortingFudge = -0.2f;
        renderer.minParticleSize = 0.02f;
        renderer.maxParticleSize = 0.8f;

        if (play)
        {
            particles.Play();
        }
    }

    private Gradient CreateSmokeGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(smokeColor * 0.55f, 0f),
                new GradientColorKey(Color.Lerp(smokeColor, smokeEndColor, 0.45f), 0.45f),
                new GradientColorKey(smokeEndColor, 1f),
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(smokeAlpha, 0.1f),
                new GradientAlphaKey(smokeAlpha * 0.47f, 0.58f),
                new GradientAlphaKey(0f, 1f),
            });
        return gradient;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private Material GetPreviewMaterial()
    {
        if (previewMaterial != null)
        {
            return previewMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        previewMaterial = new Material(shader)
        {
            name = "KartExhaustSmokePreviewMaterial",
            hideFlags = HideFlags.DontSave,
        };

        Texture2D texture = GetPreviewTexture();
        Color materialColor = WithAlpha(Color.Lerp(smokeColor, smokeEndColor, 0.35f), smokeAlpha);
        if (previewMaterial.HasProperty("_BaseMap"))
        {
            previewMaterial.SetTexture("_BaseMap", texture);
        }

        if (previewMaterial.HasProperty("_MainTex"))
        {
            previewMaterial.SetTexture("_MainTex", texture);
        }

        ConfigureTransparentMaterial(previewMaterial, materialColor);
        return previewMaterial;
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

    private Texture2D GetPreviewTexture()
    {
        if (previewTexture != null)
        {
            return previewTexture;
        }

        const int size = 96;
        previewTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        {
            name = "KartExhaustSmokePreviewTexture",
            hideFlags = HideFlags.DontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

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
                previewTexture.SetPixel(x, y, new Color(shade, shade, shade, alpha));
            }
        }

        previewTexture.Apply();
        return previewTexture;
    }

    private void DestroyRuntimePreviewAssets()
    {
        if (previewMaterial != null)
        {
            Destroy(previewMaterial);
            previewMaterial = null;
        }

        if (previewTexture != null)
        {
            Destroy(previewTexture);
            previewTexture = null;
        }
    }
}
