using System.Collections.Generic;
using UnityEngine;

/// <summary>Displays the Blender model without changing the gameplay collider or weapon roots.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class PlayerAvatar : MonoBehaviour
{
    private Transform visual;
    private AvatarPulse pulse;
    private Transform mouth;
    private Renderer[] bodies;
    private readonly List<Material> materials = new List<Material>();
    private Renderer originalBody;
    private Quaternion facingCorrection;
    private MaterialPropertyBlock properties;
    private Vector3 restPosition, restScale;
    private float footOffset, celebrationAge;
    private bool celebrating, hitTint,dashTint;
    public float CameraOpacity { get; private set; } = 1f;
    public float DistanceToVisual(Vector3 point) => bodies!=null && bodies.Length>0 ? Vector3.Distance(ModelBounds().ClosestPoint(point),point) : 10f;
    private Quaternion celebrationStart, celebrationFacing;
    private sealed class ArmPose
    {
        public Transform Part;
        public Vector3 Position, Pivot;
        public Quaternion Rotation;
        public float Sign;
    }
    private readonly List<ArmPose> arms = new List<ArmPose>();
    public Vector3 MouthPosition => mouth != null ? mouth.position : transform.position + Vector3.up * 0.45f;

    private void OnEnable() => CreateVisual();
    private void Update()
    {
        // Also handles the first editor import, when the FBX is not ready at OnEnable.
        if (visual == null) CreateVisual();
        if (Application.isPlaying && pulse != null && !celebrating) pulse.Apply(Time.time);
    }

    private void LateUpdate()
    {
        if (!celebrating || visual == null) return;
        float turn = Mathf.SmoothStep(0, 1, celebrationAge / 0.3f);
        float jump = Mathf.Clamp01((celebrationAge - 0.3f) / 0.7f);
        float landing = Mathf.Clamp01((celebrationAge - 1f) / 0.5f);
        float height = 1f - 0.22f * Mathf.Sin(landing * Mathf.PI * 2f) * (1f - landing);
        visual.localScale = Vector3.Scale(restScale, new Vector3(1f / Mathf.Sqrt(height), height, 1f / Mathf.Sqrt(height)));
        visual.localPosition = restPosition + Vector3.up * (Mathf.Sin(jump * Mathf.PI) * 1.15f + footOffset * (1f - height));
        visual.rotation = Quaternion.AngleAxis(360f * Mathf.SmoothStep(0, 1, jump), Vector3.up)
            * Quaternion.Slerp(celebrationStart, celebrationFacing, turn);
        float raise = Mathf.SmoothStep(0, 1, (celebrationAge - 1f) / 0.35f);
        Vector3 axis = Quaternion.Inverse(facingCorrection) * Vector3.forward;
        foreach (ArmPose arm in arms)
        {
            Quaternion rotation = Quaternion.AngleAxis(arm.Sign * 145f * raise, axis);
            arm.Part.position = visual.TransformPoint(arm.Pivot + rotation * (arm.Position - arm.Pivot));
            arm.Part.rotation = visual.rotation * rotation * arm.Rotation;
        }
    }

    public void BeginCelebration(Vector3 cameraDirection)
    {
        if (visual == null) return;
        celebrating = true;
        celebrationAge = 0;
        celebrationStart = visual.rotation;
        cameraDirection.y = 0;
        celebrationFacing = cameraDirection.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(cameraDirection) * facingCorrection : celebrationStart;
    }

    public void SetCelebrationTime(float age) => celebrationAge = age;

    public void EndCelebration()
    {
        if (!celebrating || visual == null) return;
        celebrating = false;
        visual.localPosition = restPosition;
        visual.localScale = restScale;
        visual.rotation = celebrationStart;
        foreach (ArmPose arm in arms)
        {
            arm.Part.position = visual.TransformPoint(arm.Position);
            arm.Part.rotation = visual.rotation * arm.Rotation;
        }
    }

    private void CreateVisual()
    {
        if (visual != null) return;
        GameObject model = Resources.Load<GameObject>("Characters/BlockHero");
        if (model == null) return;
        originalBody = GetComponent<Renderer>();
        visual = Instantiate(model, transform, false).transform;
        visual.name = "Block Hero (Blender)";
        foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.hideFlags = HideFlags.DontSave;
            if (child.name == "MouthAnchor") mouth = child;
        }
        bodies = visual.GetComponentsInChildren<Renderer>();
        var palette = new Dictionary<string, Material>();
        foreach (Renderer body in bodies)
        {
            Material[] slots = body.sharedMaterials;
            for (int i = 0; i < slots.Length; i++)
            {
                string name = slots[i] != null ? slots[i].name : "Hero_Tunic";
                if (!palette.TryGetValue(name, out Material material))
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                    {
                        name = name, color = PaletteColor(name), hideFlags = HideFlags.HideAndDontSave
                    };
                    material.SetFloat("_Smoothness", 0.15f);
                    palette.Add(name, material);
                    materials.Add(material);
                }
                slots[i] = material;
            }
            body.sharedMaterials = slots;
        }
        if (mouth != null)
        {
            Vector3 front = visual.InverseTransformPoint(mouth.position);
            front.y = 0f;
            facingCorrection = Quaternion.FromToRotation(front.normalized, Vector3.forward);
        }
        else facingCorrection = Quaternion.identity;
        visual.localRotation = Quaternion.Euler(0f, 180f, 0f) * facingCorrection;
        Bounds bounds = ModelBounds();
        visual.localScale *= 1.4f / Mathf.Max(bounds.size.y, 0.01f);
        bounds = ModelBounds();
        visual.position += Vector3.up * (transform.position.y - 0.5f - bounds.min.y);
        if (originalBody != null) originalBody.forceRenderingOff = true;
        properties = new MaterialPropertyBlock();
        pulse = new AvatarPulse(visual, ModelBounds());
        restPosition = visual.localPosition;
        restScale = visual.localScale;
        footOffset = (ModelBounds().min.y - visual.position.y) / transform.lossyScale.y;
        foreach (string side in new[] { "Left", "Right" })
        {
            Transform sleeve = null;
            foreach (Transform part in visual.GetComponentsInChildren<Transform>())
                if (part.name == side + "Sleeve") sleeve = part;
            if (sleeve == null) continue;
            Bounds shoulder = sleeve.GetComponent<Renderer>().bounds;
            Vector3 pivot = visual.InverseTransformPoint(shoulder.center + Vector3.up * shoulder.extents.y);
            foreach (Transform part in visual.GetComponentsInChildren<Transform>())
            {
                if (part.name != side + "Sleeve" && part.name != side + "ShoulderTrim"
                    && part.name != side + "Arm" && part.name != side + "WristWrap") continue;
                Vector3 position = visual.InverseTransformPoint(part.position);
                arms.Add(new ArmPose { Part = part, Position = position, Pivot = pivot,
                    Rotation = Quaternion.Inverse(visual.rotation) * part.rotation,
                    Sign = Mathf.Sign((facingCorrection * position).x) });
            }
        }
    }

    private Bounds ModelBounds()
    {
        Bounds bounds = bodies[0].bounds;
        foreach (Renderer body in bodies) bounds.Encapsulate(body.bounds);
        return bounds;
    }

    public void Face(Vector3 direction)
    {
        if (celebrating || visual == null || direction.sqrMagnitude < 0.001f) return;
        direction.y = 0f;
        visual.rotation = Quaternion.LookRotation(direction) * facingCorrection;
    }

    public void SetHit(bool hit)
    {
        hitTint=hit;
        ApplyHitTint();
    }
    public void SetDashing(bool active)
    {
        if(dashTint==active) return;
        dashTint=active; ApplyHitTint();
    }

    public void SetCameraOpacity(float opacity)
    {
        opacity=Mathf.Clamp01(opacity);
        if(Mathf.Abs(CameraOpacity-opacity)<.0001f) return;
        CameraOpacity=opacity;
        bool transparent=opacity<.999f;
        foreach(Material material in materials)
        {
            Color color=material.color; color.a=opacity; material.color=color;
            material.SetFloat("_Surface",transparent?1:0);
            material.SetFloat("_SrcBlend",transparent?(float)UnityEngine.Rendering.BlendMode.SrcAlpha:1);
            material.SetFloat("_DstBlend",transparent?(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha:0);
            material.SetFloat("_SrcBlendAlpha",1);
            material.SetFloat("_DstBlendAlpha",transparent?(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha:0);
            material.SetFloat("_ZWrite",transparent?0:1);
            if(transparent) material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            else material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType",transparent?"Transparent":"Opaque");
            material.renderQueue=transparent?3000:2000;
            material.SetShaderPassEnabled("ShadowCaster",!transparent);
            material.SetShaderPassEnabled("DepthOnly",!transparent);
        }
        ApplyHitTint();
    }

    private void ApplyHitTint()
    {
        if(bodies==null || properties==null) return;
        properties.Clear();
        if(hitTint || dashTint)
        {
            Color tint=dashTint?new Color(.65f,.94f,1f,CameraOpacity):new Color(1f,.72f,.72f,CameraOpacity);
            properties.SetColor("_BaseColor",tint);
            properties.SetColor("_Color",tint);
        }
        foreach(Renderer body in bodies) if(body!=null) body.SetPropertyBlock(properties);
    }

    private void OnDisable()
    {
        celebrating = false;
        CameraOpacity=1f; hitTint=false; dashTint=false;
        arms.Clear();
        if (originalBody != null) originalBody.forceRenderingOff = false;
        if (visual != null) Release(visual.gameObject);
        foreach (Material material in materials) if (material != null) Release(material);
        materials.Clear();
        pulse = null;
        visual = null;
        mouth = null;
        bodies = null;
    }

    private static void Release(Object obj)
    {
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    private static Color PaletteColor(string name)
    {
        if (name.Contains("Skin")) return new Color(0.92f, 0.63f, 0.36f);
        if (name.Contains("Hair")) return new Color(0.095f, 0.043f, 0.025f);
        if (name.Contains("Scarf")) return new Color(0.98f, 0.12f, 0.065f);
        if (name.Contains("Gold")) return new Color(0.95f, 0.55f, 0.12f);
        if (name.Contains("Leather")) return new Color(0.19f, 0.075f, 0.035f);
        if (name.Contains("Boots")) return new Color(0.07f, 0.075f, 0.10f);
        if (name.Contains("Eyes")) return new Color(0.018f, 0.016f, 0.02f);
        if (name.Contains("Shirt")) return new Color(0.92f, 0.80f, 0.57f);
        return new Color(0.64f, 0.025f, 0.045f);
    }
}
