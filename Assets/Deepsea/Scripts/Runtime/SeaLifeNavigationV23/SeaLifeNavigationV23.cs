using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deepsea {
 [DefaultExecutionOrder(550)]
 public sealed partial class SeaLifeNavigationV23 : MonoBehaviour {
  public bool Baseline {get;private set;}
  public float WaterTime {get;private set;}
  public float EmissionSpeed {get;private set;}
  public int WakeCount {get{return points.Count;}}
  public float WakeLifetime=5.5f;
  internal const float HarborSupportDistance = 1.65f;
  SeaLifeRuntime game;MeshRenderer surface,wakeRenderer;Material oldWater,newWater,wakeMaterial;
  Shader harborWaterShader;
  Transform visual;Mesh wakeMesh;Vector3 previousBoat;float previousSpeed,lastEmission;
  CoastMapleLeaves oldLeaves;Mesh leafMesh,leafRipple;Material leafMaterial,leafRippleMaterial;
  readonly Matrix4x4[] leafMatrices=new Matrix4x4[30];MaterialPropertyBlock leafBlock;
  struct WakePoint {public Vector3 p,right;public float born,speed;}
  readonly List<WakePoint> points=new List<WakePoint>(128);
  readonly List<Vector3> vertices=new List<Vector3>(1024);
  readonly List<Vector2> uvs=new List<Vector2>(1024);
  readonly List<Color> colors=new List<Color>(1024);
  readonly List<int> triangles=new List<int>(2048);
  bool ready;float leafTime;bool reviewMode;
  void Awake(){reviewMode=Environment.GetCommandLineArgs().Contains("-sea-navigation-check");if(reviewMode)Application.logMessageReceived+=RecordLog;}
  IEnumerator Start(){
   game=GetComponent<SeaLifeRuntime>();while(game==null||!game.Ready)yield return null;
   surface=game.world.Find("ShowcaseWater").GetComponent<MeshRenderer>();
   harborWaterShader=Shader.Find("Deepsea/SeaSurfaceSparkleHarborV27");
   oldWater=new Material(Resources.Load<Material>("SeaLifeWaterV22/Water"));newWater=new Material(Resources.Load<Material>("SeaLifeNavigationV23/Water"));surface.sharedMaterial=newWater;
   visual=new GameObject("船身随波 · 视觉层").transform;visual.SetParent(game.boat,false);
   foreach(var child in game.boat.Cast<Transform>().ToArray())if(child!=visual)child.SetParent(visual,true);
   var wake=new GameObject("航迹水纹 · 历史轨迹");wake.transform.SetParent(game.actorRootForWater,false);
   wake.AddComponent<MeshFilter>().sharedMesh=wakeMesh=new Mesh{name="有界历史尾迹"};wakeMesh.MarkDynamic();
   wakeRenderer=wake.AddComponent<MeshRenderer>();wakeMaterial=new Material(Resources.Load<Material>("SeaLifeNavigationV23/Wake"));wakeRenderer.sharedMaterial=wakeMaterial;wakeRenderer.shadowCastingMode=ShadowCastingMode.Off;wakeRenderer.receiveShadows=false;
   oldLeaves=game.world.GetComponentInChildren<CoastMapleLeaves>(true);if(oldLeaves!=null)oldLeaves.enabled=false;
   leafMesh=Resources.Load<Mesh>("CoastMaple/DriftLeaf");leafRipple=Resources.Load<Mesh>("CoastMaple/Ripple");leafMaterial=Resources.Load<Material>("CoastMaple/MapleLeaf");leafRippleMaterial=Resources.Load<Material>("CoastMaple/LeafRipple");leafBlock=new MaterialPropertyBlock();
   previousBoat=game.boat.position;SetupNavigation();game.navigationV23=this;ready=true;
   if(reviewMode){game.store=new SeaLifeStore(Path.Combine(Application.temporaryCachePath,"sea-navigation-v23-review-"+DateTime.UtcNow.Ticks+".json"));game.save=new SeaLifeSave();game.save.Normalize();game.RefreshWaterCollection();StartCoroutine(RunReview());}
   else game.Notify("航行更新：WASD开船，松开滑行，S倒车制动；F7对照v22水面和控制。");
  }
  public static Vector3 Field(Vector2 p,float t){return SeaSurfaceV24.RollingActive?OceanWaveV24.Field(p,SeaSurfaceV24.Active.Clock):NavigationWaveV23.Field(p,t);}
  public float Height(Vector3 p){return SeaLifeRuntime.Sea+(Baseline?SeaLifeWaterV22.Field(new Vector2(p.x,p.z),WaterTime):Field(new Vector2(p.x,p.z),WaterTime)).x;}
  public Vector3 Normal(Vector3 p){var f=Baseline?SeaLifeWaterV22.Field(new Vector2(p.x,p.z),WaterTime):Field(new Vector2(p.x,p.z),WaterTime);return new Vector3(-f.y,1,-f.z).normalized;}
  public void SetBaseline(bool value){Baseline=value;if(surface!=null)surface.sharedMaterial=value?oldWater:newWater;points.Clear();if(wakeMesh!=null)wakeMesh.Clear();previousBoat=game.boat.position;throttle=0;rudder=0;yawRate=0;game.body.drag=value?.8f:0;game.body.angularDrag=value?3:0;}
  void Update(){if(SeaSurfaceV24.Active==null&&ready&&!game.Paused&&Input.GetKeyDown(KeyCode.F7)){SetBaseline(!Baseline);game.Notify(Baseline?"当前为v22水面与控制对照。F7切回v23。":"当前为v23流动水面与惯性船控。F7查看v22对照。");}}
  void LateUpdate(){
   if(!ready)return;if(!game.Paused){WaterTime+=Time.deltaTime;if(oldLeaves!=null&&!oldLeaves.paused)leafTime+=Time.deltaTime;}
   Shader.SetGlobalFloat("_WaterV23Time",WaterTime);Shader.SetGlobalFloat("_WaterV22Time",WaterTime);Shader.SetGlobalFloat("_WaterV22Motion",1);Shader.SetGlobalFloat("_WaterV23Motion",Baseline?0:1);
   var p=game.boat.position;float along=Vector3.Dot(game.body.velocity,game.boat.forward);
   EmissionSpeed=(!game.OnFoot&&!game.Busy&&!game.Paused)?Mathf.Max(0,along):0;
   {
    Shader.SetGlobalVector("_CoastBoat",new Vector4(p.x,p.z,Mathf.Abs(along),(!game.OnFoot&&!game.Busy)?1:0));
    float flowSign=along<-.12f?-1:1;Shader.SetGlobalVector("_CoastBoatHeading",new Vector4(game.boat.forward.x*flowSign,game.boat.forward.z*flowSign,0,0));
    UpdateHullVisual(p,along);
   }
   previousSpeed=along;UpdateWake(p);game.FollowWaterV23(this);DrawLeaves();
  }
  void UpdateHullVisual(Vector3 position,float along){
   float center=Height(position);
   var material=surface.sharedMaterial;
   bool harbor=harborWaterShader!=null&&material!=null&&material.shader==harborWaterShader;
   float bow=0,stern=0;
   Vector3 normal;
   if(harbor){
    Vector3 forward=game.boat.forward,right=game.boat.right;
    bow=Height(position+forward*HarborSupportDistance);
    stern=Height(position-forward*HarborSupportDistance);
    float pitch=(bow-stern)/(2*HarborSupportDistance);
    float roll=(Height(position+right*.75f)-Height(position-right*.75f))/1.5f;
    normal=(Vector3.up-forward*pitch-right*roll).normalized;
   }else{
    normal=HullNormal(position);
   }
   Vector3 localNormal=Quaternion.Inverse(game.boat.rotation)*normal;
   float acceleration=Time.deltaTime>.0001f?(along-previousSpeed)/Time.deltaTime:0;
   var tilt=Quaternion.FromToRotation(Vector3.up,localNormal)*
    Quaternion.Euler(-Mathf.Clamp(acceleration*.8f,-2.1f,2.1f),0,-Mathf.Clamp(yawRate*along*.025f,-3.5f,3.5f));
   visual.localRotation=Quaternion.Slerp(visual.localRotation,tilt,1-Mathf.Exp(-4*Time.deltaTime));

   float height=center;
   if(harbor){
    // Account for the applied pitch before lifting either end of the visual hull.
    float pitchOffset=visual.forward.y*HarborSupportDistance;
    float supportedEnds=Mathf.Max(bow-pitchOffset,stern+pitchOffset);
    height=Mathf.Max(center,Mathf.Max((center+bow+stern)/3f,supportedEnds-.04f))+.06f;
   }
   visual.localPosition=new Vector3(0,(height-SeaLifeRuntime.Sea)/game.boat.lossyScale.y,0);
  }
  void UpdateWake(Vector3 p){
   if((p-previousBoat).sqrMagnitude>16)points.Clear();previousBoat=p;
   for(int i=points.Count-1;i>=0;i--)if(WaterTime-points[i].born>WakeLifetime)points.RemoveAt(i);
   if(EmissionSpeed>.28f){var stern=p-game.boat.forward*2.05f;bool separated=points.Count==0||(stern-points[points.Count-1].p).sqrMagnitude>.055f;
    if(separated&&WaterTime-lastEmission>.035f){if(points.Count==112)points.RemoveAt(0);points.Add(new WakePoint{p=stern,right=game.boat.right,born=WaterTime,speed=EmissionSpeed});lastEmission=WaterTime;}}
   vertices.Clear();uvs.Clear();colors.Clear();triangles.Clear();
   for(int side=-1;side<=1;side+=2){int start=vertices.Count;
    for(int i=0;i<points.Count;i++){var q=points[i];float age=WaterTime-q.born,s=Mathf.Clamp01(q.speed/3);float width=.09f+age*.035f+s*.055f;
     var center=q.p+q.right*side*(.64f+age*(.29f+s*.16f));float fade=Mathf.Sin(Mathf.Clamp01(age/WakeLifetime)*Mathf.PI)*(.10f+.14f*s);
     for(int j=0;j<2;j++){var v=center+q.right*(j==0?-width:width);v.y=Height(v)+.014f;vertices.Add(v);uvs.Add(new Vector2(j,q.born*1.9f));colors.Add(new Color(.42f,.70f,.73f,fade));}
     if(i>0){int k=start+i*2;triangles.Add(k-2);triangles.Add(k);triangles.Add(k-1);triangles.Add(k-1);triangles.Add(k);triangles.Add(k+1);}}
   }
   // A short, soft centre disturbance connects the hull to the spreading V-shaped arms.
   int centreStart=vertices.Count,centreCount=0;
   for(int i=0;i<points.Count;i++){var q=points[i];float age=WaterTime-q.born;if(age>1.55f)continue;float strength=Mathf.Clamp01(q.speed/3.5f),width=.20f+age*.14f;
    for(int j=0;j<2;j++){var v=q.p+q.right*(j==0?-width:width);v.y=Height(v)+.016f;vertices.Add(v);uvs.Add(new Vector2(j,q.born*2.8f));colors.Add(new Color(.51f,.75f,.76f,.11f*strength*(1-age/1.55f)));}
    if(centreCount>0){int k=centreStart+centreCount*2;triangles.AddRange(new[]{k-2,k,k-1,k-1,k,k+1});}centreCount++;
   }
   wakeMesh.Clear();if(vertices.Count>0){wakeMesh.SetVertices(vertices);wakeMesh.SetUVs(0,uvs);wakeMesh.SetColors(colors);wakeMesh.SetTriangles(triangles,0);wakeMesh.RecalculateBounds();}
   wakeRenderer.enabled=!game.InRoom;
  }
  void DrawLeaves(){
   if(oldLeaves==null||!oldLeaves.visible||game.InRoom||leafMesh==null||oldLeaves.targets==null||oldLeaves.targets.Length==0)return;
   float t=oldLeaves.manual?oldLeaves.manualTime:leafTime;
   for(int i=0;i<30;i++){var s=oldLeaves.Evaluate(i,t);float blend=s.floating?1:1-Mathf.SmoothStep(0,.55f,s.p.y-(SeaLifeRuntime.Sea+.026f));
    s.p.y+=(Height(s.p)-SeaLifeRuntime.Sea)*blend;s.q=Quaternion.Slerp(s.q,Quaternion.FromToRotation(Vector3.up,Normal(s.p))*s.q,blend*.65f);
    leafMatrices[i]=Matrix4x4.TRS(s.p,s.q,Vector3.one*s.scale);
    if(s.floating&&s.rippleAge<1.8f){float radius=.05f+s.rippleAge*.13f;leafBlock.SetFloat("_Opacity",.20f*(1-s.rippleAge/1.8f));var p=s.p;p.y=Height(p)+.032f;Graphics.DrawMesh(leafRipple,Matrix4x4.TRS(p,Quaternion.FromToRotation(Vector3.up,Normal(p)),Vector3.one*radius*2),leafRippleMaterial,0,null,0,leafBlock,ShadowCastingMode.Off,false);}}
   Graphics.DrawMeshInstanced(leafMesh,0,leafMaterial,leafMatrices,30,null,ShadowCastingMode.Off,true,0,null,LightProbeUsage.Off);
  }
  void OnDestroy(){if(reviewMode)Application.logMessageReceived-=RecordLog;if(game!=null&&game.navigationV23==this)game.navigationV23=null;if(hullMesh!=null)Destroy(hullMesh);if(hullFriction!=null)Destroy(hullFriction);if(oldLeaves!=null)oldLeaves.enabled=true;if(oldWater!=null)Destroy(oldWater);if(newWater!=null)Destroy(newWater);if(wakeMaterial!=null)Destroy(wakeMaterial);if(wakeMesh!=null)Destroy(wakeMesh);}
 }
}
