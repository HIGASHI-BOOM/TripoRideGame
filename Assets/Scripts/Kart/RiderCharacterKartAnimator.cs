using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class RiderCharacterKartAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KartController kart;
    [SerializeField] private Animator animator;

    [Header("Thresholds")]
    [Min(0f)]
    [SerializeField] private float fastSpeedMps = 6f;
    [Range(0f, 1f)]
    [SerializeField] private float steerDeadZone = 0.2f;
    [Min(0f)]
    [SerializeField] private float transitionDuration = 0.12f;

    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int IdleLeftHash = Animator.StringToHash("Idle_L");
    private static readonly int IdleRightHash = Animator.StringToHash("Idle_R");
    private static readonly int FastIdleHash = Animator.StringToHash("FastIdle");
    private static readonly int FastIdleLeftHash = Animator.StringToHash("FastIdle_L");
    private static readonly int FastIdleRightHash = Animator.StringToHash("FastIdle_R");

    private int activeStateHash;
    private Renderer[] renderers;
    private bool riderVisible = true;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        kart = GetComponentInParent<KartController>();
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        ResolveKart();
        CollectRenderers();
        ApplyVisibility(ShouldShowRider());
        PlayState(IdleHash, 0f);
    }

    private void LateUpdate()
    {
        if (kart == null)
        {
            ResolveKart();
        }

        ApplyVisibility(ShouldShowRider());
        if (!riderVisible)
        {
            return;
        }

        if (kart == null || animator == null)
        {
            return;
        }

        PlayState(SelectStateHash(), transitionDuration);
    }

    private void ResolveKart()
    {
        kart = GetComponentInParent<KartController>();
    }

    private void CollectRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    private bool ShouldShowRider()
    {
        KartDriveConfig config = kart != null ? kart.DriveConfig : null;
        return config == null || config.showRiderCharacter;
    }

    private void ApplyVisibility(bool visible)
    {
        if (renderers == null || renderers.Length == 0)
        {
            CollectRenderers();
        }

        if (riderVisible == visible)
        {
            return;
        }

        riderVisible = visible;
        if (animator != null)
        {
            animator.enabled = visible;
        }

        if (renderers == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }

    private int SelectStateHash()
    {
        bool fast = Mathf.Abs(kart.ForwardSpeedMps) >= fastSpeedMps || kart.Boosting;
        float steer = kart.VisualSteerInput;

        if (steer <= -steerDeadZone)
        {
            return fast ? FastIdleLeftHash : IdleLeftHash;
        }

        if (steer >= steerDeadZone)
        {
            return fast ? FastIdleRightHash : IdleRightHash;
        }

        return fast ? FastIdleHash : IdleHash;
    }

    private void PlayState(int stateHash, float duration)
    {
        if (activeStateHash == stateHash || animator == null)
        {
            return;
        }

        activeStateHash = stateHash;
        animator.CrossFade(stateHash, duration, 0);
    }
}
