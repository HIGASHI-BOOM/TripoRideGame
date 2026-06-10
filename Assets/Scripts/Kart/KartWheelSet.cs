using UnityEngine;

public sealed class KartWheelSet : MonoBehaviour
{
    [SerializeField] private KartController controller;
    [SerializeField] private Transform frontLeftSocket;
    [SerializeField] private Transform frontRightSocket;
    [SerializeField] private Transform rearLeftSocket;
    [SerializeField] private Transform rearRightSocket;
    [SerializeField] private GameObject smallTirePrefab;
    [SerializeField] private GameObject mediumTirePrefab;
    [SerializeField] private GameObject largeTirePrefab;
    [SerializeField] private bool showManagedTireVisuals = true;
    [SerializeField] private KartTireSize selectedSize = KartTireSize.Medium;

    public KartTireSize SelectedSize => selectedSize;

    public void Configure(
        KartController kartController,
        Transform frontLeft,
        Transform frontRight,
        Transform rearLeft,
        Transform rearRight,
        GameObject smallPrefab,
        GameObject mediumPrefab,
        GameObject largePrefab)
    {
        controller = kartController;
        frontLeftSocket = frontLeft;
        frontRightSocket = frontRight;
        rearLeftSocket = rearLeft;
        rearRightSocket = rearRight;
        smallTirePrefab = smallPrefab;
        mediumTirePrefab = mediumPrefab;
        largeTirePrefab = largePrefab;
    }

    public void ApplyWheelSize(KartTireSize size)
    {
        selectedSize = size;
        GameObject prefab = GetPrefab(size);
        if (prefab == null)
        {
            return;
        }

        Transform frontLeft = InstallWheel(frontLeftSocket, prefab, "Tire_FL");
        Transform frontRight = InstallWheel(frontRightSocket, prefab, "Tire_FR");
        Transform rearLeft = InstallWheel(rearLeftSocket, prefab, "Tire_RL");
        Transform rearRight = InstallWheel(rearRightSocket, prefab, "Tire_RR");

        SetManagedTireVisibility(frontLeft);
        SetManagedTireVisibility(frontRight);
        SetManagedTireVisibility(rearLeft);
        SetManagedTireVisibility(rearRight);

        if (controller != null)
        {
            float radius = GetRadius(prefab);
            controller.SetWheelVisuals(
                new[] { frontLeft, frontRight },
                new[] { rearLeft, rearRight },
                radius);
        }
    }

    private GameObject GetPrefab(KartTireSize size)
    {
        return size switch
        {
            KartTireSize.Small => smallTirePrefab,
            KartTireSize.Large => largeTirePrefab,
            _ => mediumTirePrefab
        };
    }

    private static float GetRadius(GameObject prefab)
    {
        KartTire tire = prefab != null ? prefab.GetComponent<KartTire>() : null;
        return tire != null ? tire.Radius : 0.34f;
    }

    private static Transform InstallWheel(Transform socket, GameObject prefab, string wheelName)
    {
        if (socket == null || prefab == null)
        {
            return null;
        }

        for (int i = socket.childCount - 1; i >= 0; i--)
        {
            GameObject child = socket.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                child.SetActive(false);
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }

        GameObject wheel = Instantiate(prefab, socket);
        wheel.name = wheelName;
        wheel.transform.localPosition = Vector3.zero;
        wheel.transform.localRotation = Quaternion.identity;
        wheel.transform.localScale = Vector3.one;
        return wheel.transform;
    }

    private void SetManagedTireVisibility(Transform wheel)
    {
        if (wheel == null)
        {
            return;
        }

        Renderer[] renderers = wheel.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = showManagedTireVisuals;
        }
    }
}
