using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
namespace HollowCurrent.Editor
{
    // Original Blender geometry is imported without an external runtime loader.
    #pragma warning disable 0649 // Fields populated by JsonUtility.
    public static class AuthoredImport
    {
        [Serializable] class Kit {public Surface[] materials;public Asset[] assets;}
        [Serializable] class Surface {public string name;public float[] color;public float smoothness,metallic;}
        [Serializable] class Asset {public string name;public Part[] parts;}
        [Serializable] class Part {public string file,material;public int vertices,triangles;}
        const string Source="Assets/HollowCurrent/Art/Authored/";
        const string Target="Assets/HollowCurrent/Resources/Architecture/";
        public static void Prepare()
        {
            if(!File.Exists(Source+"kit.json"))throw new Exception("Blender-authored architecture has not been exported");
            Directory.CreateDirectory(Target);AssetDatabase.Refresh();
            var kit=JsonUtility.FromJson<Kit>(File.ReadAllText(Source+"kit.json"));
            var materials=new Dictionary<string,Material>();
            foreach(var s in kit.materials)
            {
                var m=new Material(s.name=="Glass"?Resources.Load<Shader>("RainGlass"):Shader.Find("Standard")){name="Bellwether / "+s.name};
                m.enableInstancing=true;m.color=new Color(s.color[0],s.color[1],s.color[2],s.color[3]);m.SetFloat("_Glossiness",s.smoothness);m.SetFloat("_Metallic",s.metallic);
                if(s.name=="Oak"||s.name=="Walnut")
                {
                    m.mainTexture=Resources.Load<Texture2D>("Surfaces/OakFloor");m.mainTextureScale=new Vector2(.35f,.35f);
                    m.color=s.name=="Oak"?new Color(.95f,.84f,.67f):new Color(.63f,.53f,.43f);
                }
                if(s.name=="Linen")
                {m.shader=Resources.Load<Shader>("HouseSurface");m.SetFloat("_Pattern",1);m.SetFloat("_Glossiness",.06f);}
                if(s.name=="Bark"||s.name=="WeatheredTimber"||s.name.StartsWith("Slate"))
                {m.shader=Resources.Load<Shader>("HouseSurface");m.SetFloat("_Pattern",s.name=="Bark"?4:s.name=="WeatheredTimber"?3:5);m.SetFloat("_Glossiness",s.smoothness);}
                if(s.name=="Shade"){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(.72f,.50f,.25f));}
                materials[s.name]=Save(m,Target+s.name+".mat");
            }
            foreach(var spec in kit.assets)
            {
                var root=new GameObject(spec.name);
                foreach(var part in spec.parts)
                {
                    var mesh=new Mesh{name=spec.name+" / "+part.material,indexFormat=IndexFormat.UInt32};
                    using(var r=new BinaryReader(File.OpenRead(Source+part.file)))
                    {
                        if(r.ReadUInt32()!=0x48434d31)throw new Exception("Invalid authored mesh header");
                        int count=r.ReadInt32(),ic=r.ReadInt32();
                        if(count!=part.vertices||ic!=part.triangles*3)throw new Exception("Authored manifest mismatch");
                        var v=new Vector3[count];var n=new Vector3[count];var uv=new Vector2[count];var ix=new int[ic];
                        for(int i=0;i<count;i++){v[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());n[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());}
                        for(int i=0;i<ic;i++)ix[i]=r.ReadInt32();
                        mesh.vertices=v;mesh.normals=n;mesh.uv=uv;mesh.triangles=ix;mesh.RecalculateBounds();mesh.RecalculateTangents();
                    }
                    var child=new GameObject(part.material);child.transform.SetParent(root.transform,false);
                    child.AddComponent<MeshFilter>().sharedMesh=Save(mesh,Target+Path.GetFileNameWithoutExtension(part.file)+".asset");
                    child.AddComponent<MeshRenderer>().sharedMaterial=materials[part.material];
                }
                PrefabUtility.SaveAsPrefabAsset(root,Target+spec.name+".prefab");UnityEngine.Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();Debug.Log("HC_ARCHITECTURE_IMPORTED "+kit.assets.Length+" original Blender assets");
        }
        static T Save<T>(T value,string path) where T:UnityEngine.Object
        {
            var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing){EditorUtility.CopySerialized(value,existing);UnityEngine.Object.DestroyImmediate(value);return existing;}
            AssetDatabase.CreateAsset(value,path);return value;
        }
    }
}
