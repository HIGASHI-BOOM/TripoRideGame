using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

public enum TripoGenerationMode
{
    VehicleBody,
    AnimalMount
}

public sealed class TripoVehicleBodyProvider : GeneratedBodyProvider
{
    private const string DefaultVehiclePrompt = "A compact stylized off-road buggy body, game-ready, chunky silhouette, no wheels";
    private const string DefaultAnimalMountPrompt = "A full-body stylized quadruped fantasy animal mount for a rider, side-view friendly, sturdy legs, broad back, saddle-ready silhouette, game-ready, PBR textured";

    [Header("Prompt")]
    [SerializeField] private TripoGenerationMode generationMode;
    [SerializeField] private string prompt = DefaultVehiclePrompt;
    [SerializeField] private string negativePrompt = "wheels, tires, blurry, low quality, text, watermark";
    [SerializeField] private string animalMountNegativePrompt = "vehicle wheels, tires, car parts, extra limbs, broken mesh, blurry, low quality, text, watermark";

    [Header("Tripo")]
    [SerializeField] private string apiKeyOverride;
    [FormerlySerializedAs("modelVersion")]
    [SerializeField] private string model = "P1-20260311";
    [SerializeField] private int faceLimit = 3000;
    [SerializeField] private bool texture = true;
    [SerializeField] private bool pbr = true;
    [SerializeField] private bool autoSize;
    [SerializeField] private bool exportUv = true;
    [SerializeField] private string textureQuality = "standard";
    [SerializeField] private string compress;
    [SerializeField] private int imageSeed = -1;
    [SerializeField] private int modelSeed = -1;
    [SerializeField] private int textureSeed = -1;

    [Header("Animal Mount Animation")]
    [SerializeField] private string rigModel = "v2.5-20260210";
    [SerializeField] private string rigType = "quadruped";
    [SerializeField] private string rigSpec = "tripo";
    [SerializeField] private string animationPreset = "preset:quadruped:walk";
    [SerializeField] private bool animateInPlace;

    [Header("Import")]
    [SerializeField] private string outputFolder = "Assets/Generated/TripoModels";
    [SerializeField] private bool convertToFbx = true;
    [SerializeField] private Vector3 targetSize = new Vector3(2.4f, 0.8f, 4.2f);
    [SerializeField] private bool autoOrientLongestAxisToLength;
    [SerializeField] private Vector3 fixedRotationEuler = new Vector3(90f, -90f, 0f);
    [SerializeField] private float fixedUniformScale = 5f;
    [SerializeField] private float pollIntervalSeconds = 2f;
    [SerializeField] private int requestTimeoutSeconds = 120;

    public string Prompt => prompt;
    public TripoGenerationMode GenerationMode => generationMode;
    public string LastStatus { get; private set; } = "Idle";
    public bool Busy { get; private set; }
    private bool useLatestLocalModelOnce;

    public void SetPrompt(string value)
    {
        prompt = value;
    }

    public void SetGenerationMode(TripoGenerationMode value)
    {
        generationMode = value;
        if (string.IsNullOrWhiteSpace(prompt) ||
            (value == TripoGenerationMode.AnimalMount && prompt == DefaultVehiclePrompt) ||
            (value == TripoGenerationMode.VehicleBody && prompt == DefaultAnimalMountPrompt))
        {
            prompt = value == TripoGenerationMode.AnimalMount ? DefaultAnimalMountPrompt : DefaultVehiclePrompt;
        }
    }

    public void UseLatestLocalModelOnce()
    {
        useLatestLocalModelOnce = true;
    }

    public override GeneratedVehicleBody CreateBody(Transform parent, VehicleBuildData buildData)
    {
        GameObject root = CreateLoadingBody(parent);
        Vector3 fitTarget = buildData != null ? buildData.bodyTargetSize : targetSize;
        StartCoroutine(GenerateAndReplace(root.transform, fitTarget));
        return new GeneratedVehicleBody(root);
    }

    private IEnumerator GenerateAndReplace(Transform root, Vector3 fitTarget)
    {
        if (Busy)
        {
            LastStatus = "Already generating";
            yield break;
        }

        if (useLatestLocalModelOnce)
        {
            useLatestLocalModelOnce = false;
            if (TryLoadLatestLocalModel(out GameObject latestModel, out string localAssetPath))
            {
                ReplaceLoadingVisual(root, latestModel, localAssetPath, null, fitTarget, autoOrientLongestAxisToLength, fixedRotationEuler, fixedUniformScale);
                RefreshVehicleCollider(root);
                LastStatus = $"Loaded local model: {localAssetPath}";
            }
            else
            {
                LastStatus = $"No local Tripo model found in {outputFolder}.";
                Debug.LogWarning(LastStatus);
            }

            yield break;
        }

        string apiKey = TripoApiLocalSettings.ResolveApiKey(apiKeyOverride);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            LastStatus = $"Missing API key. Set {TripoApiLocalSettings.ApiKeyEnvironmentVariable} or {TripoApiLocalSettings.LocalSettingsPath}.";
            Debug.LogError(LastStatus);
            yield break;
        }

