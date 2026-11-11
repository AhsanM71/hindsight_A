Shader "EyeCanDo/GardenSky"
{
 Properties { _Top("Sky blue", Color)=(0.12,0.58,0.89,1) _Bottom("Horizon", Color)=(0.75,0.94,0.93,1) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Background" }
  Pass
  {
   Cull Off ZWrite Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
   CBUFFER_START(UnityPerMaterial)
   half4 _Top; half4 _Bottom;
   CBUFFER_END
   Varyings vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;return o; }
   half4 frag(Varyings i):SV_Target { return lerp(_Bottom,_Top,smoothstep(0.1,1.0,i.uv.y)); }
   ENDHLSL
  }
 }
}
