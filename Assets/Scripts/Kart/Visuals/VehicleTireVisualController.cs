using UnityEngine;

public sealed class VehicleTireVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KartController kart;
    [SerializeField] private Transform tireTransform;
    [SerializeField] private Renderer radiusSource;

    [Header("Behavior")]
    [SerializeField] private bool steeringTire;
    [SerializeField] private bool autoDetectRadius = true;
    [Min(0.05f)]
    [SerializeField] private float fallbackRadius = 0.34f;
    [SerializeField] private Vector3 steerAxis = Vector3.up;
    [SerializeField] private Vector3 rollAxis = Vector3.right;
    [SerializeField] private bool invertRoll;

    private Quaternion baseLocalRotation;
    private float rollAngle;

    private void Reset()
    {
        tireTransform = transform;
        radiusSource = GetComponentInChildren<Renderer>(true);
        kart = GetComponentInParent<KartController>();
    }

    private void Awake()
    {
        if (tireTransform == null)
        {
            tireTransform = transform;
        }

        if (radiusSource == null)
        {
            radiusSource = GetComponentInChildren<Renderer>(true);
        }

        ResolveDriver();
        baseLocalRotation = tireTransform.localRotation;
    }

    private void LateUpdate()
    {
        if (kart == null)
        {
            ResolveDriver();
        }

        if (kart == null || tireTransform == null)
        {
            return;
        }

        float radius = GetRuntimeRadius();
        float signedDistance = GetForwardSpeedMps() * Time.deltaTime;
        float direction = invertRoll ? -1f : 1f;
        rollAngle += signedDistance / (2f * Mathf.PI * radius) * 360f * direction;

        float steerAngle = steeringTire ? GetVisualSteerAngle() : 0f;
        Quaternion steerRotation = Quaternion.AngleAxis(steerAngle, SafeAxis(steerAxis, Vector3.up));
        Quaternion rollRotation = Quaternion.AngleAxis(rollAngle, SafeAxis(rollAxis, Vector3.right));
        tireTransform.localRotation = baseLocalRotation * steerRotation * rollRotation;
    }

    private void ResolveDriver()
    {
        kart = GetComponentInParent<KartController>();
    }

    private float GetForwardSpeedMps()
    {
        return kart != null ? kart.ForwardSpeedMps : 0f;
    }

    private float GetVisualSteerAngle()
    {
        return kart != null ? kart.VisualSteerAngle : 0f;
    }

    private float GetRuntimeRadius()
    {
        if (autoDetectRadius && radiusSource != null)
        {
            return Mathf.Max(0.05f, radiusSource.bounds.extents.y);
        }

        float largestScale = Mathf.Max(
            Mathf.Abs(tireTransform.lossyScale.x),
            Mathf.Abs(tireTransform.lossyScale.y),
            Mathf.Abs(tireTransform.lossyScale.z));
        return Mathf.Max(0.05f, fallbackRadius * Mathf.Max(0.01f, largestScale));
    }

    private static Vector3 SafeAxis(Vector3 axis, Vector3 fallback)
    {
        return axis.sqrMagnitude > 0.0001f ? axis.normalized : fallback;
    }
}
