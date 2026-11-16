#ifndef DEEPSEA_HARBOR_BOAT_CABIN_V401
#define DEEPSEA_HARBOR_BOAT_CABIN_V401

float _CoastV40Presentation;
float4x4 _CoastV40HullWorldToLocal;
float4 _CoastBoat;

float3 HarborCabinLocal(float3 positionWS)
{
    return mul(_CoastV40HullWorldToLocal, float4(positionWS, 1.0)).xyz;
}

float HarborCabinOutline(float3 local)
{
    // Fitted to the Rowboat mesh sections. The stern keeps more beam than the bow.
    float bow = saturate((local.z + 0.35) / 2.42);
    float stern = saturate((-local.z - 0.35) / 1.72);
    float beam = lerp(0.43, 0.78, saturate((local.y + 0.045) / 0.43));
    float halfWidth = max(0.025, beam * (1.0 - 0.96 * bow * bow - 0.40 * stern * stern) - 0.036);
    return max(abs(local.x) / halfWidth, abs(local.z + 0.015) / 2.01);
}

float HarborDepthNDC(float rawDepth)
{
    #if !UNITY_REVERSED_Z
    rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
    #endif
    return rawDepth;
}

float HarborCabinMask(float3 waterWS, float3 opaqueWS)
{
    float3 water = HarborCabinLocal(waterWS);
    float3 solid = HarborCabinLocal(opaqueWS);
    float atWater = water.y >= -0.10 && water.y <= 0.66 ? HarborCabinOutline(water) : 99.0;
    float atSolid = solid.y >= -0.08 && solid.y <= 0.68 ? HarborCabinOutline(solid) : 99.0;
    return min(atWater, atSolid);
}

bool HarborHullOpaqueSample(float3 positionWS)
{
    float3 local = HarborCabinLocal(positionWS);
    return local.y >= -0.14 && local.y <= 0.70 && HarborCabinOutline(local) < 1.13;
}

#endif
