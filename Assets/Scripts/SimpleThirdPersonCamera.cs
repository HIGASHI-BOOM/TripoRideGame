using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public sealed class SimpleThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.35f, 0f);
    [SerializeField] private float distance = 6f;
    [SerializeField] private float minPitch = -25f;
    [SerializeField] private float maxPitch = 65f;
    [SerializeField] private float mouseSensitivity = 0.16f;
    [SerializeField] private float gamepadSensitivity = 120f;
    [SerializeField] private float lookSmoothTime = 0.025f;
    [SerializeField] private float followSharpness = 18f;

    private Vector2 smoothedLookInput;
    private Vector2 lookInputVelocity;
    private float yaw;
    private float pitch = 18f;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    public float AimYaw => yaw;
    public Quaternion AimRotation => Quaternion.Euler(pitch, yaw, 0f);
    public Quaternion YawRotation => Quaternion.Euler(0f, yaw, 0f);

    private void Start()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        yaw = euler.y;
        pitch = NormalizePitch(euler.x);
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            LockCursor(true);
        }
    }

    private void Update()
    {
        if (!GameInputContext.PlayerEnabled)
        {
            return;
        }

        HandleCursorToggle();
        ReadLookInput();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Quaternion orbitRotation = AimRotation;
        Vector3 focusPoint = target.position + targetOffset;
        Vector3 desiredPosition = focusPoint - orbitRotation * Vector3.forward * distance;

        float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);
        transform.rotation = Quaternion.LookRotation(focusPoint - transform.position, Vector3.up);
    }

    private void ReadLookInput()
    {
        Vector2 lookInput = Vector2.zero;

        Mouse mouse = Mouse.current;
        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            lookInput += mouse.delta.ReadValue() * mouseSensitivity;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            lookInput += gamepad.rightStick.ReadValue() * (gamepadSensitivity * Time.deltaTime);
        }

        smoothedLookInput = Vector2.SmoothDamp(smoothedLookInput, lookInput, ref lookInputVelocity, lookSmoothTime);
        yaw += smoothedLookInput.x;
        pitch = Mathf.Clamp(pitch - smoothedLookInput.y, minPitch, maxPitch);
    }

    private static void HandleCursorToggle()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            LockCursor(false);
        }

        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            LockCursor(true);
        }
    }

    private static void LockCursor(bool shouldLock)
    {
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }

    private static float NormalizePitch(float pitchAngle)
    {
        return pitchAngle > 180f ? pitchAngle - 360f : pitchAngle;
    }
}
