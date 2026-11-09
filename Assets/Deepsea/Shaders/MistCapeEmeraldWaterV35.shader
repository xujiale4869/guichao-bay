Shader "Deepsea/MistCapeEmeraldWaterV35"
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
            Name "MistCapeEmeraldWaterForward"
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
            #include "SeaSurfaceV24/OceanV24Waves.hlsl"
            float _CoastWaterDepthProbe;
            float _CoastWaterDetails;
            float _OceanV24Layer;
            float4 _MistCapeBoatV35;
            float4 _MistCapeHeadingV35;
            float4 _MistCapeContactsV35[16];
            int _MistCapeContactCountV35;
            float _MistCapeWatchV35;

            #include "HarborLightingV27.hlsl"
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

                // Reconstruct the bed in world space for both camera projections.
                // Refraction stays off the air side of the waterline and nearby hull edges.
                float2 offset = n.xz * min(_Refraction, 0.0042) * smoothstep(0.06, 0.8, waterDepth);
                float2 refractedUV = saturate(screenUV + offset);
                float3 refractedBed = ComputeWorldSpacePosition(refractedUV, SampleDeviceDepth(refractedUV), UNITY_MATRIX_I_VP);
                float refractedDepth = i.worldPos.y - refractedBed.y;
                if (refractedDepth < 0.005 || -TransformWorldToView(refractedBed).z < i.eyeDepth - 0.04 ||
                    abs(refractedDepth - waterDepth) > max(0.42, waterDepth * 0.48))
                {
                    refractedUV = screenUV;
                    refractedBed = bottomWS;
                    refractedDepth = waterDepth;
                }

                float depthM = lerp(waterDepth, refractedDepth, 0.55);
                half3 bed = SampleSceneColor(refractedUV);
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.worldPos));
                float daylight = saturate(dot(sun.color, half3(.21,.72,.07)) * 0.51);

                // Advect two independently baked Voronoi edge fields in opposite directions.
                // The mipmapped field keeps thin filaments stable at the sailing camera's wide zoom.
                float causticZone = smoothstep(0.05, 0.34, depthM) * (1.0 - smoothstep(2.15, 4.0, depthM));
                float2 uv = (refractedBed.xz - sun.direction.xz * (0.30 * depthM)) * (_CausticsScale * 0.63);
                float2 curl = n.xz * .09 + .018 * sin(uv.yx * float2(15.0,11.0) + t * .56);
                float speed = t * _CausticsSpeed;
                half a = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, uv + curl + speed * float2(.10,-.08)).r;
                half b = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, uv * 1.39 - curl + speed * float2(-.065,.09) + .47).g;
                half primary = smoothstep(.23,.76,a);
                half secondary = smoothstep(.36,.82,b);
                half web = saturate(primary*.79 + secondary*.26 + primary*secondary*.17);
                Light bedSun = GetMainLight(TransformWorldToShadowCoord(refractedBed));
                half brightness = saturate(dot(bed, half3(.24,.68,.08)));
                half pattern = web * causticZone * daylight * lerp(.12,1.0,bedSun.shadowAttenuation)
                    * lerp(.23,1.0,brightness) * _CausticsIntensity;
                half3 causticLight = half3(1.0,.84,.53) * min(.33,pattern*.45) * (1.0 - saturate(bed)*.58);
                bed += causticLight;

                // Thin amber shore water, a clear emerald shelf and a blue absorbing trench.
                // Values are in the render's linear space, before URP tonemapping.
                half3 shore = half3(.42,.66,.49);
                half3 emerald = half3(.023,.465,.280);
                half3 shelf = half3(.016,.30,.29);
                half3 abyss = lerp(half3(.012,.055,.097),half3(.0035,.018,.043),smoothstep(4.2,13.5,depthM));
                half3 body = lerp(shore, emerald, smoothstep(.06,1.13,depthM));
                body = lerp(body, shelf, smoothstep(.9,2.85,depthM));
                body = lerp(body, abyss, smoothstep(2.4,6.45,depthM));
                half3 grade = 1.0;
                if (_MistCapeWatchV35 < .5) grade = half3(.94,.85,.91);
                else if (_MistCapeWatchV35 > 2.5) grade = half3(.40,.53,.78);
                else if (_MistCapeWatchV35 > 1.5) grade = half3(.96,.73,.77);
                body *= grade;

                float opticalPath = min(depthM / max(.46,abs(v.y)), 30.0);
                float3 extinction = lerp(float3(.57,.22,.185),float3(.84,.46,.36),smoothstep(2.35,5.7,depthM));
                half3 transmitted = exp(-opticalPath * extinction);
                float visibility = lerp(.79,1.0,_Visibility);
                float clarity = visibility * (1.0 - .89 * smoothstep(2.0,6.2,depthM));
                half3 underwaterComposite = body + (bed - body) * transmitted * clarity;

                // -------------------------------------------------------------
                // 5. 水面天光菲涅尔反射与涌浪条带运动感
                // -------------------------------------------------------------
                float fresnel = 0.02 + 0.98 * pow(1.0 - ndotv, 4.4);
                float3 reflDir = reflect(-v, n);
                half3 envRefl = GlossyEnvironmentReflection(reflDir, i.worldPos, 0.09, 1.0);
                half3 skyGrad = lerp(half3(0.07, 0.26, 0.34), half3(0.36, 0.56, 0.64), saturate(reflDir.y * 0.85 + 0.15));
                half3 surfaceReflection = max(envRefl * .40, skyGrad * .47) * grade;

                float waveShading = saturate(dot(n.xz, normalize(float2(-0.65, -0.75))) * 0.36 + 0.54);
                half3 finalColor = lerp(underwaterComposite, surfaceReflection, saturate(fresnel * _SurfaceWeight * .73 + .019));
                finalColor *= lerp(.91,1.10,waveShading);
                // Local wave facets catch the sky in broken patches across the trench.
                float facing = smoothstep(-.028,.13,dot(n.xz,float2(.88,.47)));
                float facetBreaks = smoothstep(-.36,.62,rippleA.y*.55 + rippleB.x*.30 + rippleC.y*.15);
                float openWater = smoothstep(.55,2.8,depthM);
                finalColor += half3(.014,.032,.049) * grade * facing * facetBreaks * openWater * (.18+.82*daylight);
                finalColor += causticLight * .13 * (1.0-smoothstep(1.1,3.2,depthM));

                // -------------------------------------------------------------
                // 6. 波光粼粼动态微面元太阳碎光 (v26.2 黄金克制版：收束光道、稀疏灵动星芒)
                //    移除 v26.1 中泛滥全屏的 pow 16 宽光锥，收束至迎光波峰，既波光粼粼又绝不刺眼！
                // -------------------------------------------------------------
                float3 halfVec = normalize(sun.direction + v);
                float ndoth = saturate(dot(n, halfVec));

                // 柔和迎光高光基底
                float broadSpec = pow(ndoth, 98.0) * .105;

                // 双层反向微面元交汇采样
                float2 glintUV1 = p * (_GlintScale * 0.095) + n.xz * 1.05 + float2(t * _GlintSpeed * 0.07, -t * _GlintSpeed * 0.08);
                float2 glintUV2 = p * (_GlintScale * 0.145) - n.xz * 0.85 + float2(-t * _GlintSpeed * 0.06, t * _GlintSpeed * 0.09);
                float maskA = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, glintUV1).b;
                float maskB = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, glintUV2).b;
                float maskC = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, glintUV1 * 0.53 + float2(0.41, 0.19)).r;

                float facet = maskA * maskB * (0.45 + 0.55 * maskC);
                float fWidth = max(fwidth(facet), 0.018);

                // 提高触发门槛 (0.25~0.44)，让碎光点呈稀疏灵动的星点分布，而不是成片白斑
                float sparkleMid = smoothstep(0.24 - fWidth, 0.38 + fWidth, facet);
                float sparkleCore = smoothstep(0.34 - fWidth, 0.48 + fWidth, facet);

                // 使用可调聚焦指数 _GlintFocus (默认 72)，将波光严格约束在太阳反射光道与波峰迎光面上
                float midSunCone = pow(ndoth, max(24.0, _GlintFocus * 0.65)) * smoothstep(0.35, 0.85, i.waveCrest);
                float coreSunCone = pow(ndoth, max(48.0, _GlintFocus * 1.45)) * smoothstep(0.42, 0.90, i.waveCrest);

                // 浅滩极浅处自动减弱表面碎光，避免与水底焦散打架
                float depthSparkleMask = smoothstep(0.35, 1.80, depthM);

                float totalSparkle = (
                    broadSpec +
                    (sparkleMid * midSunCone * 0.35 + sparkleCore * coreSunCone * 0.85) * _GlintIntensity * _GlintEnable * depthSparkleMask
                ) * sun.shadowAttenuation * detailScale;

                finalColor += lerp(sun.color, _SunPathColor.rgb, .40) * min(totalSparkle * daylight, .59);

                // Fine broken shore ribbon. Unrefracted depth keeps the line attached
                // to the same geometry that masks the water, including emergent rocks.
                half4 foamSample = SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap, p * .39 + n.xz * .25 + t * float2(.021,-.012));
                half lace = saturate(foamSample.r*.68 + foamSample.g*.32);
                float shoreline = 1.0 - smoothstep(.014, .125, waterDepth);
                float wetEdge = smoothstep(.004,.026,waterDepth);
                float surgeWidth = _FoamWidth * .54;
                float phase = waterDepth * 42.0 - t * 4.1 + lace * 1.1;
                float crest = pow(.5 + .5*sin(phase), 4.5);
                float surge = crest * (1.0 - smoothstep(.035, surgeWidth, waterDepth));
                float shoreFoam = (shoreline * lerp(.44,.88,lace) + surge * .37) * wetEdge;

                float pierRipples = 0.0;
                // The small box excludes open ocean fragments from the contact loop.
                [branch] if (p.x > -24.0 && p.x < 10.0 && p.y > -3.5 && p.y < 10.0)
                {
                    [loop]
                    for (int j = 0; j < _MistCapeContactCountV35; ++j)
                    {
                        float4 post = _MistCapeContactsV35[j];
                        float2 delta = p - post.xy;
                        float sq = dot(delta,delta);
                        float reach = post.z + 1.19;
                        if (sq >= reach*reach) continue;
                        float r = sqrt(sq) - post.z;
                        float influence = smoothstep(.025,.14,r) * (1.0 - smoothstep(.38,1.15,r));
                        float ring = pow(.5 + .5*sin((r+dot(delta,n.xz)*.21)*23.0 - t*5.7),4.0);
                        pierRipples = max(pierRipples, ring * influence * lerp(.55,1.0,lace));
                    }
                }
                finalColor += half3(.080,.136,.145) * pierRipples * wetEdge * lerp(.37,1.0,daylight);

                float2 boatDelta = p - _MistCapeBoatV35.xy;
                float2 heading = normalize(_MistCapeHeadingV35.xy + float2(.00001,0.0));
                float along = dot(boatDelta,heading);
                float side = dot(boatDelta,float2(heading.y,-heading.x));
                float activeSpeed = smoothstep(.11,2.5,_MistCapeBoatV35.z);
                float hullRing = length(float2(along/2.16,side/.89));
                float hullFoam = exp(-pow((hullRing-1.0)/.105,2.0)) * lerp(.11,.38,activeSpeed);
                float stern = max(0.0,-along-1.22);
                float behind = smoothstep(1.02,1.60,-along);
                float wakeWidth = .31 + stern*.25 + .055*sin(stern*2.1 - t*1.4);
                float fade = behind * exp(-stern*.32) * (1.0-smoothstep(4.4,8.4,stern)) * activeSpeed;
                float stripe = abs(side) - wakeWidth;
                float arm = exp(-pow(stripe/(.079+stern*.026),2.0));
                float echo = exp(-pow((stripe+.23)/(.061+stern*.019),2.0));
                float breaks = lerp(.46,1.0,smoothstep(.22,.75,lace));
                float wake = (arm + echo*.24) * breaks * fade;

                half3 foamColor = half3(.83,.94,.92) * lerp(grade,half3(1,1,1),.48);
                float foam = saturate((shoreFoam*.63 + hullFoam*.83 + wake*.68) * _FoamIntensity);
                finalColor = lerp(finalColor, foamColor, foam);

                if (_OceanV24Layer > 0.5 && _OceanV24Layer < 1.5) finalColor = surfaceReflection;
                if (_OceanV24Layer > 1.5) finalColor = half3(clarity, clarity, clarity);

                finalColor += HarborWaterV27(i.worldPos, n, v);
                return half4(max(finalColor, 0.0), 1.0);
            }
            ENDHLSL
        }
    }
}