        Busy = true;
        LastStatus = generationMode == TripoGenerationMode.AnimalMount
            ? "Submitting Tripo animal mount task"
            : "Submitting Tripo text-to-model task";

        TripoApiClient client = new TripoApiClient(apiKey, pollIntervalSeconds, requestTimeoutSeconds);
        TripoTaskData completedTask = null;
        string failure = null;
        bool animalMountMode = generationMode == TripoGenerationMode.AnimalMount;
        TripoTextToModelRequest request = new TripoTextToModelRequest
        {
            prompt = prompt,
            negativePrompt = animalMountMode ? animalMountNegativePrompt : negativePrompt,
            model = model,
            textureQuality = textureQuality,
            compress = compress,
            faceLimit = faceLimit,
            imageSeed = imageSeed,
            modelSeed = modelSeed,
            textureSeed = textureSeed,
            texture = animalMountMode || texture,
            pbr = animalMountMode || pbr,
            autoSize = autoSize,
            exportUv = exportUv
        };

        if (animalMountMode)
        {
            TripoAnimalAnimationRequest animationRequest = new TripoAnimalAnimationRequest
            {
                rigModel = rigModel,
                rigType = rigType,
                spec = rigSpec,
                outFormat = "fbx",
                animation = animationPreset,
                bakeAnimation = true,
                exportWithGeometry = true,
                animateInPlace = animateInPlace
            };

            yield return client.GenerateAnimatedAnimalMountFromText(
                request,
                animationRequest,
                UpdateTaskStatus,
                task => completedTask = task,
                message => failure = message);
        }
        else
        {
            yield return client.GenerateModelFromText(
                request,
                convertToFbx,
                UpdateTaskStatus,
                task => completedTask = task,
                message => failure = message);
        }

        if (!string.IsNullOrWhiteSpace(failure))
        {
            LastStatus = failure;
            Debug.LogError(failure);
            Busy = false;
            yield break;
        }

        string modelUrl = completedTask?.output?.BestModelUrl;
        byte[] modelBytes = null;
        string contentDisposition = null;
        string contentType = null;
        LastStatus = "Downloading generated model";
        yield return client.DownloadBytes(
            modelUrl,
            (bytes, disposition, type) =>
            {
                modelBytes = bytes;
                contentDisposition = disposition;
                contentType = type;
            },
            message => failure = message);

        if (!string.IsNullOrWhiteSpace(failure))
        {
            LastStatus = failure;
            Debug.LogError(failure);
            Busy = false;
            yield break;
        }

        string assetPath;
        string textureSourceAssetPath = null;
        string stem = $"{System.DateTime.Now:yyyyMMdd_HHmmss}_{TripoModelAssetStore.MakeSafeFileName(prompt)}";
        bool saveFailed = false;
        try
        {
            assetPath = TripoModelAssetStore.SaveGeneratedModel(outputFolder, stem, modelBytes, contentDisposition, contentType);
        }
        catch (System.Exception exception)
        {
            LastStatus = $"Could not save generated model: {exception.Message}";
            Debug.LogError(LastStatus);
            Busy = false;
            assetPath = null;
            saveFailed = true;
        }

        if (saveFailed)
        {
            yield break;
        }

        if (animalMountMode && !string.IsNullOrWhiteSpace(completedTask?.texture_source_model_url))
        {
            byte[] textureSourceBytes = null;
            string textureSourceDisposition = null;
            string textureSourceContentType = null;
            LastStatus = "Downloading animal mount texture source";
            yield return client.DownloadBytes(
                completedTask.texture_source_model_url,
                (bytes, disposition, type) =>
                {
                    textureSourceBytes = bytes;
                    textureSourceDisposition = disposition;
                    textureSourceContentType = type;
                },
                message => Debug.LogWarning($"Could not download animal mount texture source: {message}"));

            if (textureSourceBytes != null && textureSourceBytes.Length > 0)
            {
                try
                {
                    textureSourceAssetPath = TripoModelAssetStore.SaveGeneratedModel(outputFolder, $"{stem}_texture_source", textureSourceBytes, textureSourceDisposition, textureSourceContentType);
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning($"Could not save animal mount texture source: {exception.Message}");
                }
            }
        }

