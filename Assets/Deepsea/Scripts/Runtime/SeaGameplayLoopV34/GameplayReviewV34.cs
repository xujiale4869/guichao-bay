using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Deepsea {
 public sealed partial class SeaBayGameplayLoopV34 {
  internal Transform ReviewHost(int i)=>hosts[i];
  internal void AdvanceReviewClock(int seconds){reviewClockOffset+=seconds*System.TimeSpan.TicksPerSecond;}
  internal bool ReviewPotReady(BayPlacedBuildingEntry p)=>PotReady(p,Now);
  internal bool ReviewPotMarker(BayPlacedBuildingEntry p)=>readyMarkers.ContainsKey(p);
  internal Vector3 ReviewFloat(int slot)=>floats[slot]!=null?floats[slot].model.position:Vector3.zero;
  internal bool ReviewSchool(int i)=>schools[i].gameObject.activeSelf;
  internal bool StageReviewCargoAtPot(BayPlacedBuildingEntry pot){
   var f=floats[0];
   if(f==null||pot==null)return false;
   var anchor=PotPosition(pot);
   for(int i=0;i<12;i++){
    float a=i*Mathf.PI/6;
    var direction=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
    var p=anchor+direction*6.1f;
    if(!CanCastAt(p)||!CanCastAt(anchor+direction*3.2f))continue;
    f.origin=f.position=p;
    f.model.position=p;
    return true;
   }
   return false;
  }
 }

 public sealed partial class SeaSeamlessWorldAndNauticalUIV29 {
  IEnumerator ReviewGameplayV34(){
   var g=game.GameplayV34;
   Check(g!=null&&watchLabelV34!=null,"v34 active in the released village scene");
   if(g==null)yield break;
   Check(g.State.watch==1,"v34 first start preserves noon lighting");
   Check(game.MaxCarryCount==6,"v34 base fish hold retains six slots");
   Check(Enumerable.Range(0,4).All(i=>g.ReviewFloat(i)!=Vector3.zero&&g.CanCastAt(g.ReviewFloat(i))),"v34 four floating cargo types spawn on actual water");
   Check(Enumerable.Range(0,3).All(i=>g.ReviewHost(i)!=null&&g.ReviewHost(i).position.y>SeaLifeRuntime.Sea+.22f),"v34 three hosts stand on land at the actual compounds");

   var woodFloat=g.ReviewFloat(0);
   var pot=PlaceReviewStructureV34(3,woodFloat);
   var potCenter=pot!=null?SeaBayExpansionAndBuildSpaceV28.CellPosition(new Vector2Int(pot.gridX,pot.gridZ)):woodFloat;
   var beacon=PlaceReviewStructureV34(1,potCenter);
   var garden=PlaceReviewStructureV34(2,potCenter);
   Check(pot!=null&&beacon!=null&&garden!=null,"v34 production uses actual placed pots, beacons and coral gardens");
   if(pot!=null)Check(!g.ReviewPotReady(pot),"v34 a new pot starts with an empty soak timer");
   g.CycleWatch();
   Check(g.State.watch==2,"v34 time advances from noon to dusk");
   g.CycleWatch();
   Check(g.State.watch==3,"v34 time advances from dusk to night");
   game.PlaceBoat(new Vector3(78,SeaLifeRuntime.Sea,24),90);
   LanternOn=false;
   Check(!g.LitWater(game.boat.position),"v34 unlit offshore water has no phototaxis");
   LanternOn=true;
   Check(g.LitWater(game.boat.position+Vector3.right*7.99f)&&!g.LitWater(game.boat.position+Vector3.right*8.01f),
       "v34 boat lantern uses an eight metre horizontal radius");
   if(beacon!=null){
    var center=SeaBayExpansionAndBuildSpaceV28.CellPosition(new Vector2Int(beacon.gridX,beacon.gridZ));
    LanternOn=false;game.PlaceBoat(center+Vector3.right*2,90);
    yield return null;
    Check(g.LitWater(center)&&g.ReviewSchool(1)&&!g.ReviewSchool(0),"v34 built beacon attracts a school with boat lantern off");
   }

   game.PlaceBoat(new Vector3(20,SeaLifeRuntime.Sea,12),90);LanternOn=true;
   g.Commit(s=>{s.fish.RemoveAll(f=>f.tank<0);s.gameplayV34.bait=3;});
   game.SelectTool(0);
   game.Interact();
   Check(game.fishingStage==1&&game.Cast(game.boat.position+Vector3.right*4),"v34 lit water enables a real cast");
   yield return new WaitForSeconds(1.9f);
   Check(game.fishingStage==3&&game.PendingFishName=="月光水母"&&g.ReviewSchool(0),"v34 lit bite after .8 second soak with rare jellyfish");
   if(game.fishingStage==3){
    game.Interact();
    float until=Time.time+15;
    while(game.fishingStage==4&&Time.time<until){game.AutoReel=game.tension<48;yield return null;}
    Check(game.fishingStage==5,"v34 real reeling can land a rare fish");
    if(game.fishingStage==5){
     yield return Capture("v34-night-catch");
     Check(game.KeepCatch()&&game.save.fish.Any(f=>f.tank<0&&f.variantV34==1)&&g.State.bait==2,
         "v34 retained rare catch and bait debit persisted together");
    }
   }
   game.CancelActivity();
   g.AdvanceReviewClock(46);
   int coins=game.save.coins,wood=bay.DataV29.wood;
   game.SelectTool(1);
   Check(g.StageReviewCargoAtPot(pot),"v34 proof uses navigable water between cargo and an actual pot");
   var cargo=g.ReviewFloat(0);
   if(cargo!=Vector3.zero){
    Vector3 towardPot=pot==null?Vector3.left:SeaBayExpansionAndBuildSpaceV28.CellPosition(new Vector2Int(pot.gridX,pot.gridZ))-cargo;
    towardPot.y=0;
    Vector3 boatPos=cargo+towardPot.normalized*Mathf.Min(3.8f,towardPot.magnitude*.48f);
    game.PlaceBoat(boatPos,Quaternion.LookRotation(cargo-boatPos).eulerAngles.y);
    game.Interact();Check(g.Towing,"v34 E starts a real flotsam tow");
    game.CancelActivity();Check(!g.Towing&&game.save.coins==coins&&bay.DataV29.wood==wood,"v34 Esc cancellation restores the drifting cargo");
    game.Interact();
    customSailZoom=22;game.SnapWaterCamera();
    yield return new WaitForSeconds(.8f);
    Check(pot!=null&&g.ReviewPotMarker(pot),"v34 full bay preview has a real ready crab pot during salvage");
    yield return Capture("v34-preview");
    customSailZoom=12;game.SnapWaterCamera();
    yield return Capture("v34-hook-detail");
    yield return new WaitForSeconds(1.65f);
    Check(!g.Towing&&game.save.coins==coins+10&&bay.DataV29.wood>=wood+2&&bay.DataV29.wood<=wood+3,"v34 flotsam credits bounded coin and material rewards");
    int woodAfter=bay.DataV29.wood;
    Check(bay.SyncGameplayV34()&&bay.SyncGameplayV34()&&bay.DataV29.wood==woodAfter&&
          game.store.Load().gameplayV34.floatRespawn[0]>g.Now,"v34 material replay is idempotent and cargo cooldown survives reload");
   }

   if(pot!=null){
    var cell=new Vector2Int(pot.gridX,pot.gridZ);
    var p=SeaBayExpansionAndBuildSpaceV28.CellPosition(cell);
    game.PlaceBoat(p+Vector3.right*3.2f,270);game.SelectTool(0);
    yield return new WaitForSeconds(.55f);
    Check(g.ReviewPotReady(pot)&&g.ReviewPotMarker(pot),"v34 built pot produces a visible gold indicator after 45 seconds");
    customSailZoom=22;game.SnapWaterCamera();game.NoticeTime=0;
    yield return Capture("v34-pot-harvest");
    int crabs=g.State.crabs;coins=game.save.coins;wood=bay.DataV29.wood;
    game.Interact();
    Check(g.State.crabs==crabs+1&&game.save.coins==coins+18&&bay.DataV29.wood==wood+1,"v34 E harvests crab, coins and driftwood");
    game.Interact();game.CancelActivity();
    Check(g.State.crabs==crabs+1&&!g.ReviewPotReady(pot)&&game.store.Load().gameplayV34.pots.Count>0,"v34 pot cooldown prevents duplicate harvest and persists");
   }
   g.CycleWatch();Check(g.State.watch==0,"v34 night wraps to dawn");
   g.CycleWatch();Check(g.State.watch==1,"v34 dawn restores v33 noon");
   float baseline=0;
   game.DriveInput=Vector2.up;game.PlaceBoat(new Vector3(40,SeaLifeRuntime.Sea,-62),0);
   yield return new WaitForSeconds(8);
   baseline=Vector3.Dot(game.boat.forward,game.body.velocity);
   game.DriveInput=Vector2.zero;

   game.PlaceBoat(game.dockBerth,90);game.ToggleBoard();
   var host=g.ReviewHost(0);
   if(host!=null){
    game.PlaceWalker(host.position+host.forward*.95f);game.Interact();
    Check(g.Dialog==0&&game.UiBlocking,"v34 E opens boatwright modal and blocks movement");
    if(g.Dialog==0){
     yield return new WaitForSeconds(.6f);
     Check(VisibleLabels(workshopModalV34),"v34 workshop uses visible Chinese labels");
     var motor=WorkshopButtonV34(0);
     wood=bay.DataV29.wood;int brass=bay.DataV29.brass;
     motor.onClick.Invoke();
     Check(g.State.motor==1&&bay.DataV29.wood==wood-6&&bay.DataV29.brass==brass-2,"v34 boatwright button persists level one and exact materials");
     g.Commit(s=>s.coins+=250,new BayTransferV34{wood=24,brass=8});
     WorkshopButtonV34(1).onClick.Invoke();
     Check(game.MaxCarryCount==12&&game.store.Load().gameplayV34.hold==1,"v34 hold upgrade expands actual carry cap and persists");
     yield return Capture("v34-boatwright");
     WorkshopButtonV34(0).onClick.Invoke();
     Check(g.State.motor==2&&!WorkshopButtonV34(0).interactable,"v34 second motor upgrade caps at +50 percent");
     WorkshopButtonV34(1).onClick.Invoke();
     Check(game.MaxCarryCount==18&&!WorkshopButtonV34(1).interactable,"v34 second hold upgrade caps at eighteen");
     g.CloseWorkshop();
    }
   }
   host=g.ReviewHost(1);
   if(host!=null){
    game.PlaceWalker(host.position+host.forward*.95f);game.Interact();
    Check(g.Dialog==1,"v34 E opens tavern at the actual tavern compound");
    if(g.Dialog==1){
     g.Commit(s=>{
      s.fish.RemoveAll(f=>f.tank<0);
      for(int i=0;i<4;i++)s.fish.Add(new SeaLifeFish{id="v34-order-"+i,species=i%3,variantV34=i==0?2:0});
      s.fish.Add(new SeaLifeFish{id="v34-display",species=1,tank=0});
      s.gameplayV34.crabs=2;
     });
     coins=game.save.coins;int brass=bay.DataV29.brass;
     WorkshopButtonV34(1).onClick.Invoke();
     Check(game.save.coins==coins+120&&bay.DataV29.brass==brass+3&&game.CarryCount==1&&g.State.crabs==1,
         "v34 bounty consumes fish and crab and pays the documented order");
     g.RunWorkshopAction(1);
     Check(game.save.coins==coins+120&&bay.DataV29.brass==brass+3,"v34 repeated bounty click is rejected");
     coins=game.save.coins;
     WorkshopButtonV34(0).onClick.Invoke();
     Check(game.save.coins==coins+27&&game.CarryCount==0&&g.State.crabs==0&&game.save.fish.Any(f=>f.id=="v34-display"),
         "v34 tavern premium applies to carried fish and crabs and protects displayed fish");
     yield return new WaitForSeconds(.6f);
     yield return Capture("v34-tavern");
     g.CloseWorkshop();
    }
   }
   host=g.ReviewHost(2);
   if(host!=null){
    game.PlaceWalker(host.position+host.forward*.95f);game.Interact();
    Check(g.Dialog==2,"v34 E opens scholar at the actual observatory compound");
    if(g.Dialog==2){
     g.Commit(s=>s.coins+=45,new BayTransferV34{stone=5});
     int stone=bay.DataV29.stone,seeds=bay.DataV29.coralSeed;
     WorkshopButtonV34(0).onClick.Invoke();
     Check(bay.DataV29.stone==stone-4&&bay.DataV29.coralSeed==seeds+3,"v34 scholar exchanges real building resources");
     coins=game.save.coins;int prosperity=g.Prosperity;
     WorkshopButtonV34(1).onClick.Invoke();g.RunWorkshopAction(1);
     Check(prosperity>0&&game.save.coins==coins+prosperity&&game.store.Load().gameplayV34.ecoDay==SeaBayGameplayLoopV34.Today,
         "v34 coral prosperity pays only once and saves the receipt day");
     yield return new WaitForSeconds(.6f);
     yield return Capture("v34-scholar");
     g.CloseWorkshop();
    }
   }
   game.PlaceWalker(game.Grounded(game.dockFeet));game.ToggleBoard();
   game.DriveInput=Vector2.up;game.PlaceBoat(new Vector3(40,SeaLifeRuntime.Sea,-62),0);
   yield return new WaitForSeconds(8);
   float upgraded=Vector3.Dot(game.boat.forward,game.body.velocity);
   game.DriveInput=Vector2.zero;
   Check(baseline>2&&upgraded/baseline>1.40f&&upgraded/baseline<1.60f,"v34 actual full-throttle speed increases 50 percent ("+baseline.ToString("F2")+" to "+upgraded.ToString("F2")+")");
   Check(game.store.Load().gameplayV34.motor==2&&game.store.Load().gameplayV34.hold==2,"v34 upgraded player profile round-trips");
  }
  BayPlacedBuildingEntry PlaceReviewStructureV34(int type,Vector3 near){
   var candidates=new List<Vector2Int>();
   var center=new Vector2Int(Mathf.RoundToInt((near.x+1)/2),Mathf.RoundToInt((near.z-2.4f)/2));
   for(int z=-15;z<=15;z++)
    for(int x=-15;x<=15;x++)candidates.Add(center+new Vector2Int(x,z));
   foreach(var cell in candidates.OrderBy(c=>SeaBayGameplayLoopV34.HorizontalSqr(near,SeaBayExpansionAndBuildSpaceV28.CellPosition(c)))){
    if(!bay.CanPlaceV29(cell,type)||!bay.TryPlaceBuildingAt(cell,type,0,false))continue;
    return bay.GameplayBuildingsV34.First(b=>b.gridX==cell.x&&b.gridZ==cell.y);
   }
   var free=bay.FreeV29(type);
   return bay.TryPlaceBuildingAt(free,type,0,false)?bay.GameplayBuildingsV34.First(b=>b.gridX==free.x&&b.gridZ==free.y):null;
  }
  IEnumerator NativeGameplayV34(){
   var g=game.GameplayV34;
   int watch=g.State.watch;
   Phase("await-v34-time");float end=Time.unscaledTime+12;
   while(g.State.watch==watch&&Time.unscaledTime<end)yield return null;
   Check(g.State.watch==(watch+1)%4,"Windows T changes the actual watch once");

   bool lantern=LanternOn;
   Phase("await-v34-lamp");end=Time.unscaledTime+12;
   while(LanternOn==lantern&&Time.unscaledTime<end)yield return null;
   Check(LanternOn!=lantern,"Windows L toggles the actual boat lantern");
   var host=g.ReviewHost(0);
   if(host==null){Check(false,"Windows workshop host is present");yield break;}
   game.PlaceWalker(host.position+host.forward*.9f);
   Phase("await-v34-workshop");end=Time.unscaledTime+12;
   while(g.Dialog<0&&Time.unscaledTime<end)yield return null;
   Check(g.Dialog==0&&game.UiBlocking,"Windows E opens the boatwright at the house");
   if(g.Dialog!=0)yield break;

   yield return new WaitForSeconds(.6f);
   Canvas.ForceUpdateCanvases();
   var button=(RectTransform)WorkshopButtonV34(0).transform;
   Vector2 target=RectTransformUtility.WorldToScreenPoint(null,button.TransformPoint(button.rect.center));
   var culture=System.Globalization.CultureInfo.InvariantCulture;
   System.IO.File.WriteAllText(System.IO.Path.Combine(evidence,"v34-button.json"),
       "{\"x\":"+target.x.ToString(culture)+",\"y\":"+target.y.ToString(culture)+"}");
   Phase("await-v34-motor");end=Time.unscaledTime+12;
   while(g.State.motor==0&&Time.unscaledTime<end)yield return null;
   Check(g.State.motor==1,"Windows click commits the boatwright motor upgrade");
   yield return Capture("native-v34-workshop");
   Phase("await-v34-close");end=Time.unscaledTime+12;
   while(g.Dialog>=0&&Time.unscaledTime<end)yield return null;
   Check(g.Dialog<0&&!game.Paused&&!game.UiBlocking,"Windows Escape closes the workshop without opening pause");

   game.PlaceWalker(game.Grounded(game.dockFeet));game.ToggleBoard();
   var cargo=g.ReviewFloat(0);game.PlaceBoat(cargo-Vector3.right*3,90);
   Phase("await-v34-hook-tool");end=Time.unscaledTime+12;
   while(game.SelectedTool!=1&&Time.unscaledTime<end)yield return null;
   Check(game.SelectedTool==1,"Windows 2 equips the real salvage hook");
   int coins=game.save.coins;
   Phase("await-v34-tow");end=Time.unscaledTime+12;
   while(!g.Towing&&Time.unscaledTime<end)yield return null;
   Check(g.Towing,"Windows E starts the flotsam cast");
   yield return new WaitForSeconds(2.45f);
   Check(!g.Towing&&game.save.coins==coins+10,"Windows salvage cast reaches the real reward");
  }
  Button WorkshopButtonV34(int index)=>workshopModalV34.Find("WorkshopCard/Action"+index).GetComponent<Button>();
 }
}
