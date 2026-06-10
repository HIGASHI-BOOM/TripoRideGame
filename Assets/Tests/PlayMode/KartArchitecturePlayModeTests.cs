using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class KartArchitecturePlayModeTests
{
    [UnityTest]
    public IEnumerator ScriptedInputSourceDrivesKartController()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "InputTestGround";
        ground.transform.position = new Vector3(0f, -0.05f, 0f);
        ground.transform.localScale = new Vector3(80f, 0.1f, 80f);

        GameObject kartObject = new GameObject("InputTestKart", typeof(Rigidbody), typeof(KartScriptedInputSource), typeof(KartController));
        kartObject.transform.position = new Vector3(0f, 0.55f, 0f);

        KartController kart = kartObject.GetComponent<KartController>();
        KartScriptedInputSource inputSource = kartObject.GetComponent<KartScriptedInputSource>();
        KartDriveConfig driveConfig = ScriptableObject.CreateInstance<KartDriveConfig>();
        driveConfig.accelerationForce = 12000f;
        driveConfig.maxSpeed = 35f;
        driveConfig.downforce = 0f;
        driveConfig.uprightStability = 0f;

        kart.SetDriveConfig(driveConfig);
        kart.SetInputSource(inputSource);
        kart.SetInputEnabled(true);
        inputSource.SetInput(new KartInputFrame(1f, 0f, false, false));

        yield return null;
        for (int i = 0; i < 20; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        Assert.Greater(kart.ForwardSpeedKph, 1f);

        Object.Destroy(kartObject);
        Object.Destroy(ground);
        Object.Destroy(driveConfig);
    }

    [UnityTest]
    public IEnumerator FormalSceneDoesNotDuplicateRuntimeHud()
    {
        SceneManager.LoadScene("KartFormalGame", LoadSceneMode.Single);
        yield return null;
        yield return null;
        yield return new WaitForFixedUpdate();

        Assert.AreEqual(1, CountNamedObjects("Kart_DashboardCanvas"));
        Assert.AreEqual(1, Object.FindObjectsByType<SpeedDashboardGaugeUi>(FindObjectsInactive.Include).Length);
        Assert.AreEqual(1, Object.FindObjectsByType<KartScreenSpeedLines>(FindObjectsInactive.Include).Length);
        Assert.AreEqual(15, Object.FindObjectsByType<KartPickupBox>(FindObjectsInactive.Include).Length);
        Assert.AreEqual(1, Object.FindObjectsByType<KartPlayerInputSource>(FindObjectsInactive.Include).Length);
    }

    private static int CountNamedObjects(string objectName)
    {
        int count = 0;
        GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null && objects[i].name == objectName)
            {
                count++;
            }
        }

        return count;
    }
}
