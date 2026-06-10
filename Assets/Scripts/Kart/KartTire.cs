using UnityEngine;

public enum KartTireSize
{
    Small,
    Medium,
    Large
}

public sealed class KartTire : MonoBehaviour
{
    [SerializeField] private KartTireSize tireSize = KartTireSize.Medium;
    [SerializeField] private float radius = 0.34f;

    public KartTireSize TireSize => tireSize;
    public float Radius => Mathf.Max(0.05f, radius);

    public void Configure(KartTireSize size, float tireRadius)
    {
        tireSize = size;
        radius = Mathf.Max(0.05f, tireRadius);
    }
}
