using UnityEngine;

public sealed class VehicleAssemblyManager : MonoBehaviour
{
    [SerializeField] private VehicleController vehicleRigPrefab;
    [SerializeField] private VehicleSimulationConfig vehicleSimulationConfig;
    [SerializeField] private GeneratedBodyProvider bodyProvider;
    [SerializeField] private WheelAssembly smallWheelPrefab;
    [SerializeField] private WheelAssembly mediumWheelPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform generationCameraPose;
    [SerializeField] private Transform assemblyCameraPose;
    [SerializeField] private BuildOrbitCamera buildCamera;
    [SerializeField] private VehicleFollowCamera driveCamera;
    [SerializeField] private GameObject playerObject;
    [SerializeField] private VehicleModeCoordinator modeCoordinator;
    [SerializeField] private VehicleBuildData buildData = new VehicleBuildData();

    private VehicleController currentVehicle;
    private WheelSize selectedWheelSize = WheelSize.Medium;
    private bool vehicleGenerationMode;
    private bool driving;

    public VehicleController CurrentVehicle => currentVehicle;
    public WheelSize SelectedWheelSize => selectedWheelSize;
    public bool VehicleGenerationMode => vehicleGenerationMode;
    public bool Driving => driving;
    public VehicleBuildData BuildData => buildData;
    public VehicleSimulationConfig VehicleSimulationConfig => vehicleSimulationConfig;

    public void SetBodyProvider(GeneratedBodyProvider provider)
    {
        if (provider != null)
        {
            bodyProvider = provider;
        }
    }

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            if (buildCamera == null)
            {
                buildCamera = mainCamera.GetComponent<BuildOrbitCamera>();
            }
        }

        if (buildCamera != null)
        {
            buildCamera.enabled = false;
        }

        if (driveCamera != null)
        {
            driveCamera.enabled = false;
        }

        if (modeCoordinator == null)
        {
            modeCoordinator = GetComponent<VehicleModeCoordinator>();
        }

        if (modeCoordinator == null)
        {
            modeCoordinator = gameObject.AddComponent<VehicleModeCoordinator>();
        }

        modeCoordinator.Configure(
            mainCamera,
            generationCameraPose,
            assemblyCameraPose,
            buildCamera,
            driveCamera,
            playerObject);
    }

    public void EnterVehicleGenerationMode()
    {
        vehicleGenerationMode = true;
        driving = false;
        SetGenerationView();
    }

    public void GenerateVehicle()
    {
        if (!vehicleGenerationMode)
        {
            EnterVehicleGenerationMode();
        }

        DestroyCurrentVehicle();
        buildData.ClearWheels();

        if (vehicleRigPrefab == null)
        {
            Debug.LogError("Vehicle rig prefab is missing.");
            return;
        }

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        currentVehicle = Instantiate(vehicleRigPrefab, position, rotation);
        currentVehicle.name = "Runtime_VehicleRig";
        currentVehicle.SetSimulationConfig(vehicleSimulationConfig);
        currentVehicle.SetDriveEnabled(false);

        if (bodyProvider != null && currentVehicle.GeneratedBodyRoot != null)
        {
            GeneratedVehicleBody generatedBody = bodyProvider.CreateBody(currentVehicle.GeneratedBodyRoot, buildData);
            currentVehicle.ApplyGeneratedBody(generatedBody);
        }

        SetGenerationView();
    }

    public void SelectSmallWheel()
    {
        selectedWheelSize = WheelSize.Small;
    }

    public void SelectMediumWheel()
    {
        selectedWheelSize = WheelSize.Medium;
    }

    public void InstallNextWheel()
    {
        if (currentVehicle == null)
        {
            return;
        }

        WheelSocket socket = currentVehicle.GetFirstEmptySocket();
        if (socket == null)
        {
            return;
        }

        WheelAssembly prefab = selectedWheelSize == WheelSize.Small ? smallWheelPrefab : mediumWheelPrefab;
        WheelAssembly wheel = socket.InstallWheel(prefab);
        if (wheel != null)
        {
            buildData.SetWheel(socket.SocketId, selectedWheelSize);
            currentVehicle.RefitGeneratedBodyAttachments();
        }

        currentVehicle.SnapToWheelGround();
        currentVehicle.SetDriveEnabled(false);
    }

    public void InstallAllWheels()
    {
        if (currentVehicle == null)
        {
            return;
        }

        while (currentVehicle.GetFirstEmptySocket() != null)
        {
            InstallNextWheel();
        }
    }

    public void RemoveAllWheels()
    {
        if (currentVehicle == null)
        {
            return;
        }

        currentVehicle.ClearWheels();
        buildData.ClearWheels();
        driving = false;
        GameInputContext.SetMode(GameInputMode.Build);
        SetGenerationView();
    }

    public void RotateGeneratedBody(BuildRotationAxis axis, int direction)
    {
        if (currentVehicle == null || currentVehicle.GeneratedBodyRoot == null)
        {
            return;
        }

        BuildSelectableBody selectableBody = currentVehicle.GeneratedBodyRoot.GetComponentInChildren<BuildSelectableBody>();
        if (selectableBody == null)
        {
            return;
        }

        selectableBody.RotateStep(axis, direction);
        currentVehicle.ApplyGeneratedBody(new GeneratedVehicleBody(currentVehicle.GeneratedBodyRoot.gameObject));
        currentVehicle.RefitGeneratedBodyAttachments();
        currentVehicle.SnapToWheelGround();
    }

    public void StartDriving()
    {
        if (currentVehicle == null || !currentVehicle.HasAllWheels)
        {
            Debug.Log("Install all four wheels before driving.");
            return;
        }

        driving = true;
        vehicleGenerationMode = false;
        currentVehicle.SetDriveEnabled(true);
        modeCoordinator.EnterDrivingView(currentVehicle.CameraTarget);
    }

    public void ExitAndDestroyVehicle()
    {
        DestroyCurrentVehicle();

        vehicleGenerationMode = false;
        driving = false;
        modeCoordinator.EnterPlayerView();
    }

    private void DestroyCurrentVehicle()
    {
        if (currentVehicle != null)
        {
            Destroy(currentVehicle.gameObject);
            currentVehicle = null;
        }
    }

    private void SetGenerationView()
    {
        modeCoordinator.EnterBuildView(GetGenerationCameraTarget(), GetGenerationFocusPoint());
    }

    private Transform GetGenerationCameraTarget()
    {
        if (currentVehicle != null)
        {
            return currentVehicle.CameraTarget;
        }

        return spawnPoint != null ? spawnPoint : transform;
    }

    private Vector3 GetGenerationFocusPoint()
    {
        if (currentVehicle != null)
        {
            return currentVehicle.CameraTarget.position;
        }

        if (spawnPoint != null)
        {
            return spawnPoint.position + Vector3.up * 0.9f;
        }

        return transform.position + Vector3.up;
    }

}
