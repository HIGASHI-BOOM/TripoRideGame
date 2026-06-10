using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public sealed class KartFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [FormerlySerializedAs("localOffset")]
    [SerializeField] private Vector3 chaseLocalOffset = new Vector3(0f, 4.2f, -7.5f);
    [SerializeField] private Vector3 frontLocalOffset = new Vector3(0f, 2.25f, 6.4f);
    [SerializeField] private float followSharpness = 9f;
    [FormerlySerializedAs("lookAhead")]
    [SerializeField] private float chaseLookAhead = 4f;
    [SerializeField] private float frontLookBack = 2.2f;
    [SerializeField] private Vector2 speedFovKphRange = new Vector2(125f, 250f);
    [SerializeField] private Vector2 fovRange = new Vector2(110f, 155f);
    [SerializeField] private Vector2 frontFovRange = new Vector2(92f, 120f);
    [SerializeField] private float fovSharpness = 8f;
    [SerializeField] private Key toggleKey = Key.C;

    private Camera targetCamera;
    private KartController targetKart;
    private bool frontView;

    public Transform Target
    {
        get => target;
        set
        {
            target = value;
            ResolveTargetKart();
        }
    }

    public bool FrontView => frontView;

    public void ToggleView()
    {
        SetFrontView(!frontView);
    }

    public void SetFrontView(bool enabled)
    {
        frontView = enabled;
    }

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        ResolveTargetKart();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        bool keyboardPressed = keyboard != null && keyboard[toggleKey].wasPressedThisFrame;
        Gamepad gamepad = Gamepad.current;
        bool gamepadPressed = gamepad != null && gamepad.rightShoulder.wasPressedThisFrame;

        if (keyboardPressed || gamepadPressed)
        {
            ToggleView();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        Vector3 desiredPosition = target.TransformPoint(frontView ? frontLocalOffset : chaseLocalOffset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);

        Vector3 focus = frontView
            ? target.position - target.forward * frontLookBack + Vector3.up * 0.75f
            : target.position + target.forward * chaseLookAhead + Vector3.up * 0.7f;
        Vector3 lookDirection = focus - transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection, Vector3.up), blend);
        }

        ApplySpeedFov();
    }

    private void ResolveTargetKart()
    {
        targetKart = target != null ? target.GetComponentInParent<KartController>() : null;
    }

    private void ApplySpeedFov()
    {
        if (targetCamera == null)
        {
            return;
        }

        if (targetKart == null)
        {
            ResolveTargetKart();
        }

        float speedKph = targetKart != null ? targetKart.ForwardSpeedKph : 0f;
        float speed01 = GetRange01(speedFovKphRange, speedKph);
        Vector2 activeFovRange = frontView ? frontFovRange : fovRange;
        float targetFov = Mathf.Lerp(activeFovRange.x, activeFovRange.y, speed01);
        if (fovSharpness <= 0f)
        {
            targetCamera.fieldOfView = targetFov;
            return;
        }

        float blend = 1f - Mathf.Exp(-fovSharpness * Time.deltaTime);
        targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFov, blend);
    }

    private static float GetRange01(Vector2 range, float value)
    {
        if (Mathf.Approximately(range.x, range.y))
        {
            return value >= range.x ? 1f : 0f;
        }

        return Mathf.InverseLerp(range.x, range.y, value);
    }
}
