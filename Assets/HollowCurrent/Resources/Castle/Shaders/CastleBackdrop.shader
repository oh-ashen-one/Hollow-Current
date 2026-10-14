// Distant ridge ring for the Haunted Castle exterior (hc/art-exterior). Unlit, ignores fog, alpha-blended, never writes depth.
// The Transparent queue keeps it out of the lightmap bake (HouseLighting skips renderQueue >= Transparent).
// UV.x = layer id (0 far ridge, 1 near ridge, 2 mist band), UV.y = height fraction (0 at the base, 1 at the crest).
Shader "HollowCurrent/CastleBackdrop"
{
    Properties
    {
        _FarLow ("Far ridge, low", Color) = (0.040, 0.055, 0.095, 1)
        _FarHigh ("Far ridge, crest", Color) = (0.016, 0.024, 0.046, 1)
        _NearLow ("Near ridge, low", Color) = (0.022, 0.030, 0.055, 1)
        _NearHigh ("Near ridge, crest", Color) = (0.008, 0.011, 0.022, 1)
        _Haze ("Mist band (alpha = strength at the base)", Color) = (0.10, 0.13, 0.20, 0.38)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _FarLow, _FarHigh, _NearLow, _NearHigh, _Haze;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                float layer = i.uv.x; float v = saturate(i.uv.y);
                fixed4 c;
                if (layer < 0.5) c = lerp(_FarLow, _FarHigh, smoothstep(0.35, 0.95, v));
                else if (layer < 1.5) c = lerp(_NearLow, _NearHigh, smoothstep(0.35, 0.95, v));
                else { c = _Haze; c.a *= pow(1.0 - v, 2.0); }
                return c;
            }
            ENDCG
        }
    }
}
