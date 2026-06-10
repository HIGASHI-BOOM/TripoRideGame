using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class KartScreenSpeedLines : MaskableGraphic
{
    [Header("References")]
    [Tooltip("玩家卡丁车；为空时会自动寻找场景中的 KartController。速度线会读取它的前进速度。")]
    [SerializeField] private KartController playerKart;

    [Header("Speed Mapping")]
    [Tooltip("是否启用屏幕速度线效果。关闭后不会显示屏幕边缘速度线。")]
    [SerializeField] private bool effectEnabled = true;
    [Tooltip("低于这个速度时速度线完全隐藏，单位 km/h。")]
    [SerializeField] private float visibleAtSpeedKph = 45f;
    [Tooltip("达到这个速度时速度线数量和强度接近最大，单位 km/h。")]
    [SerializeField] private float fullAtSpeedKph = 150f;
    [Tooltip("速度变化到视觉强度的平滑时间，数值越小响应越快。")]
    [SerializeField] private float intensitySmoothTime = 0.12f;

    [Header("Lines")]
    [Tooltip("刚出现时的最少速度线数量。")]
    [SerializeField] private int minLineCount = 5;
    [Tooltip("满速时的最多速度线数量，速度越快越接近这个值。")]
    [SerializeField] private int maxLineCount = 54;
    [Tooltip("低速时每条速度线的长度，单位为 Canvas 像素。")]
    [SerializeField] private float minLineLength = 180f;
    [Tooltip("高速时每条速度线的长度，单位为 Canvas 像素。")]
    [SerializeField] private float maxLineLength = 620f;
    [Tooltip("低速时速度线靠近屏幕边缘的宽度。")]
    [SerializeField] private float minLineWidth = 5f;
    [Tooltip("高速时速度线靠近屏幕边缘的宽度。")]
    [SerializeField] private float maxLineWidth = 24f;
    [Tooltip("屏幕中心保留的空白比例，避免速度线盖住车和道路焦点。")]
    [SerializeField] private float centerClearRadius = 0.34f;
    [Tooltip("整体线条颜色和最大透明度。")]
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.82f);
    [Tooltip("线条向中心流动的速度。")]
    [SerializeField] private float flowSpeed = 1.35f;

    [Header("Runtime Preview")]
    [Tooltip("开启后不读取真实车速，而是使用下面的预览速度，方便在 Play Mode 手动调速度线效果。")]
    [SerializeField] private bool usePreviewSpeed;
    [Tooltip("手动预览速度，单位 km/h。适合填 80、120、150 来检查不同速度下的线条数量。")]
    [SerializeField] private float previewSpeedKph = 150f;
    [Tooltip("当前视觉强度，只读参考值：0 表示隐藏，1 表示满强度。")]
    [SerializeField] private float currentIntensity;

    private float intensityVelocity;

    public KartController PlayerKart
    {
        get => playerKart;
        set => playerKart = value;
    }

    public void ApplyDriveConfig(KartDriveConfig config)
    {
        if (config == null)
        {
            return;
        }

        effectEnabled = config.enableScreenSpeedLines;
        visibleAtSpeedKph = config.screenSpeedLineVisibleAtSpeedKph;
        fullAtSpeedKph = config.screenSpeedLineFullAtSpeedKph;
        intensitySmoothTime = config.screenSpeedLineIntensitySmoothTime;
        minLineCount = config.screenSpeedLineMinCount;
        maxLineCount = config.screenSpeedLineMaxCount;
        minLineLength = config.screenSpeedLineMinLength;
        maxLineLength = config.screenSpeedLineMaxLength;
        minLineWidth = config.screenSpeedLineMinWidth;
        maxLineWidth = config.screenSpeedLineMaxWidth;
        centerClearRadius = config.screenSpeedLineCenterClearRadius;
        lineColor = config.screenSpeedLineColor;
        flowSpeed = config.screenSpeedLineFlowSpeed;
        usePreviewSpeed = config.useScreenSpeedLinePreview;
        previewSpeedKph = config.screenSpeedLinePreviewSpeedKph;
    }

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;

        if (playerKart == null)
        {
            playerKart = FindAnyObjectByType<KartController>();
        }
    }

    private void Update()
    {
        if (playerKart != null)
        {
            ApplyDriveConfig(playerKart.DriveConfig);
        }

        if (!effectEnabled)
        {
            currentIntensity = Mathf.SmoothDamp(currentIntensity, 0f, ref intensityVelocity, Mathf.Max(0.01f, intensitySmoothTime));
            SetVerticesDirty();
            return;
        }

        float speedKph = usePreviewSpeed ? previewSpeedKph : playerKart != null ? playerKart.ForwardSpeedKph : 0f;
        float targetIntensity = Mathf.InverseLerp(visibleAtSpeedKph, Mathf.Max(visibleAtSpeedKph + 1f, fullAtSpeedKph), speedKph);
        targetIntensity = Mathf.SmoothStep(0f, 1f, targetIntensity);
        currentIntensity = Mathf.SmoothDamp(currentIntensity, targetIntensity, ref intensityVelocity, Mathf.Max(0.01f, intensitySmoothTime));
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (currentIntensity <= 0.01f)
        {
            return;
        }

        Rect rect = rectTransform.rect;
        if (rect.width <= 1f || rect.height <= 1f)
        {
            return;
        }

        int lineCount = Mathf.RoundToInt(Mathf.Lerp(minLineCount, maxLineCount, currentIntensity));
        float baseLength = Mathf.Lerp(minLineLength, maxLineLength, currentIntensity);
        float baseWidth = Mathf.Lerp(minLineWidth, maxLineWidth, currentIntensity);
        float alpha = lineColor.a * Mathf.Lerp(0.18f, 1f, currentIntensity);
        Vector2 center = rect.center;
        float halfWidth = rect.width * 0.5f;
        float halfHeight = rect.height * 0.5f;
        float clearRadius = Mathf.Min(halfWidth, halfHeight) * Mathf.Clamp01(centerClearRadius);
        float time = Application.isPlaying ? Time.unscaledTime : 0f;

        int slots = Mathf.Max(12, maxLineCount);
        for (int i = 0; i < lineCount; i++)
        {
            int slot = PositiveModulo(i * 37 + Mathf.FloorToInt(time * flowSpeed * 4f), slots);
            float jitter = Hash01(slot * 19 + 3) - 0.5f;
            float angle = ((slot + jitter * 0.36f) / slots) * Mathf.PI * 2f;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float edgeDistance = DistanceToRectEdge(direction, halfWidth, halfHeight);
            float length = baseLength * Mathf.Lerp(0.72f, 1.22f, Hash01(slot * 23 + 11));
            float width = baseWidth * Mathf.Lerp(0.55f, 1.25f, Hash01(slot * 29 + 17));
            float flow = Mathf.Repeat(time * flowSpeed + Hash01(slot * 31 + 5), 1f);
            float offset = Mathf.Lerp(-80f, 70f, flow);
            float innerDistance = Mathf.Max(clearRadius, edgeDistance - length + offset);
            float outerDistance = edgeDistance + 48f + offset * 0.18f;

            if (innerDistance >= outerDistance - 8f)
            {
                continue;
            }

            float lineAlpha = alpha * Mathf.Lerp(0.42f, 1f, Hash01(slot * 41 + 7));
            AddSpeedLine(vh, center, direction, innerDistance, outerDistance, width, lineAlpha);
        }
    }

    private void AddSpeedLine(VertexHelper vh, Vector2 center, Vector2 direction, float innerDistance, float outerDistance, float width, float alpha)
    {
        Vector2 tangent = new Vector2(-direction.y, direction.x);
        Vector2 tip = center + direction * innerDistance;
        Vector2 mid = center + direction * Mathf.Lerp(innerDistance, outerDistance, 0.56f);
        Vector2 outer = center + direction * outerDistance;
        Color32 transparent = new Color(lineColor.r, lineColor.g, lineColor.b, 0f);
        Color32 body = new Color(lineColor.r, lineColor.g, lineColor.b, alpha);
        Color32 edge = new Color(lineColor.r, lineColor.g, lineColor.b, alpha * 0.68f);

        int start = vh.currentVertCount;
        vh.AddVert(tip, transparent, Vector2.zero);
        vh.AddVert(mid + tangent * width * 0.22f, body, Vector2.zero);
        vh.AddVert(outer + tangent * width * 0.5f, edge, Vector2.zero);
        vh.AddVert(outer - tangent * width * 0.5f, edge, Vector2.zero);
        vh.AddVert(mid - tangent * width * 0.22f, body, Vector2.zero);

        vh.AddTriangle(start, start + 1, start + 4);
        vh.AddTriangle(start + 1, start + 2, start + 3);
        vh.AddTriangle(start + 1, start + 3, start + 4);
    }

    private static float DistanceToRectEdge(Vector2 direction, float halfWidth, float halfHeight)
    {
        float xDistance = Mathf.Abs(direction.x) > 0.001f ? halfWidth / Mathf.Abs(direction.x) : float.PositiveInfinity;
        float yDistance = Mathf.Abs(direction.y) > 0.001f ? halfHeight / Mathf.Abs(direction.y) : float.PositiveInfinity;
        return Mathf.Min(xDistance, yDistance);
    }

    private static float Hash01(int seed)
    {
        float value = Mathf.Sin(seed * 12.9898f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }

    private static int PositiveModulo(int value, int modulo)
    {
        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }
}
