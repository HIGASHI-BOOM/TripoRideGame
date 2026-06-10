using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BuildSelectionController : MonoBehaviour
{
    [SerializeField] private VehicleAssemblyManager manager;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private BuildTransformGizmo transformGizmo;
    [SerializeField] private Rect ignoredUiRect = new Rect(0f, 0f, 300f, 480f);

    private BuildSelectableBody selectedBody;
    private VehicleDemoUI demoUi;

    private void Awake()
    {
        if (manager == null)
        {
            manager = FindAnyObjectByType<VehicleAssemblyManager>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        demoUi = FindAnyObjectByType<VehicleDemoUI>();

        if (transformGizmo == null)
        {
            GameObject gizmoObject = new GameObject("RuntimeTransformGizmo");
            transformGizmo = gizmoObject.AddComponent<BuildTransformGizmo>();
        }
    }

    private void Update()
    {
        bool active = GameInputContext.BuildEnabled && manager != null && manager.VehicleGenerationMode && !manager.Driving;
        if (!active)
        {
            Select(null);
            return;
        }

        if (Mouse.current == null || targetCamera == null || transformGizmo.IsDragging)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TrySelectUnderCursor();
        }
    }

    public void Select(BuildSelectableBody body)
    {
        if (selectedBody == body)
        {
            return;
        }

        if (selectedBody != null)
        {
            selectedBody.SetSelected(false);
        }

        selectedBody = body;

        if (selectedBody != null)
        {
            selectedBody.EnsureRuntimeSetup();
            selectedBody.SetSelected(true);
        }

        if (transformGizmo != null)
        {
            transformGizmo.SetSelection(selectedBody, targetCamera);
        }
    }

    private void TrySelectUnderCursor()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Rect uiRect = demoUi != null ? demoUi.ActiveMenuRect : ignoredUiRect;
        if (uiRect.Contains(new Vector2(mousePosition.x, Screen.height - mousePosition.y)))
        {
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        if (hits.Length == 0)
        {
            Select(null);
            return;
        }

        BuildSelectableBody closestBody = null;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.GetComponentInParent<BuildGizmoAxisHandle>() != null)
            {
                return;
            }

            BuildSelectableBody body = hits[i].collider.GetComponentInParent<BuildSelectableBody>();
            if (body == null || hits[i].distance >= closestDistance)
            {
                continue;
            }

            closestBody = body;
            closestDistance = hits[i].distance;
        }

        Select(closestBody);
    }
}
