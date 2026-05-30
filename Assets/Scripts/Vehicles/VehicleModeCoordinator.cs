using UnityEngine;

public sealed class VehicleModeCoordinator : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform generationCameraPose;
    [SerializeField] private Transform assemblyCameraPose;
    [SerializeField] private BuildOrbitCamera buildCamera;
    [SerializeField] private VehicleFollowCamera driveCamera;
    [SerializeField] private GameObject playerObject;

    private SimpleThirdPersonCamera playerCamera;
    private SimpleThirdPersonController playerController;

    public void Configure(
        Camera camera,
        Transform generationPose,
        Transform assemblyPose,
        BuildOrbitCamera orbitCamera,
        VehicleFollowCamera followCamera,
        GameObject player)
    {
        mainCamera = camera != null ? camera : mainCamera;
        generationCameraPose = generationPose != null ? generationPose : generationCameraPose;
        assemblyCameraPose = assemblyPose != null ? assemblyPose : assemblyCameraPose;
        buildCamera = orbitCamera != null ? orbitCamera : buildCamera;
        driveCamera = followCamera != null ? followCamera : driveCamera;
        playerObject = player != null ? player : playerObject;
        CachePlayerControl();
    }

    public void EnterBuildView(Transform target, Vector3 fallbackFocus)
    {
        GameInputContext.SetMode(GameInputMode.Build);
        SetPlayerControlEnabled(false);

        if (driveCamera != null)
        {
            driveCamera.enabled = false;
        }

        if (buildCamera != null)
        {
            buildCamera.enabled = true;
            buildCamera.SetTarget(target, snapCamera: true);
            return;
        }

        if (playerCamera != null)
        {
            playerCamera.enabled = false;
        }

        Transform cameraPose = generationCameraPose != null ? generationCameraPose : assemblyCameraPose;
        if (mainCamera != null && cameraPose != null)
        {
            mainCamera.transform.position = cameraPose.position;
            mainCamera.transform.rotation = Quaternion.LookRotation(fallbackFocus - cameraPose.position, Vector3.up);
        }
    }

    public void EnterDrivingView(Transform target)
    {
        GameInputContext.SetMode(GameInputMode.Driving);
        SetPlayerControlEnabled(false);

        if (playerObject != null)
        {
            playerObject.SetActive(false);
        }

        if (driveCamera != null)
        {
            driveCamera.Target = target;
            driveCamera.enabled = true;
        }

        if (buildCamera != null)
        {
            buildCamera.enabled = false;
        }

        if (playerCamera != null)
        {
            playerCamera.enabled = false;
        }
    }

    public void EnterPlayerView()
    {
        GameInputContext.SetMode(GameInputMode.Player);

        if (driveCamera != null)
        {
            driveCamera.enabled = false;
        }

        if (buildCamera != null)
        {
            buildCamera.enabled = false;
        }

        if (playerObject != null)
        {
            playerObject.SetActive(true);
        }

        SetPlayerControlEnabled(true);
    }

    private void CachePlayerControl()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            playerCamera = mainCamera.GetComponent<SimpleThirdPersonCamera>();
            if (buildCamera == null)
            {
                buildCamera = mainCamera.GetComponent<BuildOrbitCamera>();
            }
        }

        if (playerObject == null)
        {
            playerObject = GameObject.FindWithTag("Player");
        }

        if (playerObject != null)
        {
            playerController = playerObject.GetComponent<SimpleThirdPersonController>();
        }

        if (playerController == null)
        {
            playerController = FindAnyObjectByType<SimpleThirdPersonController>(FindObjectsInactive.Include);
            if (playerController != null)
            {
                playerObject = playerController.gameObject;
            }
        }
    }

    private void SetPlayerControlEnabled(bool enabled)
    {
        CachePlayerControl();

        if (playerController != null)
        {
            playerController.enabled = enabled;
        }

        if (playerCamera != null)
        {
            playerCamera.enabled = enabled;
        }
    }
}
