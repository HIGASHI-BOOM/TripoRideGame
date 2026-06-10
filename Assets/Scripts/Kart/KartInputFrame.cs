using UnityEngine;

public struct KartInputFrame
{
    public float throttle;
    public float steer;
    public bool brake;
    public bool drift;

    public KartInputFrame(float throttle, float steer, bool brake, bool drift)
    {
        this.throttle = throttle;
        this.steer = steer;
        this.brake = brake;
        this.drift = drift;
    }

    public float Throttle => throttle;
    public float Steer => steer;
    public bool Brake => brake;
    public bool Drift => drift;
    public static KartInputFrame Neutral => new KartInputFrame(0f, 0f, false, false);

    public KartInputFrame Sanitized()
    {
        return new KartInputFrame(
            Mathf.Clamp(throttle, -1f, 1f),
            Mathf.Clamp(steer, -1f, 1f),
            brake,
            drift);
    }
}
