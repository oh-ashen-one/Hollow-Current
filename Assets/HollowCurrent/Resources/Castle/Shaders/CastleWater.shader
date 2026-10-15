// Moat water for the Haunted Castle exterior (wave L, hc/exterior-l): a dark blood-tinted mirror, not a flat red strip.
// Unlit, opaque. Five analytic ripple waves bend the normal (no texture, so tools/castle_exterior_composite.py reproduces it exactly);
// Schlick fresnel mixes a deep blood body with a reflection of (a) the SkyCastle night sky rebuilt analytically: horizon gradient, the pale
// and red moons at their sky directions, the aurora ribbon, and (b) a crude ray-vs-box reflection of the castle itself (dark mass), so
// the water shows moon columns and a dark castle reflection instead of a flat tint. World space = castle-local space (root at the origin).
// Fog is applied like every other surface. Constants are linear values of the castle_sky.py display colours (display ^ 2.2).
Shader "HollowCurrent/CastleWater"
{
    Properties
    {
        _Deep ("Blood body (linear)", Color) = (0.016, 0.0018, 0.004, 1)
        _Reflect ("Reflection strength", Range(0, 4)) = 2.2
        _Ripple ("Ripple slope scale", Range(0, 2)) = 0.55
        _CastleTint ("Castle reflection (linear)", Color) = (0.012, 0.016, 0.030, 1)
        _Time2 ("Time offset (proofs)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            float4 _Deep, _CastleTint; float _Reflect, _Ripple, _Time2;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; UNITY_FOG_COORDS(1) };
            v2f vert (appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            // gradient of five travelling sine waves (direction, wavenumber, speed, amplitude)
            float2 Ripple (float2 p, float t)
            {
                float2 g = 0;
                #define WAVE(dx, dz, k, w, a) g += (a) * (k) * float2(dx, dz) * cos((k) * (dx * p.x + dz * p.y) + (w) * t);
                WAVE( 0.80,  0.60,  3.1, 1.1, 0.030)
                WAVE(-0.50,  0.87,  4.7, 1.6, 0.022)
                WAVE( 0.20, -0.98,  7.3, 2.1, 0.012)
                WAVE(-0.90, -0.40, 11.0, 2.7, 0.007)
                WAVE( 0.60, -0.80,  2.1, 0.7, 0.040)
                return g;
            }

            static const float3 MOON_PALE = float3(-0.3207, 0.8192, 0.4755);   // az -34, el 55
            static const float3 MOON_RED  = float3( 0.4973, 0.6691, 0.5522);   // az +42, el 42

            float3 SkyColor (float3 R)
            {
                float el = R.y;
                float3 hor = float3(0.0033, 0.0063, 0.0203), zen = float3(0.00001, 0.00003, 0.00033);
                float3 c = lerp(hor, zen, smoothstep(0.0, 0.7, el));
                float a1 = acos(clamp(dot(R, MOON_PALE), -1, 1)), a2 = acos(clamp(dot(R, MOON_RED), -1, 1));
                c += float3(0.50, 0.57, 0.70) * smoothstep(0.215, 0.185, a1) * 0.9 + float3(0.05, 0.085, 0.26) * exp(-a1 / 0.16) * 0.9;
                c += float3(0.42, 0.010, 0.004) * smoothstep(0.085, 0.065, a2) * 0.9 + float3(0.20, 0.008, 0.008) * exp(-a2 / 0.07) * 0.8;
                float az = atan2(R.x, R.z);
                float center = radians(31.0) + radians(9.0) * sin(az * 2.3 + 0.7) + radians(2.5) * sin(az * 7.0);
                float dz = (el - center) / radians(8.0);
                float win = smoothstep(radians(-18.0), radians(10.0), az) * (1.0 - smoothstep(radians(62.0), radians(100.0), az));
                c += float3(0.006, 0.86, 0.23) * exp(-dz * dz * 1.2) * win * (0.5 + 0.5 * sin(az * 5.0 + 1.3)) * 0.16;
                return c;
            }

            // does the reflected ray from P hit the castle's mass? slab test against three boxes (main body, hall roof block, keep)
            bool HitBox (float3 P, float3 R, float3 mn, float3 mx)
            {
                float3 inv = 1.0 / (R + float3(1e-5, 1e-5, 1e-5));
                float3 t0 = (mn - P) * inv, t1 = (mx - P) * inv;
                float3 tmin = min(t0, t1), tmax = max(t0, t1);
                float tn = max(max(tmin.x, tmin.y), tmin.z), tf = min(min(tmax.x, tmax.y), tmax.z);
                return tf > max(tn, 0.0);
            }

            half4 frag (v2f i) : SV_Target
            {
                float3 P = i.wpos;
                float3 V = normalize(_WorldSpaceCameraPos - P);
                float2 g = Ripple(P.xz, _Time.y + _Time2) * _Ripple;
                float3 N = normalize(float3(-g.x, 1.0, -g.y));
                float3 R = reflect(-V, N); R.y = abs(R.y);
                float ndv = saturate(dot(N, V));
                float F = 0.03 + 0.97 * pow(1.0 - ndv, 5.0);
                float3 refl = SkyColor(R);
                if (HitBox(P, R, float3(-14.45, -3.2, -0.45), float3(14.45, 7.2, 28.45)) ||
                    HitBox(P, R, float3(-6.0, -3.2, 14.0), float3(6.0, 9.2, 28.45)) ||
                    HitBox(P, R, float3(-0.5, -3.2, 17.6), float3(5.1, 32.0, 23.2)))
                    refl = _CastleTint.rgb;
                float3 col = _Deep.rgb * (1.0 - F * 0.6) + refl * float3(1.0, 0.86, 0.82) * F * _Reflect;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return half4(col, 1);
            }
            ENDCG
        }
    }
}
