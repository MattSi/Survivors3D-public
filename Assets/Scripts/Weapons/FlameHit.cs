using System;

public static class FlameHit
{
    public static bool Contains(float forward, float sideways, float range, float halfAngleDegrees)
    {
        // A finite sector: no damage behind the nozzle or past the visible flame tip.
        return forward >= 0f && forward * forward + sideways * sideways <= range * range
            && Math.Abs(sideways) <= forward * Math.Tan(halfAngleDegrees * Math.PI / 180);
    }
}
