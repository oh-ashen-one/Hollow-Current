using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    // Procedural meshes, textures and materials behind SurrealProps. Everything
    // is built lazily, once, and shared by every instance.
    public static partial class SurrealProps
    {
        static partial void ResetAssets()
        {
            baseQuad=centreQuad=null;flameTexture=null;flameMaterial=null;
            candelabraMesh=closedBookMesh=openBookMesh=null;candelabraMaterials=bookMaterials=null;
            ResetMoreAssets();
        }
        static void ResetMoreAssets()
        {
            runeAtlas=null;runeGlyphMeshes=null;
        }

        // Small flat-shaded mesh builder (build-time only; allocation is fine here).
        internal sealed class MeshBuilder
        {
            readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
            readonly List<Vector2> uvs=new List<Vector2>();
            readonly List<int>[] triangles;
            public MeshBuilder(int submeshes){triangles=new List<int>[submeshes];for(int i=0;i<submeshes;i++)triangles[i]=new List<int>();}
            // Triangle whose front face points the way `outward` does.
            public void Tri(Vector3 a,Vector3 b,Vector3 c,Vector3 outward,int submesh)
            {
                var n=Vector3.Cross(b-a,c-a);
                if(n.sqrMagnitude<1e-12f)return;
                if(Vector3.Dot(n,outward)<0){var swap=b;b=c;c=swap;n=-n;}
                n.Normalize();
                int i=vertices.Count;
                vertices.Add(a);vertices.Add(b);vertices.Add(c);
                normals.Add(n);normals.Add(n);normals.Add(n);
                uvs.Add(new Vector2(0,0));uvs.Add(new Vector2(1,0));uvs.Add(new Vector2(0,1));
                triangles[submesh].Add(i);triangles[submesh].Add(i+1);triangles[submesh].Add(i+2);
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 outward,int submesh){Tri(a,b,c,outward,submesh);Tri(a,c,d,outward,submesh);}
            public void Box(Vector3 centre,Vector3 size,Quaternion rotation,int submesh)
            {
                var h=size*.5f;
                for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
                {
                    var n=Vector3.zero;n[axis]=sign;
                    var u=Vector3.zero;u[(axis+1)%3]=1;var v=Vector3.zero;v[(axis+2)%3]=1;
                    var c=Vector3.Scale(n,h);var du=Vector3.Scale(u,h);var dv=Vector3.Scale(v,h);
                    Quad(centre+rotation*(c-du-dv),centre+rotation*(c+du-dv),centre+rotation*(c+du+dv),centre+rotation*(c-du+dv),rotation*n,submesh);
                }
            }
            // Frustum with a cap on top and bottom, axis along +Y from `baseCentre`.
            public void Prism(Vector3 baseCentre,float bottomRadius,float topRadius,float height,int sides,int submesh)
            {
                var top=baseCentre+Vector3.up*height;
                for(int i=0;i<sides;i++)
                {
                    float a0=i*Mathf.PI*2/sides,a1=(i+1)*Mathf.PI*2/sides;
                    var d0=new Vector3(Mathf.Cos(a0),0,Mathf.Sin(a0));var d1=new Vector3(Mathf.Cos(a1),0,Mathf.Sin(a1));
                    var mid=(d0+d1).normalized;
                    Quad(baseCentre+d0*bottomRadius,baseCentre+d1*bottomRadius,top+d1*topRadius,top+d0*topRadius,mid,submesh);
                    Tri(top,top+d0*topRadius,top+d1*topRadius,Vector3.up,submesh);
                    Tri(baseCentre,baseCentre+d0*bottomRadius,baseCentre+d1*bottomRadius,Vector3.down,submesh);
                }
            }
            public Mesh Build(string name)
            {
                var mesh=new Mesh{name=name,subMeshCount=triangles.Length};
                mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);
                for(int i=0;i<triangles.Length;i++)mesh.SetTriangles(triangles[i],i);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        // ---- candelabra: brass (submesh 0) and wax (submesh 1), origin at its centre ----
        const float CandleReach=.3f,ArmY=.12f,CandleTop=.29f;
        static readonly Vector3[] CandlePositions=
        {
            new Vector3(0,CandleTop,0),new Vector3(CandleReach,CandleTop-.02f,0),new Vector3(-CandleReach,CandleTop-.02f,0),
            new Vector3(0,CandleTop-.02f,CandleReach),new Vector3(0,CandleTop-.02f,-CandleReach)
        };
        static Mesh candelabraMesh,closedBookMesh,openBookMesh;
        static Material[] candelabraMaterials,bookMaterials;
        static Mesh CandelabraMesh
        {
            get
            {
                if(candelabraMesh)return candelabraMesh;
                var b=new MeshBuilder(2);
                b.Prism(new Vector3(0,-.4f,0),.1f,.05f,.06f,8,0);   // foot
                b.Prism(new Vector3(0,-.34f,0),.026f,.02f,.46f,6,0); // stem
                b.Prism(new Vector3(0,-.12f,0),.05f,.03f,.07f,8,0);  // knop
                b.Box(new Vector3(0,ArmY,0),new Vector3(CandleReach*2,.02f,.03f),Quaternion.identity,0);
                b.Box(new Vector3(0,ArmY,0),new Vector3(.03f,.02f,CandleReach*2),Quaternion.identity,0);
                b.Prism(new Vector3(0,ArmY+.01f,0),.04f,.028f,.04f,8,0);
                foreach(var p in CandlePositions)
                {
                    if(p.x==0&&p.z==0)continue; // centre cup is the one above
                    b.Prism(new Vector3(p.x,ArmY+.01f,p.z),.04f,.028f,.04f,8,0);
                }
                foreach(var p in CandlePositions)
                {
                    float height=p.y-(ArmY+.05f)-.0f;
                    b.Prism(new Vector3(p.x,ArmY+.05f,p.z),.017f,.015f,height,6,1);
                }
                return candelabraMesh=b.Build("Surreal candelabra");
            }
        }
        static Material[] CandelabraMaterials=>candelabraMaterials!=null?candelabraMaterials:candelabraMaterials=new[]
        {
            Lit("Floating candelabra brass",new Color(.34f,.25f,.1f),.75f,.45f),
            Lit("Floating candelabra wax",new Color(.78f,.72f,.58f),0,.2f)
        };

        // ---- books: leather (submesh 0) and pages (submesh 1) ----
        static Mesh ClosedBookMesh
        {
            get
            {
                if(closedBookMesh)return closedBookMesh;
                var b=new MeshBuilder(2);
                b.Box(Vector3.zero,new Vector3(.22f,.045f,.3f),Quaternion.identity,0);
                b.Box(new Vector3(.006f,0,0),new Vector3(.2f,.036f,.286f),Quaternion.identity,1);
                return closedBookMesh=b.Build("Surreal closed book");
            }
        }
        static Mesh OpenBookMesh
        {
            get
            {
                if(openBookMesh)return openBookMesh;
                var b=new MeshBuilder(2);
                foreach(int side in new[]{-1,1})
                {
                    var tilt=Quaternion.Euler(0,0,-side*16);
                    var centre=new Vector3(side*.115f,0,0);
                    b.Box(centre,new Vector3(.22f,.012f,.3f),tilt,0);
                    b.Box(centre+tilt*new Vector3(0,.011f,0),new Vector3(.2f,.012f,.28f),tilt,1);
                }
                return openBookMesh=b.Build("Surreal open book");
            }
        }
        static Material[] BookMaterials=>bookMaterials!=null?bookMaterials:bookMaterials=new[]
        {
            Lit("Floating book leather",new Color(.3f,.07f,.08f),0,.3f),
            Lit("Floating book pages",new Color(.7f,.65f,.5f),0,.05f)
        };

        // ---- rune atlas: 7 glyphs plus a solid cell, 4 x 3 cells of 64 px ----
        public const int RuneGlyphCount=7;
        const int AtlasColumns=4,AtlasRows=3,CellPixels=64;
        static Texture2D runeAtlas;
        static Mesh[] runeGlyphMeshes;
        public static Texture2D RuneAtlas
        {
            get
            {
                if(runeAtlas)return runeAtlas;
                var strokes=new List<Vector4>[RuneGlyphCount]; // x0,y0,x1,y1 in 0..1 cell space
                for(int g=0;g<RuneGlyphCount;g++)
                {
                    var random=new System.Random(g*7919+13);
                    var list=new List<Vector4>{new Vector4(.5f,.1f,.5f,.9f)};
                    if(g%3==2)list.Add(new Vector4(.28f,.1f,.28f,.7f));
                    int extra=2+random.Next(2);
                    for(int i=0;i<extra;i++)
                    {
                        float y=.25f+.25f*random.Next(3);
                        float side=random.Next(2)==0?-1:1;
                        list.Add(new Vector4(.5f,y,.5f+side*(.18f+.22f*(float)random.NextDouble()),Mathf.Clamp(y+(random.Next(3)-1)*.22f,.08f,.92f)));
                    }
                    strokes[g]=list;
                }
                runeAtlas=MakeTexture("Surreal runes",AtlasColumns*CellPixels,AtlasRows*CellPixels,(u,v)=>
                {
                    int column=Mathf.Min(AtlasColumns-1,(int)(u*AtlasColumns)),row=Mathf.Min(AtlasRows-1,(int)(v*AtlasRows));
                    float cu=u*AtlasColumns-column,cv=v*AtlasRows-row;
                    int cell=row*AtlasColumns+column;
                    if(cell==AtlasColumns*2)return new Color32(255,255,255,255); // solid cell
                    if(cell>=RuneGlyphCount)return new Color32(255,255,255,0);
                    float best=9;
                    foreach(var s in strokes[cell])best=Mathf.Min(best,SegmentDistance(cu,cv,s)*CellPixels);
                    byte a=(byte)(255*Mathf.Clamp01(2.4f-best));
                    return new Color32(255,255,255,a);
                });
                return runeAtlas;
            }
        }
        static float SegmentDistance(float x,float y,Vector4 s)
        {
            var a=new Vector2(s.x,s.y);var ab=new Vector2(s.z-s.x,s.w-s.y);var ap=new Vector2(x,y)-a;
            float t=Mathf.Clamp01(Vector2.Dot(ap,ab)/Mathf.Max(1e-6f,Vector2.Dot(ab,ab)));
            return (ap-ab*t).magnitude;
        }
        // UV of the solid cell centre, for flat-coloured geometry that shares the rune material.
        public static Vector2 RuneSolidUV=>new Vector2(.5f/AtlasColumns,(2.5f)/AtlasRows);
        public static Mesh RuneGlyphMesh(int glyph)
        {
            if(runeGlyphMeshes==null)runeGlyphMeshes=new Mesh[RuneGlyphCount];
            glyph=((glyph%RuneGlyphCount)+RuneGlyphCount)%RuneGlyphCount;
            if(runeGlyphMeshes[glyph])return runeGlyphMeshes[glyph];
            int column=glyph%AtlasColumns,row=glyph/AtlasColumns;
            float inset=.03f;
            float u0=(column+inset)/AtlasColumns,u1=(column+1-inset)/AtlasColumns,v0=(row+inset)/AtlasRows,v1=(row+1-inset)/AtlasRows;
            var mesh=new Mesh{name="Surreal rune "+glyph};
            mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0)};
            mesh.uv=new[]{new Vector2(u0,v0),new Vector2(u1,v0),new Vector2(u0,v1),new Vector2(u1,v1)};
            mesh.normals=new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.back};
            mesh.triangles=new[]{0,2,1,2,3,1};
            mesh.RecalculateBounds();
            return runeGlyphMeshes[glyph]=mesh;
        }
    }
}
