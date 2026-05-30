using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BuildTransformGizmo : MonoBehaviour
{
    private const float AxisLength = 1.45f;
    private const float HandleRadius = 0.08f;
    private const float FrontPadding = 0.45f;
    private const float ClickHandleRadius = 0.22f;

    private Camera targetCamera;
    private Transform selectedTransform;
    private BuildSelectableBody selectedBody;
    private Axis activeAxis = Axis.None;
    private Vector3 dragStartPosition;
    private float dragStartAxisValue;
    private bool dragging;

    private enum Axis
    {
        None,
        X,
        Y,
        Z
    }

    public bool IsDragging => dragging;

    private void Awake()
    {
        BuildAxis(Axis.X, Color.red, Vector3.right, Quaternion.Euler(0f, 0f, 90f));
        BuildAxis(Axis.Y, Color.green, Vector3.up, Quaternion.identity);
        BuildAxis(Axis.Z, Color.blue, Vector3.forward, Quaternion.Euler(90f, 0f, 0f));
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!GameInputContext.BuildEnabled)
        {
            dragging = false;
            activeAxis = Axis.None;
            return;
        }

        if (selectedTransform == null)
        {
            gameObject.SetActive(false);
            return;
        }

        Bounds bounds = selectedBody != null ? selectedBody.WorldBounds : new Bounds(selectedTransform.position, Vector3.one);
        transform.position = GetFrontGizmoPosition(bounds);
        transform.rotation = Quaternion.identity;

        if (Mouse.current == null || targetCamera == null)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryBeginDrag();
        }
        else if (Mouse.current.leftButton.isPressed && dragging)
        {
            ContinueDrag();
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            dragging = false;
            activeAxis = Axis.None;
        }
    }

    public void SetSelection(BuildSelectableBody body, Camera camera)
    {
        selectedBody = body;
        selectedTransform = body != null ? body.transform : null;
        targetCamera = camera;
        dragging = false;
        activeAxis = Axis.None;
        gameObject.SetActive(selectedTransform != null);
    }

    private void TryBeginDrag()
    {
        Ray ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        BuildGizmoAxisHandle handle = RaycastAxisHandle(ray);
        if (handle == null)
        {
            return;
        }

        activeAxis = (Axis)handle.AxisValue;
        dragging = true;
        dragStartPosition = selectedTransform.position;
        dragStartAxisValue = GetMouseAxisValue(activeAxis);
    }

    private void ContinueDrag()
    {
        float currentValue = GetMouseAxisValue(activeAxis);
        float delta = currentValue - dragStartAxisValue;
        selectedTransform.position = dragStartPosition + GetAxisVector(activeAxis) * delta;
    }

    private float GetMouseAxisValue(Axis axis)
    {
        Ray ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Vector3 axisVector = GetAxisVector(axis);
        Vector3 axisOrigin = transform.position;
        Vector3 closestOnAxis;
        Vector3 closestOnRay;
        ClosestPoints(axisOrigin, axisVector, ray.origin, ray.direction, out closestOnAxis, out closestOnRay);
        return Vector3.Dot(closestOnAxis - axisOrigin, axisVector);
    }

    private static void ClosestPoints(
        Vector3 p1,
        Vector3 d1,
        Vector3 p2,
        Vector3 d2,
        out Vector3 c1,
        out Vector3 c2)
    {
        Vector3 r = p1 - p2;
        float a = Vector3.Dot(d1, d1);
        float e = Vector3.Dot(d2, d2);
        float f = Vector3.Dot(d2, r);
        float b = Vector3.Dot(d1, d2);
        float c = Vector3.Dot(d1, r);
        float denominator = a * e - b * b;
        float s = Mathf.Abs(denominator) > 0.0001f ? (b * f - c * e) / denominator : 0f;
        float t = (b * s + f) / e;

        c1 = p1 + d1 * s;
        c2 = p2 + d2 * Mathf.Max(0f, t);
    }

    private static Vector3 GetAxisVector(Axis axis)
    {
        return axis switch
        {
            Axis.X => Vector3.right,
            Axis.Y => Vector3.up,
            Axis.Z => Vector3.forward,
            _ => Vector3.zero
        };
    }

    private Vector3 GetFrontGizmoPosition(Bounds bounds)
    {
        if (targetCamera == null)
        {
            return bounds.center;
        }

        Vector3 center = bounds.center;
        Vector3 cameraDirection = targetCamera.transform.position - center;
        if (cameraDirection.sqrMagnitude < 0.0001f)
        {
            cameraDirection = -targetCamera.transform.forward;
        }

        cameraDirection.Normalize();
        float projectedExtent =
            Mathf.Abs(cameraDirection.x) * bounds.extents.x +
            Mathf.Abs(cameraDirection.y) * bounds.extents.y +
            Mathf.Abs(cameraDirection.z) * bounds.extents.z;

        return center + cameraDirection * (projectedExtent + FrontPadding);
    }

    private static BuildGizmoAxisHandle RaycastAxisHandle(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        BuildGizmoAxisHandle closestHandle = null;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            BuildGizmoAxisHandle handle = hits[i].collider.GetComponentInParent<BuildGizmoAxisHandle>();
            if (handle == null || hits[i].distance >= closestDistance)
            {
                continue;
            }

            closestHandle = handle;
            closestDistance = hits[i].distance;
        }

        return closestHandle;
    }

    private void BuildAxis(Axis axis, Color color, Vector3 direction, Quaternion rotation)
    {
        GameObject root = new GameObject($"{axis}_Axis");
        root.transform.SetParent(transform, false);

        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Shaft";
        shaft.transform.SetParent(root.transform, false);
        shaft.transform.localPosition = direction * (AxisLength * 0.5f);
        shaft.transform.localRotation = rotation;
        shaft.transform.localScale = new Vector3(HandleRadius, AxisLength * 0.5f, HandleRadius);
        shaft.GetComponent<Renderer>().sharedMaterial = CreateAxisMaterial(color);
        Collider shaftCollider = shaft.GetComponent<Collider>();
        if (shaftCollider != null)
        {
            Destroy(shaftCollider);
        }

        CapsuleCollider shaftClickCollider = shaft.AddComponent<CapsuleCollider>();
        shaftClickCollider.radius = ClickHandleRadius;
        shaftClickCollider.height = AxisLength;
        shaftClickCollider.direction = 1;
        shaftClickCollider.isTrigger = true;
        BuildGizmoAxisHandle shaftHandle = shaft.AddComponent<BuildGizmoAxisHandle>();
        shaftHandle.AxisValue = (int)axis;

        GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tip.name = "Tip";
        tip.transform.SetParent(root.transform, false);
        tip.transform.localPosition = direction * AxisLength;
        tip.transform.localScale = Vector3.one * (HandleRadius * 3.2f);
        tip.GetComponent<Renderer>().sharedMaterial = CreateAxisMaterial(color);
        SphereCollider tipCollider = tip.GetComponent<SphereCollider>();
        if (tipCollider != null)
        {
            tipCollider.radius = ClickHandleRadius / Mathf.Max(HandleRadius * 3.2f, 0.001f);
            tipCollider.isTrigger = true;
        }

        BuildGizmoAxisHandle tipHandle = tip.AddComponent<BuildGizmoAxisHandle>();
        tipHandle.AxisValue = (int)axis;
    }

    private static Material CreateAxisMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = color;
        return material;
    }
}
