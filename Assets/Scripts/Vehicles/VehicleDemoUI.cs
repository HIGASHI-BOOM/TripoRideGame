using UnityEngine;
using UnityEngine.InputSystem;

public sealed class VehicleDemoUI : MonoBehaviour
{
    [SerializeField] private VehicleAssemblyManager manager;
    [SerializeField] private bool menuVisible = true;

    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;
    private GUIStyle tipStyle;

    public bool MenuVisible => menuVisible;

    private void Awake()
    {
        if (manager == null)
        {
            manager = FindAnyObjectByType<VehicleAssemblyManager>();
        }

        ApplyCursorState();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
        else
        {
            ApplyCursorState();
        }
    }

    private void OnGUI()
    {
        EnsureStyles();
        if (!menuVisible)
        {
            DrawClosedMenuTip();
            return;
        }

        GUILayout.BeginArea(new Rect(16f, 16f, 260f, 430f), GUI.skin.box);
        GUILayout.Label("Local Vehicle MVP", labelStyle);

        bool inGenerationMode = manager != null && manager.VehicleGenerationMode;
        if (GUILayout.Button("进入载具生成模式", buttonStyle))
        {
            manager?.EnterVehicleGenerationMode();
        }

        GUILayout.Space(8f);
        GUI.enabled = inGenerationMode;
        if (GUILayout.Button("Generate Local Body", buttonStyle))
        {
            manager?.GenerateVehicle();
        }

        GUILayout.Space(8f);
        GUILayout.Label($"Wheel: {(manager != null ? manager.SelectedWheelSize : WheelSize.Medium)}", labelStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Small", buttonStyle))
        {
            manager?.SelectSmallWheel();
        }

        if (GUILayout.Button("Medium", buttonStyle))
        {
            manager?.SelectMediumWheel();
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Install Next Wheel", buttonStyle))
        {
            manager?.InstallNextWheel();
        }

        if (GUILayout.Button("Install All", buttonStyle))
        {
            manager?.InstallAllWheels();
        }

        if (GUILayout.Button("Remove Wheels", buttonStyle))
        {
            manager?.RemoveAllWheels();
        }
        GUI.enabled = true;

        GUILayout.Space(8f);
        bool canDrive = manager != null && manager.CurrentVehicle != null && manager.CurrentVehicle.HasAllWheels;
        GUI.enabled = canDrive;
        if (GUILayout.Button("Start Driving", buttonStyle))
        {
            manager?.StartDriving();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Exit + Destroy Runtime Vehicle", buttonStyle))
        {
            manager?.ExitAndDestroyVehicle();
        }

        GUILayout.Space(8f);
        GUILayout.Label(GetStatus(), labelStyle);
        GUILayout.EndArea();
    }

    private void ApplyCursorState()
    {
        bool buildMode = manager != null && manager.VehicleGenerationMode && !manager.Driving;
        bool shouldUnlock = menuVisible || buildMode;
        Cursor.lockState = shouldUnlock ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = shouldUnlock;
    }

    public void ToggleMenu()
    {
        SetMenuVisible(!menuVisible);
    }

    public void SetMenuVisible(bool visible)
    {
        menuVisible = visible;
        ApplyCursorState();
    }

    private void DrawClosedMenuTip()
    {
        const float width = 230f;
        const float height = 38f;
        Rect rect = new Rect(16f, 16f, width, height);
        GUI.Box(rect, GUIContent.none);
        GUI.Label(rect, "按 Tab 呼出菜单", tipStyle);
    }

    private string GetStatus()
    {
        if (manager == null || manager.CurrentVehicle == null)
        {
            return manager != null && manager.VehicleGenerationMode
                ? "Status: generation mode"
                : "Status: no runtime vehicle";
        }

        int installed = 0;
        WheelSocket[] sockets = manager.CurrentVehicle.WheelSockets;
        for (int i = 0; sockets != null && i < sockets.Length; i++)
        {
            if (sockets[i] != null && sockets[i].HasWheel)
            {
                installed++;
            }
        }

        return manager.Driving
            ? "Status: driving"
            : manager.VehicleGenerationMode
                ? $"Status: generation mode ({installed}/4 wheels)"
            : $"Status: assembling ({installed}/4 wheels)";
    }

    private void EnsureStyles()
    {
        if (buttonStyle != null)
        {
            return;
        }

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fixedHeight = 30,
            fontSize = 13
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            wordWrap = true,
            fontSize = 13
        };

        tipStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
    }
}
