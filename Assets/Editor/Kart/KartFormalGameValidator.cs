using System.Text;
using UnityEditor;
using UnityEngine;

public static class KartFormalGameValidator
{
    private const string EntryScenePath = "Assets/Scenes/KartEntryGame.unity";
    private const string VehicleConfigScenePath = "Assets/Scenes/KartVehicleConfig.unity";
    private const string FormalGameScenePath = "Assets/Scenes/KartFormalGame.unity";
    private const string KartPrefabPath = "Assets/Prefabs/Kart/PF_ArcadeKart.prefab";
    private const string DriveConfigPath = "Assets/ScriptableObjects/Kart/SC_ArcadeKart.asset";
    private const string RenderProfilePath = "Assets/Settings/KartRenderProfile.asset";
    private const string TrackDefinitionPath = "Assets/ScriptableObjects/Kart/KartFormalGameTrackDefinition.asset";
    private const int FormalPickupBoxCount = 15;
    private const int FormalCheckpointCount = 4;
    private const string ImportedTrackRootName = "ImportedBlenderTrack";
    private const string VegetationRootName = "Generated_KartVegetation1";
    private const string ItemBoxContainerName = "ItemBox";
    private const string LegacyRoadPrefix = "Kart_Road_";

    [MenuItem("Tools/Kart/Validate Formal Game Scene")]
    public static void ValidateFromMenu()
    {
        string report = ValidateActiveSceneForAutomation();
        if (report.StartsWith("PASS"))
        {
            Debug.Log(report);
        }
        else
        {
            Debug.LogError(report);
        }
    }

    public static string ValidateActiveSceneForAutomation()
    {
        ValidationReport report = new ValidationReport();
        ValidateScenePath(report);
        ValidateBuildSettings(report);
        ValidateAssets(report);
        ValidateSceneObjects(report);
        return report.ToString();
    }

    private static void ValidateScenePath(ValidationReport report)
    {
        string activeScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        report.Append("scene", activeScenePath);
        report.Expect(activeScenePath == FormalGameScenePath, $"Active scene must be {FormalGameScenePath}.");
    }

    private static void ValidateBuildSettings(ValidationReport report)
    {
        string[] expectedScenes =
        {
            EntryScenePath,
            VehicleConfigScenePath,
            FormalGameScenePath
        };

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        report.Append("buildSceneCount", scenes.Length.ToString());
        report.Expect(scenes.Length == expectedScenes.Length, "Build Settings should contain only the kart flow scenes.");

        for (int i = 0; i < expectedScenes.Length; i++)
        {
            if (i >= scenes.Length)
            {
                break;
            }

            report.Append($"buildScene[{i}]", $"{scenes[i].enabled}:{scenes[i].path}");
            report.Expect(scenes[i].enabled && scenes[i].path == expectedScenes[i], $"Build scene {i} should be {expectedScenes[i]}.");
        }
    }

    private static void ValidateAssets(ValidationReport report)
    {
        report.Expect(AssetDatabase.LoadAssetAtPath<GameObject>(KartPrefabPath) != null, $"Missing kart prefab: {KartPrefabPath}");
        report.Expect(AssetDatabase.LoadAssetAtPath<KartDriveConfig>(DriveConfigPath) != null, $"Missing drive config: {DriveConfigPath}");
        report.Expect(AssetDatabase.LoadAssetAtPath<Object>(RenderProfilePath) != null, $"Missing render profile: {RenderProfilePath}");
        TrackDefinition trackDefinition = AssetDatabase.LoadAssetAtPath<TrackDefinition>(TrackDefinitionPath);
        report.Expect(trackDefinition != null, $"Missing track definition: {TrackDefinitionPath}");
        if (trackDefinition != null)
        {
            report.Append("trackDefinition", trackDefinition.name);
            report.Append("trackDefinitionCheckpoints", trackDefinition.CheckpointCount.ToString());
            report.Append("trackDefinitionItemBoxes", trackDefinition.ItemBoxCount.ToString());
            report.Append("trackDefinitionCenterline", trackDefinition.Centerline.Count.ToString());
            report.Append("trackDefinitionStartGrid", trackDefinition.StartGrid.Count.ToString());
            report.Append("trackDefinitionAiWaypoints", trackDefinition.AiWaypoints.Count.ToString());
            report.Append("trackDefinitionCameraZones", trackDefinition.CameraZones.Count.ToString());
            report.Expect(trackDefinition.CheckpointCount == FormalCheckpointCount, $"Track definition should have {FormalCheckpointCount} checkpoints.");
            report.Expect(trackDefinition.ItemBoxCount == FormalPickupBoxCount, $"Track definition should have {FormalPickupBoxCount} item boxes.");
            report.Expect(trackDefinition.Centerline.Count > 0, "Track definition must include centerline points.");
            report.Expect(trackDefinition.StartGrid.Count > 0, "Track definition must include start grid points.");
            report.Expect(trackDefinition.AiWaypoints.Count > 0, "Track definition must include AI waypoints.");
            report.Expect(trackDefinition.CameraZones.Count > 0, "Track definition must include camera zones.");
        }
    }

