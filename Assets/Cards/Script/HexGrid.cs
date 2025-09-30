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

   
    public static Vector2Int WorldToAxial(Vector3 worldPos, float radius, Vector3 origin)
    {
        // coords relativas al origen
        float x = worldPos.x - origin.x;
        float z = worldPos.z - origin.z;

        // inversa del flat-top
        float qf = (2f / 3f) * x / radius;
        float rf = (-1f / 3f) * x / radius + (1f / SQRT3) * z / radius;

        // redondeo cúbico correcto
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

   
    public static Vector3[] GetHexCorners(Vector3 center, float radius)
    {
        var corners = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60f * i; 
            float rad = angleDeg * Mathf.Deg2Rad;
            corners[i] = center + new Vector3(radius * Mathf.Cos(rad), 0f, radius * Mathf.Sin(rad));
        }
        return corners;
    }
}
