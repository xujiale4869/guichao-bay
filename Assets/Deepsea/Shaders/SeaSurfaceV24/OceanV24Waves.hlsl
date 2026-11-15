// Adapted from Unity-Technologies/boat-attack-water/GerstnerWaves.hlsl
// commit 45d6e8c52a9f615fd6b83a91cdd0e9fda9ea86d3.
// Boat Attack Water copyright (c) 2018 Unity Technologies ApS. Unity Companion License.
// Full notices in Tools/SeaLifeNavigationV23/ThirdParty/BoatAttack.
#ifndef OCEAN_V24_WAVES_INCLUDED
#define OCEAN_V24_WAVES_INCLUDED
float _OceanV24Time,_OceanV24Motion;
void WaveV24(float2 p,float t,float2 direction,float amplitude,float wavelength,float phase,inout float3 d,inout float3 dx,inout float3 dz){
 direction=normalize(direction);float k=6.28318530718/wavelength;
 float frequency=sqrt(9.8*k),angle=dot(direction,p)*k-t*frequency*.78+phase;
 float s,c;
 sincos(angle,s,c);
 float q=.70;
 d+=float3(q*amplitude*direction.x*c,amplitude*s,q*amplitude*direction.y*c);
 float horizontal=-q*amplitude*k*s,vertical=amplitude*k*c;
 dx+=float3(horizontal*direction.x*direction.x,vertical*direction.x,horizontal*direction.y*direction.x);
 dz+=float3(horizontal*direction.x*direction.y,vertical*direction.y,horizontal*direction.y*direction.y);
}
float3 OceanV24Sample(float2 p,float t,out float3 dx,out float3 dz){
 float3 d=0;dx=float3(1,0,0);dz=float3(0,0,1);
 WaveV24(p,t,float2(.45,.89),.190,7.6,0,d,dx,dz);
 WaveV24(p,t,float2(.13,1),.070,4.7,.8,d,dx,dz);
 WaveV24(p,t,float2(-.8,.6),.030,2.8,2.1,d,dx,dz);return d;
}
float3 OceanV24Displacement(float2 p,float t){float3 dx,dz;return OceanV24Sample(p,t,dx,dz);}
float3 OceanV24Field(float2 world,float t){
 float2 param=world;float3 dx,dz,d;
 // This is the inverse used for shader queries. The surface vertex and CPU samples keep their wave definitions.
 [unroll]for(int j=0;j<2;j++){d=OceanV24Displacement(param,t);param=world-d.xz;}
 d=OceanV24Sample(param,t,dx,dz);float det=dx.x*dz.z-dz.x*dx.z;
 return float3(d.y,(dx.y*dz.z-dz.y*dx.z)/det,(dz.y*dx.x-dx.y*dz.x)/det);
}
#endif
