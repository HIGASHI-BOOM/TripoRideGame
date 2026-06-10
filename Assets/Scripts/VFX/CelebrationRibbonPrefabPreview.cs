using UnityEngine;

[DisallowMultipleComponent]
public sealed class CelebrationRibbonPrefabPreview : MonoBehaviour
{
    private void Awake()
    {
        if (Application.isPlaying)
        {
            Destroy(gameObject);
        }
    }
}
