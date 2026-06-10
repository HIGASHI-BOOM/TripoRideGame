using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

public static class TripoModelAssetStore
{
    private static readonly string[] ImportableModelExtensions = { ".fbx", ".obj", ".gltf", ".glb" };

    public static string SaveGeneratedModel(string outputFolder, string stem, byte[] bytes, string contentDisposition, string contentType)
    {
        string safeStem = MakeSafeFileName(stem);
        string assetFolder = NormalizeAssetFolder(outputFolder);
        string fullFolder = ToFullPath(assetFolder);
        Directory.CreateDirectory(fullFolder);

        string extension = GuessExtension(bytes, contentDisposition, contentType);
        string fullPath = Path.Combine(fullFolder, $"{safeStem}{extension}");
        File.WriteAllBytes(fullPath, bytes);

        if (string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase))
        {
            string extractFolder = Path.Combine(fullFolder, safeStem);
            if (Directory.Exists(extractFolder))
            {
                Directory.Delete(extractFolder, true);
            }

            ZipFile.ExtractToDirectory(fullPath, extractFolder);
            string modelPath = FindFirstModelFile(extractFolder);
            return ToAssetPath(modelPath);
        }

        return ToAssetPath(fullPath);
    }

    public static GameObject LoadModelPrefab(string assetPath)
    {
#if UNITY_EDITOR
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ApplyModelImporterDefaults(assetPath);
        return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
#else
        Debug.LogWarning("Project asset import is only available in the Unity Editor. The model was downloaded but cannot be imported in a player build.");
        return null;
#endif
    }

#if UNITY_EDITOR
    private static void ApplyModelImporterDefaults(string assetPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
        {
            return;
        }

        bool changed = false;
        if (!importer.bakeAxisConversion)
        {
            importer.bakeAxisConversion = true;
            changed = true;
        }

        if (importer.importCameras)
        {
            importer.importCameras = false;
            changed = true;
        }

        if (importer.importLights)
        {
            importer.importLights = false;
            changed = true;
        }

        if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportStandard)
        {
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            changed = true;
        }

        if (!importer.importAnimation)
        {
            importer.importAnimation = true;
            changed = true;
        }

        if (importer.animationWrapMode != WrapMode.Loop)
        {
            importer.animationWrapMode = WrapMode.Loop;
            changed = true;
        }

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        bool clipsChanged = false;
        for (int i = 0; i < clips.Length; i++)
        {
            if (!clips[i].loopTime || !clips[i].loopPose)
            {
                clips[i].loopTime = true;
                clips[i].loopPose = true;
                clipsChanged = true;
            }
        }

        if (clipsChanged)
        {
            importer.clipAnimations = clips;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    public static void ApplyGeneratedModelRuntimeDefaults(string assetPath, GameObject instance, string textureSourceAssetPath)
    {
        if (instance == null)
        {
            return;
        }

        AttachGeneratedAnimationController(assetPath, instance);
        RepairGeneratedMaterials(instance, textureSourceAssetPath ?? assetPath);
    }

    private static void AttachGeneratedAnimationController(string assetPath, GameObject instance)
    {
        AnimationClip clip = FindFirstUsableClip(assetPath);
        if (clip == null)
        {
            Debug.LogWarning($"No playable animation clip was found in generated model: {assetPath}");
            return;
        }

        Animator animator = instance.GetComponent<Animator>();
        if (animator == null)
        {
            animator = instance.AddComponent<Animator>();
        }

        string controllerPath = GetControllerPath(assetPath);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ToFullPath(controllerPath)));
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        EnsureLoopState(controller, clip);
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        Debug.Log($"Generated animation controller assigned: {controllerPath} -> {clip.name}");
    }

    private static void EnsureLoopState(AnimatorController controller, AnimationClip clip)
    {
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState state = stateMachine.defaultState;
        if (state == null || state.name != "GeneratedLoop")
        {
            state = stateMachine.AddState("GeneratedLoop");
            stateMachine.defaultState = state;
        }

        state.motion = clip;
        state.writeDefaultValues = true;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
    }

    private static void RepairGeneratedMaterials(GameObject instance, string textureSourceAssetPath)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null)
        {
            return;
        }

        Texture2D baseMap = FindTextureNearModel(textureSourceAssetPath, "basecolor", "albedo", "diffuse");
        Texture2D normalMap = FindTextureNearModel(textureSourceAssetPath, "normal");
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Material[] materials = renderers[rendererIndex].sharedMaterials;
            bool changed = false;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = new Material(shader)
                {
                    name = $"{instance.name}_GeneratedMaterial_{rendererIndex}_{materialIndex}"
                };
                material.shader = shader;
                material.color = Color.white;

                if (baseMap != null)
                {
                    SetTextureIfPropertyExists(material, "_BaseMap", baseMap);
                    SetTextureIfPropertyExists(material, "_MainTex", baseMap);
                }

                if (normalMap != null)
                {
                    SetTextureIfPropertyExists(material, "_BumpMap", normalMap);
                    material.EnableKeyword("_NORMALMAP");
                }

                materials[materialIndex] = material;
                changed = true;
            }

            if (changed)
            {
                renderers[rendererIndex].sharedMaterials = materials;
            }
        }

        if (baseMap == null)
        {
            Debug.LogWarning($"No generated basecolor texture was found near texture source: {textureSourceAssetPath}");
        }
    }

    private static void SetTextureIfPropertyExists(Material material, string propertyName, Texture texture)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetTexture(propertyName, texture);
        }
    }

    private static Texture2D FindTextureNearModel(string assetPath, params string[] nameParts)
    {
        string fullModelPath = ToFullPath(assetPath);
        string folder = Path.ChangeExtension(fullModelPath, ".fbm");
        if (!Directory.Exists(folder))
        {
            folder = Path.GetDirectoryName(fullModelPath);
        }

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return null;
        }

        string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            string extension = Path.GetExtension(files[i]);
            if (!string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string fileName = Path.GetFileNameWithoutExtension(files[i]).ToLowerInvariant();
            for (int partIndex = 0; partIndex < nameParts.Length; partIndex++)
            {
                if (fileName.Contains(nameParts[partIndex]))
                {
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(ToAssetPath(files[i]));
                }
            }
        }

        return null;
    }

    private static AnimationClip FindFirstUsableClip(string assetPath)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is AnimationClip clip &&
                !string.IsNullOrWhiteSpace(clip.name) &&
                !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            {
                return clip;
            }
        }

        return null;
    }

    private static string GetControllerPath(string assetPath)
    {
        string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/') ?? "Assets/Generated/TripoModels";
        string name = Path.GetFileNameWithoutExtension(assetPath);
        return $"{folder}/{name}_Auto.controller";
    }
