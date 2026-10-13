// Unlit additive flame/glow shader (premultiplied by alpha, so a soft teardrop sprite adds warm light and never a card).
// Lives in Resources so it is always in a player build.
//
// The candle lean lives HERE, in the vertex stage, so nothing that happens to the billboard transform (facing code, batching,
// the baked scene copy, instancing) can cancel it. The quad is a base-pivot billboard (local y runs 0..1 base to tip).
// _LeanDir.xyz is the world-space direction from the flame to the Count (.w = side hint, +1/-1, used when the Count is dead
// ahead or behind), _LeanAmount is the lean in degrees (clamped to 45). Round 8: the flame is ROTATED about its wick, not
// sheared. For a vertex at fraction f (= local y, 0 at the base vertices, 1 at the tip) the offset is
//     right * sign * sin(a) * height * f  -  quadUp * (1 - cos(a)) * height * f
// which is exactly zero at f = 0 (the wick never moves) and keeps the flame's length (a shear stretched it by 1/cos(a), and with
// a = 55 degrees on a candle 0.7 m from the camera put the tip 240 px away: a streak or a petal detached from its wick).
// _LeanAmount = 0 (the default) leaves the quad untouched, which is what the halo (same shader, no property block) gets.
Shader "HollowCurrent/CastleFlame"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _LeanDir ("Lean direction (world, w = side hint)", Vector) = (0,0,0,1)
        _LeanAmount ("Lean amount (degrees)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" "IgnoreProjector"="True" }
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
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                float4 lean = UNITY_ACCESS_INSTANCED_PROP(Props, _LeanDir);
                float amount = UNITY_ACCESS_INSTANCED_PROP(Props, _LeanAmount);
                if (amount > 0.01)
                {
                    float3 view = normalize(world - _WorldSpaceCameraPos);
                    float3 right = normalize(cross(float3(0, 1, 0), view));        // Unity: right = up x forward
                    float3 quadUp = normalize(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21));
                    float side = dot(lean.xyz, right);
                    float sgn = abs(side) > 0.05 ? sign(side) : (lean.w >= 0 ? 1.0 : -1.0);
                    // flame height in metres: the length of the object's y axis in world space
                    float height = length(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21));
                    float a = radians(min(amount, 45.0));
                    float f = v.vertex.y;                                           // 0 at the base vertices, 1 at the tip
                    world += (right * (sgn * sin(a)) - quadUp * (1.0 - cos(a))) * (height * f);
                }
                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1.0));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                return fixed4(c.rgb * c.a, c.a);
            }
            ENDCG
        }
    }
}
