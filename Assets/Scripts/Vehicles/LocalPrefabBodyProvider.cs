using UnityEngine;

public sealed class LocalPrefabBodyProvider : GeneratedBodyProvider
{
    [SerializeField] private GameObject[] bodyPrefabs;
    [SerializeField] private Vector3 targetSize = new Vector3(2.4f, 0.8f, 4.2f);
    [SerializeField] private int selectedIndex;

    public int BodyCount => bodyPrefabs != null ? bodyPrefabs.Length : 0;

    public void SelectBody(int index)
    {
        selectedIndex = Mathf.Max(0, index);
    }

    public override GeneratedVehicleBody CreateBody(Transform parent, VehicleBuildData buildData)
    {
        if (buildData != null)
        {
            selectedIndex = Mathf.Max(0, buildData.bodyIndex);
        }

        GameObject prefab = GetSelectedPrefab();
        GameObject body = prefab != null
            ? Instantiate(prefab, parent)
            : CreateFallbackBody(parent);

        body.name = prefab != null ? $"{prefab.name}_Runtime" : "FallbackBody_Runtime";
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.identity;
        body.transform.localScale = Vector3.one;
        Vector3 fitTarget = buildData != null ? buildData.bodyTargetSize : targetSize;
        FitBodyToTarget(body.transform, fitTarget);
        BuildSelectableBody selectableBody = body.GetComponent<BuildSelectableBody>();
        if (selectableBody == null)
        {
            selectableBody = body.AddComponent<BuildSelectableBody>();
        }

        selectableBody.EnsureRuntimeSetup();
        return new GeneratedVehicleBody(body);
    }

    private GameObject GetSelectedPrefab()
    {
        if (bodyPrefabs == null || bodyPrefabs.Length == 0)
        {
            return null;
        }

        return bodyPrefabs[Mathf.Clamp(selectedIndex, 0, bodyPrefabs.Length - 1)];
    }

    private static GameObject CreateFallbackBody(Transform parent)
    {
        GameObject root = new GameObject("FallbackBody");
        root.transform.SetParent(parent, false);

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Visual";
        cube.transform.SetParent(root.transform, false);
        cube.transform.localScale = new Vector3(2.4f, 0.65f, 4f);
        Destroy(cube.GetComponent<Collider>());
        return root;
    }

    private static void FitBodyToTarget(Transform body, Vector3 target)
    {
        Bounds bounds = CalculateBounds(body);
        if (bounds.size.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 localCenter = body.InverseTransformPoint(bounds.center);
        foreach (Transform child in body)
        {
            child.localPosition -= localCenter;
        }

        float scaleX = target.x / Mathf.Max(bounds.size.x, 0.01f);
        float scaleY = target.y / Mathf.Max(bounds.size.y, 0.01f);
        float scaleZ = target.z / Mathf.Max(bounds.size.z, 0.01f);
        float scale = Mathf.Min(scaleX, scaleY, scaleZ);
        body.localScale *= scale;

        AlignBodyBottom(body, Mathf.Max(0.35f, target.y * 0.55f));
    }

    private static void AlignBodyBottom(Transform body, float desiredBottomY)
    {
        if (body.parent == null)
        {
            return;
        }

        Bounds bounds = CalculateBounds(body);
        if (bounds.size.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float currentBottomY = body.parent.InverseTransformPoint(bounds.min).y;
        body.localPosition += Vector3.up * (desiredBottomY - currentBottomY);
    }

    private static Bounds CalculateBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(root.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }
}
