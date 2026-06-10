using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public sealed class VehicleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VehicleSimulationConfig simulationConfig;
    [SerializeField] private Rigidbody body;
    [SerializeField] private Transform generatedBodyRoot;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform driverSeat;
    [SerializeField] private BoxCollider bodyCollider;
    [SerializeField] private CapsuleCollider cabinCollider;
    [SerializeField] private WheelSocket[] wheelSockets;
    [SerializeField] private bool autoFitSocketsToGeneratedBody = true;
    [SerializeField] private bool showColliderDebug = true;

    [Header("Generated Wheel Fit")]
    [SerializeField] private bool alignWheelSocketsByBodyBottom = true;
    [SerializeField] private float wheelBodyBottomClearance = 0f;
    [Min(0.05f)]
    [SerializeField] private float fallbackWheelRadius = 0.42f;

    [Header("Generated Body Collision")]
    [Range(0.5f, 1.2f)]
    [SerializeField] private float chassisWidthScale = 0.9f;
    [Range(0.5f, 1.2f)]
    [SerializeField] private float chassisLengthScale = 0.92f;
    [Range(0.2f, 1f)]
    [SerializeField] private float chassisHeightFraction = 0.42f;
    [Min(0.2f)]
    [SerializeField] private float chassisMinHeight = 0.45f;
    [SerializeField] private bool useUpperCabinCollider;

    [Header("Drive")]
    [Min(0f)]
    [SerializeField] private float driveForce = 5200f;
    [Min(0f)]
    [SerializeField] private float reverseForce = 2600f;
    [Min(0.1f)]
    [SerializeField] private float maxFlatSpeed = 41.666668f;
    [Min(0.1f)]
    [SerializeField] private float maxReverseSpeed = 13.888889f;

    [Header("Steering")]
    [Tooltip("Low-speed front wheel angle in degrees. Lower values produce a larger turn radius.")]
    [Range(3f, 35f)]
    [SerializeField] private float maxSteerAngle = 10f;
    [Tooltip("Front wheel angle at max speed. Keep this much lower than Max Steer Angle for stable high-speed turns.")]
    [Range(1f, 20f)]
    [SerializeField] private float highSpeedSteerAngle = 3f;
    [Tooltip("How quickly steering input reaches the requested value.")]
    [Range(1f, 20f)]
    [SerializeField] private float steerSharpness = 5f;

    [Header("Tire Grip")]
    [Tooltip("Front axle side grip. Lower values widen the turn radius and reduce snap turning.")]
    [Range(0.5f, 15f)]
    [SerializeField] private float frontTireGrip = 1.6f;
    [Tooltip("Rear axle side grip. Higher values keep the tail planted.")]
    [Range(0.5f, 20f)]
    [SerializeField] private float rearTireGrip = 14f;

    [Header("Braking And Damping")]
    [Min(0f)]
    [SerializeField] private float brakeForce = 9000f;
    [Min(0f)]
    [SerializeField] private float brakeDrag = 7.5f;
    [Min(0f)]
    [SerializeField] private float idleDrag = 0.25f;

    private float throttleInput;
    private float steerInput;
    private float smoothedSteerInput;
    private bool brakeHeld;
    private bool canDrive;

    public Transform GeneratedBodyRoot => generatedBodyRoot;
    public Transform CameraTarget => cameraTarget != null ? cameraTarget : transform;
    public Transform DriverSeat => driverSeat != null ? driverSeat : transform;
    public VehicleSimulationConfig SimulationConfig => simulationConfig;
    public bool CanDrive => canDrive;
    public bool HasAllWheels => wheelSockets != null && wheelSockets.Length >= 4 && AllSocketsFilled();
    public WheelSocket[] WheelSockets => wheelSockets;
    public float SpeedMps => body != null ? Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude : 0f;
    public float SpeedKph => SpeedMps * 3.6f;
    public float ForwardSpeedMps => body != null ? Vector3.Dot(body.linearVelocity, transform.forward) : 0f;
    public float ThrottleInput => throttleInput;
    public float SteerInput => steerInput;
    public float VisualSteerInput => smoothedSteerInput;
    public float VisualSteerAngle => CalculateSteerAngle(ForwardSpeedMps);
    public bool BrakeHeld => brakeHeld;

    private void Reset()
    {
        body = GetComponent<Rigidbody>();
        wheelSockets = GetComponentsInChildren<WheelSocket>(true);
        CacheBodyColliders();
    }

    private void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        if (wheelSockets == null || wheelSockets.Length == 0)
        {
            wheelSockets = GetComponentsInChildren<WheelSocket>(true);
        }

        CacheBodyColliders();
        ApplySimulationSettingsToBody(preserveGeneratedCenterOfMass: false);
        SetDriveEnabled(false);
    }

    private void Update()
    {
        ReadInput();
        UpdateWheelVisuals();
    }

    private void FixedUpdate()
    {
        if (!canDrive)
        {
            body.linearDamping = BrakeDrag;
            return;
        }

        body.linearDamping = brakeHeld ? BrakeDrag : IdleDrag;
        smoothedSteerInput = Mathf.MoveTowards(
            smoothedSteerInput,
            steerInput,
            SteerSharpness * Time.fixedDeltaTime);

        Vector3 frontAxle = GetAxleCenter(frontAxle: true);
        Vector3 rearAxle = GetAxleCenter(frontAxle: false);
        Vector3 flatVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
        float signedSpeed = Vector3.Dot(flatVelocity, transform.forward);
        float force = throttleInput >= 0f ? DriveForce : ReverseForce;

        if (brakeHeld)
        {
            ApplyBrakeForce(flatVelocity);
        }
        else if (ShouldApplyDriveForce(signedSpeed))
        {
            body.AddForceAtPosition(transform.forward * (throttleInput * force), rearAxle, ForceMode.Force);
        }

        float steerAngle = CalculateSteerAngle(signedSpeed);
        Quaternion steerRotation = Quaternion.AngleAxis(steerAngle, Vector3.up);

        ApplyLateralTireGrip(frontAxle, steerRotation * transform.right, FrontTireGrip);
        ApplyLateralTireGrip(rearAxle, transform.right, RearTireGrip);
        ApplyStabilityForces(flatVelocity);
    }

    public void SetDriveEnabled(bool enabled)
    {
        canDrive = enabled && HasAllWheels;
        ApplySimulationSettingsToBody(preserveGeneratedCenterOfMass: true);
        body.useGravity = canDrive;

        if (canDrive)
        {
            SnapToWheelGround();
            body.isKinematic = false;
            body.WakeUp();
            return;
        }

        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        smoothedSteerInput = 0f;
        body.isKinematic = true;
    }

    public void SetSimulationConfig(VehicleSimulationConfig config)
    {
        simulationConfig = config;
        RefreshSimulationSettings();
    }

    public void RefreshSimulationSettings()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        ApplySimulationSettingsToBody(preserveGeneratedCenterOfMass: true);
    }

    public WheelSocket GetFirstEmptySocket()
    {
        if (wheelSockets == null)
        {
            return null;
        }

        for (int i = 0; i < wheelSockets.Length; i++)
        {
            if (wheelSockets[i] != null && !wheelSockets[i].HasWheel)
            {
                return wheelSockets[i];
            }
        }

        return null;
    }

    public WheelSocket GetSocket(WheelSocketId id)
    {
        if (wheelSockets == null)
        {
            return null;
        }

        for (int i = 0; i < wheelSockets.Length; i++)
        {
            if (wheelSockets[i] != null && wheelSockets[i].SocketId == id)
            {
                return wheelSockets[i];
            }
        }

        return null;
    }

    public void ClearWheels()
    {
        if (wheelSockets == null)
        {
            return;
        }

        for (int i = 0; i < wheelSockets.Length; i++)
        {
            if (wheelSockets[i] != null)
            {
                wheelSockets[i].RemoveWheel();
            }
        }

        SetDriveEnabled(false);
    }

    public void ApplyGeneratedBody(GeneratedVehicleBody generatedBody)
    {
        if (generatedBody == null || generatedBody.Root == null)
        {
            return;
        }

        Bounds localBounds = CalculateLocalRendererBounds(generatedBody.Root.transform, transform);
        if (localBounds.size.sqrMagnitude < 0.0001f)
        {
            return;
        }

        FitBodyColliders(localBounds);
        FitAttachmentPoints(localBounds);

        if (showColliderDebug)
        {
            Debug.Log($"Vehicle collider refreshed from generated body bounds. Bounds center={localBounds.center:F3}, size={localBounds.size:F3}");
        }
    }

    public void RefitGeneratedBodyAttachments()
    {
        if (generatedBodyRoot == null)
        {
            return;
        }

        Bounds localBounds = CalculateLocalRendererBounds(generatedBodyRoot, transform);
        if (localBounds.size.sqrMagnitude < 0.0001f)
        {
            return;
        }

        FitAttachmentPoints(localBounds);
    }

    public void SnapToWheelGround()
    {
        if (wheelSockets == null)
        {
            return;
        }

        bool foundWheel = false;
        float totalDelta = 0f;
        int deltaCount = 0;

        for (int i = 0; i < wheelSockets.Length; i++)
        {
            WheelSocket socket = wheelSockets[i];
            if (socket == null || socket.InstalledWheel == null)
            {
                continue;
            }

            foundWheel = true;
            WheelAssembly wheel = socket.InstalledWheel;
            float groundY = FindGroundY(socket.transform.position);
            float wheelBottomY = socket.transform.position.y - wheel.Radius;
            totalDelta += groundY - wheelBottomY;
            deltaCount++;
        }

        if (!foundWheel || deltaCount == 0)
        {
            return;
        }

        Vector3 position = transform.position;
        position.y += totalDelta / deltaCount;
        transform.position = position;
    }

    private void ReadInput()
    {
        throttleInput = 0f;
        steerInput = 0f;
        brakeHeld = false;

        if (!GameInputContext.DrivingEnabled)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) throttleInput += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) throttleInput -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steerInput += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steerInput -= 1f;
            brakeHeld = keyboard.spaceKey.isPressed;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            throttleInput += stick.y;
            steerInput += stick.x;
            brakeHeld |= gamepad.buttonSouth.isPressed;
        }

        throttleInput = Mathf.Clamp(throttleInput, -1f, 1f);
        steerInput = Mathf.Clamp(steerInput, -1f, 1f);
    }

    private void UpdateWheelVisuals()
    {
        if (wheelSockets == null || body == null)
        {
            return;
        }

        float forwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
        for (int i = 0; i < wheelSockets.Length; i++)
        {
            if (wheelSockets[i] != null)
            {
                wheelSockets[i].UpdateWheelVisual(forwardSpeed, smoothedSteerInput, Time.deltaTime);
            }
        }
    }

    private bool ShouldApplyDriveForce(float signedSpeed)
    {
        if (Mathf.Abs(throttleInput) < 0.01f)
        {
            return false;
        }

        bool changingDirection = Mathf.Sign(throttleInput) != Mathf.Sign(signedSpeed) && Mathf.Abs(signedSpeed) > 0.5f;
        float speedLimit = throttleInput >= 0f ? MaxFlatSpeed : MaxReverseSpeed;
        float speedInThrottleDirection = throttleInput >= 0f ? signedSpeed : -signedSpeed;
        return changingDirection || speedInThrottleDirection < speedLimit;
    }

    private float CalculateSteerAngle(float signedSpeed)
    {
        float speedSteerBlend = Mathf.Clamp01(Mathf.Abs(signedSpeed) / Mathf.Max(MaxFlatSpeed, 0.01f));
        return smoothedSteerInput * Mathf.Lerp(MaxSteerAngle, HighSpeedSteerAngle, speedSteerBlend);
    }

    private void ApplyBrakeForce(Vector3 flatVelocity)
    {
        if (flatVelocity.sqrMagnitude < 0.01f)
        {
            return;
        }

        Vector3 brakeDirection = -flatVelocity.normalized;
        body.AddForce(brakeDirection * BrakeForce, ForceMode.Force);
    }

    private void ApplyLateralTireGrip(Vector3 axlePosition, Vector3 tireRight, float grip)
    {
        Vector3 pointVelocity = body.GetPointVelocity(axlePosition);
        float lateralSpeed = Vector3.Dot(pointVelocity, tireRight);
        Vector3 lateralForce = -tireRight * (lateralSpeed * grip * body.mass);
        lateralForce = Vector3.ClampMagnitude(lateralForce, body.mass * 9.81f * grip);
        body.AddForceAtPosition(lateralForce, axlePosition, ForceMode.Force);
    }

    private void ApplyStabilityForces(Vector3 flatVelocity)
    {
        float speedSquared = flatVelocity.sqrMagnitude;
        if (SpeedDownforce > 0f && speedSquared > 0.01f)
        {
            body.AddForce(-transform.up * (SpeedDownforce * speedSquared), ForceMode.Force);
        }

        if (UprightStabilization <= 0f)
        {
            return;
        }

        Vector3 correctionAxis = Vector3.Cross(transform.up, Vector3.up);
        body.AddTorque(correctionAxis * (UprightStabilization * body.mass), ForceMode.Force);
    }

    private Vector3 GetAxleCenter(bool frontAxle)
    {
        WheelSocket left = GetSocket(frontAxle ? WheelSocketId.FL : WheelSocketId.RL);
        WheelSocket right = GetSocket(frontAxle ? WheelSocketId.FR : WheelSocketId.RR);

        if (left != null && right != null)
        {
            return (left.transform.position + right.transform.position) * 0.5f;
        }

        float fallbackOffset = frontAxle ? 1.1f : -1.1f;
        return transform.TransformPoint(new Vector3(0f, 0.35f, fallbackOffset));
    }

    private bool AllSocketsFilled()
    {
        for (int i = 0; i < wheelSockets.Length; i++)
        {
            if (wheelSockets[i] == null || !wheelSockets[i].HasWheel)
            {
                return false;
            }
        }

        return true;
    }

    private void CacheBodyColliders()
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponentInChildren<BoxCollider>(true);
        }

        if (cabinCollider == null)
        {
            cabinCollider = GetComponentInChildren<CapsuleCollider>(true);
        }
    }

    private void ApplySimulationSettingsToBody(bool preserveGeneratedCenterOfMass)
    {
        if (body == null)
        {
            return;
        }

        body.mass = Mass;
        if (!preserveGeneratedCenterOfMass || !FitCenterOfMassToGeneratedBody)
        {
            body.centerOfMass = CenterOfMass;
        }

        body.linearDamping = canDrive ? IdleDrag : BrakeDrag;
        body.angularDamping = canDrive ? DriveAngularDamping : ParkedAngularDamping;
    }

    private float Mass => simulationConfig != null ? Mathf.Max(1f, simulationConfig.mass) : Mathf.Max(1f, body != null ? body.mass : 900f);
    private Vector3 CenterOfMass => simulationConfig != null ? simulationConfig.centerOfMass : new Vector3(0f, -0.35f, 0.1f);
    private bool FitCenterOfMassToGeneratedBody => simulationConfig == null || simulationConfig.fitCenterOfMassToGeneratedBody;
    private float GeneratedCenterOfMassHeight => simulationConfig != null ? simulationConfig.generatedCenterOfMassHeight : 0.18f;
    private float IdleDrag => simulationConfig != null ? simulationConfig.idleDrag : idleDrag;
    private float BrakeDrag => simulationConfig != null ? simulationConfig.brakeDrag : brakeDrag;
    private float DriveAngularDamping => simulationConfig != null ? simulationConfig.driveAngularDamping : 2.4f;
    private float ParkedAngularDamping => simulationConfig != null ? simulationConfig.parkedAngularDamping : 4f;
    private float DriveForce => simulationConfig != null ? simulationConfig.driveForce : driveForce;
    private float ReverseForce => simulationConfig != null ? simulationConfig.reverseForce : reverseForce;
    private float MaxFlatSpeed => simulationConfig != null ? simulationConfig.maxFlatSpeed : maxFlatSpeed;
    private float MaxReverseSpeed => simulationConfig != null ? simulationConfig.maxReverseSpeed : maxReverseSpeed;
    private float MaxSteerAngle => simulationConfig != null ? simulationConfig.maxSteerAngle : maxSteerAngle;
    private float HighSpeedSteerAngle => simulationConfig != null ? simulationConfig.highSpeedSteerAngle : highSpeedSteerAngle;
    private float SteerSharpness => simulationConfig != null ? simulationConfig.steerSharpness : steerSharpness;
    private float FrontTireGrip => simulationConfig != null ? simulationConfig.frontTireGrip : frontTireGrip;
    private float RearTireGrip => simulationConfig != null ? simulationConfig.rearTireGrip : rearTireGrip;
    private float BrakeForce => simulationConfig != null ? simulationConfig.brakeForce : brakeForce;
    private bool AutoFitSocketsToGeneratedBody => simulationConfig != null ? simulationConfig.autoFitSocketsToGeneratedBody : autoFitSocketsToGeneratedBody;
    private bool AlignWheelSocketsByBodyBottom => simulationConfig != null ? simulationConfig.alignWheelSocketsByBodyBottom : alignWheelSocketsByBodyBottom;
    private float WheelBodyBottomClearance => simulationConfig != null ? simulationConfig.wheelBodyBottomClearance : wheelBodyBottomClearance;
    private float FallbackWheelRadius => simulationConfig != null ? simulationConfig.fallbackWheelRadius : fallbackWheelRadius;
    private float ChassisWidthScale => simulationConfig != null ? simulationConfig.chassisWidthScale : chassisWidthScale;
    private float ChassisLengthScale => simulationConfig != null ? simulationConfig.chassisLengthScale : chassisLengthScale;
    private float ChassisHeightFraction => simulationConfig != null ? simulationConfig.chassisHeightFraction : chassisHeightFraction;
    private float ChassisMinHeight => simulationConfig != null ? simulationConfig.chassisMinHeight : chassisMinHeight;
    private bool UseUpperCabinCollider => simulationConfig != null ? simulationConfig.useUpperCabinCollider : useUpperCabinCollider;
    private float SpeedDownforce => simulationConfig != null ? simulationConfig.speedDownforce : 0f;
    private float UprightStabilization => simulationConfig != null ? simulationConfig.uprightStabilization : 0f;

    private void FitBodyColliders(Bounds bounds)
    {
        CacheBodyColliders();

        if (bodyCollider != null)
        {
            float chassisHeight = Mathf.Clamp(
                bounds.size.y * ChassisHeightFraction,
                ChassisMinHeight,
                Mathf.Max(bounds.size.y, ChassisMinHeight));
            float chassisCenterY = bounds.min.y + chassisHeight * 0.5f;

            bodyCollider.center = bounds.center;
            bodyCollider.center = new Vector3(bounds.center.x, chassisCenterY, bounds.center.z);
            bodyCollider.size = new Vector3(
                Mathf.Max(bounds.size.x * ChassisWidthScale, 0.4f),
                Mathf.Max(chassisHeight, 0.35f),
                Mathf.Max(bounds.size.z * ChassisLengthScale, 0.8f));
        }

        if (cabinCollider != null)
        {
            cabinCollider.enabled = UseUpperCabinCollider;
            if (UseUpperCabinCollider)
            {
                float radius = Mathf.Max(Mathf.Min(bounds.size.x, bounds.size.y) * 0.18f, 0.16f);
                cabinCollider.center = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.62f, bounds.center.z - bounds.extents.z * 0.15f);
                cabinCollider.radius = radius;
                cabinCollider.height = Mathf.Max(bounds.size.z * 0.32f, radius * 2.1f);
                cabinCollider.direction = 2;
            }
        }

        if (FitCenterOfMassToGeneratedBody)
        {
            body.centerOfMass = new Vector3(
                bounds.center.x,
                bounds.min.y + Mathf.Max(bounds.size.y * GeneratedCenterOfMassHeight, 0.2f),
                bounds.center.z);
        }
        else
        {
            body.centerOfMass = CenterOfMass;
        }
    }

    private void FitAttachmentPoints(Bounds bounds)
    {
        if (cameraTarget != null)
        {
            cameraTarget.localPosition = new Vector3(bounds.center.x, bounds.max.y + 0.75f, bounds.center.z - bounds.extents.z * 0.08f);
        }

        if (driverSeat != null)
        {
            driverSeat.localPosition = new Vector3(bounds.center.x, bounds.max.y + 0.08f, bounds.center.z - bounds.extents.z * 0.25f);
        }

        if (!AutoFitSocketsToGeneratedBody || wheelSockets == null)
        {
            return;
        }

        float halfWidth = Mathf.Max(bounds.extents.x + 0.18f, 0.75f);
        float frontZ = Mathf.Lerp(bounds.center.z, bounds.max.z, 0.65f);
        float rearZ = Mathf.Lerp(bounds.center.z, bounds.min.z, 0.65f);

        SetSocketPosition(WheelSocketId.FL, new Vector3(-halfWidth, GetWheelSocketY(bounds, WheelSocketId.FL), frontZ));
        SetSocketPosition(WheelSocketId.FR, new Vector3(halfWidth, GetWheelSocketY(bounds, WheelSocketId.FR), frontZ));
        SetSocketPosition(WheelSocketId.RL, new Vector3(-halfWidth, GetWheelSocketY(bounds, WheelSocketId.RL), rearZ));
        SetSocketPosition(WheelSocketId.RR, new Vector3(halfWidth, GetWheelSocketY(bounds, WheelSocketId.RR), rearZ));
    }

    private void SetSocketPosition(WheelSocketId socketId, Vector3 localPosition)
    {
        WheelSocket socket = GetSocket(socketId);
        if (socket != null)
        {
            socket.transform.localPosition = localPosition;
        }
    }

    private float GetWheelSocketY(Bounds bounds, WheelSocketId socketId)
    {
        if (!AlignWheelSocketsByBodyBottom)
        {
            return Mathf.Max(bounds.min.y - 0.05f, 0.35f);
        }

        return bounds.min.y + GetWheelRadius(socketId) + WheelBodyBottomClearance;
    }

    private float GetWheelRadius(WheelSocketId socketId)
    {
        WheelSocket socket = GetSocket(socketId);
        if (socket != null && socket.InstalledWheel != null)
        {
            return socket.InstalledWheel.Radius;
        }

        return Mathf.Max(0.05f, FallbackWheelRadius);
    }

    private static Bounds CalculateLocalRendererBounds(Transform root, Transform relativeTo)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        bool hasBounds = false;
        Bounds bounds = new Bounds();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] is LineRenderer)
            {
                continue;
            }

            Bounds rendererBounds = renderers[i].bounds;
            Vector3 min = rendererBounds.min;
            Vector3 max = rendererBounds.max;
            EncapsulateWorldPoint(ref bounds, ref hasBounds, relativeTo, new Vector3(min.x, min.y, min.z));
            EncapsulateWorldPoint(ref bounds, ref hasBounds, relativeTo, new Vector3(min.x, min.y, max.z));
            EncapsulateWorldPoint(ref bounds, ref hasBounds, relativeTo, new Vector3(min.x, max.y, min.z));
            EncapsulateWorldPoint(ref bounds, ref hasBounds, relativeTo, new Vector3(min.x, max.y, max.z));
            EncapsulateWorldPoint(ref bounds, ref hasBounds, relativeTo, new Vector3(max.x, min.y, min.z));
            EncapsulateWorldPoint(ref bounds, ref hasBounds, relativeTo, new Vector3(max.x, min.y, max.z));
            EncapsulateWorldPoint(ref bounds, ref hasBounds, relativeTo, new Vector3(max.x, max.y, min.z));
            EncapsulateWorldPoint(ref bounds, ref hasBounds, relativeTo, new Vector3(max.x, max.y, max.z));
        }

        return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.zero);
    }

    private static void EncapsulateWorldPoint(ref Bounds bounds, ref bool hasBounds, Transform relativeTo, Vector3 worldPoint)
    {
        Vector3 localPoint = relativeTo.InverseTransformPoint(worldPoint);
        if (!hasBounds)
        {
            bounds = new Bounds(localPoint, Vector3.zero);
            hasBounds = true;
            return;
        }

        bounds.Encapsulate(localPoint);
    }

    private float FindGroundY(Vector3 nearPosition)
    {
        Vector3 origin = nearPosition + Vector3.up * 3f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float bestY = 0f;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hitCollider.GetComponentInParent<VehicleController>() != null)
            {
                continue;
            }

            if (hits[i].distance < bestDistance)
            {
                bestDistance = hits[i].distance;
                bestY = hits[i].point.y;
            }
        }

        return bestY;
    }

    private void OnDrawGizmos()
    {
        if (!showColliderDebug)
        {
            return;
        }

        CacheBodyColliders();
        DrawBoxColliderGizmo(bodyCollider, new Color(0.1f, 1f, 0.2f, 0.75f));
        DrawCapsuleApproxGizmo(cabinCollider, new Color(1f, 0.85f, 0.1f, 0.75f));

        if (generatedBodyRoot != null)
        {
            Bounds bounds = CalculateLocalRendererBounds(generatedBodyRoot, transform);
            if (bounds.size.sqrMagnitude > 0.0001f)
            {
                Matrix4x4 previous = Gizmos.matrix;
                Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.65f);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
                Gizmos.matrix = previous;
            }
        }
    }

    private static void DrawBoxColliderGizmo(BoxCollider collider, Color color)
    {
        if (collider == null || !collider.enabled)
        {
            return;
        }

        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.color = color;
        Gizmos.matrix = collider.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(collider.center, collider.size);
        Gizmos.matrix = previous;
    }

    private static void DrawCapsuleApproxGizmo(CapsuleCollider collider, Color color)
    {
        if (collider == null || !collider.enabled)
        {
            return;
        }

        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.color = color;
        Gizmos.matrix = collider.transform.localToWorldMatrix;

        Vector3 axis = collider.direction == 0 ? Vector3.right : collider.direction == 1 ? Vector3.up : Vector3.forward;
        Vector3 sideA = collider.direction == 0 ? Vector3.up : Vector3.right;
        Vector3 sideB = collider.direction == 2 ? Vector3.up : Vector3.forward;
        float cylinderHalf = Mathf.Max(0f, collider.height * 0.5f - collider.radius);
        Vector3 endOffset = axis * cylinderHalf;
        Gizmos.DrawWireSphere(collider.center + endOffset, collider.radius);
        Gizmos.DrawWireSphere(collider.center - endOffset, collider.radius);
        Gizmos.DrawLine(collider.center + endOffset + sideA * collider.radius, collider.center - endOffset + sideA * collider.radius);
        Gizmos.DrawLine(collider.center + endOffset - sideA * collider.radius, collider.center - endOffset - sideA * collider.radius);
        Gizmos.DrawLine(collider.center + endOffset + sideB * collider.radius, collider.center - endOffset + sideB * collider.radius);
        Gizmos.DrawLine(collider.center + endOffset - sideB * collider.radius, collider.center - endOffset - sideB * collider.radius);

        Gizmos.matrix = previous;
    }
}
