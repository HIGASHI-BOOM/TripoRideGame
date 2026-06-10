using System;
using System.IO;
using UnityEngine;

public static class TripoApiLocalSettings
{
    public const string ApiKeyEnvironmentVariable = "TRIPO_API_KEY";
    public const string LocalSettingsPath = "ProjectSettings/TripoApi.local.json";

    public static string ResolveApiKey(string overrideApiKey)
    {
        if (!string.IsNullOrWhiteSpace(overrideApiKey))
        {
            return overrideApiKey.Trim();
        }

        string environmentKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(environmentKey))
        {
            return environmentKey.Trim();
        }

        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), LocalSettingsPath);
        if (!File.Exists(fullPath))
        {
            return string.Empty;
        }

        try
        {
            TripoLocalSettings settings = JsonUtility.FromJson<TripoLocalSettings>(File.ReadAllText(fullPath));
            return settings != null && !string.IsNullOrWhiteSpace(settings.apiKey) ? settings.apiKey.Trim() : string.Empty;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not read Tripo local settings at {LocalSettingsPath}: {exception.Message}");
            return string.Empty;
        }
    }

    [Serializable]
    private sealed class TripoLocalSettings
    {
        public string apiKey;
    }
}
