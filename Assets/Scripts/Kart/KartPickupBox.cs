using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class KartPickupBox : MonoBehaviour
{
    [SerializeField] private float boostMultiplier = 1.25f;
    [SerializeField] private float boostDuration = 0.85f;
    [SerializeField] private float respawnDelay = 3.5f;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private MonoBehaviour pickupEffectPrefab;
    [SerializeField] private Vector3 pickupEffectOffset = new Vector3(0f, 0.95f, 0f);
    [Min(1)]
    [SerializeField] private int pickupEffectBursts = 3;
    [Min(0.1f)]
    [SerializeField] private float pickupEffectLifetime = 3f;

    private float respawnTimer;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
        visualRoot = transform;
    }

    private void Update()
    {
        if (visualRoot != null && respawnTimer <= 0f)
        {
            visualRoot.Rotate(0f, 95f * Time.deltaTime, 0f, Space.World);
            visualRoot.localPosition = Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.08f);
        }

        if (respawnTimer <= 0f)
        {
            return;
        }

        respawnTimer -= Time.deltaTime;
        if (respawnTimer <= 0f)
        {
            SetVisible(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (respawnTimer > 0f)
        {
            return;
        }

        KartController kart = other.GetComponentInParent<KartController>();
        if (kart == null)
        {
            return;
        }

        kart.ApplyBoost(boostMultiplier, boostDuration);
        PlayPickupEffect();
        respawnTimer = respawnDelay;
        SetVisible(false);
    }

    private void PlayPickupEffect()
    {
        if (pickupEffectPrefab == null)
        {
            return;
        }

        Vector3 effectPosition = transform.position + pickupEffectOffset;
        MonoBehaviour effect = Instantiate(pickupEffectPrefab, effectPosition, Quaternion.identity);
        for (int i = 0; i < pickupEffectBursts; i++)
        {
            PlayEffectBurst(effect, effectPosition);
        }

        Destroy(effect.gameObject, pickupEffectLifetime);
    }

    private static void PlayEffectBurst(MonoBehaviour effect, Vector3 effectPosition)
    {
        if (effect is IKartPickupEffect pickupEffect)
        {
            pickupEffect.PlayPickupEffect(effectPosition);
            return;
        }

        effect.SendMessage("PlayBurst", effectPosition, SendMessageOptions.DontRequireReceiver);
    }

    private void SetVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = visible;
        }
    }
}
