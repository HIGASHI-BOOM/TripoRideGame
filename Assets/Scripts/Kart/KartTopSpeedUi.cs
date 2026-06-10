using UnityEngine;
using UnityEngine.UI;

public sealed class KartTopSpeedUi : MonoBehaviour
{
    [SerializeField] private KartController playerKart;
    [SerializeField] private Text speedText;

    public KartController PlayerKart
    {
        get => playerKart;
        set => playerKart = value;
    }

    private void Awake()
    {
        if (speedText == null)
        {
            speedText = GetComponent<Text>();
        }

        if (playerKart == null)
        {
            playerKart = FindAnyObjectByType<KartController>();
        }
    }

    private void Update()
    {
        if (speedText == null)
        {
            return;
        }

        float speedKph = playerKart != null ? playerKart.ForwardSpeedKph : 0f;
        speedText.text = $"{Mathf.RoundToInt(speedKph):000} KM/H";
    }
}
