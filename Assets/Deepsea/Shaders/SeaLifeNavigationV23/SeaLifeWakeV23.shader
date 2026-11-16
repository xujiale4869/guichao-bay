Shader "Deepsea/SeaLifeWakeV23" {
 Properties { [HideInInspector] _MistCapeWakeBreakupV351("Mist Cape wake breakup", Float) = 0 }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+8" "RenderType"="Transparent"}
  Pass {Name "HistoricalWake" Tags {"LightMode"="UniversalForward"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   #include "../SeaSurfaceV24/HarborBoatCabinV401.hlsl"
   CBUFFER_START(UnityPerMaterial)
    float _MistCapeWakeBreakupV351;
   CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;float3 world:TEXCOORD1;};
   V Vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);o.uv=a.uv;o.c=a.c;return o;}
   float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float Noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);}
   half4 Frag(V i):SV_Target {
    float2 boatDelta=i.world.xz-_CoastBoat.xy;
    [branch] if(_CoastV40Presentation>.5 && dot(boatDelta,boatDelta)<25.0){
     float2 uv=GetNormalizedScreenSpaceUV(i.p);
     float3 solid=ComputeWorldSpacePosition(uv,HarborDepthNDC(SampleSceneDepth(uv)),UNITY_MATRIX_I_VP);
     float cabin=HarborCabinMask(i.world,solid);
     clip(cabin-.98);
     i.c.a*=smoothstep(.98,1.20,cabin);
    }
    float noise=Noise(float2(i.uv.y*8.3,i.uv.x*4.7));
    float centre=.5+.08*sin(i.uv.y*5.1);
    float edge=pow(saturate(1-abs(i.uv.x-centre)*2),1.45);
    float foam=.18+.82*smoothstep(.2,.78,noise);
    if(_MistCapeWakeBreakupV351>.5){
     float patches=Noise(float2(i.uv.y*4.7+3.7,i.uv.x*3.3));
     float grain=Noise(float2(i.uv.y*15.1-2.8,i.uv.x*8.7));
     float broken=smoothstep(.39,.79,.62*patches+.38*grain);
     return half4(i.c.rgb*.89,i.c.a*edge*(.09+.91*broken)*.47);
    }
    return half4(i.c.rgb,i.c.a*edge*foam);
   }
   ENDHLSL
  }
 }
}
