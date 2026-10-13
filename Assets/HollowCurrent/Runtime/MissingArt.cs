using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    // Visible stand-ins for art a map has not received yet. Bellwether keeps its
    // required-asset throws; the castle (Placeholders=true while it is open)
    // gets magenta geometry/textures and silent clips so it runs before art lands.
    public static class MissingArt
    {
        public static bool Placeholders;
        static Material magenta;
        static Texture2D magentaTexture;
        static AudioClip silence;
        static readonly HashSet<string> reported=new HashSet<string>();
        public static Material Magenta
        {
            get
            {
                if(!magenta){magenta=new Material(Shader.Find("Standard")){name="Missing art / magenta",color=Color.magenta};magenta.EnableKeyword("_EMISSION");magenta.SetColor("_EmissionColor",new Color(.35f,0,.35f));}
                return magenta;
            }
        }
        public static Texture2D MagentaTexture
        {
            get
            {
                if(!magentaTexture){magentaTexture=new Texture2D(4,4){name="Missing art / magenta texture"};var pixels=new Color[16];for(int i=0;i<16;i++)pixels[i]=Color.magenta;magentaTexture.SetPixels(pixels);magentaTexture.Apply();}
                return magentaTexture;
            }
        }
        public static AudioClip Silence
        {
            get{if(!silence)silence=AudioClip.Create("Missing audio / silence",4410,1,44100,false);return silence;}
        }
        // Log each missing asset once, in a greppable form.
        public static void Report(string kind,string name)
        {if(reported.Add(kind+"/"+name))Debug.LogWarning("HC_MISSING_ART "+kind+" "+name);}
        // A magenta cube of the requested size, standing on its origin, no collider.
        public static Transform Placeholder(Transform parent,string name,Vector3 size)
        {
            Report("model",name);
            var root=new GameObject("Missing art / "+name).transform;root.SetParent(parent,false);
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name="Placeholder";cube.transform.SetParent(root,false);
            cube.transform.localScale=size;cube.transform.localPosition=Vector3.up*size.y*.5f;
            Object.DestroyImmediate(cube.GetComponent<Collider>());cube.GetComponent<Renderer>().sharedMaterial=Magenta;
            return root;
        }
    }
}
