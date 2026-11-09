using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Deepsea {
 public sealed partial class SeaLifeRuntime {
  public int fishingStage,salvageStage;public float catchProgress,tension;public bool AutoReel;public int pendingSpecies;public Vector3 castPoint;
  float activityTime,activityStart,castDuration=.8f;bool struggling;Vector3 startCast;Transform rodRoot;Vector3 galleryDoor=new Vector3(-10,.9f,3.1f);readonly List<Vector3> shellPositions=new List<Vector3>();
  readonly List<Transform> rippleMarkers=new List<Transform>();
  void SetupShore(){
   var front=Group("海洋小馆 · 码头入口",actorRoot,new Vector3(-10,.8f,1.2f));Box("小馆木平台",front,new Vector3(0,-.16f,0),new Vector3(4.8f,.3f,3.9f),wood,true);
   foreach(float x in new[]{-2.1f,2.1f})foreach(float z in new[]{-1.6f,1.6f})Box("廊柱",front,new Vector3(x,1.45f,z),new Vector3(.15f,2.9f,.15f),wood);
   Box("低矮栏墙",front,new Vector3(0,.40f,-1.7f),new Vector3(4.6f,.8f,.15f),stone,true);foreach(float x in new[]{-2.1f,2.1f})Box("开敞廊架纵梁",front,new Vector3(x,2.9f,0),new Vector3(.18f,.18f,3.9f),wood);foreach(float z in new[]{-1.6f,1.6f})Box("开敞廊架横梁",front,new Vector3(0,2.9f,z),new Vector3(4.8f,.18f,.18f),wood);
   var sign=Box("海洋小馆门牌",front,new Vector3(0,2.15f,1.76f),new Vector3(2.8f,.65f,.12f),metal);WorldText("海洋小馆",sign,new Vector3(0,0,-.6f),1);
   var tidal=Group("栈桥旁潮池",actorRoot,tideCenter);Primitive("潮池浅石",PrimitiveType.Sphere,tidal,new Vector3(0,-.16f,0),new Vector3(5.8f,.55f,3.8f),stone,true);Box("小步板",actorRoot,new Vector3(-7.8f,.45f,4.9f),new Vector3(2.0f,.18f,2.1f),wood,true);
   for(int i=0;i<4;i++){var p=tideCenter+new Vector3(-1.2f+i*.76f,.14f,Mathf.Sin(i*1.9f)*.55f);shellPositions.Add(p);var s=CreateShell("潮池贝壳 "+i,actorRoot,p,1);shellObjects.Add(s);}
   var marker=Primitive("打捞小浮标",PrimitiveType.Cylinder,actorRoot,new Vector3(cratePoint.x,Sea+.17f,cratePoint.z),new Vector3(.22f,.38f,.22f),metal);Primitive("浮标暖色帽",PrimitiveType.Sphere,marker,new Vector3(0,.53f,0),new Vector3(1.1f,.35f,1.1f),cream);
   crate=Group("浅海旧箱",actorRoot,cratePoint);Box("木箱",crate,Vector3.zero,new Vector3(.9f,.65f,.72f),wood);for(int i=-1;i<=1;i+=2)Box("绑带",crate,new Vector3(i*.28f,0,0),new Vector3(.045f,.68f,.76f),metal);Primitive("吊环",PrimitiveType.Sphere,crate,new Vector3(0,.4f,0),Vector3.one*.12f,metal);
   hook=Primitive("绞盘挂钩",PrimitiveType.Capsule,actorRoot,Vector3.zero,new Vector3(.10f,.17f,.1f),metal);hook.gameObject.SetActive(false);towLine=Line("绞盘缆线",actorRoot,rope,.018f);towLine.enabled=false;
  }
  void WorldText(string value,Transform parent,Vector3 position,float size){var go = new GameObject(value);
   // Initialize TMP while active so Awake caches the renderer before the font setter.
   // Saved aquarium tanks can be restored under an inactive room at startup.
   var text = go.AddComponent<TMPro.TextMeshPro>();
   text.font = Resources.Load<TMPro.TMP_FontAsset>("SeaLifeV21/SeaLifeChinese");
   go.transform.SetParent(parent, false);
   go.transform.localPosition = position;
   go.transform.rotation = Quaternion.identity;text.text=value;text.fontSize=3.5f;text.alignment=TMPro.TextAlignmentOptions.Center;text.color=new Color(.96f,.86f,.65f);text.rectTransform.sizeDelta=new Vector2(2.7f,.6f);var s=parent.lossyScale;text.transform.localScale=new Vector3(size/s.x,size/s.y,size/s.z);}
  Transform CreateShell(string name,Transform parent,Vector3 p,float scale){var root=Group(name,parent,p);var verts=new List<Vector3>{new Vector3(0,.06f,-.15f)};int steps=18;for(int i=0;i<=steps;i++){float a=i*Mathf.PI/steps;verts.Add(new Vector3(Mathf.Cos(a)*.22f,.05f+Mathf.Sin(a)*.08f,Mathf.Sin(a)*.30f-.12f));}var tris=new List<int>();for(int i=1;i<=steps;i++)tris.AddRange(new[]{0,i+1,i});var mesh=new Mesh{name="拾贝 · 扇贝"};mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=cream;root.localScale=Vector3.one*scale;for(int i=0;i<6;i++){float a=(i+1)*Mathf.PI/7;var rib=Line("贝壳肋纹",root,rope,.006f);rib.useWorldSpace=false;rib.SetPosition(0,new Vector3(0,.07f,-.15f));rib.SetPosition(1,new Vector3(Mathf.Cos(a)*.21f,.13f,Mathf.Sin(a)*.29f-.12f));}return root;}
  void RefreshShells(){for(int i=0;i<shellObjects.Count;i++)shellObjects[i].gameObject.SetActive(!save.shells[i]);if(crate!=null)crate.gameObject.SetActive(!save.salvaged);}
  int NearestShell(){if(!OnFoot||InRoom)return -1;for(int i=0;i<shellPositions.Count;i++)if(!save.shells[i]&&Vector3.Distance(walker.position,shellPositions[i])<1.5f)return i;return -1;}
  public bool PickupShell(int i){if(i<0||i>=4||save.shells[i])return false;save.shells[i]=true;RefreshShells();SaveNow();Cue();Notify("拾得一枚贝壳。回海洋小馆，可以把它放进展缸布景。");return true;}
  void SetupFishing(){
   rodRoot=Group("钓竿 · 手持",captain,new Vector3(.22f,.78f,.34f));rodRoot.localRotation=Quaternion.Euler(-22,0,0);rod=Primitive("竹竿",PrimitiveType.Cylinder,rodRoot,new Vector3(0,0,1.15f),new Vector3(.025f,1.15f,.025f),wood);rod.localRotation=Quaternion.Euler(90,0,0);rodTip=Group("竿尖",rodRoot,new Vector3(0,0,2.30f));rodRoot.gameObject.SetActive(false);
   bobber=Group("红白浮漂",actorRoot,Vector3.zero);Primitive("白漂",PrimitiveType.Sphere,bobber,Vector3.zero,new Vector3(.13f,.18f,.13f),cream);Primitive("漂帽",PrimitiveType.Sphere,bobber,Vector3.up*.09f,new Vector3(.11f,.08f,.11f),Lit("浮漂红",new Color(.83f,.22f,.14f)));bobber.gameObject.SetActive(false);
   fishingLine=Line("钓线",actorRoot,rope,.010f);fishingLine.positionCount=18;fishingLine.enabled=false;fishTarget=MakeFishObject("上钩鱼影",actorRoot,0,.6f);fishTarget.gameObject.SetActive(false);
   for(int i=0;i<fishSpots.Length;i++){var group=Group("鱼影与水纹 · "+areaNames[i],actorRoot,fishSpots[i]);for(int j=0;j<3;j++){var f=MakeFishObject("近水鱼影",group,(i+j)%3,.45f);var motion=f.gameObject.AddComponent<SeaLifeFishMotion>();motion.center=new Vector3(0,-.35f,0);motion.extent=new Vector3(1.4f,.05f,.7f);motion.phase=j*2.1f;motion.speed=.6f;}
    var ring=Line("水面轻纹",group,glass,.025f);ring.positionCount=45;for(int j=0;j<45;j++){float a=j*Mathf.PI*2/44;ring.SetPosition(j,fishSpots[i]+new Vector3(Mathf.Cos(a)*1.8f,.04f,Mathf.Sin(a)*1.8f));}rippleMarkers.Add(group);}
  }
  int FishingArea(){for(int i=0;i<fishSpots.Length;i++)if(Vector3.Distance(boat.position,fishSpots[i])<8)return i;return -1;}
  public bool BeginFishing(){if(OnFoot||InRoom||Busy)return false;int area=FishingArea();if(area<0&&(GameplayV34==null||!GameplayV34.CanStartNightFishing)){Notify("这里暂时没有鱼影，去海带湾、浅礁水道或湾口清水看看。");return false;}if(CarryCount>=MaxCarryCount){Notify("活鱼箱"+MaxCarryCount+"格已满，先到小馆放鱼或出售。");return false;}body.velocity=Vector3.zero;body.angularVelocity=Vector3.zero;fishingStage=1;activityTime=0;rodRoot.gameObject.SetActive(true);castPoint=area>=0?fishSpots[area]:boat.position+boat.forward*4;pendingSpecies=(Mathf.Max(0,area)+save.fish.Count)%3;pendingVariantV34=0;fastBiteV34=false;Panel=0;ui.RebuildPanel();Notify("已取出钓竿。鼠标指向附近水面，左键投钩；Esc可取消。");return true;}
  public bool Cast(Vector3 point){if(fishingStage!=1)return false;var delta=point-boat.position;delta.y=0;if(delta.sqrMagnitude<.01f)delta=boat.forward;point=boat.position+delta.normalized*Mathf.Clamp(delta.magnitude,2.8f,7);point.y=Sea;var hits=Physics.RaycastAll(point+Vector3.up*20,Vector3.down,20);if((GameplayV34!=null&&!GameplayV34.CanCastAt(point))||hits.Any(h=>!h.transform.IsChildOf(boat)&&!h.transform.IsChildOf(walker)&&ground.Contains(h.collider)&&h.point.y>Sea+.08f)){Notify("落钩点在岸上，请选择水面。");return false;}castPoint=point;fastBiteV34=GameplayV34!=null&&GameplayV34.LitWater(point);startCast=rodTip.position;fishingStage=2;activityTime=0;bobber.gameObject.SetActive(true);fishingLine.enabled=true;Cue(true);return true;}
  public bool StartReeling(){if(fishingStage!=3)return false;fishingStage=4;catchProgress=0;tension=12;activityTime=0;fishTarget.gameObject.SetActive(true);fishTarget.GetComponent<MeshFilter>().sharedMesh=fishMeshes[pendingSpecies];ApplyCatchModelV34();Cue();return true;}
  void UpdateFishing(){if(fishingStage==0||Paused)return;activityTime+=Time.deltaTime;if(fishingStage==1&&!UiBlocking&&Input.GetMouseButtonDown(0)&&!ui.PointerOverUi()){var ray=view.ScreenPointToRay(Input.mousePosition);float d;if(new Plane(Vector3.up,new Vector3(0,Sea,0)).Raycast(ray,out d))Cast(ray.GetPoint(d));}
   if(fishingStage==2){if(fastBiteV34&&!GameplayV34.LitWater(castPoint))fastBiteV34=false;float t=Mathf.Clamp01(activityTime/castDuration);bobber.position=Vector3.Lerp(startCast,castPoint,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*1.5f;if(activityTime>BiteWaitV34){CheckNightBiteV34();fishingStage=3;activityTime=0;Cue(true);Notify("浮漂下沉了，按E提竿！");}}
   if(fishingStage==3)bobber.position=castPoint+Vector3.up*(-.14f+Mathf.Sin(clock*18)*.025f);
   if(fishingStage==4){struggling=activityTime%2.8f>1.8f;bool reel=AutoInput?AutoReel:Input.GetMouseButton(0)&&!ui.PointerOverUi();if(reel){catchProgress+=Time.deltaTime*13;tension+=Time.deltaTime*(struggling?37:7);}else tension-=Time.deltaTime*31;tension=Mathf.Clamp(tension,0,100);var side=Vector3.Cross(Vector3.up,castPoint-boat.position).normalized;bobber.position=Vector3.Lerp(castPoint,boat.position+boat.right*1.4f,catchProgress/100)+side*Mathf.Sin(clock*(struggling?8:3))*(struggling?.55f:.1f);bobber.position=new Vector3(bobber.position.x,Sea+.01f,bobber.position.z);fishTarget.position=bobber.position+Vector3.down*.22f;fishTarget.rotation=Quaternion.Euler(0,clock*80,Mathf.Sin(clock*9)*12);if(tension>=100){CancelActivity();Notify("张力过高，鱼脱钩了。可以马上重试。");}else if(catchProgress>=100){fishingStage=5;fishTarget.position=boat.position+boat.right*1.1f+Vector3.up*.8f;Panel=3;ui.RebuildPanel();Cue(true);}}
   if(fishingStage==2&&activityTime>castDuration&&activityTime<BiteWaitV34)bobber.position=castPoint+Vector3.up*Mathf.Sin(clock*5)*.025f;
   if(fishingLine.enabled){for(int i=0;i<18;i++){float t=i/17f;var p=Vector3.Lerp(rodTip.position,bobber.position,t);p.y-=Mathf.Sin(t*Mathf.PI)*(fishingStage==4?.04f:.16f);fishingLine.SetPosition(i,p);}}
  }
  public bool KeepCatch(){
   if(fishingStage!=5||CarryCount>=MaxCarryCount)return false;
   var name=PendingFishName;
   var fish=new SeaLifeFish{id=Guid.NewGuid().ToString("N"),species=pendingSpecies,tank=-1,variantV34=pendingVariantV34,
       area=pendingVariantV34>0?"提灯浅湾":areaNames[Mathf.Max(0,FishingArea())]};
   if(GameplayV34!=null){
    if(!GameplayV34.Commit(s=>{
     s.fish.Add(fish);
     if(fish.variantV34==0)return;
     s.gameplayV34.rareOrdinal=(s.gameplayV34.rareOrdinal+1)%3;
     if(s.gameplayV34.bait>0)s.gameplayV34.bait--;
    }))return false;
   }else{save.fish.Add(fish);SaveNow();}
   CancelActivity();RefreshCollection();Notify("已收进活鱼箱："+name+"。可以回馆放入展缸。");return true;
  }
  public void ReleaseCatch(){if(fishingStage!=5)return;CancelActivity();Cue(true);Notify("鱼已放流，随身收藏不变。");}
  string FishingHint(){return fishingStage==1?"左键选择附近水面投钩 · Esc取消":fishingStage==2?(fastBiteV34?"灯下有鱼影聚集 · 留意浮漂":"看浮漂：正在等待鱼咬钩"):fishingStage==3?"浮漂下沉！E提竿":fishingStage==4?(struggling?"鱼在挣扎：松开左键卸力":"鱼平稳：按住左键收线") :"捕获成功：选择收进鱼箱或放流";}
  void PoseFishing(){if(captainRig==null)return;var d=castPoint-boat.position;d.y=0;if(d.sqrMagnitude>.1f)captain.rotation=Quaternion.LookRotation(d);captainRig.rightArm.localRotation=Quaternion.Euler(-46+(fishingStage==4?Mathf.Sin(clock*7)*8:0),0,-5);captainRig.leftArm.localRotation=Quaternion.Euler(-48,20,4);captainRig.rightElbow.localRotation=Quaternion.Euler(-25,0,0);rodRoot.localRotation=Quaternion.Euler(fishingStage==4?-36:-22,0,0);}
  public bool BeginSalvage(){if(Busy||OnFoot||InRoom)return false;if(save.salvaged){Notify("这只旧箱已经打捞，可以去海带湾钓鱼。");return false;}if(Vector3.Distance(boat.position,new Vector3(cratePoint.x,Sea,cratePoint.z))>5.8f){Notify("到蓝色小浮标旁停船，再按E下放挂钩。");return false;}body.velocity=Vector3.zero;body.angularVelocity=Vector3.zero;salvageStage=1;activityTime=0;hook.gameObject.SetActive(true);towLine.enabled=true;Notify("挂钩已对准吊环。按住左键启动绞盘，Esc可取消。");return true;}
  void UpdateSalvage(){if(salvageStage==0||Paused)return;bool pull=AutoInput?AutoReel:Input.GetMouseButton(0)&&!ui.PointerOverUi();if(pull)activityTime+=Time.deltaTime;float p=Mathf.Clamp01(activityTime/3.2f);var up=Vector3.Lerp(cratePoint,boat.position+boat.right*1.4f+Vector3.up*.45f,p);crate.position=up;hook.position=up+Vector3.up*.45f;towLine.SetPosition(0,boat.position+boat.right*.5f+Vector3.up*.8f);towLine.SetPosition(1,hook.position);if(p>=1){save.salvaged=true;salvageStage=0;crate.gameObject.SetActive(false);hook.gameObject.SetActive(false);towLine.enabled=false;SaveNow();Cue(true);Notify("旧箱中找到船锚摆件。回馆可布置在展区旁。");}}
  public void CancelActivity(){GameplayV34?.CancelTow();pendingVariantV34=0;fastBiteV34=false;fishingStage=0;salvageStage=0;Panel=0;rodRoot.gameObject.SetActive(false);bobber.gameObject.SetActive(false);fishingLine.enabled=false;fishTarget.gameObject.SetActive(false);hook.gameObject.SetActive(false);towLine.enabled=false;if(!save.salvaged){crate.position=cratePoint;crate.gameObject.SetActive(true);}captain.localRotation=Quaternion.identity;AutoReel=false;if(ui!=null)ui.RebuildPanel();}
  Mesh MakeFish(int species){var v=new List<Vector3>();var tris=new List<int>();int sides=12;float wide=species==1?.68f:species==2?1.35f:1;float tall=species==2?1.45f:1;float[] z={-.35f,-.18f,.02f,.22f,.38f},r={.015f,.11f,.14f,.085f,.01f};for(int j=0;j<z.Length;j++)for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Cos(a)*r[j]*wide,Mathf.Sin(a)*r[j]*tall,z[j]));}for(int j=0;j<z.Length-1;j++)for(int i=0;i<sides;i++){int a=j*sides+i,b=j*sides+(i+1)%sides;tris.AddRange(new[]{a,b,b+sides,a,b+sides,a+sides});}int n=v.Count;v.AddRange(new[]{new Vector3(0,0,-.3f),new Vector3(0,.20f,-.56f),new Vector3(0,-.20f,-.56f),new Vector3(0,.12f,.02f),new Vector3(0,.30f* tall,-.1f),new Vector3(0,.04f,-.21f)});tris.AddRange(new[]{n,n+1,n+2,n+2,n+1,n,n+3,n+4,n+5,n+5,n+4,n+3});var mesh=new Mesh{name="海湾生活鱼 · "+FishNames[species]};mesh.SetVertices(v);mesh.SetTriangles(tris,0);mesh.SetUVs(0,v.Select(p=>new Vector2(p.z+.6f,p.y+.5f)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
  Transform MakeFishObject(string name,Transform parent,int species,float scale){var g=Group(name,parent,Vector3.zero);g.localScale=Vector3.one*scale;g.gameObject.AddComponent<MeshFilter>().sharedMesh=fishMeshes[species];g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=fishMaterials[species];DecorateFishV25(g,species);return g;}
  void SetupAmbientFish(){for(int i=0;i<34;i++){var f=MakeFishObject("沿用海岬鱼群表现",actorRoot,i%3,.32f);var motion=f.gameObject.AddComponent<SeaLifeFishMotion>();motion.center=new Vector3(13,-2.8f,11);motion.extent=new Vector3(3,.25f,5);motion.phase=i*2.39996f;motion.speed=.22f;}}
 }
}
