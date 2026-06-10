using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(KartController))]
public sealed class KartDriftFxController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KartController kart;
    [SerializeField] private Rigidbody body;
    [SerializeField] private Transform rearLeftAnchor;
    [SerializeField] private Transform rearRightAnchor;
    [SerializeField] private Material trailMaterial;
    [SerializeField] private Material sparkMaterial;
    [SerializeField] private Material pulseMaterial;

    [Header("Grounding")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRayLength = 1.4f;
    [SerializeField] private float groundOffset = 0.035f;

    [Header("Drift Feel")]
    [SerializeField] private float minVisualSpeed = 7f;
    [SerializeField] private float lateralSpeedForFullIntensity = 7f;
    [SerializeField] private float intensitySharpness = 8f;
    [SerializeField] private float trailMinTime = 0.12f;
    [SerializeField] private float trailMaxTime = 0.55f;
    [SerializeField] private float trailMinWidth = 0.12f;
    [SerializeField] private float trailMaxWidth = 0.42f;
    [SerializeField] private float sparksPerSecond = 90f;
    [SerializeField] private float pulseDuration = 0.36f;
    [SerializeField] private float pulseRadius = 0.72f;

    private TrailRenderer leftTrail;
    private TrailRenderer rightTrail;
    private ParticleSystem leftSparks;
    private ParticleSystem rightSparks;
    private LineRenderer leftPulse;
    private LineRenderer rightPulse;
    private float intensity;
    private float leftSparkBudget;
    private float rightSparkBudget;
    private float pulseAge = 999f;
    private bool wasDrifting;
    private ContactPointState leftContact;
    private ContactPointState rightContact;

    public float VisualIntensity => intensity;

    private void Reset()
    {
        AutoAssignReferences();
    }

    private void Awake()
    {
        AutoAssignReferences();
        EnsureEffects();
    }

    private void OnEnable()
    {
        EnsureEffects();
    }

    private void Update()
    {
        if (kart == null || body == null)
        {
            AutoAssignReferences();
        }

        EnsureEffects();
        UpdateContactPoints();

        float targetIntensity = CalculateTargetIntensity();
        intensity = Mathf.MoveTowards(intensity, targetIntensity, intensitySharpness * Time.deltaTime);

        if (!wasDrifting && targetIntensity > 0.05f)
        {
            BeginPulse();
        }

        wasDrifting = targetIntensity > 0.05f;

        UpdateTrail(leftTrail, leftContact, intensity);
        UpdateTrail(rightTrail, rightContact, intensity);
        EmitSparks(leftSparks, leftContact, ref leftSparkBudget);
        EmitSparks(rightSparks, rightContact, ref rightSparkBudget);
        UpdatePulse(leftPulse, leftContact);
        UpdatePulse(rightPulse, rightContact);

        if (pulseAge <= pulseDuration)
        {
            pulseAge += Time.deltaTime;
        }
    }

    private void OnDisable()
    {
        ClearTrail(leftTrail);
        ClearTrail(rightTrail);
        SetPulseVisible(leftPulse, false);
        SetPulseVisible(rightPulse, false);
    }

    private void AutoAssignReferences()
    {
        if (kart == null)
        {
            kart = GetComponent<KartController>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        if (rearLeftAnchor == null)
        {
            rearLeftAnchor = transform.Find("WheelSocket_RL");
        }

        if (rearRightAnchor == null)
        {
            rearRightAnchor = transform.Find("WheelSocket_RR");
        }
    }

    private void EnsureEffects()
    {
        leftTrail = EnsureTrail("DriftTrail_L", leftTrail);
        rightTrail = EnsureTrail("DriftTrail_R", rightTrail);
        leftSparks = EnsureSparks("DriftSparks_L", leftSparks);
        rightSparks = EnsureSparks("DriftSparks_R", rightSparks);
        leftPulse = EnsurePulse("DriftPulse_L", leftPulse);
        rightPulse = EnsurePulse("DriftPulse_R", rightPulse);
    }

    private TrailRenderer EnsureTrail(string childName, TrailRenderer current)
    {
        if (current != null)
        {
            return current;
        }

        Transform child = transform.Find(childName);
        if (child == null)
        {
            child = new GameObject(childName).transform;
            child.SetParent(transform, false);
        }

        TrailRenderer trail = child.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = child.gameObject.AddComponent<TrailRenderer>();
        }

        ConfigureTrail(trail);
        return trail;
    }

    private void ConfigureTrail(TrailRenderer trail)
    {
        trail.sharedMaterial = trailMaterial;
        trail.emitting = false;
        trail.time = trailMinTime;
        trail.minVertexDistance = 0.045f;
        trail.alignment = LineAlignment.View;
        trail.textureMode = LineTextureMode.Stretch;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.numCapVertices = 6;
        trail.numCornerVertices = 4;
        trail.widthMultiplier = trailMinWidth;
        trail.colorGradient = CreateTrailGradient(0f);
    }

    private ParticleSystem EnsureSparks(string childName, ParticleSystem current)
    {
        if (current != null)
        {
            return current;
        }

        Transform child = transform.Find(childName);
        if (child == null)
        {
            child = new GameObject(childName).transform;
            child.SetParent(transform, false);
        }

        ParticleSystem particles = child.GetComponent<ParticleSystem>();
        if (particles == null)
        {
            particles = child.gameObject.AddComponent<ParticleSystem>();
        }

        ConfigureSparks(particles);
        return particles;
    }

    private void ConfigureSparks(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.12f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.maxParticles = 260;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = CreateSparkGradient();

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.35f),
            new Keyframe(0.14f, 1f),
            new Keyframe(1f, 0f)));

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.material = sparkMaterial;
        renderer.sortingFudge = 0.2f;
        renderer.minParticleSize = 0.02f;
        renderer.maxParticleSize = 0.4f;
    }

    private LineRenderer EnsurePulse(string childName, LineRenderer current)
    {
        if (current != null)
        {
            return current;
        }

        Transform child = transform.Find(childName);
        if (child == null)
        {
            child = new GameObject(childName).transform;
            child.SetParent(transform, false);
        }

        LineRenderer line = child.GetComponent<LineRenderer>();
        if (line == null)
        {
            line = child.gameObject.AddComponent<LineRenderer>();
        }

        ConfigurePulse(line);
        return line;
    }

    private void ConfigurePulse(LineRenderer line)
    {
        line.sharedMaterial = pulseMaterial;
        line.loop = true;
        line.useWorldSpace = true;
        line.positionCount = 36;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        SetPulseVisible(line, false);
    }

    private void UpdateContactPoints()
    {
        leftContact = ResolveContact(rearLeftAnchor);
        rightContact = ResolveContact(rearRightAnchor);
    }

    private ContactPointState ResolveContact(Transform anchor)
    {
        Vector3 origin = anchor != null ? anchor.position : transform.position;
        origin += transform.up * 0.35f;

        if (Physics.Raycast(origin, -transform.up, out RaycastHit hit, groundRayLength, groundMask, QueryTriggerInteraction.Ignore))
        {
            return new ContactPointState(hit.point + hit.normal * groundOffset, hit.normal, true);
        }

        return new ContactPointState(origin - transform.up * 0.45f, transform.up, false);
    }

    private float CalculateTargetIntensity()
    {
        if (kart == null || body == null || !kart.Grounded)
        {
            return 0f;
        }

        Vector3 flatVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
        float forwardSpeed = Mathf.Abs(Vector3.Dot(flatVelocity, transform.forward));
        if (forwardSpeed < minVisualSpeed || !kart.Drifting)
        {
            return 0f;
        }

        float lateralSpeed = Mathf.Abs(Vector3.Dot(flatVelocity, transform.right));
        float speed01 = Mathf.InverseLerp(minVisualSpeed, minVisualSpeed + 14f, forwardSpeed);
        float lateral01 = Mathf.Clamp01(lateralSpeed / Mathf.Max(0.01f, lateralSpeedForFullIntensity));
        float time01 = Mathf.Clamp01(kart.DriftTime / 0.65f);
        return Mathf.Clamp01(Mathf.Max(lateral01, 0.32f) * Mathf.Lerp(0.55f, 1f, speed01) * Mathf.Lerp(0.55f, 1f, time01));
    }

    private void UpdateTrail(TrailRenderer trail, ContactPointState contact, float visualIntensity)
    {
        if (trail == null)
        {
            return;
        }

        trail.transform.position = contact.Position;
        trail.emitting = contact.Valid && visualIntensity > 0.04f;
        trail.time = Mathf.Lerp(trailMinTime, trailMaxTime, visualIntensity);
        trail.widthMultiplier = Mathf.Lerp(trailMinWidth, trailMaxWidth, visualIntensity);
        trail.colorGradient = CreateTrailGradient(visualIntensity);
    }

    private void EmitSparks(ParticleSystem particles, ContactPointState contact, ref float budget)
    {
        if (particles == null || !contact.Valid || intensity <= 0.05f)
        {
            return;
        }

        budget += sparksPerSecond * intensity * Time.deltaTime;
        int count = Mathf.FloorToInt(budget);
        if (count <= 0)
        {
            return;
        }

        budget -= count;
        Vector3 flatVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
        float lateralSign = Mathf.Sign(Vector3.Dot(flatVelocity, transform.right));
        if (Mathf.Abs(lateralSign) < 0.01f)
        {
            lateralSign = Mathf.Sign(kart.VisualSteerInput);
        }

        Vector3 sideDirection = transform.right * lateralSign;
        Vector3 rearDirection = -transform.forward;
        Color sparkColor = EvaluateNeonColor(intensity);

        for (int i = 0; i < count; i++)
        {
            Vector3 velocity = sideDirection * Random.Range(1.6f, 4.6f)
                + rearDirection * Random.Range(0.8f, 2.2f)
                + contact.Normal * Random.Range(0.25f, 1.15f);

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = contact.Position + Random.insideUnitSphere * 0.035f,
                velocity = velocity,
                startLifetime = Random.Range(0.2f, 0.48f),
                startSize = Random.Range(0.035f, 0.12f) * Mathf.Lerp(0.8f, 1.35f, intensity),
                startColor = sparkColor,
                rotation = Random.Range(-Mathf.PI, Mathf.PI),
            };

            particles.Emit(emitParams, 1);
        }
    }

    private void BeginPulse()
    {
        pulseAge = 0f;
    }

    private void UpdatePulse(LineRenderer line, ContactPointState contact)
    {
        if (line == null)
        {
            return;
        }

        if (pulseAge > pulseDuration || !contact.Valid)
        {
            SetPulseVisible(line, false);
            return;
        }

        float pulse01 = Mathf.Clamp01(pulseAge / Mathf.Max(0.01f, pulseDuration));
        float radius = pulseRadius * Mathf.Lerp(0.25f, 1f, pulse01);
        Color color = EvaluateNeonColor(Mathf.Lerp(0.35f, 1f, intensity));
        color.a = Mathf.Lerp(0.95f, 0f, pulse01);

        SetPulseVisible(line, true);
        line.widthMultiplier = Mathf.Lerp(0.045f, 0.012f, pulse01);
        line.startColor = color;
        line.endColor = color;

        Vector3 tangent = Vector3.Cross(contact.Normal, transform.forward);
        if (tangent.sqrMagnitude < 0.001f)
        {
            tangent = transform.right;
        }

        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(tangent, contact.Normal).normalized;

        for (int i = 0; i < line.positionCount; i++)
        {
            float t = (float)i / line.positionCount * Mathf.PI * 2f;
            Vector3 offset = (Mathf.Cos(t) * tangent + Mathf.Sin(t) * bitangent) * radius;
            line.SetPosition(i, contact.Position + offset);
        }

    }

    private static void ClearTrail(TrailRenderer trail)
    {
        if (trail == null)
        {
            return;
        }

        trail.emitting = false;
        trail.Clear();
    }

    private static void SetPulseVisible(LineRenderer line, bool visible)
    {
        if (line != null)
        {
            line.enabled = visible;
        }
    }

    private static Gradient CreateTrailGradient(float visualIntensity)
    {
        Color head = EvaluateNeonColor(visualIntensity);
        Color mid = Color.Lerp(new Color(0.1f, 0.82f, 1f, 1f), new Color(1f, 0.72f, 0.18f, 1f), visualIntensity);
        Color tail = new Color(0.02f, 0.1f, 0.22f, 0f);
        head.a = Mathf.Lerp(0.32f, 0.72f, visualIntensity);
        mid.a = Mathf.Lerp(0.22f, 0.48f, visualIntensity);

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(head, 0f),
                new GradientColorKey(mid, 0.35f),
                new GradientColorKey(tail, 1f),
            },
            new[]
            {
                new GradientAlphaKey(head.a, 0f),
                new GradientAlphaKey(mid.a, 0.35f),
                new GradientAlphaKey(0f, 1f),
            });
        return gradient;
    }

    private static Gradient CreateSparkGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.65f, 1f, 1f), 0f),
                new GradientColorKey(new Color(0.12f, 0.88f, 1f), 0.35f),
                new GradientColorKey(new Color(1f, 0.78f, 0.18f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.2f),
                new GradientAlphaKey(0f, 1f),
            });
        return gradient;
    }

    private static Color EvaluateNeonColor(float value)
    {
        value = Mathf.Clamp01(value);
        Color blue = new Color(0.08f, 0.78f, 1f, 1f);
        Color cyan = new Color(0.58f, 1f, 1f, 1f);
        Color gold = new Color(1f, 0.72f, 0.16f, 1f);
        return value < 0.68f
            ? Color.Lerp(blue, cyan, value / 0.68f)
            : Color.Lerp(cyan, gold, (value - 0.68f) / 0.32f);
    }

    private readonly struct ContactPointState
    {
        public ContactPointState(Vector3 position, Vector3 normal, bool valid)
        {
            Position = position;
            Normal = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;
            Valid = valid;
        }

        public Vector3 Position { get; }
        public Vector3 Normal { get; }
        public bool Valid { get; }
    }
}
