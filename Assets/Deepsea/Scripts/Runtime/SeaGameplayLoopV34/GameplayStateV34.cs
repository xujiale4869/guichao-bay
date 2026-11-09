using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Deepsea {
 [Serializable] public sealed class SeaGameplayProgressV34 {
  public int motor,hold,crabs,bait,watch=1,rareOrdinal,bountyDay,ecoDay;
  public long startedUtc;
  public long[] floatRespawn=new long[8];
  public BayTransferV34 transfer;
  public List<PotHarvestV34> pots=new List<PotHarvestV34>();
  internal void Normalize(){
   motor=Mathf.Clamp(motor,0,2);hold=Mathf.Clamp(hold,0,2);watch=Mathf.Clamp(watch,0,3);
   crabs=Math.Max(crabs,0);bait=Math.Max(bait,0);rareOrdinal=Math.Max(rareOrdinal,0)%3;
   if(floatRespawn==null||floatRespawn.Length!=8)floatRespawn=new long[8];
   if(pots==null)pots=new List<PotHarvestV34>();
   pots.RemoveAll(p=>p==null);
  }
 }
 [Serializable] public sealed class PotHarvestV34 {
  public int x,z;
  public long placedUtc,harvestedUtc;
 }
 [Serializable] public sealed class BayTransferV34 {
  public long revision;
  public int wood,brass,stone,seeds;
  internal bool HasMaterials=>wood!=0||brass!=0||stone!=0||seeds!=0;
 }
 internal static class GameplayProfileV34 {
  // Publish a complete copy at once. Older builds keep their own profile.
  internal static string Import(string name,string previous,bool useBackup=false){
   var target=Path.Combine(Application.persistentDataPath,name);
   if(File.Exists(target))return target;
   var source=File.Exists(target+".bak")?target+".bak":Path.Combine(Application.persistentDataPath,previous);
   if(!File.Exists(source)&&useBackup)source+=".bak";
   if(!File.Exists(source))return target;
   var staging=target+"."+Guid.NewGuid().ToString("N")+".tmp";
   try{
    File.Copy(source,staging);
    try{File.Move(staging,target);}
    catch(IOException) when(File.Exists(target)){}
    if(useBackup&&File.Exists(source+".bak")&&!File.Exists(target+".bak"))
     File.Copy(source+".bak",target+".bak");
   }finally{if(File.Exists(staging))File.Delete(staging);}
   return target;
  }
 }

 public sealed partial class SeaBayExpansionAndBuildSpaceV28 {
  internal IEnumerable<BayPlacedBuildingEntry> GameplayBuildingsV34=>data.placedBuildings;
  internal bool IsWaterV34(Vector3 point)=>DepthAt(point)>.15f;
  internal Transform PotFloatV34(BayPlacedBuildingEntry pot){
   return buildings.TryGetValue(new Vector2Int(pot.gridX,pot.gridZ),out var node)?node.transform.Find("Float"):null;
  }
  internal bool TryLitBeaconV34(Vector3 point,out Vector3 nearest){
   nearest=Vector3.zero;
   float limit=64;
   bool found=false;
   foreach(var entry in data.placedBuildings){
    if(entry.buildType!=1)continue;
    var cell=new Vector2Int(entry.gridX,entry.gridZ);
    var p=CellPosition(cell);
    float distance=SeaBayGameplayLoopV34.HorizontalSqr(p,point);
    if(distance>limit||!buildings.TryGetValue(cell,out var lamp)||!lamp.activeInHierarchy)continue;
    nearest=p;limit=distance;found=true;
   }
   return found;
  }
  internal bool CanApplyV34(BayTransferV34 r){
   return Ready&&saveWritable&&FitsV34(data.wood,r.wood)&&FitsV34(data.brass,r.brass)
       &&FitsV34(data.stone,r.stone)&&FitsV34(data.coralSeed,r.seeds);
  }
  static bool FitsV34(int value,int delta)=>((long)value+delta)>=0&&((long)value+delta)<=int.MaxValue;
  internal bool SyncGameplayV34(){
   if(!SeaBayGameplayLoopV34.Enabled)return true;
   var r=game.save.gameplayV34.transfer;
   if(r==null||!r.HasMaterials||r.revision<=data.appliedGameplayV34)return true;
   if(!CanApplyV34(r))return false;
   long old=data.appliedGameplayV34;
   data.wood+=r.wood;data.brass+=r.brass;data.stone+=r.stone;data.coralSeed+=r.seeds;
   data.appliedGameplayV34=r.revision;
   if(SaveLayout())return true;
   data.wood-=r.wood;data.brass-=r.brass;data.stone-=r.stone;data.coralSeed-=r.seeds;
   data.appliedGameplayV34=old;
   return false;
  }
 }
}
