using UnityEngine;

[CreateAssetMenu(fileName = "VehicleSimulationConfig", menuName = "Tripo Ride/Vehicle Simulation Config")]
public sealed class VehicleSimulationConfig : ScriptableObject
{
    [Header("Body")]
    [Min(1f)] public float mass = 980f;
    public Vector3 centerOfMass = new Vector3(0f, -0.35f, 0.08f);
    public bool fitCenterOfMassToGeneratedBody = true;
    [Range(0.05f, 0.5f)] public float generatedCenterOfMassHeight = 0.18f;
    [Min(0f)] public float idleDrag = 0.25f;
    [Min(0f)] public float brakeDrag = 7.5f;
    [Min(0f)] public float driveAngularDamping = 2.4f;
    [Min(0f)] public float parkedAngularDamping = 4f;

    [Header("Powertrain")]
    [Min(0f)] public float driveForce = 6200f;
    [Min(0f)] public float reverseForce = 3200f;
    [Min(0.1f)] public float maxFlatSpeed = 41.666668f;
    [Min(0.1f)] public float maxReverseSpeed = 13.888889f;

    [Header("Steering")]
    [Range(3f, 35f)] public float maxSteerAngle = 14f;
    [Range(1f, 20f)] public float highSpeedSteerAngle = 4f;
    [Range(1f, 20f)] public float steerSharpness = 7f;

    [Header("Tire Grip")]
    [Range(0.5f, 25f)] public float frontTireGrip = 3.2f;
    [Range(0.5f, 30f)] public float rearTireGrip = 16f;

    [Header("Braking")]
    [Min(0f)] public float brakeForce = 10500f;

    [Header("Generated Body Fit")]
    public bool autoFitSocketsToGeneratedBody = true;
    public bool alignWheelSocketsByBodyBottom = true;
    public float wheelBodyBottomClearance = 0f;
    [Min(0.05f)] public float fallbackWheelRadius = 0.42f;

    [Header("Generated Body Collision")]
    [Range(0.5f, 1.2f)] public float chassisWidthScale = 0.9f;
    [Range(0.5f, 1.2f)] public float chassisLengthScale = 0.92f;
    [Range(0.2f, 1f)] public float chassisHeightFraction = 0.42f;
    [Min(0.2f)] public float chassisMinHeight = 0.45f;
    public bool useUpperCabinCollider;

    [Header("Stability")]
    [Min(0f)] public float speedDownforce = 8f;
    [Min(0f)] public float uprightStabilization = 0.22f;
}
