using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TripoRideSceneBootstrapper
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string MarkerPath = "ProjectSettings/TripoRideSceneBootstrapper.done";

    [MenuItem("Tools/TripoRide/Rebuild Simple Third Person Scene")]
    private static void RebuildFromMenu()
    {
        BuildScene(force: true);
    }

    private static void BuildScene(bool force)
    {
        if (!EnsureSampleSceneIsActive(force))
        {
            Debug.LogWarning("TripoRide bootstrap skipped because another dirty scene is active.");
            return;
        }

        Material groundMaterial = GetOrCreateMaterial("Assets/Settings/M_Ground_Grass.mat", new Color(0.28f, 0.48f, 0.26f));

        GameObject ground = GetOrCreatePrimitive("Ground", PrimitiveType.Cube);
        ground.transform.position = new Vector3(0f, -0.05f, 0f);
        ground.transform.localScale = new Vector3(30f, 0.1f, 30f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

        GameObject player = GetOrCreatePrimitive("Player_ThirdPerson", PrimitiveType.Capsule);
        player.transform.position = Vector3.zero;
        player.transform.rotation = Quaternion.identity;
        player.transform.localScale = Vector3.one;
        RemovePrimitiveVisual(player);
        ConfigurePlayerModel(player.transform);

        CapsuleCollider capsuleCollider = player.GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            Object.DestroyImmediate(capsuleCollider);
        }

        CharacterController characterController = GetOrAdd<CharacterController>(player);
        characterController.center = new Vector3(0f, 0.44f, 0f);
        characterController.height = 0.88f;
        characterController.radius = 0.2f;
        characterController.stepOffset = 0.35f;
        characterController.slopeLimit = 45f;
        characterController.skinWidth = 0.04f;
        characterController.minMoveDistance = 0.001f;

        GameObject cameraObject = GetOrCreateCamera(player.transform);
        SimpleThirdPersonCamera thirdPersonCamera = GetOrAdd<SimpleThirdPersonCamera>(cameraObject);
        thirdPersonCamera.Target = player.transform;

        SimpleThirdPersonController controller = GetOrAdd<SimpleThirdPersonController>(player);
        SerializedObject controllerObject = new SerializedObject(controller);
        controllerObject.FindProperty("cameraTransform").objectReferenceValue = cameraObject.transform;
        controllerObject.FindProperty("modelAnimator").objectReferenceValue = player.GetComponentInChildren<Animator>(true);
        controllerObject.ApplyModifiedPropertiesWithoutUndo();

        CreateOrUpdateLight();
        CreateOrUpdateSpawnMarker();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        File.WriteAllText(MarkerPath, "TripoRide simple third-person scene generated.\n");
        AssetDatabase.Refresh();
        Debug.Log("TripoRide simple third-person scene is ready. Use WASD to move, Shift to run, Space to jump, and move the mouse to control the camera. Press Esc to release the cursor and left-click to lock it again.");
    }

    private static bool EnsureSampleSceneIsActive(bool force)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path == ScenePath)
        {
            return true;
        }

        if (!force && activeScene.isDirty)
        {
            return false;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return true;
    }

    private static GameObject GetOrCreatePrimitive(string objectName, PrimitiveType primitiveType)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
        {
            return existing;
        }

        GameObject created = GameObject.CreatePrimitive(primitiveType);
        created.name = objectName;
        return created;
    }

    private static void RemovePrimitiveVisual(GameObject player)
    {
        MeshRenderer renderer = player.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Object.DestroyImmediate(renderer);
        }

        MeshFilter filter = player.GetComponent<MeshFilter>();
        if (filter != null)
        {
            Object.DestroyImmediate(filter);
        }
    }

    private static void ConfigurePlayerModel(Transform playerRoot)
    {
        const string modelPath = "Assets/Prefabs/Player/Pongpong.prefab";
        GameObject existing = GameObject.Find("Pongpong_Model");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

        if (prefab == null)
        {
            Debug.LogWarning($"Could not find player model prefab at {modelPath}.");
            return;
        }

        GameObject model = existing != null ? existing : (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        model.name = "Pongpong_Model";
        model.transform.SetParent(playerRoot, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        model.transform.localScale = Vector3.one;

        Animator animator = model.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            RuntimeAnimatorController controller =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Player/Pongpong_Player.controller");
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }
        }
    }

    private static GameObject GetOrCreateCamera(Transform player)
    {
        Camera camera = Camera.main;
        GameObject cameraObject = camera != null ? camera.gameObject : GameObject.Find("Main Camera");

        if (cameraObject == null)
        {
            cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
        }

        cameraObject.transform.position = player.position + new Vector3(0f, 2.4f, -6f);
        cameraObject.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
        cameraObject.tag = "MainCamera";
        GetOrAdd<Camera>(cameraObject).fieldOfView = 60f;

        return cameraObject;
    }

    private static void CreateOrUpdateLight()
    {
        GameObject lightObject = GameObject.Find("Directional Light");
        if (lightObject == null)
        {
            lightObject = new GameObject("Directional Light", typeof(Light));
        }

        Light light = GetOrAdd<Light>(lightObject);
        light.type = LightType.Directional;
        light.intensity = 2.2f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
    }

    private static void CreateOrUpdateSpawnMarker()
    {
        GameObject marker = GameObject.Find("Start_Marker");
        if (marker == null)
        {
            marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Start_Marker";
        }

        marker.transform.position = new Vector3(0f, 0.01f, 0f);
        marker.transform.localScale = new Vector3(1.6f, 0.02f, 1.6f);
        marker.GetComponent<Renderer>().sharedMaterial =
            GetOrCreateMaterial("Assets/Settings/M_Start_Marker.mat", new Color(0.95f, 0.8f, 0.2f));

        Object.DestroyImmediate(marker.GetComponent<Collider>());
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

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
