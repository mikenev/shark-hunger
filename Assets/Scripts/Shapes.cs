using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class Shapes
{
    // Referencing collider types keeps them from being stripped in WebGL builds
    // (CreatePrimitive adds colliders internally; we only use it once per type to grab the mesh).
    static readonly System.Type[] KeepTypes = { typeof(SphereCollider), typeof(BoxCollider), typeof(CapsuleCollider), typeof(MeshCollider) };
    static readonly Dictionary<PrimitiveType, Mesh> primMeshes = new Dictionary<PrimitiveType, Mesh>();

    public static Mesh PrimitiveMesh(PrimitiveType type)
    {
        Mesh m;
        if (!primMeshes.TryGetValue(type, out m) || m == null)
        {
            var tmp = GameObject.CreatePrimitive(type);
            m = tmp.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.Destroy(tmp);
            primMeshes[type] = m;
        }
        return m;
    }

    public static GameObject Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default(Vector3))
    {
        var go = MeshPart(PrimitiveMesh(type), parent, pos, scale, mat, euler);
        go.name = type.ToString();
        return go;
    }

    public static GameObject MeshPart(Mesh mesh, Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default(Vector3))
    {
        var go = new GameObject(mesh.name);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        var t = go.transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        t.localEulerAngles = euler;
        t.localScale = scale;
        return go;
    }

    public static Transform Pivot(Transform parent, Vector3 pos, string name = "Pivot")
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        return t;
    }

    public static void NoShadows(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
    }

    static Mesh fin, cone;

    // Triangular fin prism: base from x=-0.5..0.5 at y=0, apex swept back at (-0.25, 1).
    public static Mesh Fin()
    {
        if (fin != null) return fin;
        Vector3[] tri = { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(-0.25f, 1f, 0f) };
        Vector3 off = new Vector3(0f, 0f, 0.5f);
        var faces = new List<Vector3[]>();
        faces.Add(new[] { tri[0] - off, tri[1] - off, tri[2] - off });
        faces.Add(new[] { tri[0] + off, tri[1] + off, tri[2] + off });
        for (int i = 0; i < 3; i++)
        {
            int j = (i + 1) % 3;
            faces.Add(new[] { tri[i] - off, tri[j] - off, tri[j] + off, tri[i] + off });
        }
        fin = BuildConvex(faces, "Fin");
        return fin;
    }

    // Cone: base radius 0.5 at y=0, apex at y=1.
    public static Mesh Cone()
    {
        if (cone != null) return cone;
        int seg = 10;
        var faces = new List<Vector3[]>();
        var b = new Vector3[seg];
        for (int i = 0; i < seg; i++)
        {
            float a = i * Mathf.PI * 2f / seg;
            b[i] = new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f);
        }
        faces.Add(b);
        var apex = new Vector3(0f, 1f, 0f);
        for (int i = 0; i < seg; i++) faces.Add(new[] { b[i], b[(i + 1) % seg], apex });
        cone = BuildConvex(faces, "Cone");
        return cone;
    }

    static Mesh BuildConvex(List<Vector3[]> faces, string name)
    {
        Vector3 center = Vector3.zero; int count = 0;
        foreach (var f in faces) foreach (var p in f) { center += p; count++; }
        center /= count;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        foreach (var f in faces)
        {
            Vector3 fc = Vector3.zero;
            foreach (var p in f) fc += p;
            fc /= f.Length;
            Vector3 n = Vector3.Cross(f[1] - f[0], f[2] - f[0]);
            bool flip = Vector3.Dot(n, fc - center) < 0f;
            int start = verts.Count;
            verts.AddRange(f);
            for (int i = 1; i < f.Length - 1; i++)
            {
                tris.Add(start);
                if (!flip) { tris.Add(start + i); tris.Add(start + i + 1); }
                else { tris.Add(start + i + 1); tris.Add(start + i); }
            }
        }
        var m = new Mesh();
        m.name = name;
        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
}
