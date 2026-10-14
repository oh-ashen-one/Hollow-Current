// Lit-window shader for the Haunted Castle exterior kit (hc/art-exterior2). Unlit, fog-free, opaque: the atlas texel times an HDR multiplier.
// Castle_ExtWindows.png (tools/castle_exterior_atlas.py): dark leading, glass core #d9933a falling to #5a2e0a at the edges. _Intensity 1.5
// keeps the hottest channel under 2.0 (bloom-friendly if a post stack ever reads it). Same family as CastleEmissive, with a texture.
Shader "HollowCurrent/CastleWindow"
{
    Properties
    {
        _MainTex ("Window atlas", 2D) = "white" {}
        _Intensity ("Emission multiplier (HDR)", Range(0, 2)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            half _Intensity;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            half4 frag (v2f i) : SV_Target
            {
                half4 c = tex2D(_MainTex, i.uv);
                return half4(c.rgb * _Intensity, 1.0);
            }
            ENDCG
        }
    }
}
