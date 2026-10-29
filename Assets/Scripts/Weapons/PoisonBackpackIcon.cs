using UnityEngine;

/// <summary>Shared poison backpack icon for starter cards, the shop and equipment bar.</summary>
public static class PoisonBackpackIcon
{
    private static Texture2D icon;
    public static Texture2D Get()
    {
        if(icon!=null) return icon;
        const int size=256;
        var pixels=new Color[size*size];
        Color iron=new Color32(53,60,56,255),ironLight=new Color32(94,105,91,255),
            wood=new Color32(137,83,40,255),woodLight=new Color32(179,116,57,255),woodShade=new Color32(94,59,32,255),
            strap=new Color32(184,151,92,255),poison=new Color32(166,218,51,255),shine=new Color32(215,245,124,255),
            glass=new Color32(65,88,44,255);
        void Polygon(Color color,params float[] v)
        {
            for(int py=0;py<size;py++) for(int px=0;px<size;px++)
            {
                float x=(px+.5f)/2,y=(py+.5f)/2; bool inside=false;
                for(int i=0,j=v.Length-2;i<v.Length;j=i,i+=2)
                    if((v[i+1]>y)!=(v[j+1]>y) && x<(v[j]-v[i])*(y-v[i+1])/(v[j+1]-v[i+1])+v[i]) inside=!inside;
                if(inside) pixels[(size-1-py)*size+px]=color;
            }
        }
        void Rect(Color color,float x,float y,float width,float height)=>Polygon(color,x,y,x+width,y,x+width,y+height,x,y+height);

        // Two shoulder loops identify a backpack even at the equipment bar's small size.
        Rect(iron,19,26,19,66); Rect(strap,19,26,7,66); Rect(strap,24,26,14,7); Rect(strap,24,85,14,7);
        Rect(iron,69,25,19,62); Rect(strap,81,25,7,62);
        // Simple chamfered wood reservoir, with a darker right face.
        Polygon(woodShade,32,28,71,25,84,34,84,87,75,98,32,94);
        Polygon(wood,30,33,37,26,71,26,77,33,77,90,71,97,36,97,30,90);
        Polygon(woodLight,30,33,37,26,71,26,77,33,70,37,36,37);
        Rect(woodLight,34,39,5,46);
        // Block cap and broad dark iron bands match the project's wooden weapons.
        Rect(iron,43,15,24,13); Rect(ironLight,43,15,24,5);
        Rect(iron,28,34,52,8); Polygon(ironLight,28,34,36,30,76,30,80,34);
        Rect(iron,28,82,52,9); Polygon(ironLight,28,82,77,82,84,78,84,87,80,91,80,82);
        // One bright inset level window; no tiny lettering or warning symbol.
        Rect(iron,44,44,23,36); Rect(glass,48,47,15,29);
        Rect(poison,48,55,15,21); Rect(shine,48,55,15,4); Rect(shine,50,61,3,11);
        // Short elbow pipe and a single angular drip communicate automatic poison spraying.
        Rect(iron,76,66,21,12); Rect(ironLight,78,66,19,4);
        Rect(iron,88,70,10,23); Rect(ironLight,88,72,4,17);
        Rect(ironLight,85,88,16,8); Rect(iron,88,93,10,5);
        Polygon(poison,93,100,100,109,100,114,96,118,90,118,86,114,86,109);
        Rect(shine,89,109,3,5);

        icon=new Texture2D(size,size,TextureFormat.RGBA32,false) {
            name="Poison Backpack Weapon Icon",filterMode=FilterMode.Bilinear,hideFlags=HideFlags.HideAndDontSave
        };
        icon.SetPixels(pixels); icon.Apply(false,false); return icon;
    }
}
