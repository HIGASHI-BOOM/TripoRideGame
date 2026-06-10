using System.Collections.Generic;

public sealed class RaceSession
{
    private readonly List<RaceProgress> progress = new List<RaceProgress>();
    private int targetLaps = 3;
    private int checkpointCount = 1;
    private int nextFinishPosition = 1;

    public int TargetLaps => targetLaps;
    public int CheckpointCount => checkpointCount;
    public IReadOnlyList<RaceProgress> Progress => progress;

    public void Configure(IList<RaceParticipant> participants, int targetLaps, int checkpointCount)
    {
        this.targetLaps = UnityEngine.Mathf.Max(1, targetLaps);
        this.checkpointCount = UnityEngine.Mathf.Max(1, checkpointCount);
        nextFinishPosition = 1;
        progress.Clear();

        if (participants == null)
        {
            return;
        }

        for (int i = 0; i < participants.Count; i++)
        {
            if (participants[i] != null && participants[i].Kart != null)
            {
                progress.Add(new RaceProgress(participants[i]));
            }
        }
    }

    public RaceProgress GetProgress(KartController kart)
    {
        if (kart == null)
        {
            return null;
        }

        for (int i = 0; i < progress.Count; i++)
        {
            if (progress[i].Kart == kart)
            {
                return progress[i];
            }
        }

        return null;
    }

    public bool PassCheckpoint(KartController kart, int checkpointIndex, float raceTime)
    {
        RaceProgress participantProgress = GetProgress(kart);
        if (participantProgress == null)
        {
            return false;
        }

        int finishPositionToAssign = participantProgress.NextCheckpoint == checkpointCount - 1
            && participantProgress.CurrentLap >= targetLaps
                ? nextFinishPosition
                : 0;

        bool accepted = participantProgress.TryPassCheckpoint(
            checkpointIndex,
            checkpointCount,
            targetLaps,
            raceTime,
            finishPositionToAssign);

        if (accepted && participantProgress.Finished && participantProgress.FinishPosition == nextFinishPosition)
        {
            nextFinishPosition++;
        }

        return accepted;
    }

    public List<RaceProgress> GetRankings()
    {
        List<RaceProgress> rankings = new List<RaceProgress>(progress);
        rankings.Sort(CompareProgress);
        return rankings;
    }

    private static int CompareProgress(RaceProgress a, RaceProgress b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        if (a.Finished && b.Finished)
        {
            return a.FinishPosition.CompareTo(b.FinishPosition);
        }

        if (a.Finished) return -1;
        if (b.Finished) return 1;

        int checkpointComparison = b.CompletedCheckpoints.CompareTo(a.CompletedCheckpoints);
        if (checkpointComparison != 0)
        {
            return checkpointComparison;
        }

        return string.CompareOrdinal(a.Participant.DisplayName, b.Participant.DisplayName);
    }
}
