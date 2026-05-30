using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BuildOrbitCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 focusOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private float distance = 6.8f;
    [SerializeField] private float minDistance = 2.8f;
    [SerializeField] private float maxDistance = 14f;
    [SerializeField] private float orbitSensitivity = 0.18f;
    [SerializeField] private float panSensitivity = 0.008f;
    [SerializeField] private float zoomSensitivity = 0.5f;
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 75f;
    [SerializeField] private float followSharpness = 22f;

    private Vector3 focusPoint;
    private Vector3 manualPanOffset;
    private float yaw = 35f;
    private float pitch = 22f;

    public Transform Target => target;
    public Vector3 FocusPoint => focusPoint;

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Refocus(snapCamera: true);
    }

    private void Update()
    {
        if (!GameInputContext.BuildEnabled)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ReadShortcuts();
        ReadOrbit();
        ReadPan();
        ReadZoom();
    }

    private void LateUpdate()
    {
        Vector3 desiredFocus = GetTargetFocus();
        float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        focusPoint = Vector3.Lerp(focusPoint, desiredFocus + manualPanOffset, blend);
        ApplyCameraPose();
    }

    public void SetTarget(Transform newTarget, bool snapCamera)
    {
        target = newTarget;
        manualPanOffset = Vector3.zero;
        Refocus(snapCamera);
    }

    public void Refocus(bool snapCamera)
    {
        manualPanOffset = Vector3.zero;
        focusPoint = GetTargetFocus();
        if (snapCamera)
        {
            ApplyCameraPose();
        }
    }

    public void SnapToLeftSide()
    {
        SetView(-90f, 12f);
    }

    public void SnapToRightSide()
    {
        SetView(90f, 12f);
    }

    public void SnapToFront()
    {
        SetView(180f, 12f);
    }

    public void SnapToBack()
    {
        SetView(0f, 12f);
    }

    private void SetView(float viewYaw, float viewPitch)
    {
        yaw = viewYaw;
        pitch = Mathf.Clamp(viewPitch, minPitch, maxPitch);
        Refocus(snapCamera: true);
    }

    private void ReadShortcuts()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.fKey.wasPressedThisFrame)
        {
            Refocus(snapCamera: true);
        }
        else if (keyboard.digit1Key.wasPressedThisFrame)
        {
            SnapToLeftSide();
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            SnapToRightSide();
        }
        else if (keyboard.digit3Key.wasPressedThisFrame)
        {
            SnapToFront();
        }
        else if (keyboard.digit4Key.wasPressedThisFrame)
        {
            SnapToBack();
        }
    }

    private void ReadOrbit()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.rightButton.isPressed)
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        yaw += delta.x * orbitSensitivity;
        pitch = Mathf.Clamp(pitch - delta.y * orbitSensitivity, minPitch, maxPitch);
    }

    private void ReadPan()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.middleButton.isPressed)
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        Transform cameraTransform = transform;
        float scale = distance * panSensitivity;
        Vector3 panDelta = (-cameraTransform.right * delta.x - cameraTransform.up * delta.y) * scale;
        manualPanOffset += panDelta;
        focusPoint += panDelta;
    }

    private void ReadZoom()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        float scrollY = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scrollY) < 0.01f)
        {
            return;
        }

        distance = Mathf.Clamp(distance - scrollY * zoomSensitivity, minDistance, maxDistance);
    }

    private Vector3 GetTargetFocus()
    {
        return target != null ? target.position + focusOffset : focusPoint;
    }

    private void ApplyCameraPose()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = focusPoint - rotation * Vector3.forward * distance;
        transform.rotation = Quaternion.LookRotation(focusPoint - transform.position, Vector3.up);
    }
}
