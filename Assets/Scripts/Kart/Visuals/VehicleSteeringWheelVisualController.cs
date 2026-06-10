using UnityEngine;
using UnityEngine.Serialization;

public sealed class VehicleSteeringWheelVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KartController kart;
    [SerializeField] private Transform steeringWheel;

    [Header("Behavior")]
    [Min(0f)]
    [SerializeField] private float maxTurnAngle = 180f;
    [FormerlySerializedAs("turnAxis")]
    [SerializeField] private Vector3 localTurnAxis = Vector3.forward;
    [SerializeField] private bool invertTurn;

    private Quaternion baseLocalRotation;

    private void Reset()
    {
        steeringWheel = transform;
        kart = GetComponentInParent<KartController>();
    }

    private void Awake()
    {
        if (steeringWheel == null)
        {
            steeringWheel = transform;
        }

        ResolveDriver();
        baseLocalRotation = steeringWheel.localRotation;
    }

    private void LateUpdate()
    {
        if (kart == null)
        {
            ResolveDriver();
        }

        if (kart == null || steeringWheel == null)
        {
            return;
        }

        float direction = invertTurn ? -1f : 1f;
        float turnAngle = GetVisualSteerInput() * maxTurnAngle * direction;
        steeringWheel.localRotation = ApplyLocalAxisRotation(baseLocalRotation, turnAngle, localTurnAxis);
    }

    private void ResolveDriver()
    {
        kart = GetComponentInParent<KartController>();
    }

    private float GetVisualSteerInput()
    {
        return kart != null ? kart.VisualSteerInput : 0f;
    }

    private static Vector3 SafeAxis(Vector3 axis)
    {
        return axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.forward;
    }

    private static Quaternion ApplyLocalAxisRotation(Quaternion baseRotation, float angle, Vector3 localAxis)
    {
        return baseRotation * Quaternion.AngleAxis(angle, SafeAxis(localAxis));
    }
}
