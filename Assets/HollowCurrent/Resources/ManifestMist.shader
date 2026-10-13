Shader "HollowCurrent/ManifestMist" {
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct input {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct output {float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   output vert(input v){output o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o;}
   fixed4 frag(output i):SV_Target {float r=length(i.uv-.5)*2;return fixed4(i.color.rgb,i.color.a*pow(saturate(1-r*r),2));}
   ENDCG
  }
 }
}