    private static void ValidateSceneObjects(ValidationReport report)
    {
        int missingScripts = CountMissingScripts();
        report.Append("missingScripts", missingScripts.ToString());
        report.Expect(missingScripts == 0, "Scene has missing MonoBehaviour scripts.");

        ExpectCount<KartController>(report, "kartControllers", 1);
        ExpectCount<KartPlayerInputSource>(report, "playerInputSources", 1);
        ExpectCount<KartRaceManager>(report, "raceManagers", 1);
        ExpectCount<KartFollowCamera>(report, "followCameras", 1);
        ExpectCount<KartFixedAspectCamera>(report, "fixedAspectCameras", 1);
        ExpectCount<VehicleTireVisualController>(report, "tireVisualControllers", 4);
        ExpectCount<VehicleSteeringWheelVisualController>(report, "steeringWheelControllers", 1);
        ExpectCount<SpeedDashboardGaugeUi>(report, "speedDashboardGauges", 1);
        ExpectCount<KartScreenSpeedLines>(report, "screenSpeedLines", 1);
        ExpectDashboardCanvasCount(report);

        ExpectCount<KartCheckpoint>(report, "checkpoints", FormalCheckpointCount);
        ExpectCount<KartPickupBox>(report, "pickupBoxes", FormalPickupBoxCount);
        ExpectActiveObject(report, "importedTrack", ImportedTrackRootName);
        ExpectActiveObject(report, "vegetationRoot", VegetationRootName);
        ExpectActiveObject(report, "itemBoxContainer", ItemBoxContainerName);
        ExpectNoNamedPrefix(report, "legacyRoadObjects", LegacyRoadPrefix);
        ExpectRaceManagerConfiguration(report);

        Camera mainCamera = Camera.main;
        report.Append("mainCamera", mainCamera != null ? mainCamera.name : "null");
        report.Expect(mainCamera != null, "Scene must have a MainCamera.");
    }

    private static int CountMissingScripts()
    {
        int count = 0;
        GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        for (int i = 0; i < objects.Length; i++)
        {
            count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(objects[i]);
        }

        return count;
    }

    private static void ExpectCount<T>(ValidationReport report, string label, int expected) where T : Object
    {
        int count = Object.FindObjectsByType<T>(FindObjectsInactive.Include).Length;
        report.Append(label, count.ToString());
        report.Expect(count == expected, $"{label} should be {expected}, found {count}.");
    }

    private static void ExpectAtLeast<T>(ValidationReport report, string label, int minimum) where T : Object
    {
        int count = Object.FindObjectsByType<T>(FindObjectsInactive.Include).Length;
        report.Append(label, count.ToString());
        report.Expect(count >= minimum, $"{label} should be at least {minimum}, found {count}.");
    }

    private static void ExpectDashboardCanvasCount(ValidationReport report)
    {
        int count = 0;
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].name == "Kart_DashboardCanvas")
            {
                count++;
            }
        }

        report.Append("dashboardCanvases", count.ToString());
        report.Expect(count == 1, $"dashboardCanvases should be 1, found {count}.");
    }

    private static void ExpectRaceManagerConfiguration(ValidationReport report)
    {
        KartRaceManager raceManager = Object.FindAnyObjectByType<KartRaceManager>(FindObjectsInactive.Include);
        if (raceManager == null)
        {
            return;
        }

        report.Append("raceTrackDefinition", raceManager.TrackDefinition != null ? raceManager.TrackDefinition.name : "null");
        report.Expect(raceManager.TrackDefinition != null, "Race manager must reference a TrackDefinition.");
    }

    private static void ExpectActiveObject(ValidationReport report, string label, string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        report.Append(label, found != null ? found.name : "null");
        report.Expect(found != null, $"Missing active scene object: {objectName}.");
    }

    private static void ExpectNoNamedPrefix(ValidationReport report, string label, string prefix)
    {
        int count = 0;
        GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null && objects[i].name.StartsWith(prefix))
            {
                count++;
            }
        }

        report.Append(label, count.ToString());
        report.Expect(count == 0, $"Scene still has legacy objects with prefix {prefix}.");
    }

    private sealed class ValidationReport
    {
        private readonly StringBuilder details = new StringBuilder();
        private readonly StringBuilder failures = new StringBuilder();

        public void Append(string key, string value)
        {
            details.Append(key).Append('=').AppendLine(value);
        }

        public void Expect(bool condition, string message)
        {
            if (!condition)
            {
                failures.AppendLine(message);
            }
        }

        public override string ToString()
        {
            if (failures.Length == 0)
            {
                return "PASS KartFormalGame validation\n" + details;
            }

            return "FAIL KartFormalGame validation\n" + failures + details;
        }
    }
}
