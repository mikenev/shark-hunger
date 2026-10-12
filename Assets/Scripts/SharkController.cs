using UnityEngine;

public class SharkController : MonoBehaviour
{
    public Vector2 Velocity;
    public bool Boosting;
    public float BiteRadius = 0.75f;

    const float Speed = 6.5f, BoostSpeed = 11f, Accel = 14f;

    Transform model, tailPivot, jawPivot;
    float yaw, facing = 1f, tailPhase, biteTimer, hurtTimer, deadRoll;
    ParticleSystem trail;

    public Vector3 MouthPos { get { return model.TransformPoint(new Vector3(1.55f, -0.18f, 0f)); } }

    public void Build()
    {
        model = new GameObject("Model").transform;
        model.SetParent(transform, false);
        var skin = Mats.Solid(new Color(0.4f, 0.5f, 0.6f), 0.45f);
        var belly = Mats.Solid(new Color(0.93f, 0.93f, 0.9f), 0.35f);
        var dark = Mats.Solid(new Color(0.03f, 0.03f, 0.05f), 0.95f);
        var mouth = Mats.Solid(new Color(0.55f, 0.1f, 0.12f), 0.3f);
        var gill = Mats.Solid(new Color(0.25f, 0.3f, 0.36f), 0.2f);
        var tooth = Mats.Solid(Color.white, 0.6f);

        Shapes.Part(PrimitiveType.Sphere, model, Vector3.zero, new Vector3(3.2f, 0.95f, 0.85f), skin);
        Shapes.Part(PrimitiveType.Sphere, model, new Vector3(0.15f, -0.17f, 0f), new Vector3(2.8f, 0.68f, 0.8f), belly);
        Shapes.Part(PrimitiveType.Sphere, model, new Vector3(1.2f, 0.02f, 0f), new Vector3(1.0f, 0.55f, 0.6f), skin);
        Shapes.Part(PrimitiveType.Sphere, model, new Vector3(1.15f, -0.2f, 0f), new Vector3(0.7f, 0.22f, 0.5f), mouth);
        for (int i = 0; i < 5; i++)
            Shapes.MeshPart(Shapes.Cone(), model, new Vector3(0.9f + i * 0.11f, -0.16f, -0.2f), new Vector3(0.06f, 0.12f, 0.06f), tooth, new Vector3(180f, 0f, 0f));

        jawPivot = Shapes.Pivot(model, new Vector3(0.7f, -0.22f, 0f), "Jaw");
        Shapes.Part(PrimitiveType.Sphere, jawPivot, new Vector3(0.45f, -0.04f, 0f), new Vector3(0.95f, 0.22f, 0.55f), belly);

        for (int s = -1; s <= 1; s += 2)
        {
            Shapes.Part(PrimitiveType.Sphere, model, new Vector3(1.15f, 0.1f, s * 0.27f), Vector3.one * 0.13f, dark);
            for (int i = 0; i < 3; i++)
                Shapes.Part(PrimitiveType.Cube, model, new Vector3(0.55f - i * 0.13f, -0.02f, s * 0.4f), new Vector3(0.03f, 0.32f, 0.03f), gill, new Vector3(0f, 0f, -10f));
            Shapes.MeshPart(Shapes.Fin(), model, new Vector3(0.45f, -0.3f, s * 0.3f), new Vector3(0.75f, 0.8f, 0.06f), skin, new Vector3(-s * 30f, 0f, 150f));
        }
        Shapes.MeshPart(Shapes.Fin(), model, new Vector3(0f, 0.38f, 0f), new Vector3(0.95f, 0.95f, 0.09f), skin);
        Shapes.MeshPart(Shapes.Fin(), model, new Vector3(-1.05f, 0.22f, 0f), new Vector3(0.3f, 0.28f, 0.05f), skin);

        tailPivot = Shapes.Pivot(model, new Vector3(-1.35f, 0.02f, 0f), "Tail");
        Shapes.Part(PrimitiveType.Sphere, tailPivot, new Vector3(-0.45f, 0.03f, 0f), new Vector3(1.3f, 0.42f, 0.34f), skin);
        Shapes.MeshPart(Shapes.Fin(), tailPivot, new Vector3(-1.0f, 0.05f, 0f), new Vector3(0.7f, 1.15f, 0.07f), skin, new Vector3(0f, 0f, 35f));
        Shapes.MeshPart(Shapes.Fin(), tailPivot, new Vector3(-1.0f, 0f, 0f), new Vector3(0.55f, 0.75f, 0.07f), skin, new Vector3(0f, 0f, 150f));

        trail = Fx.Make("Trail", tailPivot, new Vector3(-0.8f, 0f, 0f), Fx.BubbleMat);
        var main = trail.main;
        main.loop = true;
        main.startLifetime = Fx.Range(1f, 2f);
        main.startSpeed = Fx.Range(0.1f, 0.5f);
        main.startSize = Fx.Range(0.08f, 0.2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.15f;
        main.maxParticles = 200;
        var sh = trail.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
        trail.Play();
    }

    public void ResetShark(Vector3 p)
    {
        transform.position = p;
        Velocity = Vector2.zero;
        facing = 1f; yaw = 0f; deadRoll = 0f; biteTimer = 0f; hurtTimer = 0f;
        Boosting = false;
    }

    public void Bite() { biteTimer = 0.25f; }
    public void Hurt() { hurtTimer = 0.4f; }

    bool PointerWorld(out Vector3 wp)
    {
        wp = Vector3.zero;
        var cam = Camera.main;
        if (cam == null) return false;
        Ray r = cam.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.forward, Vector3.zero);
        float e;
        if (plane.Raycast(r, out e)) { wp = r.GetPoint(e); return true; }
        return false;
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || model == null) return;
        float dt = Time.deltaTime;
        Vector2 input = Vector2.zero;
        Boosting = false;

