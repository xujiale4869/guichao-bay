Shader "Deepsea/V24CoastShowcaseSurface" {
  Properties {
    _BaseMap("Base colour",2D)="white"{} _BumpMap("Normal",2D)="bump"{} _MaskMap("ARM",2D)="white"{}
    _SandMap("Sand",2D)="white"{} _CoverMap("Ground cover",2D)="white"{} _CausticMap("Caustics",2D)="black"{}
    _BaseColor("Tint",Color)=(1,1,1,1) _Mode("Rock Ground Wood Bark Foliage Coral Kelp Solid",Float)=0
    _Scale("Texture metres",Float)=.28 _NormalStrength("Normal",Float)=.4 _UseMask("Use ARM",Float)=0
    _Roughness("Roughness",Float)=.8 _Metallic("Metallic",Float)=0 _Cutoff("Alpha cut",Float)=.35
    _Sway("Sway",Float)=0 _Emission("Emission",Float)=0
    _AtlasCell("Coral atlas cell",Vector)=(0,0,.5,.5)
  }
  SubShader {Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Cull Off
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);TEXTURE2D(_MaskMap);SAMPLER(sampler_MaskMap);
    TEXTURE2D(_SandMap);SAMPLER(sampler_SandMap);TEXTURE2D(_CoverMap);SAMPLER(sampler_CoverMap);TEXTURE2D(_CausticMap);SAMPLER(sampler_CausticMap);
    float _OceanV24UnderLOD;
    CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;float4 _AtlasCell;float _Mode,_Scale,_NormalStrength,_UseMask,_Roughness,_Metallic,_Cutoff,_Sway,_Emission;
    CBUFFER_END
    float _ShowcaseClay;
    float4 _CoastAtmosphereV37;
    half4 _CoastMistV37;
    struct A{float4 p:POSITION;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;half4 c:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
    struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float4 t:TEXCOORD2;float2 uv:TEXCOORD3;half4 c:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
    V Vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,o);o.w=TransformObjectToWorld(a.p.xyz);
    float sway=sin(_Time.y*1.3+o.w.x*.8+o.w.z*.4)*_Sway*a.c.a;
    o.w.x+=sway;o.w.z+=sway*.36;o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(a.n);o.t=float4(TransformObjectToWorldDir(a.t.xyz),a.t.w*GetOddNegativeScale());o.uv=a.uv;o.c=a.c;return o;}
    half4 Base(V i,float3 weight){
      if(abs(_Mode-5)<.1)return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,_AtlasCell.xy+.003+frac(i.uv*2)*.494);
      if(_Mode>1.5)return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
      return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.w.zy*_Scale)*weight.x+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.w.xz*_Scale)*weight.y+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.w.xy*_Scale)*weight.z;
    }
    void ClipLeaf(V i){if(abs(_Mode-4)<.1)clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a-_Cutoff);}

    half3 OceanLowLight(float3 p,float3 n,half3 albedo){
      Light sun=GetMainLight(TransformWorldToShadowCoord(p));
      half3 ambient=max(half3(.075,.11,.13),SampleSH(n));
      return albedo*(ambient+sun.color*saturate(dot(n,sun.direction))*.96*sun.shadowAttenuation);
    }

    half3 OceanLow(V i){
      float3 n=normalize(i.n),w=pow(abs(n),4);w/=max(.001,dot(w,1));
      half3 albedo=Base(i,w).rgb*_BaseColor.rgb*half3(.9184,.9712,.9568);
      if(abs(_Mode-1)<.1){float sand=smoothstep(.50,.91,n.y)*(1-saturate(i.c.r));half3 grit=SAMPLE_TEXTURE2D(_SandMap,sampler_SandMap,i.w.xz*.17).rgb;grit=lerp(half3(.57,.58,.47),grit,.6)*half3(.92,.92,.80);albedo=lerp(albedo,grit,sand);}
      half3 c=OceanLowLight(i.w,n,albedo);float depth=-.22-i.w.y;
      c+=albedo*half3(.12,.17,.18)*exp(-depth*.10);
      if(abs(_Mode-5)<.1)c.r*=min(24,exp(depth*1.12));
      return c+albedo*_Emission;
    }

    ENDHLSL
    Pass{Name "ShowcaseLit" Tags{"LightMode"="UniversalForward"}
    HLSLPROGRAM
    #pragma target 3.5
    #pragma vertex Vert
    #pragma fragment Frag
    #pragma multi_compile_instancing
    #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
    #pragma multi_compile_fragment _ _SHADOWS_SOFT
    half4 Frag(V i):SV_Target {UNITY_SETUP_INSTANCE_ID(i);float budget=smoothstep(.25,.85,-.22-i.w.y)*_OceanV24UnderLOD*((_Mode<1.5||(_Mode>4.5&&_Mode<7.5))?1:0)*step(-.22,_WorldSpaceCameraPos.y);if(budget>.999)return half4(OceanLow(i),1);ClipLeaf(i);float3 n=normalize(i.n),geom=n,weight=pow(abs(n),4);weight/=max(.001,dot(weight,1));
      half4 tex=Base(i,weight);half3 albedo=tex.rgb*_BaseColor.rgb;float rough=_Roughness,ao=1;
      float2 uv=i.uv;if(_Mode<1.5)uv=(weight.x>weight.y&&weight.x>weight.z?i.w.zy:weight.y>weight.z?i.w.xz:i.w.xy)*_Scale;
      if(_UseMask>.5){half3 arm=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,uv).rgb;ao=lerp(.76,1,arm.r);rough=arm.g;}
      if(_NormalStrength>.01){float3 b=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uv));
        float3 bump;if(_Mode<1.5)bump=weight.x>weight.y&&weight.x>weight.z?float3(sign(n.x)*b.z,b.y,b.x):weight.y>weight.z?float3(b.x,sign(n.y)*b.z,b.y):float3(b.x,b.y,sign(n.z)*b.z);
        else{float3 tangent=normalize(i.t.xyz);bump=tangent*b.x+cross(n,tangent)*i.t.w*b.y+n*b.z;}n=normalize(lerp(n,bump,_NormalStrength));}
      float sandV37=0;
      if(_Mode<1.5){
        float patch=.5+.22*sin(i.w.x*.37+sin(i.w.z*.24)*2)+.18*sin(i.w.z*.54-i.w.x*.21);
        float land=smoothstep(.1,.8,i.w.y),moss=smoothstep(.42,.88,geom.y)*land*smoothstep(.30,.72,patch);
        half3 cover=SAMPLE_TEXTURE2D(_CoverMap,sampler_CoverMap,i.w.xz*.31).rgb;cover=lerp(dot(cover,half3(.299,.587,.114)).xxx,cover,.25)*half3(.32,.42,.21);
        albedo=lerp(albedo,cover,moss*.65);
        if(_Mode>.5){float sand=smoothstep(.50,.91,geom.y)*(1-smoothstep(-8,-4,i.w.y)*0);sand*=1-saturate(i.c.r);sandV37=sand;
          half3 grit=SAMPLE_TEXTURE2D(_SandMap,sampler_SandMap,i.w.xz*.17).rgb;grit=lerp(half3(.57,.58,.47),grit,.6)*half3(.94,.92,.81);albedo=lerp(albedo,grit,sand);
          float coverWeight=saturate(i.c.r)*smoothstep(.57,.94,geom.y);albedo=lerp(albedo,cover,coverWeight*.88);}
      }
      if(abs(_Mode-3)<.1){float grain=.78+.22*sin(i.w.y*8+sin(i.w.x*12)*1.2);albedo*=grain;}
      if(abs(_Mode-5)<.1){float pores=.88+.12*sin(i.w.x*28+sin(i.w.z*23))*sin(i.w.y*31);albedo*=pores;}
      if(abs(_Mode-8)<.1){half greyStone=dot(albedo,half3(.299,.587,.114));albedo=lerp(greyStone.xxx,albedo,.38);
        float patch=.5+.24*sin(i.w.x*.83+sin(i.w.z*.61)*2)+.2*sin(i.w.z*1.37+i.w.x*.53);
        float moss=smoothstep(.58,.9,geom.y)*smoothstep(.45,.74,patch)*smoothstep(.2,1.3,i.w.y);albedo=lerp(albedo,albedo*half3(.55,.68,.34),moss*.52);}
      float wet=1-smoothstep(-.45,.6,i.w.y);albedo*=lerp(half3(1,1,1),half3(.88,.96,.94),wet*.32);

      // =====================================================================
      // v37.1 精修 1：消除“灰黑窄脏边”湿沙，改为【宽幅渐变、带海浪舌状边缘的温暖深金琥珀退潮湿滩】
      // =====================================================================
      float wetSand=0;
      if(_CoastAtmosphereV37.w>0 && _ShowcaseClay<.5){
        // 双频有机退潮水迹舌 (Organic Tide Swash Tongues)，打破规则等高线硬边
        float swashTongue = .085 * sin(i.w.x * .62 + cos(i.w.z * .48) * 1.7)
                          + .045 * cos(i.w.z * 1.15 - sin(i.w.x * .85) * 1.4);
        float shore = i.w.y + .22 + swashTongue;
        // 将干湿过渡带从原来的 0.43m 大幅拓宽至 1.05m，二次方丝滑渐变，绝不描黑线！
        float upperFade = 1.0 - smoothstep(-.02, 1.05, shore);
        float lowerFade = smoothstep(-.48, -.06, shore);
        float band = (upperFade * upperFade) * lowerFade;
        wetSand = band * sandV37;

        // 温暖润泽的深金琥珀沙色 (Warm Golden Amber Wet Sand)，拒绝死灰黑色！
        albedo *= lerp(half3(1,1,1), half3(.78, .66, .52), wetSand * .85);
        rough = lerp(rough, .10, wetSand);

        // 岩石与木桩水线处仅保留极轻柔的清透水润青苔色，绝不压黑
        if(_Mode<.5||abs(_Mode-2)<.1||abs(_Mode-8)<.1){
          float stain=(1-smoothstep(.02,.32,abs(shore)))*(1-smoothstep(.45,.83,geom.y));
          albedo*=lerp(half3(1,1,1),half3(.72,.82,.75),stain*.28);
        }
      }
      if(_ShowcaseClay>.5){albedo=half3(.52,.52,.52);n=geom;rough=.95;}

      // 提高最低环境天光托底，防止 PBR 在桥下与石缝算出死黑
      SurfaceData s=(SurfaceData)0;s.albedo=albedo;s.metallic=_Metallic;s.specular=half3(.04,.04,.04);s.smoothness=1-rough;s.normalTS=half3(0,0,1);s.occlusion=ao;s.alpha=1;s.emission=albedo*_Emission;
      InputData input=(InputData)0;input.positionWS=i.w;input.normalWS=n;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);input.shadowCoord=TransformWorldToShadowCoord(i.w);
      float depth=-.22-i.w.y;
      bool refinedShore=_CoastAtmosphereV37.w>0 && _ShowcaseClay<.5 && depth<.26;
      half3 skyFloor=refinedShore ? lerp(half3(.075,.11,.13),half3(.16,.26,.36),smoothstep(-.22,.24,-depth)*_CoastAtmosphereV37.w) : half3(.075,.11,.13);
      input.bakedGI=max(skyFloor,SampleSH(n));
      input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);input.shadowMask=half4(1,1,1,1);
      half3 c=UniversalFragmentPBR(input,s).rgb;

      Light sun=GetMainLight(input.shadowCoord);
      if(abs(_Mode-4)<.1){float back=saturate(dot(-n,sun.direction));c+=albedo*sun.color*(refinedShore ? .34*pow(back,.65) : .22*pow(back,.7));}
      if(depth>.12&&depth<12&&geom.y>.15){float2 cuv=i.w.xz*.11+.04*float2(sin(i.w.z*.6+_Time.y*.3),cos(i.w.x*.5-_Time.y*.23));float ca=SAMPLE_TEXTURE2D(_CausticMap,sampler_CausticMap,cuv+float2(_Time.y*.002,0)).r;c+=half3(.20,.29,.27)*ca*exp(-depth*.19)*saturate(geom.y)*sun.shadowAttenuation;}
      if(depth>0)c+=albedo*half3(.12,.17,.18)*exp(-depth*.10);
      if(depth>0)c+=albedo*half3(.09,.14,.16)*(1-sun.shadowAttenuation)*exp(-depth*.09);
      if(abs(_Mode-5)<.1&&depth>0&&_WorldSpaceCameraPos.y>-.22)c.r*=min(24,exp(depth*1.12));
      if(_WorldSpaceCameraPos.y<-.25){float f=1-exp(-distance(_WorldSpaceCameraPos,i.w)*.11);c=lerp(c,half3(.018,.10,.13),f);}

      // =====================================================================
      // v37.1 精修 2：彻底消灭木栈桥下方与礁石根部的“死黑团块”！
      // 废除旧版把阴影乘暗的 `c *= lerp(1, half3(.74,.88,1.01), shadow*.63)`，
      // 改为注入【吉卜力绘本风的通透蔚蓝天光漫射补光 (Translucent Sky Bounce Lift)】！
      // =====================================================================
      if(refinedShore){
        float air=smoothstep(-.22,.24,-depth);
        float shadow=(1-sun.shadowAttenuation)*air*_CoastAtmosphereV37.w;

        // 关键：为背光面与桥下阴影提供清澈的蔚蓝天光托底，既拉开冷暖色温对比，又让暗部木板与礁石纹理 100% 通透清晰！
        half3 coolSkyBounce = albedo * half3(.22, .35, .48);
        c = max(c, coolSkyBounce * (.65 * air));
        c += albedo * half3(.05, .12, .20) * shadow;

        // 强化受光面与树冠/栈桥边缘的暖金逆光镶边 (Warm Golden Rim Light)
        float facing=saturate(dot(geom,input.viewDirectionWS));
        float rim=pow(1-facing,3.4)*saturate(dot(geom,sun.direction)+.28);
        c+=albedo*sun.color*half3(1.08,.95,.76)*rim*sun.shadowAttenuation*air*_CoastAtmosphereV37.w*.42;

        float along=TransformWorldToView(_CoastAtmosphereV37.xyz).z-TransformWorldToView(i.w).z;
        float distant=smoothstep(14,76,along);
        float coastal=1-smoothstep(1,11,i.w.y+.22);
        c=lerp(c,_CoastMistV37.rgb,distant*(.45+.55*coastal)*air*_CoastMistV37.a);
      }

      if(wetSand>.001){
        // 温润清澈的退潮水膜：倒映蔚蓝天光与温暖夕阳金光，在阴影里也保持清透水光而不发黑
        float3 filmNormal=normalize(lerp(geom,float3(0,1,0),.42));
        float viewCos=saturate(dot(filmNormal,input.viewDirectionWS));
        float fresnel=.025+.975*pow(1-viewCos,4.2);
        float3 reflected=reflect(-input.viewDirectionWS,filmNormal);
        float horizon=1-smoothstep(.04,.72,reflected.y);
        half3 sky=lerp(half3(.38,.58,.76),half3(.68,.84,.96),horizon);
        float moonlight=lerp(.32,1,saturate(sun.color.r));
        float specCos=saturate(dot(reflected,sun.direction));
        float reflectedSun=pow(specCos,80)*1.45+pow(specCos,20)*.22;
        float sunMask=saturate(dot(filmNormal,sun.direction))*(.25+.75*sun.shadowAttenuation);
        c=lerp(c,sky,wetSand*saturate((.06+fresnel)*1.45)*moonlight);
        c+=sun.color*half3(1.05,.94,.78)*(wetSand*reflectedSun*sunMask*(.25+fresnel));
      }
      if(budget>0)c=lerp(c,OceanLow(i),budget);return half4(c,1);}
    ENDHLSL
    }
    Pass{Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask 0
    HLSLPROGRAM
    #pragma target 3.5
    #pragma vertex Vert
    #pragma fragment Depth
    #pragma multi_compile_instancing
    half4 Depth(V i):SV_Target{UNITY_SETUP_INSTANCE_ID(i);ClipLeaf(i);return 0;}
    ENDHLSL
    }
    Pass{Name "ShadowCaster" Tags{"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
    HLSLPROGRAM
    #pragma target 3.5
    #pragma vertex ShadowVert
    #pragma fragment Depth
    #pragma multi_compile_instancing
    float3 _LightDirection;
    V ShadowVert(A a){V o=Vert(a);o.p=TransformWorldToHClip(ApplyShadowBias(o.w,normalize(o.n),_LightDirection));
    #if UNITY_REVERSED_Z
    o.p.z=min(o.p.z,o.p.w*UNITY_NEAR_CLIP_VALUE);
    #else
    o.p.z=max(o.p.z,o.p.w*UNITY_NEAR_CLIP_VALUE);
    #endif
    return o;}
    half4 Depth(V i):SV_Target{UNITY_SETUP_INSTANCE_ID(i);ClipLeaf(i);return 0;}
    ENDHLSL
    }
  }
}
