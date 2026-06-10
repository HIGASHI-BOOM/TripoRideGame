using UnityEngine;
using UnityEngine.UI;

public sealed class SpeedDashboardGaugeUi : MonoBehaviour
{
    [SerializeField] private KartController playerKart;
    [SerializeField] private RectTransform needle;
    [SerializeField] private Text speedText;

    [Header("Speed Blocks")]
    [SerializeField] private Image[] speedSegments;
    [Tooltip("速度块被点亮时的最终不透明度。1 表示完全不透明。")]
    [Range(0f, 1f)]
    [SerializeField] private float activeSegmentAlpha = 1f;
    [Tooltip("速度块未被当前速度点亮时的常驻不透明度。0 表示完全隐藏，0.25 表示淡淡常驻，1 表示一直完全不透明。")]
    [Range(0f, 1f)]
    [SerializeField] private float inactiveSegmentAlpha = 0f;
    [Tooltip("速度块透明度跟随车速变化的平滑速度。数值越大，显示/隐藏越快。")]
    [Min(0.01f)]
    [SerializeField] private float segmentSmoothSpeed = 12f;

    [Header("Other HUD")]
    [Tooltip("非速度块 HUD 元素的最终不透明度，包括底框、小灯、KM/h 和速度数字。1 表示完全不透明，0 表示隐藏。")]
    [Range(0f, 1f)]
    [SerializeField] private float otherHudAlpha = 1f;
    [SerializeField] private Graphic[] otherHudGraphics;

    [SerializeField] private float maxSpeedKph = 160f;
    [SerializeField] private float zeroSpeedNeedleAngle = 122f;
    [SerializeField] private float maxSpeedNeedleAngle = -122f;

    private readonly float[] segmentIntensities = new float[12];
    private int displayedSpeed = -1;

    public KartController PlayerKart
    {
        get => playerKart;
        set => playerKart = value;
    }

    public bool ShowsNumericSpeed => speedText != null;

    private void Awake()
    {
        if (playerKart == null)
        {
            playerKart = FindAnyObjectByType<KartController>();
        }
    }

    private void Update()
    {
        float speedKph = playerKart != null ? playerKart.ForwardSpeedKph : 0f;
        float speed01 = Mathf.Clamp01(speedKph / Mathf.Max(1f, maxSpeedKph));
        float angle = Mathf.Lerp(zeroSpeedNeedleAngle, maxSpeedNeedleAngle, speed01);

        if (needle != null)
        {
            needle.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        if (speedText != null)
        {
            int roundedSpeed = Mathf.RoundToInt(speedKph);
            if (roundedSpeed != displayedSpeed)
            {
                displayedSpeed = roundedSpeed;
                speedText.text = displayedSpeed.ToString("000");
            }
        }

        UpdateOtherHudAlpha();
        UpdateSegments(speed01);
    }

    private void UpdateSegments(float speed01)
    {
        if (speedSegments == null || speedSegments.Length == 0)
        {
            return;
        }

        float scaledSpeed = speed01 * speedSegments.Length;
        float step = segmentSmoothSpeed * Time.deltaTime;

        for (int i = 0; i < speedSegments.Length; i++)
        {
            Image segment = speedSegments[i];
            if (segment == null)
            {
                continue;
            }

            float targetIntensity = Mathf.Clamp01(scaledSpeed - i);
            if (i < segmentIntensities.Length)
            {
                segmentIntensities[i] = Mathf.MoveTowards(segmentIntensities[i], targetIntensity, step);
                targetIntensity = segmentIntensities[i];
            }

            Color color = segment.color;
            color.a = Mathf.Lerp(inactiveSegmentAlpha, activeSegmentAlpha, targetIntensity);
            segment.color = color;
        }
    }

    private void UpdateOtherHudAlpha()
    {
        if (otherHudGraphics == null || otherHudGraphics.Length == 0)
        {
            return;
        }

        for (int i = 0; i < otherHudGraphics.Length; i++)
        {
            Graphic graphic = otherHudGraphics[i];
            if (graphic == null)
            {
                continue;
            }

            Color color = graphic.color;
            color.a = otherHudAlpha;
            graphic.color = color;
        }
    }
}
