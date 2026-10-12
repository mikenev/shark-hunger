using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class SharkBuild
{
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string MatDir = "Assets/Resources/Materials";

    [MenuItem("Shark Hunger/Setup Project")]
    public static void Setup()
    {
        Directory.CreateDirectory(MatDir);
        Directory.CreateDirectory("Assets/Scenes");
        AssetDatabase.Refresh();

        var opaque = MakeMat("Opaque", Shader.Find("Standard"));
        opaque.SetFloat("_Glossiness", 0.35f);

        var tr = MakeMat("Transparent", Shader.Find("Standard"));
        tr.SetFloat("_Mode", 2f);
        tr.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        tr.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        tr.SetInt("_ZWrite", 0);
        tr.DisableKeyword("_ALPHATEST_ON");
        tr.EnableKeyword("_ALPHABLEND_ON");
        tr.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        tr.EnableKeyword("_EMISSION");
        tr.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        tr.SetColor("_EmissionColor", new Color(0.3f, 0.1f, 0.3f));
        tr.color = new Color(1f, 1f, 1f, 0.5f);
        tr.renderQueue = (int)RenderQueue.Transparent;

        var add = Shader.Find("Legacy Shaders/Particles/Additive");
        if (add == null) add = Shader.Find("Mobile/Particles/Additive");
        Debug.Log("[SharkBuild] Additive shader: " + (add != null ? add.name : "NULL"));
        MakeMat("Additive", add);
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.022f;
        RenderSettings.fogColor = new Color(0.08f, 0.35f, 0.5f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.3f, 0.45f, 0.55f);
        RenderSettings.skybox = null;
        new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.companyName = "Mike";
        PlayerSettings.productName = "Shark Hunger";
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching = true;

        // Make sure legacy Input Manager is active (0 = Input Manager, 2 = Both).
        var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (ps != null && ps.Length > 0)
        {
            var so = new SerializedObject(ps[0]);
            var p = so.FindProperty("activeInputHandler");
            if (p != null)
            {
                Debug.Log("[SharkBuild] activeInputHandler = " + p.intValue);
                if (p.intValue == 1) { p.intValue = 0; so.ApplyModifiedProperties(); Debug.Log("[SharkBuild] switched to legacy Input Manager"); }
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[SharkBuild] Setup complete");
    }

    static Material MakeMat(string name, Shader shader)
    {
        string path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else m.shader = shader;
        EditorUtility.SetDirty(m);
        return m;
    }

    public static void BuildWebGL() { Setup(); Build(BuildTarget.WebGL, "Builds/WebGL"); }
    public static void BuildMac() { Setup(); Build(BuildTarget.StandaloneOSX, "Builds/Mac/SharkHunger.app"); }

    static void Build(BuildTarget target, string path)
    {
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = path,
            target = target,
            targetGroup = BuildPipeline.GetBuildTargetGroup(target),
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opts);
        var s = report.summary;
        Debug.Log("[SharkBuild] Result: " + s.result + " errors=" + s.totalErrors + " warnings=" + s.totalWarnings + " size=" + s.totalSize + " time=" + s.totalTime + " output=" + s.outputPath);
        if (s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
