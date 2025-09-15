using UnityEngine;

public static class HexGrid
{
    // Convenciones: hexagono "pointy top" (puntas arriba/abajo)
    // Fórmulas basadas en red axial (q, r)
    // x = world.x, z = world.z (usamos plano XZ)

    public static Vector2Int WorldToAxial(Vector3 worldPos, float radius)
    {
        float qf = (Mathf.Sqrt(3f) / 3f * worldPos.x - 1f / 3f * worldPos.z) / radius;
        float rf = (2f / 3f * worldPos.z) / radius;

        // Pasar a coordenadas cubo para redondeo correcto
        float xf = qf;
        float zf = rf;
        float yf = -xf - zf;

        int rx = Mathf.RoundToInt(xf);
        int ry = Mathf.RoundToInt(yf);
        int rz = Mathf.RoundToInt(zf);

        float x_diff = Mathf.Abs(rx - xf);
        float y_diff = Mathf.Abs(ry - yf);
        float z_diff = Mathf.Abs(rz - zf);

        if (x_diff > y_diff && x_diff > z_diff)
            rx = -ry - rz;
        else if (y_diff > z_diff)
            ry = -rx - rz;
        else
            rz = -rx - ry;

        // axial: q = x, r = z
        return new Vector2Int(rx, rz);
    }

    public static Vector3 AxialToWorld(Vector2Int axial, float radius, float y = 0f)
    {
        int q = axial.x;
        int r = axial.y;

        float x = radius * Mathf.Sqrt(3f) * (q + r * 0.5f);
        float z = radius * 1.5f * r;

        return new Vector3(x, y, z);
    }

    // Devuelve los 6 vértices (en mundo) del hex centrado en "center"
    public static Vector3[] GetHexCorners(Vector3 center, float radius)
    {
        Vector3[] corners = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60f * i - 30f; // pointy-top
            float angleRad = Mathf.Deg2Rad * angleDeg;
            corners[i] = center + new Vector3(radius * Mathf.Cos(angleRad), 0f, radius * Mathf.Sin(angleRad));
        }
        return corners;
    }
}
