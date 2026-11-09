Shader "Deepsea/CoastalEcologyV33"
{
    Properties
    {
        [MainColor] _BaseColor("Tint",Color)=(1,1,1,1)
        [MainTexture] _BaseMap("Leaf Atlas",2D)="white"{}
        _LeafAtlas("Use leaf alpha atlas",Float)=0
        _Cutoff("Leaf cutoff",Range(0,1))=.35
        _Wind("Tip wind in metres",Range(0,.25))=.04
        _Ember("Campfire glow",Float)=0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _LeafAtlas, _Cutoff, _Wind, _Ember;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            half4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 world : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            float2 uv : TEXCOORD2;
            half fog : TEXCOORD3;
            half4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        Varyings Vert(Attributes a)
        {
            Varyings o;
            UNITY_SETUP_INSTANCE_ID(a);
            UNITY_TRANSFER_INSTANCE_ID(a,o);
            o.world = TransformObjectToWorld(a.positionOS.xyz);
            float tip = _LeafAtlas > .5 ? a.color.a : saturate(a.uv.y);
            float gust = sin(_Time.y*(1.65+_Ember*5.4)+o.world.x*.41+o.world.z*.66);
            o.world += float3(1,.30*_Ember,.47)*gust*_Wind*tip*tip;
            o.positionCS = TransformWorldToHClip(o.world);
            o.normalWS = TransformObjectToWorldNormal(a.normalOS);
            o.uv = a.uv;
            o.color = a.color;
            o.fog = ComputeFogFactor(o.positionCS.z);
            return o;
        }
        half4 LeafSample(float2 uv)
        {
            half4 tex = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv);
            if (_LeafAtlas > .5) clip(tex.a-_Cutoff);
            return tex;
        }
        ENDHLSL

        Pass
        {
            Name "EcologyForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            half4 Frag(Varyings i, FRONT_FACE_TYPE front : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 atlas = LeafSample(i.uv);
                half3 leaf = i.color.rgb;
                if (_LeafAtlas > .5)
                {
                    half value = max(atlas.r,max(atlas.g,atlas.b));
                    half vertexValue = max(leaf.r,max(leaf.g,leaf.b));
                    leaf = (.25+.75*value)*(.64+.36*vertexValue);
                }
                half3 color = leaf*_BaseColor.rgb;
                if (_Ember > .5)
                {
                    half flicker = .92+.13*sin(_Time.y*13+i.world.x*4);
                    return half4(MixFog(color*flicker*1.7,i.fog),1);
                }
                half3 n = normalize(i.normalWS * IS_FRONT_VFACE(front,1,-1));
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(i.world));
                #else
                    float4 shadowCoord = TransformWorldToShadowCoord(i.world);
                #endif
                Light sun = GetMainLight(shadowCoord);
                half wrap = saturate(dot(n,sun.direction)*.62+.38);
                half3 light = max(half3(.35,.38,.32),SampleSH(n)) +
                    sun.color * (.56*wrap*sun.shadowAttenuation+.10);
                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint index = 0; index < lightCount; index++)
                {
                    Light local = GetAdditionalLight(index,i.world);
                    light += local.color*local.distanceAttenuation*saturate(dot(n,local.direction))*.46;
                }
                #endif
                color *= light*lerp(.73,1.0,i.color.a);
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "EcologyDepth"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Depth
            #pragma multi_compile_instancing
            half4 Depth(Varyings i) : SV_Target
            {
                LeafSample(i.uv);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "EcologyShadow"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            float3 _LightDirection;
            Varyings ShadowVert(Attributes a)
            {
                Varyings o = Vert(a);
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(o.world,o.normalWS,_LightDirection));
                #if UNITY_REVERSED_Z
                    o.positionCS.z = min(o.positionCS.z,o.positionCS.w*UNITY_NEAR_CLIP_VALUE);
                #else
                    o.positionCS.z = max(o.positionCS.z,o.positionCS.w*UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }
            half4 ShadowFrag(Varyings i) : SV_Target
            {
                LeafSample(i.uv);
                return 0;
            }
            ENDHLSL
        }
    }
}
