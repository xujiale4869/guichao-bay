Shader "Deepsea/VelvetShellGrassV44"
{
    Properties
    {
        _ShellThickness ("Shell Total Height", Range(0.15, 1.0)) = 0.62
        _StrandDensity ("Velvet Strand Density", Range(6, 18)) = 9.5
        _WindStrength ("Swirl Wind Strength", Range(0, 0.9)) = 0.52
        _WindSpeed ("Wind Wave Speed", Range(0.2, 4)) = 1.45
        _GrassRootCol ("Grass Root Color", Color) = (0.054, 0.147, 0.085, 1)
        _GrassMidCol ("Grass Mid Color", Color) = (0.118, 0.325, 0.168, 1)
        _GrassTipCol ("Grass Tip Color", Color) = (0.305, 0.545, 0.255, 1)
        _GoldRootCol ("Golden Root Color", Color) = (0.105, 0.199, 0.104, 1)
        _GoldMidCol ("Golden Mid Color", Color) = (0.70, 0.49, 0.103, 1)
        _GoldTipCol ("Golden Tip Color", Color) = (0.91, 0.73, 0.246, 1)
        _SandGrassCol ("Dune Transition Color", Color) = (0.43, 0.49, 0.28, 1)
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="Geometry+5" "RenderPipeline"="UniversalPipeline" }
        Cull Back
        ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _ShellThickness;
            float _StrandDensity;
            float _WindStrength;
            float _WindSpeed;
            half4 _GrassRootCol;
            half4 _GrassMidCol;
            half4 _GrassTipCol;
            half4 _GoldRootCol;
            half4 _GoldMidCol;
            half4 _GoldTipCol;
            half4 _SandGrassCol;
        CBUFFER_END

        float4 _CoastAtmosphereV37;
        float4 _CoastMistV37;

        struct Attributes
        {
            float3 position : POSITION;
            float3 normal : NORMAL;
            float2 shell : TEXCOORD0;
            half4 biome : COLOR;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            float2 rootXZ : TEXCOORD2;
            half4 shell : TEXCOORD3;
            half3 shade : TEXCOORD4;
            #if defined(SHADER_API_D3D11)
                float shellVisibility : SV_ClipDistance0;
            #else
                float shellVisibility : TEXCOORD5;
            #endif
        };

        inline float3 StrandNoise(float2 cell)
        {
            float3 hash = frac(cell.xyx * float3(.1031, .1030, .0973));
            hash += dot(hash, hash.yzx + 33.33);
            return frac((hash.xxy + hash.yzz) * hash.zyx);
        }

        // Return value and analytic gradient, keeping the same field continuous across cell boundaries.
        float3 FlowNoise(float2 p)
        {
            float2 cell = floor(p);
            float2 f = frac(p);
            float2 u = f*f*(3-2*f);
            float2 du = 6*f*(1-f);
            float a = StrandNoise(cell).z;
            float b = StrandNoise(cell+float2(1,0)).z;
            float c = StrandNoise(cell+float2(0,1)).z;
            float d = StrandNoise(cell+1).z;
            float crossTerm = a-b-c+d;
            return float3(a+(b-a)*u.x+(c-a)*u.y+crossTerm*u.x*u.y,
                          (b-a+crossTerm*u.y)*du.x,
                          (c-a+crossTerm*u.x)*du.y);
        }

        float3 SwirlWind(float2 xz, float time)
        {
            float2 flow = xz*.075 - time*float2(.24,.15);
            float3 primary = FlowNoise(flow);
            float3 gust = FlowNoise(flow*1.72 + float2(5.2,8.1) +
                                    float2(primary.x,-primary.x)*.84);
            float2 gradient = primary.yz + gust.yz*.48;
            float2 curl = float2(gradient.y,-gradient.x) + float2(.36,.17);
            float crest = smoothstep(.23,.86,.53*primary.x+.47*gust.x);
            float2 wind = curl * rsqrt(1+dot(curl,curl)) *
                          (_WindStrength*(.33+.67*crest));
            return float3(wind,crest);
        }

        Varyings Vert(Attributes input)
        {
            Varyings output;
            float3 rootWS = TransformObjectToWorld(input.position);
            float3 normalWS = TransformObjectToWorldNormal(input.normal);
            float2 rootXZ = rootWS.xz;
            float shellH = input.shell.x;
            float taper = smoothstep(.035,.49,input.biome.r) * input.shell.y;

            // Select shells by projected height, including in orthographic views.
            // Use the centre shell as the distant meadow surface with a continuous colour field.
            float viewDepth = lerp(max(1,-TransformWorldToView(rootWS).z),1,unity_OrthoParams.w);
            float pixels = _ScaledScreenParams.y * abs(UNITY_MATRIX_P._m11) * .5 * _ShellThickness / viewDepth;
            float layer = round(shellH*11);
            float minimumPixels = layer == 6 ? 0 : layer == 3 || layer == 9 ? 18 : 22;
            bool extraShell = layer == 1 || layer == 4 || layer == 7 || layer == 10;
            float seam = extraShell ? input.biome.a : 1;
            output.shellVisibility = min(pixels-minimumPixels,seam*32-.015);
            float detail = smoothstep(16,23,pixels);
            float distanceFade = smoothstep(65,125,length(_WorldSpaceCameraPos-rootWS));
            taper *= lerp(lerp(1,.62,distanceFade),1,detail);

            float3 flow = 0;
            #if defined(SHADER_API_D3D11)
            [branch] if (output.shellVisibility > -1.5)
            #endif
                flow = SwirlWind(rootXZ,_Time.y*_WindSpeed);
            float2 wind = flow.xy;
            float bend = shellH * shellH * taper;
            float3 displacement = float3(normalWS.x*.40,normalWS.y,normalWS.z*.40) *
                                  (shellH * _ShellThickness * taper);
            displacement.xz += wind*bend;
            displacement.y -= dot(wind,wind) * .52 * bend;

            output.positionWS = rootWS + displacement;
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = normalWS + float3(wind.x,0,wind.y)*(.92*shellH);
            output.rootXZ = rootXZ;
            output.shell = half4(shellH, input.biome.r, input.biome.g, input.biome.b);
            output.shade = half3(flow.z,1-detail,seam);
            return output;
        }

        float2 StrandMargin(float2 coords, float shell, float coverage, float gold, float seed)
        {
            float3 grain = StrandNoise(floor(coords)+seed);
            float2 offset = frac(coords)-lerp(.29,.71,grain.xy);
            float sampleHeight = shell + (grain.x-.5)*.085;
            float tip = lerp(.46,1.07,grain.z)*smoothstep(.045,.43,coverage);
            float petal = gold*smoothstep(.40,.84,shell)*smoothstep(.38,.83,grain.z);
            float radius = lerp(.61,.093,saturate(sampleHeight)) + petal*.13;
            return float2(min(tip-sampleHeight, radius*radius-dot(offset,offset)),grain.z);
        }

        half ClipVelvet(Varyings input)
        {
            float height = input.shell.x;
            float coverage = input.shell.y;
            #if !defined(SHADER_API_D3D11)
                clip(input.shellVisibility);
            #endif
            clip(coverage - .04);
            float2 coords = input.rootXZ * _StrandDensity;
            if (input.shade.z < .88)
            {
                float transition = StrandNoise(floor(coords)+27.5).z;
                clip(input.shade.z-lerp(.10,.88,transition));
            }

            [branch] if (input.shade.y > .70 && abs(height-6.0/11.0) < .016)
            {
                float grain = StrandNoise(floor(input.rootXZ * 3.8)).z;
                clip(coverage - lerp(.045,.26,grain));
                float2 p = input.rootXZ*float2(1.65,1.18);
                return .5 + .21*sin(p.x+1.7*cos(p.y*.83)) + .15*cos(1.2*p.y-.57*p.x) + .18*(grain-.5);
            }

            coords += .21*sin(coords.yx*float2(.27,.23)+float2(.5,1.7));
            if (height < .02)
            {
                float grain = StrandNoise(floor(coords)).z;
                clip(coverage - lerp(.045,.20,grain));
                return grain;
            }

            // Keep either overlapping strand if it covers this shell.
            float2 a = StrandMargin(coords,height,coverage,input.shell.z,0);
            float2 b = StrandMargin(coords+.5,height,coverage,input.shell.z,53.7);
            float2 strand = a.x > b.x ? a : b;
            clip(strand.x);
            return strand.y;
        }

        half4 Frag(Varyings input) : SV_Target
        {
            half variation = ClipVelvet(input);
            half height = input.shell.x;
            half coverage = input.shell.y;
            half gold = input.shell.z;
            half forest = input.shell.w;
            half3 green = lerp(_GrassRootCol.rgb, _GrassMidCol.rgb, smoothstep(0,.58,height));
            green *= lerp(1,lerp(.92,1.06,variation),input.shade.y);
            green = lerp(green, _GrassTipCol.rgb, smoothstep(.42,1,height));
            green = lerp(green, _SandGrassCol.rgb * lerp(.62,1.05,height), (1-smoothstep(.10,.48,coverage))*.72);

            half bloom = smoothstep(.28,.70,height + (variation-.5)*.16);
            half3 petal = lerp(_GoldMidCol.rgb,_GoldTipCol.rgb,smoothstep(.50,1,height)*(.65+.35*variation));
            half3 flower = lerp(_GoldRootCol.rgb,petal,bloom);
            half3 albedo = lerp(green,flower,gold);

            Light sun = GetMainLight();
            half3 normal = normalize(input.normalWS);
            half daylight = saturate(dot(normal,sun.direction)*.55+.45);
            half3 lightColor = lerp(half3(1,.96,.88),min(sun.color,1.45),.62);
            half3 light = half3(.34,.40,.37)+lightColor*daylight*.62;
            half occlusion = lerp(.66,1,sqrt(height)) * lerp(1,.83,forest);
            half crest = input.shade.x * height * height;
            half3 sheen = lerp(half3(.065,.145,.065),half3(.153,.125,.035),gold);
            half viewRim = 1-saturate(dot(normal,GetWorldSpaceNormalizeViewDir(input.positionWS)));
            half3 color = albedo * light * (occlusion + viewRim*viewRim*height*.11);
            color += lightColor * sheen * crest;

            float depth = TransformWorldToView(_CoastAtmosphereV37.xyz).z-TransformWorldToView(input.positionWS).z;
            half fog = smoothstep(24,86,depth)*_CoastMistV37.a*.60*step(.01,_CoastAtmosphereV37.w);
            return half4(lerp(color,_CoastMistV37.rgb,fog),1);
        }

        half4 FragDepth(Varyings input) : SV_Target
        {
            ClipVelvet(input);
            return 0;
        }
        ENDHLSL

        Pass
        {
            Name "VelvetShellForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex Vert
            #pragma fragment FragDepth
            ENDHLSL
        }
    }
}
