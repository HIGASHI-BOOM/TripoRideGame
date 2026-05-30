using UnityEngine;

public sealed class WheelSocket : MonoBehaviour
{
    [SerializeField] private WheelSocketId socketId;
    [SerializeField] private bool steeringSocket;
    [SerializeField] private WheelAssembly installedWheel;
    [SerializeField] private float visualSteerLimit = 28f;

    public WheelSocketId SocketId => socketId;
    public bool SteeringSocket => steeringSocket;
    public bool HasWheel => installedWheel != null;
    public WheelAssembly InstalledWheel => installedWheel;

    public void Configure(WheelSocketId id, bool canSteer)
    {
        socketId = id;
        steeringSocket = canSteer;
    }

    public WheelAssembly InstallWheel(WheelAssembly wheelPrefab)
    {
        if (wheelPrefab == null)
        {
            return null;
        }

        RemoveWheel();
        installedWheel = Instantiate(wheelPrefab, transform);
        installedWheel.name = $"{wheelPrefab.name}_{socketId}";
        installedWheel.transform.localPosition = Vector3.zero;
        installedWheel.transform.localRotation = Quaternion.identity;
        installedWheel.transform.localScale = Vector3.one;
        return installedWheel;
    }

    public void RemoveWheel()
    {
        if (installedWheel == null)
        {
            return;
        }

        Destroy(installedWheel.gameObject);
        installedWheel = null;
    }

    public void UpdateWheelVisual(float forwardSpeed, float steerInput, float deltaTime)
    {
        if (installedWheel == null)
        {
            return;
        }

        float steerAngle = steeringSocket ? Mathf.Clamp(steerInput, -1f, 1f) * visualSteerLimit : 0f;
        installedWheel.ApplySteer(steerAngle);
        installedWheel.RollByDistance(forwardSpeed * deltaTime);
    }
}
