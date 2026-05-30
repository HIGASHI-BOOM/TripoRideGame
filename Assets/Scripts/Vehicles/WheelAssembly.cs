using UnityEngine;

#if UNITY_6000_0_OR_NEWER
using WheelPhysicsMaterial = UnityEngine.PhysicsMaterial;
using WheelPhysicsMaterialCombine = UnityEngine.PhysicsMaterialCombine;
#else
using WheelPhysicsMaterial = UnityEngine.PhysicMaterial;
using WheelPhysicsMaterialCombine = UnityEngine.PhysicMaterialCombine;
#endif

public sealed class WheelAssembly : MonoBehaviour
{
    [SerializeField] private WheelSize wheelSize = WheelSize.Medium;
    [SerializeField] private float radius = 0.38f;
    [SerializeField] private Transform steeringPivot;
    [SerializeField] private Transform rollingPivot;
    [SerializeField] private Transform contactProbe;
    [SerializeField] private SphereCollider physicsCollider;

    private float rollAngle;
    private static WheelPhysicsMaterial sharedWheelPhysicsMaterial;

    public WheelSize WheelSize => wheelSize;
    public float Radius => Mathf.Max(0.05f, radius);
    public Transform ContactProbe => contactProbe != null ? contactProbe : transform;
    public SphereCollider PhysicsCollider => physicsCollider != null ? physicsCollider : GetComponent<SphereCollider>();

    private void Awake()
    {
        ConfigurePhysicsCollider();
    }

    public void Configure(WheelSize size, float wheelRadius)
    {
        wheelSize = size;
        radius = Mathf.Max(0.05f, wheelRadius);
    }

    public void ApplySteer(float steerAngle)
    {
        if (steeringPivot == null)
        {
            steeringPivot = transform;
        }

        steeringPivot.localRotation = Quaternion.Euler(0f, steerAngle, 0f);
    }

    public void RollByDistance(float signedDistance)
    {
        if (rollingPivot == null)
        {
            rollingPivot = transform;
        }

        float degrees = signedDistance / (2f * Mathf.PI * Radius) * 360f;
        rollAngle += degrees;
        rollingPivot.localRotation = Quaternion.Euler(rollAngle, 0f, 0f);
    }

    public void SetPivots(Transform steer, Transform roll, Transform probe)
    {
        steeringPivot = steer;
        rollingPivot = roll;
        contactProbe = probe;
    }

    public void SetPhysicsCollider(SphereCollider collider)
    {
        physicsCollider = collider;
        ConfigurePhysicsCollider();
    }

    private void ConfigurePhysicsCollider()
    {
        SphereCollider collider = PhysicsCollider;
        if (collider == null)
        {
            return;
        }

        collider.radius = Radius;
        collider.isTrigger = false;
        collider.sharedMaterial = GetWheelPhysicsMaterial();
    }

    private static WheelPhysicsMaterial GetWheelPhysicsMaterial()
    {
        if (sharedWheelPhysicsMaterial != null)
        {
            return sharedWheelPhysicsMaterial;
        }

        sharedWheelPhysicsMaterial = new WheelPhysicsMaterial("Runtime_Wheel_LowFriction")
        {
            dynamicFriction = 0.08f,
            staticFriction = 0.08f,
            bounciness = 0f,
            frictionCombine = WheelPhysicsMaterialCombine.Minimum,
            bounceCombine = WheelPhysicsMaterialCombine.Minimum
        };

        return sharedWheelPhysicsMaterial;
    }
}
