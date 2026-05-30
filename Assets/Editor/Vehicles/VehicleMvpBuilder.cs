using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VehicleMvpBuilder
{
    private const string VehicleFolder = "Assets/Prefabs/Vehicles";
    private const string MaterialFolder = "Assets/Materials/Vehicles";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/TripoRide/Rebuild Local Vehicle MVP")]
    public static void BuildAll()
    {
        EnsureFolders();
        Material bodyMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Vehicle_Body.mat", new Color(0.15f, 0.42f, 0.95f));
        Material glassMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Vehicle_Glass.mat", new Color(0.08f, 0.18f, 0.24f));
        Material tireMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Wheel_Tire.mat", new Color(0.025f, 0.025f, 0.025f));
        Material hubMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Wheel_Hub.mat", new Color(0.72f, 0.76f, 0.78f));
        Material groundMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Test_Ground.mat", new Color(0.26f, 0.42f, 0.27f));
        Material obstacleMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Test_Obstacle.mat", new Color(0.9f, 0.55f, 0.15f));

        GameObject localBodyPrefab = CreateLocalBodyPrefab(bodyMaterial, glassMaterial);
        WheelAssembly smallWheelPrefab = CreateWheelPrefab("PF_Wheel_Small", WheelSize.Small, 0.32f, 0.22f, tireMaterial, hubMaterial);
        WheelAssembly mediumWheelPrefab = CreateWheelPrefab("PF_Wheel_Medium", WheelSize.Medium, 0.42f, 0.28f, tireMaterial, hubMaterial);
        VehicleController rigPrefab = CreateVehicleRigPrefab();

        BuildScene(rigPrefab, smallWheelPrefab, mediumWheelPrefab, localBodyPrefab, groundMaterial, obstacleMaterial);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Local vehicle MVP rebuilt: rig prefab, wheels, body provider, scene, and UI are ready.");
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets", "Prefabs");
        CreateFolder("Assets/Prefabs", "Vehicles");
        CreateFolder("Assets", "Materials");
        CreateFolder("Assets/Materials", "Vehicles");
    }

    private static void CreateFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static GameObject CreateLocalBodyPrefab(Material bodyMaterial, Material glassMaterial)
    {
        GameObject root = new GameObject("PF_LocalBody_Blockout");

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(2.25f, 0.55f, 3.6f);
        body.transform.localPosition = new Vector3(0f, 0.55f, 0.05f);
        body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.name = "Cabin";
        cabin.transform.SetParent(root.transform, false);
        cabin.transform.localScale = new Vector3(1.35f, 0.55f, 1.35f);
        cabin.transform.localPosition = new Vector3(0f, 1.05f, -0.25f);
        cabin.GetComponent<Renderer>().sharedMaterial = glassMaterial;
        Object.DestroyImmediate(cabin.GetComponent<Collider>());

        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "FrontNose";
        nose.transform.SetParent(root.transform, false);
        nose.transform.localScale = new Vector3(1.85f, 0.35f, 1.2f);
        nose.transform.localPosition = new Vector3(0f, 0.78f, 1.45f);
        nose.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
        Object.DestroyImmediate(nose.GetComponent<Collider>());

        return SavePrefab(root, $"{VehicleFolder}/PF_LocalBody_Blockout.prefab");
    }

    private static WheelAssembly CreateWheelPrefab(
        string prefabName,
        WheelSize size,
        float radius,
        float width,
        Material tireMaterial,
        Material hubMaterial)
    {
        GameObject root = new GameObject(prefabName);
        WheelAssembly assembly = root.AddComponent<WheelAssembly>();
        assembly.Configure(size, radius);

        Transform steerPivot = new GameObject("SteerPivot").transform;
        steerPivot.SetParent(root.transform, false);

        Transform rollPivot = new GameObject("RollPivot").transform;
        rollPivot.SetParent(steerPivot, false);

        GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tire.name = "Tire";
        tire.transform.SetParent(rollPivot, false);
        tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        tire.transform.localScale = new Vector3(radius * 2f, width * 0.5f, radius * 2f);
        tire.GetComponent<Renderer>().sharedMaterial = tireMaterial;
        Object.DestroyImmediate(tire.GetComponent<Collider>());

        GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hub.name = "Hub";
        hub.transform.SetParent(rollPivot, false);
        hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        hub.transform.localScale = new Vector3(radius * 1.1f, width * 0.56f, radius * 1.1f);
        hub.GetComponent<Renderer>().sharedMaterial = hubMaterial;
        Object.DestroyImmediate(hub.GetComponent<Collider>());

        GameObject probe = new GameObject("ContactProbe");
        probe.transform.SetParent(root.transform, false);
        probe.transform.localPosition = new Vector3(0f, -radius, 0f);

        SphereCollider socketCollider = root.AddComponent<SphereCollider>();
        socketCollider.radius = radius;
        socketCollider.isTrigger = false;

        assembly.SetPivots(steerPivot, rollPivot, probe.transform);
        assembly.SetPhysicsCollider(socketCollider);
        GameObject prefab = SavePrefab(root, $"{VehicleFolder}/{prefabName}.prefab");
        return prefab.GetComponent<WheelAssembly>();
    }

    private static VehicleController CreateVehicleRigPrefab()
    {
        GameObject root = new GameObject("PF_VehicleRig");
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 900f;
        rb.linearDamping = 0.25f;
        rb.angularDamping = 1.8f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Transform generatedBodyRoot = CreateChild(root.transform, "GeneratedBodyRoot", Vector3.zero);
        Transform colliderRoot = CreateChild(root.transform, "ColliderRoot", Vector3.zero);
        Transform wheelSocketsRoot = CreateChild(root.transform, "WheelSockets", Vector3.zero);
        Transform cameraTarget = CreateChild(root.transform, "CameraTarget", new Vector3(0f, 1.25f, -0.2f));
        Transform driverSeat = CreateChild(root.transform, "DriverSeat", new Vector3(0f, 0.95f, -0.45f));

        BoxCollider bodyCollider = colliderRoot.gameObject.AddComponent<BoxCollider>();
        bodyCollider.center = new Vector3(0f, 0.62f, 0f);
        bodyCollider.size = new Vector3(2.15f, 0.78f, 3.65f);

        CapsuleCollider cabinCollider = colliderRoot.gameObject.AddComponent<CapsuleCollider>();
        cabinCollider.center = new Vector3(0f, 1.18f, -0.25f);
        cabinCollider.radius = 0.55f;
        cabinCollider.height = 1.55f;
        cabinCollider.direction = 2;

        WheelSocket fl = CreateSocket(wheelSocketsRoot, "FL", WheelSocketId.FL, new Vector3(-1.12f, 0.42f, 1.28f), true);
        WheelSocket fr = CreateSocket(wheelSocketsRoot, "FR", WheelSocketId.FR, new Vector3(1.12f, 0.42f, 1.28f), true);
        WheelSocket rl = CreateSocket(wheelSocketsRoot, "RL", WheelSocketId.RL, new Vector3(-1.12f, 0.42f, -1.28f), false);
        WheelSocket rr = CreateSocket(wheelSocketsRoot, "RR", WheelSocketId.RR, new Vector3(1.12f, 0.42f, -1.28f), false);

        VehicleController controller = root.AddComponent<VehicleController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("body").objectReferenceValue = rb;
        serialized.FindProperty("generatedBodyRoot").objectReferenceValue = generatedBodyRoot;
        serialized.FindProperty("cameraTarget").objectReferenceValue = cameraTarget;
        serialized.FindProperty("driverSeat").objectReferenceValue = driverSeat;
        serialized.FindProperty("bodyCollider").objectReferenceValue = bodyCollider;
        serialized.FindProperty("cabinCollider").objectReferenceValue = cabinCollider;
        SerializedProperty sockets = serialized.FindProperty("wheelSockets");
        sockets.arraySize = 4;
        sockets.GetArrayElementAtIndex(0).objectReferenceValue = fl;
        sockets.GetArrayElementAtIndex(1).objectReferenceValue = fr;
        sockets.GetArrayElementAtIndex(2).objectReferenceValue = rl;
        sockets.GetArrayElementAtIndex(3).objectReferenceValue = rr;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = SavePrefab(root, $"{VehicleFolder}/PF_VehicleRig.prefab");
        return prefab.GetComponent<VehicleController>();
    }

    private static WheelSocket CreateSocket(Transform parent, string name, WheelSocketId id, Vector3 localPosition, bool canSteer)
    {
        GameObject socketObject = new GameObject(name);
        socketObject.transform.SetParent(parent, false);
        socketObject.transform.localPosition = localPosition;
        WheelSocket socket = socketObject.AddComponent<WheelSocket>();
        socket.Configure(id, canSteer);
        return socket;
    }

    private static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        return child.transform;
    }

    private static void BuildScene(
        VehicleController rigPrefab,
        WheelAssembly smallWheelPrefab,
        WheelAssembly mediumWheelPrefab,
        GameObject bodyPrefab,
        Material groundMaterial,
        Material obstacleMaterial)
    {
        EnsureSceneLoaded();
        ClearGeneratedSceneObjects();

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "RideTest_Ground";
        ground.transform.position = new Vector3(0f, -0.05f, 0f);
        ground.transform.localScale = new Vector3(120f, 0.1f, 120f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

        CreateRamp(obstacleMaterial);
        CreateObstacle("RideTest_Obstacle_Box_A", new Vector3(4.8f, 0.5f, 5.5f), new Vector3(1.2f, 1f, 1.2f), obstacleMaterial);
        CreateObstacle("RideTest_Obstacle_Box_B", new Vector3(-4.2f, 0.35f, 7.5f), new Vector3(2.2f, 0.7f, 0.8f), obstacleMaterial);
        CreateAssemblyArea();

        Transform spawnPoint = CreateMarker("VehicleSpawnPoint", new Vector3(0f, 0f, 0f), Quaternion.identity);
        Transform generationCameraPose = CreateMarker(
            "VehicleGenerationCameraPose",
            new Vector3(3.8f, 2.4f, -5.4f),
            LookAtRotation(new Vector3(3.8f, 2.4f, -5.4f), spawnPoint.position + Vector3.up * 0.9f));
        Transform assemblyCameraPose = CreateMarker(
            "AssemblyCameraPose",
            new Vector3(0f, 5.1f, -8.2f),
            Quaternion.Euler(57f, 0f, 0f));

        GameObject mainCameraObject = GetMainCamera();
        BuildOrbitCamera buildCamera = mainCameraObject.GetComponent<BuildOrbitCamera>();
        if (buildCamera == null)
        {
            buildCamera = mainCameraObject.AddComponent<BuildOrbitCamera>();
        }
        SerializedObject buildCameraObject = new SerializedObject(buildCamera);
        buildCameraObject.FindProperty("zoomSensitivity").floatValue = 0.5f;
        buildCameraObject.ApplyModifiedPropertiesWithoutUndo();
        buildCamera.enabled = false;

        VehicleFollowCamera followCamera = mainCameraObject.GetComponent<VehicleFollowCamera>();
        if (followCamera == null)
        {
            followCamera = mainCameraObject.AddComponent<VehicleFollowCamera>();
        }
        followCamera.enabled = false;

        GameObject systems = new GameObject("VehicleDemoSystem");
        LocalPrefabBodyProvider provider = systems.AddComponent<LocalPrefabBodyProvider>();
        VehicleModeCoordinator modeCoordinator = systems.AddComponent<VehicleModeCoordinator>();
        VehicleAssemblyManager manager = systems.AddComponent<VehicleAssemblyManager>();
        BuildSelectionController selectionController = systems.AddComponent<BuildSelectionController>();
        systems.AddComponent<VehicleDemoUI>();

        SerializedObject providerObject = new SerializedObject(provider);
        SerializedProperty bodies = providerObject.FindProperty("bodyPrefabs");
        bodies.arraySize = 1;
        bodies.GetArrayElementAtIndex(0).objectReferenceValue = bodyPrefab;
        providerObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject managerObject = new SerializedObject(manager);
        managerObject.FindProperty("vehicleRigPrefab").objectReferenceValue = rigPrefab;
        managerObject.FindProperty("bodyProvider").objectReferenceValue = provider;
        managerObject.FindProperty("smallWheelPrefab").objectReferenceValue = smallWheelPrefab;
        managerObject.FindProperty("mediumWheelPrefab").objectReferenceValue = mediumWheelPrefab;
        managerObject.FindProperty("spawnPoint").objectReferenceValue = spawnPoint;
        managerObject.FindProperty("mainCamera").objectReferenceValue = mainCameraObject.GetComponent<Camera>();
        managerObject.FindProperty("generationCameraPose").objectReferenceValue = generationCameraPose;
        managerObject.FindProperty("assemblyCameraPose").objectReferenceValue = assemblyCameraPose;
        managerObject.FindProperty("buildCamera").objectReferenceValue = buildCamera;
        managerObject.FindProperty("driveCamera").objectReferenceValue = followCamera;
        GameObject player = GameObject.Find("PF_Player_ThirdPerson") ?? GameObject.Find("Player_ThirdPerson");
        managerObject.FindProperty("playerObject").objectReferenceValue = player;
        managerObject.FindProperty("modeCoordinator").objectReferenceValue = modeCoordinator;
        managerObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject modeObject = new SerializedObject(modeCoordinator);
        modeObject.FindProperty("mainCamera").objectReferenceValue = mainCameraObject.GetComponent<Camera>();
        modeObject.FindProperty("generationCameraPose").objectReferenceValue = generationCameraPose;
        modeObject.FindProperty("assemblyCameraPose").objectReferenceValue = assemblyCameraPose;
        modeObject.FindProperty("buildCamera").objectReferenceValue = buildCamera;
        modeObject.FindProperty("driveCamera").objectReferenceValue = followCamera;
        modeObject.FindProperty("playerObject").objectReferenceValue = player;
        modeObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject selectionObject = new SerializedObject(selectionController);
        selectionObject.FindProperty("manager").objectReferenceValue = manager;
        selectionObject.FindProperty("targetCamera").objectReferenceValue = mainCameraObject.GetComponent<Camera>();
        selectionObject.ApplyModifiedPropertiesWithoutUndo();

        CreateOrUpdateLight();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    private static void EnsureSceneLoaded()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }

    private static void ClearGeneratedSceneObjects()
    {
        string[] names =
        {
            "RideTest_Ground",
            "RideTest_Ramp",
            "RideTest_Obstacle_Box_A",
            "RideTest_Obstacle_Box_B",
            "AssemblyArea",
            "VehicleSpawnPoint",
            "VehicleGenerationCameraPose",
            "AssemblyCameraPose",
            "VehicleDemoSystem",
            "Runtime_VehicleRig"
        };

        for (int i = 0; i < names.Length; i++)
        {
            GameObject existing = GameObject.Find(names[i]);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }
    }

    private static void CreateRamp(Material material)
    {
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "RideTest_Ramp";
        ramp.transform.position = new Vector3(0f, 0.35f, 8.5f);
        ramp.transform.rotation = Quaternion.Euler(-16f, 0f, 0f);
        ramp.transform.localScale = new Vector3(4.8f, 0.25f, 5.2f);
        ramp.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CreateObstacle(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = name;
        obstacle.transform.position = position;
        obstacle.transform.localScale = scale;
        obstacle.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CreateAssemblyArea()
    {
        GameObject area = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        area.name = "AssemblyArea";
        area.transform.position = new Vector3(0f, 0.01f, 0f);
        area.transform.localScale = new Vector3(3.3f, 0.02f, 3.3f);
        area.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Assembly_Area.mat", new Color(0.15f, 0.75f, 0.95f));
        Object.DestroyImmediate(area.GetComponent<Collider>());
    }

    private static Transform CreateMarker(string name, Vector3 position, Quaternion rotation)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetPositionAndRotation(position, rotation);
        return marker.transform;
    }

    private static Quaternion LookAtRotation(Vector3 position, Vector3 focus)
    {
        return Quaternion.LookRotation(focus - position, Vector3.up);
    }

    private static GameObject GetMainCamera()
    {
        Camera main = Camera.main;
        GameObject cameraObject = main != null ? main.gameObject : GameObject.Find("Main Camera");
        if (cameraObject == null)
        {
            cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        }

        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.fieldOfView = 62f;
        cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 5.1f, -8.2f), Quaternion.Euler(57f, 0f, 0f));
        return cameraObject;
    }

    private static void CreateOrUpdateLight()
    {
        GameObject lightObject = GameObject.Find("Directional Light");
        if (lightObject == null)
        {
            lightObject = new GameObject("Directional Light", typeof(Light));
        }

        Light light = lightObject.GetComponent<Light>();
        if (light == null)
        {
            light = lightObject.AddComponent<Light>();
        }

        light.type = LightType.Directional;
        light.intensity = 2.2f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
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

    private static GameObject SavePrefab(GameObject root, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }
}
