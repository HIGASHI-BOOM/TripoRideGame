using System;
using UnityEngine;

[Serializable]
public sealed class RaceParticipant
{
    [SerializeField] private string displayName = "Kart";
    [SerializeField] private KartController kart;
    [SerializeField] private bool playerControlled;

    public RaceParticipant(string displayName, KartController kart, bool playerControlled)
    {
        this.displayName = string.IsNullOrWhiteSpace(displayName) ? "Kart" : displayName;
        this.kart = kart;
        this.playerControlled = playerControlled;
    }

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? (kart != null ? kart.name : "Kart") : displayName;
    public KartController Kart => kart;
    public bool PlayerControlled => playerControlled;
}
