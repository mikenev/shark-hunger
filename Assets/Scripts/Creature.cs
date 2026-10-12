using UnityEngine;

public enum CreatureKind { SmallFish, ReefFish, BigFish, Turtle, Jellyfish, Pufferfish, Crab, GoldenFish }

public class CreatureInfo
{
    public CreatureKind kind;
    public string name;
    public float hunger;
    public int score;
    public float speed, radius, fleeRange, fleeSpeed;
    public Color color;
    public string note;

    public CreatureInfo(CreatureKind k, string n, float h, int s, float sp, float r, float fr, float fs, Color c, string note)
    {
        kind = k; name = n; hunger = h; score = s; speed = sp; radius = r; fleeRange = fr; fleeSpeed = fs; color = c; this.note = note;
    }
}

public static class CreatureDB
{
    public static readonly CreatureInfo[] All =
    {
        new CreatureInfo(CreatureKind.SmallFish,  "Sardine",     6,  10, 3.2f, 0.35f, 4f, 5f,   new Color(0.7f, 0.8f, 0.95f), "Quick snack. Swims in schools."),
        new CreatureInfo(CreatureKind.ReefFish,   "Clownfish",   10, 20, 2.4f, 0.4f,  3f, 3.8f, new Color(1f, 0.55f, 0.1f),   "Easy prey."),
        new CreatureInfo(CreatureKind.BigFish,    "Tuna",        22, 60, 4.5f, 0.85f, 7f, 8f,   new Color(0.3f, 0.45f, 0.85f), "Big meal, but flees fast. Boost!"),
        new CreatureInfo(CreatureKind.Turtle,     "Sea Turtle",  30, 100, 1.4f, 0.9f, 0f, 0f,   new Color(0.4f, 0.65f, 0.25f), "Slow and very filling."),
        new CreatureInfo(CreatureKind.Jellyfish,  "Jellyfish",  -15, 0,  0.6f, 0.6f,  0f, 0f,   new Color(1f, 0.45f, 0.85f),  "STINGS! Drains hunger. Avoid."),
        new CreatureInfo(CreatureKind.Pufferfish, "Pufferfish",  14, 40, 1.6f, 0.45f, 0f, 0f,   new Color(0.95f, 0.85f, 0.4f), "Puffs up near you. Spiky = -10!"),
        new CreatureInfo(CreatureKind.Crab,       "Crab",        15, 40, 1.2f, 0.5f,  3f, 2.6f, new Color(0.9f, 0.3f, 0.2f),   "Scuttles along the seafloor."),
        new CreatureInfo(CreatureKind.GoldenFish, "Golden Fish", 45, 250, 5f,  0.5f,  8f, 9.5f, new Color(1f, 0.85f, 0.2f),   "Rare and fast. Huge reward!"),
    };

    public static CreatureInfo Get(CreatureKind k) { return All[(int)k]; }
}

public class Creature : MonoBehaviour
{
    public CreatureInfo info;
    public Transform model, tail;
    public Transform[] limbs;
    public Transform[] spikes;
    public Vector2 velocity;

    float dir = 1f, phase, tailPhase, turnTimer, puff, yaw, fleeTime;
    static readonly Vector3 SpikeScale = new Vector3(0.09f, 0.22f, 0.09f);

    public Vector3 Center { get { return transform.position; } }
    public bool Inflated { get { return info.kind == CreatureKind.Pufferfish && puff > 0.5f; } }
    public float Radius { get { return info.radius * (info.kind == CreatureKind.Pufferfish ? Mathf.Lerp(1f, 1.6f, puff) : 1f); } }
    public float HungerValue { get { return Inflated ? -10f : info.hunger; } }
    public int ScoreValue { get { return Inflated ? 5 : info.score; } }

    public void Init(CreatureInfo i, float direction)
    {
        info = i;
        dir = direction >= 0f ? 1f : -1f;
        phase = Random.value * 10f;
        turnTimer = Random.Range(6f, 14f);
        velocity = new Vector2(dir * info.speed, 0f);
        yaw = dir > 0f ? 0f : 180f;
        if (spikes != null) foreach (var s in spikes) s.localScale = Vector3.zero;
        if (info.kind != CreatureKind.Jellyfish && info.kind != CreatureKind.Crab)
            model.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || info == null) return;
        float dt = Time.deltaTime;
        phase += dt;
        Vector2 pos = transform.position;
        Vector2 shark = gm.SharkPos;
        Vector2 toMe = pos - shark;
        float dist = toMe.magnitude;
        bool hunted = gm.Playing;
        bool fleeing = false;
        Vector2 desired;

