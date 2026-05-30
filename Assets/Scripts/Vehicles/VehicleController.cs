using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public sealed class VehicleController : MonoBehaviour
{
    [SerializeField] private Rigidbody body;
    [SerializeField] private Transform generatedBodyRoot;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform driverSeat;
    [SerializeField] private BoxCollider bodyCollider;
    [SerializeField] private CapsuleCollider cabinCollider;
    [SerializeField] private WheelSocket[] wheelSockets;
    [SerializeField] private bool autoFitSocketsToGeneratedBody = true;
    [SerializeField] private float driveForce = 5200f;
    [SerializeField] private float reverseForce = 2600f;
    [SerializeField] private float turnTorque = 1900f;
    [SerializeField] private float brakeDrag = 7.5f;
    [SerializeField] private float idleDrag = 0.25f;
    [SerializeField] private float maxFlatSpeed = 24f;

    private float throttleInput;
    private float steerInput;
    private bool brakeHeld;
    private bool canDrive;

    public Transform GeneratedBodyRoot => generatedBodyRoot;
    public Transform CameraTarget => cameraTarget != null ? cameraTarget : transform;
    public Transform DriverSeat => driverSeat != null ? driverSeat : transform;
    public bool CanDrive => canDrive;
    public bool HasAllWheels => wheelSockets != null && wheelSockets.Length >= 4 && AllSocketsFilled();
    public WheelSocket[] WheelSockets => wheelSockets;

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
        body.centerOfMass = new Vector3(0f, -0.35f, 0.1f);
        body.linearDamping = idleDrag;
        body.angularDamping = 1.8f;
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
            body.linearDamping = brakeDrag;
            return;
        }

        float flatSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
        float force = throttleInput >= 0f ? driveForce : reverseForce;

        body.linearDamping = brakeHeld ? brakeDrag : idleDrag;

        if (!brakeHeld && Mathf.Abs(flatSpeed) < maxFlatSpeed)
        {
            body.AddForce(transform.forward * (throttleInput * force), ForceMode.Force);
        }

        float speedFactor = Mathf.Clamp01(body.linearVelocity.magnitude / 2f);
        body.AddTorque(Vector3.up * (steerInput * turnTorque * speedFactor), ForceMode.Force);
    }

    public void SetDriveEnabled(bool enabled)
    {
        canDrive = enabled && HasAllWheels;
        body.linearDamping = canDrive ? idleDrag : brakeDrag;
        body.angularDamping = canDrive ? 1.8f : 4f;
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

        body.isKinematic = true;
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
                wheelSockets[i].UpdateWheelVisual(forwardSpeed, steerInput, Time.deltaTime);
            }
        }
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

    private void FitBodyColliders(Bounds bounds)
    {
        CacheBodyColliders();

        if (bodyCollider != null)
        {
            bodyCollider.center = bounds.center;
            bodyCollider.size = new Vector3(
                Mathf.Max(bounds.size.x, 0.4f),
                Mathf.Max(bounds.size.y, 0.35f),
                Mathf.Max(bounds.size.z, 0.8f));
        }

        if (cabinCollider != null)
        {
            float radius = Mathf.Max(Mathf.Min(bounds.size.x, bounds.size.y) * 0.25f, 0.2f);
            cabinCollider.center = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z - bounds.extents.z * 0.2f);
            cabinCollider.radius = radius;
            cabinCollider.height = Mathf.Max(bounds.size.z * 0.42f, radius * 2.1f);
            cabinCollider.direction = 2;
        }

        body.centerOfMass = new Vector3(bounds.center.x, bounds.min.y - 0.15f, bounds.center.z);
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

        if (!autoFitSocketsToGeneratedBody || wheelSockets == null)
        {
            return;
        }

        float halfWidth = Mathf.Max(bounds.extents.x + 0.18f, 0.75f);
        float frontZ = Mathf.Lerp(bounds.center.z, bounds.max.z, 0.65f);
        float rearZ = Mathf.Lerp(bounds.center.z, bounds.min.z, 0.65f);
        float socketY = Mathf.Max(bounds.min.y - 0.05f, 0.35f);

        SetSocketPosition(WheelSocketId.FL, new Vector3(-halfWidth, socketY, frontZ));
        SetSocketPosition(WheelSocketId.FR, new Vector3(halfWidth, socketY, frontZ));
        SetSocketPosition(WheelSocketId.RL, new Vector3(-halfWidth, socketY, rearZ));
        SetSocketPosition(WheelSocketId.RR, new Vector3(halfWidth, socketY, rearZ));
    }

    private void SetSocketPosition(WheelSocketId socketId, Vector3 localPosition)
    {
        WheelSocket socket = GetSocket(socketId);
        if (socket != null)
        {
            socket.transform.localPosition = localPosition;
        }
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
}
