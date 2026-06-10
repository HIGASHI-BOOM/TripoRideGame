using UnityEngine;

public sealed class KartHud : MonoBehaviour
{
    [SerializeField] private KartRaceManager raceManager;
    [SerializeField] private KartController playerKart;

    private GUIStyle panelStyle;
    private GUIStyle labelStyle;

    private void Awake()
    {
        if (raceManager == null)
        {
            raceManager = FindAnyObjectByType<KartRaceManager>();
        }

        if (playerKart == null)
        {
            playerKart = raceManager != null ? raceManager.PlayerKart : FindAnyObjectByType<KartController>();
        }
    }

    private void OnGUI()
    {
        EnsureStyles();
        Rect panel = new Rect(16f, 16f, 260f, 112f);
        GUI.Box(panel, GUIContent.none, panelStyle);

        float speed = playerKart != null ? playerKart.SpeedKph : 0f;
        int lap = raceManager != null ? raceManager.CurrentLap : 1;
        int laps = raceManager != null ? raceManager.TargetLaps : 3;
        int checkpoint = raceManager != null ? raceManager.NextCheckpoint + 1 : 1;
        int checkpoints = raceManager != null ? raceManager.CheckpointCount : 1;

        GUI.Label(new Rect(32f, 24f, 220f, 24f), $"LAP {lap}/{laps}", labelStyle);
        GUI.Label(new Rect(32f, 52f, 220f, 24f), $"CHECKPOINT {checkpoint}/{checkpoints}", labelStyle);
        GUI.Label(new Rect(32f, 80f, 220f, 24f), $"{speed:000} KM/H", labelStyle);

        if (raceManager != null && raceManager.Finished)
        {
            GUI.Label(new Rect(Screen.width * 0.5f - 120f, 42f, 240f, 40f), "FINISH!", labelStyle);
        }
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
        {
            return;
        }

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = Texture2D.blackTexture }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
    }
}
