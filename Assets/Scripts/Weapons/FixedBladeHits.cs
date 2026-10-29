public static class FixedBladeHits
{
    public static bool InSweptArc(float x, float z, float range, float halfSize, float aim, float from, float to)
    {
        double distance = System.Math.Sqrt(x * x + z * z);
        double radius = halfSize * System.Math.Sqrt(2);
        if (distance > range + radius) return false;
        if (distance <= radius) return true;
        double angle = System.Math.Atan2(x, z) * 180 / System.Math.PI - aim;
        angle = (angle % 360 + 540) % 360 - 180;
        double padding = System.Math.Asin(System.Math.Min(1, (radius + 0.15) / distance)) * 180 / System.Math.PI;
        return angle + padding >= from && angle - padding <= to;
    }
}