        if (gm.State == GameState.Playing)
        {
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude < 0.01f && Input.GetMouseButton(0))
            {
                Vector3 wp;
                if (PointerWorld(out wp))
                {
                    Vector2 d = (Vector2)(wp - transform.position);
                    float mag = d.magnitude;
                    if (mag > 0.4f)
                    {
                        input = d / mag * Mathf.Clamp01(mag / 2.5f);
                        if (mag > 7f) Boosting = true;
                    }
                }
            }
            if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) Boosting = true;
            if (input.sqrMagnitude > 1f) input.Normalize();
            if (input.sqrMagnitude < 0.01f) Boosting = false;
        }
        else if (gm.State == GameState.Title)
        {
            input = new Vector2(0f, Mathf.Sin(Time.time) * 0.1f);
        }

        if (gm.State == GameState.GameOver)
        {
            deadRoll = Mathf.MoveTowards(deadRoll, 180f, 120f * dt);
            Velocity = Vector2.MoveTowards(Velocity, new Vector2(0f, -0.8f), 4f * dt);
        }
        else
        {
            Vector2 target = input * (Boosting ? BoostSpeed : Speed);
            Velocity = Vector2.MoveTowards(Velocity, target, (Boosting ? Accel * 1.5f : Accel) * dt);
        }

        Vector3 p = transform.position + (Vector3)(Velocity * dt);
        float minY = World.FloorY + 0.9f, maxY = World.SurfaceY - 0.4f;
        if (p.y < minY) { p.y = minY; if (Velocity.y < 0f) Velocity.y = 0f; }
        if (p.y > maxY) { p.y = maxY; if (Velocity.y > 0f) Velocity.y = 0f; }
        p.z = 0f;
        transform.position = p;

        if (Mathf.Abs(Velocity.x) > 0.3f && gm.State != GameState.GameOver) facing = Mathf.Sign(Velocity.x);
        yaw = Mathf.MoveTowardsAngle(yaw, facing > 0f ? 0f : 180f, 600f * dt);
        float pitch = Mathf.Clamp(Mathf.Atan2(Velocity.y, Mathf.Max(Mathf.Abs(Velocity.x), 1.5f)) * Mathf.Rad2Deg, -55f, 55f);
        if (gm.State == GameState.GameOver) pitch = 0f;
        model.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, pitch) * Quaternion.Euler(deadRoll, 0f, 0f);

        hurtTimer = Mathf.Max(0f, hurtTimer - dt);
        model.localPosition = hurtTimer > 0f ? Random.insideUnitSphere * 0.12f * (hurtTimer / 0.4f) : Vector3.zero;

        float spd = Velocity.magnitude;
        if (gm.State != GameState.GameOver) tailPhase += dt * (3f + spd * 1.2f);
        tailPivot.localRotation = Quaternion.Euler(0f, Mathf.Sin(tailPhase) * (14f + spd * 2f), 0f);

        biteTimer = Mathf.Max(0f, biteTimer - dt);
        float open = biteTimer > 0f ? Mathf.Sin((1f - biteTimer / 0.25f) * Mathf.PI) : 0f;
        jawPivot.localRotation = Quaternion.Euler(0f, 0f, -28f * open);

        var em = trail.emission;
        em.rateOverTime = Boosting ? 40f : (gm.State == GameState.GameOver ? 0f : 3f);
    }
}
