using UnityEngine;
using UnityEngine.InputSystem;

public sealed class VehicleDemoUI : MonoBehaviour
{
    private const string DefaultVehiclePrompt = "A compact stylized off-road buggy body, game-ready, chunky silhouette, no wheels";
    private const string DefaultAnimalMountPrompt = "A full-body stylized quadruped fantasy animal mount for a rider, side-view friendly, sturdy legs, broad back, saddle-ready silhouette, game-ready, PBR textured";
    private static readonly Rect OpenMenuRect = new Rect(16f, 16f, 330f, 820f);
    private static readonly Rect ClosedMenuRect = new Rect(16f, 16f, 230f, 38f);

    [SerializeField] private VehicleAssemblyManager manager;
    [SerializeField] private GeneratedBodyProvider localProvider;
    [SerializeField] private TripoVehicleBodyProvider tripoProvider;
    [SerializeField] private bool menuVisible = true;
    [SerializeField] private TripoGenerationMode tripoMode;
    [SerializeField] private string tripoPrompt = DefaultVehiclePrompt;

    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;
    private GUIStyle tipStyle;
    private GUIStyle textAreaStyle;

    public bool MenuVisible => menuVisible;
    public Rect ActiveMenuRect => menuVisible ? OpenMenuRect : ClosedMenuRect;

