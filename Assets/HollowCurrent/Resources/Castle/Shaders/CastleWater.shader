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
// Wave FF (hc/ff-art, r14 critic section 4 and item 1: "the moat is red now but not reflective: a high-frequency glitter (luma sd 29-33) with no
// mirrored moon or castle"; "in battlement_out and bridge_left a second, striped red moat material meets the glitter one at a hard seam"):
//   * the glitter field and the per-cell sparkle are gone (they were the sd 29-33 and the "glitter" read); glints are smooth lobes now;
//   * the seam was not a second material: it was this shader's own hard edges. (a) The rock-outcrop slab test returned "hit" for every water
//     point inside its footprint (z > -6.6) whatever the ray direction, so the water near the castle went dark along the slab face while the
//     rest reflected the sky; the outcrop now only reflects for rays that ENTER it. (b) Every box reflection is weighted by the ray's path
//     length inside the box (_BoxSoft metres to full), so silhouettes in the mirror have soft edges instead of a step. (c) The reflected skyline
//     band had a +-0.012 edge that the ripples turned into saw-tooth stripes; it is +-_SkySoft now. The ripple slope is calmer (_Ripple .55 -> .3)
//     so the mirror holds together;
//   * moon columns: when a moon is in front of the camera its mirror column sits directly under it in the frame (the column is the set of view
//     rays in the plane through the eye that holds the moon direction and the camera's up vector), _GladeWidth wide, its edges wobbled by the
//     ripples. A true mirror image of a 42-55 degree moon would be 42-55 degrees below the horizon, outside every eye-level frame; the column is
//     the reflection a viewer expects, the same documented cheat as the glade it replaces (the glade's azimuth rule stays for moons behind the
//     camera: gate_out_S6 and battlement_out look south, at a weaker _GladeBody). Both moons get one, each about its disc's width
//     (pale .075, blood .07), the blood moon's red (r14 test: R > 120 below the red moon); the scene fog is kept at 30 % (was 45 %) so
//     the water stays dark blood instead of pink mist, with a faint reflected night glow over the mirrored ridge line;
//   * lamp and brazier glints keep only the soft column (the 400-power point glints were hot single pixels).
// Cost: one opaque pass, no texture, no render target; the FF version drops the glitter hash and two lobes and adds two dot products.
// World space = castle-local space (the castle root sits at the origin). Constants are linear values of the castle_sky.py display colours.
// ff-integ (2026-10-19, one tuning pass from a -hc-moat sweep on the FF build): _Reflect 2.2 -> 1.4, _Ripple .3 -> .2, _SkySoft .06 -> .15,
// _RedGlade 1 -> 4 and _RedGladeWidth .07 -> .10 (the blood moon's column read R 85 under the moon, test needs > 120), _GladeBody .25 -> .08
// (the grey lobe beside the red in battlement_out), and the rock outcrop's reflection fades in over _RockFade m (BoxW).
// Wave FG (hc/fg-polish; FF build: S4 sd 12.6 right / 16.3 left, battlement_out step 32.9). Most of those numbers were mask errors: the
// proxy masks of tools/check_art_views.py covered the two foreground shore rocks (the whole left sd patch) and the dark bank rocks above the
// water edge (the battlement step: lamp-lit water against a rock inside the mask); tools/fg_moat_masks.py cuts them out (water alone on the FF
// frames: right 12.6, left 14.5, step 13.4). What is left is low-frequency: the right patch fades from the blood-moon column to the black
// mirrored rock outcrop, the left one steps from a bright sky band to the black mirrored castle mass. So: the mirrored castle and outcrop are
// less black (_CastleTint x2.2, _RockTint x3: still the darkest things in the water), the night glow over the ridge line is off
// (_HorizonGlow .03 -> 0), the ripple slope one notch calmer (_Ripple .2 -> .15), and the lamp glints are tamer: narrower lobe
// (_LampRough .11 -> .085, a property now) and 20 % dimmer (_LampGain .55 -> .45); they stay warm columns.
// tools/ff_moat_twin.py is the numpy twin of this file (same constants, incl. ff-integ's and FG's).
Shader "HollowCurrent/CastleWater"
{
    Properties
    {
        _Deep ("Blood body (linear)", Color) = (0.050, 0.0034, 0.0040, 1)
        _Reflect ("Reflection strength", Range(0, 4)) = 1.4
        _Ripple ("Ripple slope scale", Range(0, 2)) = 0.15
        _CastleTint ("Castle reflection (linear)", Color) = (0.022, 0.0264, 0.0484, 1)
        _RockTint ("Rock outcrop reflection (linear)", Color) = (0.0105, 0.012, 0.0195, 1)
        _BoxSoft ("Reflection silhouette softness (m of ray inside a box to full)", Range(0.05, 6)) = 1.5
        _RockFade ("Rock outcrop reflection fade-in (m of ray before the slab)", Range(0.05, 20)) = 4
        _BloodTint ("Reflection tint (luminance x tint, linear)", Color) = (5.6, 0.45, 0.40, 1)
        _MoonKeep ("Moon disc own colour kept", Range(0, 1)) = 0.35
        _Ridge ("Reflected ridge haze (linear)", Color) = (0.0040, 0.0060, 0.0140, 1)
        _SkySoft ("Reflected skyline edge half-width (R.y)", Range(0.005, 0.2)) = 0.15
        _HorizonGlow ("Reflected night glow above the ridge line (linear luminance)", Range(0, 0.3)) = 0
        _Glade ("Pale moon glade / column strength", Range(0, 4)) = 1.8
        _GladeElev ("Glade effective elevation, moon behind the camera (deg)", Range(2, 60)) = 13
        _GladeRough ("Glade roughness, moon behind the camera (slope)", Range(0.02, 0.6)) = 0.26
        _GladeColor ("Pale moon glade colour (linear)", Color) = (0.62, 0.55, 0.55, 1)
        _RedGlade ("Blood moon glade / column strength", Range(0, 4)) = 4.0
        _RedGladeColor ("Blood moon glade colour (linear)", Color) = (0.30, 0.0045, 0.004, 1)
        _GladeBody ("Glade lobe weight, moon behind the camera", Range(0, 2)) = 0.08
        _GladeWidth ("Pale moon column half-width (sine of the lateral angle)", Range(0.005, 0.3)) = 0.075
        _RedGladeWidth ("Blood moon column half-width", Range(0.005, 0.3)) = 0.10
        _GladeWobble ("Moon column ripple wobble", Range(0, 2)) = 0.6
        _LampColor ("Lamp glint colour (linear)", Color) = (1.0, 0.36, 0.08, 1)
        _LampRough ("Lamp glint lobe width (slope)", Range(0.02, 0.3)) = 0.085
        _LampGain ("Lamp glint gain", Range(0, 2)) = 0.45
        _Lamp0 ("Lamp 0 (xyz pos, w intensity)", Vector) = (0, 0, 0, 0)
        _Lamp1 ("Lamp 1", Vector) = (0, 0, 0, 0)
        _Lamp2 ("Lamp 2", Vector) = (0, 0, 0, 0)
        _Lamp3 ("Lamp 3", Vector) = (0, 0, 0, 0)
        _FogKeep ("Scene fog applied (fraction)", Range(0, 1)) = 0.30
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
            float _Reflect, _Ripple, _Time2, _MoonKeep, _Glade, _GladeElev, _GladeRough, _FogKeep, _RedGlade, _GladeBody, _BoxSoft, _SkySoft;
            float _GladeWidth, _RedGladeWidth, _GladeWobble, _HorizonGlow, _RockFade, _LampRough, _LampGain;
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

            // wave FF: soft box reflection. Weight = the reflected ray's path length inside the box over _BoxSoft (0 on a miss), so a mirrored
            // silhouette fades over the last metre and a half instead of stepping. enterOnly: a water point inside the box's footprint does not count.
            float BoxW (float3 P, float3 R, float3 mn, float3 mx, bool enterOnly)
            {
                float3 inv = 1.0 / (R + float3(1e-5, 1e-5, 1e-5));
                float3 t0 = (mn - P) * inv, t1 = (mx - P) * inv;
                float3 tmin = min(t0, t1), tmax = max(t0, t1);
                float tn = max(max(tmin.x, tmin.y), tmin.z), tf = min(min(tmax.x, tmax.y), tmax.z);
                if (enterOnly && tn <= 0) return 0;
                // ff-integ: an enter-only box (the rock outcrop) fades in with the ray's run to its face, so the water just outside the slab's
                // footprint does not step from sky to rock (the FF build showed that step as a straight line at z -6.6 across S4: sd 13.5 / 16.5)
                float fade = enterOnly ? saturate(tn / _RockFade) : 1.0;
                return saturate((tf - max(tn, 0.0)) / _BoxSoft) * fade;
            }

            // warm glint of one lamp: the soft column on the calm surface only (wave FF dropped the hot 400-power points)
            float Lamp (float4 L, float3 P, float3 V)
            {
                if (L.w <= 0) return 0;
                float3 d = L.xyz - P; float dist = length(d); d /= dist;
                float3 H = normalize(V + d);
                float c = max(H.y, 1e-3), t2 = (1.0 - c * c) / (c * c);
                return L.w * exp(-t2 / (_LampRough * _LampRough)) * _LampGain * saturate(30.0 / (dist + 6.0));   // wave FG: was .11 / .55
            }

            // the moon's reflection. In front of the camera: a column straight under the moon in the frame. Behind it: wave FD's glade, a smooth
            // lobe at the moon's azimuth (or 21 degrees beside the view axis) with an effective elevation of _GladeElev.
            float MoonGlade (float3 M, float3 D, float3 V, float2 g, float3 camUp, float3 camFwd, float width)
            {
                float2 f2 = normalize(camFwd.xz + float2(1e-5, 0));
                float2 m2 = normalize(M.xz);
                if (dot(m2, f2) > 0 && dot(M, camFwd) > 0)
                {
                    float3 n = normalize(cross(camUp, M));
                    float s = dot(D, n) + dot(g, n.xz) * _GladeWobble;
                    // a little stronger toward the horizon (grazing water), never zero at the near edge
                    float graze = 0.55 + 0.45 * smoothstep(-0.35, -0.05, D.y);
                    return exp(-s * s / (width * width)) * graze;
                }
                float2 r2 = float2(f2.y, -f2.x);
                m2 = normalize(f2 + 0.38 * (dot(m2, r2) >= 0 ? r2 : -r2));
                float ce = cos(radians(_GladeElev)), se = sin(radians(_GladeElev));
                float3 Hg = normalize(V + float3(m2.x * ce, se, m2.y * ce));
                float hc = max(Hg.y, 1e-3), ht2 = (1.0 - hc * hc) / (hc * hc);
                return exp(-ht2 / (_GladeRough * _GladeRough)) * _GladeBody;
            }

            half4 frag (v2f i) : SV_Target
            {
                float3 P = i.wpos;
                float3 V = normalize(_WorldSpaceCameraPos - P);
                float t = _Time.y + _Time2;
                float2 g = Ripple(P.xz, t) * _Ripple;
                float3 N = normalize(float3(-g.x, 1.0, -g.y));
                float3 R = reflect(-V, N); R.y = abs(R.y);
                float ndv = saturate(dot(N, V));
                float F = 0.03 + 0.97 * pow(1.0 - ndv, 5.0);

                float moon;
                float3 sky = SkyColor(R, moon);
                // reflected skyline: the backdrop ring (ridges and banks) stands about 6-14 degrees above the water from the moat
                float az = atan2(R.x, R.z);
                float skyline = 0.10 + 0.06 * sin(az * 3.1 + 0.4) + 0.035 * sin(az * 7.3 + 1.1) + 0.02 * sin(az * 17.0);
                float under = 1.0 - smoothstep(skyline - _SkySoft, skyline + _SkySoft, R.y);
                // wave FF: the night glow just above the ridge line, so the far water shows a brighter band over the mirrored ridges
                float glow = _HorizonGlow * (1.0 - smoothstep(skyline, skyline + 0.28, R.y)) * (1.0 - under);
                sky = lerp(sky, _Ridge.rgb, under) + glow * float3(0.30, 0.32, 0.45);
                // the castle mass, then the rock outcrop round its foot (rays that enter it only), then the causeway deck and rails
                float wc = max(max(BoxW(P, R, float3(-14.45, -3.2, -0.45), float3(14.45, 7.2, 28.45), false),
                                   BoxW(P, R, float3(-6.0, -3.2, 14.0), float3(6.0, 9.2, 28.45), false)),
                                   BoxW(P, R, float3(-0.5, -3.2, 17.6), float3(5.1, 32.0, 23.2), false));
                float wr = BoxW(P, R, float3(-21.5, -3.0, -6.6), float3(21.5, 3.6, 35.0), true) * (1.0 - wc);
                sky = lerp(sky, _CastleTint.rgb, wc);
                sky = lerp(sky, _RockTint.rgb, wr);
                float ww = BoxW(P, R, float3(-2.3, -1.6, -16.0), float3(2.3, 1.0, -6.3), false);
                sky = lerp(sky, _CastleTint.rgb * 0.6, ww);
                moon *= saturate(1.0 - wc - wr - ww);

                float lum = dot(sky, float3(0.2126, 0.7152, 0.0722));
                float3 refl = lum * _BloodTint.rgb + sky * moon * _MoonKeep * 3.0;
                float3 col = _Deep.rgb * (1.0 - F * 0.6) + refl * F * _Reflect;

                // moon columns / glades (see the header)
                float3 camUp = UNITY_MATRIX_V[1].xyz, camFwd = -UNITY_MATRIX_V[2].xyz;
                float3 D = -V;
                col += _GladeColor.rgb * _Glade * MoonGlade(MOON_PALE, D, V, g, camUp, camFwd, _GladeWidth);
                col += _RedGladeColor.rgb * _RedGlade * MoonGlade(MOON_RED, D, V, g, camUp, camFwd, _RedGladeWidth);

                float lamps = Lamp(_Lamp0, P, V) + Lamp(_Lamp1, P, V) + Lamp(_Lamp2, P, V) + Lamp(_Lamp3, P, V);
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
