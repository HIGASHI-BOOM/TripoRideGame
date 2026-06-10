using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class KartTrackBuilder
{
    private const string EntryScenePath = "Assets/Scenes/KartEntryGame.unity";
    private const string VehicleConfigScenePath = "Assets/Scenes/KartVehicleConfig.unity";
    private const string FormalGameScenePath = "Assets/Scenes/KartFormalGame.unity";

    [MenuItem("Tools/Kart/Build Mario Kart Style Map")]
    public static void BuildMap()
    {
        SyncKartFlowBuildSettings();
        Debug.LogWarning("Legacy scene rebuilding is disabled. KartFormalGame is the source of truth; use Tools/Kart/Validate Formal Game Scene to verify it.");
    }

    [MenuItem("Tools/Kart/Sync Kart Flow Build Settings")]
    public static void SyncKartFlowBuildSettings()
    {
        ApplyProjectAspectSettings();
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log($"Kart build settings synced: {EntryScenePath}, {VehicleConfigScenePath}, {FormalGameScenePath}.");
    }

    private static void ApplyProjectAspectSettings()
    {
        PlayerSettings.defaultScreenWidth = 1080;
        PlayerSettings.defaultScreenHeight = 1920;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
    }

    private static void UpdateBuildSettings()
    {
        string[] kartScenes =
        {
            EntryScenePath,
            VehicleConfigScenePath,
            FormalGameScenePath
        };

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        for (int i = 0; i < kartScenes.Length; i++)
        {
            scenes.Add(new EditorBuildSettingsScene(kartScenes[i], true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
