using UnityEngine;
using UnityEngine.Rendering;

public static class Fx
{
    static Material soft, bubble;

    public static Material SoftMat
    {
        get { if (soft == null) soft = Mats.Glow(Mats.Soft(), new Color(1f, 1f, 1f, 0.6f)); return soft; }
    }

    public static Material BubbleMat
    {
        get { if (bubble == null) bubble = Mats.Glow(Mats.Circle(), new Color(0.7f, 0.9f, 1f, 0.55f)); return bubble; }
    }

    public static ParticleSystem.MinMaxCurve Range(float a, float b) { return new ParticleSystem.MinMaxCurve(a, b); }

    public static ParticleSystem Make(string name, Transform parent, Vector3 localPos, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return ps;
    }

    public static ParticleSystem Sparkles(Transform parent, Color color)
    {
        var ps = Make("Sparkles", parent, Vector3.zero, SoftMat);
        var main = ps.main;
        main.loop = true;
        main.startLifetime = Range(0.5f, 1f);
        main.startSpeed = Range(0.2f, 0.6f);
        main.startSize = Range(0.06f, 0.18f);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;
        var em = ps.emission; em.rateOverTime = 14f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.4f;
        ps.Play();
        return ps;
    }
}

public static class Sfx
{
    static AudioSource src;
    public static AudioClip Chomp, Sting, Over, StartClip;

    public static void Init(GameObject host)
    {
        try
        {
            src = host.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            var rng = new System.Random(7);
            Chomp = Make("chomp", 0.18f, t => ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 35f) * 0.6f + Mathf.Sin(2f * Mathf.PI * 110f * t) * Mathf.Exp(-t * 18f) * 0.9f);
            Sting = Make("sting", 0.45f, t => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * (220f + 60f * Mathf.Sin(t * 60f)) * t)) * 0.3f * Mathf.Exp(-t * 5f));
            Over = Make("over", 1.2f, t => Mathf.Sin(2f * Mathf.PI * (300f - 100f * t) * t) * 0.6f * (1f - t / 1.2f));
            StartClip = Make("start", 0.35f, t => Mathf.Sin(2f * Mathf.PI * (300f + 500f * t) * t) * 0.4f * (1f - t / 0.35f));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Audio disabled: " + e.Message);
        }
    }

    static AudioClip Make(string name, float dur, System.Func<float, float> f)
    {
        const int rate = 44100;
        int n = Mathf.CeilToInt(dur * rate);
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Clamp01((dur - t) * 40f) * Mathf.Clamp01(t * 2000f);
            d[i] = Mathf.Clamp(f(t) * env, -1f, 1f) * 0.5f;
        }
        var c = AudioClip.Create(name, n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    public static void Play(AudioClip c, float vol = 1f, float pitch = 1f)
    {
        if (src == null || c == null) return;
        src.pitch = pitch;
        src.PlayOneShot(c, vol);
    }
}
