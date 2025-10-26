using UnityEngine;

public static class HexGridFlat
{
    const float SQRT3 = 1.73205080757f;

    // Axial (q,r) -> mundo (XZ), FLAT-TOP
    public static Vector3 AxialToWorld(Vector2Int axial, float radius, Vector3 origin, float y = 0f)
    {
        int q = axial.x;
        int r = axial.y;
        float x = radius * 1.5f * q;
        float z = radius * SQRT3 * (r + q * 0.5f);
        return new Vector3(origin.x + x, y, origin.z + z);
    }

    // Mundo (XZ) -> Axial (q,r), FLAT-TOP
    public static Vector2Int WorldToAxial(Vector3 worldPos, float radius, Vector3 origin)
    {
        // coords relativas al origen
        float x = worldPos.x - origin.x;
        float z = worldPos.z - origin.z;

        // inversa del flat-top
        float qf = (2f / 3f) * x / radius;
        float rf = (-1f / 3f) * x / radius + (1f / SQRT3) * z / radius;

        // redondeo c�bico correcto
        float xf = qf, zf = rf, yf = -xf - zf;
        int rx = Mathf.RoundToInt(xf);
        int ry = Mathf.RoundToInt(yf);
        int rz = Mathf.RoundToInt(zf);

        float x_diff = Mathf.Abs(rx - xf);
        float y_diff = Mathf.Abs(ry - yf);
        float z_diff = Mathf.Abs(rz - zf);

        if (x_diff > y_diff && x_diff > z_diff) rx = -ry - rz;
        else if (y_diff > z_diff) ry = -rx - rz;
        else rz = -rx - ry;

        return new Vector2Int(rx, rz);
    }

    // Devuelve los 6 v�rtices de un hex�gono flat-top
    public static Vector3[] GetHexCorners(Vector3 center, float radius)
    {
        var corners = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            // flat-top empieza en 30�
            float angleDeg = 60f * i ;
            float rad = angleDeg * Mathf.Deg2Rad;
            corners[i] = center + new Vector3(radius * Mathf.Cos(rad), 0f, radius * Mathf.Sin(rad));
        }
        return corners;
    }

    // Construye un mesh de hex�gono (relleno s�lido)
    public static Mesh BuildHexMesh(Vector3 center, float radius)
    {
        Vector3[] corners = GetHexCorners(center, radius);

        Mesh mesh = new Mesh();

        // 7 v�rtices: centro + 6 esquinas
        Vector3[] verts = new Vector3[7];
        verts[0] = center;
        for (int i = 0; i < 6; i++)
            verts[i + 1] = corners[i];

        // Tri�ngulos (6 alrededor del centro)
        int[] tris = new int[18];
        for (int i = 0; i < 6; i++)
        {
            tris[i * 3] = 0;               // centro
            tris[i * 3 + 1] = i + 1;       // esquina actual
            tris[i * 3 + 2] = (i == 5 ? 1 : i + 2); // siguiente esquina
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}
