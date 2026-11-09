using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace Deepsea {
 [DefaultExecutionOrder(880)]
 public sealed partial class SeaHarborAndReefUpgradeV27 : MonoBehaviour {
  public static SeaHarborAndReefUpgradeV27 Active {get;private set;}
  public bool v27AtmosphereEnabled=true;
  [Range(0,1.5f)] public float coralVibrancy=1.05f;
  [Range(.5f,3.5f)] public float lanternLightIntensity=2.15f;
  [Range(.05f,.35f)] public float kelpSwayAmplitude=.16f;
  public bool Ready {get;private set;}
  SeaLifeRuntime game;Transform worldRoot,boatRoot,boatPivot,pierPivot;Light boatLight,pierLight;
  Material wood,bronze,rope,glow,kelp;Material[] corals;
  readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
  readonly Dictionary<Renderer,Material[]> originalMaterials=new Dictionary<Renderer,Material[]>();
  readonly Dictionary<MeshFilter,Mesh> originalMeshes=new Dictionary<MeshFilter,Mesh>();
  readonly List<Action<bool>> restores=new List<Action<bool>>();
  Vector3 previousVelocity;Vector2 swing,swingVelocity;float clock;int loadedMeshes;
  Material water;Shader originalWaterShader,harborWaterShader;float originalReefLife;
  public int ChangedRenderers {get;private set;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Bootstrap(){if(SceneManager.GetActiveScene().name!="CoastSeaHarborV27"&&!Environment.GetCommandLineArgs().Contains("-harbor-v27"))return;if(FindObjectOfType<SeaHarborAndReefUpgradeV27>()!=null)return;var rt=FindObjectOfType<SeaLifeRuntime>();if(rt!=null)rt.gameObject.AddComponent<SeaHarborAndReefUpgradeV27>();}
  void Awake(){Active=this;}
  IEnumerator Start(){
   game=GetComponent<SeaLifeRuntime>();if(game==null)yield break;
   while(!game.Ready||SeaWaterArtUpgradeV26.Active==null||!SeaWaterArtUpgradeV26.Active.Ready)yield return null;
   InitMaterials();worldRoot=Group("V27_HarborAndReefArt",game.world,Vector3.zero);
   Transform visual=game.boat.Find("船身随波 · 视觉层");boatRoot=Group("V27_BoatArtDetails",visual!=null?visual:game.boat,Vector3.zero);
   BuildBoat();BuildPier();BuildGallery();UpgradeReef();
   var wr=game.world.Find("ShowcaseWater").GetComponent<Renderer>();water=wr.sharedMaterial;originalWaterShader=water.shader;harborWaterShader=ApplyMistCapeEmeraldWaterV35.Bind(game,Resources.Load<Material>("SeaHarborV27/WaterShaderKeep").shader);
   originalReefLife=Shader.GetGlobalFloat("_BayReefLife");
   ApplyAtmosphereState(v27AtmosphereEnabled);Ready=true;
   game.Notify("v27 港湾与礁区 · F8岸景/珊瑚对照 · F7水面调参 · F6水面对照 · F9帧率。");
   Debug.Log("HARBOR_V27_READY renderers="+ChangedRenderers+" importedMeshUses="+loadedMeshes);
   if(Environment.GetCommandLineArgs().Contains("-harbor-v27-check"))StartCoroutine(ReviewHarbor());
  }
  Material Make(Shader shader,string name,Color color){var m=new Material(shader){name=name,enableInstancing=true};m.SetColor("_BaseColor",color);owned.Add(m);return m;}
  void InitMaterials(){
   Shader lit=Resources.Load<Material>("SeaHarborV27/HarborShaderKeep").shader,reef=Resources.Load<Material>("SeaHarborV27/ReefShaderKeep").shader;
   wood=Make(lit,"V27_WeatheredWood",new Color(.80f,.73f,.62f));wood.SetTexture("_BaseMap",Resources.Load<Texture2D>("SeaHarborV27/Textures/WeatheredPierWood_V27"));wood.SetFloat("_Smoothness",.2f);
   bronze=Make(lit,"V27_AgedBronze",new Color(.19f,.18f,.15f));bronze.SetFloat("_Metallic",.78f);bronze.SetFloat("_Smoothness",.52f);
   rope=Make(lit,"V27_HempRope",new Color(.56f,.43f,.26f));
   glow=Make(Resources.Load<Material>("SeaHarborV27/GlowShaderKeep").shader,"V27_WarmGlass",new Color(1,.70f,.26f));
   corals=new Material[4];for(int i=0;i<4;i++){corals[i]=Make(reef,"V27_Coral_"+i,Color.white);corals[i].SetFloat("_PaletteBand",i);corals[i].SetTexture("_PaletteMap",Resources.Load<Texture2D>("SeaHarborV27/Textures/CoralPaletteLUT_V27"));corals[i].SetFloat("_CoralVibrancy",coralVibrancy);corals[i].SetFloat("_SwayAmplitude",.04f);}
   kelp=Make(reef,"V27_CleanGreenKelp",Color.white);kelp.SetFloat("_IsKelp",1);kelp.SetFloat("_SwayAmplitude",kelpSwayAmplitude);kelp.SetFloat("_SubsurfaceGlow",.42f);
  }
  public void ApplyAtmosphereState(bool enabled){
   v27AtmosphereEnabled=enabled;if(worldRoot!=null)worldRoot.gameObject.SetActive(enabled);if(boatRoot!=null)boatRoot.gameObject.SetActive(enabled);
   foreach(var restore in restores)restore(enabled);
   if(water!=null&&harborWaterShader!=null)water.shader=enabled?harborWaterShader:originalWaterShader;
   Shader.SetGlobalFloat("_HarborV27LightsOn",enabled?1:0);Shader.SetGlobalFloat("_BayReefLife",enabled?1:originalReefLife);
   previousVelocity=game!=null&&game.body!=null?game.body.velocity:Vector3.zero;
  }
  void LateUpdate(){
   if(!Ready)return;if(Input.GetKeyDown(KeyCode.F8))ApplyAtmosphereState(!v27AtmosphereEnabled);
   if(!v27AtmosphereEnabled||game.Paused||game.InRoom)return;
   float dt=Time.deltaTime;if(dt<=.0001f)return;clock+=dt;
   Vector3 velocity=game.body.velocity;Vector3 acceleration=Vector3.ClampMagnitude((velocity-previousVelocity)/dt,6);previousVelocity=velocity;
   Vector3 local=game.boat.InverseTransformDirection(acceleration);
   Vector2 target=new Vector2(Mathf.Clamp(-local.z*2.8f+Mathf.Sin(clock*2.6f)*5,-22,22),Mathf.Clamp(local.x*2.8f+Mathf.Cos(clock*2.1f)*4,-22,22));
   swing=Vector2.SmoothDamp(swing,target,ref swingVelocity,.24f,80,dt);boatPivot.localRotation=Quaternion.Euler(swing.x,0,swing.y);
   pierPivot.localRotation=Quaternion.Euler(Mathf.Sin(clock*1.65f)*3.5f,0,Mathf.Cos(clock*1.35f)*2.8f);
   float flicker=.97f+.03f*Mathf.Sin(clock*9+Mathf.Cos(clock*4));boatLight.intensity=lanternLightIntensity*flicker;pierLight.intensity=lanternLightIntensity*1.15f*flicker;
   PublishLight("_HarborV27Boat",boatLight);PublishLight("_HarborV27Pier",pierLight);
   Shader.SetGlobalFloat("_HarborV27Time",clock);
   foreach(var m in corals)m.SetFloat("_CoralVibrancy",coralVibrancy);kelp.SetFloat("_SwayAmplitude",kelpSwayAmplitude);
  }
  void PublishLight(string key,Light light){var p=light.transform.position;Shader.SetGlobalVector(key,new Vector4(p.x,p.y,p.z,light.range));Shader.SetGlobalVector(key+"Color",(Vector4)(light.color*light.intensity));}
  static Transform Group(string name,Transform parent,Vector3 local){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=local;return t;}
  GameObject Box(string name,Transform parent,Vector3 p,Vector3 s,Quaternion q,Material mat){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;var c=go.GetComponent<Collider>();c.enabled=false;Destroy(c);go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=s;go.transform.localRotation=q;go.GetComponent<Renderer>().sharedMaterial=mat;return go;}
  void Box(string name,Transform parent,Vector3 p,Vector3 s,Material mat){Box(name,parent,p,s,Quaternion.identity,mat);}
  Mesh MeshAsset(string name){var m=Resources.Load<Mesh>("SeaHarborV27/Baked/"+name);if(m==null)throw new InvalidOperationException("Missing verified v27 mesh: "+name);loadedMeshes++;return m;}
  GameObject MeshObject(string name,Transform parent,Vector3 p,Mesh mesh,Material mat,Vector3 scale){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;return go;}
  void BuildBoat(){
   Crate(boatRoot,new Vector3(-.22f,.26f,.57f),new Vector3(.43f,.26f,.40f),8);Crate(boatRoot,new Vector3(.24f,.25f,.44f),new Vector3(.31f,.22f,.34f),-12);
   MeshObject("V27_CoiledHempRope",boatRoot,new Vector3(0,.30f,1.10f),Coil(),rope,Vector3.one);
   var p=new Vector3(0,.65f,-1.48f);Box("船尾灯架木立柱",boatRoot,p,new Vector3(.075f,.92f,.075f),wood);
   Box("船尾斜撑",boatRoot,p+new Vector3(0,.31f,-.10f),new Vector3(.05f,.32f,.05f),Quaternion.Euler(38,0,0),wood);
   Box("船尾横挑铁臂",boatRoot,p+new Vector3(0,.44f,-.18f),new Vector3(.045f,.045f,.42f),bronze);
   boatPivot=Group("V27_BoatSternLanternPivot",boatRoot,p+new Vector3(0,.42f,-.34f));boatLight=Lantern(boatPivot,.88f,6.5f,lanternLightIntensity);
  }
  void Crate(Transform parent,Vector3 p,Vector3 size,float yaw){var t=Group("V27_SlattedCatchCrate",parent,p);t.localRotation=Quaternion.Euler(0,yaw,0);Box("箱体",t,Vector3.zero,size,wood);foreach(float x in new[]{-.38f,.38f})Box("窄木压条",t,new Vector3(size.x*x,0,0),new Vector3(.025f,size.y+.02f,size.z+.02f),wood);}
  Light Lantern(Transform pivot,float scale,float range,float intensity){
   MeshObject("V27_LanternOpenCage",pivot,Vector3.zero,MeshAsset("Lantern_V27_Hanging"),bronze,Vector3.one*scale);
   var core=GameObject.CreatePrimitive(PrimitiveType.Cylinder);core.name="V27_LanternWarmCore";var c=core.GetComponent<Collider>();c.enabled=false;Destroy(c);core.transform.SetParent(pivot,false);core.transform.localPosition=new Vector3(0,-.24f*scale,0);core.transform.localScale=new Vector3(.14f,.10f,.14f)*scale;core.GetComponent<Renderer>().sharedMaterial=glow;
   var l=Group("V27_LanternWarmLight",pivot,new Vector3(0,-.25f*scale,0)).gameObject.AddComponent<Light>();l.type=LightType.Point;l.range=range;l.color=new Color(1,.63f,.24f);l.intensity=intensity;l.shadows=LightShadows.None;return l;
  }
  void BuildPier(){
   // v16 accepted pier is a combined mesh. These anchors are its authored post centres,
   // not the incompatible v8 blockout coordinates from the supplied draft.
   for(int side=-1;side<=1;side+=2){var verts=new List<Vector3>();var tris=new List<int>();
    for(int i=0;i<6;i++){if(side==1&&i==5)continue;Vector3 a=PierPost(side,i),b=PierPost(side,i+1);for(int k=0;k<8;k++){float t0=k/8f,t1=(k+1)/8f;Tube(verts,tris,Vector3.Lerp(a,b,t0)-Vector3.up*Mathf.Sin(t0*Mathf.PI)*.16f,Vector3.Lerp(a,b,t1)-Vector3.up*Mathf.Sin(t1*Mathf.PI)*.16f,.022f,6);}}
    MeshObject("V27_CatenaryRope_"+side,worldRoot,Vector3.zero,FinishMesh(verts,tris,"V27_RopeRail"),rope,Vector3.one);
   }
   RemoveCoveredLegacyLamp();
   var head=Group("V27_PierHeadLanternAndCargo",worldRoot,new Vector3(-2.4f,.74f,3.68f));
   Box("栈桥高挑灯柱",head,new Vector3(0,1.15f,0),new Vector3(.12f,2.3f,.12f),wood);Box("灯柱横梁",head,new Vector3(.28f,2.18f,0),new Vector3(.68f,.09f,.09f),wood);Box("灯柱斜撑",head,new Vector3(.18f,1.92f,0),new Vector3(.07f,.48f,.07f),Quaternion.Euler(0,0,-42),wood);
   pierPivot=Group("V27_PierHangingLanternPivot",head,new Vector3(.54f,2.14f,0));pierLight=Lantern(pierPivot,1.05f,8.5f,lanternLightIntensity*1.15f);
   // Place the cluster beside the south rail; retain the centre and boarding opening.
   MeshObject("V27_PierBarrelsAndCrates",worldRoot,new Vector3(-5.7f,.76f,1.55f),MeshAsset("Pier_V27_BarrelsAndCrates"),wood,Vector3.one*.67f);
  }
  static Vector3 PierPost(int side,int i){return new Vector3(-16.7f+i*2.45f,1.13f,2.4f+side*1.36f+.35f*Mathf.Sin(i/6f*Mathf.PI));}
  void RemoveCoveredLegacyLamp(){
   foreach(var mf in game.world.GetComponentsInChildren<MeshFilter>()){
    if(!mf.name.StartsWith("EN_Pier_"))continue;var original=mf.sharedMesh;if(original==null)continue;
    var renderer = mf.GetComponent<MeshRenderer>();
    var craft = CoastalCraftsmanshipUpgradeV32.For(this);
    if (renderer != null && craft != null && craft.TryPierMaterials(renderer, out var materials))
     SwapSlots(renderer,materials);
    var vertices=original.vertices;var mesh=Instantiate(original);bool changed=false;
    for(int sub=0;sub<original.subMeshCount;sub++){var src=original.GetTriangles(sub);var dst=new List<int>();for(int i=0;i<src.Length;i+=3){bool inside=true,high=false;for(int k=0;k<3;k++){var v=mf.transform.TransformPoint(vertices[src[i+k]]);inside&=v.x> -2.62f&&v.x< -1.60f&&v.z>3.48f&&v.z<3.90f;high|=v.y>1.40f;}if(inside&&high){changed=true;continue;}dst.Add(src[i]);dst.Add(src[i+1]);dst.Add(src[i+2]);}mesh.SetTriangles(dst,sub);}
    if(changed){mesh.RecalculateBounds();owned.Add(mesh);var filter=mf;originalMeshes[filter]=original;restores.Add(on=>{if(filter!=null)filter.sharedMesh=on?mesh:original;});}else Destroy(mesh);
   }
  }
  void BuildGallery(){
   var front=game.exterior.Find("室外活动/海洋小馆 · 码头入口");if(front==null)return;
   var craft = CoastalCraftsmanshipUpgradeV32.For(this);
   foreach(var r in front.GetComponentsInChildren<MeshRenderer>())
   {
    bool timber = r.name == "廊柱" || r.name.Contains("廊架") || r.name == "小馆木平台";
    if (!timber && (craft == null || r.name != "低矮栏墙")) continue;
    var crafted = craft?.GalleryMesh(r);
    if (crafted == null) { if (timber) Swap(r,wood); continue; }
    var filter = r.GetComponent<MeshFilter>();
    var original = filter.sharedMesh;
    originalMeshes[filter] = original;
    restores.Add(on => { if (filter != null) filter.sharedMesh = on ? crafted : original; });
    Swap(r,craft.GalleryMaterial(r.name));
   }
   var roof=Group("V27_GalleryTimberEaves",worldRoot,Vector3.zero);roof.position=front.position;
   for (int side=-1;side<=1;side+=2)
   {
    var eave = Box("斜屋檐",roof,new Vector3(side*1.24f,3.16f,0),new Vector3(2.6f,.10f,4.15f),Quaternion.Euler(0,0,-side*12),wood);
    craft?.GalleryEave(eave);
   }
  }
  void Swap(Renderer renderer,Material material) => SwapSlots(renderer,renderer.sharedMaterials.Select(_=>material).ToArray());
  void SwapSlots(Renderer renderer,Material[] next)
  {
   var original = renderer.sharedMaterials;
   originalMaterials[renderer] = original;
   restores.Add(on => { if (renderer != null) renderer.sharedMaterials = on ? next : original; });
   ChangedRenderers++;
  }
  int Band(string name){if(name.Contains("Gold"))return 3;if(name.Contains("Mint"))return 2;if(name.Contains("Salmon")||name.Contains("Orange"))return 1;return 0;}
  void UpgradeReef(){
   foreach(var r in game.world.GetComponentsInChildren<MeshRenderer>(true)){
    if(r.transform.IsChildOf(worldRoot)||r.sharedMaterial==null)continue;string shader=r.sharedMaterial.shader.name,n=r.name;
    if(n.StartsWith("EN_Coral_")||n.Contains("珊瑚群落")||shader.Contains("ReefCoral"))Swap(r,corals[Band(n)]);
    else if(n.StartsWith("EN_Kelp_")||n.Contains("礁台海带")||shader.Contains("ReefFoliage"))Swap(r,kelp);
   }
   // Reflection is one-time only and preserves the private instanced groups for exact F8 restore.
   foreach(var canopy in FindObjectsOfType<CoastReefCanopy>()){
    var field=typeof(CoastReefCanopy).GetField("groups",BindingFlags.Instance|BindingFlags.NonPublic);var groups=field?.GetValue(canopy) as IList;if(groups==null)continue;
    foreach(object g in groups){var f=g.GetType().GetField("material");var old=f?.GetValue(g) as Material;if(old==null||!old.name.Contains("Coral"))continue;object target=g;restores.Add(on=>f.SetValue(target,on?corals[0]:old));}
   }
   var coralMesh=MeshAsset("Coral_V27_Cluster");var kelpMesh=MeshAsset("Kelp_V27_GiantFrond");
   var spots=new[]{new Vector2(4.5f,-4.6f),new Vector2(5.1f,-3.8f),new Vector2(4.3f,-3.3f),new Vector2(5.7f,2.4f),new Vector2(6.3f,3.1f),new Vector2(5.5f,3.7f)};
   int i=0;foreach(var spot in spots){Vector3 p;Vector3 normal;if(!VisibleBed(spot,out p,out normal))continue;
    float budget=SeaLifeRuntime.Sea-.32f-p.y;if(budget<.35f)continue;float size=Mathf.Min(1.25f+(i%3)*.25f,budget/Mathf.Max(.1f,coralMesh.bounds.max.y));
    var c=MeshObject("V27_ShowcaseCoralCluster_"+i,worldRoot,p-normal*.025f,coralMesh,corals[i%4],Vector3.one*size);c.transform.rotation=Quaternion.Euler(0,i*67,0);
    Vector3 kp,kn;if(VisibleBed(spot+new Vector2(.45f,-.35f),out kp,out kn)){
     float h=Mathf.Min(1.35f+(i%3)*.17f,SeaLifeRuntime.Sea-.35f-kp.y);if(h>.4f){var k=MeshObject("V27_ShowcaseGiantKelp_"+i,worldRoot,kp-kn*.025f,kelpMesh,kelp,new Vector3(1.1f,h/kelpMesh.bounds.size.y,1.1f));k.transform.localRotation=Quaternion.Euler(0,i*113,0);}
    }i++;
   }
  }
  // Sample the visible shore mesh, including overlays that are deliberately non-colliding.
  // A raycast against physics alone can leave new plants hovering above the final art.
  bool VisibleBed(Vector2 xz,out Vector3 point,out Vector3 normal){
   point=new Vector3(xz.x,-100,xz.y);normal=Vector3.up;
   foreach(var f in game.world.GetComponentsInChildren<MeshFilter>()){
    if(f.sharedMesh==null||f.transform.IsChildOf(worldRoot))continue;
    string name=f.name;
    if(name=="ShowcaseWater"||name.Contains("Coral")||name.Contains("Kelp")||name.Contains("Pier")||name.Contains("Foam")||name.Contains("Leaves")||name.Contains("Bark")||name.Contains("Shadow")||name.Contains("Fish")||name.Contains("Tower")||name.Contains("Hero"))continue;
    var renderer=f.GetComponent<Renderer>();if(renderer==null||!renderer.enabled)continue;
    var verts=f.sharedMesh.vertices;var tr=f.sharedMesh.triangles;
    for(int i=0;i<tr.Length;i+=3){Vector3 a=f.transform.TransformPoint(verts[tr[i]]),b=f.transform.TransformPoint(verts[tr[i+1]]),c=f.transform.TransformPoint(verts[tr[i+2]]);
     float d=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(d)<.000001f)continue;
     float u=((b.z-c.z)*(xz.x-c.x)+(c.x-b.x)*(xz.y-c.z))/d,v=((c.z-a.z)*(xz.x-c.x)+(a.x-c.x)*(xz.y-c.z))/d,w=1-u-v;if(u<0||v<0||w<0)continue;
     float y=u*a.y+v*b.y+w*c.y;if(y>point.y&&y<SeaLifeRuntime.Sea-.35f){point.y=y;normal=Vector3.Cross(b-a,c-a).normalized;if(normal.y<0)normal=-normal;}
    }
   }return point.y> -6.5f&&normal.y>.55f;
  }
  Mesh Coil(){var v=new List<Vector3>();var t=new List<int>();for(int i=0;i<96;i++){float a=i/96f*Mathf.PI*5,b=(i+1)/96f*Mathf.PI*5,ra=Mathf.Lerp(.07f,.23f,i/96f),rb=Mathf.Lerp(.07f,.23f,(i+1)/96f);Tube(v,t,new Vector3(Mathf.Cos(a)*ra,.012f,Mathf.Sin(a)*ra),new Vector3(Mathf.Cos(b)*rb,.012f,Mathf.Sin(b)*rb),.015f,6);}return FinishMesh(v,t,"V27_CoiledRope");}
  Mesh FinishMesh(List<Vector3> v,List<int> t,string name){var m=new Mesh{name=name};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();owned.Add(m);return m;}
  static void Tube(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,float radius,int sides){var axis=(b-a).normalized;var right=Vector3.Cross(axis,Vector3.up);if(right.sqrMagnitude<.001f)right=Vector3.Cross(axis,Vector3.right);right.Normalize();var up=Vector3.Cross(axis,right);int start=v.Count;for(int ring=0;ring<2;ring++)for(int i=0;i<sides;i++){float angle=i*Mathf.PI*2/sides;v.Add((ring==0?a:b)+(right*Mathf.Cos(angle)+up*Mathf.Sin(angle))*radius);}for(int i=0;i<sides;i++){int a0=start+i,a1=start+(i+1)%sides,b0=a0+sides,b1=a1+sides;t.AddRange(new[]{a0,a1,b0,a1,b1,b0});}}
  void OnDestroy(){ApplyAtmosphereState(false);if(worldRoot!=null)Destroy(worldRoot.gameObject);if(boatRoot!=null)Destroy(boatRoot.gameObject);foreach(var o in owned)if(o!=null)Destroy(o);if(Active==this)Active=null;}
 }
}
