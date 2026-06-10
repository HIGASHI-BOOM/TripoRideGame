using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VehicleMvpBuilder
{
    private const string VehicleFolder = "Assets/Prefabs/Vehicles";
    private const string MaterialFolder = "Assets/Materials/Vehicles";
    private const string ConfigFolder = "Assets/ScriptableObjects/Vehicles";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string SimulationScenePath = "Assets/Scenes/VehicleSimulationMap.unity";
    private const string DefaultSimulationConfigPath = ConfigFolder + "/SC_BuggyBalanced.asset";

    [MenuItem("Tools/TripoRide/Rebuild Local Vehicle MVP")]
    public static void BuildAll()
    {
        BuildVehicleScene(ScenePath, includeSimulationCourse: false);
    }

    [MenuItem("Tools/TripoRide/Build Vehicle Simulation Map")]
    public static void BuildSimulationMap()
    {
        BuildVehicleScene(SimulationScenePath, includeSimulationCourse: true);
    }

    private static void BuildVehicleScene(string scenePath, bool includeSimulationCourse)
    {
        EnsureFolders();
        VehicleSimulationConfig simulationConfig = GetOrCreateSimulationConfig(DefaultSimulationConfigPath);
        Material bodyMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Vehicle_Body.mat", new Color(0.15f, 0.42f, 0.95f));
        Material glassMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Vehicle_Glass.mat", new Color(0.08f, 0.18f, 0.24f));
        Material tireMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Wheel_Tire.mat", new Color(0.025f, 0.025f, 0.025f));
        Material hubMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Wheel_Hub.mat", new Color(0.72f, 0.76f, 0.78f));
        Material groundMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Test_Ground.mat", new Color(0.26f, 0.42f, 0.27f));
        Material roadMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Test_Road.mat", new Color(0.12f, 0.13f, 0.14f));
        Material barrierMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Test_Barrier.mat", new Color(0.82f, 0.84f, 0.78f));
        Material obstacleMaterial = GetOrCreateMaterial($"{MaterialFolder}/M_Test_Obstacle.mat", new Color(0.9f, 0.55f, 0.15f));

        GameObject localBodyPrefab = CreateLocalBodyPrefab(bodyMaterial, glassMaterial);
        WheelAssembly smallWheelPrefab = CreateWheelPrefab("PF_Wheel_Small", WheelSize.Small, 0.32f, 0.22f, tireMaterial, hubMaterial);
        WheelAssembly mediumWheelPrefab = CreateWheelPrefab("PF_Wheel_Medium", WheelSize.Medium, 0.42f, 0.28f, tireMaterial, hubMaterial);
        VehicleController rigPrefab = CreateVehicleRigPrefab(simulationConfig);

        BuildScene(
            scenePath,
            rigPrefab,
            smallWheelPrefab,
            mediumWheelPrefab,
            localBodyPrefab,
            simulationConfig,
            groundMaterial,
            roadMaterial,
            barrierMaterial,
            obstacleMaterial,
            includeSimulationCourse);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Vehicle scene rebuilt at {scenePath}: rig prefab, wheels, simulation config, map, and UI are ready.");
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets", "Prefabs");
        CreateFolder("Assets/Prefabs", "Vehicles");
        CreateFolder("Assets", "Materials");
        CreateFolder("Assets/Materials", "Vehicles");
        CreateFolder("Assets", "ScriptableObjects");
        CreateFolder("Assets/ScriptableObjects", "Vehicles");
        CreateFolder("Assets", "Scenes");
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

    private static VehicleController CreateVehicleRigPrefab(VehicleSimulationConfig simulationConfig)
    {
        GameObject root = new GameObject("PF_VehicleRig");
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 900f;
        rb.linearDamping = 0.25f;
        rb.angularDamping = 2.4f;
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
        serialized.FindProperty("simulationConfig").objectReferenceValue = simulationConfig;
        serialized.FindProperty("body").objectReferenceValue = rb;
        serialized.FindProperty("generatedBodyRoot").objectReferenceValue = generatedBodyRoot;
        serialized.FindProperty("cameraTarget").objectReferenceValue = cameraTarget;
        serialized.FindProperty("driverSeat").objectReferenceValue = driverSeat;
        serialized.FindProperty("bodyCollider").objectReferenceValue = bodyCollider;
        serialized.FindProperty("cabinCollider").objectReferenceValue = cabinCollider;
        serialized.FindProperty("alignWheelSocketsByBodyBottom").boolValue = true;
        serialized.FindProperty("wheelBodyBottomClearance").floatValue = 0f;
        serialized.FindProperty("fallbackWheelRadius").floatValue = 0.42f;
        serialized.FindProperty("maxSteerAngle").floatValue = 10f;
        serialized.FindProperty("highSpeedSteerAngle").floatValue = 3f;
        serialized.FindProperty("steerSharpness").floatValue = 5f;
        serialized.FindProperty("frontTireGrip").floatValue = 1.6f;
        serialized.FindProperty("rearTireGrip").floatValue = 14f;
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
        string scenePath,
        VehicleController rigPrefab,
        WheelAssembly smallWheelPrefab,
        WheelAssembly mediumWheelPrefab,
        GameObject bodyPrefab,
        VehicleSimulationConfig simulationConfig,
        Material groundMaterial,
        Material roadMaterial,
        Material barrierMaterial,
        Material obstacleMaterial,
        bool includeSimulationCourse)
    {
        EnsureSceneLoaded(scenePath);
        ClearGeneratedSceneObjects();

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "RideTest_Ground";
        ground.transform.position = new Vector3(0f, -0.05f, 0f);
        ground.transform.localScale = new Vector3(120f, 0.1f, 120f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

        CreateRamp(obstacleMaterial);
        CreateObstacle("RideTest_Obstacle_Box_A", new Vector3(4.8f, 0.5f, 5.5f), new Vector3(1.2f, 1f, 1.2f), obstacleMaterial);
        CreateObstacle("RideTest_Obstacle_Box_B", new Vector3(-4.2f, 0.35f, 7.5f), new Vector3(2.2f, 0.7f, 0.8f), obstacleMaterial);
        if (includeSimulationCourse)
        {
            CreateSimulationCourse(roadMaterial, barrierMaterial, obstacleMaterial);
        }
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
        LocalPrefabBodyProvider localProvider = systems.AddComponent<LocalPrefabBodyProvider>();
        TripoVehicleBodyProvider provider = systems.AddComponent<TripoVehicleBodyProvider>();
        VehicleModeCoordinator modeCoordinator = systems.AddComponent<VehicleModeCoordinator>();
        VehicleAssemblyManager manager = systems.AddComponent<VehicleAssemblyManager>();
        BuildSelectionController selectionController = systems.AddComponent<BuildSelectionController>();
        VehicleDemoUI demoUI = systems.AddComponent<VehicleDemoUI>();

        SerializedObject providerObject = new SerializedObject(localProvider);
        SerializedProperty bodies = providerObject.FindProperty("bodyPrefabs");
        bodies.arraySize = 1;
        bodies.GetArrayElementAtIndex(0).objectReferenceValue = bodyPrefab;
        providerObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject tripoProviderObject = new SerializedObject(provider);
        tripoProviderObject.FindProperty("model").stringValue = "P1-20260311";
        tripoProviderObject.FindProperty("faceLimit").intValue = 3000;
        tripoProviderObject.FindProperty("texture").boolValue = true;
        tripoProviderObject.FindProperty("pbr").boolValue = true;
        tripoProviderObject.FindProperty("exportUv").boolValue = true;
        tripoProviderObject.FindProperty("convertToFbx").boolValue = true;
        tripoProviderObject.FindProperty("rigModel").stringValue = "v2.5-20260210";
        tripoProviderObject.FindProperty("rigType").stringValue = "quadruped";
        tripoProviderObject.FindProperty("rigSpec").stringValue = "tripo";
        tripoProviderObject.FindProperty("animationPreset").stringValue = "preset:quadruped:walk";
        tripoProviderObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject managerObject = new SerializedObject(manager);
        managerObject.FindProperty("vehicleRigPrefab").objectReferenceValue = rigPrefab;
        managerObject.FindProperty("vehicleSimulationConfig").objectReferenceValue = simulationConfig;
        managerObject.FindProperty("bodyProvider").objectReferenceValue = localProvider;
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

        SerializedObject uiObject = new SerializedObject(demoUI);
        uiObject.FindProperty("manager").objectReferenceValue = manager;
        uiObject.FindProperty("localProvider").objectReferenceValue = localProvider;
        uiObject.FindProperty("tripoProvider").objectReferenceValue = provider;
        uiObject.FindProperty("tripoMode").enumValueIndex = 0;
        uiObject.ApplyModifiedPropertiesWithoutUndo();

        CreateOrUpdateLight();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    private static void EnsureSceneLoaded(string scenePath)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path == scenePath)
        {
            return;
        }

        if (File.Exists(scenePath))
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            return;
        }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
    }

    private static void ClearGeneratedSceneObjects()
    {
        GameObject[] generatedObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        for (int i = 0; i < generatedObjects.Length; i++)
        {
            string objectName = generatedObjects[i].name;
            if (objectName.StartsWith("RideTest_") || objectName.StartsWith("RideSim_"))
            {
                Object.DestroyImmediate(generatedObjects[i]);
            }
        }

        string[] names =
        {
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

    private static void CreateSimulationCourse(Material roadMaterial, Material barrierMaterial, Material obstacleMaterial)
    {
        CreateRoad("RideSim_Road_StartPad", new Vector3(0f, 0.015f, 0f), new Vector3(9f, 0.03f, 12f), roadMaterial);
        CreateRoad("RideSim_Road_Acceleration", new Vector3(0f, 0.018f, 23f), new Vector3(7.5f, 0.03f, 34f), roadMaterial);
        CreateRoad("RideSim_Road_Braking", new Vector3(0f, 0.019f, 47f), new Vector3(9.5f, 0.03f, 12f), roadMaterial);
        CreateRoad("RideSim_Road_LeftTurn", new Vector3(-15f, 0.018f, 47f), new Vector3(25f, 0.03f, 7.5f), roadMaterial);
        CreateRoad("RideSim_Road_Return", new Vector3(-27f, 0.018f, 21f), new Vector3(7.5f, 0.03f, 38f), roadMaterial);
        CreateRoad("RideSim_Road_Slalom", new Vector3(-12f, 0.018f, 2f), new Vector3(24f, 0.03f, 7.5f), roadMaterial);

        CreateBarrier("RideSim_Barrier_Start_L", new Vector3(-5.1f, 0.35f, 22f), new Vector3(0.3f, 0.7f, 42f), barrierMaterial);
        CreateBarrier("RideSim_Barrier_Start_R", new Vector3(5.1f, 0.35f, 22f), new Vector3(0.3f, 0.7f, 42f), barrierMaterial);
        CreateBarrier("RideSim_Barrier_Return_L", new Vector3(-32.1f, 0.35f, 22f), new Vector3(0.3f, 0.7f, 42f), barrierMaterial);
        CreateBarrier("RideSim_Barrier_Return_R", new Vector3(-21.9f, 0.35f, 22f), new Vector3(0.3f, 0.7f, 42f), barrierMaterial);
        CreateBarrier("RideSim_Barrier_Turn_Outer", new Vector3(-15f, 0.35f, 52.1f), new Vector3(27f, 0.7f, 0.3f), barrierMaterial);

        CreateObstacle("RideSim_SpeedBump_A", new Vector3(0f, 0.14f, 36f), new Vector3(6f, 0.28f, 0.45f), obstacleMaterial);
        CreateObstacle("RideSim_SpeedBump_B", new Vector3(0f, 0.11f, 39f), new Vector3(6f, 0.22f, 0.45f), obstacleMaterial);
        CreateObstacle("RideSim_BrakeMarker_25m", new Vector3(-3.2f, 0.18f, 43f), new Vector3(0.35f, 0.36f, 4f), obstacleMaterial);
        CreateObstacle("RideSim_BrakeMarker_10m", new Vector3(3.2f, 0.18f, 49f), new Vector3(0.35f, 0.36f, 4f), obstacleMaterial);

        for (int i = 0; i < 6; i++)
        {
            float x = -22f + i * 4f;
            float z = i % 2 == 0 ? -1.8f : 5.8f;
            CreateCone($"RideSim_SlalomCone_{i + 1}", new Vector3(x, 0.45f, z), obstacleMaterial);
        }
    }

    private static void CreateRoad(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = name;
        road.transform.position = position;
        road.transform.localScale = scale;
        road.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(road.GetComponent<Collider>());
    }

    private static void CreateBarrier(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrier.name = name;
        barrier.transform.position = position;
        barrier.transform.localScale = scale;
        barrier.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CreateCone(string name, Vector3 position, Material material)
    {
        GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cone.name = name;
        cone.transform.position = position;
        cone.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
        cone.GetComponent<Renderer>().sharedMaterial = material;
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

    private static VehicleSimulationConfig GetOrCreateSimulationConfig(string path)
    {
        VehicleSimulationConfig config = AssetDatabase.LoadAssetAtPath<VehicleSimulationConfig>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<VehicleSimulationConfig>();
            AssetDatabase.CreateAsset(config, path);
        }

        config.mass = 980f;
        config.centerOfMass = new Vector3(0f, -0.36f, 0.08f);
        config.fitCenterOfMassToGeneratedBody = true;
        config.generatedCenterOfMassHeight = 0.17f;
        config.idleDrag = 0.22f;
        config.brakeDrag = 7.5f;
        config.driveAngularDamping = 2.4f;
        config.parkedAngularDamping = 4f;
        config.driveForce = 6200f;
        config.reverseForce = 3200f;
        config.maxFlatSpeed = 41.666668f;
        config.maxReverseSpeed = 13.888889f;
        config.maxSteerAngle = 14f;
        config.highSpeedSteerAngle = 4f;
        config.steerSharpness = 7f;
        config.frontTireGrip = 3.2f;
        config.rearTireGrip = 16f;
        config.brakeForce = 10500f;
        config.autoFitSocketsToGeneratedBody = true;
        config.alignWheelSocketsByBodyBottom = true;
        config.wheelBodyBottomClearance = 0f;
        config.fallbackWheelRadius = 0.42f;
        config.chassisWidthScale = 0.9f;
        config.chassisLengthScale = 0.92f;
        config.chassisHeightFraction = 0.42f;
        config.chassisMinHeight = 0.45f;
        config.useUpperCabinCollider = false;
        config.speedDownforce = 8f;
        config.uprightStabilization = 0.22f;
        EditorUtility.SetDirty(config);
        return config;
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
