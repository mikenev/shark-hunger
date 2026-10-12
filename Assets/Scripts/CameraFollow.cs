using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public SharkController shark;
    public Camera cam;
    public Color shallow = new Color(0.12f, 0.5f, 0.64f);
    public Color deep = new Color(0.02f, 0.1f, 0.22f);
    Vector3 vel;
    const float Z = -17f;

    Vector3 Desired()
    {
        Vector3 p = shark.transform.position;
        var d = new Vector3(p.x + shark.Velocity.x * 0.35f, p.y + 1f, Z);
        d.y = Mathf.Clamp(d.y, World.FloorY + 7.5f, World.SurfaceY - 4.5f);
        return d;
    }

    public void Snap()
    {
        if (shark == null) return;
        transform.position = Desired();
        vel = Vector3.zero;
        UpdateColors();
    }

    void LateUpdate()
    {
        if (shark == null) return;
        transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref vel, 0.25f);
        UpdateColors();
    }

    void UpdateColors()
    {
        float t = Mathf.InverseLerp(World.SurfaceY - 4.5f, World.FloorY + 7.5f, transform.position.y);
        Color c = Color.Lerp(shallow, deep, t * 0.85f);
        if (cam != null) cam.backgroundColor = c;
        RenderSettings.fogColor = c;
    }
}
