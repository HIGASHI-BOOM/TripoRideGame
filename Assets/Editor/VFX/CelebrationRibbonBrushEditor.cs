using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CelebrationRibbonBrush))]
[CanEditMultipleObjects]
public sealed class CelebrationRibbonBrushEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Prefab Preview", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview Pickup Burst", GUILayout.Height(28f)))
            {
                ForEachTargetBrush(brush => brush.PreviewPickupBurst());
            }

            if (GUILayout.Button("Clear Preview", GUILayout.Height(28f)))
            {
                ForEachTargetBrush(brush => brush.ClearPreviewParticles());
            }
        }
    }

    [MenuItem("Tools/Kart/Preview Selected Ribbon Brush")]
    private static void PreviewSelectedRibbonBrush()
    {
        ForEachSelectedBrush(brush => brush.PreviewPickupBurst());
    }

    [MenuItem("Tools/Kart/Preview Selected Ribbon Brush", true)]
    private static bool CanPreviewSelectedRibbonBrush()
    {
        return FindSelectedBrush() != null;
    }

    [MenuItem("Tools/Kart/Clear Selected Ribbon Brush Preview")]
    private static void ClearSelectedRibbonBrushPreview()
    {
        ForEachSelectedBrush(brush => brush.ClearPreviewParticles());
    }

    [MenuItem("Tools/Kart/Clear Selected Ribbon Brush Preview", true)]
    private static bool CanClearSelectedRibbonBrushPreview()
    {
        return FindSelectedBrush() != null;
    }

    private void ForEachTargetBrush(System.Action<CelebrationRibbonBrush> action)
    {
        foreach (Object targetObject in targets)
        {
            CelebrationRibbonBrush brush = targetObject as CelebrationRibbonBrush;
            if (brush == null)
            {
                continue;
            }

            action(brush);
            EditorUtility.SetDirty(brush);
        }

        SceneView.RepaintAll();
    }

    private static void ForEachSelectedBrush(System.Action<CelebrationRibbonBrush> action)
    {
        foreach (GameObject selectedObject in Selection.gameObjects)
        {
            CelebrationRibbonBrush brush = FindBrush(selectedObject);
            if (brush == null)
            {
                continue;
            }

            action(brush);
            EditorUtility.SetDirty(brush);
        }

        SceneView.RepaintAll();
    }

    private static CelebrationRibbonBrush FindSelectedBrush()
    {
        foreach (GameObject selectedObject in Selection.gameObjects)
        {
            CelebrationRibbonBrush brush = FindBrush(selectedObject);
            if (brush != null)
            {
                return brush;
            }
        }

        return null;
    }

    private static CelebrationRibbonBrush FindBrush(GameObject selectedObject)
    {
        if (selectedObject == null)
        {
            return null;
        }

        CelebrationRibbonBrush brush = selectedObject.GetComponent<CelebrationRibbonBrush>();
        if (brush != null)
        {
            return brush;
        }

        brush = selectedObject.GetComponentInParent<CelebrationRibbonBrush>();
        if (brush != null)
        {
            return brush;
        }

        return selectedObject.GetComponentInChildren<CelebrationRibbonBrush>(true);
    }
}
