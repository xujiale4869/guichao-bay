#ifndef DEEPSEA_ATMOSPHERE_GEOMETRY_V37
#define DEEPSEA_ATMOSPHERE_GEOMETRY_V37
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    half4 color : COLOR;
};
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 world : TEXCOORD0;
    float2 uv : TEXCOORD1;
    half4 color : COLOR;
};

Varyings Vert(Attributes a)
{
    Varyings o;
    o.world = TransformObjectToWorld(a.positionOS);
    o.positionCS = TransformWorldToHClip(o.world);
    o.uv = a.uv;
    o.color = a.color;
    return o;
}
#endif
