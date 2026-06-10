using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public sealed class CelebrationRibbonBrush : MonoBehaviour, IKartPickupEffect
{
    [Header("References")]
    [SerializeField] private ParticleSystem ribbonParticles;
    [SerializeField] private ParticleSystem[] additionalRibbonParticles;

    [Header("Brush")]
    [Min(0.01f)]
    [SerializeField] private float strokeSpacing = 0.18f;
    [Min(1)]
    [SerializeField] private int particlesPerStamp = 8;
    [Min(0f)]
    [SerializeField] private float brushRadius = 0.34f;
    [Min(0f)]
    [SerializeField] private float positionJitter = 0.08f;
    [SerializeField] private Vector2 startSizeRange = new Vector2(0.42f, 0.9f);
    [SerializeField] private Vector2 lifetimeRange = new Vector2(1.05f, 1.85f);
    [SerializeField] private Vector2 speedRange = new Vector2(0.45f, 1.35f);
    [SerializeField] private float outwardLift = 0.72f;
    [SerializeField] private float tangentPush = 0.45f;
    [SerializeField] private Vector3 defaultNormal = Vector3.up;
    [Min(0)]
    [SerializeField] private int meshVariantCount = 4;
    [SerializeField] private int[] additionalMeshVariantCounts;
    [SerializeField]
    private Color[] palette =
    {
        new Color(1f, 0.78f, 0.18f, 1f),
        new Color(0.15f, 0.78f, 1f, 1f),
        new Color(1f, 0.22f, 0.58f, 1f),
        new Color(0.32f, 1f, 0.45f, 1f),
        new Color(1f, 0.40f, 0.18f, 1f),
        new Color(0.70f, 0.36f, 1f, 1f),
    };

    [Header("Auto Stroke")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private bool emitWhileTargetMoves;
    [Min(0f)]
    [SerializeField] private float minimumTargetSpeed = 0.1f;

    private bool strokeActive;
    private bool haveLastTargetPosition;
    private Vector3 lastStrokePosition;
    private Vector3 lastTargetPosition;
    private float carriedDistance;

    public ParticleSystem RibbonParticles => ribbonParticles;

    private void Reset()
    {
        ribbonParticles = GetComponent<ParticleSystem>();
    }

    private void Awake()
    {
        if (ribbonParticles == null)
        {
            ribbonParticles = GetComponent<ParticleSystem>();
        }

        if (Application.isPlaying)
        {
            DisablePanelPreviewBurstsForRuntime();
        }
    }

    private void OnEnable()
    {
        haveLastTargetPosition = false;
        strokeActive = false;
        carriedDistance = 0f;
    }

    private void Update()
    {
        if (!emitWhileTargetMoves || followTarget == null)
        {
            return;
        }

        Vector3 currentPosition = followTarget.position;
        if (!haveLastTargetPosition)
        {
            lastTargetPosition = currentPosition;
            haveLastTargetPosition = true;
            return;
        }

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        float speed = Vector3.Distance(lastTargetPosition, currentPosition) / deltaTime;
        if (speed >= minimumTargetSpeed)
        {
            MoveStrokeTo(currentPosition, defaultNormal, 1f);
        }
        else if (strokeActive)
        {
            EndStroke();
        }

        lastTargetPosition = currentPosition;
    }

    public void PlayBurst(Vector3 position)
    {
        Vector3 normal = SafeNormal(defaultNormal, Vector3.up);
        for (int i = 0; i < 3; i++)
        {
            Vector3 offset = Random.insideUnitSphere * brushRadius * 0.55f;
            offset = Vector3.ProjectOnPlane(offset, normal);
            EmitStamp(position + offset, normal, 1f, Random.onUnitSphere);
        }
    }

    public void PlayPickupEffect(Vector3 position)
    {
        PlayBurst(position);
    }

    public void BrushAt(Vector3 position, Vector3 normal, float pressure = 1f)
    {
        EmitStamp(position, SafeNormal(normal, defaultNormal), pressure, Vector3.zero);
    }

    public void BeginStroke(Vector3 position, Vector3 normal, float pressure = 1f)
    {
        strokeActive = true;
        lastStrokePosition = position;
        carriedDistance = 0f;
        EmitStamp(position, SafeNormal(normal, defaultNormal), pressure, Vector3.zero);
    }

    public void MoveStrokeTo(Vector3 position, Vector3 normal, float pressure = 1f)
    {
        if (!strokeActive)
        {
            BeginStroke(position, normal, pressure);
            return;
        }

        Vector3 delta = position - lastStrokePosition;
        float distance = delta.magnitude;
        if (distance < 0.0001f)
        {
            return;
        }

        Vector3 direction = delta / distance;
        Vector3 safeNormal = SafeNormal(normal, defaultNormal);
        float spacing = Mathf.Max(0.01f, strokeSpacing);
        float nextStampDistance = spacing - carriedDistance;

        while (nextStampDistance <= distance)
        {
            Vector3 stampPosition = lastStrokePosition + direction * nextStampDistance;
            EmitStamp(stampPosition, safeNormal, pressure, direction);
            nextStampDistance += spacing;
        }

        carriedDistance = distance - (nextStampDistance - spacing);
        lastStrokePosition = position;
    }

    public void EndStroke()
    {
        strokeActive = false;
        carriedDistance = 0f;
    }

    [ContextMenu("Preview Pickup Burst")]
    public void PreviewPickupBurst()
    {
        ClearPreviewParticles();

        Vector3 origin = transform.position;
        Vector3 normal = SafeNormal(defaultNormal, Vector3.up);
        BeginStroke(origin + new Vector3(-1.45f, 0f, 0f), normal, 1f);
        for (int i = 1; i <= 18; i++)
        {
            float t = i / 18f;
            Vector3 point = origin + new Vector3(
                Mathf.Lerp(-1.45f, 1.45f, t),
                Mathf.Sin(t * Mathf.PI) * 0.55f,
                Mathf.Sin(t * Mathf.PI * 2f) * 0.34f);
            MoveStrokeTo(point, normal, 1f);
        }

        EndStroke();
        PlayBurst(origin + normal * 0.25f);
        SimulatePreviewParticles(0.28f);
    }

    [ContextMenu("Clear Preview Particles")]
    public void ClearPreviewParticles()
    {
        ClearParticleSystem(ribbonParticles);
        if (additionalRibbonParticles != null)
        {
            for (int i = 0; i < additionalRibbonParticles.Length; i++)
            {
                ClearParticleSystem(additionalRibbonParticles[i]);
            }
        }
    }

    private void EmitStamp(Vector3 position, Vector3 normal, float pressure, Vector3 strokeDirection)
    {
        if (ribbonParticles == null && !HasAdditionalParticleSystem())
        {
            return;
        }

        pressure = Mathf.Clamp01(pressure);
        normal = SafeNormal(normal, Vector3.up);
        Vector3 tangent = SafeTangent(strokeDirection, normal);
        Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;
        int count = Mathf.Max(1, Mathf.RoundToInt(particlesPerStamp * Mathf.Lerp(0.5f, 1.3f, pressure)));
        float radius = brushRadius * Mathf.Lerp(0.45f, 1.2f, pressure);

        for (int i = 0; i < count; i++)
        {
            Vector2 disk = Random.insideUnitCircle * radius;
            Vector3 offset = tangent * disk.x + bitangent * disk.y;
            offset += normal * Random.Range(-positionJitter, positionJitter);

            Vector3 velocity = normal * Random.Range(outwardLift * 0.65f, outwardLift * 1.35f);
            velocity += tangent * Random.Range(0.05f, tangentPush * Mathf.Lerp(0.55f, 1.35f, pressure));
            velocity += bitangent * Random.Range(-tangentPush, tangentPush) * 0.55f;
            velocity = velocity.normalized * RandomRange(speedRange);

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = position + offset,
                velocity = velocity,
                startLifetime = RandomRange(lifetimeRange),
                startSize = RandomRange(startSizeRange) * Mathf.Lerp(0.7f, 1.25f, pressure),
                startColor = PickColor(pressure),
                rotation3D = new Vector3(Random.Range(-45f, 45f), Random.Range(0f, 360f), Random.Range(0f, 360f)),
                angularVelocity3D = new Vector3(Random.Range(-80f, 80f), Random.Range(-160f, 160f), Random.Range(-240f, 240f)),
            };

            ParticleSystem targetParticles = PickParticleSystem(out int targetMeshVariantCount);
            if (targetParticles == null)
            {
                continue;
            }

            if (targetMeshVariantCount > 0)
            {
                emitParams.meshIndex = Random.Range(0, targetMeshVariantCount);
            }

            if (!targetParticles.isPlaying)
            {
                targetParticles.Play(true);
            }

            targetParticles.Emit(emitParams, 1);
        }
    }

    private void SimulatePreviewParticles(float time)
    {
        SimulatePreviewParticleSystem(ribbonParticles, time);
        if (additionalRibbonParticles != null)
        {
            for (int i = 0; i < additionalRibbonParticles.Length; i++)
            {
                SimulatePreviewParticleSystem(additionalRibbonParticles[i], time);
            }
        }

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private static void SimulatePreviewParticleSystem(ParticleSystem particles, float time)
    {
        if (particles == null)
        {
            return;
        }

        particles.Simulate(time, true, false, true);
        particles.Pause(true);
    }

    private static void ClearParticleSystem(ParticleSystem particles)
    {
        if (particles == null)
        {
            return;
        }

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.Clear(true);
    }

    private void DisablePanelPreviewBurstsForRuntime()
    {
        DisablePanelPreviewBursts(ribbonParticles);
        if (additionalRibbonParticles != null)
        {
            for (int i = 0; i < additionalRibbonParticles.Length; i++)
            {
                DisablePanelPreviewBursts(additionalRibbonParticles[i]);
            }
        }
    }

    private static void DisablePanelPreviewBursts(ParticleSystem particles)
    {
        if (particles == null)
        {
            return;
        }

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;
        emission.SetBursts(new ParticleSystem.Burst[0]);
    }

    private bool HasAdditionalParticleSystem()
    {
        if (additionalRibbonParticles == null)
        {
            return false;
        }

        for (int i = 0; i < additionalRibbonParticles.Length; i++)
        {
            if (additionalRibbonParticles[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private ParticleSystem PickParticleSystem(out int targetMeshVariantCount)
    {
        targetMeshVariantCount = Mathf.Max(0, meshVariantCount);
        int validCount = ribbonParticles != null ? 1 : 0;
        if (additionalRibbonParticles != null)
        {
            for (int i = 0; i < additionalRibbonParticles.Length; i++)
            {
                if (additionalRibbonParticles[i] != null)
                {
                    validCount++;
                }
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        int choice = Random.Range(0, validCount);
        if (ribbonParticles != null)
        {
            if (choice == 0)
            {
                return ribbonParticles;
            }

            choice--;
        }

        if (additionalRibbonParticles != null)
        {
            for (int i = 0; i < additionalRibbonParticles.Length; i++)
            {
                ParticleSystem candidate = additionalRibbonParticles[i];
                if (candidate == null)
                {
                    continue;
                }

                if (choice == 0)
                {
                    targetMeshVariantCount = GetAdditionalMeshVariantCount(i);
                    return candidate;
                }

                choice--;
            }
        }

        return ribbonParticles;
    }

    private int GetAdditionalMeshVariantCount(int index)
    {
        if (additionalMeshVariantCounts != null &&
            index >= 0 &&
            index < additionalMeshVariantCounts.Length &&
            additionalMeshVariantCounts[index] > 0)
        {
            return additionalMeshVariantCounts[index];
        }

        return Mathf.Max(0, meshVariantCount);
    }

    private Color PickColor(float pressure)
    {
        if (palette == null || palette.Length == 0)
        {
            return Color.white;
        }

        Color color = palette[Random.Range(0, palette.Length)];
        color.a *= Mathf.Lerp(0.72f, 1f, pressure);
        return color;
    }

    private static float RandomRange(Vector2 range)
    {
        float min = Mathf.Min(range.x, range.y);
        float max = Mathf.Max(range.x, range.y);
        return Random.Range(min, max);
    }

    private static Vector3 SafeNormal(Vector3 normal, Vector3 fallback)
    {
        if (normal.sqrMagnitude > 0.0001f)
        {
            return normal.normalized;
        }

        return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.up;
    }

    private static Vector3 SafeTangent(Vector3 preferredDirection, Vector3 normal)
    {
        Vector3 tangent = Vector3.ProjectOnPlane(preferredDirection, normal);
        if (tangent.sqrMagnitude > 0.0001f)
        {
            return tangent.normalized;
        }

        tangent = Vector3.Cross(normal, Vector3.forward);
        if (tangent.sqrMagnitude < 0.0001f)
        {
            tangent = Vector3.Cross(normal, Vector3.right);
        }

        return tangent.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.72f, 0.18f, 0.35f);
        Vector3 center = followTarget != null ? followTarget.position : transform.position;
        Gizmos.DrawWireSphere(center, brushRadius);
    }
}
