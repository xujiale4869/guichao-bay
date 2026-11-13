Shader "Deepsea/SeaContactShadeV37"
{
    Properties { _Tint("Contact shadow", Color) = (.035,.056,.068,.34) }
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
                float2 offset = i.uv * 2 - 1;
                float shape = 1 - smoothstep(.04,.98,length(offset));
                return half4(_Tint.rgb,_Tint.a * shape * shape * i.color.a);
            }
            ENDHLSL
        }
    }
}
