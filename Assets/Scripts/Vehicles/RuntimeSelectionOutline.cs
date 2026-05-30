using UnityEngine;

public sealed class RuntimeSelectionOutline : MonoBehaviour
{
    private const int EdgeCount = 12;

    [SerializeField] private Color outlineColor = new Color(1f, 0.85f, 0.05f, 1f);
    [SerializeField] private float lineWidth = 0.035f;

    private readonly LineRenderer[] lines = new LineRenderer[EdgeCount];
    private bool selected;
    private Bounds currentBounds;

    private void Awake()
    {
        EnsureLines();
    }

    public void SetSelected(bool isSelected)
    {
        selected = isSelected;
        EnsureLines();
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] != null)
            {
                lines[i].enabled = selected;
            }
        }
    }

    public void SetBounds(Bounds bounds)
    {
        currentBounds = bounds;
        if (!selected)
        {
            return;
        }

        EnsureLines();
        UpdateLinePositions();
    }

    private void EnsureLines()
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] != null)
            {
                continue;
            }

            GameObject lineObject = new GameObject($"OutlineEdge_{i:00}");
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.material = CreateLineMaterial();
            line.startColor = outlineColor;
            line.endColor = outlineColor;
            line.enabled = selected;
            lines[i] = line;
        }
    }

    private static Material CreateLineMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material material = new Material(shader);
        material.color = Color.white;
        return material;
    }

    private void UpdateLinePositions()
    {
        Vector3 min = currentBounds.min;
        Vector3 max = currentBounds.max;

        Vector3 p000 = new Vector3(min.x, min.y, min.z);
        Vector3 p001 = new Vector3(min.x, min.y, max.z);
        Vector3 p010 = new Vector3(min.x, max.y, min.z);
        Vector3 p011 = new Vector3(min.x, max.y, max.z);
        Vector3 p100 = new Vector3(max.x, min.y, min.z);
        Vector3 p101 = new Vector3(max.x, min.y, max.z);
        Vector3 p110 = new Vector3(max.x, max.y, min.z);
        Vector3 p111 = new Vector3(max.x, max.y, max.z);

        SetEdge(0, p000, p001);
        SetEdge(1, p001, p101);
        SetEdge(2, p101, p100);
        SetEdge(3, p100, p000);
        SetEdge(4, p010, p011);
        SetEdge(5, p011, p111);
        SetEdge(6, p111, p110);
        SetEdge(7, p110, p010);
        SetEdge(8, p000, p010);
        SetEdge(9, p001, p011);
        SetEdge(10, p101, p111);
        SetEdge(11, p100, p110);
    }

    private void SetEdge(int index, Vector3 a, Vector3 b)
    {
        lines[index].SetPosition(0, a);
        lines[index].SetPosition(1, b);
    }
}
