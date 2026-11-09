Shader "Deepsea/MistCapePhysicalWaterV351"
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
            Name "MistCapePhysicalWaterForward"
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
            float _MistCapeMeanWaterV351;

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

                // A fourth short Gerstner derivative stays in the shading normal. The
                // three geometric swells still match the boat and buoy height solver.
                float2 wind = float2(-.91,.4146);
                float phase = dot(p,wind)*5.82 - t*5.89 + .34*rippleA.y;
                float pixelCoverage = 1.0-smoothstep(.25,1.3,fwidth(phase));
                float2 shortSlope = -wind * (.036*cos(phase)*pixelCoverage);
                float2 combinedSlope = (rippleA*.085 + rippleB*.071 + rippleC*.039 + shortSlope)*detailScale;
                float3 n = normalize(i.waveNormal + float3(combinedSlope.x, 0.0, combinedSlope.y));
                float3 v = GetWorldSpaceNormalizeViewDir(i.worldPos);
                float ndotv = saturate(dot(n, v));

                // Compare the tilted and flat transmitted rays. Their difference is
                // projected in metres before checking the actual sampled scene depth.
                const float airToWater = .7502;
                float3 through = refract(-v,n,airToWater);
                float3 calm = refract(-v,float3(0,1,0),airToWater);
                float2 shift = (through.xz/max(.25,-through.y) - calm.xz/max(.25,-calm.y))
                    * min(waterDepth,2.4) * .83;
                shift *= min(1.0,.32/max(.001,length(shift))) * smoothstep(.025,.22,waterDepth)
                    * clamp(_Refraction/.0034,0.0,1.8);

                float4 offsetScreen = ComputeScreenPos(TransformWorldToHClip(bottomWS+float3(shift.x,0,shift.y)));
                float2 deltaUV = offsetScreen.xy/offsetScreen.w - screenUV;
                float2 refractedUV = clamp(screenUV+clamp(deltaUV,-.009,.009),.002,.998);
                float3 refractedBed = ComputeWorldSpacePosition(refractedUV,SampleDeviceDepth(refractedUV),UNITY_MATRIX_I_VP);
                float refractedDepth = i.worldPos.y-refractedBed.y;
                if (refractedDepth < .008 || -TransformWorldToView(refractedBed).z < i.eyeDepth-.045 ||
                    abs(refractedDepth-waterDepth)>max(.39,waterDepth*.42))
                {
                    refractedUV = screenUV;
                    refractedBed = bottomWS;
                    refractedDepth = waterDepth;
                }

                float depthM = max(0.0,refractedDepth);
                half3 bed = SampleSceneColor(refractedUV);
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.worldPos));
                float daylight = saturate(dot(sun.color,half3(.21,.72,.07))*.51);

                // Trace sunlight back to the mean water plane from the refracted
                // opaque hit. Caustics are part of bed radiance before absorption.
                float causticDepth = max(0.0,_MistCapeMeanWaterV351-refractedBed.y);
                float3 sunInWater = refract(-sun.direction,float3(0,1,0),airToWater);
                float2 projectedBed = refractedBed.xz-sunInWater.xz*(causticDepth/max(.35,-sunInWater.y));
                float2 causticUV = projectedBed*(_CausticsScale*.57);
                float causticTime = t*_CausticsSpeed;
                float2 flow = SAMPLE_TEXTURE2D(_WaveMap,sampler_WaveMap,projectedBed*.13 + causticTime*float2(.045,-.031)).rg*2.0-1.0;
                float2 curl = float2(flow.y,-flow.x)*.062;
                half a = SAMPLE_TEXTURE2D(_CausticsMap,sampler_CausticsMap,causticUV+curl+causticTime*float2(.091,-.067)).r;
                float2 crossedUV = mul(float2x2(.86,-.51,.51,.86),causticUV)*1.24;
                half b = SAMPLE_TEXTURE2D(_CausticsMap,sampler_CausticsMap,crossedUV-curl+causticTime*float2(-.054,.085)+.47).g;
                half primary = smoothstep(.26,.84,a);
                half secondary = smoothstep(.34,.89,b);
                half web = saturate(primary*.63 + secondary*.24 + primary*secondary*.26);
                Light bedSun = GetMainLight(TransformWorldToShadowCoord(refractedBed));
                float3 bedArea = cross(ddy(refractedBed),ddx(refractedBed));
                float bedSlope = abs(bedArea.y)*rsqrt(max(1e-8,dot(bedArea,bedArea)));
                float causticZone = smoothstep(.09,.43,causticDepth)*exp(-causticDepth*.36)
                    * (1.0-smoothstep(_CausticsMaxDepth*.55,_CausticsMaxDepth*.88,causticDepth));
                half modulation = web*causticZone*daylight*lerp(.10,1.0,bedSun.shadowAttenuation)
                    * lerp(.22,1.0,bedSlope)*lerp(.54,1.0,saturate(flow.x*.72+.55))*_CausticsIntensity;
                bed += (bed*.75+.035)*half3(1.0,.85,.59)*min(.55,modulation*.89);

                half3 grade = 1.0;
                if (_MistCapeWatchV35 < .5) grade = half3(.94,.85,.91);
                else if (_MistCapeWatchV35 > 2.5) grade = half3(.40,.53,.78);
                else if (_MistCapeWatchV35 > 1.5) grade = half3(.96,.73,.77);

                // Continuous spectral transmission and in-scattering. The sandy
                // bed dominates where travel through the water is nearly zero.
                float pathLength = min(depthM/max(.32,-through.y),48.0);
                float3 extinction = float3(.62,.238,.176)*lerp(1.27,.96,saturate(_Visibility));
                half3 transmitted = exp(-pathLength*extinction);
                half3 lagoon = half3(.020,.405,.271);
                half3 shelf = half3(.011,.161,.193);
                half3 abyss = half3(.0038,.020,.050);
                half3 body = lerp(lagoon,shelf,1.0-exp(-depthM*.44));
                body = lerp(body,abyss,1.0-exp(-depthM*(.16+depthM*.024)));
                float forwardScatter = depthM*exp(-depthM*1.07)*daylight;
                body += half3(.012,.071,.056)*forwardScatter;
                half3 underwaterComposite = bed*transmitted + body*grade*(1.0-transmitted);

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

                // Two advected foam scales break up contacts, surf and the current
                // stern wash. Distant wake history remains in the v23 moving mesh.
                float2 flowUV = p*.61 + t*float2(.067,-.042);
                half2 coarse = SAMPLE_TEXTURE2D(_FoamMap,sampler_FoamMap,flowUV).rg;
                float2 stretch = mul(float2x2(.78,-.63,.63,.78),p);
                half2 fine = SAMPLE_TEXTURE2D(_FoamMap,sampler_FoamMap,
                    stretch*1.87 + (coarse-.5)*.25 + t*float2(-.094,.087)).rg;
                half foamField = saturate(dot(coarse,half2(.39,.22))+dot(fine,half2(.25,.14)));
                float wetEdge = smoothstep(.004,.031,waterDepth);

                float shoreDepth = max(0.0,waterDepth+(coarse.y-.5)*.066);
                float shoreEdge = 1.0-smoothstep(.019,_FoamWidth*.52,shoreDepth);
                float surge = .56+.44*sin(t*3.8+coarse.x*5.4);
                float shoreFoam = shoreEdge*smoothstep(.36,.72,foamField+shoreEdge*.16)*(.16+.32*surge);

                float pierFoam = 0.0;
                [branch] if (p.x > -24.0 && p.x < 10.0 && p.y > -3.5 && p.y < 10.0)
                {
                    [loop] for (int j=0; j<_MistCapeContactCountV35; ++j)
                    {
                        float4 post = _MistCapeContactsV35[j];
                        float2 delta = p-post.xy;
                        float reach = post.z+.72;
                        float d2 = dot(delta,delta);
                        if (d2>reach*reach) continue;
                        float r = sqrt(d2)-post.z;
                        float reachNoise = .32+coarse.x*.27;
                        float contact = (1.0-smoothstep(.012,reachNoise,r))*smoothstep(-.078,.016,r);
                        float frayed = smoothstep(.43,.71,foamField+contact*.12);
                        pierFoam = max(pierFoam,contact*frayed*.38);
                    }
                }

                float activeSpeed = smoothstep(.12,2.7,_MistCapeBoatV35.z);
                float wakeFoam = 0.0;
                float2 rel = p-_MistCapeBoatV35.xy;
                [branch] if (activeSpeed>0.0 && dot(rel,rel)<64.0)
                {
                    float2 heading = normalize(_MistCapeHeadingV35.xy+float2(.00001,0.0));
                    float along = -dot(rel,heading)-1.55;
                    float side = dot(rel,float2(heading.y,-heading.x));
                    float back = max(0.0,along);
                    float width = .19+.28*back+.048*back*back+(coarse.x-.5)*(.14+back*.14);
                    float thickness = .14+back*.105;
                    float arm = saturate(1.0-abs(abs(side)-width)/thickness);
                    float frayed = smoothstep(.45,.71,foamField+arm*.12);
                    float spread = arm*frayed*exp(-back*.74);
                    float washWidth = .20+.17*back;
                    float wash = exp(-pow((side+(coarse.y-.5)*.22)/washWidth,2.0));
                    wash *= smoothstep(.37,.75,foamField+fine.r*.11)*exp(-back*1.16);
                    float envelope = smoothstep(-.25,.25,along)*(1.0-smoothstep(2.1,4.65,back));
                    wakeFoam = (spread*.46+wash*.63)*envelope*activeSpeed;
                }

                half3 foamColor = half3(.79,.90,.89)*lerp(grade,half3(1,1,1),.43);
                float foam = saturate((shoreFoam+pierFoam)*wetEdge+wakeFoam)*saturate(_FoamIntensity);
                finalColor = lerp(finalColor,foamColor,foam);

                if (_OceanV24Layer > 0.5 && _OceanV24Layer < 1.5) finalColor = surfaceReflection;
                if (_OceanV24Layer > 1.5) finalColor = dot(transmitted,half3(.2126,.7152,.0722)).xxx;

                finalColor += HarborWaterV27(i.worldPos, n, v);
                // Resolve the water once and softly blend only the true waterline.
                half3 airSide = SampleSceneColor(screenUV);
                half3 resolved = lerp(airSide,max(finalColor,0.0),smoothstep(.002,.052,waterDepth));
                return half4(resolved,1.0);
            }
            ENDHLSL
        }
    }
}
