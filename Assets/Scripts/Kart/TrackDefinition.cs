using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Kart/Track Definition", fileName = "KartTrackDefinition")]
public sealed class TrackDefinition : ScriptableObject
{
    [Min(1)]
    [SerializeField] private int targetLaps = 3;
    [SerializeField] private TrackPoint[] centerline = new TrackPoint[0];
    [SerializeField] private TrackPoint[] startGrid = new TrackPoint[0];
    [SerializeField] private TrackPoint[] checkpoints = new TrackPoint[0];
    [SerializeField] private TrackPoint[] itemBoxes = new TrackPoint[0];
    [SerializeField] private TrackPoint[] aiWaypoints = new TrackPoint[0];
    [SerializeField] private TrackCameraZone[] cameraZones = new TrackCameraZone[0];

    public int TargetLaps => Mathf.Max(1, targetLaps);
    public IReadOnlyList<TrackPoint> Centerline => centerline;
    public IReadOnlyList<TrackPoint> StartGrid => startGrid;
    public IReadOnlyList<TrackPoint> Checkpoints => checkpoints;
    public IReadOnlyList<TrackPoint> ItemBoxes => itemBoxes;
    public IReadOnlyList<TrackPoint> AiWaypoints => aiWaypoints;
    public IReadOnlyList<TrackCameraZone> CameraZones => cameraZones;
    public int CheckpointCount => checkpoints != null ? checkpoints.Length : 0;
    public int ItemBoxCount => itemBoxes != null ? itemBoxes.Length : 0;

    public void Configure(
        int targetLaps,
        TrackPoint[] centerline,
        TrackPoint[] startGrid,
        TrackPoint[] checkpoints,
        TrackPoint[] itemBoxes,
        TrackPoint[] aiWaypoints,
        TrackCameraZone[] cameraZones)
    {
        this.targetLaps = Mathf.Max(1, targetLaps);
        this.centerline = centerline ?? new TrackPoint[0];
        this.startGrid = startGrid ?? new TrackPoint[0];
        this.checkpoints = checkpoints ?? new TrackPoint[0];
        this.itemBoxes = itemBoxes ?? new TrackPoint[0];
        this.aiWaypoints = aiWaypoints ?? new TrackPoint[0];
        this.cameraZones = cameraZones ?? new TrackCameraZone[0];
    }
}

[Serializable]
public struct TrackPoint
{
    [SerializeField] private string id;
    [SerializeField] private Vector3 position;
    [SerializeField] private Vector3 eulerAngles;

    public TrackPoint(string id, Vector3 position, Vector3 eulerAngles)
    {
        this.id = id;
        this.position = position;
        this.eulerAngles = eulerAngles;
    }

    public string Id => id;
    public Vector3 Position => position;
    public Quaternion Rotation => Quaternion.Euler(eulerAngles);
    public Vector3 EulerAngles => eulerAngles;
}

[Serializable]
public struct TrackCameraZone
{
    [SerializeField] private string id;
    [SerializeField] private Vector3 center;
    [SerializeField] private Vector3 size;
    [SerializeField] private float fieldOfView;

    public TrackCameraZone(string id, Vector3 center, Vector3 size, float fieldOfView)
    {
        this.id = id;
        this.center = center;
        this.size = size;
        this.fieldOfView = fieldOfView;
    }

    public string Id => id;
    public Vector3 Center => center;
    public Vector3 Size => size;
    public float FieldOfView => fieldOfView;
}
