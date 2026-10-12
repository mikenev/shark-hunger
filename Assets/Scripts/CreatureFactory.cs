using System.Collections.Generic;
using UnityEngine;

public static class CreatureFactory
{
    static readonly Color Black = new Color(0.03f, 0.03f, 0.05f);

    public static Creature Create(CreatureKind kind, Vector3 pos, float dir)
    {
        var info = CreatureDB.Get(kind);
        var go = new GameObject(info.name);
        go.transform.position = pos;
        var model = new GameObject("Model").transform;
        model.SetParent(go.transform, false);
        var c = go.AddComponent<Creature>();
        c.model = model;

        switch (kind)
        {
            case CreatureKind.SmallFish:
                BuildFish(c, new Color(0.55f, 0.66f, 0.82f), new Color(0.9f, 0.94f, 1f), new Color(0.45f, 0.55f, 0.75f), 0.9f, 0.3f, 0.85f);
                break;
            case CreatureKind.ReefFish:
            {
                BuildFish(c, new Color(1f, 0.48f, 0.08f), new Color(1f, 0.7f, 0.35f), new Color(0.95f, 0.38f, 0.05f), 0.9f, 0.55f, 0.5f);
                var white = Mats.Solid(Color.white, 0.4f);
                float W = 0.55f * 0.55f;
                Shapes.Part(PrimitiveType.Sphere, model, new Vector3(0.16f, 0f, 0f), new Vector3(0.12f, 0.5f, W * 1.06f), white);
                Shapes.Part(PrimitiveType.Sphere, model, new Vector3(-0.16f, 0f, 0f), new Vector3(0.1f, 0.42f, W * 1.04f), white);
                break;
            }
            case CreatureKind.BigFish:
                BuildFish(c, new Color(0.15f, 0.25f, 0.5f), new Color(0.8f, 0.84f, 0.9f), new Color(0.95f, 0.78f, 0.2f), 2.6f, 0.85f, 0.7f);
                break;
            case CreatureKind.GoldenFish:
                BuildFish(c, new Color(1f, 0.78f, 0.12f), new Color(1f, 0.95f, 0.6f), new Color(1f, 0.55f, 0.08f), 1.1f, 0.5f, 0.95f);
                Fx.Sparkles(go.transform, new Color(1f, 0.9f, 0.4f, 1f));
                break;
            case CreatureKind.Pufferfish: BuildPuffer(c); break;
            case CreatureKind.Turtle: BuildTurtle(c); break;
            case CreatureKind.Jellyfish: BuildJelly(c); break;
            case CreatureKind.Crab: BuildCrab(c); break;
        }

        c.Init(info, dir);
        return c;
    }

    static void Eyes(Transform m, float x, float y, float z, float size)
    {
        var white = Mats.Solid(Color.white, 0.8f);
        var black = Mats.Solid(Black, 0.9f);
        for (int s = -1; s <= 1; s += 2)
        {
            Shapes.Part(PrimitiveType.Sphere, m, new Vector3(x, y, s * z), Vector3.one * size, white);
            Shapes.Part(PrimitiveType.Sphere, m, new Vector3(x + size * 0.1f, y, s * (z + size * 0.32f)), Vector3.one * size * 0.6f, black);
        }
    }

    static void BuildFish(Creature c, Color body, Color belly, Color fin, float L, float H, float gloss)
    {
        var m = c.model;
        var bodyMat = Mats.Solid(body, gloss);
        var bellyMat = Mats.Solid(belly, gloss);
        var finMat = Mats.Solid(fin, 0.2f);
        float W = H * 0.55f;
        Shapes.Part(PrimitiveType.Sphere, m, Vector3.zero, new Vector3(L, H, W), bodyMat);
        Shapes.Part(PrimitiveType.Sphere, m, new Vector3(0.04f * L, -0.14f * H, 0f), new Vector3(L * 0.86f, H * 0.66f, W * 0.92f), bellyMat);
        var tail = Shapes.Pivot(m, new Vector3(-0.42f * L, 0f, 0f), "Tail");
        Shapes.MeshPart(Shapes.Fin(), tail, new Vector3(-0.06f * L, 0f, 0f), new Vector3(H * 0.6f, H * 0.8f, 0.05f), finMat, new Vector3(0f, 0f, 40f));
        Shapes.MeshPart(Shapes.Fin(), tail, new Vector3(-0.06f * L, 0f, 0f), new Vector3(H * 0.6f, H * 0.8f, 0.05f), finMat, new Vector3(0f, 0f, 140f));
        Shapes.MeshPart(Shapes.Fin(), m, new Vector3(-0.05f * L, H * 0.38f, 0f), new Vector3(L * 0.45f, H * 0.5f, 0.04f), finMat);
        Shapes.MeshPart(Shapes.Fin(), m, new Vector3(0.05f * L, -H * 0.1f, -W * 0.4f), new Vector3(H * 0.4f, H * 0.4f, 0.03f), finMat, new Vector3(25f, 0f, 150f));
        Eyes(m, 0.3f * L, 0.1f * H, 0.36f * W, 0.22f * H);
        c.tail = tail;
    }

