Shader "HollowCurrent/RainGlass"
{
    Properties { _Color("Glass tint",Color)=(.21,.29,.32,.18) _Glossiness("Smoothness",Range(0,1))=.88 }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0
        struct Input {float3 worldPos;float3 viewDir;};
        fixed4 _Color;half _Glossiness;
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 uv=IN.worldPos.xz+float2(IN.worldPos.y*.07,0);
            float streak=pow(saturate(sin((uv.x+uv.y)*177)),36)*pow(saturate(sin(IN.worldPos.y*14+uv.y*32-_Time.y*.34)),10);
            float edge=pow(1-saturate(abs(IN.viewDir.z)),4);
            o.Albedo=_Color.rgb;o.Smoothness=_Glossiness;o.Metallic=.1;
            o.Alpha=_Color.a+edge*.18+streak*.16;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
