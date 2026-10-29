using UnityEngine;

// Small code-drawn illustrations, cached once and shared by the shop and codex.
public static class ItemIcons
{
    private static readonly Texture2D[] icons=new Texture2D[ItemLibrary.Count];
    public static Texture2D Get(int index)
    {
        if(icons[index]!=null) return icons[index];
        var d=new Drawing();
        Color steel=new Color32(210,231,233,255), gold=new Color32(255,202,77,255),
            red=new Color32(248,84,65,255), green=new Color32(142,228,105,255), blue=new Color32(98,199,239,255);
        switch((RunItem)index)
        {
            case RunItem.SlowReleaseValve:
                d.R(steel,29,55,99,77); d.R(steel,54,31,74,66);
                d.C(gold,64,31,22); d.C(new Color32(35,50,35,255),64,31,12);
                d.L(gold,44,31,84,31,5); d.L(gold,64,11,64,51,5);
                d.P(green,64,80,77,100,73,111,55,111,51,100); break;
            case RunItem.ArmorPiercing:
                d.P(steel,63,15,100,23,100,68,81,91,63,68); d.L(gold,22,54,78,54,12);
                d.P(gold,70,39,94,54,70,69); d.L(steel,16,39,34,39,4); d.L(steel,16,72,39,72,4); break;
            case RunItem.Ricochet:
                d.L(steel,27,18,27,88,9); d.L(blue,91,23,40,57,8); d.L(blue,40,57,91,87,8);
                d.P(gold,83,72,106,95,74,96); d.C(gold,38,56,9); break;
            case RunItem.IncendiaryRounds:
                d.Flame(red,gold,63,52,35); d.P(steel,50,51,62,33,74,51,74,99,50,99); d.R(gold,49,86,76,99); break;
            case RunItem.SplitCore:
                d.C(blue,62,76,17); d.L(gold,61,60,61,25,7); d.L(gold,50,63,24,37,7); d.L(gold,73,63,99,37,7);
                d.P(steel,51,29,61,13,71,29); d.P(steel,16,46,16,24,37,30); d.P(steel,87,30,108,24,108,46); break;
            case RunItem.Fuse:
                d.L(gold,22,92,44,92,9); d.L(gold,44,92,44,52,9); d.L(gold,44,52,83,52,9);
                d.Flame(red,gold,86,36,24); d.L(steel,99,18,109,10,4); d.L(steel,103,38,115,38,4); break;
            case RunItem.ConcentratedFuel:
                d.R(gold,30,34,97,104); d.R(steel,42,18,75,36); d.R(new Color32(28,40,35,255),50,23,67,34);
                d.P(steel,83,33,99,20,108,28,99,45); d.Flame(red,gold,63,70,23); break;
            case RunItem.CharBomb:
                d.C(steel,61,76,31); d.C(new Color32(52,63,64,255),61,76,24); d.L(gold,62,44,83,28,7);
                d.Flame(red,gold,90,27,19); d.L(red,45,74,57,64,5); d.L(red,57,64,67,85,5); d.L(red,67,85,80,74,5); break;
            case RunItem.Whetstone:
                d.P(blue,19,81,76,68,107,85,47,104); d.P(steel,32,69,74,16,87,28,43,81);
                d.L(gold,69,81,87,66,4); d.L(gold,82,89,102,87,4); break;
            case RunItem.BloodPendant:
                d.L(gold,32,19,60,76,5); d.L(gold,93,19,64,76,5);
                d.P(red,62,57,84,87,78,102,62,110,46,102,41,87); d.L(steel,53,89,56,98,4); break;
            case RunItem.RevengeCharm:
                d.P(red,63,17,78,40,103,46,89,71,91,101,62,94,32,101,35,71,22,46,48,40);
                d.L(gold,42,54,54,61,5); d.L(gold,83,54,70,61,5); d.P(gold,62,65,54,82,68,82,62,95,78,75,64,75); break;
            case RunItem.EmergencyShield:
                d.P(blue,63,16,102,30,96,76,82,95,63,110,43,95,29,76,23,30);
                d.P(steel,63,25,91,35,86,73,75,89,63,97); d.L(gold,43,60,57,75,7); d.L(gold,57,75,82,45,7); break;
            case RunItem.FirstAid:
                d.R(steel,39,23,86,47); d.R(new Color32(28,40,35,255),47,30,78,44); d.R(green,20,42,105,103);
                d.R(steel,53,51,72,93); d.R(steel,41,63,84,81); break;
            case RunItem.SavingsJar:
                d.R(steel,41,34,87,43); d.P(blue,37,44,90,44,101,68,95,102,31,102,25,68);
                d.C(gold,62,76,18); d.L(steel,63,64,63,88,4); d.C(gold,64,19,10); break;
            case RunItem.HuntingBadge:
                d.P(red,35,69,57,75,47,111,34,98,21,99); d.P(red,70,75,91,69,105,99,91,98,81,111);
                d.C(gold,63,53,33); d.C(new Color32(47,66,39,255),63,53,24); d.C(steel,63,53,13);
                d.L(red,63,28,63,78,4); d.L(red,38,53,88,53,4); break;
            case RunItem.DangerContract:
                d.P(steel,28,16,83,16,101,34,101,111,28,111); d.P(gold,83,16,83,34,101,34);
                d.P(red,64,39,88,80,40,80); d.L(steel,64,52,64,65,5); d.C(steel,64,73,3);
                d.L(gold,41,94,83,94,5); break;
            case RunItem.MagneticCoil:
                d.C(blue,62,63,38); d.C(new Color32(28,40,35,255),62,63,25);
                for(int i=0;i<6;i++) { float a=i*Mathf.PI/3; d.L(steel,62+Mathf.Cos(a)*27,63+Mathf.Sin(a)*27,62+Mathf.Cos(a)*37,63+Mathf.Sin(a)*37,6); }
                d.P(gold,69,12,43,67,61,67,51,112,85,51,66,51); break;
        }
        return icons[index]=d.Finish(((RunItem)index).ToString());
    }
    private sealed class Drawing
    {
        const int Size=256;
        readonly Color[] pixels=new Color[Size*Size];
        public void R(Color c,float x,float y,float r,float b)=>P(c,x,y,r,y,r,b,x,b);
        public void C(Color c,float x,float y,float radius)
        { for(int py=0;py<Size;py++) for(int px=0;px<Size;px++) if(new Vector2(px/2f-x,py/2f-y).sqrMagnitude<=radius*radius) pixels[(Size-1-py)*Size+px]=c; }
        public void L(Color c,float x,float y,float tx,float ty,float width)
        { Vector2 n=new Vector2(-(ty-y),tx-x).normalized*width/2; P(c,x+n.x,y+n.y,tx+n.x,ty+n.y,tx-n.x,ty-n.y,x-n.x,y-n.y); }
        public void P(Color c,params float[] v)
        {
            for(int py=0;py<Size;py++) for(int px=0;px<Size;px++)
            {
                float x=px/2f,y=py/2f; bool inside=false;
                for(int i=0,j=v.Length-2;i<v.Length;j=i,i+=2)
                    if((v[i+1]>y)!=(v[j+1]>y) && x<(v[j]-v[i])*(y-v[i+1])/(v[j+1]-v[i+1])+v[i]) inside=!inside;
                if(inside) pixels[(Size-1-py)*Size+px]=c;
            }
        }
        public void Flame(Color outer,Color inner,float x,float y,float s)
        { P(outer,x,y-s,x+s*.25f,y-s*.15f,x+s*.7f,y-s*.4f,x+s*.85f,y+s*.35f,x+s*.4f,y+s,x-s*.4f,y+s,x-s*.8f,y+s*.3f,x-s*.4f,y-s*.2f); P(inner,x,y,x+s*.4f,y+s*.5f,x,y+s*.85f,x-s*.35f,y+s*.5f); }
        public Texture2D Finish(string name)
        { var t=new Texture2D(Size,Size,TextureFormat.RGBA32,false) { name="Item Icon / "+name,filterMode=FilterMode.Bilinear,hideFlags=HideFlags.HideAndDontSave }; t.SetPixels(pixels); t.Apply(false,true); return t; }
    }
}
