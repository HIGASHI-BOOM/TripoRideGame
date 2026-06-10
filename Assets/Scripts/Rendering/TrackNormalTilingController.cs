using UnityEngine;

[ExecuteAlways]
public sealed class TrackNormalTilingController : MonoBehaviour
{
    private const string DefaultNormalProperty = "_BumpMap";

    [SerializeField]
    [Tooltip("要调整法线贴图 UV 缩放的赛道材质。默认用于 MAT_Minimal_Asphalt。")]
    private Material targetMaterial;

    [SerializeField]
    [Tooltip("法线贴图的材质属性名。URP/Lit 的法线贴图属性通常是 _BumpMap。")]
    private string normalTextureProperty = DefaultNormalProperty;

    [SerializeField]
    [Min(0.01f)]
    [Tooltip("法线贴图的 U 方向重复次数。数值越大，柏油颗粒越密。")]
    private float normalTilingX = 18f;

    [SerializeField]
    [Min(0.01f)]
    [Tooltip("法线贴图的 V 方向重复次数。数值越大，柏油颗粒越密。")]
    private float normalTilingY = 18f;

    public Material TargetMaterial
    {
        get => targetMaterial;
        set
        {
            targetMaterial = value;
            ApplyTiling();
        }
    }

    public Vector2 NormalTiling
    {
        get => new Vector2(normalTilingX, normalTilingY);
        set
        {
            normalTilingX = Mathf.Max(0.01f, value.x);
            normalTilingY = Mathf.Max(0.01f, value.y);
            ApplyTiling();
        }
    }

    private void OnEnable()
    {
        ApplyTiling();
    }

    private void OnValidate()
    {
        normalTilingX = Mathf.Max(0.01f, normalTilingX);
        normalTilingY = Mathf.Max(0.01f, normalTilingY);
        ApplyTiling();
    }

    private void ApplyTiling()
    {
        if (targetMaterial == null || string.IsNullOrEmpty(normalTextureProperty))
        {
            return;
        }

        if (!targetMaterial.HasProperty(normalTextureProperty))
        {
            return;
        }

        targetMaterial.SetTextureScale(normalTextureProperty, NormalTiling);
    }
}
