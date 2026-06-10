using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public sealed class TexturedObjectOutlineBootstrap : MonoBehaviour
{
    private const string ShaderName = "Hidden/TripoRide/TexturedObjectOutline";
    private const string ShaderResourcePath = "Shaders/TexturedObjectOutline";
    private const string OutlineChildName = "__TexturedObjectOutline";
    private const bool OutlineEnabled = false;

    private static TexturedObjectOutlineBootstrap instance;

    [SerializeField] private Color outlineColor = new Color(0.03f, 0.025f, 0.02f, 1f);
    [SerializeField] private float outlineWidth = 0.025f;
    [SerializeField] private float scanInterval = 0.5f;

    private readonly List<OutlineEntry> entries = new List<OutlineEntry>();
    private Material outlineMaterial;
    private float nextScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        if (!OutlineEnabled)
        {
            return;
        }

        if (instance != null)
        {
            return;
        }

        GameObject host = new GameObject("TexturedObjectOutlineBootstrap");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<TexturedObjectOutlineBootstrap>();
    }

    private void Awake()
    {
        if (!OutlineEnabled)
        {
            Destroy(gameObject);
            return;
        }

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextScanTime)
        {
            ScanScene();
            nextScanTime = Time.unscaledTime + Mathf.Max(0.1f, scanInterval);
        }

        SyncOutlineRenderers();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ScanScene();
    }

    private void ScanScene()
    {
        EnsureOutlineMaterial();
        if (outlineMaterial == null)
        {
            return;
        }

        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer source = renderers[i];
            if (!ShouldOutline(source))
            {
                RemoveOutline(source);
                continue;
            }

            TexturedObjectOutlineMarker marker = source.GetComponent<TexturedObjectOutlineMarker>();
            if (marker != null && marker.OutlineRenderer != null)
            {
                continue;
            }

            Renderer outline = CreateOutlineRenderer(source);
            if (outline == null)
            {
                continue;
            }

            if (marker == null)
            {
                marker = source.gameObject.AddComponent<TexturedObjectOutlineMarker>();
            }

            marker.OutlineRenderer = outline;
            entries.Add(new OutlineEntry(source, outline));
        }
    }

    private bool ShouldOutline(Renderer source)
    {
        if (source == null ||
            source is LineRenderer ||
            source is TrailRenderer ||
            source is ParticleSystemRenderer ||
            source.GetComponent<TexturedObjectOutlineMarker>()?.IsOutlineRenderer == true ||
            IsVfxRenderer(source) ||
            RendererUsesTransparentMaterial(source) ||
            source.transform.Find(OutlineChildName) != null)
        {
            return false;
        }

        return RendererHasMesh(source) && RendererHasTexture(source);
    }

    private static bool IsVfxRenderer(Renderer source)
    {
        if (source.GetComponentInParent<KartMiniBoostFxController>() != null)
        {
            return true;
        }

        Material[] materials = source.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            Shader shader = materials[i] != null ? materials[i].shader : null;
            string shaderName = shader != null ? shader.name : string.Empty;
            if (shaderName.StartsWith("TripoRide/Kart/MiniBoost") ||
                shaderName == "TripoRide/Kart/NeonVertexColorAdditive")
            {
                return true;
            }
        }

        return false;
    }

    private static bool RendererUsesTransparentMaterial(Renderer source)
    {
        Material[] materials = source.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null)
            {
                continue;
            }

            if (material.renderQueue >= (int)RenderQueue.Transparent)
            {
                return true;
            }

            if (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f)
            {
                return true;
            }
        }

        return false;
    }

    private static bool RendererHasMesh(Renderer source)
    {
        if (source is SkinnedMeshRenderer skinned)
        {
            return skinned.sharedMesh != null;
        }

        MeshFilter meshFilter = source.GetComponent<MeshFilter>();
        return meshFilter != null && meshFilter.sharedMesh != null;
    }

    private static bool RendererHasTexture(Renderer source)
    {
        Material[] materials = source.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null)
            {
                continue;
            }

            int propertyCount = material.shader != null ? material.shader.GetPropertyCount() : 0;
            for (int propertyIndex = 0; propertyIndex < propertyCount; propertyIndex++)
            {
                if (material.shader.GetPropertyType(propertyIndex) != ShaderPropertyType.Texture)
                {
                    continue;
                }

                string propertyName = material.shader.GetPropertyName(propertyIndex);
                if (material.GetTexture(propertyName) != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void RemoveOutline(Renderer source)
    {
        if (source == null)
        {
            return;
        }

        TexturedObjectOutlineMarker marker = source.GetComponent<TexturedObjectOutlineMarker>();
        if (marker != null && marker.OutlineRenderer != null)
        {
            Destroy(marker.OutlineRenderer.gameObject);
            marker.OutlineRenderer = null;
        }

        Transform outlineChild = source.transform.Find(OutlineChildName);
        if (outlineChild != null)
        {
            Destroy(outlineChild.gameObject);
        }
    }

    private Renderer CreateOutlineRenderer(Renderer source)
    {
        GameObject outlineObject = new GameObject(OutlineChildName);
        outlineObject.transform.SetParent(source.transform, false);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;
        outlineObject.layer = source.gameObject.layer;

        Renderer outline;
        if (source is SkinnedMeshRenderer skinnedSource)
        {
            SkinnedMeshRenderer skinnedOutline = outlineObject.AddComponent<SkinnedMeshRenderer>();
            skinnedOutline.sharedMesh = skinnedSource.sharedMesh;
            skinnedOutline.rootBone = skinnedSource.rootBone;
            skinnedOutline.bones = skinnedSource.bones;
            skinnedOutline.updateWhenOffscreen = skinnedSource.updateWhenOffscreen;
            outline = skinnedOutline;
        }
        else
        {
            MeshFilter sourceMeshFilter = source.GetComponent<MeshFilter>();
            if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null)
            {
                Destroy(outlineObject);
                return null;
            }

            MeshFilter outlineMeshFilter = outlineObject.AddComponent<MeshFilter>();
            outlineMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;
            outline = outlineObject.AddComponent<MeshRenderer>();
        }

        outline.sharedMaterials = CreateOutlineMaterials(GetSubMeshCount(source));
        outline.shadowCastingMode = ShadowCastingMode.Off;
        outline.receiveShadows = false;
        outline.lightProbeUsage = LightProbeUsage.Off;
        outline.reflectionProbeUsage = ReflectionProbeUsage.Off;
        outline.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

        TexturedObjectOutlineMarker marker = outlineObject.AddComponent<TexturedObjectOutlineMarker>();
        marker.IsOutlineRenderer = true;
        return outline;
    }

    private Material[] CreateOutlineMaterials(int count)
    {
        int materialCount = Mathf.Max(1, count);
        Material[] materials = new Material[materialCount];
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = outlineMaterial;
        }

        return materials;
    }

    private static int GetSubMeshCount(Renderer source)
    {
        if (source is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
        {
            return skinned.sharedMesh.subMeshCount;
        }

        MeshFilter meshFilter = source.GetComponent<MeshFilter>();
        return meshFilter != null && meshFilter.sharedMesh != null ? meshFilter.sharedMesh.subMeshCount : 1;
    }

    private void SyncOutlineRenderers()
    {
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            OutlineEntry entry = entries[i];
            if (entry.Source == null || entry.Outline == null)
            {
                entries.RemoveAt(i);
                continue;
            }

            entry.Outline.enabled = entry.Source.enabled;
        }
    }

    private void EnsureOutlineMaterial()
    {
        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_OutlineColor", outlineColor);
            outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
            return;
        }

        Shader shader = Shader.Find(ShaderName) ?? Resources.Load<Shader>(ShaderResourcePath);
        if (shader == null)
        {
            Debug.LogWarning($"Textured object outline shader was not found: {ShaderName}");
            return;
        }

        outlineMaterial = new Material(shader)
        {
            name = "Runtime_TexturedObjectOutline"
        };
        outlineMaterial.SetColor("_OutlineColor", outlineColor);
        outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
    }

    private readonly struct OutlineEntry
    {
        public readonly Renderer Source;
        public readonly Renderer Outline;

        public OutlineEntry(Renderer source, Renderer outline)
        {
            Source = source;
            Outline = outline;
        }
    }
}

public sealed class TexturedObjectOutlineMarker : MonoBehaviour
{
    public bool IsOutlineRenderer { get; set; }
    public Renderer OutlineRenderer { get; set; }
}
