// Moat / blood-lake water for the Haunted Castle exterior. Unlit, opaque, one plane per moat strip, no extra camera, no render texture.
// Wave L (hc/exterior-l): dark blood body, five analytic ripple waves (no texture, so tools/castle_exterior_composite.py reproduces it exactly),
// Schlick fresnel, a reflection of the SkyCastle night sky rebuilt analytically (horizon gradient, both moons, the aurora) and a ray-vs-box
// reflection of the castle mass.
// Wave FD (hc/fd-art, r13 critic item 3: "the moat is a grey fog plane, not red and not reflective"):
//   * blood mirror: the reflected sky is taken as luminance and re-tinted with _BloodTint, so every reflection reads red (R/G well above 1.6)
//     instead of the blue horizon the r13 water showed; the moon discs keep a little of their own colour;
//   * reflected skyline: rays that leave the water below the ridge line (CastleBackdrop's ring, a cheap analytic band) reflect the dark ridge
//     haze, so the far shore and the mountains show in the water as a darker band under a brighter sky reflection;
//   * art-directed moon glade: a glitter column at the pale moon's azimuth with an effective elevation of _GladeElev degrees (a mirror image of
//     the real 55 degree moon would only appear 55 degrees below the horizon, outside every eye-level frame). When the moon is behind the camera
//     (gate_out_S6 looks south, the moons are in the north sky) the glade is placed 21 degrees off the view axis on the moon's side. Documented cheat;
//   * lamp glints: up to four warm point sources (_Lamp0.._Lamp3: xyz castle-local position, w intensity; the far-bank braziers and the causeway
//     lamp posts, set by CastleExterior) give the elongated fire reflections of the reference;
//   * the scene's exponential fog is applied at _FogKeep strength only, so the water does not dissolve into the grey mist.
// Wave FE (hc/fe-exterior, r13 moat test: bright glint area 273 / 263 / 201 px2 in gate_out / S4 / battlement against >= 400; "the lake reads as
// flat saturated red with one white sparkle patch"):
//   * a second glade for the blood moon at its own azimuth (+42 deg; red-gold, hot core), under the same art-directed elevation and the same
//     behind-the-camera rule, so every view over the lake has a glade on each side of the axis instead of one patch;
//   * the pale glade is a little wider (_GladeRough .20 -> .26) and brighter, its core reads as a broken column instead of a dot field;
//   * glitter: one hashed sparkle field (world cells re-rolled 4 times a second, a share of them lit, each a small round point at a random spot
//     in its cell, only on chop facets that face the half-vector) scattered through a halo round each glade and along each lamp column, so the
//     reflections break into hot points the way rippled water throws a light (fe r1's square 10 cm cells read as confetti; r2 makes them points);
//   * the smooth glade lobe is weaker (_GladeBody .8 -> .45) so the water between the glints stays blood-red;
//   * values from the fe r2 -hc-moat sweep (bright px2 gate_out / S4 / battlement: r2 defaults 650 / 386 / 1250; density .26 + glitter 3.2:
//     930 / 636 / 2312; the pass line is 400)
//   * the castle-end lamps throw twice the glint (CastleExterior.Lamps) and every lamp column sparkles.
// Cost: still one opaque pass, no texture, no render target: two extra half-vector lobes and one hash per pixel.
// World space = castle-local space (the castle root sits at the origin). Constants are linear values of the castle_sky.py display colours.
Shader "HollowCurrent/CastleWater"
{
    Properties
    {
        _Deep ("Blood body (linear)", Color) = (0.050, 0.0034, 0.0040, 1)
        _Reflect ("Reflection strength", Range(0, 4)) = 2.2
        _Ripple ("Ripple slope scale", Range(0, 2)) = 0.55
        _CastleTint ("Castle reflection (linear)", Color) = (0.010, 0.012, 0.022, 1)
        _RockTint ("Rock outcrop reflection (linear)", Color) = (0.0035, 0.0040, 0.0065, 1)
        _BloodTint ("Reflection tint (luminance x tint, linear)", Color) = (5.6, 0.45, 0.40, 1)
        _MoonKeep ("Moon disc own colour kept", Range(0, 1)) = 0.35
        _Ridge ("Reflected ridge haze (linear)", Color) = (0.0040, 0.0060, 0.0140, 1)
        _Glade ("Moon glade strength", Range(0, 4)) = 1.0
        _GladeElev ("Moon glade effective elevation (deg)", Range(2, 60)) = 13
        _GladeRough ("Moon glade roughness (slope)", Range(0.02, 0.6)) = 0.26
        _GladeColor ("Moon glade colour (linear)", Color) = (0.62, 0.52, 0.50, 1)
        _RedGlade ("Blood moon glade strength", Range(0, 4)) = 0.9
        _RedGladeColor ("Blood moon glade colour (linear)", Color) = (1.0, 0.30, 0.20, 1)
        _GladeBody ("Smooth glade lobe weight", Range(0, 2)) = 0.45
        _Glitter ("Glitter strength (hot points in the glade halos and lamp columns)", Range(0, 8)) = 3.2
        _GlitterDensity ("Glitter share of cells lit", Range(0, 0.6)) = 0.26
        _GlitterCell ("Glitter cells per metre", Range(2, 40)) = 12
        _GlitterHalo ("Glitter halo width (x glade roughness)", Range(1, 3)) = 1.6
        _GlitterFacet ("Glitter facet power (chop normal . half-vector)", Range(0, 64)) = 6
        _LampColor ("Lamp glint colour (linear)", Color) = (1.0, 0.36, 0.08, 1)
        _Lamp0 ("Lamp 0 (xyz pos, w intensity)", Vector) = (0, 0, 0, 0)
        _Lamp1 ("Lamp 1", Vector) = (0, 0, 0, 0)
        _Lamp2 ("Lamp 2", Vector) = (0, 0, 0, 0)
        _Lamp3 ("Lamp 3", Vector) = (0, 0, 0, 0)
        _FogKeep ("Scene fog applied (fraction)", Range(0, 1)) = 0.45
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

            float4 _Deep, _CastleTint, _RockTint, _BloodTint, _Ridge, _GladeColor, _LampColor, _Lamp0, _Lamp1, _Lamp2, _Lamp3, _RedGladeColor;
            float _Reflect, _Ripple, _Time2, _MoonKeep, _Glade, _GladeElev, _GladeRough, _FogKeep, _RedGlade, _Glitter, _GlitterDensity;
            float _GladeBody, _GlitterCell, _GlitterHalo, _GlitterFacet;
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
            // short choppy waves for the glitter only (not used for the body normal, so the mirror stays calm)
            float2 Chop (float2 p, float t)
            {
                float2 g = 0;
                WAVE( 0.31,  0.95, 19.0, 3.3, 0.010)
                WAVE(-0.83,  0.56, 23.0, 3.9, 0.008)
                WAVE( 0.97, -0.24, 29.0, 4.4, 0.006)
                WAVE(-0.45, -0.89, 37.0, 5.1, 0.005)
                return g;
            }

            static const float3 MOON_PALE = float3(-0.3207, 0.8192, 0.4755);   // az -34, el 55
            static const float3 MOON_RED  = float3( 0.4973, 0.6691, 0.5522);   // az +42, el 42

            float3 SkyColor (float3 R, out float moon)
            {
                float el = R.y;
                float3 hor = float3(0.0033, 0.0063, 0.0203), zen = float3(0.00001, 0.00003, 0.00033);
                float3 c = lerp(hor, zen, smoothstep(0.0, 0.7, el));
                float a1 = acos(clamp(dot(R, MOON_PALE), -1, 1)), a2 = acos(clamp(dot(R, MOON_RED), -1, 1));
                float d1 = smoothstep(0.215, 0.185, a1), d2 = smoothstep(0.085, 0.065, a2);
                c += float3(0.50, 0.57, 0.70) * d1 * 0.9 + float3(0.05, 0.085, 0.26) * exp(-a1 / 0.16) * 0.9;
                c += float3(0.42, 0.010, 0.004) * d2 * 0.9 + float3(0.20, 0.008, 0.008) * exp(-a2 / 0.07) * 0.8;
                moon = saturate(d1 + d2);
                float az = atan2(R.x, R.z);
                float center = radians(31.0) + radians(9.0) * sin(az * 2.3 + 0.7) + radians(2.5) * sin(az * 7.0);
                float dz = (el - center) / radians(8.0);
                float win = smoothstep(radians(-18.0), radians(10.0), az) * (1.0 - smoothstep(radians(62.0), radians(100.0), az));
                c += float3(0.006, 0.86, 0.23) * exp(-dz * dz * 1.2) * win * (0.5 + 0.5 * sin(az * 5.0 + 1.3)) * 0.16;
                return c;
            }

            // does the reflected ray from P hit a box? (slab test)
            bool HitBox (float3 P, float3 R, float3 mn, float3 mx)
            {
                float3 inv = 1.0 / (R + float3(1e-5, 1e-5, 1e-5));
                float3 t0 = (mn - P) * inv, t1 = (mx - P) * inv;
                float3 tmin = min(t0, t1), tmax = max(t0, t1);
                float tn = max(max(tmin.x, tmin.y), tmin.z), tf = min(min(tmax.x, tmax.y), tmax.z);
                return tf > max(tn, 0.0);
            }

            // warm glint of one lamp: a tight specular on the rippled normal plus a soft column on the calm surface; wave FE: lit glitter cells
            // (`spark`) along a column twice as wide
            float Lamp (float4 L, float3 P, float3 V, float3 Nr, float spark)
            {
                if (L.w <= 0) return 0;
                float3 d = L.xyz - P; float dist = length(d); d /= dist;
                float3 H = normalize(V + d);
                float c = max(H.y, 1e-3), t2 = (1.0 - c * c) / (c * c);
                float soft = exp(-t2 / (0.11 * 0.11));
                float sharp = pow(saturate(dot(Nr, H)), 400.0) * exp(-t2 / (0.30 * 0.30));   // glints only along the reflection column
                float glitter = spark * exp(-t2 / (0.24 * 0.24)) * pow(saturate(dot(Nr, H)), _GlitterFacet) * _Glitter;
                return L.w * (soft * 0.55 + sharp * 3.0 + glitter) * saturate(30.0 / (dist + 6.0));
            }

            // the glade's azimuth: the moon's own, or (moon behind the camera) 21 degrees beside the view axis on the moon's side
            float2 GladeAzimuth (float2 m2, float2 f2)
            {
                if (dot(m2, f2) < 0)
                {
                    float2 r2 = float2(f2.y, -f2.x);
                    m2 = normalize(f2 + 0.38 * (dot(m2, r2) >= 0 ? r2 : -r2));
                }
                return m2;
            }

            // one art-directed glade: x = soft column + rippled glints (wave FD's terms), y = glitter weight in a wider halo, on facing facets
            float2 Glade (float2 m2, float3 V, float3 Nr, float spark, float sparkCell)
            {
                float ce = cos(radians(_GladeElev)), se = sin(radians(_GladeElev));
                float3 Lg = float3(m2.x * ce, se, m2.y * ce);
                float3 Hg = normalize(V + Lg);
                float hc = max(Hg.y, 1e-3), ht2 = (1.0 - hc * hc) / (hc * hc);
                float r2 = _GladeRough * _GladeRough;
                float lobe = exp(-ht2 / r2);
                float halo = exp(-ht2 / (r2 * _GlitterHalo * _GlitterHalo));
                float ndh = saturate(dot(Nr, Hg));
                float glint = pow(ndh, 160.0);
                float body = lobe * _GladeBody + glint * lobe * 2.2 * (0.35 + spark) + glint * 0.2 * spark * saturate(lobe * 8.0);
                return float2(body, sparkCell * halo * pow(ndh, _GlitterFacet));
            }

            half4 frag (v2f i) : SV_Target
            {
                float3 P = i.wpos;
                float3 V = normalize(_WorldSpaceCameraPos - P);
                float t = _Time.y + _Time2;
                float2 g = Ripple(P.xz, t) * _Ripple;
                float3 N = normalize(float3(-g.x, 1.0, -g.y));
                float2 gc = g + Chop(P.xz, t);
                float3 Nr = normalize(float3(-gc.x, 1.0, -gc.y));
                float3 R = reflect(-V, N); R.y = abs(R.y);
                float ndv = saturate(dot(N, V));
                float F = 0.03 + 0.97 * pow(1.0 - ndv, 5.0);

                float moon;
                float3 sky = SkyColor(R, moon);
                // reflected skyline: the backdrop ring (ridges and banks) stands about 6-14 degrees above the water from the moat
                float az = atan2(R.x, R.z);
                float skyline = 0.10 + 0.06 * sin(az * 3.1 + 0.4) + 0.035 * sin(az * 7.3 + 1.1) + 0.02 * sin(az * 17.0);
                float under = 1.0 - smoothstep(skyline - 0.012, skyline + 0.012, R.y);
                sky = lerp(sky, _Ridge.rgb, under);
                if (HitBox(P, R, float3(-14.45, -3.2, -0.45), float3(14.45, 7.2, 28.45)) ||
                    HitBox(P, R, float3(-6.0, -3.2, 14.0), float3(6.0, 9.2, 28.45)) ||
                    HitBox(P, R, float3(-0.5, -3.2, 17.6), float3(5.1, 32.0, 23.2)))
                    sky = _CastleTint.rgb;
                // the rock outcrop round the castle's foot (tools/castle_exterior.py apron, up to ~7 m out and ~5 m high): a dark band under the walls
                else if (HitBox(P, R, float3(-21.5, -3.0, -6.6), float3(21.5, 3.6, 35.0))) sky = _RockTint.rgb;
                // the causeway deck and its rails (CastleExterior.Approach): x +-2.3, deck top 0, rails to 1.0, z -16 .. -6.3
                if (HitBox(P, R, float3(-2.3, -1.6, -16.0), float3(2.3, 1.0, -6.3))) sky = _CastleTint.rgb * 0.6;

                float lum = dot(sky, float3(0.2126, 0.7152, 0.0722));
                float3 refl = lum * _BloodTint.rgb + sky * moon * _MoonKeep * 3.0;
                float3 col = _Deep.rgb * (1.0 - F * 0.6) + refl * F * _Reflect;

                // art-directed moon glades (see the header): the pale moon's and (wave FE) the blood moon's, each at its own azimuth, mirrored
                // beside the view axis when the moon is behind the camera
                float3 fwd = -UNITY_MATRIX_V[2].xyz;
                float2 f2 = normalize(fwd.xz + float2(1e-5, 0));
                // break the regular chop pattern: only some 12 cm cells sparkle at a time (wave FD)
                float2 cell = floor(P.xz * 8.0);
                float spark = step(0.55, frac(sin(dot(cell, float2(12.9898, 78.233)) + floor(t * 3.0) * 1.7) * 43758.5453));
                // wave FE glitter: cells of 1/_GlitterCell m, a share _GlitterDensity of them lit at any moment, re-rolled four times a second;
                // a lit cell is a small round point at a random spot inside it
                float2 gp = P.xz * _GlitterCell + 0.37;
                float2 gcell = floor(gp);
                float ts = floor(t * 4.0);
                float gh = frac(sin(dot(gcell, float2(39.3468, 11.1353)) + ts * 2.3) * 24634.6345);
                float2 gofs = frac(sin(float2(dot(gcell, float2(73.156, 52.235)), dot(gcell, float2(17.719, 91.417))) + ts * 1.3) * 43758.5453);
                float dotMask = 1.0 - smoothstep(0.17, 0.30, length(gp - (gcell + 0.2 + 0.6 * gofs)));
                float sparkCell = step(1.0 - _GlitterDensity, gh) * dotMask;
                float2 pale = Glade(GladeAzimuth(normalize(MOON_PALE.xz), f2), V, Nr, spark, sparkCell);
                float2 red = Glade(GladeAzimuth(normalize(MOON_RED.xz), f2), V, Nr, spark, sparkCell);
                col += _GladeColor.rgb * _Glade * (pale.x + pale.y * _Glitter);
                col += _RedGladeColor.rgb * _RedGlade * (red.x + red.y * _Glitter);

                float3 Nl = Nr;
                float lamps = Lamp(_Lamp0, P, V, Nl, sparkCell) + Lamp(_Lamp1, P, V, Nl, sparkCell) + Lamp(_Lamp2, P, V, Nl, sparkCell) + Lamp(_Lamp3, P, V, Nl, sparkCell);
                col += _LampColor.rgb * lamps;

                float3 fogged = col;
                UNITY_APPLY_FOG(i.fogCoord, fogged);
                col = lerp(col, fogged, _FogKeep);
                return half4(col, 1);
            }
            ENDCG
        }
    }
}
