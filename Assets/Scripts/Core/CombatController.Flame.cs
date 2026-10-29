using System.Collections.Generic;
using UnityEngine;

public partial class CombatController
{
    [Header("Slot 3 / Fire Breath")]
    [SerializeField, Min(1f)] protected internal float flameRange = 5f;
    [SerializeField, Min(1)] protected internal int flameDamage = 3;
    [SerializeField, Min(0.1f)] protected internal float flameInterval = 2f;
    protected internal const float FlameHalfAngle = 22f;
    protected internal const float FlameBurstDuration = 0.45f;
    protected internal ParticleSystem fireParticles;
    protected internal Transform flameMouth;
    protected internal Material fireMaterial;
    protected internal Material charMaterial;
    protected internal bool flameActive;
    protected internal float flameCooldown;
    protected internal float flameBurst;
    protected internal float flameTick;
    protected internal float flameEmit;
    protected internal Vector3 flameDirection = Vector3.forward;
    protected internal readonly Dictionary<Transform, Burn> burns = new Dictionary<Transform, Burn>();
    protected internal readonly HashSet<Transform> charred = new HashSet<Transform>();
    protected internal readonly List<GameObject> ash = new List<GameObject>();
    protected internal sealed class Burn { public float Until; public float NextTick; public float DamageRemainder; public bool HasSpread; }
    protected internal float FlameRange => equipment.Flame.Range(flameRange);

