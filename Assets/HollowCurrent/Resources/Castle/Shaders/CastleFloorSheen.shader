// Wave FD (hc/fd-art, r13 critic item 3, S1: "there is no floor sheen"): a wet-stone sheen for the great hall floor, as one additive quad.
// Analytic reflections only (no reflection camera, no render texture): each of eight sources (_S0.._S7: xyz castle-local position, w > 0 a
// warm candle of that intensity, w < 0 a cold window) adds a glossy lobe where the floor's mirror ray points at it, masked by a puddle noise
// (wet patches strong, dry stone faint) and a Schlick fresnel. It only ADDS light where a reflection is (the black floor stays black), so the
// lantern-off exposure band is not lifted except along the reflection streaks. Fog fades the sheen toward black with distance.
// Wave FF (hc/ff-art, r14 critic: "the floor is matte"; test: the rose window's mirror as a bright cluster below the horizon near its x): a
// window (w < 0) also throws a stretched streak, the elongated highlight of rough wet stone at a grazing view: narrow across (the window's
// own half-width over its distance plus _StreakWidth, in azimuth), long along the view (_StreakLen radians of elevation), so the cold pillar
// runs from under the window toward the viewer along every floor and table point that lines up with it, as on a wet road at night. A sharp
// mirror image of the rose would lie 30-39 degrees below S1's horizon, under the skeleton; the streak is what shows around it. _WetMin is the
// dry-stone floor of the puddle mask (the polished table-top instance uses .9).
Shader "HollowCurrent/CastleFloorSheen"
{
    Properties
    {
        _MainTex ("Puddle noise (A)", 2D) = "white" {}
        _Tile ("Noise tiles per metre", Float) = 0.21
        _Strength ("Strength", Range(0, 4)) = 1
        _Rough ("Roughness (lobe width)", Range(0.005, 0.3)) = 0.045
        _Warm ("Candle colour (linear)", Color) = (1.0, 0.52, 0.20, 1)
        _Cold ("Window colour (linear)", Color) = (0.22, 0.32, 0.62, 1)
        _Streak ("Window streak weight", Range(0, 4)) = 1.0
        _StreakLen ("Window streak length (elevation, rad)", Range(0.05, 2)) = 0.9
        _StreakWidth ("Window streak width (azimuth, rad)", Range(0.002, 0.3)) = 0.035
        _WinHalf ("Window half-width (m)", Range(0, 2)) = 0.6
        _WetMin ("Dry floor of the puddle mask", Range(0, 1)) = 0.15
        _S0 ("Source 0", Vector) = (0, 0, 0, 0)
        _S1 ("Source 1", Vector) = (0, 0, 0, 0)
        _S2 ("Source 2", Vector) = (0, 0, 0, 0)
        _S3 ("Source 3", Vector) = (0, 0, 0, 0)
        _S4 ("Source 4", Vector) = (0, 0, 0, 0)
        _S5 ("Source 5", Vector) = (0, 0, 0, 0)
        _S6 ("Source 6", Vector) = (0, 0, 0, 0)
        _S7 ("Source 7", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent-20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Tile, _Strength, _Rough, _Streak, _StreakLen, _StreakWidth, _WinHalf, _WetMin;
            float4 _Warm, _Cold, _S0, _S1, _S2, _S3, _S4, _S5, _S6, _S7;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; UNITY_FOG_COORDS(1) };
            v2f vert (appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            float3 Src (float4 S, float3 P, float3 R)
            {
                if (S.w == 0) return 0;
                float3 d = S.xyz - P; float dist = length(d); d /= dist;
                float k = _Rough * _Rough + (0.12 / dist) * (0.12 / dist);   // a nearby source looks bigger
                float lobe = exp((dot(R, d) - 1.0) / k);
                if (S.w < 0)
                {
                    float daz = atan2(d.x, d.z) - atan2(R.x, R.z);
                    daz = daz - 6.2831853 * floor((daz + 3.14159265) / 6.2831853);
                    float del = asin(clamp(d.y, -1, 1)) - asin(clamp(R.y, -1, 1));
                    float wa = _StreakWidth + _WinHalf / dist;
                    lobe = max(lobe, _Streak * exp(-daz * daz / (wa * wa) - del * del / (_StreakLen * _StreakLen)));
                }
                float3 c = S.w > 0 ? _Warm.rgb : _Cold.rgb;
                return c * abs(S.w) * lobe / (1.0 + dist * 0.12);
            }
            half4 frag (v2f i) : SV_Target
            {
                float3 P = i.wpos;
                float3 V = normalize(_WorldSpaceCameraPos - P);
                float2 uv = P.xz * _Tile;
                float a = tex2D(_MainTex, uv).a, ax = tex2D(_MainTex, uv + float2(0.01, 0)).a, az = tex2D(_MainTex, uv + float2(0, 0.01)).a;
                float wet = smoothstep(0.38, 0.62, a) * (1.0 - _WetMin) + _WetMin;
                float3 N = normalize(float3((a - ax) * 0.9, 1.0, (a - az) * 0.9));     // the puddle edges tilt the mirror a little
                float3 R = reflect(-V, N);
                float F = 0.04 + 0.96 * pow(1.0 - saturate(dot(N, V)), 5.0);
                float3 col = Src(_S0, P, R) + Src(_S1, P, R) + Src(_S2, P, R) + Src(_S3, P, R) + Src(_S4, P, R) + Src(_S5, P, R) + Src(_S6, P, R) + Src(_S7, P, R);
                col *= wet * F * _Strength;
                UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0, 0, 0, 0));
                return half4(col, 1);
            }
            ENDCG
        }
    }
}
