using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Deepsea { public sealed partial class SeaSeamlessWorldAndNauticalUIV29 {
 bool cameraV31;Vector3 viewTargetV31;float viewSizeV31,viewPitchV31=48,viewYawV31=-32;
 void CameraReviewV31(){if(!cameraV31)return;game.view.orthographic=true;game.view.orthographicSize=viewSizeV31;game.view.transform.rotation=Quaternion.Euler(viewPitchV31,viewYawV31,0);game.view.transform.position=viewTargetV31-game.view.transform.forward*85;}
 IEnumerator VillageFrameV31(string name,Vector3 target,float size,float pitch=48,float yaw=-32){viewTargetV31=target;viewSizeV31=size;viewPitchV31=pitch;viewYawV31=yaw;cameraV31=true;yield return new WaitForSeconds(.2f);yield return Capture(name);cameraV31=false;}
 IEnumerator VillageReviewV31(){
  if(!IsV31)yield break;
  Check(v31VillageHouseCount==3&&housesV31.Count==3,"v31 three grounded coastal occupational houses exist");
  Check(housesV31.All(h=>h.Find("GroundedSeaStoneSkirt").localScale.y<=1.6f),"v31 houses occupy gentle plots without cliff-height foundation walls");
  Check(boatyardAnnexV31!=null&&annexDepthV31>=.55f&&annexDepthV31<=2.5f,"v31 authored boatwright slipway stands in rooted shallow water");
  Check(worldRoot.GetComponentsInChildren<Transform>().Count(q=>q.name.StartsWith("V29_CoastMaple_"))==18,"v31 village clearing relocates generated maples without removing the eighteen trees");
  var clearEnvelope=new Bounds(boatyardAnnexV31.GetComponent<Renderer>().bounds.center,new Vector3(17,5,17));Check(!ShoreObstaclesV31(true).Any(b=>b.Intersects(clearEnvelope)),"v31 slipway clearance includes native static renderers after camera LOD evaluation");
  Check(slipwayRouteV31.All(p=>p.y>=SeaLifeRuntime.Sea+.59f),"v31 every shore-access walking tread remains above the water surface");
  Check(courtsV31.Count==2&&slipwayRouteV31.Count>4,"v31 supported forecourts and continuous shore-to-slipway access exist");
  Check(samplesV31.SelectMany((a,i)=>samplesV31.Skip(i+1).Select(b=>Vector3.Distance(a.position,b.position))).All(d=>d>.5f),"v31 six morphology captures target distinct actual habitat sites");
  Check(_v31FloraMeshes.Distinct().Count()==6&&speciesV31.All(c=>c>0),"v31 six distinct authored marine morphologies actually placed");
  Check(v31DiverseFloraCount>=110&&v31ShorelineEcotoneCount>=20,"v31 actual biodiversity and shoreline transition counts");
  bool tips=true,roots=true;foreach(var t in floraV31){foreach(var p in t.GetComponent<MeshFilter>().sharedMesh.vertices)tips &= t.TransformPoint(p).y<SeaLifeRuntime.Sea-.22f;roots &= Mathf.Abs(t.position.y-(V31Ground(t.position.x,t.position.z)-.025f))<.005f;}
  Check(tips,"v31 every transformed marine vertex stays submerged with sway reserve");Check(roots,"v31 marine roots use actual visible bed height");
  bool authored=housesV31.All(t=>{var f=t.GetChild(0).GetComponent<MeshFilter>();return f!=null&&f.sharedMesh.name.StartsWith("Village_")&&f.GetComponent<MeshRenderer>().sharedMaterials.Length==f.sharedMesh.subMeshCount&&f.sharedMesh.subMeshCount>=4;});Check(authored,"v31 generated house triangles and separate material roles are actually rendered");
  Check(_v31Root.GetComponentsInChildren<Collider>().All(c=>(c.transform.position-game.dockBerth).sqrMagnitude>30),"v31 new building colliders leave the old docking berth clear");
  int oldObjects=_v31Root.GetComponentsInChildren<Transform>(true).Length,oldAssets=_owned.Count;var scales=floraV31.Select(t=>t.localScale).ToArray();BuildVillageAndBiodiversityV31();Check(_v31Root.GetComponentsInChildren<Transform>(true).Length==oldObjects&&_owned.Count==oldAssets&&floraV31.Select((t,i)=>t.localScale==scales[i]).All(x=>x),"v31 repeated initialization does not duplicate geometry, materials or multiply scales");
  Check(_v31Root.Find("V31_CoastalNpcVillage/V31_VillageStonePaths").GetComponentsInChildren<MeshRenderer>().Length==2,"v31 stepping stones and posts merge into two static material batches");
  File.WriteAllText(Path.Combine(evidence,"village-stats.json"),"{\"houses\":"+v31VillageHouseCount+",\"marinePlacements\":"+v31DiverseFloraCount+",\"shorePlacements\":"+v31ShorelineEcotoneCount+",\"species\":["+string.Join(",",speciesV31.Select(x=>x.ToString()).ToArray())+"],\"source\":\"authored Baked meshes from same Blender source triangles\"}");
  var framing=new Bounds(new Vector3(18,0,0),Vector3.zero);foreach(var h in housesV31)framing.Encapsulate(h.position);var aim=framing.center;aim.y=1;float size=37;
  var inverse=Quaternion.Inverse(Quaternion.Euler(48,-32,0));foreach(var h in housesV31){var q=inverse*(h.position-aim);size=Mathf.Max(size,Mathf.Max(Mathf.Abs(q.y)+9,(Mathf.Abs(q.x)+9)/game.view.aspect));}
  yield return VillageFrameV31("11-village-panorama",aim,size);
  yield return VillageFrameV31("12-south-boatwright",housesV31[0].position+Vector3.right*3,13);
  var repairFrame=boatyardAnnexV31.GetComponent<Renderer>().bounds;foreach(var p in slipwayRouteV31){repairFrame.Encapsulate(p+Vector3.up);repairFrame.Encapsulate(p-Vector3.up*1.5f);}repairFrame.Expand(new Vector3(6,2,6));var repairInverse=Quaternion.Inverse(Quaternion.Euler(48,-90,0));float repairSize=12;for(int i=0;i<8;i++){var corner=new Vector3((i&1)==0?repairFrame.min.x:repairFrame.max.x,(i&2)==0?repairFrame.min.y:repairFrame.max.y,(i&4)==0?repairFrame.min.z:repairFrame.max.z);var q=repairInverse*(corner-repairFrame.center);repairSize=Mathf.Max(repairSize,Mathf.Max(Mathf.Abs(q.y)+2,(Mathf.Abs(q.x)+2)/game.view.aspect));}
  yield return VillageFrameV31("12b-waterfront-slipway",repairFrame.center,repairSize,48,-90);
  yield return VillageFrameV31("12c-hull-rib-detail",boatyardAnnexV31.GetComponent<Renderer>().bounds.center+Vector3.up*.35f,4.5f,42,-90);
  yield return VillageFrameV31("15-workshop-terrace-side",housesV31[0].position+Vector3.right*3,11,42,-95);
  yield return VillageFrameV31("15b-tavern-terrace-side",housesV31[1].position+Vector3.right*3,11,42,-95);
  yield return VillageFrameV31("13-tavern-and-ecotone",housesV31[1].position+Vector3.right*4,13);
  yield return VillageFrameV31("02-village-and-flora",housesV31[2].position+Vector3.right*5,18);
  yield return VillageFrameV31("15c-observatory-workdeck",housesV31[2].TransformPoint(new Vector3(.8f,1.6f,3.7f)),5,42,-50);
  yield return VillageFrameV31("15d-tavern-fish-rack",housesV31[1].TransformPoint(new Vector3(0,2.0f,6.0f)),5,40,-55);
  for(int i=0;i<6;i++)if(samplesV31[i]!=null){var sample=samplesV31[i];var bounds=sample.GetComponent<Renderer>().bounds;float specimenSize=Mathf.Clamp(Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z))*.9f,1.6f,3.0f);yield return VillageFrameV31("14-flora-"+i,bounds.center,specimenSize,i==3?58:36,sample.eulerAngles.y+180);}

  File.WriteAllText(Path.Combine(evidence,"village-plots.json"),"{\"positions\":["+string.Join(",",housesV31.Select(h=>JsonUtility.ToJson(h.position)).ToArray())+"],\"relocatedGeneratedMaples\":"+relocatedMaplesV31.Count+",\"slipwayDepth\":"+annexDepthV31.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
  if (CoastalCraftsmanshipUpgradeV32.Enabled) yield return ReviewCraftV32();
  if (LushV33) yield return ReviewLushWorldV33();
  game.SnapWaterCamera();
  if (CoralContrastV311) yield return ReviewCoralContrastV311();
 }
}}
