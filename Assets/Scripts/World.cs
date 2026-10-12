using UnityEngine;

public static class World
{
    public const float SurfaceY = 0f;
    public const float FloorY = -26f;
}

public class Sway : MonoBehaviour
{
    public float amp = 8f, speed = 1f, offset;
    Quaternion baseRot;
    void Start() { baseRot = transform.localRotation; }
    void Update() { transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * speed + offset) * amp); }
}
