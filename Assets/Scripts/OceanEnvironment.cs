using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class OceanEnvironment : MonoBehaviour
{
    const float Chunk = 16f;
    Transform cam, floor, surface;
    readonly Dictionary<int, GameObject> chunks = new Dictionary<int, GameObject>();
    readonly List<int> toRemove = new List<int>();
    Material rayMat;

    public void Build(Camera camera)
    {
        cam = camera.transform;
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(0.8f, 0.93f, 1f);
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.55f;
        sun.transform.rotation = Quaternion.Euler(65f, -25f, 0f);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.3f, 0.45f, 0.55f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.022f;
        RenderSettings.fogColor = camera.backgroundColor;

        floor = Shapes.Part(PrimitiveType.Cube, transform, new Vector3(0f, World.FloorY - 1f, 30f), new Vector3(600f, 2f, 100f), Mats.Solid(new Color(0.62f, 0.56f, 0.42f), 0.05f)).transform;
        var sGo = Shapes.Part(PrimitiveType.Cube, transform, new Vector3(0f, World.SurfaceY + 0.05f, 30f), new Vector3(600f, 0.1f, 100f), Mats.Clear(new Color(0.6f, 0.9f, 1f, 0.35f), new Color(0.3f, 0.5f, 0.6f)));
        Shapes.NoShadows(sGo);
        surface = sGo.transform;
        rayMat = Mats.Glow(Mats.RayGradient(), new Color(0.55f, 0.8f, 1f, 0.22f));
        BuildBubbles(cam);
        Update();
    }

    void BuildBubbles(Transform parent)
    {
        var ps = Fx.Make("Bubbles", parent, new Vector3(0f, 0f, 25f), Fx.BubbleMat);
        var main = ps.main;
        main.loop = true;
        main.startLifetime = Fx.Range(4f, 8f);
        main.startSpeed = 0f;
        main.startSize = Fx.Range(0.06f, 0.28f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 400;
        var em = ps.emission; em.rateOverTime = 30f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(50f, 26f, 30f);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = Fx.Range(-0.15f, 0.15f);
        vel.y = Fx.Range(0.8f, 2.2f);
        vel.z = Fx.Range(0f, 0f);
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);
        ps.Play();
    }

    void Update()
    {
        if (cam == null) return;
        float cx = cam.position.x;
        floor.position = new Vector3(cx, floor.position.y, floor.position.z);
        surface.position = new Vector3(cx, surface.position.y, surface.position.z);
        int ci = Mathf.FloorToInt(cx / Chunk);
        for (int i = ci - 3; i <= ci + 3; i++)
            if (!chunks.ContainsKey(i)) chunks[i] = BuildChunk(i);
        toRemove.Clear();
        foreach (var kv in chunks) if (Mathf.Abs(kv.Key - ci) > 5) toRemove.Add(kv.Key);
        foreach (var k in toRemove) { Destroy(chunks[k]); chunks.Remove(k); }
    }

    static float R(System.Random rng, float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

    GameObject BuildChunk(int idx)
    {
        var root = new GameObject("Chunk" + idx).transform;
        root.SetParent(transform, false);
        var rng = new System.Random(idx * 7919 + 1234);
        float x0 = idx * Chunk;

        int rocks = rng.Next(1, 4);
        for (int i = 0; i < rocks; i++)
        {
            var mat = Mats.Solid(Color.Lerp(new Color(0.32f, 0.34f, 0.36f), new Color(0.5f, 0.47f, 0.4f), (float)rng.Next(0, 4) / 3f), 0.15f);
            Shapes.Part(PrimitiveType.Sphere, root, new Vector3(x0 + R(rng, 0f, Chunk), World.FloorY + R(rng, 0.1f, 0.5f), R(rng, 2f, 28f)),
                new Vector3(R(rng, 1.2f, 4f), R(rng, 0.7f, 2.2f), R(rng, 1.2f, 3.5f)), mat, new Vector3(0f, R(rng, 0f, 360f), R(rng, -15f, 15f)));
        }

        Color[] greens = { new Color(0.2f, 0.5f, 0.2f), new Color(0.3f, 0.55f, 0.15f), new Color(0.15f, 0.42f, 0.28f) };
        int weeds = rng.Next(2, 6);
        for (int i = 0; i < weeds; i++)
            Seaweed(root, new Vector3(x0 + R(rng, 0f, Chunk), World.FloorY, R(rng, 1f, 24f)), rng.Next(4, 9), R(rng, 0.6f, 1.1f), greens[rng.Next(0, greens.Length)], rng);

        Color[] corals = { new Color(1f, 0.45f, 0.6f), new Color(0.6f, 0.35f, 0.85f), new Color(1f, 0.55f, 0.25f), new Color(0.95f, 0.85f, 0.3f) };
        int cc = rng.Next(0, 3);
        for (int i = 0; i < cc; i++)
            Coral(root, new Vector3(x0 + R(rng, 0f, Chunk), World.FloorY, R(rng, 1f, 20f)), corals[rng.Next(0, corals.Length)], rng);

        if (rng.NextDouble() < 0.6)
        {
            var q = Shapes.Part(PrimitiveType.Quad, root, new Vector3(x0 + R(rng, 0f, Chunk), World.SurfaceY - 11f, R(rng, 8f, 30f)),
                new Vector3(R(rng, 2f, 5f), 24f, 1f), rayMat, new Vector3(0f, 0f, R(rng, -25f, -10f)));
            Shapes.NoShadows(q);
        }

        if (rng.NextDouble() < 0.4)
        {
            Shapes.Part(PrimitiveType.Sphere, root, new Vector3(x0 + R(rng, 0f, Chunk), World.FloorY + R(rng, 2f, 6f), R(rng, 45f, 70f)),
                new Vector3(R(rng, 4f, 10f), R(rng, 8f, 20f), R(rng, 4f, 10f)), Mats.Solid(new Color(0.2f, 0.25f, 0.3f), 0.1f));
        }
        return root.gameObject;
    }

    void Seaweed(Transform parent, Vector3 basePos, int segs, float segLen, Color col, System.Random rng)
    {
        var mat = Mats.Solid(col, 0.25f);
        Transform p = Shapes.Pivot(parent, basePos, "Kelp");
        float phase = (float)rng.NextDouble() * 6f;
        for (int i = 0; i < segs; i++)
        {
            float w = Mathf.Lerp(0.28f, 0.14f, i / (float)segs);
            Shapes.Part(PrimitiveType.Capsule, p, new Vector3(0f, segLen * 0.5f, 0f), new Vector3(w, segLen * 0.6f, w * 0.6f), mat);
            var sw = p.gameObject.AddComponent<Sway>();
            sw.amp = 5f + i * 1.5f; sw.speed = 1.2f; sw.offset = phase + i * 0.5f;
            p = Shapes.Pivot(p, new Vector3(0f, segLen, 0f), "Seg");
        }
    }

    void Coral(Transform parent, Vector3 pos, Color col, System.Random rng)
    {
        var mat = Mats.Solid(col, 0.2f);
        var root = Shapes.Pivot(parent, pos, "Coral");
        if (rng.NextDouble() < 0.4)
        {
            Shapes.Part(PrimitiveType.Sphere, root, Vector3.zero, new Vector3(R(rng, 1.2f, 2f), R(rng, 0.8f, 1.3f), R(rng, 1.2f, 1.8f)), mat);
            return;
        }
        int branches = rng.Next(3, 7);
        for (int i = 0; i < branches; i++)
        {
            float len = R(rng, 0.8f, 2f);
            var b = Shapes.Pivot(root, Vector3.zero, "Branch");
            b.localEulerAngles = new Vector3(R(rng, -30f, 30f), 0f, R(rng, -50f, 50f));
            Shapes.Part(PrimitiveType.Cylinder, b, new Vector3(0f, len * 0.5f, 0f), new Vector3(0.18f, len * 0.5f, 0.18f), mat);
            Shapes.Part(PrimitiveType.Sphere, b, new Vector3(0f, len, 0f), Vector3.one * 0.3f, mat);
        }
    }
}
