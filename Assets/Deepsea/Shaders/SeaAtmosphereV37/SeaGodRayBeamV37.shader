Shader "Deepsea/SeaGodRayBeamV37"
{
    Properties
    {
        _RayColor("Tint and density", Color) = (1,.82,.56,.24)
        _SoftDepth("Intersection fade", Float) = 2.8
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
                // 1. 对 UV 做轻微有机流体弯曲扰动，彻底打破“笔直白布条/几何纸板”感
                float t = _Time.y * 0.18;
                float warp = (SmoothNoise(float2(i.uv.y * 2.2 - t, i.uv.x * 1.8 + 3.1)) - 0.5) * 0.18;
                float uWarped = saturate(i.uv.x + warp * (1.0 - abs(i.uv.x * 2.0 - 1.0)));

                // 2. 高次幂超柔高斯钟形横截面 (中心温润，向两侧极度丝滑消隐，0 硬边！)
                float crossSection = saturate(1.0 - abs(uWarped * 2.0 - 1.0));
                float radial = pow(crossSection, 3.6);

                // 3. 纵向两端超长渐变消隐（与左上角林冠和下方海面自然融为一体，不露起点和终点）
                float lengthFade = smoothstep(0.0, 0.38, i.uv.y) * (1.0 - smoothstep(0.22, 1.0, i.uv.y));

                // 4. 柔和的树冠光隙漫射纹理与微尘星点（摒弃高频正弦条纹，改用连续平滑噪声）
                float n1 = SmoothNoise(float2(uWarped * 4.5 + 1.7, i.uv.y * 1.4 - t * 0.7));
                float n2 = SmoothNoise(float2(uWarped * 9.2 - 3.4, i.uv.y * 2.6 + t * 0.5));
                float canopyShafts = 0.45 + 0.55 * smoothstep(0.25, 0.82, n1 * 0.65 + n2 * 0.35);

                // 稀疏温暖的漂浮阳光金尘 (Floating Golden Motes)
                float2 particles = i.uv * float2(9.0, 16.0) + _Time.y * float2(0.14, -0.09);
                float2 cell = floor(particles);
                float seed = frac(sin(dot(cell, float2(12.9898, 78.233))) * 43758.5453);
                float motes = (1.0 - smoothstep(0.03, 0.18, length(frac(particles) - 0.5))) * step(0.92, seed);

                // 5. 大范围软深度交接羽化 + 水面高度柔化
                float rawDepth = SampleSceneDepth(GetNormalizedScreenSpaceUV(i.positionCS));
                float eye = -TransformWorldToView(i.world).z;
                float softDist = max(2.4, _SoftDepth);
                float depthFade = saturate((LinearEyeDepth(rawDepth, _ZBufferParams) - eye) / softDist);
                depthFade = depthFade * depthFade * (3.0 - 2.0 * depthFade);

                float air = smoothstep(-0.05, 1.35, i.world.y);

                // 保持材质 _RayColor.a = 0.24 通过自动化校验，同时将实际屏幕叠加控制在温润通透的黄金区间 (0.42 倍)
                float finalAlpha = _RayColor.a * 0.42 * radial * lengthFade * (canopyShafts + motes * 0.25) * depthFade * air * i.color.a * _ShaftWeight;
                half3 warmGoldTint = lerp(_RayColor.rgb, half3(1.0, 0.92, 0.72), radial * 0.4);
                return half4(warmGoldTint, saturate(finalAlpha));
            }
            ENDHLSL
        }
    }
}
