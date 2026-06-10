#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class KartArchitectureEditModeTests
{
    private const string FormalScenePath = "Assets/Scenes/KartFormalGame.unity";
    private const string TrackDefinitionPath = "Assets/ScriptableObjects/Kart/KartFormalGameTrackDefinition.asset";

    [Test]
    public void RaceSessionTracksMultipleParticipantsAndFinishOrder()
    {
        GameObject firstObject = new GameObject("FirstKart", typeof(Rigidbody), typeof(KartController));
        GameObject secondObject = new GameObject("SecondKart", typeof(Rigidbody), typeof(KartController));

        try
        {
            KartController firstKart = firstObject.GetComponent<KartController>();
            KartController secondKart = secondObject.GetComponent<KartController>();
            RaceSession session = new RaceSession();
            session.Configure(
                new List<RaceParticipant>
                {
                    new RaceParticipant("First", firstKart, true),
                    new RaceParticipant("Second", secondKart, false)
                },
                3,
                2);

            session.PassCheckpoint(secondKart, 0, 1f);
            session.PassCheckpoint(firstKart, 0, 1f);
            session.PassCheckpoint(firstKart, 1, 2f);
            session.PassCheckpoint(firstKart, 0, 3f);
            Assert.AreSame(firstKart, session.GetRankings()[0].Kart);

            session.PassCheckpoint(firstKart, 1, 4f);
            session.PassCheckpoint(firstKart, 0, 5f);
            session.PassCheckpoint(firstKart, 1, 6f);

            RaceProgress firstProgress = session.GetProgress(firstKart);
            Assert.IsTrue(firstProgress.Finished);
            Assert.AreEqual(1, firstProgress.FinishPosition);
            Assert.AreEqual(3, firstProgress.CurrentLap);
            Assert.AreSame(firstKart, session.GetRankings()[0].Kart);
        }
        finally
        {
            Object.DestroyImmediate(firstObject);
            Object.DestroyImmediate(secondObject);
        }
    }

    [Test]
    public void FormalTrackDefinitionMatchesSceneContract()
    {
        TrackDefinition trackDefinition = AssetDatabase.LoadAssetAtPath<TrackDefinition>(TrackDefinitionPath);
        Assert.IsNotNull(trackDefinition);
        Assert.AreEqual(3, trackDefinition.TargetLaps);
        Assert.AreEqual(4, trackDefinition.CheckpointCount);
        Assert.AreEqual(15, trackDefinition.ItemBoxCount);
        Assert.Greater(trackDefinition.Centerline.Count, 0);
        Assert.Greater(trackDefinition.StartGrid.Count, 0);
        Assert.Greater(trackDefinition.AiWaypoints.Count, 0);
        Assert.Greater(trackDefinition.CameraZones.Count, 0);
    }

    [Test]
    public void FormalSceneValidatorCoversArchitectureContract()
    {
        EditorSceneManager.OpenScene(FormalScenePath);
        string report = KartFormalGameValidator.ValidateActiveSceneForAutomation();
        Assert.That(report, Does.StartWith("PASS"), report);
    }
}
#endif
