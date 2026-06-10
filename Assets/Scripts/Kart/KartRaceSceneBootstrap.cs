using UnityEngine;

public sealed class KartRaceSceneBootstrap : MonoBehaviour
{
    [SerializeField] private KartController playerKart;
    [SerializeField] private KartPlayerInputSource playerInputSource;
    [SerializeField] private KartRaceManager raceManager;
    [SerializeField] private KartWheelSet wheelSet;
    [SerializeField] private KartHud hud;
    [SerializeField] private SpeedDashboardGaugeUi speedDashboardGauge;
    [SerializeField] private KartScreenSpeedLines screenSpeedLines;
    [SerializeField] private KartTopSpeedUi topSpeedUi;
    [SerializeField] private bool enableLegacyHud;
    [SerializeField] private bool enableInputOnStart = true;

    private void Start()
    {
        ResolveReferences();
        BindRaceSystems();
        BindPresentation();
        ApplyPlayerSetup();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ResolveReferences()
    {
        if (playerKart == null)
        {
            playerKart = FindAnyObjectByType<KartController>();
        }

        if (playerInputSource == null)
        {
            playerInputSource = FindAnyObjectByType<KartPlayerInputSource>();
        }

        if (raceManager == null)
        {
            raceManager = FindAnyObjectByType<KartRaceManager>();
        }

        if (wheelSet == null)
        {
            wheelSet = FindAnyObjectByType<KartWheelSet>();
        }

        if (hud == null)
        {
            hud = FindAnyObjectByType<KartHud>();
        }

        if (speedDashboardGauge == null)
        {
            speedDashboardGauge = FindAnyObjectByType<SpeedDashboardGaugeUi>(FindObjectsInactive.Include);
        }

        if (screenSpeedLines == null)
        {
            screenSpeedLines = FindAnyObjectByType<KartScreenSpeedLines>(FindObjectsInactive.Include);
        }

        if (topSpeedUi == null)
        {
            topSpeedUi = FindAnyObjectByType<KartTopSpeedUi>(FindObjectsInactive.Include);
        }
    }

    private void BindRaceSystems()
    {
        if (raceManager != null)
        {
            raceManager.SetPlayerKart(playerKart);
            KartCheckpoint[] checkpoints = FindObjectsByType<KartCheckpoint>(FindObjectsInactive.Include);
            for (int i = 0; i < checkpoints.Length; i++)
            {
                checkpoints[i].BindRaceManager(raceManager);
            }
        }

        if (playerKart != null && playerInputSource != null)
        {
            playerKart.SetInputSource(playerInputSource);
        }
    }

    private void BindPresentation()
    {
        if (hud != null)
        {
            hud.enabled = enableLegacyHud;
        }

        if (screenSpeedLines != null)
        {
            screenSpeedLines.PlayerKart = playerKart;
            screenSpeedLines.ApplyDriveConfig(playerKart != null ? playerKart.DriveConfig : null);
            screenSpeedLines.gameObject.SetActive(true);
            screenSpeedLines.transform.SetAsFirstSibling();
        }

        if (speedDashboardGauge != null)
        {
            speedDashboardGauge.PlayerKart = playerKart;
        }

        if (topSpeedUi != null)
        {
            topSpeedUi.PlayerKart = playerKart;
            topSpeedUi.gameObject.SetActive(speedDashboardGauge == null || !speedDashboardGauge.ShowsNumericSpeed);
        }
    }

    private void ApplyPlayerSetup()
    {
        wheelSet?.ApplyWheelSize(KartGameState.SelectedTireSize);

        if (playerKart != null)
        {
            playerKart.SetInputEnabled(enableInputOnStart);
        }
    }
}
