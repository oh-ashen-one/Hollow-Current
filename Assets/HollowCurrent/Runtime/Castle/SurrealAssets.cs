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
            runeAtlas=null;runeGlyphMeshes=null;portraitTexture=null;pupilTexture=null;batTexture=null;batMaterial=null;knifeMesh=null;knifeMaterials=null;frameMesh=null;frameMaterial=null;portraitMaterial=null;pupilMaterial=null;
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

        // ---- portrait: frame, painting and pupils ----
        static Texture2D portraitTexture,pupilTexture;
        static Mesh frameMesh;
        static Material frameMaterial,portraitMaterial,pupilMaterial;
        // Painted eye positions in portrait UV space (also used to place pupils).
        public const float PortraitEyeV=.64f,PortraitEyeU=.1f; // eyes at 0.5 +- PortraitEyeU horizontally
        static Texture2D PortraitTexture=>portraitTexture?portraitTexture:portraitTexture=MakeTexture("Surreal portrait",128,192,(u,v)=>
        {
            float vignette=Mathf.Clamp01(1.25f-2.2f*((u-.5f)*(u-.5f)+(v-.55f)*(v-.55f)));
            float noise=Mathf.PerlinNoise(u*9,v*9)*.25f;
            var colour=Color.Lerp(new Color(.05f,.02f,.03f),new Color(.2f,.07f,.06f),vignette*(.7f+noise));
            // Shoulders and coat.
            float coat=Smooth(.42f,.3f,v)*Smooth(.62f,.5f,Mathf.Abs(u-.5f)+(.34f-v)*.3f);
            colour=Color.Lerp(colour,new Color(.03f,.03f,.04f),coat);
            // Hair, then the pale face.
            float hair=Smooth(1f,.8f,Mathf.Sqrt(Mathf.Pow((u-.5f)/.25f,2)+Mathf.Pow((v-.66f)/.3f,2)));
            colour=Color.Lerp(colour,new Color(.02f,.02f,.025f),hair*.95f);
            float face=Smooth(1f,.92f,Mathf.Sqrt(Mathf.Pow((u-.5f)/.19f,2)+Mathf.Pow((v-.58f)/.25f,2)));
            var skin=Color.Lerp(new Color(.36f,.34f,.31f),new Color(.58f,.55f,.5f),Mathf.Clamp01(1.1f-Mathf.Abs(u-.5f)*5f))*(.8f+noise*.5f);
            colour=Color.Lerp(colour,skin,face);
            foreach(float side in new[]{-1f,1f})
            {
                float dx=(u-(.5f+side*PortraitEyeU))/.065f,dy=(v-PortraitEyeV)/.04f;
                float r=Mathf.Sqrt(dx*dx+dy*dy);
                colour=Color.Lerp(colour,new Color(.08f,.05f,.05f),Smooth(1.45f,1.05f,r)*face);      // socket
                colour=Color.Lerp(colour,new Color(.78f,.74f,.62f),Smooth(1f,.8f,r)*face);           // eye white
            }
            colour=Color.Lerp(colour,new Color(.1f,.04f,.04f),Smooth(.012f,.004f,Mathf.Abs(v-.43f))*Smooth(.07f,.04f,Mathf.Abs(u-.5f))*face); // mouth
            colour.a=1;return colour;
        });
        static Texture2D PupilTexture=>pupilTexture?pupilTexture:pupilTexture=MakeTexture("Surreal pupil",32,32,(u,v)=>
        {
            float r=Mathf.Sqrt((u-.5f)*(u-.5f)+(v-.5f)*(v-.5f))*2;
            return new Color(1,1,1,Smooth(1f,.8f,r));
        });
        static Mesh FrameMesh
        {
            get
            {
                if(frameMesh)return frameMesh;
                // Unit-size frame: four bars around a 1 x 1 opening, 0.05 deep, behind z = 0.
                var b=new MeshBuilder(1);
                const float bar=.07f,depth=.05f;
                b.Box(new Vector3(0,.5f+bar*.5f,-depth*.5f),new Vector3(1+bar*2,bar,depth),Quaternion.identity,0);
                b.Box(new Vector3(0,-.5f-bar*.5f,-depth*.5f),new Vector3(1+bar*2,bar,depth),Quaternion.identity,0);
                b.Box(new Vector3(.5f+bar*.5f,0,-depth*.5f),new Vector3(bar,1,depth),Quaternion.identity,0);
                b.Box(new Vector3(-.5f-bar*.5f,0,-depth*.5f),new Vector3(bar,1,depth),Quaternion.identity,0);
                return frameMesh=b.Build("Surreal portrait frame");
            }
        }
        static Material FrameMaterial=>frameMaterial?frameMaterial:frameMaterial=Lit("Portrait frame",new Color(.22f,.15f,.07f),.5f,.35f);
        static Material PortraitMaterial=>portraitMaterial?portraitMaterial:portraitMaterial=NewPortraitMaterial();
        static Material NewPortraitMaterial(){var m=Lit("Portrait painting",Color.white,0,.15f);m.mainTexture=PortraitTexture;return m;}
        static Material PupilMaterial=>pupilMaterial?pupilMaterial:pupilMaterial=Unlit("Portrait pupil",PupilTexture,new Color(.02f,.01f,.01f,1));

        // ---- bat silhouette (billboard, 2:1) ----
        static Texture2D batTexture;
        static Material batMaterial;
        static Texture2D BatTexture=>batTexture?batTexture:batTexture=MakeTexture("Surreal bat",128,64,(u,v)=>
        {
            float x=Mathf.Abs(u-.5f)*2;
            float alpha=0;
            // Body and head.
            float body=Smooth(.1f,.07f,x)*Smooth(.14f,.2f,v)*Smooth(.8f,.7f,v);
            float head=Smooth(1f,.8f,Mathf.Sqrt(Mathf.Pow(x/.075f,2)+Mathf.Pow((v-.78f)/.13f,2)));
            float ears=Smooth(.03f,.0f,Mathf.Abs(x-.05f-(v-.86f)*.25f))*Smooth(.84f,.88f,v)*Smooth(.97f,.93f,v);
            alpha=Mathf.Max(body,Mathf.Max(head,ears));
            // Wings: from the shoulder out to a tip, with a scalloped trailing edge.
            if(x>=.05f)
            {
                float a=(x-.05f)/.95f;
                float top=a<.5f?Mathf.Lerp(.6f,.92f,a/.5f):Mathf.Lerp(.92f,.4f,(a-.5f)/.5f);
                float bottom=Mathf.Lerp(.42f,.3f,a)+.13f*Mathf.Pow(Mathf.Abs(Mathf.Sin(a*Mathf.PI*3.2f)),.7f);
                float wing=Smooth(top+.03f,top-.02f,v)*Smooth(bottom-.02f,bottom+.03f,v)*Smooth(1f,.95f,a);
                alpha=Mathf.Max(alpha,wing);
            }
            return new Color(1,1,1,alpha);
        });
        // Bat billboard material; the tint is deliberately not pure black so the
        // flock reads against dark stone.
        public static Material BatMaterial=>batMaterial?batMaterial:batMaterial=Unlit("Giant bat",BatTexture,new Color(.1f,.075f,.13f,1));

        // ---- knife (tip along +Z, origin at the guard): steel blade, wooden handle ----
        static Mesh knifeMesh;
        static Material[] knifeMaterials;
        public static Mesh KnifeMesh
        {
            get
            {
                if(knifeMesh)return knifeMesh;
                var b=new MeshBuilder(2);
                const float hw=.017f,ht=.003f,straight=.14f,tip=.23f;
                b.Box(new Vector3(0,0,straight*.5f),new Vector3(hw*2,ht*2,straight),Quaternion.identity,0);
                var a=new Vector3(-hw,-ht,straight);var bb=new Vector3(hw,-ht,straight);var c=new Vector3(hw,ht,straight);var d=new Vector3(-hw,ht,straight);
                var point=new Vector3(0,0,tip);
                b.Tri(d,c,point,Vector3.up,0);b.Tri(a,bb,point,Vector3.down,0);
                b.Tri(bb,c,point,Vector3.right,0);b.Tri(a,d,point,Vector3.left,0);
                b.Box(Vector3.zero,new Vector3(.06f,.012f,.014f),Quaternion.identity,0);          // guard
                b.Box(new Vector3(0,0,-.055f),new Vector3(.02f,.022f,.11f),Quaternion.identity,1); // handle
                return knifeMesh=b.Build("Surreal knife");
            }
        }
        public static Material[] KnifeMaterials=>knifeMaterials!=null?knifeMaterials:knifeMaterials=new[]
        {
            Lit("Orbiting knife steel",new Color(.62f,.64f,.68f),.9f,.7f),
            Lit("Orbiting knife handle",new Color(.14f,.08f,.05f),0,.3f)
        };
    }
}
