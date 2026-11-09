using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Deepsea {
 [DefaultExecutionOrder(980)]
 public sealed partial class SeaBayGameplayLoopV34:MonoBehaviour {
  public static bool Enabled {
   get {
#if DEEPSEA_V34
    return true;
#else
    return false;
#endif
   }
  }
  internal static readonly string[] Watches={"朝霞清晨","蔚蓝正午","橘红黄昏","荧光静夜"};
  internal static readonly string[] RareNames={"月光水母","黄金锦鲤","极光雀鲷"};
  internal static readonly string[] Workshops={"老船匠 · 索恩","酒馆老板娘 · 玛琳","潮汐学者 · 艾瑟尔"};
  internal static readonly string[] Houses={"南湾工坊","海风酒馆","北岬观测屋"};
  static readonly Color[] Uniforms={new Color(.38f,.52f,.26f),new Color(.76f,.39f,.27f),new Color(.38f,.47f,.76f)};
  readonly Transform[] hosts=new Transform[3];
  readonly List<UnityEngine.Object> assets=new List<UnityEngine.Object>();
  SeaLifeRuntime game;
  SeaBayExpansionAndBuildSpaceV28 bay;
  SeaSeamlessWorldAndNauticalUIV29 hud;
  SeaLifeNavigationV23 water;
  Transform root;
  internal int Dialog{get;private set;}=-1;
  internal string Feedback{get;private set;}="";
  internal bool Towing=>towing!=null;
  internal SeaGameplayProgressV34 State=>game.save.gameplayV34;
  internal int Prosperity=>12*bay.GameplayBuildingsV34.Count(b=>b.buildType==2);
  internal static int Today=>(int)(DateTime.Today.Ticks/TimeSpan.TicksPerDay);
  internal long Now=>DateTime.UtcNow.Ticks+reviewClockOffset;
  long reviewClockOffset;
  bool materialsPending;

  internal void Initialize(SeaLifeRuntime runtime,SeaBayExpansionAndBuildSpaceV28 layout,SeaSeamlessWorldAndNauticalUIV29 ui,IReadOnlyList<Transform> houses){
   game=runtime;bay=layout;hud=ui;water=game.GetComponent<SeaLifeNavigationV23>();
   root=game.Group("V34_Gameplay",game.exterior,Vector3.zero);
   if(State.startedUtc==0)Commit(s=>s.gameplayV34.startedUtc=Now);
   materialsPending=!bay.SyncGameplayV34();
   SpawnWorkshops(houses);
   InitializeWaterActivities();
   game.SetWatchLightingV34(State.watch);
   SeaSunnyHarborAudioV36.Attach(game);
  }

  // The player file commits the action first. The bay stores the receipt revision
  // with the material change, so a failed or interrupted second write can be retried.
  internal bool Commit(Action<SeaLifeSave> change,BayTransferV34 materials=null){
   if(materials!=null&&(!bay.SyncGameplayV34()||!bay.CanApplyV34(materials))){
    Report("建材不足或海湾存档暂时不可写。");return false;
   }
   var next=JsonUtility.FromJson<SeaLifeSave>(JsonUtility.ToJson(game.save));
   next.Normalize();
   change(next);
   if(materials!=null){
    materials.revision=(State.transfer?.revision??0)+1;
    next.gameplayV34.transfer=materials;
   }
   if(!game.TrySaveGameplayV34(next)){Report("保存失败，请重试。");return false;}
   if(materials!=null)materialsPending=!bay.SyncGameplayV34();
   Feedback="";
   return true;
  }
  void Report(string text){Feedback=materialsPending?text+" · 建材待同步":text;game.Notify(Feedback);}
  internal void CycleWatch(){
   int watch=(State.watch+1)%Watches.Length;
   if(!Commit(s=>s.gameplayV34.watch=watch))return;
   game.SetWatchLightingV34(watch);
   Report(Watches[watch]+(watch>=2?" · L 提灯，灯圈内可钓稀有鱼":" · T 切换时段"));
   game.Cue();
  }
  internal bool LitWater(Vector3 point){
   return State.watch>=2&&((hud.LanternOn&&HorizontalSqr(point,game.boat.position)<=64)
       ||bay.TryLitBeaconV34(point,out _));
  }
  internal bool CanStartNightFishing=>State.watch>=2&&(hud.LanternOn||bay.TryLitBeaconV34(game.boat.position,out _));
  internal bool CanCastAt(Vector3 point)=>bay.IsWaterV34(point);
  internal static float HorizontalSqr(Vector3 a,Vector3 b){
   float x=a.x-b.x,z=a.z-b.z;return x*x+z*z;
  }

  void SpawnWorkshops(IReadOnlyList<Transform> houses){
   var prefab=Resources.Load<GameObject>("CoastHero/Fisherman");
   for(int i=0;i<Mathf.Min(hosts.Length,houses.Count);i++){
    var house=houses[i];
    var front=game.Grounded(house.TransformPoint(new Vector3(i==1?-1.9f:0,0,i==1?5.25f:4.45f)));
    var host=game.Group("V34 "+Workshops[i],root,front);
    host.rotation=house.rotation;
    hosts[i]=host;
    var model=Instantiate(prefab,host,false);
    model.transform.localScale=Vector3.one*1.18f;
    var rig=model.GetComponent<CoastHeroRig>();if(rig!=null)rig.seated=false;
    foreach(var collider in model.GetComponentsInChildren<Collider>())collider.enabled=false;
    var uniform=new MaterialPropertyBlock();
    uniform.SetColor("_BaseColor",Uniforms[i]);
    uniform.SetColor("_Color",Uniforms[i]);
    foreach(var part in model.GetComponentsInChildren<Renderer>())
     if(part.name=="Hips"||part.name=="LeftArm"||part.name=="RightArm")part.SetPropertyBlock(uniform);
    game.Primitive("工坊标记",PrimitiveType.Sphere,host,new Vector3(0,2.5f,0),new Vector3(.19f,.27f,.19f),
        MakeMaterial("Workshop "+i,Uniforms[i],1.5f));
   }
  }
  int NearbyWorkshop(){
   for(int i=0;i<hosts.Length;i++)
    if(hosts[i]!=null&&Vector3.Distance(game.walker.position,hosts[i].position)<=3.1f)return i;
   return -1;
  }
  internal bool TryOpenWorkshop(){
   int index=NearbyWorkshop();
   if(index<0)return false;
   Dialog=index;Feedback="";
   hud.ShowWorkshopV34();game.Cue();return true;
  }
  internal void CloseWorkshop(){
   if(Dialog<0)return;
   Dialog=-1;Feedback="";hud.ShowWorkshopV34();
  }
  internal string Hint(){
   if(game.InRoom)return "";
   if(game.OnFoot){
    int index=NearbyWorkshop();
    return index>=0?"E  "+Workshops[index]:"";
   }
   if(Towing)return "挂钩收缆中 · Esc 取消";
   var target=FindFloat();
   var pot=FindPot();
   if(target!=null&&PreferFloat(target,pot))return "E  打捞 "+SalvageNames[target.kind];
   if(pot!=null)return "E  收取捕蟹笼 · 肥蟹 / 金币 / 浮木";
   if(game.SelectedTool==0&&CanStartNightFishing)return "E  准备灯下夜钓 · L 提灯 · T 时段";
   return "";
  }
  internal void RunWorkshopAction(int index){
   if(Dialog<0||!game.OnFoot||NearbyWorkshop()!=Dialog)return;
   if(index<0||index>=2)return;
   if(Dialog==0){if(index==0)UpgradeMotor();else UpgradeHold();}
   if(Dialog==1){if(index==0)Sell();else Bounty();}
   if(Dialog==2){if(index==0)Breed();else Eco();}
   hud.ShowWorkshopV34();
  }
  internal string ActionLabel(int index){
   if(Dialog==0&&index==0)return State.motor>=2?"马达已满级 · 极速 +50%":
       "改装马达  +"+(25*(State.motor+1))+"%   ·   浮木 "+MotorWood+" / 黄铜 "+MotorBrass;
   if(Dialog==0)return State.hold>=2?"活鱼舱已满级 · 18 条":
       "扩建活鱼舱  "+(12+6*State.hold)+" 条   ·   浮木 "+HoldWood+" / 金币 "+HoldCoins;
   if(Dialog==1&&index==0)return "出售随身鱼与肥蟹   ·   +35% 溢价";
   if(Dialog==1)return State.bountyDay>=Today?"今日悬赏已交付":"交付悬赏   ·   3 条鱼（含稀有鱼）/ 1 只肥蟹";
   if(index==0)return "培育极光珊瑚苗 ×3   ·   潮汐石 4 / 金币 30";
   return State.ecoDay>=Today?"今日保护基金已领取":"领取保护基金   ·   金币 +"+Prosperity;
  }
  internal bool ActionEnabled(int index){
   if(Dialog==0)return index==0?State.motor<2:State.hold<2;
   if(Dialog==1)return index==0?game.CarryCount>0||State.crabs>0:State.bountyDay<Today;
   return index==0||State.ecoDay<Today&&Prosperity>0;
  }
  int MotorWood=>6+4*State.motor;
  int MotorBrass=>2+2*State.motor;
  int HoldWood=>8+4*State.hold;
  int HoldCoins=>80+60*State.hold;
  void UpgradeMotor(){
   if(State.motor>=2)return;
   if(Commit(s=>s.gameplayV34.motor++,new BayTransferV34{wood=-MotorWood,brass=-MotorBrass}))
    Report("马达改装完成 · 极速 +"+25*State.motor+"% · 挂钩范围扩大");
  }
  void UpgradeHold(){
   if(State.hold>=2)return;
   int cost=HoldCoins;
   if(game.save.coins<cost){Report("金币不足，需要 "+cost+"。");return;}
   if(Commit(s=>{s.coins-=cost;s.gameplayV34.hold++;},new BayTransferV34{wood=-HoldWood}))
    Report("活鱼舱扩建完成 · "+game.MaxCarryCount+" 条");
  }
  void Sell(){
   int count=game.CarryCount+State.crabs;
   if(count==0)return;
   int value=game.save.fish.Where(f=>f.tank<0).Sum(SeaLifeRuntime.SaleValueV34)+12*State.crabs;
   int coins=Mathf.RoundToInt(value*1.35f);
   if(Commit(s=>{s.fish.RemoveAll(f=>f.tank<0);s.gameplayV34.crabs=0;s.coins+=coins;}))
    Report("已收购 "+count+" 份鱼蟹 · 金币 +"+coins);
  }
  void Bounty(){
   if(State.bountyDay>=Today){Report("今天的悬赏已经交付。");return;}
   var fish=game.save.fish.Where(f=>f.tank<0).OrderByDescending(f=>f.variantV34>0).Take(3).ToArray();
   if(fish.Length<3||fish[0].variantV34==0||State.crabs<1){Report("需要 3 条随身鱼，其中至少 1 条稀有鱼，再加 1 只肥蟹。");return;}
   var ids=new HashSet<string>(fish.Select(f=>f.id));
   int day=Today;
   if(Commit(s=>{s.fish.RemoveAll(f=>ids.Contains(f.id));s.gameplayV34.crabs--;s.gameplayV34.bountyDay=day;s.coins+=120;},
       new BayTransferV34{brass=3})){
    SeaSunnyHarborAudioV36.PlayHarvest(game,SeaSunnyHarborAudioV36.Harvest.Bounty);
    Report("悬赏已交付 · 金币 +120 · 黄铜 +3");
   }
  }
  void Breed(){
   if(game.save.coins<30){Report("金币不足，需要 30。");return;}
   if(Commit(s=>s.coins-=30,new BayTransferV34{stone=-4,seeds=3}))Report("育种完成 · 极光珊瑚苗 +3");
  }
  void Eco(){
   int bonus=Prosperity,day=Today;
   if(State.ecoDay>=day||bonus==0)return;
   if(Commit(s=>{s.coins+=bonus;s.gameplayV34.ecoDay=day;}))Report("保护基金已入账 · 金币 +"+bonus);
  }
  Material MakeMaterial(string name,Color color,float emission=0){
   var material=game.MakeGameplayMaterialV34(name,color,emission);
   assets.Add(material);return material;
  }
  void OnDestroy(){foreach(var asset in assets)if(asset!=null)Destroy(asset);}
 }
}
