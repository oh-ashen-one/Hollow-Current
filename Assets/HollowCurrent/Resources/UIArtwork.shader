Shader "HollowCurrent/UIArtwork"
{
    Properties { _MainTex("Artwork",2D)="white"{} _Tint("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Overlay" }
        ZTest Always ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _Tint;
            float4 frag(v2f_img i):SV_Target { return tex2D(_MainTex,i.uv)*_Tint; }
            ENDCG
        }
    }
}
