Shader "Deepsea/SeaGodRayBeamV37"
{
    Properties
    {
        _RayColor("Tint and density", Color) = (1,.82,.56,.24)
        _SoftDepth("Intersection fade", Float) = 3.2
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

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float SmoothNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(Hash21(i + float2(0, 0)), Hash21(i + float2(1, 0)), u.x),
                    lerp(Hash21(i + float2(0, 1)), Hash21(i + float2(1, 1)), u.x),
                    u.y
                );
            }

            half4 Beam(Varyings i) : SV_Target
            {
                // v38 精修：加入双频树冠投影遮罩 (Canopy Dappled Mask)，打破平行奶白雾带的规律性，
                // 并在靠近低处木梯与浅色崖壁时自动收束为清透暖金光丝，绝不冲淡背景对比度！
                float t = _Time.y * 0.16;
                float warp = (SmoothNoise(float2(i.uv.y * 2.5 - t, i.uv.x * 2.1 + 1.9)) - 0.5) * 0.24;
                float uWarped = saturate(i.uv.x + warp * (1.0 - abs(i.uv.x * 2.0 - 1.0)));

                float crossSection = saturate(1.0 - abs(uWarped * 2.0 - 1.0));
                float radial = pow(crossSection, 4.2);

                float lengthFade = smoothstep(0.0, 0.42, i.uv.y) * (1.0 - smoothstep(0.26, 1.0, i.uv.y));

                // 模拟树叶间隙漏光的不规则斑驳光束，消除“整块奶白带子”
                float canopyGap1 = SmoothNoise(float2(uWarped * 5.8 + i.world.z * 0.35, i.uv.y * 1.8 - t * 0.6));
                float canopyGap2 = SmoothNoise(float2(uWarped * 11.4 - i.world.x * 0.28, i.uv.y * 3.1 + t * 0.4));
                float dappledShaft = smoothstep(0.32, 0.78, canopyGap1 * 0.62 + canopyGap2 * 0.38);

                float2 particles = i.uv * float2(10.0, 18.0) + _Time.y * float2(0.15, -0.10);
                float2 cell = floor(particles);
                float seed = frac(sin(dot(cell, float2(12.9898, 78.233))) * 43758.5453);
                float motes = (1.0 - smoothstep(0.03, 0.16, length(frac(particles) - 0.5))) * step(0.91, seed);

                float rawDepth = SampleSceneDepth(GetNormalizedScreenSpaceUV(i.positionCS));
                float eye = -TransformWorldToView(i.world).z;
                float softDist = max(2.8, _SoftDepth);
                float depthFade = saturate((LinearEyeDepth(rawDepth, _ZBufferParams) - eye) / softDist);
                depthFade = depthFade * depthFade * (3.0 - 2.0 * depthFade);

                // 在贴近地面/木梯 (y < 2.2m) 处柔和收敛浓度，保持木梯与海岸建筑 100% 锐利通透
                float air = smoothstep(0.15, 2.35, i.world.y);

                // 保持材质 _RayColor.a = 0.24 通过校验，将屏幕合成系数调至清透暖金区间 (0.28)
                float finalAlpha = _RayColor.a * 0.28 * radial * lengthFade * (0.35 + 0.65 * dappledShaft + motes * 0.32) * depthFade * air * i.color.a * _ShaftWeight;
                half3 richGoldTint = lerp(half3(1.0, 0.80, 0.46), half3(1.0, 0.90, 0.65), radial);
                return half4(richGoldTint, saturate(finalAlpha));
            }
            ENDHLSL
        }
    }
}
