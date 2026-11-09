using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deepsea {
 public sealed partial class SeaBayGameplayLoopV34 {
  internal static readonly string[] SalvageNames={"海盐浮木捆","沉船黄铜零件箱","荧光海晶瓶","潮汐矿石气囊包"};
  static readonly Vector2[] Lanes={
   new Vector2(8.5f,12),new Vector2(18.2f,-8.5f),new Vector2(26.5f,16),new Vector2(14,-22),
   new Vector2(32,4.5f),new Vector2(22,32),new Vector2(33,-24),new Vector2(42,29)
  };
  static readonly Color[] GlowColors={new Color(.42f,.78f,1),new Color(1,.64f,.19f),new Color(.38f,1,.73f)};
  sealed class Flotsam {
   public int slot,kind;
   public Transform model;
   public Vector3 origin,position,current;
  }
  readonly Flotsam[] floats=new Flotsam[Lanes.Length];
  readonly Dictionary<BayPlacedBuildingEntry,Transform> readyMarkers=new Dictionary<BayPlacedBuildingEntry,Transform>();
  readonly List<BayPlacedBuildingEntry> expiredMarkers=new List<BayPlacedBuildingEntry>();
  readonly Transform[] schools=new Transform[2];
  Flotsam towing;
  Transform grab;
  LineRenderer tow;
  ParticleSystem splash;
  Material wetWood,binding,brass,crystal,canvas,goldMarker;
  float nextFloat,nextDrift,nextIndicators,towTime;
  internal float HookRange=>4.5f+.75f*State.motor;
  const long PotPeriod=45*TimeSpan.TicksPerSecond;

  void InitializeWaterActivities(){
   wetWood=MakeMaterial("Salt oak",new Color(.43f,.26f,.14f));
   binding=MakeMaterial("Salt rope",new Color(.78f,.60f,.36f));
   brass=MakeMaterial("Salvage brass",new Color(.73f,.44f,.15f));
   crystal=MakeMaterial("Sea glass",new Color(.22f,.85f,.62f),.8f);
   canvas=MakeMaterial("Tarred canvas",new Color(.29f,.39f,.53f));
   goldMarker=MakeMaterial("Crab pot marker",new Color(1,.74f,.21f),2.2f);
   grab=game.Primitive("挂钩尖",PrimitiveType.Capsule,root,Vector3.zero,new Vector3(.14f,.31f,.13f),brass);
   grab.gameObject.SetActive(false);
   tow=game.Line("打捞缆绳",root,binding,.028f);
   tow.positionCount=12;tow.enabled=false;
   splash=game.Group("抛钩水花",root,Vector3.zero).gameObject.AddComponent<ParticleSystem>();
   var main=splash.main;main.playOnAwake=false;main.loop=false;main.startLifetime=.42f;main.startSpeed=new ParticleSystem.MinMaxCurve(.7f,1.8f);
   main.startSize=new ParticleSystem.MinMaxCurve(.027f,.067f);main.gravityModifier=.65f;
   var emission=splash.emission;emission.enabled=false;
   var shape=splash.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=43;shape.radius=.12f;
   splash.transform.rotation=Quaternion.Euler(-90,0,0);
   splash.GetComponent<ParticleSystemRenderer>().sharedMaterial=MakeMaterial("Water spray",new Color(.64f,.88f,.91f));
   splash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   for(int i=0;i<6;i++)TrySpawnFloat(i);
   nextFloat=Time.time+24;
   for(int i=0;i<schools.Length;i++){
    var school=game.Group("趋光稀有鱼群 "+i,root,Vector3.zero);
    for(int j=0;j<9;j++){
     var f=game.CreateRareModelV34(school,1+j%3,.92f);
     var motion=f.gameObject.AddComponent<SeaLifeFishMotion>();
     motion.center=new Vector3(Mathf.Cos(j*2.4f)*1.9f,-.30f-j%3*.12f,Mathf.Sin(j*2.4f)*1.9f);
     motion.extent=new Vector3(.7f,.1f,.8f);motion.phase=j*1.8f;motion.speed=.8f+j*.05f;
    }
    school.gameObject.SetActive(false);schools[i]=school;
   }
  }
  bool TrySpawnFloat(int slot){
   if(floats[slot]!=null||State.floatRespawn[slot]>Now)return false;
   Vector2 lane=Lanes[slot];
   Vector3 position=new Vector3(lane.x,SeaLifeRuntime.Sea,lane.y);
   for(int i=0;i<12;i++){
    var p=position+new Vector3(Mathf.Cos(i*2.4f)*i*.48f,0,Mathf.Sin(i*2.4f)*i*.48f);
    if(!bay.IsWaterV34(p)||HorizontalSqr(p,game.boat.position)<9)continue;
    int kind=slot%4;
    var model=game.Group("漂流物 "+SalvageNames[kind],root,p);
    BuildFlotsam(model,kind);
    floats[slot]=new Flotsam{slot=slot,kind=kind,model=model,origin=p,position=p,
        current=new Vector3(.20f,0,Mathf.Sin(slot*1.8f)*.17f)};
    return true;
   }
   return false;
  }
  void BuildFlotsam(Transform model,int kind){
   switch(kind){
    case 0:
     for(int i=-1;i<=1;i++){
      var log=game.Primitive("海盐木",PrimitiveType.Cylinder,model,new Vector3(.15f*i,.12f,0),new Vector3(.17f,.61f+i*.09f,.18f),wetWood);
      log.localRotation=Quaternion.Euler(90,0,0);
     }
     for(int i=-1;i<=1;i+=2)game.Box("木捆绑绳",model,new Vector3(0,.12f,i*.27f),new Vector3(.47f,.20f,.08f),binding);
     break;
    case 1:
     game.Box("机件箱",model,new Vector3(0,.15f,0),new Vector3(.68f,.42f,.53f),wetWood);
     for(int i=-1;i<=1;i+=2)game.Box("黄铜护边",model,new Vector3(i*.25f,.15f,0),new Vector3(.07f,.46f,.57f),brass);
     game.Box("黄铜提手",model,new Vector3(0,.41f,0),new Vector3(.17f,.12f,.07f),brass);
     break;
    case 2:
     game.Primitive("玻璃瓶身",PrimitiveType.Sphere,model,new Vector3(0,.20f,0),new Vector3(.48f,.56f,.47f),crystal);
     game.Primitive("瓶颈",PrimitiveType.Cylinder,model,new Vector3(0,.48f,0),new Vector3(.17f,.12f,.17f),crystal);
     game.Primitive("木瓶塞",PrimitiveType.Cylinder,model,new Vector3(0,.59f,0),new Vector3(.20f,.055f,.20f),wetWood);
     break;
    case 3:
     game.Primitive("气囊",PrimitiveType.Sphere,model,new Vector3(0,.17f,0),new Vector3(.69f,.46f,.55f),canvas);
     game.Primitive("束口",PrimitiveType.Capsule,model,new Vector3(0,.37f,0),new Vector3(.19f,.12f,.16f),binding);
     break;
   }
   var marker=game.Primitive("可打捞标记",PrimitiveType.Sphere,model,new Vector3(0,.78f,0),new Vector3(.11f,.13f,.11f),kind==2?crystal:goldMarker);
   marker.localRotation=Quaternion.Euler(0,0,45);
  }
  internal void Tick(){
   if(game.Paused||game.InRoom||bay.godBuildMode)return;
   float time=Time.time;
   if(time>=nextFloat){
    nextFloat=time+UnityEngine.Random.Range(24f,42f);
    int start=UnityEngine.Random.Range(0,floats.Length);
    for(int i=0;i<floats.Length;i++){
     int slot=(i+start)%floats.Length;
     if(TrySpawnFloat(slot))break;
    }
   }
   if(time>=nextDrift){
    nextDrift=time+.45f;
    foreach(var f in floats){
     if(f==null||f==towing)continue;
     var proposed=f.position+f.current*.45f;
     if(HorizontalSqr(f.origin,proposed)>25||!bay.IsWaterV34(proposed))f.current=-f.current;
     else f.position=proposed;
    }
   }
   foreach(var f in floats){
    if(f==null||f==towing)continue;
    var p=f.model.position;p.y=f.position.y;
    p=Vector3.MoveTowards(p,f.position,f.current.magnitude*Time.deltaTime);
    p.y=WaterHeight(p)+.04f+Mathf.Sin(time*2+f.slot)*.06f;
    f.model.SetPositionAndRotation(p,Quaternion.Euler(Mathf.Sin(time*1.3f+f.slot)*9,22*f.slot+time*4,Mathf.Cos(time*1.8f+f.slot)*8));
   }
   if(time>=nextIndicators){
    nextIndicators=time+.4f;
    if(materialsPending)materialsPending=!bay.SyncGameplayV34();
    RefreshPotMarkers();
   }
   foreach(var kv in readyMarkers){
    var marker=kv.Value;
    if(marker!=null)marker.localPosition=Vector3.up*(.53f+Mathf.Sin(time*4+kv.Key.gridX)*.14f);
   }
   UpdateSchools();
   if(towing!=null)UpdateTow();
  }
  float WaterHeight(Vector3 p)=>water!=null?water.Height(p):SeaLifeRuntime.Sea;
  void UpdateSchools(){
   bool evening=State.watch>=2;
   var p=game.boat.position;
   p.y=WaterHeight(p);
   schools[0].gameObject.SetActive(evening&&hud.LanternOn);
   schools[0].position=p-game.boat.forward*2.15f;
   Vector3 beacon=Vector3.zero;
   bool near=evening&&bay.TryLitBeaconV34(p,out beacon);
   schools[1].gameObject.SetActive(near);
   if(near){
    var d=p-beacon;d.y=0;
    schools[1].position=beacon+d.normalized*1.6f;
   }
  }
  Flotsam FindFloat(){
   if(game.SelectedTool!=1)return null;
   Flotsam best=null;
   float max=HookRange*HookRange;
   foreach(var f in floats){
    if(f==null)continue;
    float sq=HorizontalSqr(game.boat.position,f.model.position);
    if(sq<=max){max=sq;best=f;}
   }
   return best;
  }
  BayPlacedBuildingEntry FindPot(){
   BayPlacedBuildingEntry best=null;
   float limit=4.5f*4.5f;
   long now=Now;
   foreach(var pot in bay.GameplayBuildingsV34){
    if(pot.buildType!=3)continue;
    float dist=HorizontalSqr(game.boat.position,PotPosition(pot));
    if(dist<=limit&&PotReady(pot,now)){best=pot;limit=dist;}
   }
   return best;
  }
  static Vector3 PotPosition(BayPlacedBuildingEntry pot)=>SeaBayExpansionAndBuildSpaceV28.CellPosition(new Vector2Int(pot.gridX,pot.gridZ));
  static bool SamePot(PotHarvestV34 record,BayPlacedBuildingEntry pot)=>record.x==pot.gridX&&record.z==pot.gridZ&&record.placedUtc==pot.placedUtcV34;
  bool PotReady(BayPlacedBuildingEntry pot,long now){
   var history=State.pots.Find(p=>SamePot(p,pot));
   long last=history?.harvestedUtc??(pot.placedUtcV34>0?pot.placedUtcV34:State.startedUtc);
   return last>0&&now-last>=PotPeriod;
  }
  void RefreshPotMarkers(){
   long now=Now;
   expiredMarkers.Clear();
   foreach(var item in readyMarkers)
    if(item.Value==null||!PotReady(item.Key,now))expiredMarkers.Add(item.Key);
   foreach(var pot in expiredMarkers){
    if(readyMarkers[pot]!=null)Destroy(readyMarkers[pot].gameObject);
    readyMarkers.Remove(pot);
   }
   foreach(var pot in bay.GameplayBuildingsV34){
    if(pot.buildType!=3||!PotReady(pot,now)||readyMarkers.ContainsKey(pot))continue;
    var buoy=bay.PotFloatV34(pot);
    if(buoy!=null)readyMarkers.Add(pot,game.Primitive("可收获 · 捕蟹笼",PrimitiveType.Sphere,buoy,Vector3.up*.6f,new Vector3(.30f,.30f,.30f),goldMarker));
   }
  }
  bool PreferFloat(Flotsam target,BayPlacedBuildingEntry pot)=>pot==null||
      HorizontalSqr(game.boat.position,target.model.position)<HorizontalSqr(game.boat.position,PotPosition(pot));
  internal bool TrySeaInteraction(){
   var f=FindFloat();
   var pot=FindPot();
   if(f!=null&&PreferFloat(f,pot)){BeginTow(f);return true;}
   if(pot==null)return false;
   long now=Now;
   if(Commit(s=>{
    var state=s.gameplayV34;
    state.pots.RemoveAll(p=>p.x==pot.gridX&&p.z==pot.gridZ);
    state.pots.Add(new PotHarvestV34{x=pot.gridX,z=pot.gridZ,placedUtc=pot.placedUtcV34,harvestedUtc=now});
    state.crabs++;state.bait++;s.coins+=18;
   },new BayTransferV34{wood=1})){
    SeaSunnyHarborAudioV36.PlayHarvest(game,SeaSunnyHarborAudioV36.Harvest.CrabPot);
    RefreshPotMarkers();game.Cue(true);Report("起笼 · 肥蟹 +1 · 蟹饵 +1 · 金币 +18 · 浮木 +1");
   }
   return true;
  }
  void BeginTow(Flotsam f){
   towing=f;towTime=0;
   game.body.velocity=Vector3.zero;game.body.angularVelocity=Vector3.zero;
   grab.gameObject.SetActive(true);tow.enabled=true;game.Cue();
   Report("挂钩抛向 "+SalvageNames[f.kind]);
  }
  void UpdateTow(){
   towTime+=Time.deltaTime;
   var hand=game.boat.position+game.boat.right*.65f+Vector3.up*.94f;
   var point=towing.model.position;
   if(towTime<.62f){
    float t=towTime/.62f;
    grab.position=Vector3.Lerp(hand,point,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*1.4f;
   }else{
    if(towTime-Time.deltaTime<.62f){splash.transform.position=point;splash.Emit(18);game.Cue(true);}
    float t=Mathf.Clamp01((towTime-.62f)/1.7f);
    towing.model.position=Vector3.Lerp(towing.position,hand,Mathf.SmoothStep(0,1,t));
    grab.position=towing.model.position;
   }
   for(int i=0;i<tow.positionCount;i++){
    float t=i/(float)(tow.positionCount-1);
    tow.SetPosition(i,Vector3.Lerp(hand,grab.position,t)-Vector3.up*Mathf.Sin(t*Mathf.PI)*.22f);
   }
   if(towTime>=2.32f)FinishTow();
  }
  void FinishTow(){
   var f=towing;
   var reward=new BayTransferV34();
   int coins=0,bait=0;
   string goods;
   switch(f.kind){
    case 0:reward.wood=UnityEngine.Random.Range(2,4);coins=10;goods="浮木 +"+reward.wood;break;
    case 1:reward.brass=UnityEngine.Random.Range(1,3);coins=20;goods="黄铜 +"+reward.brass;break;
    case 2:reward.seeds=UnityEngine.Random.Range(1,3);coins=15;bait=1;goods="珊瑚苗 +"+reward.seeds+" · 稀有鱼饵 +1";break;
    default:reward.stone=UnityEngine.Random.Range(2,4);coins=12;goods="潮汐石 +"+reward.stone;break;
   }
   long respawn=Now+TimeSpan.TicksPerSecond*UnityEngine.Random.Range(36,61);
   if(Commit(s=>{s.coins+=coins;s.gameplayV34.bait+=bait;s.gameplayV34.floatRespawn[f.slot]=respawn;},reward)){
    floats[f.slot]=null;Destroy(f.model.gameObject);game.Cue(true);
    SeaSunnyHarborAudioV36.PlayHarvest(game,SeaSunnyHarborAudioV36.Harvest.Salvage);
    Report(SalvageNames[f.kind]+" · "+goods+" · 金币 +"+coins);
   }else f.model.position=f.position;
   towing=null;grab.gameObject.SetActive(false);tow.enabled=false;
  }
  internal void CancelTow(){
   if(towing==null)return;
   towing.model.position=towing.position;towing=null;grab.gameObject.SetActive(false);tow.enabled=false;
  }
 }
}
