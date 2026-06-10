using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum KartAspectRatioMode
{
    Portrait9x16,
    Landscape16x9
}

public static class KartAspectRatioSettings
{
    private const string PlayerPrefsKey = "Kart.AspectRatioMode";
    private const int PortraitWidth = 1080;
    private const int PortraitHeight = 1920;
    private const int LandscapeWidth = 1920;
    private const int LandscapeHeight = 1080;

    public static event Action ModeChanged;

    public static KartAspectRatioMode Mode
    {
        get => (KartAspectRatioMode)Mathf.Clamp(PlayerPrefs.GetInt(PlayerPrefsKey, (int)KartAspectRatioMode.Portrait9x16), 0, 1);
        set
        {
            if (Mode == value)
            {
                return;
            }

            PlayerPrefs.SetInt(PlayerPrefsKey, (int)value);
            PlayerPrefs.Save();
            ModeChanged?.Invoke();
        }
    }

    public static float TargetAspect => Mode == KartAspectRatioMode.Landscape16x9 ? 16f / 9f : 9f / 16f;

    public static string Label => Mode == KartAspectRatioMode.Landscape16x9 ? "16:9" : "9:16";

    public static void Toggle()
    {
        Mode = Mode == KartAspectRatioMode.Landscape16x9
            ? KartAspectRatioMode.Portrait9x16
            : KartAspectRatioMode.Landscape16x9;
    }

    public static void ApplyWindowResolution()
    {
#if !UNITY_EDITOR
        int width = Mode == KartAspectRatioMode.Landscape16x9 ? LandscapeWidth : PortraitWidth;
        int height = Mode == KartAspectRatioMode.Landscape16x9 ? LandscapeHeight : PortraitHeight;
        Screen.SetResolution(width, height, FullScreenMode.Windowed);
#endif
    }
}

[DefaultExecutionOrder(-200)]
public sealed class KartAspectRatioSwitcher : MonoBehaviour
{
    private const float LandscapeUiScale = 0.6f;
    private const float ButtonWidth = 76f;
    private const float ButtonHeight = 34f;

    private GUIStyle buttonStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindAnyObjectByType<KartAspectRatioSwitcher>() != null)
        {
            return;
        }

        GameObject instance = new GameObject("Kart_AspectRatioSwitcher");
        DontDestroyOnLoad(instance);
        instance.AddComponent<KartAspectRatioSwitcher>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        KartAspectRatioSettings.ModeChanged += ApplyCurrentAspectToUiFrames;
        KartAspectRatioSettings.ModeChanged += KartAspectRatioSettings.ApplyWindowResolution;
        KartAspectRatioSettings.ApplyWindowResolution();
        ApplyCurrentAspectToUiFrames();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        KartAspectRatioSettings.ModeChanged -= ApplyCurrentAspectToUiFrames;
        KartAspectRatioSettings.ModeChanged -= KartAspectRatioSettings.ApplyWindowResolution;
    }

    private void OnGUI()
    {
        if (!HasKartSceneContext())
        {
            return;
        }

        if (Event.current != null && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F9)
        {
            ToggleAspect();
            Event.current.Use();
        }

        if (KartAspectRatioSettings.Mode != KartAspectRatioMode.Portrait9x16)
        {
            return;
        }

        EnsureStyle();

        Rect rect = new Rect(Screen.width - ButtonWidth - 14f, 14f, ButtonWidth, ButtonHeight);
        if (GUI.Button(rect, "16:9", buttonStyle))
        {
            ToggleAspect();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCurrentAspectToUiFrames();
    }

    private static void ApplyCurrentAspectToUiFrames()
    {
        AspectRatioFitter[] fitters = FindObjectsByType<AspectRatioFitter>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        float aspect = KartAspectRatioSettings.TargetAspect;
        float scale = KartAspectRatioSettings.Mode == KartAspectRatioMode.Landscape16x9
            ? LandscapeUiScale
            : 1f;

        for (int i = 0; i < fitters.Length; i++)
        {
            AspectRatioFitter fitter = fitters[i];
            if (fitter != null && fitter.name == "AspectFrame")
            {
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = aspect;
                fitter.transform.localScale = Vector3.one * scale;
            }
        }
    }

    private static void ToggleAspect()
    {
        KartAspectRatioSettings.Toggle();
        ApplyCurrentAspectToUiFrames();
    }

    private static bool HasKartSceneContext()
    {
        return FindAnyObjectByType<KartFixedAspectCamera>() != null
            || FindAnyObjectByType<KartMenuFlow>() != null
            || FindAnyObjectByType<KartController>() != null;
    }

    private void EnsureStyle()
    {
        if (buttonStyle != null)
        {
            return;
        }

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
    }
}
