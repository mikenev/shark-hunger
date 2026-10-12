using System.Collections.Generic;
using UnityEngine;

public enum GameState { Title, Playing, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager I;
    public const float MaxHunger = 100f;

    public GameState State = GameState.Title;
    public SharkController Shark;
    public Spawner Spawner;
    public CameraFollow CamFollow;
    public Camera Cam;

    public float Hunger = MaxHunger;
    public float Elapsed;
    public int Points, Eaten, Best;
    bool newBest;
    float damageFlash, eatFlash, overTimer;
    ParticleSystem burst;

    class Popup { public Vector3 pos; public string text; public Color color; public float t; }
    readonly List<Popup> popups = new List<Popup>();

    GUIStyle big, med, small, hud, btn, pop;
    float styleScale = -1f;

    public Vector2 SharkPos { get { return Shark != null ? (Vector2)Shark.transform.position : Vector2.zero; } }
    public bool Playing { get { return State == GameState.Playing; } }
    public float DrainRate { get { return Mathf.Min(3.5f + Elapsed * 0.03f, 10f) * (Shark != null && Shark.Boosting ? 1.6f : 1f); } }
    public int Score { get { return Points + Mathf.FloorToInt(Elapsed) * 2; } }

    void Awake()
    {
        I = this;
        Best = PlayerPrefs.GetInt("SharkHunger_Best", 0);
    }

