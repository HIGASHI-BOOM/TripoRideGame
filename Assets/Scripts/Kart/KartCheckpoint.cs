using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class KartCheckpoint : MonoBehaviour
{
    [SerializeField] private int checkpointIndex;
    [SerializeField] private bool startLine;

    public int CheckpointIndex => checkpointIndex;
    public bool StartLine => startLine;

    public void Configure(int index, bool isStartLine)
    {
        checkpointIndex = index;
        startLine = isStartLine;
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

        KartRaceManager manager = FindAnyObjectByType<KartRaceManager>();
        if (manager != null)
        {
            manager.PassCheckpoint(kart, checkpointIndex);
        }
    }
}
