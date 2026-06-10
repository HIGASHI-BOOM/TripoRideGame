using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class KartBoostPad : MonoBehaviour
{
    [SerializeField] private float speedMultiplier = 1.45f;
    [SerializeField] private float duration = 1.15f;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        KartController kart = other.GetComponentInParent<KartController>();
        if (kart != null)
        {
            kart.ApplyBoost(speedMultiplier, duration);
        }
    }
}
