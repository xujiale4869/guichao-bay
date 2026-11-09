using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Deepsea {
 public sealed partial class SeaLifeRuntime {
  readonly Vector3[] tankSlots={new Vector3(-4,0,1.7f),new Vector3(1.5f,0,1.7f),new Vector3(-4,0,-2.7f)};
  Transform[] tankObjects=new Transform[3],slotObjects=new Transform[3];Transform indoorFish,indoorDecor;Material indoorSand;
  public Vector3 TankPosition(int index){return roomOrigin+tankSlots[Mathf.Clamp(index,0,2)];}
  void SetupAquarium(){
   room=Group("海洋小馆 · 可步行展室",null,roomOrigin);indoorSand=Lit("缸底暖砂",new Color(.72f,.67f,.48f));
   var floorWood=Clone(wood);floorWood.SetFloat("_Scale",.8f);floorWood.SetColor("_BaseColor",new Color(.68f,.63f,.54f));Box("展室地板",room,new Vector3(0,-.2f,0),new Vector3(15,.4f,11),floorWood,true);
   for(int i=0;i<20;i++)Box("木地板分缝",room,new Vector3(-7.3f+i*.75f,.007f,0),new Vector3(.014f,.012f,11),rope);
   Box("北侧灰泥墙",room,new Vector3(0,1.8f,5.45f),new Vector3(15,3.6f,.22f),Lit("展馆暖灰泥",new Color(.66f,.69f,.62f)),true);
   Box("西侧矮墙",room,new Vector3(-7.45f,1.45f,0),new Vector3(.22f,2.9f,11),stone,true);
   for(int i=-1;i<=1;i++){Box("背墙木柱",room,new Vector3(i*6.7f,1.8f,5.2f),new Vector3(.18f,3.6f,.18f),wood);Box("横梁",room,new Vector3(0,3.4f,5.15f),new Vector3(14.7f,.18f,.2f),wood);}
   Box("窗口深色边框",room,new Vector3(2.3f,2.0f,5.23f),new Vector3(4.8f,1.7f,.08f),metal);Box("窗后青蓝光",room,new Vector3(2.3f,2.0f,5.15f),new Vector3(4.4f,1.4f,.08f),Lit("窗后海色",new Color(.23f,.46f,.45f)));
   Box("入口地毯",room,new Vector3(5,0.02f,-3.8f),new Vector3(2.4f,.03f,2.3f),Lit("入口织物",new Color(.19f,.32f,.32f)));
   var lamp=Group("展室暖灯",room,new Vector3(1,3.2f,0));var light=lamp.gameObject.AddComponent<Light>();light.type=LightType.Point;light.range=16;light.intensity=1.5f;light.color=new Color(1,.85f,.62f);light.shadows=LightShadows.None;
   for(int i=0;i<3;i++){slotObjects[i]=Group("预留建设位 "+(i+1),room,tankSlots[i]);for(int e=0;e<4;e++){var edge=Box("地面落位边",slotObjects[i],new Vector3(e<2?(e==0?-1.85f:1.85f):0,.025f,e>=2?(e==2?-1.05f:1.05f):0),e<2?new Vector3(.035f,.025f,2.1f):new Vector3(3.7f,.025f,.035f),metal);}}
   indoorFish=Group("展缸内收藏鱼",room,Vector3.zero);indoorDecor=Group("带回的贝壳与船锚",room,Vector3.zero);room.gameObject.SetActive(false);
  }
  void EnterRoom(){if(!OnFoot||InRoom)return;CancelActivity();InRoom=true;exterior.gameObject.SetActive(false);room.gameObject.SetActive(true);PlaceWalker(roomOrigin+new Vector3(5,0,-3.8f));RefreshCollection();Panel=0;CloseView=false;ApplyCamera(true);ui.RebuildPanel();Notify("海洋小馆：B选择建设位摆缸；靠近鱼缸E放鱼，V近景观赏。");}
  public void LeaveRoom(){if(!InRoom)return;Panel=0;CloseView=false;BuildMode=false;InRoom=false;room.gameObject.SetActive(false);exterior.gameObject.SetActive(true);PlaceWalker(Grounded(galleryDoor+Vector3.forward*.7f));ApplyCamera(true);ui.RebuildPanel();SaveNow();Notify("回到红枫码头。可去潮池拾贝，或回船继续探索。");}
  int NearestTank(){if(!InRoom)return -1;int found=-1;float distance=3.2f;for(int i=0;i<3;i++){float d=Vector3.Distance(walker.position,TankPosition(i)+new Vector3(0,0,-1.5f));if(d<distance){found=i;distance=d;}}return found;}
  public void PickTank(int index){SelectedTank=Mathf.Clamp(index,0,2);CloseView=false;ui.RebuildPanel();}
  public int TankCount(int index){return save.fish.Count(f=>f.tank==index);}
  public int BuildCost{get{return save.tanks.Any(t=>t.built)?35:0;}}
  public bool BuildTank(){if(!InRoom||save.tanks[SelectedTank].built)return false;int cost=BuildCost;if(save.coins<cost){Notify("金币不足。首缸免费，其余每缸35金币；码头可出售随身鱼。");return false;}save.coins-=cost;save.tanks[SelectedTank].built=true;RefreshCollection();SaveNow();ui.RebuildPanel();Cue();Notify("实体展缸已摆好。走到缸旁按E，选择随身鱼放入。");return true;}
  public bool MoveFish(string id,int tank){if(!InRoom||tank< -1||tank>2)return false;var f=save.fish.Find(x=>x.id==id);if(f==null||f.tank==tank)return false;if(tank>=0&&(!save.tanks[tank].built||TankCount(tank)>=6)){Notify("这只缸未建设，或已满6条。");return false;}if(tank<0&&CarryCount>=MaxCarryCount){Notify("随身活鱼箱已满"+MaxCarryCount+"格。");return false;}f.tank=tank;RefreshCollection();SaveNow();ui.RebuildPanel();Cue(true);return true;}
  public void ChangeTankTheme(){if(!save.tanks[SelectedTank].built)return;save.tanks[SelectedTank].kelp=!save.tanks[SelectedTank].kelp;RefreshCollection();SaveNow();ui.RebuildPanel();}
  public void ToggleShellDisplay(){if(!save.shells.Any(x=>x))return;save.shellDisplay=!save.shellDisplay;RefreshCollection();SaveNow();ui.RebuildPanel();}
  public void ToggleAnchorDisplay(){if(!save.salvaged)return;save.anchorDisplay=!save.anchorDisplay;RefreshCollection();SaveNow();ui.RebuildPanel();}
  void RefreshCollection(){if(save==null||room==null)return;for(int i=0;i<3;i++){if(tankObjects[i]!=null)Destroy(tankObjects[i].gameObject);slotObjects[i].gameObject.SetActive(!save.tanks[i].built);if(save.tanks[i].built)tankObjects[i]=CreateTank(i,save.tanks[i].kelp);}
   foreach(Transform t in indoorFish)Destroy(t.gameObject);foreach(var f in save.fish){if(f.tank<0)continue;var model=f.variantV34>0?CreateRareModelV34(indoorFish,f.variantV34,.65f):MakeFishObject(FishNames[f.species]+" · "+f.id,indoorFish,f.species,.65f);int index=save.fish.Where(x=>x.tank==f.tank).ToList().IndexOf(f);var motion=model.gameObject.AddComponent<SeaLifeFishMotion>();motion.center=tankSlots[f.tank]+new Vector3(0,1.55f+(index%2)*.12f,-.28f);motion.extent=new Vector3(1.05f,.18f,.30f);motion.phase=index*2.39996f;motion.speed=.42f+f.species*.08f;}
   foreach(Transform t in indoorDecor)Destroy(t.gameObject);if(save.shellDisplay){for(int i=0;i<3;i++)CreateShell("带回的贝壳",indoorDecor,new Vector3(-5.2f+i*.4f,.94f,2.1f),.7f);}
   if(save.anchorDisplay){var p=Group("旧船锚展示",indoorDecor,new Vector3(4.6f,.6f,2.7f));Box("底座",p,new Vector3(0,-.3f,0),new Vector3(1.3f,.6f,1),stone);var l=Line("旧铁锚",p,metal,.13f);l.positionCount=7;var points=new[]{new Vector3(0,1.8f,0),new Vector3(0,.15f,0),new Vector3(-.65f,.6f,0),new Vector3(0,.15f,0),new Vector3(.65f,.6f,0),new Vector3(0,.15f,0),new Vector3(0,1.8f,0)};for(int i=0;i<points.Length;i++)l.SetPosition(i,p.position+points[i]);Box("锚横杆",p,new Vector3(0,1.1f,0),new Vector3(1.1f,.12f,.12f),metal);}
  }
  Transform CreateTank(int index,bool kelp){var t=Group("展缸 "+(char)('A'+index),room,tankSlots[index]);Box("木柜底座",t,new Vector3(0,.4f,0),new Vector3(3.65f,.8f,2.1f),wood,true);Box("缸底砂",t,new Vector3(0,.86f,0),new Vector3(3.3f,.12f,1.75f),indoorSand);
   foreach(float x in new[]{-1.76f,1.76f})foreach(float z in new[]{-.98f,.98f})Box("缸角细框",t,new Vector3(x,1.5f,z),new Vector3(.045f,1.45f,.045f),metal);
   foreach(float y in new[]{.82f,2.20f}){foreach(float z in new[]{-.98f,.98f})Box("缸边横框",t,new Vector3(0,y,z),new Vector3(3.56f,.045f,.045f),metal);foreach(float x in new[]{-1.76f,1.76f})Box("缸边侧框",t,new Vector3(x,y,0),new Vector3(.045f,.045f,2),metal);}
   Box("背玻璃",t,new Vector3(0,1.5f,.98f),new Vector3(3.5f,1.4f,.018f),glass);Box("左玻璃",t,new Vector3(-1.75f,1.5f,0),new Vector3(.018f,1.4f,1.95f),glass);Box("右玻璃",t,new Vector3(1.75f,1.5f,0),new Vector3(.018f,1.4f,1.95f),glass);Box("前玻璃",t,new Vector3(0,1.5f,-.98f),new Vector3(3.5f,1.4f,.018f),glass);Box("局部水面",t,new Vector3(0,2.1f,0),new Vector3(3.45f,.015f,1.9f),waterTint);
   var rock=Primitive("缸内主石",PrimitiveType.Sphere,t,new Vector3(.9f,1.05f,.48f),new Vector3(.65f,.45f,.45f),stone);rock.localRotation=Quaternion.Euler(8,32,0);
   if(kelp){var source=world.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f=>f.name.Contains("Kelp"));if(source!=null){var mesh=Instantiate(source.sharedMesh);var b=mesh.bounds;var v=mesh.vertices;var scale=new Vector3(.45f/Mathf.Max(.01f,b.size.x),1.05f/Mathf.Max(.01f,b.size.y),.32f/Mathf.Max(.01f,b.size.z));for(int j=0;j<v.Length;j++)v[j]=Vector3.Scale(v[j]-new Vector3(b.center.x,b.min.y,b.center.z),scale);mesh.vertices=v;mesh.RecalculateBounds();for(int j=0;j<2;j++){var plant=Group("缸景绿色长带海带",t,new Vector3(-1.1f+j*.65f,.92f,.43f));plant.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;plant.gameObject.AddComponent<MeshRenderer>().sharedMaterial=leafMat;}}}
   WorldText("展缸 "+(char)('A'+index),t,new Vector3(0,.46f,-1.065f),.65f);DecorateTankV25(t,index);return t;
  }
 }
}
