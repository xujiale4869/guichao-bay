Shader "Deepsea/CoastMapleSurface" {
Properties {
 _BaseMap("Base colour",2D)="white"{} _BumpMap("Normal",2D)="bump"{} _MaskMap("ARM",2D)="white"{}
 _LandMap("Land mineral",2D)="white"{} _SandMap("Sand",2D)="white"{} _CoverMap("Ground cover",2D)="white"{} _CausticMap("Caustics",2D)="black"{}
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
TEXTURE2D(_LandMap);SAMPLER(sampler_LandMap);TEXTURE2D(_SandMap);SAMPLER(sampler_SandMap);TEXTURE2D(_CoverMap);SAMPLER(sampler_CoverMap);TEXTURE2D(_CausticMap);SAMPLER(sampler_CausticMap);
CBUFFER_START(UnityPerMaterial)
half4 _BaseColor;float4 _AtlasCell;float _Mode,_Scale,_NormalStrength,_UseMask,_Roughness,_Metallic,_Cutoff,_Sway,_Emission;
CBUFFER_END
float _ShowcaseClay;
float4 _CoastAtmosphereV37;
struct A{float4 p:POSITION;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;half4 c:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float4 t:TEXCOORD2;float2 uv:TEXCOORD3;half4 c:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
V Vert(A a){
 V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,o);
 o.w=TransformObjectToWorld(a.p.xyz);
 if (_Sway != 0) {
  float amplitude = _Sway * a.c.a;
  if (_CoastAtmosphereV37.w > 0 && abs(_Mode-4) < .1 && _Sway > 0) {
   float major = sin(_Time.y*1.45+o.w.x*.75+o.w.z*.42);
   float flutter = cos(_Time.y*3.10+o.w.x*2.10-o.w.z*1.85)*.28;
   float sway = (major+flutter)*amplitude*1.35;
   o.w.x += sway;
   o.w.z += sway*.42;
   o.w.y += flutter*amplitude*.243;
  } else {
   float sway = sin(_Time.y*1.3+o.w.x*.8+o.w.z*.4)*amplitude;
   o.w.x += sway;
   o.w.z += sway*.36;
  }
 }
 o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(a.n);o.t=float4(TransformObjectToWorldDir(a.t.xyz),a.t.w*GetOddNegativeScale());o.uv=a.uv;o.c=a.c;return o;
}
half4 Base(V i,float3 weight){
 if(abs(_Mode-5)<.1)return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,_AtlasCell.xy+.003+frac(i.uv*2)*.494);
 if(_Mode>1.5)return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
 float3 uv=i.w*_Scale,dx=ddx(uv),dy=ddy(uv);
 float planar=smoothstep(.94,.98,weight.y);
 half4 top=SAMPLE_TEXTURE2D_GRAD(_BaseMap,sampler_BaseMap,uv.xz,dx.xz,dy.xz);
 [branch] if(weight.y>=.98)return top;
 half4 sides=SAMPLE_TEXTURE2D_GRAD(_BaseMap,sampler_BaseMap,uv.zy,dx.zy,dy.zy)*weight.x
     +SAMPLE_TEXTURE2D_GRAD(_BaseMap,sampler_BaseMap,uv.xy,dx.xy,dy.xy)*weight.z;
 return lerp(top*weight.y+sides,top,planar);
}
void ClipLeaf(V i){if(abs(_Mode-4)<.1)clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a-_Cutoff);}
ENDHLSL
Pass{Name "ShowcaseLit" Tags{"LightMode"="UniversalForward"}
HLSLPROGRAM
#pragma target 3.5
#pragma vertex Vert
#pragma fragment Frag
#pragma multi_compile_instancing
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _SHADOWS_SOFT
half4 Frag(V i):SV_Target {
 UNITY_SETUP_INSTANCE_ID(i);ClipLeaf(i);
 float3 n=normalize(i.n),geom=n,weight=pow(abs(n),4);weight/=max(.001,dot(weight,1));

 bool rock = _Mode < .5 || abs(_Mode-8)<.1;
 bool foliage = abs(_Mode-4)<.1;
 float depth=-.22-i.w.y;
 float air=smoothstep(-.85,.28,-depth);
 float refinement = _ShowcaseClay<.5 && (rock || foliage) ? _CoastAtmosphereV37.w*air : 0;
 half4 tex=Base(i,weight);half3 albedo=tex.rgb*_BaseColor.rgb;if(abs(_Mode-4)<.1)albedo*=i.c.rgb;float rough=_Roughness,ao=1;
 float2 uv=i.uv;if(_Mode<1.5)uv=(weight.x>weight.y&&weight.x>weight.z?i.w.zy:weight.y>weight.z?i.w.xz:i.w.xy)*_Scale;
 // 提高 ARM 贴图的最低 AO 下限 (从 .65 提至 .86)，彻底消除红枫下大礁石与前景礁石的死黑裂缝条带！
 if(_UseMask>.5){half3 arm=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,uv).rgb;ao=lerp(lerp(.65,.86,refinement),1,arm.r);rough=arm.g;}
 if(_NormalStrength>.01){
  float3 b=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uv));
  float3 bump;if(_Mode<1.5)bump=weight.x>weight.y&&weight.x>weight.z?float3(sign(n.x)*b.z,b.y,b.x):weight.y>weight.z?float3(b.x,sign(n.y)*b.z,b.y):float3(b.x,b.y,sign(n.z)*b.z);
  else{float3 tangent=normalize(i.t.xyz);bump=tangent*b.x+cross(n,tangent)*i.t.w*b.y+n*b.z;}
  // 对岩石降低裂隙法线黑度，保持温润手绘块面感
  float normStr = _NormalStrength * (rock ? lerp(1,.55,refinement) : 1);
  n=normalize(lerp(n,bump,normStr));
 }
 if(rock && refinement>0){
  float waterline = 1-smoothstep(.12,.85,abs(depth));
  n = normalize(lerp(n,float3(n.x,max(n.y,.22),n.z),waterline*refinement));
 }
 if(_Mode>.5&&_Mode<1.5){
  float patch=.5+.22*sin(i.w.x*.37+sin(i.w.z*.24)*2)+.18*sin(i.w.z*.54-i.w.x*.21);
  float land=smoothstep(.1,.8,i.w.y),moss=smoothstep(.42,.88,geom.y)*land*smoothstep(.30,.72,patch);
  half3 cover=SAMPLE_TEXTURE2D(_CoverMap,sampler_CoverMap,i.w.xz*.31).rgb;cover=lerp(dot(cover,half3(.299,.587,.114)).xxx,cover,.25)*half3(.32,.42,.21);
  albedo=lerp(albedo,cover,moss*.65);
  if(_Mode>.5){float sand=smoothstep(.50,.91,geom.y)*(1-smoothstep(-8,-4,i.w.y)*0);sand*=1-saturate(i.c.r);
   half3 grit=SAMPLE_TEXTURE2D(_SandMap,sampler_SandMap,i.w.xz*.17).rgb;grit=lerp(half3(.57,.58,.47),grit,.6)*half3(.92,.92,.80);albedo=lerp(albedo,grit,sand);
   float coverWeight=saturate(i.c.r)*smoothstep(.57,.94,geom.y);albedo=lerp(albedo,cover,coverWeight*.88);}
 }

 if(rock){
  half3 originalAlbedo = albedo;
  if(abs(_Mode-8)<.1){
   half greyStone=dot(albedo,half3(.299,.587,.114));
   albedo=lerp(greyStone.xxx,albedo,lerp(.38,.45,refinement));
   originalAlbedo=albedo;
  }
  // 将过暗的岩石底色整体托底提亮，消除死黑水平裂缝
  albedo = max(albedo, half3(.24, .25, .27));
  float strata = sin(i.w.y * 3.6 + sin(i.w.x * .45 + i.w.z * .38) * 1.5);
  float warmTop = smoothstep(.35, .88, geom.y) * smoothstep(-.1, 4.2, i.w.y);
  albedo *= lerp(half3(.92, .95, .98), half3(1.08, 1.02, .92), saturate(strata * .5 + .5));
  albedo = lerp(albedo, albedo * half3(1.12, 1.03, .88), warmTop * .35);

  float patch=.5+.24*sin(i.w.x*.83+sin(i.w.z*.61)*2)+.2*sin(i.w.z*1.37+i.w.x*.53);
  float moss=smoothstep(.58,.9,geom.y)*smoothstep(.45,.74,patch)*smoothstep(.2,1.3,i.w.y);
  albedo=lerp(albedo,albedo*half3(.56,.70,.36),moss*.48);
  if(abs(_Mode-8)<.1) originalAlbedo=lerp(originalAlbedo,originalAlbedo*half3(.55,.68,.34),moss*.52);
  albedo=lerp(originalAlbedo,albedo,refinement);
 }

 if(foliage && refinement>0){
  float crownGrad = saturate(geom.y * .55 + .45) * smoothstep(1.2, 5.8, i.w.y);
  float clusterNoise = .5 + .5 * sin(i.w.x * 1.3 + cos(i.w.z * 1.2) * 1.7 + i.w.y * .8);
  half3 deepCrimsonShadow = albedo * half3(.78, .68, .88);
  half3 sunlitAmberGold   = lerp(albedo * half3(1.22, 1.08, .76), half3(1.0, .68, .28), .32);
  half3 crownColor = lerp(deepCrimsonShadow, sunlitAmberGold, saturate(crownGrad * .65 + clusterNoise * .35));
  albedo = lerp(albedo,crownColor,refinement);
 }

 if(abs(_Mode-3)<.1){float grain=.78+.22*sin(i.w.y*8+sin(i.w.x*12)*1.2);albedo*=grain;}
 if(abs(_Mode-5)<.1){float pores=.88+.12*sin(i.w.x*28+sin(i.w.z*23))*sin(i.w.y*31);albedo*=pores;}

 float wet=1-smoothstep(-.45,.6,i.w.y);albedo*=lerp(half3(1,1,1),lerp(half3(.83,.94,.91),half3(.88,.96,.94),refinement),wet*lerp(.48,.28,refinement));
 if(abs(_Mode-1)<.1){float lm=smoothstep(.04,.50,i.w.y);half3 mineral=SAMPLE_TEXTURE2D(_LandMap,sampler_LandMap,i.w.xz*.38).rgb*half3(.78,.75,.68);albedo=lerp(albedo,mineral,lm*.90);}
 if(_ShowcaseClay>.5){albedo=half3(.52,.52,.52);n=geom;rough=.95;}

 half3 skyFloor=lerp(half3(.075,.11,.13),half3(.24,.36,.48),refinement);
 SurfaceData s=(SurfaceData)0;s.albedo=albedo;s.metallic=_Metallic;s.specular=half3(.04,.04,.04);s.smoothness=1-rough;s.normalTS=half3(0,0,1);s.occlusion=ao;s.alpha=1;s.emission=albedo*_Emission;
 InputData input=(InputData)0;input.positionWS=i.w;input.normalWS=n;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);input.shadowCoord=TransformWorldToShadowCoord(i.w);
 input.bakedGI=max(skyFloor,SampleSH(n));
 input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);input.shadowMask=half4(1,1,1,1);
 half3 c=UniversalFragmentPBR(input,s).rgb;

 Light sun=GetMainLight(input.shadowCoord);
 if(abs(_Mode-4)<.1){
  float back=saturate(dot(-n,sun.direction));
  float rimLeaf=pow(1.0-saturate(dot(n,input.viewDirectionWS)),2.4)*saturate(sun.direction.y+.3);
  c+=albedo*sun.color*lerp(.22*pow(back,.7),.40*pow(back,.62)+.28*rimLeaf,refinement);
 }
 if(depth>.12&&depth<12&&geom.y>.15){float2 cuv=i.w.xz*.11+.04*float2(sin(i.w.z*.6+_Time.y*.3),cos(i.w.x*.5-_Time.y*.23));float ca=SAMPLE_TEXTURE2D(_CausticMap,sampler_CausticMap,cuv+float2(_Time.y*.002,0)).r;c+=half3(.20,.29,.27)*ca*exp(-depth*.19)*saturate(geom.y)*sun.shadowAttenuation;}
 if(depth>0)c+=albedo*half3(.12,.17,.18)*exp(-depth*.10);
 if(depth>0)c+=albedo*half3(.07,.10,.10)*(1-sun.shadowAttenuation)*exp(-depth*.09);
 if(abs(_Mode-5)<.1&&depth>0&&_WorldSpaceCameraPos.y>-.22)c.r*=min(24,exp(depth*1.12));
 if(_WorldSpaceCameraPos.y<-.25){float f=1-exp(-distance(_WorldSpaceCameraPos,i.w)*.11);c=lerp(c,half3(.018,.10,.13),f);}

 if(refinement>0){
  float shadow=(1-sun.shadowAttenuation)*refinement;
  half3 coolSkyBounce = albedo * half3(.30, .44, .56);
  c = max(c, coolSkyBounce * (.78 * refinement));
  c += albedo * half3(.07, .15, .24) * shadow;

  // 在礁石与潮池浅石贴近水线处 (abs(i.w.y + 0.22) < 0.55m)，注入清透的浅海翡翠水光与湿润反光，彻底消除黑底圈！
  if(rock){
   float waterlineBand = (1.0 - smoothstep(.02, .58, abs(depth)))*refinement;
   c = max(c, albedo * half3(.28, .48, .54) * waterlineBand);
   c += half3(.05, .13, .16) * waterlineBand;
  }

  float facing=saturate(dot(geom,input.viewDirectionWS));
  float rim=pow(1-facing,3.2)*saturate(dot(geom,sun.direction)+.30);
  c+=albedo*sun.color*half3(1.10,.96,.76)*rim*sun.shadowAttenuation*refinement*.42;
 }

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
