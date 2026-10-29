using UnityEngine;

/// <summary>Squash and stretch only the rendered child; gameplay roots never change scale.</summary>
public sealed class AvatarPulse
{
    private readonly Transform visual;
    private readonly Vector3 baseScale;
    private readonly Vector3 basePosition;
    private readonly float bottomOffset;
    private readonly float phase;

    public AvatarPulse(Transform visual, Bounds bounds, float phase = 0f)
    {
        this.visual = visual;
        baseScale = visual.localScale;
        basePosition = visual.localPosition;
        bottomOffset = (bounds.min.y - visual.position.y) / visual.parent.lossyScale.y;
        this.phase = phase;
    }

    public void Apply(float time)
    {
        if (visual == null) return;
        // A 0.65-second breathing/bouncing cycle, with the feet anchored to the floor.
        float wave = Mathf.Sin(time * (Mathf.PI * 2f / 0.65f) + phase);
        float width = 1f + wave * 0.12f;
        float height = 1f - wave * 0.18f;
        visual.localScale = Vector3.Scale(baseScale, new Vector3(width, height, width));
        visual.localPosition = basePosition + Vector3.up * (bottomOffset * (1f - height));
    }
}
