using UnityEngine;
using UnityEngine.Rendering;

namespace Deepsea {
 public sealed partial class SeaLifeRuntime {
  internal SeaBayGameplayLoopV34 GameplayV34;
  internal bool WorkshopOpenV34=>GameplayV34!=null&&GameplayV34.Dialog>=0;
  public int MaxCarryCount=>6+(SeaBayGameplayLoopV34.Enabled?6*(save?.gameplayV34?.hold??0):0);
  internal float ForwardSpeedV34=>1+(SeaBayGameplayLoopV34.Enabled?.25f*(save?.gameplayV34?.motor??0):0);
  int pendingVariantV34;
  bool fastBiteV34;
  Transform hookJellyV34;
  readonly Material[] rareMaterialsV34=new Material[3];
  static readonly Color[] RareColorsV34={new Color(.35f,.76f,1),new Color(1,.66f,.19f),new Color(.34f,.92f,.69f)};
  public string PendingFishName=>pendingVariantV34>0?SeaBayGameplayLoopV34.RareNames[pendingVariantV34-1]:FishNames[pendingSpecies];
  public static string FishDisplayName(SeaLifeFish fish)=>fish.variantV34>0?SeaBayGameplayLoopV34.RareNames[fish.variantV34-1]:FishNames[fish.species];
  internal static int SaleValueV34(SeaLifeFish fish){
   int price=fish.species==0?8:fish.species==1?11:15;
   return fish.variantV34>0?price*2+6:price;
  }

