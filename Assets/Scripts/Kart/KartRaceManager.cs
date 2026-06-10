using UnityEngine;

public sealed class KartRaceManager : MonoBehaviour
{
    [SerializeField] private KartController playerKart;
    [SerializeField] private int checkpointCount = 4;
    [SerializeField] private int targetLaps = 3;

    private int nextCheckpoint;
    private int currentLap = 1;
    private bool finished;

    public KartController PlayerKart => playerKart;
    public int CurrentLap => currentLap;
    public int TargetLaps => targetLaps;
    public int NextCheckpoint => nextCheckpoint;
    public int CheckpointCount => checkpointCount;
    public bool Finished => finished;

    private void Awake()
    {
        if (playerKart == null)
        {
            playerKart = FindAnyObjectByType<KartController>();
        }

        KartCheckpoint[] checkpoints = FindObjectsByType<KartCheckpoint>(FindObjectsInactive.Exclude);
        if (checkpoints.Length > 0)
        {
            checkpointCount = checkpoints.Length;
        }
    }

    public void SetPlayerKart(KartController kart)
    {
        playerKart = kart;
    }

    public void PassCheckpoint(KartController kart, int checkpointIndex)
    {
        if (finished || kart == null || kart != playerKart || checkpointIndex != nextCheckpoint)
        {
            return;
        }

        nextCheckpoint++;
        if (nextCheckpoint < checkpointCount)
        {
            return;
        }

        nextCheckpoint = 0;
        currentLap++;
        if (currentLap > targetLaps)
        {
            currentLap = targetLaps;
            finished = true;
        }
    }
}
