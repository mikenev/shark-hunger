using UnityEngine;

// The only object in the scene. Builds the whole game at runtime.
public class GameBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureBootstrap()
    {
        if (Object.FindAnyObjectByType<GameBootstrap>() == null)
            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.35f, 0.5f);
        cam.fieldOfView = 55f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 150f;
        camGO.AddComponent<AudioListener>();
        camGO.transform.position = new Vector3(0f, -9f, -17f);

        var ocean = new GameObject("Ocean").AddComponent<OceanEnvironment>();
        ocean.Build(cam);

        var shark = new GameObject("Shark").AddComponent<SharkController>();
        shark.Build();
        shark.ResetShark(new Vector3(0f, -10f, 0f));

        var follow = camGO.AddComponent<CameraFollow>();
        follow.shark = shark;
        follow.cam = cam;
        follow.Snap();

        var spawner = new GameObject("Spawner").AddComponent<Spawner>();
        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.Init(shark, spawner, follow, cam);
    }
}
