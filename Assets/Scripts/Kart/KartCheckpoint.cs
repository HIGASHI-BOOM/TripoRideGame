using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class KartCheckpoint : MonoBehaviour
{
    [SerializeField] private int checkpointIndex;
    [SerializeField] private bool startLine;
    [SerializeField] private KartRaceManager raceManager;

    public int CheckpointIndex => checkpointIndex;
    public bool StartLine => startLine;

    public void Configure(int index, bool isStartLine)
    {
        checkpointIndex = index;
        startLine = isStartLine;
    }

    public void BindRaceManager(KartRaceManager manager)
    {
        raceManager = manager;
    }

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        KartController kart = other.GetComponentInParent<KartController>();
        if (kart == null)
        {
            return;
        }

        if (raceManager == null)
        {
            raceManager = FindAnyObjectByType<KartRaceManager>();
        }

        if (raceManager != null)
        {
            raceManager.PassCheckpoint(kart, checkpointIndex);
        }
    }
}
