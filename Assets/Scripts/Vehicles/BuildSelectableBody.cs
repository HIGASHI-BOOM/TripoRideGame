using UnityEngine;

public sealed class BuildSelectableBody : MonoBehaviour
{
    [SerializeField] private BoxCollider selectionCollider;
    [SerializeField] private RuntimeSelectionOutline selectionOutline;

    public Bounds WorldBounds => CalculateBounds();

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
        Bounds bounds = CalculateBounds();
        if (bounds.size.sqrMagnitude < 0.0001f)
        {
            selectionCollider.center = Vector3.zero;
            selectionCollider.size = Vector3.one;
            return;
        }

        selectionCollider.center = transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = transform.InverseTransformVector(bounds.size);
        selectionCollider.size = new Vector3(
            Mathf.Abs(localSize.x),
            Mathf.Abs(localSize.y),
            Mathf.Abs(localSize.z));
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
}
