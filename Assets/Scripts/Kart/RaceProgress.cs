using System;

[Serializable]
public sealed class RaceProgress
{
    private readonly RaceParticipant participant;
    private int currentLap = 1;
    private int nextCheckpoint;
    private int completedCheckpoints;
    private bool finished;
    private float finishTime;
    private int finishPosition;

    public RaceProgress(RaceParticipant participant)
    {
        this.participant = participant;
    }

    public RaceParticipant Participant => participant;
    public KartController Kart => participant != null ? participant.Kart : null;
    public int CurrentLap => currentLap;
    public int NextCheckpoint => nextCheckpoint;
    public int CompletedCheckpoints => completedCheckpoints;
    public bool Finished => finished;
    public float FinishTime => finishTime;
    public int FinishPosition => finishPosition;

    public void Reset()
    {
        currentLap = 1;
        nextCheckpoint = 0;
        completedCheckpoints = 0;
        finished = false;
        finishTime = 0f;
        finishPosition = 0;
    }

    public bool TryPassCheckpoint(int checkpointIndex, int checkpointCount, int targetLaps, float raceTime, int finishPositionToAssign)
    {
        if (finished || checkpointIndex != nextCheckpoint || checkpointCount <= 0 || targetLaps <= 0)
        {
            return false;
        }

        completedCheckpoints++;
        nextCheckpoint++;
        if (nextCheckpoint < checkpointCount)
        {
            return true;
        }

        nextCheckpoint = 0;
        if (currentLap >= targetLaps)
        {
            finished = true;
            finishTime = raceTime;
            finishPosition = finishPositionToAssign;
            return true;
        }

        currentLap++;
        return true;
    }
}