        switch (info.kind)
        {
            case CreatureKind.Jellyfish:
            {
                float pulse = Mathf.Max(0f, Mathf.Sin(phase * 2.2f));
                desired = new Vector2(dir * info.speed, -0.35f + pulse * 1.4f);
                model.localScale = new Vector3(1f + pulse * 0.12f, 1f - pulse * 0.15f, 1f + pulse * 0.12f);
                break;
            }
            case CreatureKind.Crab:
            {
                float spd = info.speed;
                if (hunted && dist < info.fleeRange)
                {
                    dir = toMe.x >= 0f ? 1f : -1f;
                    spd = info.fleeSpeed;
                    fleeing = true;
                }
                desired = new Vector2(dir * spd, 0f);
                break;
            }
            default:
            {
                desired = new Vector2(dir * info.speed, Mathf.Sin(phase * 0.9f) * info.speed * 0.35f);
                if (info.kind == CreatureKind.GoldenFish) desired.y += Mathf.Sin(phase * 4f) * 2.5f;
                if (hunted && info.fleeRange > 0f && dist < info.fleeRange)
                {
                    fleeing = true;
                    fleeTime += dt;
                    float tired = fleeTime > 3.5f ? 0.6f : 1f;
                    Vector2 away = dist > 0.01f ? toMe / dist : Vector2.right;
                    desired = away * info.fleeSpeed * tired;
                    if (Mathf.Abs(away.x) > 0.05f) dir = Mathf.Sign(away.x);
                }
                if (info.kind == CreatureKind.Pufferfish)
                {
                    float target = hunted && dist < 4.5f ? 1f : 0f;
                    puff = Mathf.MoveTowards(puff, target, dt * (target > puff ? 3f : 0.8f));
                    model.localScale = Vector3.one * Mathf.Lerp(1f, 1.6f, puff);
                    if (spikes != null) foreach (var s in spikes) s.localScale = SpikeScale * puff;
                    desired *= Mathf.Lerp(1f, 0.25f, puff);
                }
                break;
            }
        }
        if (!fleeing) fleeTime = Mathf.Max(0f, fleeTime - dt * 0.5f);

        turnTimer -= dt;
        if (!fleeing)
        {
            if (turnTimer <= 0f) { dir = -dir; turnTimer = Random.Range(6f, 14f); }
            float dx = pos.x - shark.x;
            if (Mathf.Abs(dx) > 24f && Mathf.Sign(dx) == dir) dir = -dir;
        }

        float accel = info.speed * 2f + 4f;
        velocity = Vector2.MoveTowards(velocity, desired, accel * dt);
        pos += velocity * dt;

        if (info.kind == CreatureKind.Crab)
        {
            pos.y = World.FloorY + 0.3f;
            velocity.y = 0f;
        }
        else
        {
            float minY = World.FloorY + Radius + 0.2f, maxY = World.SurfaceY - Radius - 0.3f;
            if (pos.y < minY) { pos.y = minY; velocity.y = Mathf.Abs(velocity.y) * 0.5f; }
            if (pos.y > maxY) { pos.y = maxY; velocity.y = -Mathf.Abs(velocity.y) * 0.5f; }
        }
        transform.position = new Vector3(pos.x, pos.y, 0f);

        if (info.kind != CreatureKind.Jellyfish && info.kind != CreatureKind.Crab)
        {
            float face = Mathf.Abs(velocity.x) > 0.15f ? Mathf.Sign(velocity.x) : dir;
            yaw = Mathf.MoveTowardsAngle(yaw, face > 0f ? 0f : 180f, 400f * dt);
            float pitch = Mathf.Clamp(Mathf.Atan2(velocity.y, Mathf.Max(Mathf.Abs(velocity.x), 0.6f)) * Mathf.Rad2Deg, -35f, 35f);
            model.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, pitch);
        }

        tailPhase += dt * (7f + velocity.magnitude * 2.5f);
        if (tail != null) tail.localRotation = Quaternion.Euler(0f, Mathf.Sin(tailPhase) * 25f, 0f);

        if (limbs != null)
        {
            for (int i = 0; i < limbs.Length; i++)
            {
                var l = limbs[i];
                switch (info.kind)
                {
                    case CreatureKind.Turtle:
                        l.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 2.2f + (i % 2) * 0.6f) * 28f);
                        break;
                    case CreatureKind.Jellyfish:
                        l.localRotation = Quaternion.Euler(Mathf.Sin(phase * 1.7f + i) * 15f, 0f, Mathf.Cos(phase * 1.3f + i * 0.7f) * 15f);
                        break;
                    case CreatureKind.Crab:
                        l.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 10f + i * 1.3f) * 12f);
                        break;
                }
            }
        }
    }
}