    protected internal void CreateFlamethrower()
    {
        fireMaterial = new Material(bulletMaterial) { name = "Crimson Fire Breath", color = new Color(0.95f, 0.008f, 0.018f) };
        fireMaterial.EnableKeyword("_EMISSION");
        fireMaterial.SetColor("_EmissionColor", new Color(1.8f, 0.005f, 0.015f));
        charMaterial = new Material(enemyMaterial) { name = "Charred Enemy", color = new Color(0.055f, 0.045f, 0.035f) };
        charMaterial.SetFloat("_Smoothness", 0f);
        flameMouth = CreateVisual(PrimitiveType.Cube, "Fire Breath Mouth", player.transform.position,
            new Vector3(0.18f, 0.10f, 0.025f), gripMaterial);
        fireParticles = new GameObject("Flame and Embers", typeof(ParticleSystem)).GetComponent<ParticleSystem>();
        fireParticles.transform.SetParent(transform, false);
        fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = fireParticles.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 600;
        main.startSize = 0.22f;
        var emission = fireParticles.emission;
        emission.enabled = false;
        var shape = fireParticles.shape;
        shape.enabled = false;
        var color = fireParticles.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.015f, 0.025f), 0f),
            new GradientColorKey(new Color(0.85f, 0.005f, 0.02f), 0.45f), new GradientColorKey(new Color(0.18f, 0f, 0.008f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var size = fireParticles.sizeOverLifetime;
        size.enabled = true;
        // A tight mouth source blooms into a rolling cloud, then collapses at the tip.
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.35f), new Keyframe(0.35f, 1.8f), new Keyframe(0.75f, 1.2f), new Keyframe(1f, 0f)));
        var renderer = fireParticles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = fireMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        renderer.mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
        sphere.SetActive(false);
        Destroy(sphere);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        fireParticles.Play();
    }

    protected internal void StopFlames()
    {
        flameActive = false;
        if (fireParticles != null) fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    protected internal void ClearFlameState()
    {
        StopFlames();
        flameCooldown = flameTick = flameEmit = 0f;
        burns.Clear();
        charred.Clear();
        foreach (GameObject remains in ash) if (remains != null) Destroy(remains);
        ash.Clear();
    }

    protected internal void DisposeFlamethrower()
    {
        if (fireMaterial != null) Destroy(fireMaterial);
        if (charMaterial != null) Destroy(charMaterial);
    }

    protected internal void UpdateFlamethrower(float dt)
    {
        flameCooldown -= dt;
        Vector3 origin = player.transform.position;
        if (equipment.HasWeapon(2) && !flameActive && flameCooldown <= 0f)
        {
            Transform target = null;
            float nearest = FlameRange * FlameRange;
            foreach (Transform enemy in enemies)
            {
                Vector3 offset = BossSurfacePoint(enemy,origin) - origin;
                offset.y = 0f;
                if (offset.sqrMagnitude > nearest) continue;
                nearest = offset.sqrMagnitude;
                target = enemy;
            }
            if (target != null)
            {
                flameDirection = target.position - origin;
                flameDirection.y = 0f;
                flameDirection = flameDirection.sqrMagnitude < 0.001f ? Vector3.forward : flameDirection.normalized;
                flameActive = true;
                GameAudio.Play(AudioCue.Flame);
                flameBurst = FlameBurstDuration;
                flameCooldown = Mathf.Max(FlameBurstDuration, equipment.Flame.Interval(flameInterval)*(1+0.15f*equipment.Stacks(RunItem.ConcentratedFuel)));
                flameTick = flameEmit = 0f;
                fireParticles.Play();
            }
        }
        float faceDistance = 0.5f / Mathf.Max(Mathf.Abs(flameDirection.x), Mathf.Abs(flameDirection.z));
        flameMouth.SetPositionAndRotation(origin + Vector3.up * 0.12f + flameDirection * (faceDistance + 0.015f),
            Quaternion.LookRotation(flameDirection));
        if (playerAvatar != null)
        {
            flameMouth.gameObject.SetActive(false);
            if (flameActive) playerAvatar.Face(flameDirection);
            flameMouth.position = playerAvatar.MouthPosition;
        }
        if (flameActive)
        {
            float activeDelta = Mathf.Min(dt, flameBurst);
            flameEmit -= activeDelta;
            int emitted = 0;
            while (flameEmit <= 0f && emitted++ < 24)
            {
                EmitFlame(origin);
                float progress = 1f - flameBurst / FlameBurstDuration;
                flameEmit += 1f / Mathf.Lerp(240f, 90f, progress);
            }
            flameTick -= activeDelta;
            while (flameTick <= 0f)
            {
                Vector3 right = Vector3.Cross(Vector3.up, flameDirection);
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    Transform enemy = enemies[i];
                    Vector3 offset = BossSurfacePoint(enemy,origin) - origin;
                    offset.y = 0f;
                    if (obstacles != null && !obstacles.Clear(player.transform.position, enemies[i].position)) continue;
                    if (!FlameHit.Contains(Vector3.Dot(offset, flameDirection), Vector3.Dot(offset, right), FlameRange, FlameHalfAngle)) continue;
                    Ignite(enemy,true);
                    DamageEnemy(i, equipment.Flame.Damage(flameDamage),HitKind.Flame);
                }
                flameTick += FlameBurstDuration / 6f;
            }
            flameBurst -= activeDelta;
            if (flameBurst <= 0f) flameActive = false;
        }
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Transform enemy = enemies[i];
            if (!burns.TryGetValue(enemy, out Burn burn)) continue;
            if (Time.time > burn.Until) { burns.Remove(enemy); continue; }
            if (Time.time < burn.NextTick) continue;
            burn.NextTick = Time.time + 0.5f;
            fireParticles.Emit(new ParticleSystem.EmitParams { position = enemy.position + Vector3.up * 0.3f,
                velocity = Vector3.up * 0.7f, startLifetime = 0.4f, startSize = 0.1f }, 3);
            DamageEnemy(i, 2, HitKind.Burn);
        }
        ash.RemoveAll(remains => remains == null);
    }

    protected internal void EmitFlame(Vector3 origin)
    {
        Vector3 direction = Quaternion.Euler(0f, Random.Range(-FlameHalfAngle, FlameHalfAngle), 0f) * flameDirection;
        const float maxRadius = 0.45f;
        Vector3 mouth = flameMouth.position;
        // Allow for the growing cloud's radius so the red breath still has a finite reach.
        float distance = Mathf.Min(FlameRange - Vector3.Distance(origin, mouth) - maxRadius,
            DistanceToBorder(mouth, direction) - maxRadius);
        if (distance <= 0f) return;
        float speed = Random.Range(11f, 14f);
        fireParticles.Emit(new ParticleSystem.EmitParams { position = mouth,
            velocity = direction * speed, startLifetime = distance / speed,
            startSize = Random.Range(0.24f, 0.48f) }, 1);
    }

    protected internal void LeaveCharredRemains(Transform enemy)
    {
        burns.Remove(enemy);
        if (!charred.Remove(enemy)) return;
        Transform remains = CreateVisual(PrimitiveType.Cube, "Charred Remains", new Vector3(enemy.position.x, 0.09f, enemy.position.z),
            new Vector3(0.65f, 0.18f, 0.65f), charMaterial);
        ash.Add(remains.gameObject);
        Destroy(remains.gameObject, 2f);
    }
}
