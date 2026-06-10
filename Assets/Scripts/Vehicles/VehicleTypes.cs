using System;
using UnityEngine;

public enum WheelSocketId
{
    FL,
    FR,
    RL,
    RR
}

public enum WheelSize
{
    Small,
    Medium
}

public enum GameInputMode
{
    Player,
    Build,
    Driving,
    Menu
}

public enum BuildRotationAxis
{
    X,
    Y,
    Z
}

[Serializable]
public sealed class VehicleBuildData
{
    public int bodyIndex;
    public Vector3 bodyTargetSize = new Vector3(2.4f, 0.8f, 4.2f);

    [SerializeField] private WheelSize[] installedWheels =
    {
        WheelSize.Medium,
        WheelSize.Medium,
        WheelSize.Medium,
        WheelSize.Medium
    };

    [SerializeField] private bool[] wheelInstalled = new bool[4];

    public bool HasWheel(WheelSocketId socketId)
    {
        int index = ToIndex(socketId);
        return index >= 0 && index < wheelInstalled.Length && wheelInstalled[index];
    }

    public WheelSize GetWheelSize(WheelSocketId socketId)
    {
        int index = ToIndex(socketId);
        return index >= 0 && index < installedWheels.Length ? installedWheels[index] : WheelSize.Medium;
    }

    public void SetWheel(WheelSocketId socketId, WheelSize size)
    {
        int index = ToIndex(socketId);
        if (index < 0 || index >= wheelInstalled.Length)
        {
            return;
        }

        installedWheels[index] = size;
        wheelInstalled[index] = true;
    }

    public void ClearWheels()
    {
        for (int i = 0; i < wheelInstalled.Length; i++)
        {
            wheelInstalled[i] = false;
            installedWheels[i] = WheelSize.Medium;
        }
    }

    public static int ToIndex(WheelSocketId socketId)
    {
        return socketId switch
        {
            WheelSocketId.FL => 0,
            WheelSocketId.FR => 1,
            WheelSocketId.RL => 2,
            WheelSocketId.RR => 3,
            _ => -1
        };
    }
}

public sealed class GeneratedVehicleBody
{
    public GeneratedVehicleBody(GameObject root)
    {
        Root = root;
    }

    public GameObject Root { get; }
}

public static class GameInputContext
{
    public static GameInputMode CurrentMode { get; private set; } = GameInputMode.Player;

    public static bool PlayerEnabled => CurrentMode == GameInputMode.Player;
    public static bool BuildEnabled => CurrentMode == GameInputMode.Build || CurrentMode == GameInputMode.Menu;
    public static bool DrivingEnabled => CurrentMode == GameInputMode.Driving;

    public static void SetMode(GameInputMode mode)
    {
        CurrentMode = mode;
    }
}
