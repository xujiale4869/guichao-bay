Shader "Deepsea/SeaGodRayBeamV37"
{
    Properties
    {
        _RayColor("Tint and density", Color) = (1,.82,.56,.24)
        _SoftDepth("Intersection fade", Float) = 1.3
        _ShaftWeight("Relative density", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent+32" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Pass
        {
            Blend SrcAlpha One
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Beam
            #include "AtmosphereGeometryV37.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _RayColor;
            float _SoftDepth, _ShaftWeight;
            CBUFFER_END

            half4 Beam(Varyings i) : SV_Target
            {
                float crossSection = 1 - abs(i.uv.x * 2 - 1);
                float radial = crossSection * crossSection * (3 - 2 * crossSection);
                float lengthFade = smoothstep(0,.21,i.uv.y) * (1-smoothstep(.46,1,i.uv.y));
                float drift = sin(i.uv.x * 29 + sin(i.uv.y * 5) + _Time.y * .31);
                float filaments = .67 + drift * .11 + sin(i.uv.x * 59 - i.uv.y * 9 + _Time.y * .17) * .065;
                float2 particles = i.uv * float2(13,25) + _Time.y * float2(.21,-.13);
                float2 cell = floor(particles);
                float seed = frac(sin(dot(cell,float2(12.9898,78.233))) * 43758.5453);
                float motes = (1-smoothstep(.04,.20,length(frac(particles)-.5))) * step(.91,seed);
                filaments += motes * .21;
                float rawDepth = SampleSceneDepth(GetNormalizedScreenSpaceUV(i.positionCS));
                float eye = -TransformWorldToView(i.world).z;
                float depthFade = saturate((LinearEyeDepth(rawDepth,_ZBufferParams) - eye) / _SoftDepth);
                // Beam cards end above the nominal waterline, including their soft edges.
                float air = smoothstep(-.14,.62,i.world.y);
                return half4(_RayColor.rgb, _RayColor.a * radial * lengthFade * filaments * depthFade * air * i.color.a * _ShaftWeight);
            }
            ENDHLSL
        }
    }
}
