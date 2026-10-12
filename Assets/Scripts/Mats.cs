using System.Collections.Generic;
using UnityEngine;

public static class Mats
{
    static Material opaque, transparent, additive;
    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    static Texture2D circle, soft, ray;

    static void Init()
    {
        if (opaque != null) return;
        opaque = Resources.Load<Material>("Materials/Opaque");
        transparent = Resources.Load<Material>("Materials/Transparent");
        additive = Resources.Load<Material>("Materials/Additive");
        if (opaque == null) opaque = new Material(Shader.Find("Standard"));
        if (transparent == null) transparent = new Material(opaque);
        if (additive == null) additive = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
    }

    public static Material Solid(Color c, float gloss = 0.3f)
    {
        Init();
        string key = "S" + c.ToString("F3") + gloss.ToString("F2");
        Material m;
        if (!cache.TryGetValue(key, out m))
        {
            m = new Material(opaque);
            m.color = c;
            m.SetFloat("_Glossiness", gloss);
            cache[key] = m;
        }
        return m;
    }

    public static Material Clear(Color c, Color emission)
    {
        Init();
        string key = "C" + c.ToString("F3") + emission.ToString("F3");
        Material m;
        if (!cache.TryGetValue(key, out m))
        {
            m = new Material(transparent);
            m.color = c;
            m.SetColor("_EmissionColor", emission);
            m.SetFloat("_Glossiness", 0.7f);
            cache[key] = m;
        }
        return m;
    }

    public static Material Glow(Texture tex, Color tint)
    {
        Init();
        var m = new Material(additive);
        m.mainTexture = tex;
        m.SetColor("_TintColor", tint);
        return m;
    }

    static Texture2D NewTex(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        return t;
    }

    public static Texture2D Circle()
    {
        if (circle != null) return circle;
        int n = 64;
        circle = NewTex(n, n);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ring = Mathf.Exp(-Mathf.Pow((r - 0.82f) * 9f, 2f));
                float core = Mathf.Clamp01(1f - r) * 0.25f;
                float hl = Mathf.Exp(-((dx + 0.35f) * (dx + 0.35f) + (dy - 0.35f) * (dy - 0.35f)) * 30f) * 0.9f;
                float a = r > 1f ? 0f : Mathf.Clamp01(ring + core + hl);
                circle.SetPixel(x, y, new Color(a, a, a, a));
            }
        circle.Apply();
        return circle;
    }

    public static Texture2D Soft()
    {
        if (soft != null) return soft;
        int n = 32;
        soft = NewTex(n, n);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                a *= a;
                soft.SetPixel(x, y, new Color(a, a, a, a));
            }
        soft.Apply();
        return soft;
    }

    public static Texture2D RayGradient()
    {
        if (ray != null) return ray;
        int w = 16, h = 64;
        ray = NewTex(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                float a = Mathf.Pow(v, 1.6f) * Mathf.Sin(u * Mathf.PI);
                ray.SetPixel(x, y, new Color(a, a, a, a));
            }
        ray.Apply();
        return ray;
    }
}
