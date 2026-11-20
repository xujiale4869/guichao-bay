Shader "Deepsea/VelvetShellGrassV44"
{
    Properties
    {
        _ShellThickness ("Shell Total Height", Range(0.15, 1.0)) = 0.58
        _StrandDensity ("Velvet Strand Density", Range(6, 18)) = 11.5
        _WindStrength ("Wind Wave Strength", Range(0, 0.9)) = 0.44
        _WindSpeed ("Wind Wave Speed", Range(0.2, 4)) = 1.65
        _GrassRootCol ("Grass Root Color", Color) = (0.068, 0.154, 0.087, 1)
        _GrassMidCol ("Grass Mid Color", Color) = (0.115, 0.315, 0.165, 1)
        _GrassTipCol ("Grass Tip Color", Color) = (0.285, 0.515, 0.245, 1)
        _CrimsonRootCol ("Crimson Root Color", Color) = (0.063, 0.102, 0.117, 1)
        _CrimsonMidCol ("Crimson Mid Color", Color) = (0.62, 0.055, 0.108, 1)
        _CrimsonTipCol ("Crimson Tip Color", Color) = (0.87, 0.15, 0.15, 1)
        _SandGrassCol ("Dune Transition Color", Color) = (0.34, 0.44, 0.26, 1)
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
            half4 _CrimsonRootCol;
            half4 _CrimsonMidCol;
            half4 _CrimsonTipCol;
            half4 _SandGrassCol;
        CBUFFER_END

        float4 _CoastAtmosphereV37;
        float4 _CoastMistV37;

        struct Attributes
        {
            float3 position : POSITION;
            float3 normal : NORMAL;
            float2 shell : TEXCOORD0;
            half3 biome : COLOR;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            float2 rootXZ : TEXCOORD2;
            half4 shell : TEXCOORD3;
            half2 shade : TEXCOORD4;
            #if defined(SHADER_API_D3D11)
                float shellVisibility : SV_ClipDistance0;
            #else
                float shellVisibility : TEXCOORD5;
            #endif
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            float3 rootWS = TransformObjectToWorld(input.position);
            float3 normalWS = TransformObjectToWorldNormal(input.normal);
            float2 rootXZ = rootWS.xz;
            float shellH = input.shell.x;
            float taper = smoothstep(.08, .55, input.biome.r) * input.shell.y;

            // Select shells by projected height, including in orthographic views.
            // Use the centre shell as the distant meadow surface with a continuous colour field.
            float viewDepth = lerp(max(1,-TransformWorldToView(rootWS).z),1,unity_OrthoParams.w);
            float pixels = _ScaledScreenParams.y * abs(UNITY_MATRIX_P._m11) * .5 * _ShellThickness / viewDepth;
            float layer = round(shellH*13);
            float minimumPixels = layer == 8 ? 0 : layer == 4 || layer == 11 ? 18 : 22;
            output.shellVisibility = pixels-minimumPixels;

            float time = _Time.y * _WindSpeed;
            const float2 windDir = float2(.84,.54);
            const float2 crosswind = float2(.42,-.91);
            float phase = dot(rootXZ, windDir) * .24 - time;
            float variation = dot(rootXZ, crosswind) * .16 - time * .72 + 1.3;
            float gust = .5 + .5 * sin(dot(rootXZ, float2(.11,.09)) - time * .42);
            float wave = sin(phase + .45 * sin(variation));
            float push = .42 + .29 * (1 + wave) * (.65 + .35 * gust);
            float2 wind = (windDir * push + crosswind * (.24 * cos(variation * 1.15 - .35 * wave))) * _WindStrength;

            float bend = shellH * shellH * taper;
            float3 displacement = float3(normalWS.x * .40, normalWS.y, normalWS.z * .40) *
                (shellH * _ShellThickness * taper);
            displacement.xz += wind * bend;
            displacement.y -= push * _WindStrength * .24 * bend;

            output.positionWS = rootWS + displacement;
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = normalWS;
            output.rootXZ = rootXZ;
            output.shell = half4(shellH, input.biome.r, input.biome.g, input.biome.b);
            output.shade = half2(.5 + .5 * sin(phase + .65),1-smoothstep(16,23,pixels));
            return output;
        }

        inline float3 StrandNoise(float2 cell)
        {
            float3 hash = frac(cell.xyx * float3(.1031, .1030, .0973));
            hash += dot(hash, hash.yzx + 33.33);
            return frac((hash.xxy + hash.yzz) * hash.zyx);
        }

        half ClipVelvet(Varyings input)
        {
            half height = input.shell.x;
            half coverage = input.shell.y;
            #if !defined(SHADER_API_D3D11)
                clip(input.shellVisibility);
            #endif
            clip(coverage - .09);
            [branch] if (input.shade.y > .88 && abs(height-8.0/13.0) < .016)
            {
                float patch = .5 + .5*sin(dot(input.rootXZ,float2(6.2,8.1)));
                clip(coverage - lerp(.11,.25,patch));
                return patch;
            }

            // Break up the grid and pin its pattern to the undeformed roots.
            float2 coords = input.rootXZ * _StrandDensity;
            coords += .29*sin(coords.yx*float2(.27,.23)+float2(.5,1.7));
            float3 noise = StrandNoise(floor(coords));
            clip(coverage - lerp(.09,.35,noise.z));
            if (height < .02) return noise.z;

            float2 delta = frac(coords) - lerp(.13,.87,noise.xy);
            float radius = lerp(.60,.115,height) + input.shade.y*.29;
            float stemHeight = lerp(.46,1.09,noise.z) * smoothstep(.085,.47,coverage);
            float petals = input.shell.z * smoothstep(.48,.87,height) * smoothstep(.46,.86,noise.z);
            radius += petals * .19;
            clip(min(stemHeight-height, radius*radius-dot(delta,delta)));
            return noise.z;
        }

        half4 Frag(Varyings input) : SV_Target
        {
            half variation = ClipVelvet(input);
            half height = input.shell.x;
            half coverage = input.shell.y;
            half crimson = input.shell.z;
            half forest = input.shell.w;
            half3 green = lerp(_GrassRootCol.rgb, _GrassMidCol.rgb, smoothstep(0,.58,height));
            green *= lerp(1,lerp(.82,1.09,variation),input.shade.y);
            green = lerp(green, _GrassTipCol.rgb, smoothstep(.42,1,height));
            green = lerp(green, _SandGrassCol.rgb * lerp(.62,1.05,height), (1-smoothstep(.10,.48,coverage))*.72);

            half bloom = smoothstep(.46,.76,height + (variation-.5)*.15);
            half3 petal = lerp(_CrimsonMidCol.rgb,_CrimsonTipCol.rgb,smoothstep(.50,1,height)*(.65+.35*variation));
            half3 red = lerp(_CrimsonRootCol.rgb,petal,bloom);
            half3 albedo = lerp(green,red,crimson);

            Light sun = GetMainLight();
            half3 normal = normalize(input.normalWS);
            half daylight = saturate(dot(normal,sun.direction)*.55+.45);
            half3 lightColor = lerp(half3(1,.96,.88),min(sun.color,1.45),.62);
            half3 light = half3(.34,.40,.37)+lightColor*daylight*.62;
            half occlusion = lerp(.81,1,sqrt(height)) * lerp(1,.86,forest);
            half crest = input.shade.x * height * height;
            half3 sheen = lerp(half3(.082,.15,.061),half3(.16,.032,.034),crimson);
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