    public void Init(SharkController shark, Spawner spawner, CameraFollow follow, Camera cam)
    {
        Shark = shark; Spawner = spawner; CamFollow = follow; Cam = cam;
        burst = Fx.Make("EatBurst", transform, Vector3.zero, Fx.SoftMat);
        var main = burst.main;
        main.loop = true;
        main.startLifetime = Fx.Range(0.3f, 0.7f);
        main.startSpeed = Fx.Range(1.5f, 5f);
        main.startSize = Fx.Range(0.08f, 0.28f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        var em = burst.emission; em.rateOverTime = 0f;
        var sh = burst.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
        burst.Play();
        Sfx.Init(gameObject);
        Spawner.Populate(SharkPos, 16, 0f);
    }

    bool AnyStartKey()
    {
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.R);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        switch (State)
        {
            case GameState.Title:
                if (AnyStartKey()) StartGame();
                break;
            case GameState.Playing:
                Elapsed += dt;
                Hunger -= DrainRate * dt;
                CheckEating();
                if (Hunger <= 0f) { Hunger = 0f; GameOver(); }
                break;
            case GameState.GameOver:
                overTimer += dt;
                if (overTimer > 0.75f && AnyStartKey()) StartGame();
                break;
        }
        if (Spawner != null && Shark != null) Spawner.Tick(Elapsed, SharkPos, Shark.Velocity);
        damageFlash = Mathf.MoveTowards(damageFlash, 0f, dt * 2.5f);
        eatFlash = Mathf.MoveTowards(eatFlash, 0f, dt * 3f);
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            popups[i].t += dt;
            popups[i].pos += Vector3.up * dt * 1.2f;
            if (popups[i].t > 1f) popups.RemoveAt(i);
        }
    }

    void StartGame()
    {
        Spawner.Clear();
        Shark.ResetShark(new Vector3(0f, -10f, 0f));
        CamFollow.Snap();
        Hunger = MaxHunger;
        Elapsed = 0f; Points = 0; Eaten = 0;
        newBest = false;
        popups.Clear();
        State = GameState.Playing;
        Spawner.Populate(SharkPos, 16, 0f);
        Sfx.Play(Sfx.StartClip, 0.6f);
    }

    void GameOver()
    {
        State = GameState.GameOver;
        overTimer = 0f;
        int sc = Score;
        if (sc > Best)
        {
            Best = sc; newBest = true;
            PlayerPrefs.SetInt("SharkHunger_Best", Best);
            PlayerPrefs.Save();
        }
        Sfx.Play(Sfx.Over, 0.8f);
    }

    void CheckEating()
    {
        Vector3 mouth = Shark.MouthPos;
        var list = Spawner.Active;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var c = list[i];
            if (c == null) continue;
            Vector2 d = (Vector2)(c.Center - mouth);
            float r = c.Radius + Shark.BiteRadius;
            if (d.sqrMagnitude < r * r) Eat(c);
        }
    }

    void Eat(Creature c)
    {
        float h = c.HungerValue;
        int pts = c.ScoreValue;
        Vector3 p = c.Center;
        Hunger = Mathf.Clamp(Hunger + h, 0f, MaxHunger);
        Points += pts;
        Eaten++;
        Shark.Bite();
        var ep = new ParticleSystem.EmitParams();
        ep.position = p;
        ep.applyShapeToPosition = true;
        if (h < 0f)
        {
            damageFlash = 1f;
            Shark.Hurt();
            AddPopup(p, (c.info.kind == CreatureKind.Jellyfish ? "STING! " : "OUCH! ") + Mathf.RoundToInt(h), new Color(1f, 0.35f, 0.85f));
            ep.startColor = new Color(1f, 0.4f, 0.9f, 1f);
            burst.Emit(ep, 20);
            Sfx.Play(Sfx.Sting, 0.7f);
        }
        else
        {
            eatFlash = 1f;
            AddPopup(p, "+" + pts, c.info.kind == CreatureKind.GoldenFish ? new Color(1f, 0.85f, 0.2f) : Color.white);
            ep.startColor = new Color(1f, 0.85f, 0.8f, 1f);
            burst.Emit(ep, 12);
            ep.startColor = new Color(0.9f, 0.15f, 0.15f, 1f);
            burst.Emit(ep, 6);
            Sfx.Play(Sfx.Chomp, 0.8f, Mathf.Clamp(1.4f - c.info.radius * 0.6f, 0.8f, 1.3f));
        }
        Spawner.Remove(c);
    }

    void AddPopup(Vector3 pos, string text, Color color)
    {
        popups.Add(new Popup { pos = pos + Vector3.up * 0.5f, text = text, color = color, t = 0f });
    }

    static string FormatTime(float t)
    {
        int s = Mathf.FloorToInt(t);
        return string.Format("{0}:{1:00}", s / 60, s % 60);
    }

    // ---------- UI (IMGUI: works out of the box in WebGL, no UI packages needed) ----------

    void Styles()
    {
        float s = Mathf.Max(0.5f, Mathf.Min(Screen.height / 720f, Screen.width / 1000f));
        if (big != null && Mathf.Approximately(s, styleScale)) return;
        styleScale = s;
        big = MakeStyle(GUI.skin.label, 64, TextAnchor.MiddleCenter);
        med = MakeStyle(GUI.skin.label, 28, TextAnchor.MiddleCenter);
        small = MakeStyle(GUI.skin.label, 19, TextAnchor.UpperLeft);
        small.fontStyle = FontStyle.Normal;
        hud = MakeStyle(GUI.skin.label, 24, TextAnchor.MiddleCenter);
        pop = MakeStyle(GUI.skin.label, 26, TextAnchor.MiddleCenter);
        btn = MakeStyle(GUI.skin.button, 30, TextAnchor.MiddleCenter);
    }

    GUIStyle MakeStyle(GUIStyle baseStyle, int size, TextAnchor anchor)
    {
        var st = new GUIStyle(baseStyle);
        st.fontSize = Mathf.Max(10, Mathf.RoundToInt(size * styleScale));
        st.fontStyle = FontStyle.Bold;
        st.alignment = anchor;
        st.richText = true;
        st.wordWrap = false;
        st.normal.textColor = Color.white;
        return st;
    }

    static void Fill(Rect r, Color c)
    {
        var o = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = o;
    }

    static void Shadow(Rect r, string text, GUIStyle st, Color c)
    {
        var o = st.normal.textColor;
        st.normal.textColor = new Color(0f, 0f, 0f, 0.75f * c.a);
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, st);
        st.normal.textColor = c;
        GUI.Label(r, text, st);
        st.normal.textColor = o;
    }

    void OnGUI()
    {
        Styles();
        float s = styleScale;
        float W = Screen.width, H = Screen.height;

        if (damageFlash > 0f) Fill(new Rect(0f, 0f, W, H), new Color(0.8f, 0.1f, 0.5f, damageFlash * 0.35f));

        if (Cam != null)
        {
            foreach (var p in popups)
            {
                Vector3 sp = Cam.WorldToScreenPoint(p.pos);
                if (sp.z <= 0f) continue;
                var c = p.color; c.a = 1f - p.t;
                Shadow(new Rect(sp.x - 120f * s, H - sp.y - 20f * s, 240f * s, 40f * s), p.text, pop, c);
            }
        }

        if (State != GameState.Title) DrawHud(s, W, H);
        if (State == GameState.Title) DrawTitle(s, W, H);
        else if (State == GameState.GameOver) DrawGameOver(s, W, H);
    }

    void DrawHud(float s, float W, float H)
    {
        float bw = Mathf.Min(420f * s, W * 0.5f), bh = 34f * s;
        var bar = new Rect(20f * s, 20f * s, bw, bh);
        Fill(new Rect(bar.x - 3f * s, bar.y - 3f * s, bar.width + 6f * s, bar.height + 6f * s), new Color(0f, 0f, 0f, 0.55f));
        float f = Mathf.Clamp01(Hunger / MaxHunger);
        Color c = f > 0.5f ? Color.Lerp(Color.yellow, new Color(0.2f, 0.9f, 0.3f), (f - 0.5f) * 2f)
                           : Color.Lerp(new Color(0.95f, 0.15f, 0.1f), Color.yellow, f * 2f);
        if (f < 0.25f && Playing) c = Color.Lerp(c, Color.white, Mathf.PingPong(Time.time * 4f, 1f) * 0.6f);
        Fill(new Rect(bar.x, bar.y, bar.width * f, bar.height), c);
        if (eatFlash > 0f) Fill(new Rect(bar.x, bar.y, bar.width * f, bar.height), new Color(1f, 1f, 1f, eatFlash * 0.5f));
        Shadow(bar, "HUNGER  " + Mathf.CeilToInt(Hunger), hud, Color.white);

        hud.alignment = TextAnchor.UpperRight;
        Shadow(new Rect(W - 340f * s, 14f * s, 320f * s, 36f * s), "SCORE  " + Score, hud, Color.white);
        Shadow(new Rect(W - 340f * s, 48f * s, 320f * s, 36f * s), "TIME " + FormatTime(Elapsed) + "   EATEN " + Eaten, hud, new Color(0.85f, 0.95f, 1f));
        hud.alignment = TextAnchor.MiddleCenter;

        if (Playing && f < 0.2f)
            Shadow(new Rect(0f, H * 0.16f, W, 50f * s), "STARVING! EAT SOMETHING!", med, new Color(1f, 0.3f, 0.25f, 0.5f + 0.5f * Mathf.PingPong(Time.time * 3f, 1f)));
        if (Playing && Elapsed < 8f)
            Shadow(new Rect(20f * s, H - 40f * s, W, 30f * s), "Boost: Space / Shift (or steer far with mouse/touch) - boosting burns hunger faster", small, new Color(1f, 1f, 1f, 0.85f));
    }

    void DrawTitle(float s, float W, float H)
    {
        Fill(new Rect(0f, 0f, W, H), new Color(0f, 0.05f, 0.12f, 0.55f));
        float y = H * 0.05f;
        Shadow(new Rect(0f, y, W, 80f * s), "SHARK HUNGER", big, new Color(1f, 0.9f, 0.5f));
        y += 80f * s;
        Shadow(new Rect(0f, y, W, 40f * s), "Eat to survive. Don't let your hunger reach zero!", med, Color.white);
        y += 56f * s;

        float colW = Mathf.Min(470f * s, W * 0.46f), rowH = 56f * s;
        float x0 = W * 0.5f - colW - 8f * s;
        var all = CreatureDB.All;
        for (int i = 0; i < all.Length; i++)
        {
            var info = all[i];
            int col = i % 2, row = i / 2;
            var r = new Rect(x0 + col * (colW + 16f * s), y + row * (rowH + 6f * s), colW, rowH);
            Fill(r, new Color(0f, 0f, 0f, 0.4f));
            Fill(new Rect(r.x, r.y, 8f * s, r.height), info.color);
            string val = info.hunger >= 0 ? "+" + info.hunger + " food" : info.hunger + " food";
            string valCol = info.hunger >= 0 ? "#9CFF9C" : "#FF7AD9";
            GUI.Label(new Rect(r.x + 16f * s, r.y + 3f * s, r.width - 20f * s, 28f * s),
                "<b>" + info.name + "</b>   <color=" + valCol + ">" + val + "</color>   <color=#FFE27A>" + info.score + " pts</color>", small);
            GUI.Label(new Rect(r.x + 16f * s, r.y + 28f * s, r.width - 20f * s, 26f * s), "<color=#C8DCF0>" + info.note + "</color>", small);
        }
        y += 4 * (rowH + 6f * s) + 12f * s;
        Shadow(new Rect(0f, y, W, 30f * s), "Move: WASD / Arrow keys, or hold mouse / touch to steer   |   Boost: Space / Shift", hud, new Color(0.85f, 0.95f, 1f));
        y += 44f * s;
        if (GUI.Button(new Rect(W * 0.5f - 140f * s, y, 280f * s, 64f * s), "PLAY", btn)) StartGame();
        y += 72f * s;
        Shadow(new Rect(0f, y, W, 30f * s), "or press Space / Enter" + (Best > 0 ? "      Best score: " + Best : ""), hud, new Color(1f, 1f, 1f, 0.8f));
    }

    void DrawGameOver(float s, float W, float H)
    {
        Fill(new Rect(0f, 0f, W, H), new Color(0.05f, 0f, 0.02f, Mathf.Clamp01(overTimer * 2f) * 0.6f));
        float y = H * 0.18f;
        Shadow(new Rect(0f, y, W, 80f * s), "YOU STARVED!", big, new Color(1f, 0.35f, 0.3f));
        y += 90f * s;
        Shadow(new Rect(0f, y, W, 44f * s), "Score: " + Score, med, Color.white);
        y += 44f * s;
        Shadow(new Rect(0f, y, W, 40f * s), "Survived " + FormatTime(Elapsed) + "   |   Creatures eaten: " + Eaten, hud, new Color(0.85f, 0.95f, 1f));
        y += 40f * s;
        Shadow(new Rect(0f, y, W, 40f * s), newBest ? "NEW BEST SCORE!" : "Best: " + Best, hud, newBest ? new Color(1f, 0.85f, 0.2f) : Color.white);
        y += 60f * s;
        if (overTimer > 0.5f)
        {
            if (GUI.Button(new Rect(W * 0.5f - 160f * s, y, 320f * s, 64f * s), "PLAY AGAIN", btn)) StartGame();
            y += 74f * s;
            Shadow(new Rect(0f, y, W, 30f * s), "or press R / Space / Enter", hud, new Color(1f, 1f, 1f, 0.8f));
        }
    }
}
