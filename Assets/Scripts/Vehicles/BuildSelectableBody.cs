using UnityEngine;

public sealed class BuildSelectableBody : MonoBehaviour
{
    private const float RotationStepDegrees = 90f;

    [SerializeField] private BoxCollider selectionCollider;
    [SerializeField] private RuntimeSelectionOutline selectionOutline;

    public Bounds WorldBounds => CalculateBounds();

    public void RotateStep(BuildRotationAxis axis, int direction)
    {
        Vector3 rotationAxis = axis switch
        {
            BuildRotationAxis.X => Vector3.right,
            BuildRotationAxis.Y => Vector3.up,
            BuildRotationAxis.Z => Vector3.forward,
            _ => Vector3.up
        };

        int signedDirection = direction < 0 ? -1 : 1;
        transform.localRotation = Quaternion.AngleAxis(RotationStepDegrees * signedDirection, rotationAxis) * transform.localRotation;
        RefitColliderToRenderers();
    }

    private void Awake()
    {
        EnsureRuntimeSetup();
    }

    public void EnsureRuntimeSetup()
    {
        if (selectionCollider == null)
        {
            selectionCollider = GetComponent<BoxCollider>();
        }

        if (selectionCollider == null)
        {
            selectionCollider = gameObject.AddComponent<BoxCollider>();
        }

        selectionCollider.isTrigger = true;
        FitColliderToRenderers();

        if (selectionOutline == null)
        {
            selectionOutline = GetComponent<RuntimeSelectionOutline>();
        }

        if (selectionOutline == null)
        {
            selectionOutline = gameObject.AddComponent<RuntimeSelectionOutline>();
        }

        selectionOutline.SetSelected(false);
    }

    public void RefitColliderToRenderers()
    {
        if (selectionCollider == null)
        {
            selectionCollider = GetComponent<BoxCollider>();
        }

        if (selectionCollider != null)
        {
            FitColliderToRenderers();
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectionOutline == null)
        {
            EnsureRuntimeSetup();
        }

        selectionOutline.SetSelected(selected);
    }

    private void LateUpdate()
    {
        if (selectionOutline != null)
        {
            selectionOutline.SetBounds(WorldBounds);
        }
    }

    private void FitColliderToRenderers()
    {
        Bounds localBounds = CalculateLocalRendererBounds();
        if (localBounds.size.sqrMagnitude < 0.0001f)
        {
            selectionCollider.center = Vector3.zero;
            selectionCollider.size = Vector3.one;
            return;
        }

        selectionCollider.center = localBounds.center;
        selectionCollider.size = localBounds.size;
    }

    private Bounds CalculateBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(transform.position, Vector3.one);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] is LineRenderer)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        if (!hasBounds)
        {
            return new Bounds(transform.position, Vector3.one);
        }

        return bounds;
    }

    private Bounds CalculateLocalRendererBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] is LineRenderer)
            {
                continue;
            }

            Bounds rendererBounds = renderers[i].bounds;
            Vector3 min = rendererBounds.min;
            Vector3 max = rendererBounds.max;
            EncapsulateLocalPoint(ref bounds, ref hasBounds, new Vector3(min.x, min.y, min.z));
            EncapsulateLocalPoint(ref bounds, ref hasBounds, new Vector3(min.x, min.y, max.z));
            EncapsulateLocalPoint(ref bounds, ref hasBounds, new Vector3(min.x, max.y, min.z));
            EncapsulateLocalPoint(ref bounds, ref hasBounds, new Vector3(min.x, max.y, max.z));
            EncapsulateLocalPoint(ref bounds, ref hasBounds, new Vector3(max.x, min.y, min.z));
            EncapsulateLocalPoint(ref bounds, ref hasBounds, new Vector3(max.x, min.y, max.z));
            EncapsulateLocalPoint(ref bounds, ref hasBounds, new Vector3(max.x, max.y, min.z));
            EncapsulateLocalPoint(ref bounds, ref hasBounds, new Vector3(max.x, max.y, max.z));
        }

        return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.zero);
    }

    private void EncapsulateLocalPoint(ref Bounds bounds, ref bool hasBounds, Vector3 worldPoint)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        if (!hasBounds)
        {
            bounds = new Bounds(localPoint, Vector3.zero);
            hasBounds = true;
            return;
        }

        bounds.Encapsulate(localPoint);
    }
}
