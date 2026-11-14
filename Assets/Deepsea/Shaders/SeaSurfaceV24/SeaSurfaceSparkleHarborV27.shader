Shader "Deepsea/SeaSurfaceSparkleHarborV27"
{
    Properties
    {
        [Header(Five Zone Depth Hierarchy)]
        _CrystalShoreColor("Zone 1: Crystal Shore Tint (<0.6m)", Color) = (0.78, 0.90, 0.78, 1)
        _Shallow("Zone 2: Emerald Lagoon (0.6m - 2.8m)", Color) = (0.08, 0.66, 0.60, 1)
        _ReefMidColor("Zone 3: Turquoise Reef Shelf (2.8m - 6.5m)", Color) = (0.03, 0.39, 0.50, 1)
        _Deep("Zone 4: Deep Trench Indigo (>6.5m)", Color) = (0.008, 0.11, 0.21, 1)
        _DepthRampMap("Optional 5-Zone Depth Ramp LUT", 2D) = "white" {}
        _UseDepthRamp("Use Baked Depth Ramp", Float) = 0

        [Header(Waves and Refraction)]
        _WaveMap("Fine Wave Normal Slopes", 2D) = "gray" {}
        _Refraction("Refraction Distortion", Range(0, 0.015)) = 0.0034
        _Visibility("Underwater Clarity", Range(0, 1)) = 0.86
        _SurfaceWeight("Surface Reflection Weight", Range(0, 1)) = 0.38

        [Header(Underwater Voronoi Caustics Web)]
        _CausticsMap("Voronoi Caustics & Sparkle (RGB)", 2D) = "black" {}
        _CausticsIntensity("Caustics Brightness on Sand/Reef", Range(0, 3.0)) = 0.92
        _CausticsScale("Caustics World Tiling", Range(0.05, 1.0)) = 0.23
        _CausticsSpeed("Caustics Flow Speed", Range(0, 1.5)) = 0.26
        _CausticsMaxDepth("Caustics Depth Fade (m)", Range(1, 15)) = 5.6

        [Header(Sun Sparkle and Shimmering Glints)]
        _GlintEnable("Enable Sun Sparkle", Range(0, 1)) = 1
        _GlintIntensity("Sparkle Peak Intensity", Range(0, 5)) = 1.85
        _GlintFocus("Sun Path Cone Focus", Range(20, 200)) = 72.0
        _GlintScale("Sparkle Cell Scale", Range(3, 30)) = 12.5
        _GlintSpeed("Sparkle Shimmer Speed", Range(0, 3)) = 0.72
        _SunPathColor("Warm Sun Glint Tint", Color) = (1.0, 0.94, 0.78, 1)

        [Header(Shoreline Surge Foam and Boat Wake)]
        _FoamMap("Shore Foam Cellular Noise", 2D) = "white" {}
        _FoamIntensity("Shore & Wake Foam Brightness", Range(0, 2.5)) = 0.95
        _FoamWidth("Shore Surge Band Width (m)", Range(0.05, 1.5)) = 0.48
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Name "SparkleLayeredWaterV26"
            Tags { "LightMode"="UniversalForward" }
            Blend One Zero
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #define _SURFACE_TYPE_TRANSPARENT 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_WaveMap);        SAMPLER(sampler_WaveMap);
            TEXTURE2D(_CausticsMap);    SAMPLER(sampler_CausticsMap);
            TEXTURE2D(_FoamMap);        SAMPLER(sampler_FoamMap);
            TEXTURE2D(_DepthRampMap);   SAMPLER(sampler_DepthRampMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _CrystalShoreColor;
                half4 _Shallow;
                half4 _ReefMidColor;
                half4 _Deep;
                float _UseDepthRamp;
                float _Refraction;
                float _Visibility;
                float _SurfaceWeight;
                float _CausticsIntensity;
                float _CausticsScale;
                float _CausticsSpeed;
                float _CausticsMaxDepth;
                float _GlintEnable;
                float _GlintIntensity;
                float _GlintFocus;
                float _GlintScale;
                float _GlintSpeed;
                half4 _SunPathColor;
                float _FoamIntensity;
                float _FoamWidth;
            CBUFFER_END

            float _WaterV22Time;
            #include "OceanV24Waves.hlsl"
            float _CoastWaterDepthProbe;
            float _CoastWaterDetails;
            float _OceanV24Layer;
            float4 _CoastBoat;
            float4 _CoastBoatHeading;
            float4 _CoastAtmosphereV37;

            #include "../HarborLightingV27.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 waveNormal : TEXCOORD1;
                float  eyeDepth   : TEXCOORD2;
                float  waveCrest  : TEXCOORD3;
            };

            float3 EvaluateBayWaves(float2 p, float t, out float3 outNormal, out float outCrest)
            {
                float3 dx, dz;
                float3 displacement = OceanV24Sample(p, t, dx, dz);
                outNormal = normalize(cross(dz, dx));
                outCrest = saturate(0.5 + displacement.y / 0.52);
                return displacement;
            }

            Varyings Vert(Attributes input)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                float t = (_OceanV24Time > 0.001) ? _OceanV24Time : ((_WaterV22Time > 0.001) ? _WaterV22Time : _Time.y);

                float3 wNormal;
                float wCrest;
                float3 waveOffset = EvaluateBayWaves(world.xz, t, wNormal, wCrest);
                world += waveOffset;

                o.worldPos   = world;
                o.waveNormal = wNormal;
                o.waveCrest  = wCrest;
                o.positionCS = TransformWorldToHClip(world);
                o.eyeDepth   = -TransformWorldToView(world).z;
                return o;
            }

            float SampleDeviceDepth(float2 uv)
            {
                float d = SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                d = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, d);
                #endif
                return d;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(i.positionCS);
                float rawDepth = SampleDeviceDepth(screenUV);
                float3 bottomWS = ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);
                float waterDepth = max(0.0, i.worldPos.y - bottomWS.y);

                if (_CoastWaterDepthProbe > 0.5)
                    return half4(saturate(waterDepth / 25.0), 0, 0, 1);

                clip(waterDepth - 0.0015);

                float t = (_OceanV24Time > 0.001) ? _OceanV24Time : ((_WaterV22Time > 0.001) ? _WaterV22Time : _Time.y);
                float detailScale = (_CoastWaterDetails > 0.01) ? _CoastWaterDetails : 1.0;
                float2 p = i.worldPos.xz;

                // -------------------------------------------------------------
                // 1. 三尺度动态波纹法线场（平滑克制，既有涌浪层次又不碎乱）
                // -------------------------------------------------------------
                half2 rippleA = SAMPLE_TEXTURE2D(_WaveMap, sampler_WaveMap, p * 0.086 + float2(-t * 0.019, t * 0.013)).rg * 2.0 - 1.0;
                half2 rippleB = SAMPLE_TEXTURE2D(_WaveMap, sampler_WaveMap, p * 0.168 + float2(t * 0.014, -t * 0.020)).rg * 2.0 - 1.0;
                half2 rippleC = SAMPLE_TEXTURE2D(_WaveMap, sampler_WaveMap, p * 0.310 + float2(-t * 0.024, -t * 0.016)).rg * 2.0 - 1.0;

                float2 combinedSlope = (rippleA * 0.095 + rippleB * 0.065 + rippleC * 0.032) * detailScale;
                float3 n = normalize(i.waveNormal + float3(combinedSlope.x, 0.0, combinedSlope.y));
                float3 v = GetWorldSpaceNormalizeViewDir(i.worldPos);
                float ndotv = saturate(dot(n, v));

                // -------------------------------------------------------------
                // 2. 真实水下折射采样与岸线防溢色保护
                // -------------------------------------------------------------
                float2 refractOffset = n.xz * _Refraction * smoothstep(0.04, 0.75, waterDepth);
                float2 refractedUV = saturate(screenUV + refractOffset);
                float refractedEye = LinearEyeDepth(SampleSceneDepth(refractedUV), _ZBufferParams);
                if (refractedEye < i.eyeDepth - 0.05)
                {
                    refractedUV = screenUV;
                }
                float3 refractedBottomWS = ComputeWorldSpacePosition(refractedUV, SampleDeviceDepth(refractedUV), UNITY_MATRIX_I_VP);
                float refractedDepth = max(0.0, i.worldPos.y - refractedBottomWS.y);
                float depthM = lerp(waterDepth, refractedDepth, 0.65);

                half3 seabedScene = SampleSceneColor(refractedUV);

                // -------------------------------------------------------------
                // 3. 温润自然的水底双层交错焦散网 (v26.2 黄金克制版：告别刺眼发白！)
                //    采用雙层交汇提取柔润光丝 + 与水底自身明度相乘调制，深色阴影处与深水处自动隐退
                // -------------------------------------------------------------
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(refractedBottomWS));
                float2 sunProjOffset = mainLight.direction.xz * (depthM * 0.15);
                float2 causticsUV = (refractedBottomWS.xz - sunProjOffset) * _CausticsScale + n.xz * 0.22;
                float2 flow1 = causticsUV + float2(t * _CausticsSpeed * 0.14, t * _CausticsSpeed * 0.09);
                float2 flow2 = causticsUV * 1.22 + float2(-t * _CausticsSpeed * 0.12, t * _CausticsSpeed * 0.13) + float2(0.37, 0.81);

                half4 causticsTex1 = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, flow1);
                half4 causticsTex2 = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, flow2);

                // 关键修复：不再把两层网格生硬相加铺满全屏，而是以“双层几何平均/交汇”为主，形成疏密有致的柔和光网
                float cA = smoothstep(0.22, 0.88, causticsTex1.r);
                float cB = smoothstep(0.22, 0.88, causticsTex2.g);
                float organicWeb = sqrt(cA * cB) * 0.72 + (cA + cB) * 0.16;

                // 仅在 0.12m ~ 4.8m 的浅水受光沙地与浅礁上自然显现，岸边极浅处与深水处平滑淡出
                float causticsDepthMask = smoothstep(0.08, 0.55, depthM) * exp(-depthM / max(1.2, _CausticsMaxDepth));
                float causticsShadow = lerp(0.12, 1.0, mainLight.shadowAttenuation);
                // 根据水底自身亮度做柔和调制（避免把暗部石头染成白网）
                float seabedLuma = saturate(dot(seabedScene, half3(0.3, 0.59, 0.11)));
                float seabedMask = lerp(0.28, 0.95, seabedLuma);

                half3 causticsTint = lerp(half3(0.92, 0.96, 0.82), half3(0.38, 0.88, 0.84), saturate(depthM / 2.6));
                half3 causticsLight = causticsTint * mainLight.color * (organicWeb * _CausticsIntensity * causticsDepthMask * causticsShadow * seabedMask);

                // 柔和溶入水底，上限做软钳制防止过曝发白
                seabedScene += causticsLight * 0.36;

                // -------------------------------------------------------------
                // 4. 五级水深光学层次 + 水下珊瑚礁自然透彩
                // -------------------------------------------------------------
                float z1 = saturate(depthM / 0.90);                    // 0.0 ~ 0.9m: 极浅清透暖沙岸滩
                float z2 = smoothstep(0.65, 3.20, depthM);             // 0.65 ~ 3.2m: 翡翠薄荷绿内湾
                float z3 = smoothstep(2.60, 7.20, depthM);             // 2.6 ~ 7.2m: 孔雀青蓝礁盘边缘
                float z4 = 1.0 - exp(-max(0.0, depthM - 5.2) * 0.25);  // >5.2m:      右侧深海鱼群海沟

                half3 waterBodyColor = lerp(_CrystalShoreColor.rgb, _Shallow.rgb, z1);
                waterBodyColor = lerp(waterBodyColor, _ReefMidColor.rgb, z2);
                waterBodyColor = lerp(waterBodyColor, _Deep.rgb, saturate(z3 * 0.72 + z4 * 0.56));

                if (_UseDepthRamp > 0.5)
                {
                    half3 rampCol = SAMPLE_TEXTURE2D(_DepthRampMap, sampler_DepthRampMap, float2(saturate(depthM / 11.5), 0.5)).rgb;
                    waterBodyColor = lerp(waterBodyColor, rampCol, 0.45);
                }

                float opticalPath = min(depthM / max(0.42, abs(v.y)), 45.0);
                float3 attenCoeff = lerp(float3(0.45, 0.12, 0.08), float3(0.82, 0.18, 0.11), smoothstep(1.4, 6.0, depthM));
                half3 absorption = exp(-opticalPath * attenCoeff);
                float clarity = _Visibility * exp(-depthM * 0.125) * lerp(0.55, 0.96, smoothstep(0.28, 0.84, ndotv));

                // 波峰次表面散射 (Subsurface Scattering)
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.worldPos));
                float backSun = saturate(dot(v, -normalize(sun.direction + float3(0, 0.35, 0))));
                float crestSSS = pow(max(i.waveCrest, 0.0001), 2.1) * (0.35 + 0.65 * backSun) * exp(-depthM * 0.18);
                waterBodyColor += half3(0.06, 0.28, 0.24) * crestSSS * sun.shadowAttenuation;

                half3 underwaterComposite = lerp(waterBodyColor, seabedScene * absorption + waterBodyColor * (1.0 - absorption), saturate(clarity));

                // -------------------------------------------------------------
                // 5. 水面天光菲涅尔反射与涌浪条带运动感
                // -------------------------------------------------------------
                float fresnel = 0.02 + 0.98 * pow(1.0 - ndotv, 4.4);
                float3 reflDir = reflect(-v, n);
                half3 envRefl = GlossyEnvironmentReflection(reflDir, i.worldPos, 0.09, 1.0);
                half3 skyGrad = lerp(half3(0.07, 0.26, 0.34), half3(0.36, 0.56, 0.64), saturate(reflDir.y * 0.85 + 0.15));
                half3 surfaceReflection = max(envRefl * 0.58, skyGrad * 0.52);

                float waveShading = saturate(dot(n.xz, normalize(float2(-0.65, -0.75))) * 0.36 + 0.54);
                half3 finalColor = lerp(underwaterComposite, surfaceReflection, saturate(fresnel * _SurfaceWeight + 0.03));
                finalColor *= lerp(0.91, 1.09, waveShading);

                // 6. Sun glints use a wider lobe for the low v38 sun.
                if (_CoastAtmosphereV37.w > 0.5)
                {
                    float3 tiltedSunDir = normalize(float3(sun.direction.x * 0.55, max(0.62, sun.direction.y), sun.direction.z * 0.55));
                    float3 sparkleNormal = normalize(float3(n.x * 2.25, n.y, n.z * 2.25));
                    float3 halfVec = normalize(tiltedSunDir + v);
                    float ndoth = saturate(dot(sparkleNormal, halfVec));

                    float broadSpec = pow(ndoth, 28.0) * 0.22 * smoothstep(0.25, 0.82, i.waveCrest);

                    float2 glintUV1 = p * (_GlintScale * 0.095) + n.xz * 1.15 + float2(t * _GlintSpeed * 0.07, -t * _GlintSpeed * 0.08);
                    float2 glintUV2 = p * (_GlintScale * 0.145) - n.xz * 0.92 + float2(-t * _GlintSpeed * 0.06, t * _GlintSpeed * 0.09);
                    float maskA = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, glintUV1).b;
                    float maskB = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, glintUV2).b;
                    float maskC = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, glintUV1 * 0.53 + float2(0.41, 0.19)).r;

                    float facet = maskA * maskB * (0.45 + 0.55 * maskC);
                    float fWidth = max(fwidth(facet), 0.018);

                    float sparkleMid = smoothstep(0.22 - fWidth, 0.36 + fWidth, facet);
                    float sparkleCore = smoothstep(0.32 - fWidth, 0.46 + fWidth, facet);

                    float midSunCone = pow(ndoth, max(8.0, _GlintFocus * 0.25)) * smoothstep(0.30, 0.82, i.waveCrest);
                    float coreSunCone = pow(ndoth, max(16.0, _GlintFocus * 0.50)) * smoothstep(0.38, 0.88, i.waveCrest);

                    float depthSparkleMask = smoothstep(0.28, 1.55, depthM);

                    float totalSparkle = (
                        broadSpec * _GlintEnable * depthSparkleMask +
                        (sparkleMid * midSunCone * 0.48 + sparkleCore * coreSunCone * 0.95) * _GlintIntensity * _GlintEnable * depthSparkleMask
                    ) * sun.shadowAttenuation * detailScale * saturate(sun.direction.y * 3.0);

                    half3 warmGoldGlintColor = lerp(sun.color, half3(1.0, 0.90, 0.62), 0.65);
                    finalColor += warmGoldGlintColor * min(totalSparkle, 1.35);
                }
                else
                {
                    float3 halfVec = normalize(sun.direction + v);
                    float ndoth = saturate(dot(n, halfVec));

                    float broadSpec = pow(ndoth, 68.0) * 0.18;

                    float2 glintUV1 = p * (_GlintScale * 0.095) + n.xz * 1.05 + float2(t * _GlintSpeed * 0.07, -t * _GlintSpeed * 0.08);
                    float2 glintUV2 = p * (_GlintScale * 0.145) - n.xz * 0.85 + float2(-t * _GlintSpeed * 0.06, t * _GlintSpeed * 0.09);
                    float maskA = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, glintUV1).b;
                    float maskB = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, glintUV2).b;
                    float maskC = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, glintUV1 * 0.53 + float2(0.41, 0.19)).r;

                    float facet = maskA * maskB * (0.45 + 0.55 * maskC);
                    float fWidth = max(fwidth(facet), 0.018);

                    float sparkleMid = smoothstep(0.24 - fWidth, 0.38 + fWidth, facet);
                    float sparkleCore = smoothstep(0.34 - fWidth, 0.48 + fWidth, facet);

                    float midSunCone = pow(ndoth, max(24.0, _GlintFocus * 0.65)) * smoothstep(0.35, 0.85, i.waveCrest);
                    float coreSunCone = pow(ndoth, max(48.0, _GlintFocus * 1.45)) * smoothstep(0.42, 0.90, i.waveCrest);

                    float depthSparkleMask = smoothstep(0.35, 1.80, depthM);

                    float totalSparkle = (
                        broadSpec +
                        (sparkleMid * midSunCone * 0.35 + sparkleCore * coreSunCone * 0.85) * _GlintIntensity * _GlintEnable * depthSparkleMask
                    ) * sun.shadowAttenuation * detailScale;

                    finalColor += lerp(sun.color, _SunPathColor.rgb, 0.58) * min(totalSparkle, 1.35);
                }

                // -------------------------------------------------------------
                // 7. 岸礁/栈桥双层涌浪白沫与小船 V 型扩散尾流
                // -------------------------------------------------------------
                half4 foamSample = SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap, p * 0.36 + n.xz * 0.42 + float2(t * 0.035, -t * 0.025));
                float laceNoise = (foamSample.r > 0.01 && foamSample.r < 0.99)
                    ? (foamSample.r * 0.68 + foamSample.g * 0.32)
                    : (0.5 + 0.28 * sin(p.x * 4.2 + sin(p.y * 2.3) + t * 0.9) + 0.22 * cos(p.y * 5.6 - p.x * 2.1 - t * 1.3));

                float contactFoam = (1.0 - smoothstep(0.01, 0.14, depthM)) * smoothstep(0.28, 0.72, laceNoise);
                float surgePhase = frac(depthM / max(0.18, _FoamWidth) - t * 0.30 + laceNoise * 0.26);
                float surgeRing = sin(surgePhase * 3.14159) * (1.0 - smoothstep(0.05, _FoamWidth, depthM));
                surgeRing = smoothstep(0.45, 0.82, surgeRing * laceNoise);

                float shoreFoam = saturate(contactFoam * 0.92 + surgeRing * 0.62) * smoothstep(0.002, 0.014, depthM);

                float2 boatDelta = p - _CoastBoat.xy;
                float2 heading = normalize(_CoastBoatHeading.xy + float2(0.00001, 0.0));
                float along = dot(boatDelta, heading);
                float side = dot(boatDelta, float2(heading.y, -heading.x));
                float activeSpeed = max(0.25, saturate(_CoastBoat.z / 2.8)) * max(0.35, _CoastBoat.w);

                float hullRing = sqrt(pow(along / 2.20, 2.0) + pow(side / 0.94, 2.0));
                float hullFoam = exp(-pow((hullRing - 1.0) / 0.10, 2.0)) * (0.45 + 0.55 * laceNoise) * 0.50;
                float bowWave = exp(-pow((along - (2.05 - abs(side) * 0.42)) / 0.15, 2.0)) * (1.0 - smoothstep(0.65, 1.65, abs(side))) * activeSpeed;

                float sternDist = max(0.0, -along - 1.05);
                float wakeHalfWidth = 0.26 + sternDist * 0.27;
                float wakeFade = exp(-sternDist * 0.15) * step(along, -0.95) * activeSpeed;
                float vArm = exp(-pow((abs(side) - wakeHalfWidth) / (0.15 + sternDist * 0.04), 2.0)) * wakeFade;
                float propWash = exp(-pow(side / (0.22 + sternDist * 0.05), 2.0)) * exp(-sternDist * 0.22) * step(along, -0.95) * activeSpeed;
                float wakeFoam = saturate((vArm * 0.78 + propWash * 0.92 + hullFoam) * (0.42 + 0.58 * laceNoise));

                float combinedFoam = saturate((shoreFoam + bowWave * 0.82 + wakeFoam) * _FoamIntensity);
                half3 foamColor = lerp(half3(0.80, 0.93, 0.95), half3(0.98, 0.99, 1.0), combinedFoam);
                finalColor = lerp(finalColor, foamColor, saturate(combinedFoam * 0.80));

                if (_OceanV24Layer > 0.5 && _OceanV24Layer < 1.5) finalColor = surfaceReflection;
                if (_OceanV24Layer > 1.5) finalColor = half3(clarity, clarity, clarity);

                finalColor += HarborWaterV27(i.worldPos, n, v);
                return half4(max(finalColor, 0.0), 1.0);
            }
            ENDHLSL
        }
    }
}