#endif

    public static string MakeSafeFileName(string value)
    {
        string source = string.IsNullOrWhiteSpace(value) ? "tripo_model" : value.Trim();
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            source = source.Replace(invalid, '_');
        }

        source = source.Replace(' ', '_');
        return source.Length > 44 ? source.Substring(0, 44) : source;
    }

    private static string NormalizeAssetFolder(string outputFolder)
    {
        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            return "Assets/Generated/TripoModels";
        }

        string normalized = outputFolder.Replace('\\', '/').TrimEnd('/');
        return normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) || normalized == "Assets"
            ? normalized
            : $"Assets/{normalized.TrimStart('/')}";
    }

    private static string GuessExtension(byte[] bytes, string contentDisposition, string contentType)
    {
        string fileName = TryGetContentDispositionFileName(contentDisposition);
        string extension = !string.IsNullOrWhiteSpace(fileName) ? Path.GetExtension(fileName) : string.Empty;
        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension.ToLowerInvariant();
        }

        if (bytes != null && bytes.Length >= 4)
        {
            if (bytes[0] == 0x50 && bytes[1] == 0x4b)
            {
                return ".zip";
            }

            string magic = System.Text.Encoding.ASCII.GetString(bytes, 0, Mathf.Min(bytes.Length, 24));
            if (magic.StartsWith("Kaydara FBX Binary", StringComparison.OrdinalIgnoreCase) ||
                magic.StartsWith("; FBX", StringComparison.OrdinalIgnoreCase))
            {
                return ".fbx";
            }

            if (magic.StartsWith("glTF", StringComparison.OrdinalIgnoreCase))
            {
                return ".glb";
            }
        }

        if (!string.IsNullOrWhiteSpace(contentType) && contentType.IndexOf("zip", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return ".zip";
        }

        return ".fbx";
    }

    private static string TryGetContentDispositionFileName(string contentDisposition)
    {
        if (string.IsNullOrWhiteSpace(contentDisposition))
        {
            return string.Empty;
        }

        string[] parts = contentDisposition.Split(';');
        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i].Trim();
            if (part.StartsWith("filename=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Substring("filename=".Length).Trim('"');
            }
        }

        return string.Empty;
    }

    private static string FindFirstModelFile(string folder)
    {
        for (int i = 0; i < ImportableModelExtensions.Length; i++)
        {
            string[] files = Directory.GetFiles(folder, $"*{ImportableModelExtensions[i]}", SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                return files[0];
            }
        }

        throw new FileNotFoundException($"No importable model file was found after extracting Tripo archive: {folder}");
    }

    private static string ToFullPath(string assetPath)
    {
        string projectRoot = Directory.GetCurrentDirectory();
        string relative = assetPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(projectRoot, relative);
    }

    private static string ToAssetPath(string fullPath)
    {
        string projectRoot = Directory.GetCurrentDirectory().Replace('\\', '/').TrimEnd('/');
        string normalized = fullPath.Replace('\\', '/');
        if (!normalized.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        return normalized.Substring(projectRoot.Length + 1);
    }
}
