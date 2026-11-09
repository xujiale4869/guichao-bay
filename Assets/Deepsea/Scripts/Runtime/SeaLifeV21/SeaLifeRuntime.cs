using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Deepsea {
 [DefaultExecutionOrder(300)]
 public sealed partial class SeaLifeRuntime:MonoBehaviour {
  public const float Sea=-.22f;
  public static readonly string[] FishNames={"银纹鱼","蓝鳍鱼","金背鱼"};
  public SeaLifeSave save;public SeaLifeStore store;public SeaLifeUI ui;
  public Camera view;public Transform boat,walker,world,exterior,room,captain;public CharacterController controller;public Rigidbody body;
  public bool OnFoot,InRoom,Paused,Review,Ready;public bool AutoInput;public Vector2 DriveInput;
  public Vector3 dockBerth=new Vector3(.2f,Sea,4.7f),dockFeet=new Vector3(-3.6f,.8f,3.1f),roomOrigin=new Vector3(90,2,0);
  public Vector3 tideCenter=new Vector3(-7.8f,.32f,6.8f),cratePoint=new Vector3(8.5f,-1.3f,9.5f);
  public string Notice="",Hint="";public float NoticeTime;public int SelectedTool,SelectedTank,Panel;public bool CloseView,BuildMode;
  public Vector3 ActivePosition{get{return OnFoot?walker.position:boat.position;}}
  public int CarryCount{get{return save.fish.Count(f=>f.tank<0);}}
  public float Speed{get{return body==null?0:body.velocity.magnitude;}}
  public bool Busy{get{return fishingStage>0||salvageStage>0||(GameplayV34!=null&&GameplayV34.Towing);}}
  public bool UiBlocking{get{return Panel!=0||Paused||WorkshopOpenV34||SeaBayExpansionAndBuildSpaceV28.CapturesInput;}}
  readonly List<Material> ownedMaterials=new List<Material>();readonly List<Collider> ground=new List<Collider>();
  Material wood,stone,metal,rope,glass,waterTint,cream,leafMat,fishSilver;Material[] fishMaterials;Mesh[] fishMeshes;
  CoastHeroRig captainRig,walkerRig;Transform actorRoot,rod,rodTip,bobber,fishTarget,crate,hook;LineRenderer fishingLine,towLine;
  readonly List<Transform> shellObjects=new List<Transform>();Vector3 cameraFocus;float cameraSize=14,clock,vertical;Quaternion cameraRotation=Quaternion.Euler(42,-18,0);Light sun;AudioSource audioSource;AudioClip clickSound,waterSound;
  readonly Vector3[] fishSpots={new Vector3(3,Sea,7),new Vector3(12,Sea,0),new Vector3(7,Sea,20)};
  readonly string[] areaNames={"海带湾","浅礁水道","湾口清水"};
  string Evidence{get{return "Verification/sea-life-v21-delivery";}}
  void Awake(){Review=Environment.GetCommandLineArgs().Contains("-sea-life-check");Time.timeScale=1;if(Review){review=new ReviewReport();Application.logMessageReceived+=ReviewLog;StartCoroutine(ReviewDeadline());}}
  IEnumerator Start(){
   var pipeline=Resources.Load<UniversalRenderPipelineAsset>("CoastRefinement/SamplePipeline");if(pipeline!=null){GraphicsSettings.renderPipelineAsset=pipeline;QualitySettings.renderPipeline=pipeline;}
   QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
   world=GameObject.Find("EN_MistCapeShowcase").transform;exterior=new GameObject("海岬生活 · 室外").transform;world.SetParent(exterior,true);
   var oldCamera=GameObject.Find("ShowcaseCamera");view=oldCamera==null?new GameObject("SeaLifeCamera").AddComponent<Camera>():oldCamera.GetComponent<Camera>();view.tag="MainCamera";view.allowHDR=true;view.nearClipPlane=.1f;view.farClipPlane=350;
   var add=view.GetUniversalAdditionalCameraData();add.requiresColorTexture=true;add.requiresDepthTexture=true;add.renderPostProcessing=true;
   if(view.GetComponent<AudioListener>()==null)view.gameObject.AddComponent<AudioListener>();
   sun=GameObject.Find("ShowcaseSun").GetComponent<Light>();sun.transform.rotation=Quaternion.Euler(38,-42,0);sun.intensity=2.4f;sun.color=new Color(1,.94f,.82f);sun.shadows=LightShadows.Soft;
   var look=GameObject.Find("ShowcaseLook");if(look!=null){var volume=look.GetComponent<Volume>();volume.isGlobal=true;volume.priority=200;volume.sharedProfile=Resources.Load<VolumeProfile>("CoastRefinement/Look");}
   RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.23f,.34f,.43f);RenderSettings.ambientEquatorColor=new Color(.16f,.23f,.24f);RenderSettings.ambientGroundColor=new Color(.09f,.12f,.12f);RenderSettings.fog=false;
   var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(new Color(.15f,.20f,.23f));RenderSettings.ambientProbe=probe;
   Shader.SetGlobalFloat("_CoastShelfEnabled",0);Shader.SetGlobalFloat("_CoastWaterDetails",1);Shader.SetGlobalFloat("_CoastWaterDepthProbe",0);Shader.SetGlobalFloat("_ShowcaseClay",0);
   var sea=world.Find("ShowcaseWater");sea.GetComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>("CoastFineShore/WaterStudy");
   SetupMaterials();SetupCollision();SetupBoat();SetupShore();SetupFishing();SetupAquarium();SetupAmbientFish();SetupAudio();
   store=new SeaLifeStore(SeaBayExpansionAndBuildSpaceV28.PlayerSavePath(Review));save=Review?new SeaLifeSave():store.Load();save.Normalize();
   if(!Review&&SeaBayExpansionAndBuildSpaceV28.ValidBoatPosition(save.boatX,save.boatZ))PlaceBoat(new Vector3(save.boatX,Sea,save.boatZ),save.boatYaw);else PlaceBoat(new Vector3(.8f,Sea,5.2f),100);
   RefreshCollection();RefreshShells();cameraFocus=boat.position+Vector3.up;ApplyCamera(true);ui=gameObject.AddComponent<SeaLifeUI>();ui.Initialize(this);Ready=true;
   Notify("欢迎来到红枫海岬。WASD开船，1钓竿，2挂钩；靠码头F下船，E查看身边事物。");
   Debug.Log("SEA_LIFE_READY independentSave="+store.PathName+" oldGameDirectorLoaded="+(FindObjectOfType<GameDirector>()!=null));
   yield return null;if(Review)StartCoroutine(RunReview());
  }
  void SetupMaterials(){
   wood=Clone(Resources.Load<Material>("CoastRefinement/Wood"));stone=Clone(Resources.Load<Material>("CoastMaple/Limestone"));
   metal=Lit("深色铁",new Color(.16f,.24f,.27f),.65f);rope=Lit("麻绳",new Color(.44f,.33f,.19f));cream=Lit("贝壳乳白",new Color(.88f,.78f,.58f));leafMat=Lit("干净绿海带",new Color(.13f,.38f,.22f));
   glass=Clone(Resources.Load<Material>("SeaLifeV21/Glass"));waterTint=Clone(glass);waterTint.color=new Color(.15f,.62f,.65f,.08f);
   fishMaterials=new[]{Lit("银纹鱼材质",new Color(.58f,.78f,.79f),.35f),Lit("蓝鳍鱼材质",new Color(.12f,.40f,.58f),.2f),Lit("金背鱼材质",new Color(.82f,.55f,.18f),.25f)};
   fishMeshes=new[]{MakeFish(0),MakeFish(1),MakeFish(2)};fishSilver=Resources.Load<Material>("CoastRefinement/Silver");
  }
  Material Clone(Material source){if(source==null)throw new Exception("Sea life material missing");var m=new Material(source);ownedMaterials.Add(m);return m;}
  Material Lit(string name,Color color,float metallic=0){var m=new Material(Shader.Find("Deepsea/SeaLifeSurface")){name=name};m.color=color;m.SetFloat("_Smoothness",metallic>0?.55f:.24f);m.SetFloat("_Metallic",metallic);ownedMaterials.Add(m);return m;}
  public Transform Group(string name,Transform parent,Vector3 position){var g=new GameObject(name).transform;g.SetParent(parent,false);g.localPosition=position;return g;}
  public Transform Primitive(string name,PrimitiveType type,Transform parent,Vector3 p,Vector3 scale,Material mat,bool collision=false){var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collision)Destroy(g.GetComponent<Collider>());else if(type==PrimitiveType.Sphere){Destroy(g.GetComponent<Collider>());g.AddComponent<MeshCollider>().sharedMesh=g.GetComponent<MeshFilter>().sharedMesh;}return g.transform;}
  public Transform Box(string name,Transform parent,Vector3 p,Vector3 scale,Material mat,bool collision=false){return Primitive(name,PrimitiveType.Cube,parent,p,scale,mat,collision);}
  public LineRenderer Line(string name,Transform parent,Material mat,float width){var g=new GameObject(name);g.transform.SetParent(parent,false);var l=g.AddComponent<LineRenderer>();l.sharedMaterial=mat;l.positionCount=2;l.startWidth=l.endWidth=width;l.useWorldSpace=true;l.shadowCastingMode=ShadowCastingMode.Off;return l;}
  void SetupCollision(){foreach(var f in world.GetComponentsInChildren<MeshFilter>(true)){var n=f.name;if(n.Contains("LOD1")||n.Contains("Leaves")||n.Contains("Bark")||n.Contains("Shadow")||n.Contains("Kelp")||n.Contains("Coral")||n=="ShowcaseWater"||n.Contains("Hero"))continue;if(n.Contains("Maple")||n.Contains("FineShore")||n.Contains("Pier_Wood")||n.Contains("Land_Rock")){var c=f.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;ground.Add(c);}}}
  void SetupBoat(){
   actorRoot=Group("室外活动",exterior,Vector3.zero);boat=world.Find("HeroBoat");boat.SetParent(actorRoot,true);boat.localScale=Vector3.one*1.25f;body=boat.gameObject.AddComponent<Rigidbody>();body.useGravity=false;body.mass=8;body.drag=.8f;body.angularDrag=3;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
   var hull=boat.gameObject.AddComponent<BoxCollider>();hull.center=new Vector3(0,.4f,0);hull.size=new Vector3(1.7f,.9f,3.9f);
   captain=boat.GetComponentsInChildren<CoastHeroRig>(true).First().transform;captainRig=captain.GetComponent<CoastHeroRig>();captainRig.seated=true;
   walker=Group("同一位渔人 · 步行",null,dockFeet);controller=walker.gameObject.AddComponent<CharacterController>();controller.height=1.7f;controller.radius=.24f;controller.center=new Vector3(0,.85f,0);controller.stepOffset=.35f;controller.slopeLimit=50;controller.minMoveDistance=0;
   var model=Instantiate(Resources.Load<GameObject>("CoastHero/Fisherman"),walker,false);model.transform.localScale=Vector3.one*1.3f;walkerRig=model.GetComponent<CoastHeroRig>();walkerRig.seated=false;walker.gameObject.SetActive(false);
  }
  public void PlaceBoat(Vector3 p,float yaw){body.position=p;body.rotation=Quaternion.Euler(0,yaw,0);boat.SetPositionAndRotation(p,body.rotation);if(!body.isKinematic){body.velocity=Vector3.zero;body.angularVelocity=Vector3.zero;}}
  public void PlaceWalker(Vector3 p){controller.enabled=false;walker.position=p;vertical=0;controller.enabled=true;}
  void Update(){if(!Ready)return;if(SeaBayExpansionAndBuildSpaceV28.CapturesInput){if(ui!=null)ui.Refresh();return;}clock+=Time.deltaTime;if(Input.GetKeyDown(KeyCode.Escape)){if(WorkshopOpenV34){GameplayV34.CloseWorkshop();return;}if(fishingStage==5){Notify("请选择收进活鱼箱，或当场放流。");}else if(Panel!=0){Panel=0;CloseView=false;BuildMode=false;ui.RebuildPanel();}else if(Busy)CancelActivity();else{Paused=!Paused;Time.timeScale=Paused?0:1;ui.RebuildPanel();}}
   if(Paused)return;
   if(!UiBlocking){if(GameplayV34!=null&&!Busy&&!InRoom&&Input.GetKeyDown(KeyCode.T))GameplayV34.CycleWatch();if(Input.GetKeyDown(KeyCode.Alpha1))SelectTool(0);if(Input.GetKeyDown(KeyCode.Alpha2))SelectTool(1);if(Input.GetKeyDown(KeyCode.F)&&!Busy)ToggleBoard();if(Input.GetKeyDown(KeyCode.E))Interact();if(Input.GetKeyDown(KeyCode.B)&&InRoom)OpenPanel(1);if(Input.GetKeyDown(KeyCode.Tab))OpenPanel(2);if(Input.GetKeyDown(KeyCode.V)&&InRoom)ToggleCloseView();}
   if(OnFoot&&!UiBlocking&&!Busy&&!CloseView)MoveWalker();else walkerRig.speed=0;
   GameplayV34?.Tick();UpdateFishing();UpdateSalvage();UpdateHint();if(ui!=null)ui.Refresh();
  }
  void FixedUpdate(){if(DriveNavigationV23())return;if(!Ready||Paused||OnFoot||Busy||UiBlocking)return;var input=AutoInput?DriveInput:new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));float along=Vector3.Dot(body.velocity,boat.forward),target=input.y>=0?input.y*4.5f*ForwardSpeedV34:input.y*1.8f;body.AddForce(boat.forward*Mathf.Clamp((target-along)*1.6f,-2.7f,2.7f),ForceMode.Acceleration);body.AddForce(-(body.velocity-boat.forward*along)*3,ForceMode.Acceleration);float steer=Mathf.Clamp01(Mathf.Abs(along)/1.5f)*(along<-.1f?-1:1);body.MoveRotation(body.rotation*Quaternion.Euler(0,input.x*55*steer*Time.fixedDeltaTime,0));SeaBayExpansionAndBuildSpaceV28.ClampBoat(body);}
  void MoveWalker(){var input=AutoInput?DriveInput:new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));var q=Quaternion.Euler(0,-18,0);var d=q*(new Vector3(input.x,0,input.y));d=Vector3.ClampMagnitude(d,1);vertical=controller.isGrounded?-1:Mathf.Max(-16,vertical-18*Time.deltaTime);controller.Move((d*2.7f+Vector3.up*vertical)*Time.deltaTime);walkerRig.speed=d.magnitude*2.7f;if(d.sqrMagnitude>.01f)walkerRig.transform.rotation=Quaternion.Slerp(walkerRig.transform.rotation,Quaternion.LookRotation(d),Time.deltaTime*10);if(walker.position.y<(InRoom?roomOrigin.y-3:-1.5f))PlaceWalker(InRoom?roomOrigin+new Vector3(5,0,-3):Grounded(dockFeet));}
  public Vector3 Grounded(Vector3 p){RaycastHit h;if(Physics.Raycast(new Vector3(p.x,30,p.z),Vector3.down,out h,60))p.y=h.point.y+.04f;return p;}
  void LateUpdate(){if(!Ready)return;if(SeaBayExpansionAndBuildSpaceV28.Active==null||!SeaBayExpansionAndBuildSpaceV28.Active.godBuildMode)ApplyCamera(false);if(fishingStage>0)PoseFishing();Shader.SetGlobalVector("_CoastBoat",new Vector4(boat.position.x,boat.position.z,.7f,1));Shader.SetGlobalVector("_CoastBoatHeading",new Vector4(boat.forward.x,boat.forward.z,0,0));}
  void ApplyCamera(bool snap){var focus=InRoom?roomOrigin+new Vector3(0,1,0):ActivePosition+Vector3.up*.8f;float wanted=InRoom?8.7f:OnFoot?8.5f:fishingStage>0?7.2f:12.5f;wanted=SeaSeamlessWorldAndNauticalUIV29.SailingSize(this,wanted);if(CloseView&&InRoom){focus=TankPosition(SelectedTank)+new Vector3(0,1.8f,0);wanted=3.2f;}
   cameraFocus=snap?focus:Vector3.Lerp(cameraFocus,focus,1-Mathf.Exp(-5*Time.unscaledDeltaTime));cameraSize=Mathf.Lerp(cameraSize,wanted,1-Mathf.Exp(-4*Time.unscaledDeltaTime));view.orthographic=false;view.fieldOfView=2*Mathf.Atan(cameraSize/104)*Mathf.Rad2Deg;view.transform.SetPositionAndRotation(cameraFocus-cameraRotation*Vector3.forward*104,cameraRotation);}
  public void SelectTool(int tool){if(Busy)return;SelectedTool=tool;Panel=0;ui.RebuildPanel();Notify(tool==0?"已选择钓竿。到鱼影附近，停船按E。":(GameplayV34==null?"已选择挂钩。靠近浅海旧箱，停船按E。":"已选择挂钩。靠近漂流物并减速，按 E 打捞。"));}
  public void Interact(){if(UiBlocking)return;if(fishingStage==3){StartReeling();return;}if(Busy)return;
   if(InRoom){int near=NearestTank();if(near>=0){SelectedTank=near;OpenPanel(save.tanks[near].built?2:1);}else if(Vector3.Distance(walker.position,roomOrigin+new Vector3(5,0,-3.8f))<2.7f)LeaveRoom();return;}
   if(OnFoot){int shell=NearestShell();if(shell>=0){PickupShell(shell);return;}if(Vector3.Distance(walker.position,galleryDoor)<2.4f){EnterRoom();return;}if(GameplayV34!=null&&GameplayV34.TryOpenWorkshop())return;OpenPanel(2);return;}
   if(Speed>1.2f){Notify("先松开推进，减速停稳。");return;}if(GameplayV34!=null&&GameplayV34.TrySeaInteraction())return;if(SelectedTool==1){BeginSalvage();return;}BeginFishing();
  }
  public void ToggleBoard(){if(Busy||Paused)return;Panel=0;if(ui!=null)ui.RebuildPanel();if(InRoom){LeaveRoom();return;}if(OnFoot){if(Vector3.Distance(walker.position,dockFeet)>3.8f){Notify("回到木栈桥尽头，再按F登船。");return;}OnFoot=false;walker.gameObject.SetActive(false);captain.gameObject.SetActive(true);body.isKinematic=false;Notify("已登船。WASD开船，1钓竿，2挂钩。");SaveNow();return;}if(Vector3.Distance(boat.position,dockBerth)>5.5f||Speed>1.4f){Notify("靠近木栈桥尽头并减速，按F下船。");return;}body.velocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;PlaceBoat(dockBerth,90);OnFoot=true;captain.gameObject.SetActive(false);walker.gameObject.SetActive(true);PlaceWalker(Grounded(dockFeet));Notify("已下船。沿栈桥走向潮池，或到海洋小馆门牌旁按E。");SaveNow();}
  public void OpenPanel(int panel){if(Busy)return;Panel=panel;if(InRoom&&panel==1)BuildMode=true;ui.RebuildPanel();}
  public void ToggleCloseView(){if(!InRoom||!save.tanks[SelectedTank].built)return;CloseView=!CloseView;Panel=0;BuildMode=false;ui.RebuildPanel();}
  void UpdateHint(){if(InRoom){var n=NearestTank();Hint=CloseView?"V 返回房间视角":n>=0?"E 管理身边的展缸 · B 建设 · V 近景观赏":"WASD走动 · B建设 · Tab鱼箱 · 到门口F返回码头";return;}if(OnFoot){var workshop=GameplayV34?.Hint();Hint=NearestShell()>=0?"E 拾取脚边贝壳":Vector3.Distance(walker.position,galleryDoor)<2.4f?"E 进入海洋小馆":!string.IsNullOrEmpty(workshop)?workshop:Vector3.Distance(walker.position,dockFeet)<3.8f?"F 登船 · WASD 沿栈桥走动":"WASD 步行 · 沿栈桥找潮池与小馆门牌";return;}var seaHint=!Busy||(GameplayV34!=null&&GameplayV34.Towing)?GameplayV34?.Hint():null;Hint=!string.IsNullOrEmpty(seaHint)?seaHint:fishingStage>0?FishingHint():salvageStage>0?"绞盘正在收起旧箱 · Esc取消":Vector3.Distance(boat.position,dockBerth)<5.5f?"F下船 · E准备钓鱼 · 1钓竿 / 2挂钩":SelectedTool==0?"鱼影附近停船按E · WASD航行 · Tab活鱼箱":"浅海旧箱旁停船按E · 蓝色小浮标标出吊环";}
  public void Notify(string text){Notice=text;NoticeTime=Time.unscaledTime+7;}
  public void SaveNow(){if(save==null||store==null)return;SetSavedBoatPositionV34(save);if(!store.Save(save))Notify("保存失败，当前进度仍在运行中。");}
  public void SellCarried(){if(InRoom||Vector3.Distance(ActivePosition,dockFeet)>9){Notify("请回码头现场出售。");return;}int total=0;foreach(var f in save.fish.Where(f=>f.tank<0))total+=SaleValueV34(f);if(GameplayV34!=null){
    if(!GameplayV34.Commit(s=>{s.fish.RemoveAll(f=>f.tank<0);s.coins+=total;}))return;
   }else{save.fish.RemoveAll(f=>f.tank<0);save.coins+=total;SaveNow();}Notify("已出售随身鱼，金币 +"+total);ui.RebuildPanel();RefreshCollection();}
  void SetupAudio(){audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=0;audioSource.volume=.25f;clickSound=Tone("轻木音",520,.09f);waterSound=Tone("水面提示",280,.16f);}
  AudioClip Tone(string name,float hz,float seconds){int n=(int)(22050*seconds);var v=new float[n];for(int i=0;i<n;i++)v[i]=Mathf.Sin(i*hz*Mathf.PI*2/22050)*Mathf.Exp(-i/(22050*seconds*.2f))*.3f;var clip=AudioClip.Create(name,n,1,22050,false);clip.SetData(v,0);return clip;}
  public void Cue(bool water=false){if(audioSource!=null)audioSource.PlayOneShot(water?waterSound:clickSound);}
  void OnApplicationQuit(){SaveNow();}
  void OnDestroy(){Time.timeScale=1;foreach(var m in ownedMaterials)if(m!=null)Destroy(m);}
 }
}