  internal bool TrySaveGameplayV34(SeaLifeSave proposed){
   SetSavedBoatPositionV34(proposed);
   if(!store.Save(proposed))return false;
   save=proposed;return true;
  }
  void SetSavedBoatPositionV34(SeaLifeSave target){
   var p=SeaBayExpansionAndBuildSpaceV28.SavedBoatPosition(boat.position);
   target.boatX=p.x;target.boatZ=p.z;target.boatYaw=boat.eulerAngles.y;
  }
  internal Material MakeGameplayMaterialV34(string label,Color tint,float glow){
   var template=Resources.Load<Material>(glow>0?"SeaHarborV27/GlowShaderKeep":"SeaExpansionV28/SurfaceKeep");
   var material=new Material(template.shader){name=label};
   material.SetColor("_BaseColor",glow>0?tint*glow:tint);
   if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.4f);
   return material;
  }
  Material RareMaterialV34(int variant){
   int i=variant-1;
   if(rareMaterialsV34[i]!=null)return rareMaterialsV34[i];
   var material=MakeGameplayMaterialV34(SeaBayGameplayLoopV34.RareNames[i],RareColorsV34[i],1.4f);
   ownedMaterials.Add(material);rareMaterialsV34[i]=material;return material;
  }
  internal Transform CreateRareModelV34(Transform parent,int variant,float scale){
   var material=RareMaterialV34(variant);
   if(variant==1){
    var jelly=Group("月光水母",parent,Vector3.zero);jelly.localScale=Vector3.one*scale;
    Primitive("水母伞",PrimitiveType.Sphere,jelly,Vector3.up*.15f,new Vector3(.63f,.26f,.63f),material);
    for(int i=0;i<7;i++){
     float a=i*Mathf.PI*2/7;
     var arm=Primitive("水母垂须",PrimitiveType.Capsule,jelly,new Vector3(Mathf.Cos(a)*.21f,-.04f,Mathf.Sin(a)*.21f),new Vector3(.025f,.16f+(i%2)*.045f,.025f),material);
     arm.localRotation=Quaternion.Euler(Mathf.Sin(a)*14,0,Mathf.Cos(a)*14);
    }
    return jelly;
   }
   var model=MakeFishObject(SeaBayGameplayLoopV34.RareNames[variant-1],parent,variant==2?2:1,scale);
   model.GetComponent<Renderer>().sharedMaterial=material;
   return model;
  }
  void ApplyCatchModelV34(){
   var surface=fishTarget.GetComponent<Renderer>();
   surface.enabled=pendingVariantV34!=1;
   if(pendingVariantV34==1&&hookJellyV34==null)hookJellyV34=CreateRareModelV34(fishTarget,1,1);
   if(hookJellyV34!=null)hookJellyV34.gameObject.SetActive(pendingVariantV34==1);
   surface.sharedMaterial=pendingVariantV34>0?RareMaterialV34(pendingVariantV34):fishMaterials[pendingSpecies];
  }
  float BiteWaitV34=>castDuration+(fastBiteV34?.8f:2f);
  void CheckNightBiteV34(){
   if(GameplayV34==null||!GameplayV34.LitWater(castPoint))return;
   bool attracted=save.gameplayV34.bait>0||UnityEngine.Random.value<.72f;
   if(attracted)pendingVariantV34=1+save.gameplayV34.rareOrdinal;
  }
  internal void SetWatchLightingV34(int watch){
   Color[] sky={new Color(.30f,.30f,.38f),new Color(.23f,.34f,.43f),new Color(.34f,.22f,.30f),new Color(.055f,.11f,.23f)};
   Color[] equator={new Color(.22f,.23f,.26f),new Color(.16f,.23f,.24f),new Color(.24f,.14f,.22f),new Color(.03f,.057f,.13f)};
   Color[] groundAmbient={new Color(.11f,.12f,.15f),new Color(.09f,.12f,.12f),new Color(.13f,.07f,.11f),new Color(.017f,.03f,.077f)};
   Color[] ambient={new Color(.18f,.18f,.22f),new Color(.15f,.20f,.23f),new Color(.16f,.11f,.17f),new Color(.065f,.098f,.15f)};
   Color[] sunColor={new Color(1,.75f,.59f),new Color(1,.94f,.82f),new Color(1,.52f,.30f),new Color(.53f,.67f,1)};
   sun.transform.rotation=Quaternion.Euler(new[]{24f,38f,16f,54f}[watch],new[]{-64f,-42f,34f,38f}[watch],0);
   sun.intensity=new[]{1.65f,2.4f,1.20f,.30f}[watch];sun.color=sunColor[watch];
   SetWaterWatchV34(watch);
   RenderSettings.ambientSkyColor=sky[watch];RenderSettings.ambientEquatorColor=equator[watch];RenderSettings.ambientGroundColor=groundAmbient[watch];
   var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(ambient[watch]);RenderSettings.ambientProbe=probe;
   if(SeaCinematicLightingAndAtmosphereV37.Enabled)GetComponent<SeaCinematicLightingAndAtmosphereV37>()?.ApplyWatch(watch);
  }
  static readonly string[] WaterColorsV34={"_CrystalShoreColor","_Shallow","_ReefMidColor","_Deep","_SunPathColor"};
  static readonly string[] WaterDetailsV34={"_GlintIntensity","_FoamIntensity","_CausticsIntensity"};
  MaterialPropertyBlock waterWatchV34;
  void SetWaterWatchV34(int watch){
   var surface=world.Find("ShowcaseWater").GetComponent<Renderer>();
   if(watch==1){surface.SetPropertyBlock(null);return;}
   var material=surface.sharedMaterial;
   var tint=watch==0?new Color(.88f,.83f,.90f,1):watch==2?new Color(.82f,.70f,.75f,1):new Color(.40f,.50f,.75f,1);
   if(waterWatchV34==null)waterWatchV34=new MaterialPropertyBlock();
   waterWatchV34.Clear();
   foreach(string property in WaterColorsV34)
    if(material.HasProperty(property))waterWatchV34.SetColor(property,material.GetColor(property)*tint);
   float strength=watch==0?.82f:watch==2?.68f:.36f;
   foreach(string property in WaterDetailsV34)
    if(material.HasProperty(property))waterWatchV34.SetFloat(property,material.GetFloat(property)*strength);
   surface.SetPropertyBlock(waterWatchV34);
  }

 }
}
