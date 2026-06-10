using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public sealed class KartController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KartDriveConfig driveConfig;
    [SerializeField] private Rigidbody body;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform[] frontWheelVisuals;
    [SerializeField] private Transform[] rearWheelVisuals;
    [SerializeField] private bool inputEnabled = true;
    [SerializeField] private float wheelRadius = 0.34f;

    [Header("Grounding")]
    [SerializeField] private float groundRayLength = 1.1f;
    [SerializeField] private LayerMask groundMask = ~0;

    private float throttleInput;
    private float steerInput;
    private bool brakeHeld;
    private bool driftHeld;
    private bool grounded;
    private bool drifting;
    private int driftDirection;
    private float driftTime;
    private float driftRecoveryTimer;
    private float boostTimer;
    private float boostMultiplier = 1f;
    private int miniBoostBurstId;
    private float lastMiniBoostDriftTime;
    private float wheelRoll;

    public Transform CameraTarget => cameraTarget != null ? cameraTarget : transform;
    public KartDriveConfig DriveConfig => driveConfig;
    public bool Grounded => grounded;
    public bool Boosting => boostTimer > 0f;
    public float SpeedMps => body != null ? Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude : 0f;
    public float SpeedKph => SpeedMps * 3.6f;
    public float ForwardSpeedMps => body != null ? Vector3.Dot(body.linearVelocity, transform.forward) : 0f;
    public float ForwardSpeedKph => Mathf.Max(0f, ForwardSpeedMps) * 3.6f;
    public float ThrottleInput => throttleInput;
    public float SteerInput => steerInput;
    public float VisualSteerInput => steerInput;
    public float VisualSteerAngle => steerInput * 26f;
    public bool InputEnabled => inputEnabled;
    public bool Drifting => drifting;
    public float DriftTime => drifting ? driftTime : 0f;
    public int MiniBoostBurstId => miniBoostBurstId;
    public float LastMiniBoostDriftTime => lastMiniBoostDriftTime;

    private void Reset()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        ApplyConfigToBody();
    }

    private void Update()
    {
        ReadInput();
        AnimateWheelVisuals();
    }

    private void FixedUpdate()
    {
        if (driveConfig == null || body == null)
        {
            return;
        }

        grounded = Physics.Raycast(transform.position + Vector3.up * 0.25f, Vector3.down, groundRayLength, groundMask, QueryTriggerInteraction.Ignore);
        if (!inputEnabled)
        {
            TickBoost();
            return;
        }

        Vector3 flatVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
        float signedSpeed = Vector3.Dot(flatVelocity, transform.forward);
        float speed01 = Mathf.Clamp01(Mathf.Abs(signedSpeed) / Mathf.Max(0.01f, driveConfig.maxSpeed));

        TickBoost();
        UpdateDriftState(signedSpeed, flatVelocity);
        ApplyDrive(signedSpeed, flatVelocity);
        ApplySteering(signedSpeed, speed01);
        ApplyGrip(flatVelocity);
        ApplyAirborneGravity();
        ApplyStability(flatVelocity);
        ClampForwardSpeed();
    }

    public void SetDriveConfig(KartDriveConfig config)
    {
        driveConfig = config;
        ApplyConfigToBody();
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        throttleInput = 0f;
        steerInput = 0f;
        brakeHeld = false;
        driftHeld = false;
        drifting = false;
        driftDirection = 0;
        driftTime = 0f;
        driftRecoveryTimer = 0f;

        if (body == null)
        {
            return;
        }

        if (!enabled)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            return;
        }

        body.isKinematic = false;
        body.WakeUp();
    }

    public void SetWheelVisuals(Transform[] frontWheels, Transform[] rearWheels, float radius)
    {
        frontWheelVisuals = frontWheels;
        rearWheelVisuals = rearWheels;
        wheelRadius = Mathf.Max(0.05f, radius);
    }

    public void ApplyBoost(float speedMultiplier, float duration)
    {
        if (driveConfig == null || body == null)
        {
            return;
        }

        boostMultiplier = Mathf.Max(boostMultiplier, speedMultiplier);
        boostTimer = Mathf.Max(boostTimer, duration);

        Vector3 currentVelocity = body.linearVelocity;
        Vector3 flatVelocity = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
        Vector3 verticalVelocity = currentVelocity - flatVelocity;
        float forwardSpeed = Vector3.Dot(flatVelocity, transform.forward);
        Vector3 lateralVelocity = flatVelocity - transform.forward * forwardSpeed;
        float boostKick = Mathf.Max(4f, driveConfig.boostForce / Mathf.Max(1f, body.mass) * 0.35f);
        float speedLimit = GetCurrentForwardSpeedLimit();
        float boostedForwardSpeed = Mathf.Min(
            Mathf.Max(forwardSpeed + boostKick, boostKick),
            speedLimit);

        body.linearVelocity = lateralVelocity + transform.forward * boostedForwardSpeed + verticalVelocity;
        body.WakeUp();
    }

    private void ReadInput()
    {
        throttleInput = 0f;
        steerInput = 0f;
        brakeHeld = false;
        driftHeld = false;

        if (!inputEnabled)
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
            driftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            throttleInput += stick.y;
            steerInput += stick.x;
            brakeHeld |= gamepad.buttonSouth.isPressed;
            driftHeld |= gamepad.rightShoulder.isPressed || gamepad.leftShoulder.isPressed;
        }

        throttleInput = Mathf.Clamp(throttleInput, -1f, 1f);
        steerInput = Mathf.Clamp(steerInput, -1f, 1f);
    }

    private void ApplyDrive(float signedSpeed, Vector3 flatVelocity)
    {
        if (brakeHeld && flatVelocity.sqrMagnitude > 0.05f)
        {
            body.AddForce(-flatVelocity.normalized * driveConfig.brakeForce, ForceMode.Force);
            return;
        }

        if (Mathf.Abs(throttleInput) < 0.01f)
        {
            return;
        }

        bool changingDirection = Mathf.Sign(throttleInput) != Mathf.Sign(signedSpeed) && Mathf.Abs(signedSpeed) > 1.5f;
        float speedLimit = throttleInput >= 0f
            ? GetCurrentForwardSpeedLimit()
            : driveConfig.maxReverseSpeed;
        float speedInThrottleDirection = throttleInput >= 0f ? signedSpeed : -signedSpeed;
        if (!changingDirection && speedInThrottleDirection >= speedLimit)
        {
            return;
        }

        float force = throttleInput >= 0f ? driveConfig.accelerationForce * boostMultiplier : driveConfig.reverseForce;
        body.AddForce(transform.forward * (throttleInput * force), ForceMode.Force);
    }

    private void ApplySteering(float signedSpeed, float speed01)
    {
        if (!grounded)
        {
            return;
        }

        float turnRate = Mathf.Lerp(driveConfig.lowSpeedTurnRate, driveConfig.highSpeedTurnRate, speed01);
        if (Mathf.Abs(signedSpeed) < 0.5f)
        {
            turnRate *= 0.35f;
        }
        else if (signedSpeed < 0f)
        {
            turnRate *= -driveConfig.reverseSteerScale;
        }

        float steer = steerInput;
        if (drifting && driftDirection != 0)
        {
            bool counterSteering = Mathf.Abs(steer) > 0.01f && Mathf.Sign(steer) != driftDirection;
            turnRate *= counterSteering
                ? driveConfig.driftCounterSteerMultiplier
                : driveConfig.driftTurnMultiplier;

            if (Mathf.Abs(steer) < 0.01f)
            {
                steer = driftDirection * 0.35f;
            }
        }
        else if (Mathf.Abs(steer) < 0.01f)
        {
            return;
        }

        Quaternion yaw = Quaternion.Euler(0f, steer * turnRate * Time.fixedDeltaTime, 0f);
        body.MoveRotation(body.rotation * yaw);
    }

    private void ApplyGrip(Vector3 flatVelocity)
    {
        if (!grounded || flatVelocity.sqrMagnitude < 0.01f)
        {
            return;
        }

        float gripScale = 1f;
        if (drifting)
        {
            gripScale = driveConfig.driftGripScale;
        }
        else if (driftRecoveryTimer > 0f)
        {
            float recovery01 = 1f - driftRecoveryTimer / Mathf.Max(0.01f, driveConfig.driftRecoveryDuration);
            gripScale = Mathf.Lerp(driveConfig.driftRecoveryGripMultiplier, 1f, recovery01);
        }

        float grip = driveConfig.lateralGrip * gripScale;
        Vector3 lateralVelocity = Vector3.Project(flatVelocity, transform.right);
        body.AddForce(-lateralVelocity * (grip * body.mass), ForceMode.Force);
    }

    private void UpdateDriftState(float signedSpeed, Vector3 flatVelocity)
    {
        if (driftRecoveryTimer > 0f)
        {
            driftRecoveryTimer = Mathf.Max(0f, driftRecoveryTimer - Time.fixedDeltaTime);
        }

        if (!grounded)
        {
            EndDrift(false);
            return;
        }

        float absSpeed = Mathf.Abs(signedSpeed);
        if (!drifting)
        {
            bool canStartDrift = driftHeld
                && signedSpeed > driveConfig.driftMinSpeed
                && Mathf.Abs(steerInput) >= driveConfig.driftSteerThreshold;

            if (canStartDrift)
            {
                BeginDrift(Mathf.Sign(steerInput) >= 0f ? 1 : -1, flatVelocity);
            }

            return;
        }

        driftTime += Time.fixedDeltaTime;
        bool stillFastEnough = absSpeed >= driveConfig.driftMinSpeed * 0.45f;
        if (!driftHeld || !stillFastEnough)
        {
            EndDrift(!driftHeld && stillFastEnough);
        }
    }

    private void BeginDrift(int direction, Vector3 flatVelocity)
    {
        drifting = true;
        driftDirection = direction;
        driftTime = 0f;
        driftRecoveryTimer = 0f;

        Vector3 verticalVelocity = body.linearVelocity - flatVelocity;
        Vector3 forwardVelocity = transform.forward * Mathf.Max(0f, Vector3.Dot(flatVelocity, transform.forward));
        Vector3 lateralKick = -transform.right * (driftDirection * driveConfig.driftEntrySideImpulse);
        body.linearVelocity = forwardVelocity + lateralKick + verticalVelocity;

        if (driveConfig.driftEntryYawKick > 0f)
        {
            body.AddTorque(Vector3.up * (driftDirection * driveConfig.driftEntryYawKick), ForceMode.VelocityChange);
        }
    }

    private void EndDrift(bool releasedByPlayer)
    {
        if (!drifting)
        {
            return;
        }

        bool earnedMiniBoost = releasedByPlayer
            && driftTime >= driveConfig.driftMiniBoostMinDuration
            && throttleInput > 0.1f;

        drifting = false;
        driftDirection = 0;
        driftRecoveryTimer = driveConfig.driftRecoveryDuration;

        if (earnedMiniBoost)
        {
            ApplyDriftMiniBoost();
        }

        driftTime = 0f;
    }

    private void ApplyDriftMiniBoost()
    {
        if (driveConfig.driftMiniBoostSpeedGain <= 0f || body == null)
        {
            return;
        }

        Vector3 currentVelocity = body.linearVelocity;
        Vector3 flatVelocity = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
        Vector3 verticalVelocity = currentVelocity - flatVelocity;
        float forwardSpeed = Vector3.Dot(flatVelocity, transform.forward);
        Vector3 lateralVelocity = flatVelocity - transform.forward * forwardSpeed;
        boostTimer = Mathf.Max(boostTimer, driveConfig.driftMiniBoostDuration);
        boostMultiplier = Mathf.Max(boostMultiplier, driveConfig.boostSpeedMultiplier);
        float speedLimit = GetCurrentForwardSpeedLimit();
        float boostedForwardSpeed = Mathf.Min(
            Mathf.Max(forwardSpeed + driveConfig.driftMiniBoostSpeedGain, driveConfig.driftMiniBoostSpeedGain),
            speedLimit);

        body.linearVelocity = lateralVelocity + transform.forward * boostedForwardSpeed + verticalVelocity;
        lastMiniBoostDriftTime = driftTime;
        miniBoostBurstId++;
        body.WakeUp();
    }

    private void ApplyAirborneGravity()
    {
        if (grounded || driveConfig.airborneGravityMultiplier <= 1f)
        {
            return;
        }

        body.AddForce(Physics.gravity * (driveConfig.airborneGravityMultiplier - 1f), ForceMode.Acceleration);
    }

    private void ApplyStability(Vector3 flatVelocity)
    {
        if (driveConfig.downforce > 0f && flatVelocity.sqrMagnitude > 0.01f)
        {
            body.AddForce(-transform.up * (driveConfig.downforce * flatVelocity.sqrMagnitude), ForceMode.Force);
        }

        if (driveConfig.uprightStability <= 0f)
        {
            return;
        }

        Vector3 correctionAxis = Vector3.Cross(transform.up, Vector3.up);
        body.AddTorque(correctionAxis * (driveConfig.uprightStability * body.mass), ForceMode.Force);
    }

    private void ClampForwardSpeed()
    {
        Vector3 currentVelocity = body.linearVelocity;
        Vector3 flatVelocity = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
        float signedSpeed = Vector3.Dot(flatVelocity, transform.forward);
        float speedLimit = signedSpeed >= 0f
            ? GetCurrentForwardSpeedLimit()
            : driveConfig.maxReverseSpeed;

        if (Mathf.Abs(signedSpeed) <= speedLimit)
        {
            return;
        }

        Vector3 verticalVelocity = currentVelocity - flatVelocity;
        Vector3 lateralVelocity = flatVelocity - transform.forward * signedSpeed;
        float clampedSpeed = GetClampedForwardSpeed(signedSpeed, speedLimit);
        body.linearVelocity = verticalVelocity + lateralVelocity + transform.forward * clampedSpeed;
    }

    private float GetClampedForwardSpeed(float signedSpeed, float speedLimit)
    {
        if (signedSpeed < 0f || boostTimer > 0f || driveConfig.boostOverspeedReturnRate <= 0f)
        {
            return Mathf.Sign(signedSpeed) * speedLimit;
        }

        return Mathf.MoveTowards(signedSpeed, speedLimit, driveConfig.boostOverspeedReturnRate * Time.fixedDeltaTime);
    }

    private float GetCurrentForwardSpeedLimit()
    {
        if (driveConfig == null)
        {
            return 0f;
        }

        float multiplier = boostTimer > 0f ? Mathf.Max(1f, driveConfig.boostMaxSpeedMultiplier) : 1f;
        return driveConfig.maxSpeed * multiplier;
    }

    private void TickBoost()
    {
        if (boostTimer <= 0f)
        {
            boostMultiplier = 1f;
            return;
        }

        boostTimer -= Time.fixedDeltaTime;
        if (boostTimer <= 0f)
        {
            boostMultiplier = 1f;
        }
    }

    private void AnimateWheelVisuals()
    {
        float distance = SpeedMps * Time.deltaTime;
        wheelRoll += distance / Mathf.Max(0.05f, wheelRadius) * Mathf.Rad2Deg;

        float steerAngle = steerInput * 26f;
        ApplyWheelArray(frontWheelVisuals, steerAngle, wheelRoll);
        ApplyWheelArray(rearWheelVisuals, 0f, wheelRoll);
    }

    private static void ApplyWheelArray(Transform[] wheels, float steerAngle, float rollAngle)
    {
        if (wheels == null)
        {
            return;
        }

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] != null)
            {
                wheels[i].localRotation = Quaternion.Euler(rollAngle, steerAngle, 0f);
            }
        }
    }

    private void ApplyConfigToBody()
    {
        if (body == null || driveConfig == null)
        {
            return;
        }

        body.mass = driveConfig.mass;
        body.centerOfMass = driveConfig.centerOfMass;
        body.linearDamping = driveConfig.linearDrag;
        body.angularDamping = driveConfig.angularDrag;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
}