    private void Awake()
    {
        if (manager == null)
        {
            manager = FindAnyObjectByType<VehicleAssemblyManager>();
        }

        if (tripoProvider == null)
        {
            tripoProvider = FindAnyObjectByType<TripoVehicleBodyProvider>();
        }

        if (localProvider == null)
        {
            localProvider = FindAnyObjectByType<LocalPrefabBodyProvider>();
        }

        if (tripoProvider != null)
        {
            tripoPrompt = tripoProvider.Prompt;
            tripoMode = tripoProvider.GenerationMode;
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

        GUILayout.BeginArea(OpenMenuRect, GUI.skin.box);
        GUILayout.Label("Tripo Ride Vehicle Builder", labelStyle);

        bool inGenerationMode = manager != null && manager.VehicleGenerationMode;
        if (GUILayout.Button("Enter Vehicle Build Mode", buttonStyle))
        {
            manager?.EnterVehicleGenerationMode();
        }

        GUI.enabled = inGenerationMode || manager != null;
        if (GUILayout.Button("Spawn Local Test Vehicle", buttonStyle))
        {
            manager?.SetBodyProvider(localProvider);
            manager?.GenerateVehicle();
            manager?.InstallAllWheels();
        }
        GUI.enabled = true;

        GUILayout.Space(8f);
        GUI.enabled = inGenerationMode;
        GUILayout.Label($"Mode: {GetModeLabel(tripoMode)}", labelStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Vehicle Body", buttonStyle))
        {
            SetTripoMode(TripoGenerationMode.VehicleBody);
        }

        if (GUILayout.Button("Animal Mount", buttonStyle))
        {
            SetTripoMode(TripoGenerationMode.AnimalMount);
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("Tripo Prompt", labelStyle);
        tripoPrompt = GUILayout.TextArea(tripoPrompt, textAreaStyle, GUILayout.Height(74f));

        GUI.enabled = inGenerationMode && (tripoProvider == null || !tripoProvider.Busy);
        if (GUILayout.Button(GetGenerateButtonLabel(), buttonStyle))
        {
            tripoProvider?.SetGenerationMode(tripoMode);
            tripoProvider?.SetPrompt(tripoPrompt);
            manager?.SetBodyProvider(tripoProvider);
            manager?.GenerateVehicle();
        }

        if (tripoProvider != null && GUILayout.Button("Load Latest Saved Body", buttonStyle))
        {
            tripoProvider.SetGenerationMode(tripoMode);
            tripoProvider.SetPrompt(tripoPrompt);
            tripoProvider.UseLatestLocalModelOnce();
            manager?.SetBodyProvider(tripoProvider);
            manager?.GenerateVehicle();
        }
        GUI.enabled = inGenerationMode;

        if (tripoProvider != null)
        {
            GUILayout.Label($"Tripo: {tripoProvider.LastStatus}", labelStyle);
        }

        GUILayout.Space(8f);
        GUILayout.Label("Rotate Body 90 deg", labelStyle);
        GUI.enabled = inGenerationMode && manager != null && manager.CurrentVehicle != null;
        DrawRotationButtons("X", BuildRotationAxis.X);
        DrawRotationButtons("Y", BuildRotationAxis.Y);
        DrawRotationButtons("Z", BuildRotationAxis.Z);
        GUI.enabled = inGenerationMode;

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

        DrawSimulationConfig();

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

    private void SetTripoMode(TripoGenerationMode value)
    {
        if (tripoMode == value)
        {
            return;
        }

        tripoMode = value;
        tripoPrompt = value == TripoGenerationMode.AnimalMount ? DefaultAnimalMountPrompt : DefaultVehiclePrompt;
        tripoProvider?.SetGenerationMode(value);
    }

    private static string GetModeLabel(TripoGenerationMode value)
    {
        return value == TripoGenerationMode.AnimalMount ? "Animal mount" : "Vehicle body";
    }

    private void DrawRotationButtons(string label, BuildRotationAxis axis)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle, GUILayout.Width(20f));
        if (GUILayout.Button("-90", buttonStyle))
        {
            manager?.RotateGeneratedBody(axis, -1);
        }

        if (GUILayout.Button("+90", buttonStyle))
        {
            manager?.RotateGeneratedBody(axis, 1);
        }
        GUILayout.EndHorizontal();
    }

    private string GetGenerateButtonLabel()
    {
        if (tripoMode == TripoGenerationMode.AnimalMount)
        {
            return tripoProvider != null ? "Generate Animated Mount" : "Generate Mount";
        }

        return tripoProvider != null ? "Generate Tripo Body" : "Generate Body";
    }

    private void DrawClosedMenuTip()
    {
        Rect rect = ClosedMenuRect;
        GUI.Box(rect, GUIContent.none);
        GUI.Label(rect, "Press Tab to open menu", tipStyle);
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
            ? $"Status: driving {manager.CurrentVehicle.SpeedKph:0} km/h"
            : manager.VehicleGenerationMode
                ? $"Status: generation mode ({installed}/4 wheels)"
            : $"Status: assembling ({installed}/4 wheels)";
    }

    private void DrawSimulationConfig()
    {
        VehicleSimulationConfig config = GetActiveSimulationConfig();
        if (config == null)
        {
            return;
        }

        GUILayout.Space(8f);
        GUILayout.Label("Simulation Config", labelStyle);
        DrawFloatSlider("Mass", ref config.mass, 400f, 1800f);
        DrawFloatSlider("Drive Force", ref config.driveForce, 1000f, 14000f);
        DrawFloatSlider("Max Speed", ref config.maxFlatSpeed, 6f, 60f);
        DrawFloatSlider("Reverse Max", ref config.maxReverseSpeed, 3f, 30f);
        DrawFloatSlider("Max Steer", ref config.maxSteerAngle, 3f, 35f);
        DrawFloatSlider("Front Grip", ref config.frontTireGrip, 0.5f, 25f);
        DrawFloatSlider("Rear Grip", ref config.rearTireGrip, 0.5f, 30f);
        DrawFloatSlider("Brake", ref config.brakeForce, 1000f, 18000f);
        DrawFloatSlider("Downforce", ref config.speedDownforce, 0f, 25f);

        manager?.CurrentVehicle?.RefreshSimulationSettings();
    }

    private VehicleSimulationConfig GetActiveSimulationConfig()
    {
        if (manager != null && manager.CurrentVehicle != null && manager.CurrentVehicle.SimulationConfig != null)
        {
            return manager.CurrentVehicle.SimulationConfig;
        }

        return manager != null ? manager.VehicleSimulationConfig : null;
    }

    private void DrawFloatSlider(string label, ref float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {value:0.##}", labelStyle, GUILayout.Width(132f));
        value = GUILayout.HorizontalSlider(value, min, max);
        GUILayout.EndHorizontal();
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

        textAreaStyle = new GUIStyle(GUI.skin.textArea)
        {
            wordWrap = true,
            fontSize = 12
        };
    }
}
