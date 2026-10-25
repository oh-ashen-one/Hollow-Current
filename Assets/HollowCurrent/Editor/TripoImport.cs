using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HollowCurrent.Editor
{
    #pragma warning disable 0649 // Manifest fields are filled by JsonUtility.
    public static class TripoImport
    {
        // output "Castle": a castle prop (tools/tripo_castle_convert.py) -> Resources/Castle/<name>.prefab, loaded by CastleArt.Place / SurrealProps;
        // anchors: flattened x,y,z of candle tips -> empty children "Flame anchor <i>" (the flame billboards sit there).
        [Serializable] class Manifest { public string name; public string output; public float[] anchors; public Surface[] materials; public Primitive[] primitives; }
        [Serializable] class Surface { public string stem; public float[] colorFactor; public bool hasNormal; public bool hasMetal; }
        [Serializable] class Primitive { public string file; public int material; public int vertices; public int triangles; }
        public static void Prepare()
        {
            const string source="Assets/HollowCurrent/Art/Tripo";
            if(!Directory.Exists(source))throw new DirectoryNotFoundException("Required original furniture source missing: "+source);
            Directory.CreateDirectory("Assets/HollowCurrent/Resources/Furniture");
            AssetDatabase.Refresh();
            Texture("Assets/HollowCurrent/Resources/Surfaces/OakFloor.png",false,true,2048);
            Texture("Assets/HollowCurrent/Resources/Surfaces/FamilyRug.png",false,true,2048);
            Texture("Assets/HollowCurrent/Resources/Surfaces/FamilyPhoto.png",false,true,1024);
            Texture("Assets/HollowCurrent/Resources/Surfaces/UVHandprint.png",false,true,1024);
            Texture("Assets/HollowCurrent/Resources/Surfaces/ChildDrawing.png",false,true,1024);
            Texture("Assets/HollowCurrent/Resources/Surfaces/FurnaceDiary.png",false,true,1024);
            Texture("Assets/HollowCurrent/Resources/Surfaces/GardenGround.png",false,true,1024);
            Texture("Assets/HollowCurrent/Resources/Surfaces/DrivewayGround.png",false,true,1024);
            foreach(string page in new[]{"ChildDrawing","FurnaceDiary","FamilyPhoto"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/HollowCurrent/Resources/Surfaces/"+page+".png");
                if(importer.npotScale!=TextureImporterNPOTScale.None){importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();}
            }
            const string uiArt="Assets/HollowCurrent/Resources/UI/MenuBackdrop.png";
            Texture(uiArt,false,true,2048);
            var uiImporter=(TextureImporter)AssetImporter.GetAtPath(uiArt);
            if(uiImporter.npotScale!=TextureImporterNPOTScale.None||uiImporter.mipmapEnabled)
            {uiImporter.npotScale=TextureImporterNPOTScale.None;uiImporter.mipmapEnabled=false;uiImporter.SaveAndReimport();}
            foreach(var path in Directory.GetFiles(source,"import.json",SearchOption.AllDirectories))
            {
                var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
                string dir=Path.GetDirectoryName(path).Replace('\\','/');
                var materials=new Material[manifest.materials.Length];
                for(int i=0;i<materials.Length;i++)
                {
                    var spec=manifest.materials[i]; string stem=dir+"/"+spec.stem;
                    var material=new Material(Shader.Find("Standard")){name=manifest.name+" / original Tripo"};
                    material.enableInstancing=true;material.mainTexture=Texture(stem+"-color.png",false,true,2048);
                    if(spec.colorFactor!=null&&spec.colorFactor.Length==4)material.color=new Color(spec.colorFactor[0],spec.colorFactor[1],spec.colorFactor[2],spec.colorFactor[3]);
                    if(spec.hasNormal){material.SetTexture("_BumpMap",Texture(stem+"-normal.png",true,false,2048));material.EnableKeyword("_NORMALMAP");}
                    if(spec.hasMetal){material.SetTexture("_MetallicGlossMap",Texture(stem+"-metal.png",false,false,2048));material.EnableKeyword("_METALLICGLOSSMAP");material.SetFloat("_GlossMapScale",1);}
                    string asset=stem+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(asset);
                    if(existing){EditorUtility.CopySerialized(material,existing);UnityEngine.Object.DestroyImmediate(material);material=existing;}
                    else AssetDatabase.CreateAsset(material,asset);
                    materials[i]=material;
                }
                var root=new GameObject(manifest.name);
                foreach(var part in manifest.primitives)
                {
                    var mesh=new Mesh{name=manifest.name,indexFormat=IndexFormat.UInt32};
                    using(var reader=new BinaryReader(File.OpenRead(dir+"/"+part.file)))
                    {
                        if(reader.ReadUInt32()!=0x48434d31)throw new InvalidDataException("Invalid furniture mesh");
                        int count=reader.ReadInt32(),indices=reader.ReadInt32();
                        if(count!=part.vertices||indices!=part.triangles*3)throw new InvalidDataException("Furniture manifest mismatch");
                        var vertices=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];var triangles=new int[indices];
                        for(int j=0;j<count;j++){vertices[j]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());normals[j]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());uv[j]=new Vector2(reader.ReadSingle(),reader.ReadSingle());}
                        for(int j=0;j<indices;j++)triangles[j]=reader.ReadInt32();
                        mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateBounds();mesh.RecalculateTangents();
                    }
                    string meshPath=dir+"/"+Path.GetFileNameWithoutExtension(part.file)+".asset";
                    var existingMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(existingMesh){EditorUtility.CopySerialized(mesh,existingMesh);UnityEngine.Object.DestroyImmediate(mesh);mesh=existingMesh;}else AssetDatabase.CreateAsset(mesh,meshPath);
                    var child=new GameObject("Crafted surface");child.transform.SetParent(root.transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;
                    child.AddComponent<MeshRenderer>().sharedMaterial=materials[part.material];
                }
                bool castle=manifest.output=="Castle";
                if(castle&&manifest.anchors!=null)
                    for(int a=0;a+2<manifest.anchors.Length;a+=3)
                    {var anchor=new GameObject("Flame anchor "+(a/3));anchor.transform.SetParent(root.transform,false);anchor.transform.localPosition=new Vector3(manifest.anchors[a],manifest.anchors[a+1],manifest.anchors[a+2]);}
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/HollowCurrent/Resources/"+(castle?"Castle/":"Furniture/")+manifest.name+".prefab");
                UnityEngine.Object.DestroyImmediate(root);Debug.Log("HC_TRIPO_IMPORTED "+manifest.name);
            }
            AssetDatabase.SaveAssets();
        }
        static Texture2D Texture(string path,bool normal,bool srgb,int limit)
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new FileNotFoundException(path);
            var desiredType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            bool changed=importer.textureType!=desiredType||importer.textureShape!=TextureImporterShape.Texture2D||
                importer.sRGBTexture!=srgb||importer.maxTextureSize!=limit||!importer.mipmapEnabled||
                importer.textureCompression!=TextureImporterCompression.CompressedHQ||importer.anisoLevel!=4;
            if(changed)
            {
                importer.textureType=desiredType;importer.textureShape=TextureImporterShape.Texture2D;
                importer.sRGBTexture=srgb;importer.maxTextureSize=limit;importer.mipmapEnabled=true;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.anisoLevel=4;importer.SaveAndReimport();
            }
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(!texture)throw new InvalidDataException("Surface texture did not import as Texture2D: "+path);
            return texture;
        }
    }
}
