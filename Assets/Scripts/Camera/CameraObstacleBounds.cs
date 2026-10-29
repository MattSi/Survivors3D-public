using System.Collections.Generic;
using UnityEngine;

/// <summary>Rendered scenery bounds and reversible, instance-only camera fading.</summary>
public sealed class CameraObstacleBounds : MonoBehaviour
{
    public static readonly List<CameraObstacleBounds> Active=new List<CameraObstacleBounds>();
    public Bounds VisualBounds;
    public Renderer[] VisualRenderers;
    public float Opacity { get; private set; }=1f;
    Material[][] originals, faded;
    void OnEnable()=>Active.Add(this);
    void OnDisable() { Active.Remove(this); Restore(); }
    void OnDestroy()
    {
        if(faded==null) return;
        foreach(var slots in faded) foreach(var material in slots)
            if(material!=null) Destroy(material);
    }
    public void Restore()
    {
        if(originals!=null && VisualRenderers!=null)
            for(int i=0;i<VisualRenderers.Length;i++)
                if(VisualRenderers[i]!=null) VisualRenderers[i].sharedMaterials=originals[i];
        Opacity=1;
    }
    public void SetObscuring(bool obscuring,float dt)
    {
        float alpha=Mathf.MoveTowards(Opacity,obscuring?.22f:1f,dt*4f);
        if(Mathf.Abs(alpha-Opacity)<.0001f) return;
        if(alpha>=.999f) { Restore(); return; }
        if(VisualRenderers==null) VisualRenderers=GetComponentsInChildren<Renderer>();
        if(originals==null)
        {
            originals=new Material[VisualRenderers.Length][]; faded=new Material[VisualRenderers.Length][];
            for(int i=0;i<VisualRenderers.Length;i++)
            {
                originals[i]=VisualRenderers[i].sharedMaterials;
                faded[i]=new Material[originals[i].Length];
                for(int j=0;j<faded[i].Length;j++)
                {
                    Material source=originals[i][j];
                    var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    material.hideFlags=HideFlags.HideAndDontSave;
                    if(source!=null)
                    {
                        if(source.shader==material.shader) material.CopyPropertiesFromMaterial(source);
                        else
                        {
                            foreach(string prop in new[]{"_BaseMap","_Albedo","_MainTex"})
                                if(source.HasProperty(prop))
                                {
                                    material.SetTexture("_BaseMap",source.GetTexture(prop));
                                    material.SetTextureScale("_BaseMap",source.GetTextureScale(prop));
                                    material.SetTextureOffset("_BaseMap",source.GetTextureOffset(prop)); break;
                                }
                            material.color=source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):
                                source.HasProperty("_Color")?source.GetColor("_Color"):Color.white;
                            material.SetFloat("_Smoothness",.15f);
                        }
                    }
                    material.SetFloat("_Surface",1); material.SetFloat("_ZWrite",0);
                    material.SetFloat("_SrcBlend",5); material.SetFloat("_DstBlend",10);
                    material.SetFloat("_SrcBlendAlpha",1); material.SetFloat("_DstBlendAlpha",10);
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.DisableKeyword("_ALPHAMODULATE_ON");
                    material.SetOverrideTag("RenderType","Transparent"); material.renderQueue=3000;
                    material.SetShaderPassEnabled("DepthOnly",false);
                    material.SetShaderPassEnabled("ShadowCaster",false);
                    faded[i][j]=material;
                }
            }
        }
        for(int i=0;i<faded.Length;i++)
        {
            foreach(Material material in faded[i]) { Color color=material.color; color.a=alpha; material.color=color; }
            if(VisualRenderers[i]!=null) VisualRenderers[i].sharedMaterials=faded[i];
        }
        Opacity=alpha;
    }
}
