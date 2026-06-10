using UnityEngine;

public static class KartGameState
{
    private const string SelectedTireKey = "Kart.SelectedTireSize";

    public static KartTireSize SelectedTireSize
    {
        get
        {
            int stored = PlayerPrefs.GetInt(SelectedTireKey, (int)KartTireSize.Medium);
            return System.Enum.IsDefined(typeof(KartTireSize), stored)
                ? (KartTireSize)stored
                : KartTireSize.Medium;
        }
        set
        {
            PlayerPrefs.SetInt(SelectedTireKey, (int)value);
            PlayerPrefs.Save();
        }
    }
}
