using System.Collections.Generic;
using UnityEngine;

public sealed class KartRaceManager : MonoBehaviour
{
    [SerializeField] private KartController playerKart;
    [SerializeField] private TrackDefinition trackDefinition;
    [SerializeField] private List<RaceParticipant> participants = new List<RaceParticipant>();
    [SerializeField] private int checkpointCount = 4;
    [SerializeField] private int targetLaps = 3;

    private readonly RaceSession raceSession = new RaceSession();

    public KartController PlayerKart => playerKart;
    public TrackDefinition TrackDefinition => trackDefinition;
    public RaceSession Session => raceSession;
    public IReadOnlyList<RaceProgress> Progress => raceSession.Progress;
    public RaceProgress PlayerProgress => playerKart != null ? raceSession.GetProgress(playerKart) : null;
    public int CurrentLap => PlayerProgress != null ? PlayerProgress.CurrentLap : 1;
    public int TargetLaps => raceSession.TargetLaps;
    public int NextCheckpoint => PlayerProgress != null ? PlayerProgress.NextCheckpoint : 0;
    public int CheckpointCount => raceSession.CheckpointCount;
    public bool Finished => PlayerProgress != null && PlayerProgress.Finished;

    private void Awake()
    {
        InitializeSession();
    }

    public void SetPlayerKart(KartController kart)
    {
        playerKart = kart;
        InitializeSession();
    }

    public void SetTrackDefinition(TrackDefinition definition)
    {
        trackDefinition = definition;
        InitializeSession();
    }

    public void SetParticipants(IList<RaceParticipant> raceParticipants)
    {
        participants.Clear();
        if (raceParticipants != null)
        {
            participants.AddRange(raceParticipants);
        }

        InitializeSession();
    }

    public void PassCheckpoint(KartController kart, int checkpointIndex)
    {
        raceSession.PassCheckpoint(kart, checkpointIndex, Time.timeSinceLevelLoad);
    }

    public RaceProgress GetProgress(KartController kart)
    {
        return raceSession.GetProgress(kart);
    }

    public List<RaceProgress> GetRankings()
    {
        return raceSession.GetRankings();
    }

    public void InitializeSession()
    {
        ResolveTrackRules();
        ResolveParticipants();
        raceSession.Configure(participants, targetLaps, checkpointCount);
    }

    private void ResolveTrackRules()
    {
        if (trackDefinition != null)
        {
            return;
        }

        KartCheckpoint[] checkpoints = FindObjectsByType<KartCheckpoint>(FindObjectsInactive.Exclude);
        if (checkpoints.Length > 0)
        {
            checkpointCount = checkpoints.Length;
        }
    }

    private void ResolveParticipants()
    {
        if (playerKart == null)
        {
            playerKart = FindAnyObjectByType<KartController>();
        }

        if (participants.Count == 0 && playerKart != null)
        {
            participants.Add(new RaceParticipant("Player", playerKart, true));
        }

        if (trackDefinition != null)
        {
            targetLaps = trackDefinition.TargetLaps;
            if (trackDefinition.CheckpointCount > 0)
            {
                checkpointCount = trackDefinition.CheckpointCount;
            }
        }
    }
}