    static void BuildPuffer(Creature c)
    {
        var m = c.model;
        var body = Mats.Solid(new Color(0.88f, 0.78f, 0.42f), 0.4f);
        var belly = Mats.Solid(new Color(0.97f, 0.95f, 0.85f), 0.4f);
        var spot = Mats.Solid(new Color(0.45f, 0.35f, 0.18f), 0.3f);
        var finMat = Mats.Solid(new Color(0.85f, 0.7f, 0.3f), 0.2f);
        Shapes.Part(PrimitiveType.Sphere, m, Vector3.zero, new Vector3(0.8f, 0.7f, 0.7f), body);
        Shapes.Part(PrimitiveType.Sphere, m, new Vector3(0f, -0.12f, 0f), new Vector3(0.7f, 0.5f, 0.62f), belly);
        Vector3[] spots = { new Vector3(-0.1f, 0.2f, -0.27f), new Vector3(0.08f, 0.26f, -0.2f), new Vector3(-0.22f, 0.08f, -0.25f), new Vector3(-0.1f, 0.2f, 0.27f), new Vector3(-0.22f, 0.08f, 0.25f) };
        foreach (var p in spots) Shapes.Part(PrimitiveType.Sphere, m, p, Vector3.one * 0.12f, spot);
        var tail = Shapes.Pivot(m, new Vector3(-0.38f, 0f, 0f), "Tail");
        Shapes.MeshPart(Shapes.Fin(), tail, Vector3.zero, new Vector3(0.25f, 0.3f, 0.04f), finMat, new Vector3(0f, 0f, 45f));
        Shapes.MeshPart(Shapes.Fin(), tail, Vector3.zero, new Vector3(0.25f, 0.3f, 0.04f), finMat, new Vector3(0f, 0f, 135f));
        Eyes(m, 0.22f, 0.1f, 0.24f, 0.2f);
        int n = 14;
        var list = new Transform[n];
        var spikeMat = Mats.Solid(new Color(0.75f, 0.65f, 0.35f), 0.3f);
        for (int i = 0; i < n; i++)
        {
            float yv = 1f - (i + 0.5f) / n * 2f;
            float r = Mathf.Sqrt(1f - yv * yv);
            float th = i * 2.39996f;
            var d = new Vector3(Mathf.Cos(th) * r, yv, Mathf.Sin(th) * r);
            var sp = Shapes.MeshPart(Shapes.Cone(), m, Vector3.Scale(d, new Vector3(0.38f, 0.33f, 0.33f)), Vector3.zero, spikeMat).transform;
            sp.localRotation = Quaternion.FromToRotation(Vector3.up, d);
            list[i] = sp;
        }
        c.spikes = list;
        c.tail = tail;
    }

    static void BuildTurtle(Creature c)
    {
        var m = c.model;
        var shell = Mats.Solid(new Color(0.33f, 0.42f, 0.18f), 0.35f);
        var plate = Mats.Solid(new Color(0.24f, 0.31f, 0.12f), 0.35f);
        var skin = Mats.Solid(new Color(0.55f, 0.63f, 0.38f), 0.25f);
        var belly = Mats.Solid(new Color(0.86f, 0.8f, 0.55f), 0.25f);
        Shapes.Part(PrimitiveType.Sphere, m, new Vector3(0f, 0.1f, 0f), new Vector3(1.7f, 0.75f, 1.2f), shell);
        Shapes.Part(PrimitiveType.Sphere, m, new Vector3(0f, -0.08f, 0f), new Vector3(1.6f, 0.4f, 1.1f), belly);
        for (int i = -1; i <= 1; i++)
        {
            Shapes.Part(PrimitiveType.Sphere, m, new Vector3(i * 0.45f, 0.36f, 0f), new Vector3(0.5f, 0.2f, 0.55f), plate);
            Shapes.Part(PrimitiveType.Sphere, m, new Vector3(i * 0.5f, 0.18f, -0.42f), new Vector3(0.4f, 0.25f, 0.3f), plate);
            Shapes.Part(PrimitiveType.Sphere, m, new Vector3(i * 0.5f, 0.18f, 0.42f), new Vector3(0.4f, 0.25f, 0.3f), plate);
        }
        Shapes.Part(PrimitiveType.Sphere, m, new Vector3(0.75f, 0.05f, 0f), new Vector3(0.4f, 0.3f, 0.32f), skin);
        Shapes.Part(PrimitiveType.Sphere, m, new Vector3(1.0f, 0.08f, 0f), new Vector3(0.5f, 0.38f, 0.36f), skin);
        var black = Mats.Solid(Black, 0.9f);
        Shapes.Part(PrimitiveType.Sphere, m, new Vector3(1.12f, 0.15f, -0.15f), Vector3.one * 0.08f, black);
        Shapes.Part(PrimitiveType.Sphere, m, new Vector3(1.12f, 0.15f, 0.15f), Vector3.one * 0.08f, black);
        var limbs = new List<Transform>();
        for (int s = -1; s <= 1; s += 2)
        {
            var front = Shapes.Pivot(m, new Vector3(0.45f, -0.05f, s * 0.45f), "FrontFlipper");
            Shapes.Part(PrimitiveType.Sphere, front, new Vector3(-0.25f, -0.42f, 0f), new Vector3(0.3f, 1.0f, 0.12f), skin, new Vector3(0f, 0f, -32f));
            limbs.Add(front);
            var rear = Shapes.Pivot(m, new Vector3(-0.6f, -0.1f, s * 0.4f), "RearFlipper");
            Shapes.Part(PrimitiveType.Sphere, rear, new Vector3(-0.15f, -0.2f, 0f), new Vector3(0.22f, 0.5f, 0.1f), skin, new Vector3(0f, 0f, -50f));
            limbs.Add(rear);
        }
        c.limbs = limbs.ToArray();
    }

