Shader "HollowCurrent/HouseSurface"
{
    Properties
    {
        _Color ("Surface tint", Color) = (0.6,0.6,0.6,1)
        _Pattern ("Plaster / textile / stone / timber / bark / slate", Float) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.12
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        struct Input { float3 worldPos; float3 worldNormal; };
        fixed4 _Color;
        half _Pattern, _Glossiness;
        float grain(float3 p)
        {
            p=frac(p*.1031);p+=dot(p,p.yzx+33.33);
            return frac((p.x+p.y)*p.z);
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 p=IN.worldPos;
            float3 n=abs(IN.worldNormal);
            float2 uv=n.y>.5?p.xz:(n.x>.5?p.zy:p.xy);
            float fine=grain(floor(p*65));
            float coarse=grain(floor(p*8));
            float value=.97+fine*.02+coarse*.01;
            if(_Pattern>.5&&_Pattern<1.5)
            {
                float weave=sin(uv.x*240)*sin(uv.y*240);
                value=.91+fine*.035+weave*.015;
            }
            else if(_Pattern>1.5&&_Pattern<2.5)
            {
                float2 brick=uv*float2(1.6,3.4);
                brick.x+=fmod(floor(brick.y),2)*.5;
                float2 edge=frac(brick);
                float mortar=step(.035,edge.x)*step(.06,edge.y);
                value=(.74+grain(floor(float3(brick,0)))*.20)*(.78+.22*mortar)+fine*.04;
            }
            else if(_Pattern>2.5&&_Pattern<3.5)
                value=.83+.09*sin(uv.x*110+sin(uv.y*7)*2)+fine*.05;
            else if(_Pattern>3.5&&_Pattern<4.5)
            {
                float fissure=pow(abs(sin((p.x+p.z)*75+sin(p.y*2.2)*1.2)),6);
                value=.72+.21*fissure+coarse*.07;
            }
            else if(_Pattern>4.5)
                value=.90+fine*.055+coarse*.045;
            o.Albedo=_Color.rgb*value;
            o.Smoothness=_Glossiness;
            o.Metallic=0;
            o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
