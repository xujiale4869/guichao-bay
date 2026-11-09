using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Deepsea {
 [DefaultExecutionOrder(960)]
 public sealed partial class SeaSeamlessWorldAndNauticalUIV29:MonoBehaviour {
  public static SeaSeamlessWorldAndNauticalUIV29 Active{get;private set;}
  public static bool IsV31{get{return SceneManager.GetActiveScene().name=="CoastSeaVillageV31"||Environment.GetCommandLineArgs().Contains("-village-v31");}}
  public static bool IsV30{get{return IsV31||SceneManager.GetActiveScene().name=="CoastSeaUnifiedV30"||Environment.GetCommandLineArgs().Contains("-unified-v30");}}
  public static bool SceneEnabled{get{return IsV30||SceneManager.GetActiveScene().name=="CoastSeaSeamlessV29"||Environment.GetCommandLineArgs().Contains("-seamless-v29");}}
  public static bool Checking{get{return (Environment.GetCommandLineArgs().Contains("-seamless-v29-check")||Environment.GetCommandLineArgs().Contains("-unified-v30-check")||Environment.GetCommandLineArgs().Contains("-village-v31-check"));}}
  static readonly string TestId=DateTime.UtcNow.Ticks.ToString();
  public static string TestPlayerPath{get{return Path.Combine(Application.temporaryCachePath,"seamless-v29-player-"+TestId+".json");}}
  public static string LayoutPath{get{return Path.Combine(Checking?Application.temporaryCachePath:Application.persistentDataPath,Checking?"seamless-v29-layout-"+TestId+".json":SeaBayGameplayLoopV34.Enabled?"sea-bay-expansion-v34.json":IsV31?"sea-bay-expansion-v31.json":IsV30?"sea-bay-expansion-v30.json":"sea-bay-expansion-v29.json");}}
  public bool v29WorldEnabled=true,v29NauticalUIEnabled=true;public float customSailZoom=15.5f;
  public bool Ready{get;private set;}public bool UIReady{get;private set;}
  SeaLifeRuntime game;SeaBayExpansionAndBuildSpaceV28 bay;Transform worldRoot;NativeBayHeightfieldV29 native;
  readonly List<UnityEngine.Object> _owned=new List<UnityEngine.Object>();
  MeshFilter waterFilter;Mesh priorWaterMesh;Vector3 priorWaterPosition,priorWaterScale;Material nativeSand,nativeRock;Mesh[] rocks;
  Transform lampRoot;Renderer lampCore;Light lampLight;public bool LanternOn=true;
  TMP_FontAsset _cnFont;Sprite _roundedCardSprite,_pillBadgeSprite,_iconSlotSprite,_iconSlotActiveSprite;Sprite[] icons;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Bootstrap(){if(!SceneEnabled||FindObjectOfType<SeaSeamlessWorldAndNauticalUIV29>()!=null)return;var rt=FindObjectOfType<SeaLifeRuntime>();if(rt!=null)rt.gameObject.AddComponent<SeaSeamlessWorldAndNauticalUIV29>();}
  void Awake(){Active=this;}
  IEnumerator Start(){if(!SceneEnabled){enabled=false;yield break;}game=GetComponent<SeaLifeRuntime>();bay=GetComponent<SeaBayExpansionAndBuildSpaceV28>();while(!game.Ready||bay==null||!bay.Ready)yield return null;
   BuildNauticalUI();UIReady=true;game.ui.ApplyNauticalV29();Ready=true;
   lampRoot=game.boat.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="V27_BoatSternLanternPivot");if(lampRoot!=null){lampCore=lampRoot.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r=>r.name=="V27_LanternWarmCore");lampLight=lampRoot.GetComponentInChildren<Light>(true);}
   if(SeaBayGameplayLoopV34.Enabled){
    BuildGameplayUIV34();
    game.GameplayV34=gameObject.AddComponent<SeaBayGameplayLoopV34>();
    game.GameplayV34.Initialize(game,bay,this,housesV31);
   }
   game.Notify("雾岬岛 · 东岸  |  滚轮拉远视野，B / G 海湾营造。");Debug.Log("SEAMLESS_V29_READY nativeMeshes="+native.sourceMeshes+" samples="+native.coveredSamples+" cliffs="+coastCount+" reef="+reefCount);
   if(Checking)StartCoroutine(ReviewV29());
  }
  void Update(){if(!Ready)return;if(game.InRoom&&Input.GetKeyDown(KeyCode.V)&&!game.save.tanks[game.SelectedTank].built)game.Notify("先在当前建设位摆放展缸，再按 V 近景观赏。");if(Input.GetKeyDown(KeyCode.L)&&!game.Paused&&!game.WorkshopOpenV34&&game.Panel==0)ToggleLantern();if(!game.InRoom&&!game.OnFoot&&!game.Busy&&!game.Paused&&!game.WorkshopOpenV34&&game.Panel==0&&!bay.godBuildMode&&!PointerOverUI())AdjustSailZoom(Input.mouseScrollDelta.y);}
  public void AdjustSailZoom(float wheel){customSailZoom=Mathf.Clamp(customSailZoom-wheel*1.8f,8.5f,34);}
  public static float SailingSize(SeaLifeRuntime g,float normal){return Active!=null&&Active.Ready&&!g.InRoom&&!g.OnFoot&&!g.Busy?Active.customSailZoom:normal;}
  public void ToggleLantern(){LanternOn=!LanternOn;}
  void LateUpdate(){if(!Ready)return;RefreshNauticalUI();CameraReviewV31();if(lampCore!=null)lampCore.enabled=LanternOn;if(lampLight!=null)lampLight.enabled=LanternOn;if(!LanternOn)Shader.SetGlobalVector("_HarborV27BoatColor",Vector4.zero);}
  public static bool PointerOverUI(){return SeaWaterArtUpgradeV26.PointerOverTuningPanelV262||(EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject());}
  public Sprite CardSprite{get{return _roundedCardSprite;}}public Sprite SlotSprite{get{return _iconSlotSprite;}}
  void OnDestroy(){RestoreNativeEcologyV30();if(waterFilter!=null){waterFilter.sharedMesh=priorWaterMesh;waterFilter.transform.position=priorWaterPosition;waterFilter.transform.localScale=priorWaterScale;}if(worldRoot!=null)Destroy(worldRoot.gameObject);if(nauticalCanvas!=null)Destroy(nauticalCanvas.gameObject);foreach(var a in _owned)if(a!=null)Destroy(a);if(Active==this)Active=null;}
 }
}
