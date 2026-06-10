using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(KartController))]
[CanEditMultipleObjects]
public sealed class KartControllerEditor : Editor
{
    private SerializedProperty driveConfigProperty;

    private void OnEnable()
    {
        driveConfigProperty = serializedObject.FindProperty("driveConfig");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        DrawDriveConfigQuickTuning();
    }

    private void DrawDriveConfigQuickTuning()
    {
        serializedObject.Update();

        KartDriveConfig config = driveConfigProperty.objectReferenceValue as KartDriveConfig;
        if (config == null)
        {
            EditorGUILayout.HelpBox("Assign a Drive Config to expose kart tuning parameters here.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Drive Config Quick Tuning", EditorStyles.boldLabel);

            using SerializedObject configObject = new SerializedObject(config);
            SerializedProperty airborneGravityProperty = configObject.FindProperty("airborneGravityMultiplier");

            EditorGUILayout.PropertyField(
                airborneGravityProperty,
                new GUIContent(
                    "Airborne Gravity Multiplier",
                    "Only applies while the kart is airborne. Higher values shorten hang time without speed-based ground drag."));

            if (configObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(config);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
