using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public readonly List<Creature> Active = new List<Creature>();
    float timer;
    static readonly CreatureKind[] Kinds = (CreatureKind[])System.Enum.GetValues(typeof(CreatureKind));

    public void Clear()
    {
        foreach (var c in Active) if (c != null) Destroy(c.gameObject);
        Active.Clear();
    }

    public void Remove(Creature c)
    {
        Active.Remove(c);
        if (c != null) Destroy(c.gameObject);
    }

    public static int TargetCount(float t) { return Mathf.Min(14 + Mathf.FloorToInt(t / 10f), 34); }

    public void Tick(float elapsed, Vector2 shark, Vector2 sharkVel)
    {
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            var c = Active[i];
            if (c == null) { Active.RemoveAt(i); continue; }
            if (Mathf.Abs(c.transform.position.x - shark.x) > 48f)
            {
                Destroy(c.gameObject);
                Active.RemoveAt(i);
            }
        }
        timer -= Time.deltaTime;
        if (timer <= 0f && Active.Count < TargetCount(elapsed))
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            if (Mathf.Abs(sharkVel.x) > 1f && Random.value < 0.7f) side = Mathf.Sign(sharkVel.x);
            float dir = Random.value < 0.75f ? -side : side;
            SpawnAt(Pick(elapsed), shark.x + side * Random.Range(19f, 28f), dir);
            timer = 0.3f;
        }
    }

    public void Populate(Vector2 shark, int n, float elapsed)
    {
        for (int i = 0; i < n; i++)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            SpawnAt(Pick(elapsed), shark.x + side * Random.Range(8f, 26f), Random.value < 0.5f ? -1f : 1f);
        }
    }

    CreatureKind Pick(float t)
    {
        float total = 0f;
        foreach (var k in Kinds) total += Weight(k, t);
        float r = Random.value * total;
        foreach (var k in Kinds)
        {
            r -= Weight(k, t);
            if (r <= 0f) return k;
        }
        return CreatureKind.SmallFish;
    }

    // Difficulty: food gets rarer and jellyfish more common over time.
    static float Weight(CreatureKind k, float t)
    {
        float food = Mathf.Lerp(1f, 0.6f, t / 300f);
        switch (k)
        {
            case CreatureKind.SmallFish: return 24f * food;
            case CreatureKind.ReefFish: return 16f * food;
            case CreatureKind.BigFish: return 10f * food;
            case CreatureKind.Turtle: return 6f * food;
            case CreatureKind.Jellyfish: return 5f + Mathf.Min(t * 0.12f, 28f);
            case CreatureKind.Pufferfish: return 7f;
            case CreatureKind.Crab: return 6f * food;
            case CreatureKind.GoldenFish: return t > 20f ? 1.6f : 0f;
        }
        return 0f;
    }

    void SpawnAt(CreatureKind k, float x, float dir)
    {
        float y;
        switch (k)
        {
            case CreatureKind.Crab: y = World.FloorY + 0.3f; break;
            case CreatureKind.Turtle: y = Random.Range(World.FloorY + 3f, World.SurfaceY - 3f); break;
            default: y = Random.Range(World.FloorY + 1.5f, World.SurfaceY - 1.2f); break;
        }
        if (k == CreatureKind.SmallFish)
        {
            int n = Random.Range(3, 6);
            for (int i = 0; i < n; i++)
            {
                float yy = Mathf.Clamp(y + Random.Range(-1f, 1f), World.FloorY + 1f, World.SurfaceY - 1f);
                Active.Add(CreatureFactory.Create(k, new Vector3(x + Random.Range(-1.5f, 1.5f), yy, 0f), dir));
            }
        }
        else
        {
            Active.Add(CreatureFactory.Create(k, new Vector3(x, y, 0f), dir));
        }
    }
}
