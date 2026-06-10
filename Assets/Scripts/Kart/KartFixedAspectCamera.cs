using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class KartFixedAspectCamera : MonoBehaviour
{
    [SerializeField] private bool useGlobalAspectMode = true;
    [SerializeField] private float targetWidth = 9f;
    [SerializeField] private float targetHeight = 16f;

    private Camera targetCamera;

    private void OnEnable()
    {
        if (!TryGetComponent(out targetCamera))
        {
            enabled = false;
            return;
        }

        KartAspectRatioSettings.ModeChanged += ApplyViewport;
        ApplyViewport();
    }

    private void OnDisable()
    {
        KartAspectRatioSettings.ModeChanged -= ApplyViewport;
    }

    private void OnPreCull()
    {
        ApplyViewport();
    }

    private void ApplyViewport()
    {
        if (targetCamera == null || Screen.height <= 0 || targetWidth <= 0f || targetHeight <= 0f)
        {
            return;
        }

        float targetAspect = useGlobalAspectMode
            ? KartAspectRatioSettings.TargetAspect
            : targetWidth / targetHeight;
        float currentAspect = (float)Screen.width / Screen.height;

        if (currentAspect > targetAspect)
        {
            float width = targetAspect / currentAspect;
            targetCamera.rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
            return;
        }

        float height = currentAspect / targetAspect;
        targetCamera.rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
    }
}