    static void BuildJelly(Creature c)
    {
        var m = c.model;
        var dome = Mats.Clear(new Color(1f, 0.55f, 0.85f, 0.55f), new Color(0.45f, 0.12f, 0.35f));
        var inner = Mats.Clear(new Color(1f, 0.8f, 0.95f, 0.75f), new Color(0.6f, 0.3f, 0.5f));
        var tent = Mats.Clear(new Color(1f, 0.6f, 0.9f, 0.6f), new Color(0.5f, 0.2f, 0.4f));
        var d = Shapes.Part(PrimitiveType.Sphere, m, new Vector3(0f, 0.1f, 0f), new Vector3(1.1f, 0.8f, 1.1f), dome);
        var inn = Shapes.Part(PrimitiveType.Sphere, m, new Vector3(0f, 0.05f, 0f), new Vector3(0.6f, 0.4f, 0.6f), inner);
        Shapes.NoShadows(d); Shapes.NoShadows(inn);
        var limbs = new List<Transform>();
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2f;
            var p1 = Shapes.Pivot(m, new Vector3(Mathf.Cos(a) * 0.42f, -0.15f, Mathf.Sin(a) * 0.42f), "Tentacle");
            Shapes.NoShadows(Shapes.Part(PrimitiveType.Cylinder, p1, new Vector3(0f, -0.35f, 0f), new Vector3(0.05f, 0.35f, 0.05f), tent));
            var p2 = Shapes.Pivot(p1, new Vector3(0f, -0.7f, 0f), "Tentacle2");
            Shapes.NoShadows(Shapes.Part(PrimitiveType.Cylinder, p2, new Vector3(0f, -0.3f, 0f), new Vector3(0.035f, 0.3f, 0.035f), tent));
            limbs.Add(p1); limbs.Add(p2);
        }
        Shapes.NoShadows(Shapes.Part(PrimitiveType.Capsule, m, new Vector3(0.1f, -0.5f, 0f), new Vector3(0.18f, 0.45f, 0.18f), inner));
        Shapes.NoShadows(Shapes.Part(PrimitiveType.Capsule, m, new Vector3(-0.1f, -0.55f, 0.05f), new Vector3(0.16f, 0.5f, 0.16f), inner));
        c.limbs = limbs.ToArray();
    }

    static void BuildCrab(Creature c)
    {
        var m = c.model;
        m.localPosition = new Vector3(0f, -0.3f, 0f);
        var red = Mats.Solid(new Color(0.85f, 0.28f, 0.16f), 0.45f);
        var dark = Mats.Solid(new Color(0.6f, 0.15f, 0.1f), 0.4f);
        var black = Mats.Solid(Black, 0.9f);
        Shapes.Part(PrimitiveType.Sphere, m, new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.42f, 0.65f), red);
        for (int s = -1; s <= 1; s += 2)
        {
            Shapes.Part(PrimitiveType.Cylinder, m, new Vector3(s * 0.15f, 0.55f, -0.2f), new Vector3(0.04f, 0.12f, 0.04f), dark);
            Shapes.Part(PrimitiveType.Sphere, m, new Vector3(s * 0.15f, 0.68f, -0.2f), Vector3.one * 0.1f, black);
            Shapes.Part(PrimitiveType.Sphere, m, new Vector3(s * 0.5f, 0.35f, -0.3f), new Vector3(0.35f, 0.22f, 0.22f), red);
            Shapes.Part(PrimitiveType.Sphere, m, new Vector3(s * 0.64f, 0.46f, -0.38f), new Vector3(0.3f, 0.2f, 0.18f), dark, new Vector3(0f, 0f, s * 20f));
        }
        var limbs = new List<Transform>();
        for (int s = -1; s <= 1; s += 2)
            for (int i = 0; i < 3; i++)
            {
                var p = Shapes.Pivot(m, new Vector3(s * 0.35f, 0.25f, -0.15f + 0.15f * i), "Leg");
                Shapes.Part(PrimitiveType.Cylinder, p, new Vector3(s * 0.18f, -0.12f, 0f), new Vector3(0.06f, 0.18f, 0.06f), dark, new Vector3(0f, 0f, s * 55f));
                limbs.Add(p);
            }
        c.limbs = limbs.ToArray();
    }
}
