Shader "HollowCurrent/GardenGround"
{
    Properties
    {
        _Color ("Tint", Color) = (0.8,0.82,0.75,1)
        _GrassTex ("Original grass albedo", 2D) = "white" {}
        _RoadTex ("Original asphalt albedo", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        struct Input { float3 worldPos; };
        sampler2D _GrassTex, _RoadTex;
        fixed4 _Color;
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 p=IN.worldPos.xz;
            float drive=(1-smoothstep(3.40,3.50,abs(p.x)))*(1-smoothstep(4.40,4.50,abs(p.y+12.30)));
            fixed3 grass=tex2D(_GrassTex,p*.50).rgb;
            fixed3 road=tex2D(_RoadTex,p*.50).rgb;
            o.Albedo=lerp(grass,road,drive)*_Color.rgb;
            o.Smoothness=lerp(.07,.16,drive);o.Metallic=0;o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
