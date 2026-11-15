using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea {
 public sealed partial class SeaBayExpansionAndBuildSpaceV28 {
  GameObject Part(string name,PrimitiveType type,Transform parent,Vector3 p,Vector3 size,Material mat){var g=GameObject.CreatePrimitive(type);g.name=name;var c=g.GetComponent<Collider>();c.enabled=false;Destroy(c);g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;var r=g.GetComponent<Renderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;return g;}
  void Box(string name,Transform parent,Vector3 p,Vector3 size,Material mat){Part(name,PrimitiveType.Cube,parent,p,size,mat);}
  void Rod(string name,Transform parent,Vector3 a,Vector3 b,float width,Material mat){var p=Part(name,PrimitiveType.Cylinder,parent,(a+b)*.5f,new Vector3(width,(b-a).magnitude*.5f,width),mat);p.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);}
  GameObject Structure(BayBuildableType type,bool preview,float depth){
   var g=new GameObject("V28_"+type);var t=g.transform;
   if(type==BayBuildableType.WoodenPierDeck){
    for(int i=0;i<8;i++)Box("WeatheredPlank",t,new Vector3(0,.97f,-.875f+i*.25f),new Vector3(1.98f,.11f,.235f),wood);
    for(int side=-1;side<=1;side+=2)Box("DeckBearer",t,new Vector3(side*.78f,.82f,0),new Vector3(.13f,.20f,1.99f),wood);
    foreach(float x in new[]{-.82f,.82f})foreach(float z in new[]{-.82f,.82f})Rod("SeabedPile",t,new Vector3(x,-depth-.18f,z),new Vector3(x,1.1f,z),.18f,wood);
    foreach(float x in new[]{-.82f,.82f})foreach(float z in new[]{-.82f,.82f})Box("IronPin",t,new Vector3(x,1.033f,z),new Vector3(.045f,.014f,.045f),bronze);
    var craft = CoastalCraftsmanshipUpgradeV32.For(this);
    if (craft != null)
    {
     int board = 0;
     foreach (Transform part in t)
      craft.PierPart(part.gameObject, part.name == "WeatheredPlank" ? board++ : 0);
    }
    if(!preview){var c=g.AddComponent<BoxCollider>();c.center=new Vector3(0,.96f,0);c.size=new Vector3(2,.18f,2);foreach(float x in new[]{-.82f,.82f})foreach(float z in new[]{-.82f,.82f}){c=g.AddComponent<BoxCollider>();c.center=new Vector3(x,(1-depth)*.5f,z);c.size=new Vector3(.18f,1+depth,.18f);}}
   }else if(type==BayBuildableType.PierLanternPost){
    Rod("GroundedLampPile",t,new Vector3(0,-depth-.16f,0),new Vector3(0,2.8f,0),.16f,wood);Box("LampArm",t,new Vector3(.27f,2.76f,0),new Vector3(.70f,.10f,.10f),wood);Rod("DiagonalBrace",t,new Vector3(.03f,2.28f,0),new Vector3(.40f,2.72f,0),.07f,wood);
    MeshObject("AcceptedLanternCage",t,new Vector3(.51f,2.72f,0),Vector3.one,models[0],bronze);Part("WarmCore",PrimitiveType.Cylinder,t,new Vector3(.51f,2.47f,0),new Vector3(.13f,.10f,.13f),glow);
    if(!preview){var l=Group("BoundedWarmLight",t,new Vector3(.51f,2.47f,0)).gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.65f,.29f);l.range=6;l.intensity=1.6f;l.shadows=LightShadows.None;}
   }else if(type==BayBuildableType.CoralReefGarden){
    float y=-depth+.06f;
    bool softenReef=!preview&&SeaBayGameplayLoopV34.Enabled;
    Part("GroundedReefBase",PrimitiveType.Cylinder,t,new Vector3(0,y-(softenReef ? .12f : 0),0),new Vector3(1.60f,softenReef ? .045f : .09f,1.52f),stone);
    if(softenReef) for(int i=0;i<5;i++){
     float angle=i*72f*Mathf.Deg2Rad+.35f;
     AddCoastalRockV38(t,"ReefRimRock",new Vector3(Mathf.Cos(angle)*.65f,y,Mathf.Sin(angle)*.61f),
      new Vector3(.40f+i*.03f,.23f,.32f),i*47f);
    }
    float height=Mathf.Min(1.05f,depth-.35f);float modelH=Mathf.Max(.1f,models[1].bounds.max.y);
    var c=MeshObject("AcceptedBranchingCoral",t,new Vector3(-.24f,y+.08f,0),Vector3.one*(height/modelH),models[1],coral[0]);c.transform.localRotation=Quaternion.Euler(0,35,0);
    c=MeshObject("AcceptedOrangeCoral",t,new Vector3(.40f,y+.08f,.32f),Vector3.one*(height/modelH*.58f),models[1],coral[1]);c.transform.localRotation=Quaternion.Euler(0,148,0);
    MeshObject("CleanGreenKelp",t,new Vector3(.35f,y+.03f,-.40f),new Vector3(.48f,height/1.85f,.48f),models[2],kelp);
   }else{
    var f=Group("Float",t,Vector3.zero);Part("WhiteFloat",PrimitiveType.Cylinder,f,new Vector3(0,.12f,0),new Vector3(.33f,.13f,.33f),buoyWhite);Part("RedFloatBand",PrimitiveType.Cylinder,f,new Vector3(0,.16f,0),new Vector3(.335f,.045f,.335f),buoyRed);Rod("BuoyEye",f,new Vector3(0,.22f,0),new Vector3(0,.44f,0),.045f,bronze);
    float y=-depth+.24f;Rod("HempMooring",t,new Vector3(0,y,0),new Vector3(0,0,0),.025f,rope);
    foreach(float xx in new[]{-.46f,.46f})foreach(float zz in new[]{-.34f,.34f})Rod("PotCorner",t,new Vector3(xx,y-.20f,zz),new Vector3(xx,y+.20f,zz),.04f,wood);
    for(int h=0;h<3;h++){float yy=y-.20f+h*.20f;Box("PotRim",t,new Vector3(0,yy,-.34f),new Vector3(.96f,.035f,.035f),wood);Box("PotRim",t,new Vector3(0,yy,.34f),new Vector3(.96f,.035f,.035f),wood);Box("PotRim",t,new Vector3(-.46f,yy,0),new Vector3(.035f,.035f,.72f),wood);Box("PotRim",t,new Vector3(.46f,yy,0),new Vector3(.035f,.035f,.72f),wood);}
    for(int i=0;i<7;i++){float x=-.42f+i*.14f;Rod("PotWovenUpright",t,new Vector3(x,y-.2f,-.34f),new Vector3(x,y+.2f,-.34f),.018f,rope);Rod("PotWovenUpright",t,new Vector3(x,y-.2f,.34f),new Vector3(x,y+.2f,.34f),.018f,rope);}
   }
   CombineStaticParts(g);return g;
  }
  internal bool VerifyCraftPierV32(bool preview)
  {
   var pier = Structure(BayBuildableType.WoodenPierDeck, preview, 1.2f);
   pier.SetActive(false);
   var deck = pier.transform.Find("Combined_V32_WeatheredPlank");
   bool valid = deck != null && deck.GetComponent<MeshFilter>().sharedMesh.colors.Length > 0
     && deck.GetComponent<MeshRenderer>().sharedMaterial.shader.name == "Deepsea/StylizedCoastalCraftV32"
     && pier.GetComponents<BoxCollider>().Length == (preview ? 0 : 5);
   Destroy(pier);
   return valid;
  }

  // Merge same-material static parts once per construction, leaving coral sway and buoy motion intact.
  internal void CombineStaticParts(GameObject go){
   var staticParts=go.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<Renderer>()!=null&&!f.name.Contains("Accepted")&&!f.name.Contains("Kelp")&&!f.transform.parent.name.Equals("Float")).ToArray();
   foreach(var group in staticParts.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial)){
    if(group.Count()<2)continue;var mesh=new Mesh{name="V28_Combined_"+group.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=go.transform.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);(go.GetComponent<BayOwnedGeometryV28>()??go.AddComponent<BayOwnedGeometryV28>()).meshes.Add(mesh);MeshObject("Combined_"+group.Key.name,go.transform,Vector3.zero,Vector3.one,mesh,group.Key);
    foreach(var f in group){f.GetComponent<Renderer>().enabled=false;Destroy(f.gameObject);}
   }
  }
  readonly List<Light> bayLights=new List<Light>();readonly Vector4[] lampPositions=new Vector4[4],lampColors=new Vector4[4];float nextLightBudget;
  void UpdateLampBudget(){if(Time.unscaledTime<nextLightBudget)return;nextLightBudget=Time.unscaledTime+.25f;bayLights.RemoveAll(l=>l==null);var nearest=bayLights.OrderBy(l=>(l.transform.position-game.view.transform.position).sqrMagnitude).Take(4).ToArray();foreach(var l in bayLights)l.enabled=nearest.Contains(l);for(int i=0;i<4;i++){if(i<nearest.Length){var l=nearest[i];var p=l.transform.position;lampPositions[i]=new Vector4(p.x,p.y,p.z,l.range);lampColors[i]=(Vector4)(l.color*l.intensity);}else{lampPositions[i]=Vector4.zero;lampColors[i]=Vector4.zero;}}Shader.SetGlobalInt("_BayV28LampCount",nearest.Length);Shader.SetGlobalVectorArray("_BayV28Lamps",lampPositions);Shader.SetGlobalVectorArray("_BayV28LampColors",lampColors);}
  void AddCoastalRockV38(Transform parent,string partName,Vector3 position,Vector3 scale,float yaw){
   var rock=Part(partName,PrimitiveType.Sphere,parent,position,scale,stone);
   rock.transform.localRotation=Quaternion.Euler(8,yaw,-6);
   rock.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.On;
  }

  void AddLanternPlinthV381(Transform lantern){
   var plinth=Group("LanternReefPlinth",lantern,Vector3.zero);
   SeaCinematicLightingAndAtmosphereV37.BuildLanternPlinthV381(game,plinth,stone,wood);
   CombineStaticParts(plinth.gameObject);
  }

  void Spawn(BayPlacedBuildingEntry b){var cell=new Vector2Int(b.gridX,b.gridZ);var p=CellPosition(cell);var obj=Structure((BayBuildableType)b.buildType,false,Mathf.Clamp(DepthAt(p,Connector(cell,b.buildType)),.6f,SeaSeamlessWorldAndNauticalUIV29.SceneEnabled?15:5.5f));obj.name="V28_Built_"+b.buildType+"_"+b.gridX+"_"+b.gridZ;obj.transform.SetParent(buildRoot,false);obj.transform.position=p;obj.transform.rotation=Quaternion.Euler(0,b.rotationStep*90,0);buildings[cell]=obj;bayLights.AddRange(obj.GetComponentsInChildren<Light>());
   if (SeaBayGameplayLoopV34.Enabled && b.buildType == (int)BayBuildableType.PierLanternPost &&
       DepthAt(p) > .45f)
   {
    AddLanternPlinthV381(obj.transform);
    Debug.Log($"V38_LANTERN_PLINTH_READY {b.gridX},{b.gridZ}");
   }}
 }
}
