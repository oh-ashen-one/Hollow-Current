// Unlit additive flame shader (premultiplied by alpha, so a soft teardrop sprite adds warm light and never a card).
// Lives in Resources so it is always in a player build.
//
// ONE QUAD PER FLAME (round 9): the sprite holds the flame core and its halo, so the two can never separate, and the quad is built
// here in the vertex stage, in the view plane of the camera that is actually rendering, around the wick (the object's origin). Nothing
// on the CPU (a camera that moved after Update, facing code, Floater rotations of the transform) can leave it facing the wrong way
// or off its wick. Mesh: x -0.9..0.9, y -0.4..1.4 (in flame heights; y = 0 is the wick, 1 the tip), scaled by the object's x/y axes.
//
//   axis  = world up flattened against the sight line (the candle's own axis as it appears on screen)
//   right = axis x view
//   lean  = a rigid rotation of the whole quad about the view axis, by _LeanAmount degrees (clamped to 45) toward the side of the
//           screen the Count is on (_LeanDir.xyz = world direction to the Count, .w = side hint when he is dead ahead / behind)
//   world = wick + right' * (x * sx) + axis' * (y * sy)
//   size clamp (round 13, r12 critic: "near the camera flame quads become large flat orange teardrops"): the flame height sy is clamped to
//         [_MinScreen, _MaxScreen] of the screen height at the flame's distance (height of the view at depth d = 2 d / P11), the width follows,
//         and a flame closer than _NearFade.y metres dissolves (fully gone at _NearFade.x). Pure vertex-stage maths: nothing on the CPU,
//         nothing per frame, no allocation. CandleFlame.FlameScaleFactor mirrors it for the self-tests.
//
// The wick (x = y = 0) is the pivot of the rotation, so it never moves; the flame keeps its length. _LeanAmount = 0 leaves the quad upright.
Shader "HollowCurrent/CastleFlame"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _LeanDir ("Lean direction (world, w = side hint)", Vector) = (0,0,0,1)
        _LeanAmount ("Lean amount (degrees)", Float) = 0
        _MaxScreen ("Max flame height (fraction of screen height)", Range(0.01, 0.3)) = 0.055
        _MinScreen ("Min flame height (fraction of screen height)", Range(0, 0.02)) = 0.004
        _NearFade ("Near fade: gone at x metres, full at y metres", Vector) = (0.30, 0.80, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" "IgnoreProjector"="True" "DisableBatching"="True" }
        Cull Off
        ZWrite Off
        Blend One One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _MaxScreen, _MinScreen;
            float4 _NearFade;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _LeanDir)
                UNITY_DEFINE_INSTANCED_PROP(float, _LeanAmount)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; half fade : TEXCOORD1; };

            v2f vert (appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                float3 pivot = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                float sx = length(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20));
                float sy = length(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21));
                float depth = max(0.02, -mul(UNITY_MATRIX_V, float4(pivot, 1.0)).z);           // distance along the view axis
                float metresPerScreen = 2.0 * depth / max(0.1, abs(unity_CameraProjection._m11));   // world height of the view at this depth
                float sc = clamp(sy, _MinScreen * metresPerScreen, max(_MinScreen, _MaxScreen) * metresPerScreen) / max(sy, 1e-5);
                sx *= sc; sy *= sc;
                o.fade = (half)saturate((depth - _NearFade.x) / max(0.01, _NearFade.y - _NearFade.x));
                float3 view = normalize(pivot - _WorldSpaceCameraPos);
                float3 axis = float3(0, 1, 0) - view * view.y;                       // world up flattened against the sight line
                axis = dot(axis, axis) > 1e-4 ? normalize(axis) : normalize(UNITY_MATRIX_V[1].xyz);
                float3 right = normalize(cross(axis, view));                          // Unity: right = up x forward
                float4 lean = UNITY_ACCESS_INSTANCED_PROP(Props, _LeanDir);
                float amount = UNITY_ACCESS_INSTANCED_PROP(Props, _LeanAmount);
                if (amount > 0.01)
                {
                    float side = dot(lean.xyz, right);
                    float sgn = abs(side) > 0.05 ? sign(side) : (lean.w >= 0 ? 1.0 : -1.0);
                    float a = radians(min(amount, 45.0));
                    float3 axisLean = axis * cos(a) + right * (sgn * sin(a));
                    float3 rightLean = right * cos(a) - axis * (sgn * sin(a));
                    axis = axisLean; right = rightLean;
                }
                float3 world = pivot + right * (v.vertex.x * sx) + axis * (v.vertex.y * sy);
                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1.0));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                return fixed4(c.rgb * c.a, c.a) * i.fade;
            }
            ENDCG
        }
    }
}