        LastStatus = $"Importing {assetPath}";
        GameObject modelPrefab = TripoModelAssetStore.LoadModelPrefab(assetPath);
        if (modelPrefab == null)
        {
            LastStatus = $"Downloaded model to {assetPath}, but Unity could not import it as a prefab.";
            Debug.LogWarning(LastStatus);
            Busy = false;
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(textureSourceAssetPath))
        {
            TripoModelAssetStore.LoadModelPrefab(textureSourceAssetPath);
        }

        ReplaceLoadingVisual(root, modelPrefab, assetPath, textureSourceAssetPath, fitTarget, autoOrientLongestAxisToLength, fixedRotationEuler, fixedUniformScale);
        RefreshVehicleCollider(root);
        LastStatus = $"Ready: {assetPath}";
        Busy = false;
    }

    private void UpdateTaskStatus(TripoTaskData task)
    {
        string taskLabel = string.IsNullOrWhiteSpace(task.type) ? task.task_id : task.type;
        LastStatus = $"{taskLabel}: {task.status} {task.progress}%";
    }

    private static GameObject CreateLoadingBody(Transform parent)
    {
        GameObject root = new GameObject("TripoBody_Loading");
        root.transform.SetParent(parent, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "GeneratingPlaceholder";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        body.transform.localScale = new Vector3(2.2f, 0.45f, 3.6f);
        Destroy(body.GetComponent<Collider>());
        return root;
    }

    private bool TryLoadLatestLocalModel(out GameObject modelPrefab, out string assetPath)
    {
        modelPrefab = null;
        assetPath = null;

#if UNITY_EDITOR
        string folder = outputFolder.Replace('\\', '/').TrimEnd('/');
        if (!folder.StartsWith("Assets/"))
        {
            folder = "Assets/" + folder.TrimStart('/');
        }

        string fullFolder = Path.Combine(Directory.GetCurrentDirectory(), folder.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(fullFolder))
        {
            return false;
        }

        string latest = null;
        System.DateTime latestWriteTime = System.DateTime.MinValue;
        string[] extensions = { "*.fbx", "*.obj", "*.gltf", "*.glb" };
        for (int i = 0; i < extensions.Length; i++)
        {
            string[] files = Directory.GetFiles(fullFolder, extensions[i], SearchOption.AllDirectories);
            for (int fileIndex = 0; fileIndex < files.Length; fileIndex++)
            {
                System.DateTime writeTime = File.GetLastWriteTimeUtc(files[fileIndex]);
                if (writeTime > latestWriteTime)
                {
                    latestWriteTime = writeTime;
                    latest = files[fileIndex];
                }
            }
        }

        if (string.IsNullOrWhiteSpace(latest))
        {
            return false;
        }

        string projectRoot = Directory.GetCurrentDirectory().Replace('\\', '/').TrimEnd('/');
        assetPath = latest.Replace('\\', '/');
        if (assetPath.StartsWith(projectRoot))
        {
            assetPath = assetPath.Substring(projectRoot.Length + 1);
        }

        modelPrefab = TripoModelAssetStore.LoadModelPrefab(assetPath);
        return modelPrefab != null;
#else
        return false;
#endif
    }

    private static void ReplaceLoadingVisual(
        Transform root,
        GameObject modelPrefab,
        string assetPath,
        string textureSourceAssetPath,
        Vector3 fitTarget,
        bool autoOrientLongestAxisToLength,
        Vector3 fixedRotationEuler,
        float fixedUniformScale)
    {
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }

        GameObject visualRoot = new GameObject("TripoVisualRoot");
        visualRoot.transform.SetParent(root, false);

        GameObject model = Instantiate(modelPrefab, visualRoot.transform);
        model.name = $"{modelPrefab.name}_Runtime";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
#if UNITY_EDITOR
        TripoModelAssetStore.ApplyGeneratedModelRuntimeDefaults(assetPath, model, textureSourceAssetPath);
#endif
        LogGeneratedMaterialSummary(model);

        FitVisualToTarget(visualRoot.transform, fitTarget, autoOrientLongestAxisToLength, fixedRotationEuler, fixedUniformScale);
        BuildSelectableBody selectableBody = root.GetComponent<BuildSelectableBody>();
        if (selectableBody == null)
        {
            selectableBody = root.gameObject.AddComponent<BuildSelectableBody>();
        }

        selectableBody.EnsureRuntimeSetup();
        selectableBody.RefitColliderToRenderers();
    }

    private static void RefreshVehicleCollider(Transform generatedBodyRoot)
    {
        VehicleController vehicle = generatedBodyRoot.GetComponentInParent<VehicleController>();
        if (vehicle == null)
        {
            return;
        }

        vehicle.ApplyGeneratedBody(new GeneratedVehicleBody(generatedBodyRoot.gameObject));
    }

    private static void LogGeneratedMaterialSummary(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        int materialCount = 0;
        int texturedCount = 0;
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Material[] materials = renderers[rendererIndex].sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null)
                {
                    continue;
                }

                materialCount++;
                if (material.mainTexture != null)
                {
                    texturedCount++;
                }
            }
        }

        if (materialCount > 0 && texturedCount == 0)
        {
            Debug.LogWarning($"Generated model has {materialCount} materials but no assigned main textures. The Tripo download may be missing external texture files.");
        }
    }

    private static void FitVisualToTarget(
        Transform visualRoot,
        Vector3 target,
        bool autoOrientLongestAxisToLength,
        Vector3 fixedRotationEuler,
        float fixedUniformScale)
    {
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;

        ApplyFixedRotation(visualRoot, fixedRotationEuler);

        if (autoOrientLongestAxisToLength)
        {
            OrientLongestHorizontalAxisToLength(visualRoot);
        }

        CenterVisualOnRoot(visualRoot);
        visualRoot.localScale = Vector3.one * Mathf.Max(0.01f, fixedUniformScale);
        AlignBodyBottom(visualRoot, Mathf.Max(0.35f, target.y * 0.55f));
    }

    private static void CenterVisualOnRoot(Transform body)
    {
        Bounds localBounds = CalculateRendererBoundsRelativeTo(body, body);
        if (localBounds.size.sqrMagnitude < 0.0001f)
        {
            return;
        }

        for (int i = 0; i < body.childCount; i++)
        {
            body.GetChild(i).localPosition -= localBounds.center;
        }
    }

    private static void ApplyFixedRotation(Transform body, Vector3 fixedRotationEuler)
    {
        if (fixedRotationEuler.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion rotation = Quaternion.Euler(fixedRotationEuler);
        for (int i = 0; i < body.childCount; i++)
        {
            Transform child = body.GetChild(i);
            child.localRotation = rotation * child.localRotation;
        }
    }

    private static void OrientLongestHorizontalAxisToLength(Transform body)
    {
        Bounds bounds = CalculateBounds(body);
        if (bounds.size.x <= bounds.size.z)
        {
            return;
        }

        for (int i = 0; i < body.childCount; i++)
        {
            Transform child = body.GetChild(i);
            child.localRotation = Quaternion.Euler(0f, -90f, 0f) * child.localRotation;
        }
    }

    private static void AlignBodyBottom(Transform body, float desiredBottomY)
    {
        if (body.parent == null)
        {
            return;
        }

        Bounds bounds = CalculateRendererBoundsRelativeTo(body, body.parent);
        if (bounds.size.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float currentBottomY = bounds.min.y;
        body.localPosition += Vector3.up * (desiredBottomY - currentBottomY);
    }

    private static Bounds CalculateBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(root.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static Bounds CalculateRendererBoundsRelativeTo(Transform root, Transform relativeTo)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] is LineRenderer)
            {
                continue;
            }

            Bounds rendererBounds = renderers[i].bounds;
            Vector3 min = rendererBounds.min;
            Vector3 max = rendererBounds.max;
            EncapsulatePoint(ref bounds, ref hasBounds, relativeTo, new Vector3(min.x, min.y, min.z));
            EncapsulatePoint(ref bounds, ref hasBounds, relativeTo, new Vector3(min.x, min.y, max.z));
            EncapsulatePoint(ref bounds, ref hasBounds, relativeTo, new Vector3(min.x, max.y, min.z));
            EncapsulatePoint(ref bounds, ref hasBounds, relativeTo, new Vector3(min.x, max.y, max.z));
            EncapsulatePoint(ref bounds, ref hasBounds, relativeTo, new Vector3(max.x, min.y, min.z));
            EncapsulatePoint(ref bounds, ref hasBounds, relativeTo, new Vector3(max.x, min.y, max.z));
            EncapsulatePoint(ref bounds, ref hasBounds, relativeTo, new Vector3(max.x, max.y, min.z));
            EncapsulatePoint(ref bounds, ref hasBounds, relativeTo, new Vector3(max.x, max.y, max.z));
        }

        return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.zero);
    }

    private static void EncapsulatePoint(ref Bounds bounds, ref bool hasBounds, Transform relativeTo, Vector3 worldPoint)
    {
        Vector3 point = relativeTo.InverseTransformPoint(worldPoint);
        if (!hasBounds)
        {
            bounds = new Bounds(point, Vector3.zero);
            hasBounds = true;
            return;
        }

        bounds.Encapsulate(point);
    }
}
