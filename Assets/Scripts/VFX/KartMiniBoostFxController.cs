using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class KartMiniBoostFxController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KartController kart;
    [SerializeField] private Rigidbody body;
    [SerializeField] private ParticleSystem[] effectSystems;
    [SerializeField] private MeshRenderer[] shellRenderers;

    [Header("Prefab Preview")]
    [SerializeField] private bool showPrefabPreview = true;
    [SerializeField] [Range(0f, 1f)] private float previewTime = 0.22f;
    [SerializeField] [Range(0f, 1f)] private float previewCharge = 0.85f;

    [Header("Runtime")]
    [SerializeField] private float burstDuration = 0.55f;
    [SerializeField] private Color lowChargeColor = new Color(0f, 0.95f, 1f, 1f);
    [SerializeField] private Color highChargeColor = new Color(1f, 0.82f, 0.12f, 1f);

    private int observedBurstId = -1;
    private float activeTimer;
#if UNITY_EDITOR
    private bool editorPreviewQueued;
#endif

    public bool BurstActive => activeTimer > 0f;

    private void Reset()
    {
        AutoAssignReferences();
        CollectParticleSystems();
        CollectShellRenderers();
    }

    private void Awake()
    {
        AutoAssignReferences();
        CollectParticleSystems();
        CollectShellRenderers();
        observedBurstId = kart != null ? kart.MiniBoostBurstId : -1;
        if (Application.isPlaying)
        {
            SetShellVisible(false);
        }
    }

    private void OnEnable()
    {
        AutoAssignReferences();
        CollectParticleSystems();
        CollectShellRenderers();
        observedBurstId = kart != null ? kart.MiniBoostBurstId : -1;
        if (!Application.isPlaying)
        {
            SetShellVisible(showPrefabPreview);
            QueueEditorPreview();
        }
        else
        {
            SetShellVisible(false);
        }
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        CollectParticleSystems();
        CollectShellRenderers();
        if (!Application.isPlaying)
        {
            SetShellVisible(showPrefabPreview);
            QueueEditorPreview();
        }
    }

    private void Update()
    {
        if (kart == null || body == null)
        {
            AutoAssignReferences();
        }

        if (!Application.isPlaying)
        {
            PreviewEffect();
            return;
        }

        DetectBurst();
        if (activeTimer > 0f)
        {
            activeTimer = Mathf.Max(0f, activeTimer - Time.deltaTime);
            if (activeTimer <= 0f)
            {
                SetShellVisible(false);
            }
        }
    }

    private void AutoAssignReferences()
    {
        if (kart == null)
        {
            kart = GetComponentInParent<KartController>();
        }

        if (body == null)
        {
            body = kart != null ? kart.GetComponent<Rigidbody>() : GetComponentInParent<Rigidbody>();
        }
    }

    private void CollectParticleSystems()
    {
        effectSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void CollectShellRenderers()
    {
        shellRenderers = GetComponentsInChildren<MeshRenderer>(true);
    }

    private void DetectBurst()
    {
        if (kart == null)
        {
            return;
        }

        int burstId = kart.MiniBoostBurstId;
        if (observedBurstId < 0)
        {
            observedBurstId = burstId;
            return;
        }

        if (burstId == observedBurstId)
        {
            return;
        }

        observedBurstId = burstId;
        if (!ShouldPlayMiniBoostFx())
        {
            StopEffects();
            return;
        }

        PlayBurst(kart.LastMiniBoostDriftTime);
    }

    private bool ShouldPlayMiniBoostFx()
    {
        KartDriveConfig config = kart != null ? kart.DriveConfig : null;
        return config == null || config.enableDriftMiniBoostFx;
    }

    public void PlayBurst(float driftTime)
    {
        if (Application.isPlaying && !ShouldPlayMiniBoostFx())
        {
            StopEffects();
            return;
        }

        float charge01 = Mathf.Clamp01(Mathf.InverseLerp(0.5f, 1.45f, driftTime));
        ApplyChargeColor(charge01);

        activeTimer = burstDuration;
        SetShellVisible(true);
        for (int i = 0; i < effectSystems.Length; i++)
        {
            ParticleSystem particles = effectSystems[i];
            if (particles == null)
            {
                continue;
            }

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Play(true);
        }
    }

    private void StopEffects()
    {
        activeTimer = 0f;
        SetShellVisible(false);
        if (effectSystems == null)
        {
            return;
        }

        for (int i = 0; i < effectSystems.Length; i++)
        {
            ParticleSystem particles = effectSystems[i];
            if (particles != null)
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private void PreviewEffect()
    {
        if (!showPrefabPreview || effectSystems == null)
        {
            return;
        }

        ApplyChargeColor(previewCharge);
        SetShellVisible(true);
        float time = Mathf.Max(0.01f, previewTime * burstDuration);
        for (int i = 0; i < effectSystems.Length; i++)
        {
            ParticleSystem particles = effectSystems[i];
            if (particles == null)
            {
                continue;
            }

            particles.Simulate(time, true, true, true);
        }
    }

    private void ApplyChargeColor(float charge01)
    {
        Color chargeColor = Color.Lerp(lowChargeColor, highChargeColor, charge01);
        for (int i = 0; i < effectSystems.Length; i++)
        {
            ParticleSystem particles = effectSystems[i];
            if (particles == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = particles.main;
            Color baseColor = main.startColor.color;
            if (baseColor.a <= 0.001f)
            {
                continue;
            }

            Color tinted = Color.Lerp(baseColor, chargeColor, 0.35f);
            tinted.a = baseColor.a;
            main.startColor = tinted;
        }
    }

    private void SetShellVisible(bool visible)
    {
        if (shellRenderers == null)
        {
            return;
        }

        for (int i = 0; i < shellRenderers.Length; i++)
        {
            MeshRenderer renderer = shellRenderers[i];
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }

    private void QueueEditorPreview()
    {
#if UNITY_EDITOR
        if (editorPreviewQueued)
        {
            return;
        }

        editorPreviewQueued = true;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            editorPreviewQueued = false;
            if (this == null || Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            CollectParticleSystems();
            PreviewEffect();
        };
#endif
    }
}
