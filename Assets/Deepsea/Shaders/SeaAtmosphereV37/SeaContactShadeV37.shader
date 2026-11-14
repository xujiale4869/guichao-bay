Shader "Deepsea/SeaContactShadeV37"
{
    Properties { _Tint("Contact shadow", Color) = (.11,.23,.35,.15) }
    SubShader
    {
        Tags { "Queue"="Transparent-2" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Shade
            #include "AtmosphereGeometryV37.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            CBUFFER_END

            half4 Shade(Varyings i) : SV_Target
            {
                float2 offset = i.uv * 2.0 - 1.0;
                float r = length(offset);
                // 3次方超柔边缘衰减 + 蔚蓝天光软阴影色 (绝不产生死黑泥团!)
                float shape = saturate(1.0 - smoothstep(0.08, 0.96, r));
                float softFalloff = shape * shape * shape;
                half3 coolSkyShadow = _Tint.rgb;
                float alpha = min(_Tint.a, 0.15) * softFalloff * i.color.a;
                return half4(coolSkyShadow, alpha);
            }
            ENDHLSL
        }
    }
}
