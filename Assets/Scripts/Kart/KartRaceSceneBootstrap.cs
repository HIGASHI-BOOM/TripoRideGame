using UnityEngine;
using UnityEngine.UI;

public sealed class KartRaceSceneBootstrap : MonoBehaviour
{
    [SerializeField] private KartController playerKart;
    [SerializeField] private KartWheelSet wheelSet;
    [SerializeField] private KartHud hud;
    [SerializeField] private bool enableLegacyHud;
    [SerializeField] private bool enableInputOnStart = true;

    private void Start()
    {
        if (playerKart == null)
        {
            playerKart = FindAnyObjectByType<KartController>();
        }

        if (wheelSet == null)
        {
            wheelSet = FindAnyObjectByType<KartWheelSet>();
        }

        if (hud == null)
        {
            hud = FindAnyObjectByType<KartHud>();
        }

        wheelSet?.ApplyWheelSize(KartGameState.SelectedTireSize);

        if (hud != null)
        {
            hud.enabled = enableLegacyHud;
        }

        if (playerKart != null)
        {
            playerKart.SetInputEnabled(enableInputOnStart);
        }

        EnsureTopSpeedUi();
        EnsureScreenSpeedLines();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void EnsureScreenSpeedLines()
    {
        KartScreenSpeedLines speedLines = FindAnyObjectByType<KartScreenSpeedLines>(FindObjectsInactive.Include);
        if (speedLines != null)
        {
            speedLines.PlayerKart = playerKart;
            speedLines.ApplyDriveConfig(playerKart != null ? playerKart.DriveConfig : null);
            speedLines.gameObject.SetActive(true);
            speedLines.transform.SetAsFirstSibling();
            return;
        }

        Canvas canvas = FindDashboardCanvas();
        if (canvas == null)
        {
            Camera uiCamera = Camera.main;
            GameObject canvasObject = new GameObject("Kart_DashboardCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = uiCamera != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 35;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject overlayObject = new GameObject("ScreenSpeedLines", typeof(RectTransform), typeof(KartScreenSpeedLines));
        overlayObject.transform.SetParent(canvas.transform, false);
        overlayObject.transform.SetAsFirstSibling();

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        speedLines = overlayObject.GetComponent<KartScreenSpeedLines>();
        speedLines.PlayerKart = playerKart;
        speedLines.ApplyDriveConfig(playerKart != null ? playerKart.DriveConfig : null);
        speedLines.raycastTarget = false;
    }

    private void EnsureTopSpeedUi()
    {
        SpeedDashboardGaugeUi dashboardGauge = FindAnyObjectByType<SpeedDashboardGaugeUi>(FindObjectsInactive.Include);
        KartTopSpeedUi topSpeedUi = FindAnyObjectByType<KartTopSpeedUi>(FindObjectsInactive.Include);
        if (dashboardGauge != null && dashboardGauge.ShowsNumericSpeed)
        {
            if (topSpeedUi != null)
            {
                topSpeedUi.gameObject.SetActive(false);
            }

            return;
        }

        if (topSpeedUi != null)
        {
            topSpeedUi.PlayerKart = playerKart;
            topSpeedUi.gameObject.SetActive(true);
            return;
        }

        Canvas canvas = FindDashboardCanvas();
        if (canvas == null)
        {
            Camera uiCamera = Camera.main;
            GameObject canvasObject = new GameObject("Kart_DashboardCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = uiCamera != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 35;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject panelObject = new GameObject("TopSpeedDisplay", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -54f);
        panelRect.sizeDelta = new Vector2(260f, 74f);

        Image panel = panelObject.GetComponent<Image>();
        panel.color = new Color(0f, 0f, 0f, 0.54f);
        panel.raycastTarget = false;

        GameObject textObject = new GameObject("SpeedText", typeof(RectTransform), typeof(Text), typeof(Outline), typeof(KartTopSpeedUi));
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.text = "000 KM/H";
        text.font = GetUiFont();
        text.fontSize = 42;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.72f);
        outline.effectDistance = new Vector2(2f, -2f);

        KartTopSpeedUi speedUi = textObject.GetComponent<KartTopSpeedUi>();
        speedUi.PlayerKart = playerKart;
    }

    private static Canvas FindDashboardCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].name == "Kart_DashboardCanvas")
            {
                return canvases[i];
            }
        }

        return null;
    }

    private static Font GetUiFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
