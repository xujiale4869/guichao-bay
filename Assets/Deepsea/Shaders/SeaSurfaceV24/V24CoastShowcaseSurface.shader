Shader "Deepsea/V24CoastShowcaseSurface" {
  Properties {
    _BaseMap("Base colour",2D)="white"{} _BumpMap("Normal",2D)="bump"{} _MaskMap("ARM",2D)="white"{}
    _SandMap("Sand",2D)="white"{} _CoverMap("Ground cover",2D)="white"{} _CausticMap("Caustics",2D)="black"{}
    _BaseColor("Tint",Color)=(1,1,1,1) _Mode("Rock Ground Wood Bark Foliage Coral Kelp Solid",Float)=0
    _Scale("Texture metres",Float)=.28 _NormalStrength("Normal",Float)=.4 _UseMask("Use ARM",Float)=0
    _Roughness("Roughness",Float)=.8 _Metallic("Metallic",Float)=0 _Cutoff("Alpha cut",Float)=.35
    _Sway("Sway",Float)=0 _Emission("Emission",Float)=0
    _CoastV40PlantTint("Plant vertex tint",Float)=0
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
    half4 _BaseColor;float4 _AtlasCell;float _Mode,_Scale,_NormalStrength,_UseMask,_Roughness,_Metallic,_Cutoff,_Sway,_Emission,_CoastV40PlantTint;
    CBUFFER_END
    float _ShowcaseClay;
    float4 _CoastAtmosphereV37;
    float _CoastV40Presentation;
    half4 _CoastMistV37;
    struct A{float4 p:POSITION;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;half4 c:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
    struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float4 t:TEXCOORD2;float2 uv:TEXCOORD3;half4 c:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
    static const half3 MeadowRoot = half3(.22,.35,.16);
    static const half3 ForestLitter = half3(.31,.225,.132);

    half3 MeadowLight(float3 n, Light sun){
      half3 daylight=lerp(half3(.98,.94,.84),min(sun.color,1.45),.56);
      return half3(.34,.40,.37)+daylight*saturate(dot(n,sun.direction)*.52+.48)*.57;
    }

    half3 MeadowMist(half3 color,float3 world){
      float along=TransformWorldToView(_CoastAtmosphereV37.xyz).z-TransformWorldToView(world).z;
      float fog=smoothstep(24,86,along)*_CoastMistV37.a*.60*step(.01,_CoastAtmosphereV37.w);
      return lerp(color,_CoastMistV37.rgb,fog);
    }

    V Vert(A a){
      V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,o);
      o.w=TransformObjectToWorld(a.p.xyz);
      o.uv=a.uv;
      if(abs(_Mode-9)<.1){
        float phase=_Time.y*2.4-(o.w.x*.34+o.w.z*.26);
        float gust=sin(phase)*.68+cos(phase*1.85+o.w.z*.42)*.32;
        float sway=gust*_Sway*a.c.a;
        o.w+=float3(sway,-abs(sway)*.16,sway*.65);
        o.uv.y=saturate(gust*.48+.48)*a.c.a;
      } else if (_Sway != 0) {
        float amplitude = _Sway * a.c.a;
        if (_CoastAtmosphereV37.w > 0 && _Sway > 0) {
          float major = sin(_Time.y*1.45+o.w.x*.75+o.w.z*.42);
          float flutter = cos(_Time.y*3.10+o.w.x*2.10-o.w.z*1.85)*.28;
          amplitude *= abs(_Mode-4)<.1 ? 1.35 : 1;
          float sway = (major+flutter)*amplitude;
          o.w.x += sway;
          o.w.z += sway*.42;
          o.w.y += flutter*amplitude*.18;
        } else {
          float sway = sin(_Time.y*1.3+o.w.x*.8+o.w.z*.4)*amplitude;
          o.w.x += sway;
          o.w.z += sway*.36;
        }
      }
      o.p=TransformWorldToHClip(o.w);
      o.n=TransformObjectToWorldNormal(a.n);
      o.t=float4(TransformObjectToWorldDir(a.t.xyz),a.t.w*GetOddNegativeScale());
      o.c=a.c;
      return o;
    }
    half4 Base(V i,float3 weight,float3 uv,float3 dx,float3 dy){
      if(abs(_Mode-5)<.1)return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,_AtlasCell.xy+.003+frac(i.uv*2)*.494);
      if(_Mode>1.5)return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
      half4 top=SAMPLE_TEXTURE2D_GRAD(_BaseMap,sampler_BaseMap,uv.xz,dx.xz,dy.xz);
      [branch] if(weight.y>=.98)return top;
      half4 sides=SAMPLE_TEXTURE2D_GRAD(_BaseMap,sampler_BaseMap,uv.zy,dx.zy,dy.zy)*weight.x
          +SAMPLE_TEXTURE2D_GRAD(_BaseMap,sampler_BaseMap,uv.xy,dx.xy,dy.xy)*weight.z;
      return lerp(top*weight.y+sides,top,smoothstep(.94,.98,weight.y));
    }
    void ClipLeaf(V i){if(abs(_Mode-4)<.1)clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a-_Cutoff);}

    half3 OceanLow(float3 n,half3 baseAlbedo,half3 sandSample,float sand,float depth,Light sun){
      half3 albedo=baseAlbedo*half3(.9184,.9712,.9568);
      if(abs(_Mode-1)<.1){
        half3 grit=lerp(half3(.57,.58,.47),sandSample,.6)*half3(.92,.92,.80);
        albedo=lerp(albedo,grit,sand);
      }
      half3 ambient=max(lerp(half3(.075,.11,.13),half3(.16,.25,.34),saturate(_CoastAtmosphereV37.w)),SampleSH(n));
      half3 c=albedo*(ambient+sun.color*saturate(dot(n,sun.direction))*.96*sun.shadowAttenuation);
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
    half4 Frag(V i):SV_Target{
      UNITY_SETUP_INSTANCE_ID(i);
      [branch] if(abs(_Mode-9)<.1){
        Light daylight=GetMainLight();
        half3 grass=i.c.rgb*_BaseColor.rgb*MeadowLight(normalize(i.n),daylight);
        grass+=min(daylight.color,1.4)*half3(.075,.069,.031)*(i.uv.y*i.uv.y);
        return half4(MeadowMist(grass,i.w),1);
      }
      float depth=-.22-i.w.y;
      float budget=smoothstep(.25,.85,depth)*_OceanV24UnderLOD*((_Mode<1.5||(_Mode>4.5&&_Mode<7.5))?1:0);
      if(_CoastAtmosphereV37.w>0 && _ShowcaseClay<.5 && _Mode<1.5)
        budget=max(budget,smoothstep(1.35,2.7,depth));
      budget*=step(-.22,_WorldSpaceCameraPos.y);
      ClipLeaf(i);
      float3 n=normalize(i.n),geom=n,weight=pow(abs(n),4);weight/=max(.001,dot(weight,1));
      bool coastGround=_Mode>.5 && _Mode<1.5 && _CoastV40Presentation>.5 && _ShowcaseClay<.5;
      float grassWeight=saturate(i.c.r);
      float forestWeight=saturate(i.c.g);
      float inlandGround=coastGround ? smoothstep(.33,.78,grassWeight)*smoothstep(.30,.64,geom.y) : 0;
      half3 inlandAlbedo=MeadowRoot;
      [branch] if(inlandGround>.001){
        half3 cover=SAMPLE_TEXTURE2D(_CoverMap,sampler_CoverMap,i.w.xz*.31).rgb;
        half grain=dot(cover,half3(.299,.587,.114));
        inlandAlbedo=lerp(MeadowRoot,ForestLitter,smoothstep(.20,.90,forestWeight));
        inlandAlbedo*=lerp(.87,1.12,grain);
      }
      [branch] if(inlandGround>.999)
        return half4(MeadowMist(inlandAlbedo*MeadowLight(geom,GetMainLight()),i.w),1);
      float3 baseUV=i.w*_Scale;
      half3 baseAlbedo=Base(i,weight,baseUV,ddx(baseUV),ddy(baseUV)).rgb*_BaseColor.rgb;
      float sand=_Mode>.5 && _Mode<1.5 ? smoothstep(.50,.91,geom.y)*(1-saturate(i.c.r)) : 0;
      half3 sandSample=0;
      if(_Mode>.5 && _Mode<1.5)
        sandSample=SAMPLE_TEXTURE2D(_SandMap,sampler_SandMap,i.w.xz*.17).rgb;
      float4 shadowCoord=TransformWorldToShadowCoord(i.w);
      Light sun=GetMainLight(shadowCoord);
      [branch] if(budget>.999)return half4(OceanLow(geom,baseAlbedo,sandSample,sand,depth,sun),1);
      half3 albedo=baseAlbedo;float rough=_Roughness,ao=1;
      if(_CoastV40PlantTint>.5) albedo*=i.c.rgb;
      bool rockV40=_Mode<.5 || abs(_Mode-8)<.1;
      float stoneRelief=rockV40 && _ShowcaseClay<.5 ? _CoastV40Presentation*smoothstep(-.5,.18,i.w.y) : 0;
      albedo=lerp(albedo,lerp(half3(.43,.38,.30),albedo,.68),stoneRelief);
      float2 uv=i.uv;if(_Mode<1.5)uv=(weight.x>weight.y&&weight.x>weight.z?i.w.zy:weight.y>weight.z?i.w.xz:i.w.xy)*_Scale;
      if(_UseMask>.5){half3 arm=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,uv).rgb;ao=lerp(_CoastAtmosphereV37.w>0 ? .80 : .76,1,arm.r);rough=arm.g;}
      if(_NormalStrength>.01){float3 b=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uv));
        float3 bump;if(_Mode<1.5)bump=weight.x>weight.y&&weight.x>weight.z?float3(sign(n.x)*b.z,b.y,b.x):weight.y>weight.z?float3(b.x,sign(n.y)*b.z,b.y):float3(b.x,b.y,sign(n.z)*b.z);
        else{float3 tangent=normalize(i.t.xyz);bump=tangent*b.x+cross(n,tangent)*i.t.w*b.y+n*b.z;}n=normalize(lerp(n,bump,_NormalStrength*lerp(1,.84,stoneRelief)));}

      bool coastV38=_CoastAtmosphereV37.w>0 && _ShowcaseClay<.5;
      float sandV37=0;
      if(_Mode<1.5){
        float patch=.5+.22*sin(i.w.x*.37+sin(i.w.z*.24)*2)+.18*sin(i.w.z*.54-i.w.x*.21);
        float land=smoothstep(.1,.8,i.w.y),moss=smoothstep(.42,.88,geom.y)*land*smoothstep(.30,.72,patch);
        if(coastGround){
          float slope=smoothstep(.37,.78,geom.y);
          float dryLand=smoothstep(-.42,.08,i.w.y);
          half3 goldenSand=lerp(half3(.53,.43,.30),sandSample,.46)*half3(1,.96,.89);
          half3 loam=lerp(goldenSand,half3(.38,.36,.23),.72);
          float ecotone=smoothstep(.04,.36,grassWeight)*(1-smoothstep(.40,.80,grassWeight));
          sandV37=slope*(1-grassWeight);
          albedo=lerp(albedo,goldenSand,sandV37*dryLand);
          albedo=lerp(albedo,loam,ecotone*slope*.78);
          // Wet sand continues through the existing sea-floor and tidal reflection path.
          half3 dampSand=lerp(half3(.58,.58,.47),sandSample,.62)*half3(.95,.92,.81);
          albedo=lerp(albedo,dampSand,sandV37*(1-dryLand));
        }else{
          half3 cover=SAMPLE_TEXTURE2D(_CoverMap,sampler_CoverMap,i.w.xz*.31).rgb;
          cover=lerp(dot(cover,half3(.299,.587,.114)).xxx,cover,coastV38 ? .28 : .25)*(coastV38 ? half3(.34,.45,.23) : half3(.32,.42,.21));
          albedo=lerp(albedo,cover,moss*.65);
          if(_Mode>.5){
            sandV37=sand;
            float sandGrain = .94 + .06 * sin(i.w.x * 16.0 + cos(i.w.z * 18.0));
            half3 grit=coastV38 ? lerp(half3(.58,.58,.47),sandSample,.62)*half3(.95,.92,.81)*sandGrain : lerp(half3(.57,.58,.47),sandSample,.6)*half3(.94,.92,.81);
            albedo=lerp(albedo,grit,sand);
            float coverWeight=saturate(i.c.r)*smoothstep(.57,.94,geom.y);
            albedo=lerp(albedo,cover,coverWeight*.88);
          }
        }
      }

      // ============================================================
      // v38 全局画质提升 1：山崖与礁石手绘沉积岩层色带 + 迎光暖赭质感 (_Mode 0 & 8)
      // ============================================================
      if(coastV38 && (_Mode < .5 || abs(_Mode-8)<.1)){
        if(abs(_Mode-8)<.1){
          half greyStone=dot(albedo,half3(.299,.587,.114));
          albedo=lerp(greyStone.xxx,albedo,lerp(.42,.75,stoneRelief));
        }
        // 手绘风水平岩层裂隙与冷暖风化色阶 (Painterly Sedimentary Strata)
        float strata = sin(i.w.y * 3.8 + sin(i.w.x * .45 + i.w.z * .38) * 1.6);
        float warmLichen = smoothstep(.35, .88, geom.y) * smoothstep(.2, 4.5, i.w.y);
        albedo *= lerp(half3(.90, .93, .97), half3(1.07, 1.01, .92), saturate(strata * .5 + .5));
        albedo = lerp(albedo, albedo * half3(1.10, 1.02, .88), warmLichen * .32);

        float patch=.5+.24*sin(i.w.x*.83+sin(i.w.z*.61)*2)+.2*sin(i.w.z*1.37+i.w.x*.53);
        float moss=smoothstep(.58,.9,geom.y)*smoothstep(.45,.74,patch)*smoothstep(.2,1.3,i.w.y);
        albedo=lerp(albedo,albedo*half3(.55,.70,.36),moss*.50);
      }

      if(!coastV38 && abs(_Mode-8)<.1){
        half greyStone=dot(albedo,half3(.299,.587,.114));
        albedo=lerp(greyStone.xxx,albedo,.38);
        float patch=.5+.24*sin(i.w.x*.83+sin(i.w.z*.61)*2)+.2*sin(i.w.z*1.37+i.w.x*.53);
        float moss=smoothstep(.58,.9,geom.y)*smoothstep(.45,.74,patch)*smoothstep(.2,1.3,i.w.y);
        albedo=lerp(albedo,albedo*half3(.55,.68,.34),moss*.52);
      }

      // ============================================================
      // v38 全局画质提升 2：绘本级树冠体积色阶渐变 (_Mode == 4)
      // 树梢迎光处染上温暖金橘/暖翠光泽，树冠下部沉淀清透的蓝绿冷调
      // ============================================================
      if(coastV38 && abs(_Mode-4)<.1){
        float canopyHeightGrad = saturate(geom.y * .55 + .45);
        float leafClusterVar = .5 + .5 * sin(i.w.x * 1.4 + cos(i.w.z * 1.3) * 1.8 + i.w.y * .9);
        half3 coolCanopyBase = albedo * half3(.82, .92, 1.04);
        half3 warmCanopyCrown = albedo * half3(1.16, 1.05, .84);
        albedo = lerp(coolCanopyBase, warmCanopyCrown, saturate(canopyHeightGrad * .68 + leafClusterVar * .32));
      }

      if(abs(_Mode-3)<.1){float grain=.78+.22*sin(i.w.y*8+sin(i.w.x*12)*1.2);albedo*=grain;}
      if(abs(_Mode-5)<.1){float pores=.88+.12*sin(i.w.x*28+sin(i.w.z*23))*sin(i.w.y*31);albedo*=pores;}

      float wet=1-smoothstep(-.45,.6,i.w.y);albedo*=lerp(half3(1,1,1),coastV38 ? half3(.90,.97,.95) : half3(.88,.96,.94),wet*(coastV38 ? .24 : .32));

      // 宽幅渐变暖琥珀退潮湿滩 + 穿帮修复：消除礁石贴水处的暗色压黑
      float wetSand=0;
      float shore=i.w.y+.22;
      if(_CoastAtmosphereV37.w>0 && _ShowcaseClay<.5){
        float swashTongue = .085 * sin(i.w.x * .62 + cos(i.w.z * .48) * 1.7) + .045 * cos(i.w.z * 1.15 - sin(i.w.x * .85) * 1.4);
        shore = i.w.y + .22 + swashTongue;
        float upperFade = 1.0 - smoothstep(-.02, 1.12, shore);
        float lowerFade = smoothstep(-.52, -.06, shore);
        float band = (upperFade * upperFade) * lowerFade;
        wetSand = band * sandV37;
        albedo *= lerp(half3(1,1,1), half3(.76, .64, .49), wetSand * .88);
        rough = lerp(rough, .10, wetSand);
      }
      if(stoneRelief>0){
        float lift=(1-smoothstep(.10,.45,dot(albedo,half3(.299,.587,.114))))*stoneRelief;
        albedo=lerp(albedo,max(albedo,half3(.33,.30,.25)),lift*.48);
        ao=lerp(ao,max(ao,.83),stoneRelief);
      }
      if(_ShowcaseClay>.5){albedo=half3(.52,.52,.52);n=geom;rough=.95;}

      SurfaceData s=(SurfaceData)0;s.albedo=albedo;s.metallic=_Metallic;s.specular=half3(.04,.04,.04);s.smoothness=1-rough;s.normalTS=half3(0,0,1);s.occlusion=ao;s.alpha=1;s.emission=albedo*_Emission;
      InputData input=(InputData)0;input.positionWS=i.w;input.normalWS=n;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);input.shadowCoord=shadowCoord;

      // ============================================================
      // v38 穿帮修复 ③：将 `refinedShore` 与 `air` 的天光托底范围延伸至水下 -0.85m！
      // 彻底修复之前 `air = smoothstep(-.22, .24, -depth)` 在贴水处归零导致红枫树下礁石根部出现一圈黑炭边的问题！
      // ============================================================
      bool refinedShore=_CoastAtmosphereV37.w>0 && _ShowcaseClay<.5 && depth<.85;
      float air=smoothstep(-.85,.28,-depth);
      half3 skyFloor=refinedShore ? lerp(half3(.075,.11,.13),half3(.20,.32,.44),air*_CoastAtmosphereV37.w) : half3(.075,.11,.13);
      input.bakedGI=max(skyFloor,SampleSH(n));
      input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);input.shadowMask=half4(1,1,1,1);
      half3 c=UniversalFragmentPBR(input,s).rgb;
      c=lerp(c,max(c,albedo*half3(.33,.37,.41)),stoneRelief);

      if(abs(_Mode-4)<.1){
        float back=saturate(dot(-n,sun.direction));
        float canopyRim=pow(1.0-saturate(dot(n,input.viewDirectionWS)),2.6)*saturate(sun.direction.y+.3);
        c+=albedo*sun.color*(refinedShore ? (.42*pow(back,.62) + .24*canopyRim) : .22*pow(back,.7));
      }
      if(depth>.12&&depth<12&&geom.y>.15){float2 cuv=i.w.xz*.11+.04*float2(sin(i.w.z*.6+_Time.y*.3),cos(i.w.x*.5-_Time.y*.23));float ca=SAMPLE_TEXTURE2D(_CausticMap,sampler_CausticMap,cuv+float2(_Time.y*.002,0)).r;c+=half3(.20,.29,.27)*ca*exp(-depth*.19)*saturate(geom.y)*sun.shadowAttenuation;}
      if(depth>0)c+=albedo*half3(.12,.17,.18)*exp(-depth*.10);
      if(depth>0)c+=albedo*half3(.09,.14,.16)*(1-sun.shadowAttenuation)*exp(-depth*.09);
      if(abs(_Mode-5)<.1&&depth>0&&_WorldSpaceCameraPos.y>-.22)c.r*=min(24,exp(depth*1.12));
      if(_WorldSpaceCameraPos.y<-.25){float f=1-exp(-distance(_WorldSpaceCameraPos,i.w)*.11);c=lerp(c,half3(.018,.10,.13),f);}

      if(refinedShore){
        float shadow=(1-sun.shadowAttenuation)*air*_CoastAtmosphereV37.w;
        half3 coolSkyBounce = albedo * half3(.26, .39, .52);
        c = max(c, coolSkyBounce * (.72 * air));
        c += albedo * half3(.06, .14, .22) * shadow;

        // 针对岸边礁石贴近水线处 (abs(shore) < 0.45m)，额外注入浅海翡翠水光漫射补光，彻底消除礁石黑底边！
        if(_Mode < .5 || abs(_Mode-8)<.1){
          float waterlineBand = 1.0 - smoothstep(.02, .48, abs(i.w.y + .22));
          c = max(c, albedo * half3(.24, .42, .48) * waterlineBand);
          c += half3(.04, .11, .13) * waterlineBand * _CoastAtmosphereV37.w;
        }

        float facing=saturate(dot(geom,input.viewDirectionWS));
        float rim=pow(1-facing,3.2)*saturate(dot(geom,sun.direction)+.30);
        c+=albedo*sun.color*half3(1.10,.96,.76)*rim*sun.shadowAttenuation*air*_CoastAtmosphereV37.w*.46;
        float along=TransformWorldToView(_CoastAtmosphereV37.xyz).z-TransformWorldToView(i.w).z;
        float distant=smoothstep(14,76,along);
        float coastal=1-smoothstep(1,11,i.w.y+.22);
        c=lerp(c,_CoastMistV37.rgb,distant*(.45+.55*coastal)*air*_CoastMistV37.a);
      }

      if(wetSand>.001){
        float3 filmNormal=normalize(lerp(geom,float3(0,1,0),.46));
        float viewCos=saturate(dot(filmNormal,input.viewDirectionWS));
        float fresnel=.035+.965*pow(1-viewCos,3.8);
        float3 reflected=reflect(-input.viewDirectionWS,filmNormal);
        float horizon=1-smoothstep(.04,.72,reflected.y);
        half3 sky=lerp(half3(.40,.60,.78),half3(.72,.86,.98),horizon);
        float moonlight=lerp(.32,1,saturate(sun.color.r));
        float specCos=saturate(dot(reflected,sun.direction));
        float reflectedSun=pow(specCos,64)*1.55+pow(specCos,16)*.28;
        float sunMask=saturate(dot(filmNormal,sun.direction))*(.28+.72*sun.shadowAttenuation);
        c=lerp(c,sky,wetSand*saturate((.08+fresnel)*1.55)*moonlight);
        c+=sun.color*half3(1.06,.95,.78)*(wetSand*reflectedSun*sunMask*(.30+fresnel));
      }
      if(budget>0)c=lerp(c,OceanLow(geom,baseAlbedo,sandSample,sand,depth,sun),budget);
      if(inlandGround>.001)
        c=lerp(c,MeadowMist(inlandAlbedo*MeadowLight(geom,sun),i.w),inlandGround);
      return half4(c,1);
    }
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
